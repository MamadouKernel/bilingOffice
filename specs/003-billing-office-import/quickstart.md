# Quickstart — Billing Office IMPORT & Unicité BL

**Feature** : `003-billing-office-import` | **Date** : 2026-08-30 | **Statut** : Planifiée (`/speckit.plan`
exécuté) — pas encore implémentée. Ce document décrit comment mettre en route et valider Feature 003
**une fois implémentée**, en réutilisant l'installation existante de Feature 001/Feature 002.

> **Cadre** : aucune valeur métier n'est préconfigurée ici — les pièces exigées (PC-08), la limite de
> BL par soumission (PC-19), le rôle de validation du paiement (PC-03) et la liste des champs
> réservés IES (PC-05) restent des paramètres non fixés (Principes VI et XIII).

## 1. Prérequis

- Les prérequis de Feature 001/002 (SDK .NET 10, SQL Server 2019+/LocalDB, `dotnet-ef`) — aucun
  prérequis supplémentaire, aucune technologie ajoutée (`research.md`).
- Feature 001 et Feature 002 déjà installées et migrées (**CLOSED**, commits `a9657a4` et `46b2605`)
  — Feature 003 étend la même base, ne crée pas de nouvelle solution.

## 2. Installation / migration

```bash
dotnet ef database update --project src/CIT.Plateforme.Infrastructure --startup-project src/CIT.Plateforme.Web
dotnet run --project src/CIT.Plateforme.Web
```

Les migrations de Feature 003 seront additives uniquement (`/speckit.tasks`/`/speckit.implement`) :
- Extension de `Ticket` : `NumeroBl` (nullable, avec collation explicite fixe — insensible à la
  casse, jamais héritée du défaut de l'instance), `DeclarationPresentielle` (nullable, défaut non
  déclarée), `TypeAnnulationImport` (nullable, valeur unique `RejetManifeste`) — `research.md` D2,
  D12, D13bis.
- Index unique **filtré** SQL Server sur `NumeroBl`, restreint par **inclusion explicite** des
  statuts bloquants (`EnAttenteDePriseEnCharge`, `EnCours`, `EnAttenteClient` — jamais une exclusion,
  `research.md` D3) — mécanisme critique de l'unicité globale.
- Normalisation du numéro de BL à la soumission : trim des espaces externes uniquement (casse, tirets
  et espaces internes conservés tels que saisis — CLARIFICATION PROJET, `research.md` D2).
- Nouvelle table de suivi des branches IES (`research.md` D8) — deux lignes par ticket IES.
- **Aucune modification** de `StatutTicket`, `Dossier`, `CloisonnementService`,
  `IAutorisationService`, du moteur de files/transitions, ni du journal d'audit.

**Aucune migration destructive** — aucune table ni colonne de Feature 001/002 supprimée ou renommée.

## 3. Paramétrage initial (avant tout usage métier)

