# Implementation Plan: Billing Office IMPORT & Unicité BL

**Branch**: `003-billing-office-import` | **Date**: 2026-08-30 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/003-billing-office-import/spec.md` (24 exigences
FR-001→FR-027, 9 User Stories P1→P4, clarifications CLAR-BL-01/02, CLAR-PAIEMENT-01,
CLAR-DEPOSIT-01 validées le 30/08/2026)

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Fournir le parcours **Billing Office IMPORT** : soumission multi-BL (jusqu'à 5 BL par demande,
IMP-01/02/03), contrôle d'unicité globale du numéro de BL (UNI-01/02/03), instruction au Pilote
Manifeste (validation/rejet/complément, IMP-04/05), aiguillage automatique selon le type de client
figé au dossier (IMP-06), et les 4 circuits de traitement — Normal (facturation, paiement, Contrôle
Documentaire, IMP-10→14), En Compte (sans étape de paiement, IMP-20→22), Deposit (lettrage
Comptabilité Client, IMP-30→32), IES (traitement parallèle Manifeste/Contrôle Documentaire avec
cloisonnement de champs, IMP-40→42) — ainsi que la déclaration d'une bascule présentielle après
contrôle BL (IMP-15, Should, point d'extension pour la future feature Continuité).

Approche technique (`research.md`) : **extension ciblée de la solution .NET existante** (aucune
nouvelle solution, aucun microservice, aucune technologie ajoutée) — **aucune nouvelle entité
métier lourde** : Feature 003 ajoute un domaine de **paramétrage** (6 nouvelles instances de
`FileDeTraitement`, des lignes de `TransitionAutorisee`, de nouveaux codes `Permission`) au moteur
déjà livré par Feature 001, complété de **trois extensions additives ciblées** de `Ticket`
(`NumeroBl`, `DeclarationPresentielle`, `TypeAnnulationImport`) et d'**une seule** entité satellite
nouvelle (suivi des branches IES, D8). L'aiguillage (IMP-06) réutilise sans modification l'instantané
`Dossier.TypeClient` déjà posé par Feature 002 (D5). L'unicité globale du BL sous concurrence réelle
est garantie par un **index unique filtré SQL Server** dont le prédicat **inclut explicitement** les
statuts bloquants (jamais une exclusion des statuts non bloquants, correction du 30/08/2026 — voir
« risque d'évolution » en D3) et porte une **collation explicitement fixe**, jamais héritée du défaut
de l'instance (D3, faisabilité vérifiée explicitement — SQL Server et EF Core la supportent
nativement) — jamais par une vérification applicative seule, ni par une colonne dénormalisée (risque
de dérive explicitement écarté par analogie avec la remédiation ENT-02 de Feature 002). La
normalisation du numéro de BL (casse insensible, trim externe, tirets/espaces internes préservés) est
une **CLARIFICATION PROJET** validée le 30/08/2026, jamais présentée comme une exigence CDC (D2).
Rejeté (IMP-04) réutilise le statut
générique `Annule` de `Ticket` pour le **workflow** (jamais rouvert, ne bloque jamais l'unicité BL,
comme un Annulé ordinaire) mais s'en distingue au niveau **métier** via le nouveau champ
`TypeAnnulationImport` (D13bis, revue du 30/08/2026 — préserve l'information de reporting/audit sans
modifier `StatutTicket` ni dupliquer l'état du workflow). Les retours client (complément, preuve de
paiement invalide) réutilisent `MettreEnAttenteClient`/`RependreApresReponseClient` — **aucun nouveau
statut** n'est ajouté à `StatutTicket` (D13). UNI-04 (booking EXPORT) n'est **jamais** réintroduite ;
IMP-15 reste strictement bornée à l'enregistrement d'une déclaration, sans mécanisme de rendez-vous
(D12).

## Technical Context

**Language/Version**: C# / .NET 10 (LTS) — ASP.NET Core 10, EF Core 10 (identique à Feature 001/002,
aucune raison de diverger)

**Primary Dependencies**: ASP.NET Core Identity (authentification, réutilisée telle quelle),
SignalR/Blazor Server (UI — réutilisés ; aucun nouvel usage temps réel spécifique requis, la
supervision existante de Feature 001 couvre déjà les nouvelles files), EF Core 10 (persistance) —
**aucune dépendance ajoutée**

**Storage**: SQL Server 2019+ via EF Core 10, migrations additives uniquement (3 colonnes nullables
sur `Tickets` — `NumeroBl`, `DeclarationPresentielle`, `TypeAnnulationImport`, 1 nouvelle table de
suivi des branches IES, 1 index unique **filtré** sur `NumeroBl` restreint par **inclusion explicite**
des statuts bloquants — faisabilité SQL Server/EF Core vérifiée explicitement, D3 —, une **collation
explicite fixe** sur `NumeroBl` (`PropertyBuilder.UseCollation`, jamais héritée du défaut de
l'instance, D3), nouvelles lignes de données `FileDeTraitement`/`TransitionAutorisee`/`Permission`) —
aucune migration destructive (`research.md` D2, D3, D8, D13bis) ; normalisation du BL (casse
insensible, trim externe, tirets/espaces internes préservés) validée comme CLARIFICATION PROJET, pas
une exigence CDC (D2)

**Testing**: xUnit (unitaire + intégration), WebApplicationFactory (bout-en-bout, portions IMP/UNI de
REC-SCE-01→04 — seuls scénarios CDC §22 applicables, voir `spec.md` § Testabilité), bUnit (composants
Blazor), tests de concurrence réels contre LocalDB/SQL Server pour l'unicité globale du BL (D3) et les
deux ordres d'achèvement des branches IES (D9) — même structure à 4 projets que Feature 001/002,
aucun nouveau projet de test

**Target Platform**: Application Web serveur (identique à Feature 001/002, même processus, aucun
déploiement séparé)

**Project Type**: Extension de la solution .NET en couches existante (Domain / Application /
Infrastructure / Web + 4 projets de tests) — pas de nouveau projet, pas de nouvelle solution

**Performance Goals**: NEEDS CLARIFICATION — volumétrie (nombre de soumissions IMPORT/jour, nombre de
BL simultanés) non définie par le CDC (PC-01, même point ouvert que Feature 001/002) ; la conception
n'est pas bloquée : l'index unique filtré (D3) et la pagination systématique des files/listes déjà
en place (Feature 001) suffisent sans hypothèse de volumétrie

**Constraints**: 100 % stack .NET/SQL Server (aucune technologie ajoutée, D1→D16) ; aucune extension
de Feature 001/002 au-delà de celle strictement documentée (D2, D8, D12) ; aucune valeur métier figée
dans le code (PC-03, PC-05, PC-08, PC-19 — Principe VI) ; autorisation systématiquement vérifiée côté
serveur (Principe VII) ; aucune intégration externe (IRIS, plateforme IES — Principe XI) ; historique
jamais supprimé (Principe VIII) ; audit réutilisé, jamais dupliqué (Principe IX)

**Scale/Scope**: 24 exigences retenues (23 Must, 1 Should — UNI-04 explicitement exclue), 9 User
Stories, 16 décisions de recherche D1→D16, 19 contrats C3-01→C3-19, 1 entité satellite nouvelle +
3 extensions additives de `Ticket`

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Vérification effectuée contre les 17 principes de la constitution (v1.0.0) :

| # | Principe | Statut | Justification (référence research.md) |
|---|---|---|---|
| I | Stack Technique Imposée | ✅ PASS | C#/.NET 10, ASP.NET Core, Blazor Server, EF Core, SQL Server, xUnit/bUnit — 100 % stack .NET déjà en place, aucune technologie ajoutée (D1→D16) ; index unique filtré = fonctionnalité SQL Server native, pas une dépendance tierce |
| II | Source de Vérité et Traçabilité (NON-NÉGOCIABLE) | ✅ PASS | Le plan ne crée aucune règle métier ; les 24 exigences et les 4 clarifications validées restent la référence ; chaque décision D1→D16 est ancrée sur une exigence CDC, une clarification validée, ou du code réel vérifié |
| III | Priorisation Must / Should / Could | ✅ PASS | Aucune priorité modifiée ; IMP-15 (Should) conçue comme un point d'extension minimal (D12), jamais traitée comme un Must déguisé ; UNI-04 jamais réintroduite |
| IV | Principes d'Architecture | ✅ PASS | Aucune dépendance tierce ajoutée ; aucune abstraction sans besoin identifié — chaque nouvelle entité (suivi de branche IES) et chaque extension (`NumeroBl`, `DeclarationPresentielle`, `TypeAnnulationImport`) est justifiée individuellement, alternatives rejetées documentées (D2, D8, D13bis) ; la revue technique du 30/08/2026 a explicitement écarté une colonne dénormalisée pour l'unicité BL par analogie avec la dérive ENT-02 déjà rencontrée en Feature 002 |
| V | Domaine Métier : Ticket et Capacités Transverses | ✅ PASS | Aucune règle de ticket/dossier/workflow redéfinie ; `Ticket`/`Dossier`/`FileDeTraitement`/`TransitionAutorisee` réutilisés sans modification de comportement (D1, D6) ; les 4 circuits sont des données paramétrées, pas 4 moteurs |
| VI | Workflows Paramétrables par Conception | ✅ PASS | Files, transitions, permissions = données administrables (D6, D15) ; PC-03/05/08/19 explicitement non fixés, laissés paramètres |
| VII | Sécurité (NON-NÉGOCIABLE) | ✅ PASS | Cloisonnement société réutilisé tel quel (`CloisonnementService`, D1) ; nouvelles permissions vérifiées côté serveur (D15) ; confidentialité IES par permission de branche, jamais UI seule (D10) |
| VIII | Données et Persistance | ✅ PASS | SQL Server + EF Core, migrations additives uniquement ; aucune suppression physique ; index unique filtré = mécanisme SGBD standard, versionné comme toute migration (D3) |
| IX | Audit et Traçabilité Opérationnelle | ✅ PASS | `EntreeJournalAudit`/`EntreeHistorique` de Feature 001 réutilisés à l'identique pour toute décision (Manifeste, paiement, lettrage, IES, présentiel) — aucun second journal (D1, D13, D14) |
| X | Gestion des Documents | ✅ PASS | `Document`/`IDocumentRepository` de Feature 001 réutilisés sans extension pour toutes les pièces du module (D11) |
| XI | Intégrations et Périmètre Autonome | ✅ PASS | Aucune intégration IRIS ou plateforme IES automatique ; toute interaction externe reste déclarative/hors portail (D7, D12) |
| XII | Tests et Stratégie de Validation | ✅ PASS | Chaque Must dispose d'une stratégie de test (TEST-CDC pour les portions IMP/UNI de REC-SCE-01→04, TEST-SPEC pour IMP-15) ; tests de concurrence réels pour l'unicité BL (D3) et les deux ordres IES (D9) — jamais EF InMemory pour ces invariants (D16) |
| XIII | Performance et Exploitation | ✅ PASS | Aucune valeur métier non définie inventée (PC-01, PC-19) ; l'index unique filtré et la pagination existante suffisent sans hypothèse de volumétrie |
| XIV | Observabilité | ✅ PASS | Aucun nouveau traitement de fond introduit (D1 — les `BackgroundService` existants de Feature 001 s'appliquent automatiquement aux nouvelles files) ; l'audit existant suffit à l'observabilité fonctionnelle requise |
| XV | Gestion des Points Non Résolus (NON-NÉGOCIABLE) | ✅ PASS | PC-03, PC-05, PC-08, PC-19 explicitement non tranchés (paramètres/mécanismes génériques) ; la normalisation du numéro de BL (D2) a été explicitement validée comme **CLARIFICATION PROJET** (portée par une décision humaine du 30/08/2026), jamais présentée comme une exigence CDC ; rien n'est présenté comme décidé à tort |
| XVI | Definition of Done | ✅ PASS | Pris en compte par la stratégie de tests (D16) et les tasks à venir |
| XVII | Gouvernance des Écarts d'Implémentation | ✅ PASS | Les trois extensions de `Ticket` (D2, D12, D13bis) et l'unique nouvelle entité (D8) sont explicitement identifiées, justifiées et strictement additives ; aucun écart silencieux ; UNI-04 explicitement exclue avec justification tracée (`spec.md` §0.2) |

**Re-check après Phase 1 (design)** : effectué le 30/08/2026 — `data-model.md`,
`contracts/use-cases.md` et `quickstart.md` ne dégradent aucun PASS ; aucun nouveau risque de
conformité introduit ; la Phase 1 confirme qu'aucune extension supplémentaire de `Ticket`/`Dossier`
au-delà de D2/D12 n'est nécessaire, et que l'entité satellite IES (D8) reste la seule addition
structurelle.

## Project Structure

### Documentation (this feature)

```text
specs/003-billing-office-import/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output — 16 décisions D1→D16 (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output — C3-01→C3-19 (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

**Extension de la solution .NET existante de Feature 001/Feature 002 — aucun nouveau projet, aucune
nouvelle solution.**

```text
cit-digital-platform/
├── src/
│   ├── CIT.Plateforme.Domain/
│   │   ├── Tickets/Ticket.cs              # ÉTENDU (additif) — NumeroBl, DeclarationPresentielle,
│   │   │                                   # TypeAnnulationImport (D2, D12, D13bis)
│   │   ├── Tickets/                       # NOUVEAU — entité satellite de suivi des branches IES (D8)
│   │   └── Securite/                      # ÉTENDU (additif) — nouveaux codes Permission (D15) : billing_import.*
│   ├── CIT.Plateforme.Application/
│   │   └── BillingImport/                 # NOUVEAU — cas d'usage C3-01→C3-19 (commandes/requêtes)
│   ├── CIT.Plateforme.Infrastructure/
│   │   └── Persistence/                   # ÉTENDU — mapping des extensions Ticket, entité satellite IES,
│   │                                       # index unique filtré sur NumeroBl (D3), migration additive
│   └── CIT.Plateforme.Web/
│       └── Components/Pages/BillingImport/ # NOUVEAU — pages Blazor Server (soumission multi-BL,
│                                            # instruction Manifeste, facturation, validation paiement,
│                                            # comptabilité, contrôle documentaire, IES)
└── tests/
    ├── CIT.Plateforme.Domain.Tests/Tickets/           # ÉTENDU — tests des méthodes existantes avec NumeroBl,
    │                                                    # suivi de branche IES
    ├── CIT.Plateforme.Application.Tests/BillingImport/ # NOUVEAU — cas d'usage, autorisation, indépendance multi-BL,
    │                                                    # snapshot TypeClient, jointure IES (ordres A→B/B→A)
    ├── CIT.Plateforme.Infrastructure.Tests/BillingImport/ # NOUVEAU — concurrence BL réelle (D3), EF Core réel
    └── CIT.Plateforme.Web.Tests/BillingImport/         # NOUVEAU — portions IMP/UNI de REC-SCE-01→04
```

**Structure Decision**: aucune nouvelle couche, aucun nouveau projet — Feature 003 ajoute un module
`BillingImport` dans les couches Application/Web (par analogie avec `Comptes` en Feature 002), tandis
que le Domain reste concentré dans le module `Tickets` déjà existant (trois extensions additives +
une entité satellite, pas de nouveau module Domain dédié — le domaine Billing IMPORT n'introduit aucun
concept métier suffisamment autonome pour justifier un module séparé, contrairement à `Comptes` en
Feature 002 qui portait deux agrégats entièrement nouveaux). Confirme la promesse de `plan.md` de
Feature 001 (« les futures features ajouteront des pages Blazor et des modules Domain/Application sans
refonte du socle »).

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

**Aucune violation constatée** — table volontairement vide.

Le plan privilégie systématiquement l'option la plus simple conforme : aucune dépendance tierce
ajoutée, aucun nouveau projet/solution, une seule nouvelle entité (satellite IES, D8) strictement
justifiée par l'impossibilité structurelle de représenter deux progressions parallèles sur un ticket
qui n'appartient qu'à une seule file à la fois, et trois extensions additives ciblées de `Ticket`
(D2, D12, D13bis) — chacune documentée avec ses alternatives rejetées. La revue technique du
30/08/2026 a en particulier écarté une colonne dénormalisée pour l'unicité BL (risque de dérive,
Principe IV) au profit d'un filtre direct sur `Ticket.Statut`, seule source de vérité.
