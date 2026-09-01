# Phase 1 — Contrats de Cas d'Usage : Feature 009

> **Document** : `specs/009-notifications-multicanales/contracts/use-cases.md`
> **Date** : 1er septembre 2026
> **Règle** : chaque contrat référence son **exigence CDC exacte**, son **autorisation vérifiée côté serveur**, son **événement d'audit**, et son **test**. Aucun contrat n'existe sans exigence source (Principe II).

---

## Vue d'ensemble

| # | Contrat | Exigence | Priorité | Test |
|:--|:---|:---|:---:|:---|
| **C9-01** | Distribuer les notifications d'un événement | COM-01, NOT-02 | Must | `TEST-NOT-02` 1→4 |
| **C9-02** | Consulter mon centre de notifications | NOT-01 | Must | `TEST-NOT-01` 1, 5, 6, 7 |
| **C9-03** | Compter mes notifications non lues | NOT-03 | Should | `TEST-NOT-01` 1, 2 |
| **C9-04** | Marquer une notification comme lue | NOT-01 | Must | `TEST-NOT-01` 2, 3, 6 |
| **C9-05** | Accéder au ticket depuis une notification | COM-02 + SEC-12 | Must | `TEST-NOT-01` 4, 5, 8 |
| **C9-06** | Administrer les modèles de messages | COM-03 | Must | `TEST-NOT-02` 5, 6 |
| **C9-07** | Administrer les règles de diffusion | NOT-02 | Must | `TEST-NOT-02` 1→6 |
| **C9-08** | Réessayer les envois en échec | ERR-02 | Must (transverse) | `TEST-ERR-02` 1→6 |
| **C9-09** | Gérer mes préférences de canaux | COM-05 | Should | `TEST-NOT-02` 7 |
| **C9-10** | Notifier la relance d'absence de réponse | COM-06 | Could | `TEST-COM-06` 1→4 |
| **C9-11** | **Raccorder la mention `@Nom`** | **NOT-01 appliquée à un agent** ; raccordement de **MSG-04 (F001)** | Must (capacité) | `TEST-MSG-04` 1→8 |

---

## C9-01 — Distribuer les notifications d'un événement

- **Exigences** : COM-01 (émission), NOT-02 (règles de diffusion), COM-03 (modèles), CDC §20
- **Acteur** : Système (traitement de fond, research.md D1/D8)
- **Déclencheur** : présence d'un `EvenementSocle` dont `DistribueLeUtc is null`
- **Autorisation** : sans objet — aucun acteur humain ; **aucun point d'entrée utilisateur n'est exposé**

