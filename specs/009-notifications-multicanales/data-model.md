# Phase 1 — Modèle de Données : Feature 009

> **Document** : `specs/009-notifications-multicanales/data-model.md`
> **Date** : 1er septembre 2026
> **Fondement CDC** : §20 (*Modèle de données minimal*) — *« **Notification (canal, modèle, destinataire, statut d'envoi)** »*. Les quatre attributs exigés sont intégralement portés (§2, §3).

---

## 1. Entités réutilisées sans modification

| Entité existante | Feature | Usage par F009 |
|:---|:---:|:---|
| `Ticket` | F001 | Objet référencé par la notification (COM-02, NOT-01). **Non modifié** |
| `Dossier` | F001 | `ClientId` → résolution du destinataire client (research.md D11). **Non modifié** |
| `Message` | F001 | Source de l'événement `CollaborateurMentionne` (MSG-04). **Non modifié** — F009 ne crée aucun second journal de messages (`spec.md` §12) |
| `UtilisateurSociete` | F002 | `DroitConsultation` → fan-out aux collaborateurs habilités. **Lu, jamais réinterprété** |
| `EntreeHistorique`, `EntreeJournalAudit` | F001 | Traçage (`spec.md` §16). **Aucun second journal d'audit** |
| `Alerte` | F001 | **Non lue par F009** — ALE-03 n'est pas raccordée (research.md D6) |

---

## 2. Entité étendue — une seule, additive

### `EvenementSocle` (F001) — +1 propriété

| Propriété | Type | Contrainte | Justification |
|:---|:---|:---|:---|
| `DistribueLeUtc` | `DateTime?` | Nullable, `null` par défaut, **indexée** | Curseur de distribution (research.md D2). `null` = à examiner |

**Aucune autre propriété n'est ajoutée. Aucune propriété existante n'est modifiée ni supprimée.** La colonne est rétro-compatible : les lignes historiques valent `null`, sont examinées une fois, puis marquées — sans produire de notification si aucune règle ne s'applique.

**Écart signalé (Principe XVII)** : modification additive d'une entité de F001. Aucune règle métier n'est altérée.

---

## 3. Entités nouvelles

### 3.1 `Notification` — le fait adressé à **un** destinataire

Porte les attributs CDC §20 **destinataire** et **modèle**.

| Propriété | Type | Contrainte |
|:---|:---|:---|
| `Id` | `Guid` | Clé |
| `EvenementSourceId` | `Guid` | FK `EvenementSocle` — **idempotence** |
| `DestinataireId` | `Guid` | **Un seul destinataire nommé** (CDC §20 au singulier, CLAR-NOT-04) |
| `TypeEvenement` | `string` | Type de l'événement source (ex. `CollaborateurMentionne`) |
| `ModeleId` | `Guid?` | FK `ModeleNotification` — **attribut « modèle » du CDC §20** |
| `Titre` | `string` | Rendu à la création, figé ensuite |
| `Corps` | `string` | Rendu à la création, figé ensuite |
| `TicketId` | `Guid?` | Référence à l'objet métier (COM-02, NOT-01). **Jamais une URL** (research.md D12) |
| `EstLue` | `bool` | **NOT-01** — `false` à la création |
| `LueLeUtc` | `DateTime?` | Horodatage de la transition |
| `CreeeLeUtc` | `DateTime` | Date affichée dans le centre (`spec.md` §6) |

**Index unique** : `(EvenementSourceId, DestinataireId)` — un même événement ne peut produire deux fois la même notification pour le même destinataire (RSQ-02).

**Invariants.**
1. `DestinataireId` est **immuable** après création.
2. `Titre` et `Corps` sont **figés à la création** : modifier un modèle plus tard n'altère jamais une notification déjà émise (`TEST-NOT-02` cas 3).
3. `MarquerCommeLue()` est **idempotente** : sur une notification déjà lue, elle ne modifie rien et **ne lève aucune erreur** (`TEST-NOT-01` cas 3).
4. **Aucune suppression, aucun archivage** (research.md D13). Il n'existe ni méthode `Supprimer`, ni indicateur d'archivage.

**Transitions d'état** — une seule, irréversible :

```
Non lue ──MarquerCommeLue()──► Lue
   ▲                             │
   └──── aucune transition ──────┘   (le CDC ne prévoit aucun « marquer comme non lu »)
```

### 3.2 `EnvoiNotification` — une tentative de diffusion sur **un** canal

Porte les attributs CDC §20 **canal** et **statut d'envoi**.

| Propriété | Type | Contrainte |
|:---|:---|:---|
| `Id` | `Guid` | Clé |
| `NotificationId` | `Guid` | FK `Notification` |
| `Canal` | `CanalNotification` | **Attribut « canal » du CDC §20** |
| `Statut` | `StatutEnvoi` | **Attribut « statut d'envoi » du CDC §20** |
| `NombreTentatives` | `int` | Incrémenté à chaque tentative (ERR-02) |
| `DerniereTentativeUtc` | `DateTime?` | — |
| `AboutiLeUtc` | `DateTime?` | Renseigné au succès |
| `MotifEchec` | `string?` | **Motif technique uniquement — jamais le contenu du message** (`spec.md` §16, Principe XIV) |

**Index** : `(Statut, DerniereTentativeUtc)` — sélection des envois à reprendre.

**Aucun `EnvoiNotification` n'est créé pour le canal In-App** : l'existence de la `Notification` **est** sa diffusion (research.md D3).

**Invariant d'isolation (ERR-02)** : chaque `EnvoiNotification` évolue **indépendamment** des autres envois de la même notification. L'échec de l'un n'altère jamais le statut d'un autre.

### 3.3 `ModeleNotification` — COM-03

Patron `CategorieReclamation` (research.md D4).

| Propriété | Type | Contrainte |
|:---|:---|:---|
| `Id` | `Guid` | Clé |
| `TypeEvenement` | `string` | Obligatoire |
| `Canal` | `CanalNotification` | Obligatoire — *« par module, par étape, **par canal** »* |
| `GabaritTitre` | `string` | Obligatoire |
| `GabaritCorps` | `string` | Obligatoire |
| `EstActif` | `bool` | `true` à la création ; `Desactiver()` / `Activer()` |

**Index unique** : `(TypeEvenement, Canal)` sur les modèles actifs.

> **Aucun contenu de modèle n'est figé par la conception** (CLAR-NOT-05, **AVANT RECETTE**). Les gabarits semés sont des **valeurs de démarrage administrables**, explicitement marquées comme telles, jamais des libellés contractuels.

### 3.4 `RegleDiffusion` — NOT-02

| Propriété | Type | Contrainte |
|:---|:---|:---|
| `Id` | `Guid` | Clé |
| `TypeEvenement` | `string` | Obligatoire |
| `Canal` | `CanalNotification` | Obligatoire — *« paramétrables **par événement et par canal** »* |
| `EstObligatoire` | `bool` | **CLAR-COM-03** — si `true`, non débrayable par la préférence client (COM-05) |
| `EstActif` | `bool` | `true` à la création |

**Index unique** : `(TypeEvenement, Canal)`.

> `EstObligatoire` est une **propriété de la règle**, jamais une constante codée en dur par type d'événement (`spec.md` §21.1).

### 3.5 `PreferenceCanalUtilisateur` — COM-05 (Should)

| Propriété | Type | Contrainte |
|:---|:---|:---|
| `Id` | `Guid` | Clé |
| `UtilisateurId` | `Guid` | — |
| `Canal` | `CanalNotification` | — |
| `EstActif` | `bool` | `true` par défaut |

**Index unique** : `(UtilisateurId, Canal)`.

**Invariant COM-05** : une règle dont `EstObligatoire = true` **ignore** cette préférence. La préférence n'a **aucun effet sur le canal In-App** — le centre de notifications ne se désactive pas, sans quoi NOT-01 et NOT-03 deviendraient inatteignables.

---

## 4. Énumérations

```
CanalNotification : InApp | Email | Sms | WhatsApp
```
**Aucun autre canal** — ni push mobile, ni courrier, ni voix, ni webhook (`spec.md` §9).

```
StatutEnvoi : EnAttente | Envoye | Echec | NonDelivrable
```
- `NonDelivrable` : la coordonnée requise est absente pour ce destinataire (CLAR-COM-04). Distinct de `Echec` (le transport a échoué) — la distinction est **tracée**, sa sémantique définitive reste **à confirmer avant implémentation**.
- **Aucun statut « abandonné »** n'est défini : la condition d'arrêt du retry n'est pas spécifiée par le CDC (CLAR-NOT-02).

---

## 5. Relations

```
EvenementSocle (F001, +DistribueLeUtc)
      │ 1
      │        ┌───────────────► ModeleNotification (COM-03)
      ▼ N      │ N            1
  Notification ┘                          RegleDiffusion (NOT-02)
      │ 1                                        │
      │                                          │ consultée à la distribution
      ▼ 0..N                                     ▼
 EnvoiNotification ─── Canal, StatutEnvoi   PreferenceCanalUtilisateur (COM-05)
      (jamais pour InApp)

  Notification ──► DestinataireId (utilisateur nommé, jamais un groupe)
  Notification ──► TicketId (référence ; jamais une URL — SEC-12)
```

---

## 6. Traçabilité CDC §20

| Attribut exigé | Porté par | Vérification |
|:---|:---|:---|
| **canal** | `EnvoiNotification.Canal` · `ModeleNotification.Canal` · `RegleDiffusion.Canal` | ✅ |
| **modèle** | `Notification.ModeleId` → `ModeleNotification` | ✅ |
| **destinataire** | `Notification.DestinataireId` (**singulier**) | ✅ |
| **statut d'envoi** | `EnvoiNotification.Statut` | ✅ |

---

## 7. Migration

**Une migration EF Core est requise** — contrairement à F008 (zéro migration) :
- 5 tables créées : `Notifications`, `EnvoisNotification`, `ModelesNotification`, `ReglesDiffusion`, `PreferencesCanalUtilisateur` ;
- 1 colonne ajoutée : `JournalEvenements.DistribueLeUtc` (nullable) ;
- 5 index (dont 4 uniques) ;
- **aucune suppression de table, de colonne ou de contrainte** ;
- **aucune donnée existante modifiée**.

**Seed de développement** — strictement borné :
- `RegleDiffusion` semées **uniquement pour les événements de niveau CDC-EXIGENCE** (`spec.md` §10.2) : `TicketCree` (InApp + Email, `EstObligatoire = true` — SOC-01/IMP-03), `ComplementDemande` (InApp + Email — IMP-05), `FactureEditee` (InApp + Email — IMP-11), `TicketTransfere` (InApp + Email — TRF-05), `CollaborateurMentionne` (**InApp seul** — MSG-04, research.md D5) ;
- **aucune règle semée** pour les événements PC-17-PROPOSITION (CLAR-COM-02) ;
- **aucune règle semée** pour `SlaBientotDepasse` / `SlaDepasse` (research.md D6) ;
- `ModeleNotification` : un gabarit de démarrage par couple semé, **explicitement marqué comme administrable et non contractuel** (CLAR-NOT-05).