| # | Paramétrage | Contrat | Point ouvert |
|---|---|---|---|
| 1 | Créer les 6 files : Pilote Manifeste, Facturation, Validation Paiement, Comptabilité Client, Contrôle Documentaire, File IES (`FileDeTraitement`, module Billing IMPORT) | C3-01 à C3-19 | — |
| 2 | Définir les transitions autorisées entre ces files (`TransitionAutorisee`) selon les 4 circuits de `data-model.md` §5 | Tous | PC-07 (contenu détaillé — atelier métier, hérité de Feature 001) |
| 3 | Rattacher les rôles Pilote Manifeste, Pilote Facturation, Pilote Contrôle Documentaire, Comptable Client à leurs files respectives, avec les permissions `billing_import.*` (`research.md` D15) | Tous | — |
| 4 | Configurer les types de pièces exigées par étape/profil (formulaire de soumission, preuve de paiement, BS/BADT) | C3-01, C3-09, C3-10, C3-12 | **PC-08 — non fixé** |
| 5 | Confirmer si une limite de BL par soumission doit être appliquée et, le cas échéant, sa valeur (5 n'est qu'une valeur indicative de l'UI de démonstration — **non appliquée par le code**, voir post-implementation-audit.md §18/§20) | C3-01 | **PC-19 — non fixé, aucune contrainte serveur en l'état** |
| 6 | Attribuer la permission `billing_import.paiement.valider` au(x) utilisateur(s) habilité(s) à valider les paiements | C3-11 | **PC-03 — rôle officiel non fixé** (habilitation générique déjà opérationnelle) |
| 7 | Confirmer la liste exacte des champs/actions réservés à chaque branche IES | C3-16, C3-17 | **PC-05 — non fixé** (mécanisme de séparation déjà opérationnel par permission) |

> Sans ce paramétrage, la soumission IMPORT reste possible mais le formulaire n'affichera aucune
> pièce obligatoire au-delà du strict minimum, et aucun agent ne pourra instruire les tickets tant que
> les rattachements de l'item 3 ne sont pas réalisés — par conception, aucune valeur métier n'est
> inventée (`spec.md` § Assumptions).

## 4. Comptes de validation

Réutiliser les comptes de Feature 002 : créer, pour chacun des 4 profils (Normal, En Compte, Deposit,
IES), une société cliente active avec au moins un utilisateur disposant du droit de soumission.
Créer 4 comptes agents habilités : Pilote Manifeste, Pilote Facturation, Comptable Client, Pilote
Contrôle Documentaire (rattachés aux files de l'item 3 ci-dessus), plus un compte habilité à la
permission `billing_import.paiement.valider`.

## 5. Parcours de validation rapide

### 5.1 Scénarios CDC applicables (REC-SCE-01 à 04, portion Feature 003 uniquement)

> Conformément à `spec.md` § Testabilité et à la méthode déjà appliquée par Feature 001/002, seule la
> portion de chaque REC-SCE exerçant directement IMP/UNI est applicable à cette feature — **jamais**
> les portions relevant de SOC-*/COM-* (Feature 001/future Notifications).

| Scénario CDC | Portion applicable | Préconditions |
|---|---|---|
| REC-SCE-01 | Soumission 3 BL → 1 validé/1 rejeté/1 complément puis validation → facturation → paiement + preuve → validation paiement → BS+BADT → clôture | Compte client normal activé, pièces configurées, grille de rôles rattachée |
| REC-SCE-02 | Facture éditée → Contrôle Documentaire direct → documents sans étape paiement | Compte client en compte activé |
| REC-SCE-03 | Facture → lettrage Comptabilité → Contrôle Documentaire → clôture | Compte client deposit activé |
| REC-SCE-04 | Traitement parallèle Manifeste/CD, champs réservés, jointure, remise documents, clôture | Compte client IES activé, permissions de branche attribuées |

**Étapes REC-SCE-01 (portion Feature 003)** : soumettre 3 BL dans une même demande → le Pilote
Manifeste valide le premier, rejette le second (motif), demande un complément sur le troisième → le
client dépose le complément → nouvelle décision → suivre le circuit Normal jusqu'à clôture pour les
tickets validés → vérifier qu'aucune décision n'a affecté les deux autres tickets.

### 5.2 TEST-SPEC — Must et Should sans scénario CDC applicable

| Exigence | TEST-SPEC (dérivé de) |
|---|---|
| IMP-15 | US9 — déclarer une bascule présentielle après validation Manifeste, vérifier la trace sur le ticket, sans mécanisme de RDV |

### 5.3 Scénarios de concurrence et de normalisation (unicité BL — CLAR-BL-01)

Rejouer l'intégralité de la table CAS1→CAS12 de `research.md` D3 sur une base SQL Server réelle
(jamais EF InMemory), notamment :
- Deux tentatives de création pour le **même** BL (deux sociétés, puis même société/deux
  utilisateurs), déclenchées simultanément depuis deux `DbContext`/transactions distincts → une seule
  réussit.
- `"ABC123"` puis `"abc123"` → le second refusé (collation explicite insensible à la casse).
- `" ABC123 "` puis `"ABC123"` → le second refusé après trim des espaces externes.
- `"ABC-123"` puis `"ABC123"` → les deux autorisés (tirets et espaces internes jamais supprimés).
- Rejouer le cas de casse sur une base de test dont la collation **par défaut de l'instance** diffère
  volontairement → même résultat, indépendant de cette configuration (`research.md` D3, CAS12).

### 5.4 Scénarios IES (jointure indépendante de l'ordre)

Deux scénarios distincts à rejouer : (A) achever la branche Manifeste puis la branche Contrôle
Documentaire ; (B) l'ordre inverse — dans les deux cas, vérifier que le ticket n'est restitué au
client qu'après l'achèvement des deux branches, jamais avant (`research.md` D9).

### 5.5 Scénario multi-BL (indépendance)

Soumettre 5 BL dans une même demande, dont un déjà actif ailleurs (doublon) → vérifier que les 4 BL
valides génèrent chacun un ticket créé normalement, et que seul le BL en doublon est refusé, sans
effet sur les 4 autres (`research.md` D4).

## 6. Ne jamais inventer

- Aucun REC-SCE au-delà des portions IMP/UNI de REC-SCE-01 à 04 — les portions SOC-*/COM-* restent
  hors périmètre (`spec.md` § Testabilité et Contrôle de couverture).
- Aucune valeur de PC-03 (rôle officiel), PC-05 (champs IES exacts), PC-08 (pièces exactes) ou PC-19
  (limite de BL définitive) n'est préconfigurée par ce guide — paramètres à renseigner par le métier
  avant recette/production.
- Aucune intégration IRIS ou plateforme IES automatique — ces systèmes restent strictement hors
  portail (Principe XI, `research.md` D7).