**Comportement.** Pour chaque événement non distribué, dans l'ordre chronologique :
1. Chercher les `RegleDiffusion` actives pour son `TypeEvenement`. **Aucune règle → l'événement est marqué distribué sans produire de notification** (silence = pas de notification, `spec.md` §10).
2. Résoudre les destinataires via `IResolveurDestinataires` (research.md D11) — **toujours des utilisateurs nommés**.
3. Pour chaque destinataire, créer **une** `Notification` (idempotence par l'index unique `(EvenementSourceId, DestinataireId)`), avec `Titre`/`Corps` **rendus et figés** depuis le `ModeleNotification` du couple `(TypeEvenement, InApp)`.
4. Pour chaque canal externe de la règle : créer un `EnvoiNotification` en `EnAttente`, **sauf** si `RegleDiffusion.EstObligatoire = false` et que la `PreferenceCanalUtilisateur` du destinataire désactive ce canal (COM-05).
5. Si la coordonnée requise est absente → `EnvoiNotification` en **`NonDelivrable`**, tracé (CLAR-COM-04).
6. Marquer l'événement `DistribueLeUtc`.

**Garanties.**
- **Aucun canal `InApp` ne produit d'`EnvoiNotification`** (research.md D3).
- **Aucune notification vers un client depuis une note interne** : les événements `MessagePoste` de type note interne et `CollaborateurMentionne` ne résolvent **jamais** un destinataire client (MSG-02, `spec.md` §14 règle 3).
- Un échec de distribution sur un événement **n'empêche pas** le traitement des suivants.
- **Aucune transaction métier n'est impactée** : elle est committée depuis longtemps (research.md D1).

**Audit** : `NotificationCreee` par notification produite (`spec.md` §16).

---

## C9-02 — Consulter mon centre de notifications

- **Exigence** : NOT-01 · CDC §14.1 (*« centre de notifications regroupant les événements adressés à l'utilisateur »*)
- **Acteur** : utilisateur authentifié — **Client ou Agent**
- **Entrée** : identité dérivée du contexte authentifié — **jamais un identifiant passé par l'appelant**

**Sortie** — par notification : état Lu/Non lu, date, type, titre, corps, référence du ticket (`spec.md` §6).

**Autorisation (NON-NÉGOCIABLE).** Le filtre est une **égalité stricte** `DestinataireId == utilisateur authentifié`, appliquée **côté serveur dans la requête**, jamais après chargement. Conséquences vérifiées :
- un utilisateur ne voit jamais la notification d'un autre, **même en connaissant son identifiant** (anti-IDOR) ;
- aucune fuite inter-sociétés, par construction (le destinataire est un utilisateur, pas une société) ;
- **un administrateur fonctionnel n'a aucun accès** aux notifications d'autrui (`spec.md` §14 règle 1).

**Non authentifié** → accès refusé, invitation à s'authentifier. **Aucun détail n'est exposé.**

---

## C9-03 — Compter mes notifications non lues

- **Exigence** : NOT-03 (Should) — *« indicateur du nombre de notifications non lues visible en permanence »*
- **Acteur** : utilisateur authentifié
- **Sortie** : entier ≥ 0

**Comportement.** Compte les `Notification` du seul utilisateur authentifié dont `EstLue = false`. Même filtre serveur que C9-02.

**Zéro** est un résultat normal, jamais une erreur ; l'indicateur retombe à zéro quand plus aucune notification n'est non lue (`spec.md` §5).

---

## C9-04 — Marquer une notification comme lue

- **Exigence** : NOT-01 — *« Chaque notification dispose d'un état Lu / Non lu »*
- **Acteur** : utilisateur authentifié destinataire

**Comportement.** `EstLue ← true`, `LueLeUtc ← maintenant`. **Idempotent** : sur une notification déjà lue, aucun changement, **aucune erreur** (`TEST-NOT-01` cas 3).

**Autorisation.** Refus **côté serveur** si le demandeur n'est pas le destinataire — y compris s'il connaît l'identifiant. Le refus **ne divulgue pas** l'existence de la notification.

**Règle de non-duplication ACR-01** (`spec.md` §11) : marquer comme lue **ne retire jamais** l'action requise correspondante du tableau de bord client. F009 **ne touche pas** au bloc « Mes actions requises ».

**Audit** : `NotificationLue`.

---

## C9-05 — Accéder au ticket depuis une notification

- **Exigences** : COM-02 (*« un lien invitant le client à se connecter directement à son ticket »*) + **SEC-12** (contrainte)
- **Acteur** : utilisateur authentifié destinataire

**Comportement.** La notification porte `TicketId` ; le lien est **relatif** (`/tickets/{id}`). L'accès au ticket est arbitré par les mécanismes d'authentification et d'habilitation **déjà en place** — F009 n'en crée aucun.

**Interdictions actives (SEC-12).**
- **Aucun jeton d'accès**, aucun lien signé, aucun paramètre de contournement, sur **aucun canal**.
- **Aucun lien direct vers un document protégé.**
- Non authentifié → conduite au contrôle d'identité, **sans exposer aucun détail du dossier**.
- Ticket devenu inaccessible (habilitation retirée, rattachement modifié) → refus propre **sans divulguer** l'existence, le contenu ni l'appartenance du ticket.

---

## C9-06 — Administrer les modèles de messages

- **Exigence** : COM-03 — *« Les événements déclencheurs et les modèles de messages sont paramétrables par l'administrateur fonctionnel (par module, par étape, par canal) »*
- **Acteur** : administrateur fonctionnel habilité
- **Autorisation** : permission d'administration des notifications, **vérifiée côté serveur**, indépendamment de ce que l'interface affiche

**Comportement.** Créer, modifier, activer/désactiver un `ModeleNotification` pour un couple `(TypeEvenement, Canal)`. **Aucune suppression physique** (patron F006).

**Effet dans le temps.** Le paramétrage s'applique **aux événements survenant ensuite**, sans redéploiement. **Les notifications déjà émises ne sont jamais modifiées** — `Titre`/`Corps` sont figés à la création (data-model.md §3.1, invariant 2).

**Audit** : `ModeleNotificationModifie`, avec auteur et valeurs **avant / après**.

> **Aucun contenu de modèle n'est arrêté par ce contrat** (CLAR-NOT-05, **AVANT RECETTE**).

---

## C9-07 — Administrer les règles de diffusion

- **Exigence** : NOT-02 — *« Les règles de diffusion sont paramétrables par événement et par canal »*
- **Acteur** : administrateur fonctionnel habilité
- **Autorisation** : identique à C9-06, **côté serveur**

**Comportement.** Créer, modifier, activer/désactiver une `RegleDiffusion` pour un couple `(TypeEvenement, Canal)`, et positionner `EstObligatoire`.

**Granularité.** Modifier la règle de l'événement E1 **n'affecte jamais** E2 (`TEST-NOT-02` cas 4).

**Audit** : `RegleDiffusionModifiee`, avec auteur et valeurs **avant / après**.

> **La liste initiale des règles n'est pas arrêtée par ce contrat** : seules les règles de niveau **CDC-EXIGENCE** sont semées (data-model.md §7). CLAR-COM-02 / PC-17 restent ouverts.

---

## C9-08 — Réessayer les envois en échec

- **Exigence** : **ERR-02** (Must, transverse) — *« nouvelle tentative automatique et traçage ; l'échec d'un canal n'empêche pas les autres »* · CDC §20 (*statut d'envoi*)
- **Acteur** : Système (traitement de fond, research.md D8)
- **Autorisation** : sans objet — **aucun point d'entrée utilisateur**

**Comportement.** Sélectionner les `EnvoiNotification` en `Echec` éligibles selon les **paramètres de configuration** ; réémettre via `ICanalNotification` ; mettre à jour `Statut`, `NombreTentatives`, `DerniereTentativeUtc`, et `MotifEchec` en cas d'échec.

**Garanties vérifiées.**
- **Isolation** : chaque envoi est traité indépendamment ; l'échec de l'un n'altère jamais le statut d'un autre, ni n'interrompt la boucle.
- **Traçage** : destinataire, canal, horodatage, résultat. **Le contenu intégral du message n'est jamais journalisé** (`spec.md` §16, Principe XIV).
- **Aucune substitution de canal** : l'isolation n'est pas une suppléance (`spec.md` §15).
- La transaction métier d'origine **n'est jamais affectée**.

> **Valeurs interdites en dur.** `NombreTentativesMax`, `DelaiEntreTentatives` et la condition d'arrêt proviennent **exclusivement de la configuration**. **Aucune valeur par défaut n'est écrite dans le code** ; l'absence de configuration est une erreur de démarrage explicite, jamais une valeur devinée (research.md D9). **Aucun backoff exponentiel, aucun fallback, aucune alerte administrateur, aucun message d'échec au destinataire** (CLAR-NOT-02).

---

## C9-09 — Gérer mes préférences de canaux

- **Exigence** : COM-05 (Should) — *« Préférences de canaux par client (activation/désactivation par canal, **dans le respect des notifications obligatoires**) »*
- **Acteur** : le client lui-même, pour lui-même **uniquement**

**Comportement.** Activer / désactiver un canal. Prise en compte à la **prochaine** distribution.

**Invariants.**
- Une règle `EstObligatoire = true` est diffusée **malgré** la désactivation (`TEST-NOT-02` cas 7).
- **Le canal In-App n'est pas débrayable** — le désactiver rendrait NOT-01 et NOT-03 inatteignables.
- Un utilisateur **ne modifie que ses propres** préférences (vérifié côté serveur).
- **Aucune préférence au-delà du canal** : ni fréquence, ni langue, ni type d'événement, ni plage horaire (`spec.md` §21).

**Audit** : `PreferenceCanalModifiee`.

---

## C9-10 — Notifier la relance d'absence de réponse

- **Exigence** : **COM-06** (Could) — dimension **canal** de CYC-04
- **Acteur** : Système
- **Déclencheur** : événement `RelanceEmise`, **déjà publié par `RelancesAbsenceReponseUseCase` (F001)**

**Comportement.** Aucun développement spécifique : C9-01 traite `RelanceEmise` comme tout autre type, dès qu'une `RegleDiffusion` est configurée pour lui.

**Frontière (NON-NÉGOCIABLE).** Le **mécanisme** de relance est **CYC-04 (Must), propriété de F001**. F009 **ne crée aucun second mécanisme de relance**, ne modifie ni la cadence, ni le nombre de relances, ni la suggestion de clôture. Sans canal configuré, le mécanisme CYC-04 **s'exécute normalement** (`TEST-COM-06` cas 4).

**Conditionnalité.** COM-06 étant **Could**, sa non-livraison est une **dette tracée**, pas une omission (précédent ENR-04/ENR-05 en F008).

---

## C9-11 — Raccorder la mention `@Nom` au centre de notifications

- **Capacité tracée** : **NOT-01 (Must, F009)** appliquée à un destinataire **agent**
- **Exigence raccordée** : **MSG-04 (Should)** — *« la personne mentionnée reçoit une notification dans la plateforme et, selon paramétrage, un e-mail avec lien vers le ticket »*
- **Décision** : **Option B validée** (`spec.md` §13.1)
- **Acteur** : Système
- **Déclencheur** : événement `CollaborateurMentionne`, **déjà publié par `MentionnerCollaborateurCommand` (F001)**, dont la charge utile **porte déjà `collaborateurId`**

**Comportement.** `ResolveurCollaborateurMentionne` lit `collaborateurId` ; C9-01 produit **une notification In-App** pour ce seul collaborateur, à l'état `Non lu`, avec date, type, titre et **lien vers le ticket**. L'indicateur de non-lus du collaborateur est incrémenté.

**E-mail — strictement conditionnel.** Émis **si et seulement si** une `RegleDiffusion` `(CollaborateurMentionne, Email)` est active, conformément au *« selon paramétrage »* du CDC. **Aucune règle e-mail n'est semée** : F009 **n'active jamais ce canal par défaut**.

**Ce que ce contrat NE fait PAS.**

| | |
|:---|:---|
| Détecter la mention `@Nom` | **NON** — mécanisme F001, ni réimplémenté ni modifié |
| Modifier `MentionnerCollaborateurCommand` ou `Message` | **NON — zéro ligne de code métier modifiée** (research.md D5) |
| Réaffecter le ticket | **NON** — **MSG-05** préservé par construction : F009 ne produit aucun effet de workflow |
| Notifier le client | **NON** — **MSG-02** : une note interne ne produit **jamais** de notification client, sur aucun canal |
| Notifier les autres agents habilités ou la file | **NON** — **seul** le collaborateur mentionné (moindre privilège, CLAR-NOT-04) |
| Recompter MSG-04 dans la couverture F009 | **NON** — MSG-04 **reste propriété de F001** (`spec.md` §27) |

**Test exigé** : `TEST-MSG-04` — *Mention `@Nom` → notification In-App du collaborateur mentionné* (8 cas, `spec.md` §18 bis).

---

## Point d'intégration ALE-03 — fourni, volontairement non activé

Conformément à la décision **Option B**, F009 fournit le **contrat** permettant au mécanisme SLA existant de produire ultérieurement une notification au superviseur :

- `SurveillerEcheancesUseCase` (F001) **publie déjà** `SlaBientotDepasse` / `SlaDepasse` comme `EvenementSocle` — **le point d'intégration existe donc déjà et ne demande aucun développement** (research.md D6) ;
- l'activation consistera, le jour venu, à ajouter **une `RegleDiffusion`** et **un résolveur « superviseur du périmètre »**.

**Ce que F009 ne livre pas, délibérément** :

| | |
|:---|:---|
| Résolveur « superviseur du périmètre » | **NON implémenté** |
| `RegleDiffusion` pour les événements SLA | **NON semée** |
| Notification d'escalade au superviseur | **NON émise** |
| Modification du workflow SLA de F001 | **NON — aucune** |
| Comptage d'ALE-01/02/03 dans la couverture F009 | **NON** |

**Vérification** : `TEST-MSG-04` cas 8 — un dépassement de SLA **ne produit aucune notification** dans cette feature ; le mécanisme F001 s'exécute inchangé. L'activation relève de **PC-25**.

---

## Contrats explicitement absents

| Contrat non écrit | Raison |
|:---|:---|
| Supprimer / archiver une notification | Non prévu par le CDC (CLAR-NOT-03, research.md D13) |
| Marquer comme **non** lue | Aucune transition inverse prévue par NOT-01 |
| Notifier une file / un groupe / tous les agents | Aucun destinataire collectif au CDC (CLAR-NOT-04) |
| Saisir une adresse ou un numéro de destination | Interdit (`spec.md` §13) |
| Substituer un canal en échec par un autre | Non prévu par le CDC (`spec.md` §15) |
| Consulter les notifications d'un autre utilisateur (même admin) | Interdit (`spec.md` §14 règle 1) |
| Second moteur d'actions requises | Interdit (ACR-01, `spec.md` §11) |
| Second journal de messages | Interdit (COM-04 / SOC-05, `spec.md` §12) |
| Prise de rendez-vous, recherche guichet, continuité | **CON-01→04 — Feature 010** (`spec.md` §23) |
