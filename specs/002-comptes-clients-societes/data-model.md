# Data Model — Modèle conceptuel fonctionnel

**Feature** : `002-comptes-clients-societes` | **Date** : 2026-08-29 | **Phase 1 de `/speckit.plan`**

> **Cadre de ce document** : conformément à la constitution (Portée — pas de code, pas de schéma SQL
> détaillé, pas de classes C#) et aux décisions `research.md` (D1→D12), ce document décrit le modèle
> **au niveau fonctionnel** : entités, attributs métier significatifs, relations, invariants, états et
> traçabilité vers les exigences. Le mapping physique exact (types SQL précis, index, migrations) sera
> défini en `/speckit.tasks` et `/speckit.implement`, dans le respect de D2→D7.

---

## 1. Entités nouvelles (Feature 002)

| Entité | Description | Attributs métier significatifs | Exigences |
|---|---|---|---|
| **Société cliente** | Compte client au sens du CDC — entité principale de ce référentiel (`docs/cdc_text.txt` §10.1) | **Demandeur** (utilisateur à l'origine de la demande — précisé lors de `/speckit.implement` : nécessaire pour désigner automatiquement le premier administrateur à la validation, spec.md § Assumptions), informations/justificatifs déclarés (liste paramétrable — PC-08), profil (**type de client** : normal / en compte / deposit / IES — CPT-02), état (en attente de validation / active / refusée), **nombre d'administrateurs actifs** (invariant CLAR-03) | CPT-01, CPT-02 |
| **Utilisateur rattaché** | Rattachement d'un utilisateur (compte Identity existant, Feature 001) à une société cliente | Société de rattachement, rôle (administrateur de société / utilisateur de société), état (actif/désactivé), **droit de consultation** (ses demandes uniquement / toutes celles de la société), **droit de soumission** | ENT-01, ENT-02, ENT-03 |

**Non-entité, volontairement** : il n'existe **aucune** entité « Demande de création de compte »
distincte de la Société cliente elle-même. Comme `Ticket` en Feature 001 (dont le cycle de vie ne
repose sur aucune entité « TicketDemande » séparée), la Société cliente existe dès la soumission, à
l'état « en attente de validation » — la distinction demande/compte de `spec.md` § Key Entities est
**conceptuelle** (transitoire vs actif), pas une distinction de deux entités techniques (D2).

**Non-entité, volontairement** : il n'existe **aucune** entité « Décision de validation/modification
de compte » distincte — chaque décision (validation, rejet, modification de type) est une entrée du
journal d'audit existant de Feature 001 (D8), jamais une table propre à cette feature.

## 2. Entités étendues (Feature 001, extension additive uniquement)

| Entité | Extension | Justification | Exigences |
|---|---|---|---|
| **Dossier** (`src/CIT.Plateforme.Domain/Tickets/Dossier.cs`) | Nouveau champ **nullable** `AuteurId : Guid?` (utilisateur individuel ayant soumis la demande), en complément du champ existant `ClientId` qui devient, dans le contexte de cette feature, l'identifiant de la **société** propriétaire (CLAR-01). Aucun champ existant supprimé ; `Dossier.Creer(...)` gagne des paramètres optionnels supplémentaires par défaut à `null` (D5, D11) | Permet simultanément le cloisonnement inter-société dur (CPT-04, via `ClientId` — comportement Feature 001 inchangé) et le filtre « ses propres demandes » (ENT-03, via `AuteurId`) | CPT-04, ENT-03 — CLAR-01 |
| **Dossier** *(remédiation post-audit, 2026-08-29 — `post-implementation-audit.md` §8)* | Second nouveau champ **nullable** `TypeClient : TypeClient?` — instantané du profil de la société au moment de la création du dossier, jamais réécrit ensuite. Même paramètre optionnel additif sur `Dossier.Creer(...)` (D3, D11) | Ferme l'écart initialement constaté : sans ce champ, la non-rétroactivité de CPT-06 n'était démontrable que pour la *propriété* du dossier (`ClientId`), jamais pour le *type* applicable. Le câblage automatique depuis un flux métier réel (ex. Billing Office) reste hors périmètre de Feature 002 | CPT-06 |

**Aucune autre entité de Feature 001 n'est modifiée** (`Ticket`, `HabilitationUtilisateur`,
`ApplicationUser`, `EntreeJournalAudit`, etc. restent inchangés en structure et en comportement — D1,
D4, D11).

## 3. Relations principales

```text
ApplicationUser (Identity, Feature 001, nature = Client)
        │ 1..1 (un utilisateur rattaché à au plus une société à la fois — Assumptions spec.md)
        ▼
Utilisateur rattaché ──N..1── Société cliente ──1..N── Utilisateur rattaché (les collègues)
        │
        │ (au moins un « administrateur de société » actif par société — invariant CLAR-03)

Société cliente ──1..N── Dossier (Feature 001, ClientId = Société cliente.Id)
                                │
                                └─ AuteurId = Utilisateur rattaché à l'origine de la demande
```

## 4. États et transitions

### 4.1 Société cliente (état)

```text
EnAttenteValidation ──[validation par administrateur fonctionnel CIT]──▶ Active
EnAttenteValidation ──[rejet, motif obligatoire]──▶ Refusee
```

- Aucune transition automatique, aucun délai/expiration (le CDC n'en prévoit pas — Edge Case non
  résolu, `spec.md`).
- `Active` est un **état terminal accessible** : le type de client peut y être modifié (CPT-06) sans
  changement d'état.
- `Refusee` est terminal : aucune réactivation prévue par le CDC (non inventée ici).

### 4.2 Utilisateur rattaché (état)

```text
Actif ──[désactivation par un administrateur de société]──▶ Désactivé
Désactivé ──[réactivation par un administrateur de société]──▶ Actif
```

- Un utilisateur désactivé ne peut plus s'authentifier dans le contexte de sa société (D10) ; son
  historique n'est jamais supprimé.

## 5. Invariants fonctionnels (extraits traçables)

1. Une **société cliente** n'est jamais utilisable pour s'authentifier avant son état `Active`
   (CPT-01, SC-002 de `spec.md`).
2. Une **société cliente à l'état `Active` possède toujours au moins un administrateur de société
   actif** — le système refuse toute opération (désactivation, retrait du rôle) qui ferait tomber ce
   nombre à zéro (ENT-02 — **CLAR-03**, invariant garanti par concurrence optimiste, `research.md`
   D6/D7).
3. Un **utilisateur rattaché appartient à exactement une société à la fois** (hypothèse de travail
   documentée dans `spec.md` § Assumptions, révisable — NOUVEAU-03 non résolu).
4. Le **droit de consultation par défaut** d'un nouveau rattachement est « ses demandes uniquement »
   — jamais permissif par défaut (Principe VII, moindre privilège).
5. Un **administrateur de société** dispose toujours, de facto, du droit de consultation total sur
   les demandes de sa société, indépendamment de son propre droit individuel paramétré — **CLAR-02**.
6. Le **cloisonnement inter-société est absolu** : un utilisateur d'une société ne voit jamais une
   donnée d'une autre société, quel que soit son rôle ou ses droits (CPT-04 — aucune exception,
   y compris pour un administrateur de société, qui n'a autorité que sur SA société).
7. La **modification du type de client** (CPT-06) est tracée (ancien type, nouveau type, auteur,
   date/heure — via le journal d'audit de Feature 001, D8) et **n'a aucun effet rétroactif** sur les
   demandes (dossiers/tickets) déjà créées avant la modification : ni le champ `ClientId`/société,
   ni le champ `TypeClient` (instantané figé à la création — remédiation post-audit, §2 ci-dessus)
   d'un `Dossier` existant ne sont jamais réécrits par un changement de type ultérieur de la société.
8. **Aucun élément historisé n'est supprimé physiquement** (désactivation d'un utilisateur, refus
   d'une société) — même principe que Feature 001 (Principe VIII).
9. Toute action de gestion sensible (validation de compte, modification de type, rattachement,
   activation/désactivation, modification de droits, attribution/retrait du rôle administrateur)
   produit **au minimum une entrée du journal d'audit existant** de Feature 001 (ENT-04, D8) —
   aucun second journal.

## 6. Données paramétrables (administrées, jamais codées en dur)

Conformément aux Principes VI et XIII :

| Paramètre | Portée | Point ouvert |
|---|---|---|
| Informations et justificatifs exigés à la création d'un compte, par profil | Par type de client | PC-08 (contenu — non fixé ici) |
| Politique de conservation d'un compte désactivé/refusé | Globale | PC-14 (durée — non fixée ici) |

**Immuable, jamais paramétrable** : les quatre valeurs du type de client (normal / en compte /
deposit / IES — liste fermée par le CDC, CPT-02, `research.md` D3) ; les deux valeurs du droit de
consultation (ses demandes uniquement / toutes celles de la société — ENT-03, liste fermée par le
CDC).

## 7. Historique utile (jamais supprimé)

- Journal d'audit (Feature 001, réutilisé) : chaque décision de validation/rejet de compte, chaque
  modification de type client (ancien/nouveau), chaque rattachement/désactivation/modification de
  droits/changement d'administrateur — auteur, horodatage, entité, avant/après (D8).
- **Aucun historique fonctionnel dédié à cette feature** : contrairement à `Ticket`
  (`EntreeHistorique`, visible client), les actions de gestion de compte n'ont pas d'exigence CDC de
  chronologie visible côté client au-delà de l'audit — non inventé ici.
