# Tasks: Socle transverse de gestion des tickets et workflows

**Input**: Design documents from `specs/001-socle-tickets-workflows/` (spec.md, plan.md, research.md,
data-model.md, contracts/use-cases.md, quickstart.md, recipe-scope-audit.md) et `docs/04-matrice-recette.md`.

**Prerequisites**: plan.md ✅ · spec.md ✅ (74 exigences, 11 User Stories, Clarifications CLAR-01→05) ·
research.md ✅ (D1→D12) · data-model.md ✅ · contracts/use-cases.md ✅ (C-01→C-25) · quickstart.md ✅ ·
recipe-scope-audit.md ✅ (périmètre recette corrigé) · constitution.md v1.0.0 ✅

**Tests**: Explicitement demandés par l'utilisateur. Chaque règle métier critique (prise en charge
concurrente, transitions, transfert, réaffectation, cycle de vie, autorisations, attente client,
SLA, audit) a son test écrit **avant** sa tâche d'implémentation. Traçabilité de chaque test :

- **TEST-CDC** : scénario provenant directement de REC-SCE-09→14 (seuls scénarios CDC §22
  applicables à ce socle — voir `recipe-scope-audit.md`). **REC-SCE-01→08 n'apparaissent dans
  aucune tâche ni aucun test de cette feature**, sauf mention explicite HORS PÉRIMÈTRE.
- **TEST-SPEC** : test complémentaire dérivé d'un critère d'acceptation de `spec.md`, pour une
  exigence Must sans scénario CDC applicable dans ce périmètre (liste des 24 dans
  `recipe-scope-audit.md`). Indique toujours : Exigence CDC → User Story → Critère d'acceptation → Test.
  **N'est jamais présenté comme un scénario officiel du CDC.**
- **TEST-TECH** : test d'une propriété technique de `plan.md`/`research.md` (concurrence,
  persistance, sécurité, append-only…), sans création de nouvelle exigence métier. Indique
  toujours la décision D-xx ou section du plan concernée.

**Organization**: Tâches groupées par User Story (ordre de `spec.md` : US1→US11, priorités
P1→P6), pour implémentation et test indépendants de chaque story.

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers différents, aucune dépendance sur une tâche non terminée)
- **[Story]** : User Story concernée (US1…US11) — absent en Setup/Fondations/Hardening
- Chaque tâche fonctionnelle porte, autant que possible, un bloc `CDC: / Contract: / Test:`
- **BLOCKED BY PC-xx** : uniquement lorsque la tâche ne peut réellement pas être réalisée de
  façon configurable (données métier réelles requises, ex. configuration de recette/production) —
  jamais pour bloquer la construction du mécanisme paramétrable lui-même.
- **CLARIFICATION REQUIRED** : point ouvert non rattaché à un PC-xx numéroté (outillage technique).

## Path Conventions (architecture .NET en couches — `plan.md` § Project Structure)

```text
src/CIT.Plateforme.Domain/{Tickets,Files,Affectation,SuiviTemps,Supervision,Documents,Securite}/
src/CIT.Plateforme.Application/{Tickets,Messagerie,Documents,Affectation,Supervision,Securite,Administration}/
src/CIT.Plateforme.Infrastructure/{Persistence,Identity,Documents,Evenements,TraitementsDeFond}/
src/CIT.Plateforme.Web/{Pages,Hubs}/
tests/CIT.Plateforme.Domain.Tests/
tests/CIT.Plateforme.Application.Tests/
tests/CIT.Plateforme.Infrastructure.Tests/
tests/CIT.Plateforme.Web.Tests/RecetteCdc/   ← tests TEST-CDC (REC-SCE-09→14 uniquement)
```

---

## Phase 1: Setup

**Purpose** : initialisation de la solution .NET (D1–D3, D12), sans aucune règle métier.

- [X] T001 Créer la solution `CIT.Plateforme.sln` et les 4 projets `src/` (Domain, Application,
  Infrastructure, Web) + 4 projets `tests/` selon l'arborescence de `plan.md` § Project Structure
- [X] T002 [P] Cibler .NET 10 (LTS) sur tous les projets et ajouter les dépendances Web (ASP.NET
  Core Identity, Blazor Server, SignalR, Bootstrap) dans `src/CIT.Plateforme.Web/CIT.Plateforme.Web.csproj` [D1, D2, D4]
- [X] T003 [P] Ajouter les dépendances Infrastructure (EF Core 10, provider SQL Server, outil
  `dotnet-ef`) dans `src/CIT.Plateforme.Infrastructure/CIT.Plateforme.Infrastructure.csproj` [D3]
- [X] T004 [P] Ajouter les dépendances de test (xUnit, bUnit, `Microsoft.AspNetCore.Mvc.Testing`
  pour WebApplicationFactory) aux 4 projets `tests/` [D12]
- [X] T005 [P] Configurer `.editorconfig`, analyseurs Roslyn et nullable reference types activés
  sur toute la solution (Principe IV — simplicité, qualité de base)
- [X] T006 Configurer la gestion des secrets (chaîne de connexion SQL Server hors code source,
  `dotnet user-secrets` en local) dans `src/CIT.Plateforme.Web/appsettings.json` (squelette, aucune
  valeur secrète committée) [D3]

---

## Phase 2: Fondations transverses (bloquant)

**Purpose** : infrastructure commune requise par les 11 User Stories. **Aucune User Story ne
démarre avant la fin de cette phase.**

- [X] T007 Créer le `PlateformeDbContext` (squelette, sans mapping détaillé) dans
  `src/CIT.Plateforme.Infrastructure/Persistence/PlateformeDbContext.cs` [D3]
- [X] T008 [P] Implémenter le service de numérotation unique (séquences SQL Server par
  module/type, attribution transactionnelle, jamais réutilisé) dans
  `src/CIT.Plateforme.Domain/Tickets/ServiceNumerotation.cs`
  — CDC: SOC-01, SOC-09 Contract: — Test: voir T020 [D6]
- [X] T009 [P] Définir les entités de domaine Ticket et Dossier (statut, module, type, dossier de
  rattachement, aucun cycle de vie propre du dossier — CLAR-01) dans
  `src/CIT.Plateforme.Domain/Tickets/{Ticket.cs,Dossier.cs}`
  — CDC: SOC-01, SOC-02, SOC-03, SOC-08, SOC-09, SOC-11 Contract: — Test: voir T021
- [X] T010 [P] Définir l'entité File de traitement + rattachement habilitation paramétrable dans
  `src/CIT.Plateforme.Domain/Files/FileDeTraitement.cs`
  — CDC: SOC-04 Contract: — Test: voir T021
- [X] T011 Implémenter le double journal — historique fonctionnel (append) et journal d'audit
  (append-only, store distinct, D8) — dans
  `src/CIT.Plateforme.Infrastructure/Persistence/{HistoriqueTicket.cs,JournalAudit.cs}`
  — CDC: SOC-13, SEC-05 (cité) Contract: C-25 Test: voir T093, T094 [D8] (depends on T007)
- [X] T012 [P] Implémenter le journal d'événements du socle (événements de domaine persistés,
  dispatch in-process, aucun canal implémenté) dans
  `src/CIT.Plateforme.Infrastructure/Evenements/JournalEvenements.cs`
  — CDC: MSG-04, ALE-01 (cités) Contract: — Test: — [D10] (depends on T007)
- [X] T013 Implémenter ASP.NET Core Identity + modèle d'autorisation par policies/permissions
  (rôles/habilitations paramétrables, jamais codés en dur) dans
  `src/CIT.Plateforme.Infrastructure/Identity/`
  — CDC: SEC-02, SEC-11 (cités) Contract: C-03 Test: T014 [D4]
- [X] T014 [P] [TEST-TECH] Test d'autorisation serveur : une permission masquée uniquement côté
  UI ne doit jamais suffire — toute action sensible est refusée si la policy serveur échoue, même
  si l'appel contourne l'interface — dans
  `tests/CIT.Plateforme.Application.Tests/Securite/AutorisationServeurTests.cs`
  — Decision: D4, Principe VII de la constitution *(règle métier critique : autorisations)*
- [X] T015 Implémenter le cloisonnement client/agent (filtres de requête par propriétaire /
  habilitation de file) dans `src/CIT.Plateforme.Application/Securite/CloisonnementService.cs`
  — CDC: SEC-01, SEC-02, SEC-03, CPT-04 (cité) Contract: C-08, C-09, C-14 Test: T014
  (depends on T013)
- [X] T016 Implémenter le cas d'usage « Gérer les files » (créer/modifier une file, types de
  tickets admis, stratégie d'affectation active) dans
  `src/CIT.Plateforme.Application/Administration/GererFilesCommand.cs`
  — CDC: SOC-04, WKF-01, AFF-07 Contract: **C-01** Test: voir T021 (depends on T010)
- [X] T017 Générer et appliquer la migration EF Core initiale (schéma squelette : tickets,
  dossiers, files, deux journaux) dans `src/CIT.Plateforme.Infrastructure/Persistence/Migrations/` [D3]
  (depends on T007, T009, T010, T011)

**Checkpoint** : fondations prêtes — l'implémentation des User Stories peut commencer.

---

## Phase 3: User Story 1 — Création, mise en file et prise en charge d'un ticket (Priority: P1) 🎯 MVP

**Goal** : un ticket créé obtient un numéro unique, rejoint la bonne file, et peut être pris en
charge par un opérateur habilité — le socle minimal sans lequel aucun module métier ne fonctionne.

**Independent Test** : créer un ticket de test, vérifier son numéro, son arrivée en file à l'état
« en attente de prise en charge », puis son acceptation par un opérateur — sans dépendre d'aucune
autre User Story ni d'aucun module métier futur.

### Tests for User Story 1 ⚠️ (écrits avant l'implémentation)

- [X] T018 [P] [US1] [TEST-TECH] Test de concurrence : deux opérateurs tentent d'accepter le même
  ticket simultanément — un seul réussit, l'autre est informé que le ticket n'est plus disponible
  — dans `tests/CIT.Plateforme.Domain.Tests/Tickets/PriseEnChargeConcurrenceTests.cs`
  — Decision: D6 (numérotation/transition transactionnelle), Edge Case « concurrence sur la prise
  en charge » (spec.md) *(règle métier critique : prise en charge concurrente)*
- [X] T019 [P] [US1] [TEST-CDC] REC-SCE-09 de bout en bout : ticket en file « attente de prise en
  charge », un opérateur l'accepte, horodatages enregistrés et exploitables dans les indicateurs —
  dans `tests/CIT.Plateforme.Web.Tests/RecetteCdc/RecSce09PriseEnChargeTests.cs`
  — CDC couverts : SOC-12, PEC-01, PEC-02, PEC-03, PEC-04, TPS-01
- [X] T020 [P] [US1] [TEST-SPEC] Numérotation unique jamais réutilisée, y compris après
  annulation/archivage, dans `tests/CIT.Plateforme.Domain.Tests/Tickets/NumerotationTests.cs`
  — Exigence CDC: SOC-01, SOC-09 → US1 → critère d'acceptation 1 de US1 (FR-001) → Test
- [X] T021 [US1] [TEST-SPEC] Centralisation ticket (documents/statut/historique/échanges),
  workflow auto-déterminé par module+type, actions standard (valider/rejeter+motif/complément),
  statut visible client en temps réel — dans
  `tests/CIT.Plateforme.Application.Tests/Tickets/TicketSocleTests.cs`
  — Exigence CDC: SOC-02, SOC-03, SOC-06, SOC-08, WKF-01 → US1 → critères d'acceptation 3, 5, 6 de
  US1 (FR-002, FR-003, FR-006, FR-008, FR-027) → Test *(aucun scénario CDC direct dans ce
  périmètre — voir `recipe-scope-audit.md`)*

### Implementation for User Story 1

- [X] T022 [US1] Implémenter le cas d'usage « Créer un ticket » (rattachement dossier, orientation
  automatique en file) dans `src/CIT.Plateforme.Application/Tickets/CreerTicketCommand.cs`
  — CDC: SOC-01, SOC-02, SOC-03, SOC-11 Contract: **C-08** Test: T019, T020, T021
  (depends on T008, T009, T010, T016)
- [X] T023 [US1] Implémenter le cas d'usage « Prendre en charge » (acceptation sûre en
  concurrence) dans `src/CIT.Plateforme.Application/Tickets/PrendreEnChargeCommand.cs`
  — CDC: PEC-01, PEC-02, PEC-03, PEC-04, SOC-12 Contract: **C-14** Test: T018, T019
  (depends on T022)
- [X] T024 [US1] Implémenter les actions de workflow standard (valider / rejeter avec motif
  obligatoire / demander un complément) dans
  `src/CIT.Plateforme.Application/Tickets/ActionsWorkflowStandardCommand.cs`
  — CDC: SOC-06 Contract: — Test: T021 (depends on T022)
- [X] T025 [US1] Implémenter les requêtes « Consulter statut » (client, temps réel) et
  « Consulter ma file » (agent, tri/filtres) dans
  `src/CIT.Plateforme.Application/Tickets/{ConsulterStatutQuery.cs,ConsulterFileQuery.cs}`
  — CDC: SOC-04, SOC-08 Contract: **C-09**, **C-14** Test: T021 (depends on T022)
- [X] T026 [US1] Implémenter la prise libre (AFF-01) dans
  `src/CIT.Plateforme.Application/Affectation/PriseLibreCommand.cs`
  — CDC: AFF-01 Contract: C-14 Test: T019 (depends on T023)
- [X] T027 [P] [US1] Page Blazor : formulaire générique de soumission + file de l'agent
  (squelette d'écran, sans règle métier de module) dans
  `src/CIT.Plateforme.Web/Pages/Tickets/{Soumettre.razor,MaFile.razor}`
  — CDC: SOC-01, SOC-04 Contract: C-08, C-14 (depends on T022, T025)
- [X] T028 [US1] Raccorder historique + audit aux événements de création et de prise en charge
  dans `src/CIT.Plateforme.Infrastructure/Persistence/{HistoriqueTicket.cs,JournalAudit.cs}` (extension)
  — CDC: SOC-13 Contract: C-08, C-14 Test: T019 (depends on T011, T022, T023)

**Checkpoint** : US1 fonctionnelle et démontrable seule (MVP) — création → file → prise en charge.

---

## Phase 4: User Story 2 — Affectation, réaffectation et transfert d'un ticket (Priority: P2)

**Goal** : déplacer un ticket entre files ou opérateurs en respectant les circuits autorisés, sans
jamais perdre son historique.

**Independent Test** : sur des tickets créés/pris en charge (US1), affecter, réaffecter,
transférer, tenter une transition non autorisée, et vérifier la conservation de l'historique.

### Tests for User Story 2 ⚠️

- [X] T029 [P] [US2] [TEST-CDC] REC-SCE-10 de bout en bout : transfert avec motif obligatoire,
  conservation intégrale (numéro/dossier/documents/commentaires/historique), client notifié,
  transfert vers une destination non autorisée refusé — dans
  `tests/CIT.Plateforme.Web.Tests/RecetteCdc/RecSce10TransfertTests.cs`
  — CDC couverts : TRF-01, TRF-02, TRF-03, TRF-05, TRF-06, WKF-03 *(règle métier critique : transfert)*
- [X] T030 [P] [US2] [TEST-CDC] REC-SCE-11 de bout en bout : réaffectation A → B selon
  habilitations, tracée dans l'historique — dans
  `tests/CIT.Plateforme.Web.Tests/RecetteCdc/RecSce11ReaffectationTests.cs`
  — CDC couverts : TRF-04, WKF-03 *(règle métier critique : réaffectation)*
- [X] T031 [P] [US2] [TEST-TECH] Toute transition hors matrice des transitions (CLAR-04) est
  mécaniquement refusée, quelle que soit l'habilitation de l'initiateur — dans
  `tests/CIT.Plateforme.Domain.Tests/Files/MatriceTransitionsTests.cs`
  — Decision: CLAR-04 (spec.md § Clarifications), TRF-06 *(règle métier critique : transitions)*
- [X] T032 [US2] [TEST-SPEC] CLAR-03 : après un transfert, le ticket arrive non affecté « en
  attente de prise en charge » sauf destinataire explicitement désigné ; un nouveau temps
  d'attente démarre — dans
  `tests/CIT.Plateforme.Application.Tests/Tickets/TransfertSansAffectationTests.cs`
  — Exigence CDC: TRF-01, TRF-03, PEC-01 → US2 → scénario d'acceptation 7 de US2 (CLAR-03) → Test

### Implementation for User Story 2

- [X] T033 [US2] Implémenter la Matrice des transitions (destinations + rôles initiateurs +
  motifs autorisés par transition, paramétrable — CLAR-04) dans
  `src/CIT.Plateforme.Domain/Files/MatriceTransitions.cs`
  — CDC: TRF-06, WKF-01 Contract: **C-02** Test: T031 (depends on T010)
- [X] T034 [US2] Implémenter le cas d'usage « Transférer un ticket » (contrôle matrice, motif
  obligatoire, arrivée non affectée sauf destinataire désigné) dans
  `src/CIT.Plateforme.Application/Tickets/TransfererTicketCommand.cs`
  — CDC: TRF-01, TRF-02, TRF-03, TRF-05, TRF-06 Contract: **C-15** Test: T029, T031, T032
  (depends on T022, T033)
- [X] T035 [US2] Implémenter le cas d'usage « Réaffecter un ticket » (droit configurable, non
  réservé au superviseur) dans
  `src/CIT.Plateforme.Application/Tickets/ReaffecterTicketCommand.cs`
  — CDC: TRF-04 Contract: **C-16** Test: T030 (depends on T023)
- [X] T036 [US2] Implémenter l'affectation manuelle superviseur et l'affectation directe (AFF-02,
  PEC-05) dans `src/CIT.Plateforme.Application/Affectation/AffecterManuellementCommand.cs`
  — CDC: AFF-02, PEC-05 Contract: C-16 Test: T029 (depends on T023)
- [X] T037 [US2] Implémenter la dérogation au circuit nominal (WKF-02) — transfert/réaffectation
  hors circuit standard, tracée — dans
  `src/CIT.Plateforme.Application/Tickets/DerogationWorkflowService.cs`
  — CDC: WKF-02, WKF-03 Contract: C-15, C-16 Test: T029, T030 (depends on T034, T035)
- [X] T038 [P] [US2] Actions transfert/réaffectation sur l'écran détail ticket dans
  `src/CIT.Plateforme.Web/Pages/Tickets/DetailTicket.razor` (nouveau)
  — CDC: TRF-01, TRF-04 Contract: C-15, C-16 (depends on T034, T035)
- [X] T039 [US2] Raccorder historique + audit aux événements de transfert/réaffectation/dérogation
  — CDC: TRF-02, TRF-05, WKF-03 Contract: C-15, C-16 Test: T029, T030 (depends on T011, T034, T035, T037)

**Checkpoint** : US1 + US2 fonctionnelles indépendamment.

---

## Phase 5: User Story 3 — Échange avec le client : demande de complément et attente (Priority: P2)

**Goal** : mettre un ticket en attente d'une action client sans pénaliser la performance de
l'opérateur, puis reprendre le traitement après la réponse.

**Independent Test** : placer un ticket pris en charge (US1) en attente client, vérifier la
suspension du décompte interne, simuler la réponse et la reprise.

### Tests for User Story 3 ⚠️

- [X] T040 [P] [US3] [TEST-CDC] REC-SCE-12 (portion attente client) : mise en attente suspend le
  temps interne CIT, reprise après réponse relance le décompte — dans
  `tests/CIT.Plateforme.Web.Tests/RecetteCdc/RecSce12TempsSlaTests.cs` (fichier partagé avec US8,
  voir T067) — CDC couverts : TPS-02, TPS-04 *(règle métier critique : attente client)*
- [X] T041 [US3] [TEST-SPEC] Relances automatiques paramétrables (nombre/cadence), suggestion de
  clôture jamais automatique, jamais proposée après un état engageant — dans
  `tests/CIT.Plateforme.Domain.Tests/Tickets/RelancesAbsenceReponseTests.cs`
  — Exigence CDC: CYC-04, CYC-05 → US3 → scénarios d'acceptation 3, 4 de US3 → Test *(aucun
  scénario CDC direct dans ce périmètre)*

### Implementation for User Story 3

- [X] T042 [US3] Implémenter « Mettre en attente client » / « Reprendre après réponse » dans
  `src/CIT.Plateforme.Application/Tickets/AttenteClientCommand.cs`
  — CDC: TPS-02, TPS-04, WKF-01 Contract: **C-17** Test: T040 (depends on T022)
- [X] T043 [US3] Implémenter les relances automatiques + suggestion de clôture (jamais
  automatique, jamais après état engageant) en `BackgroundService` dans
  `src/CIT.Plateforme.Infrastructure/TraitementsDeFond/RelancesAbsenceReponseService.cs`
  — CDC: CYC-04, CYC-05 Contract: **C-22** Test: T041 [D5] (depends on T042)
- [X] T044 [P] [US3] Indicateur « en attente client » + action « demander un complément » sur
  `src/CIT.Plateforme.Web/Pages/Tickets/DetailTicket.razor` (extension)
  — CDC: TPS-02 Contract: C-17 (depends on T042)

**Checkpoint** : US1–US3 fonctionnelles indépendamment.

---

## Phase 6: User Story 4 — Cycle de vie : annulation, clôture, réouverture (Priority: P2)

**Goal** : couvrir la fin naturelle du cycle de vie d'un ticket (annulation, demande
d'annulation, clôture, réouverture), sans jamais perdre son historique.

**Independent Test** : sur un ticket pris en charge (US1), déclencher chaque issue et vérifier la
conservation du numéro, de l'historique et des documents.

### Tests for User Story 4 ⚠️

- [X] T045 [P] [US4] [TEST-SPEC] Annulation libre avant étape bloquante paramétrable, demande
  d'annulation transmise pour décision au-delà, motivation/traçabilité systématique — dans
  `tests/CIT.Plateforme.Domain.Tests/Tickets/AnnulationTests.cs`
  — Exigence CDC: CYC-01, CYC-02, CYC-03 → US4 → scénarios d'acceptation 1, 2 de US4 → Test
  *(aucun scénario CDC direct dans ce périmètre)* *(règle métier critique : cycle de vie)*
- [X] T046 [P] [US4] [TEST-SPEC] La clôture déclenche un archivage automatique ; le ticket reste
  consultable en permanence par le client et les agents habilités — dans
  `tests/CIT.Plateforme.Application.Tests/Tickets/ClotureArchivageTests.cs`
  — Exigence CDC: SOC-09 → US4 → scénario d'acceptation 3 de US4 → Test *(aucun scénario CDC
  direct dans ce périmètre)*
- [X] T047 [P] [US4] [TEST-SPEC] CLAR-01 : le dossier n'a pas de cycle de vie propre — il dérive
  de ses tickets, reste consultable, et se réactive automatiquement à la réouverture de l'un
  d'eux — dans `tests/CIT.Plateforme.Domain.Tests/Tickets/DossierDeriveTests.cs`
  — Exigence CDC: SOC-11 → US7 (rattaché ici car dépend de la clôture) → CLAR-01 → Test
- [X] T048 [US4] [TEST-SPEC] CLAR-02 : la réouverture ne s'applique qu'aux tickets clôturés,
  jamais à un ticket annulé — dans `tests/CIT.Plateforme.Application.Tests/Tickets/ReouvertureTests.cs`
  — Exigence CDC: CYC-06, CYC-07 → US4 → scénario d'acceptation 4 de US4 (CLAR-02) → Test

### Implementation for User Story 4

- [X] T049 [US4] Implémenter « Annuler » / « Demander l'annulation » (étapes bloquantes
  paramétrables) dans `src/CIT.Plateforme.Application/Tickets/AnnulerTicketCommand.cs`
  — CDC: CYC-01, CYC-02, CYC-03 Contract: **C-12**, **C-20** Test: T045 (depends on T022)
- [X] T050 [US4] Implémenter « Clôturer un ticket » (archivage automatique) dans
  `src/CIT.Plateforme.Application/Tickets/CloturerTicketCommand.cs`
  — CDC: SOC-09 Contract: **C-18** Test: T046 (depends on T022)
- [X] T051 [US4] Implémenter l'état dérivé du dossier (aucune action de clôture de dossier propre
  — CLAR-01) dans `src/CIT.Plateforme.Domain/Tickets/EtatDossierDerive.cs`
  — CDC: SOC-11 Contract: — Test: T047 (depends on T009, T050)
- [X] T052 [US4] Implémenter « Rouvrir » / « Demander une réouverture » (tickets clôturés
  uniquement — CLAR-02) dans `src/CIT.Plateforme.Application/Tickets/ReouvrirTicketCommand.cs`
  — CDC: CYC-06, CYC-07 Contract: **C-13** Test: T048 (depends on T050)
- [X] T053 [P] [US4] Actions annulation/clôture/réouverture sur
  `src/CIT.Plateforme.Web/Pages/Tickets/DetailTicket.razor` (extension)
  — CDC: CYC-01, SOC-09, CYC-06 Contract: C-12, C-13, C-18, C-20 (depends on T049, T050, T052)

**Checkpoint** : US1–US4 fonctionnelles indépendamment.

---

## Phase 7: User Story 5 — Communication : chat client/agent et notes internes (Priority: P3)

**Goal** : permettre l'échange horodaté client/agent, et la coordination interne CIT via des
notes strictement confidentielles.

**Independent Test** : publier un message client, un message agent et une note interne sur un
ticket existant, vérifier leur visibilité respective et leur distinction visuelle.

### Tests for User Story 5 ⚠️

- [X] T054 [P] [US5] [TEST-SPEC] Fil de commentaires horodaté, visible par le client et les
  agents habilités, conservé dans l'historique — dans
  `tests/CIT.Plateforme.Application.Tests/Messagerie/MessagesTests.cs`
  — Exigence CDC: SOC-05, MSG-01 → US5 → scénario d'acceptation 1 de US5 → Test *(aucun scénario
  CDC direct dans ce périmètre)*
- [X] T055 [P] [US5] [TEST-SPEC] Une note interne n'est **jamais** visible du client ; le
  caractère interne/externe est visuellement très distinct — dans
  `tests/CIT.Plateforme.Application.Tests/Messagerie/NotesInternesTests.cs`
  — Exigence CDC: MSG-02, MSG-03 → US5 → scénarios d'acceptation 2, 3 de US5 → Test
- [X] T056 [US5] [TEST-SPEC] Une mention @Nom notifie le collaborateur mais n'entraîne **jamais**
  de réaffectation du ticket — dans
  `tests/CIT.Plateforme.Application.Tests/Messagerie/MentionSansReaffectationTests.cs`
  — Exigence CDC: MSG-05 → US5 → scénario d'acceptation 4 de US5 → Test

### Implementation for User Story 5

- [X] T057 [US5] Implémenter l'entité Message et le cas d'usage « Publier un message » (client ou
  agent) dans `src/CIT.Plateforme.Domain/Messagerie/Message.cs`,
  `src/CIT.Plateforme.Application/Messagerie/PublierMessageCommand.cs`
  — CDC: SOC-05, MSG-01, MSG-02 Contract: **C-10** Test: T054, T055 (depends on T009)
- [X] T058 [US5] Implémenter la mention @Nom (notification, jamais de réaffectation) dans
  `src/CIT.Plateforme.Application/Messagerie/MentionnerCollaborateurCommand.cs`
  — CDC: MSG-04, MSG-05 Contract: C-10 Test: T056 (depends on T057, T012)
- [X] T059 [P] [US5] Composant Blazor : fil de messages avec distinction visuelle interne/externe
  dans `src/CIT.Plateforme.Web/Pages/Tickets/FilMessages.razor`
  — CDC: MSG-03 Contract: C-10 (depends on T057)

**Checkpoint** : US1–US5 fonctionnelles indépendamment.

---

## Phase 8: User Story 6 — Dépôt de documents et contrôle d'accès (Priority: P3)

**Goal** : permettre le dépôt de documents avec contrôles de conformité systématiques, et
garantir qu'aucun utilisateur n'accède au-delà de son périmètre.

**Independent Test** : déposer un document en tant que client puis agent, tenter un dépôt non
conforme, vérifier le refus d'accès hors périmètre.

### Tests for User Story 6 ⚠️

- [X] T060 [P] [US6] [TEST-SPEC] Contrôle à l'upload (type, taille) avec message d'erreur
  explicite en cas de non-conformité — dans
  `tests/CIT.Plateforme.Application.Tests/Documents/ControleDepotTests.cs`
  — Exigence CDC: SOC-07 → US6 → scénario d'acceptation 2 de US6 → Test *(aucun scénario CDC
  direct dans ce périmètre)*
- [X] T061 [P] [US6] [TEST-TECH] Accès à un document refusé hors périmètre habilité, contrôle
  systématique à chaque tentative d'accès (pas seulement au dépôt) — dans
  `tests/CIT.Plateforme.Application.Tests/Documents/AccesDocumentAutoriseTests.cs`
  — Decision: D9 (port de stockage, contrôle à chaque accès), SEC-12 *(règle métier critique :
  autorisations)*

### Implementation for User Story 6

- [X] T062 [US6] Définir le port de stockage documents et implémenter « Déposer un document »
  (métadonnées + antivirus configurable) dans
  `src/CIT.Plateforme.Domain/Documents/IStockageDocuments.cs`,
  `src/CIT.Plateforme.Application/Documents/DeposerDocumentCommand.cs`
  — CDC: SOC-07, SEC-07 (cité) Contract: **C-11** Test: T060 (depends on T022) [D9]
- [X] T063 [US6] Implémenter « Télécharger un document » (contrôle identité/habilitation à chaque
  accès) dans `src/CIT.Plateforme.Application/Documents/TelechargerDocumentQuery.cs`
  — CDC: SEC-12 (cité) Contract: C-11 Test: T061 (depends on T062, T015)
- [X] T064 [P] [US6] Composant Blazor : dépôt/liste des documents du ticket dans
  `src/CIT.Plateforme.Web/Pages/Tickets/Documents.razor`
  — CDC: SOC-07 Contract: C-11 (depends on T062)

**Checkpoint** : US1–US6 fonctionnelles indépendamment.

---

## Phase 9: User Story 7 — Regroupement en dossier et liaison entre tickets (Priority: P4)

**Goal** : offrir une vue globale d'un dossier multi-tickets et permettre la liaison entre
tickets, y compris entre modules.

**Independent Test** : créer plusieurs tickets d'un même dossier (US1), vérifier leur
regroupement, créer une liaison manuelle entre deux tickets.

### Tests for User Story 7 ⚠️

- [X] T065 [P] [US7] [TEST-CDC] REC-SCE-14 (portion dossier — hors canaux/continuité/cloisonnement
  client, voir US11) : regroupement multi-tickets, suivi global — dans
  `tests/CIT.Plateforme.Web.Tests/RecetteCdc/RecSce14TransverseTests.cs`
  — CDC couverts (portion socle) : SOC-11
- [X] T066 [US7] [TEST-TECH] Liaison entre deux tickets (y compris inter-modules) visible par les
  agents et par le client concerné — dans
  `tests/CIT.Plateforme.Application.Tests/Tickets/LiaisonInterTicketsTests.cs`
  — Decision: SOC-10 (couverture Partielle via REC-SCE-14, `recipe-scope-audit.md`)

### Implementation for User Story 7

- [X] T067 [US7] Implémenter « Consulter un dossier » (vue groupée des tickets, état dérivé) dans
  `src/CIT.Plateforme.Application/Tickets/ConsulterDossierQuery.cs`
  — CDC: SOC-11 Contract: **C-09** Test: T065, T047 (depends on T009, T051)
- [X] T068 [US7] Implémenter « Lier deux tickets » (création par un agent, visibilité client) dans
  `src/CIT.Plateforme.Application/Tickets/LierTicketsCommand.cs`
  — CDC: SOC-10 Contract: — Test: T066 (depends on T022)
- [X] T069 [P] [US7] Page Blazor : vue dossier + tickets liés dans
  `src/CIT.Plateforme.Web/Pages/Dossiers/DetailDossier.razor`
  — CDC: SOC-10, SOC-11 Contract: C-09 (depends on T067, T068)

**Checkpoint** : US1–US7 fonctionnelles indépendamment.

---

## Phase 10: User Story 8 — Mesure des temps, SLA, alertes et escalades (Priority: P4)

**Goal** : mesurer automatiquement les temps, appliquer des seuils SLA paramétrables, déclencher
alertes puis escalades.

**Independent Test** : faire transiter un ticket (US1) à travers plusieurs états avec délais
contrôlés, vérifier les mesures, le déclenchement d'une alerte de seuil et d'une escalade.

### Tests for User Story 8 ⚠️

- [X] T070 [P] [US8] [TEST-CDC] REC-SCE-12 complet : mesure attente/traitement par étape,
  distinction temps interne/temps client, déclenchement des seuils 50 %/80 %/dépassement,
  escalade — dans `tests/CIT.Plateforme.Web.Tests/RecetteCdc/RecSce12TempsSlaTests.cs` (complète
  T040) — CDC couverts : TPS-01, TPS-02, TPS-03, TPS-04, SLA-01, SLA-02, SLA-03, SLA-05, ALE-01,
  ALE-02, ALE-03 *(règle métier critique : SLA)*
- [X] T071 [P] [US8] [TEST-TECH] Moteur SLA/temps pur (classe de domaine sans dépendance
  infrastructure), unitairement testable, calcul d'échéance sur calendrier ouvré — dans
  `tests/CIT.Plateforme.Domain.Tests/SuiviTemps/MoteurSlaTests.cs` — Decision: D7

### Implementation for User Story 8

- [X] T072 [US8] Implémenter la Mesure de temps (attente / traitement / interne CIT / attente
  client / total) dans `src/CIT.Plateforme.Domain/SuiviTemps/MesureTemps.cs`
  — CDC: TPS-01, TPS-02, TPS-03, TPS-04, TPS-05 Contract: **C-17** Test: T070, T071 [D7]
  (depends on T042)
- [X] T073 [US8] Implémenter le calendrier ouvré paramétrable (horaires, jours fériés, fuseau CIT)
  dans `src/CIT.Plateforme.Domain/SuiviTemps/CalendrierOuvre.cs`
  — CDC: SLA-04 Contract: **C-05** Test: T071
- [X] T074 [US8] Implémenter l'Échéance SLA + seuils paramétrables (info/alerte/dépassement) dans
  `src/CIT.Plateforme.Domain/SuiviTemps/EcheanceSla.cs`
  — CDC: SLA-01, SLA-02, SLA-03, SLA-05 Contract: C-05 Test: T070 (depends on T073)
- [X] T075 [US8] Implémenter « Surveiller les échéances SLA et émettre les alertes » en
  `BackgroundService` dans
  `src/CIT.Plateforme.Infrastructure/TraitementsDeFond/SurveillanceSlaService.cs`
  — CDC: ALE-01, ALE-02, ALE-03 Contract: **C-21** Test: T070 [D5] (depends on T074)
- [X] T076 [P] [US8] Indicateurs temps/SLA sur `src/CIT.Plateforme.Web/Pages/Tickets/IndicateursTemps.razor`
  — CDC: TPS-01, SLA-02 Contract: C-17, C-21 (depends on T072, T075)

**Checkpoint** : US1–US8 fonctionnelles indépendamment.

---

## Phase 11: User Story 9 — Disponibilité des opérateurs et affectation automatique (Priority: P5)

**Goal** : connaître la disponibilité des opérateurs et piloter les modes d'affectation
automatique sans jamais affecter un agent indisponible.

**Independent Test** : faire varier la disponibilité déclarée d'un opérateur, vérifier qu'aucune
affectation automatique ne lui parvient tant qu'il est indisponible/absent.

### Tests for User Story 9 ⚠️

- [X] T077 [P] [US9] [TEST-SPEC] Tout changement de disponibilité (Disponible/Indisponible/Absent)
  est tracé — dans `tests/CIT.Plateforme.Application.Tests/Affectation/DisponibiliteTests.cs`
  — Exigence CDC: DIS-03 → US9 → scénario d'acceptation 2 de US9 → Test *(aucun scénario CDC
  direct dans ce périmètre)*
- [X] T078 [P] [US9] [TEST-SPEC] Le changement de stratégie d'affectation d'une file est
  réalisable sans développement — dans
  `tests/CIT.Plateforme.Application.Tests/Affectation/StrategieAffectationParametrableTests.cs`
  — Exigence CDC: AFF-07 → US9 → scénario d'acceptation 4 de US9 → Test *(aucun scénario CDC
  direct dans ce périmètre)*
- [X] T079 [US9] [TEST-TECH] Aucune stratégie d'affectation automatique (Round-Robin, charge,
  disponibilité, compétence) ne propose jamais de ticket à un agent Indisponible ou Absent — dans
  `tests/CIT.Plateforme.Domain.Tests/Affectation/DisponibiliteExclusionTests.cs`
  — Decision: invariant #11 de `data-model.md`, DIS-01

### Implementation for User Story 9

- [X] T080 [US9] Implémenter la Disponibilité opérateur (3 états) + traçabilité dans
  `src/CIT.Plateforme.Domain/Affectation/Disponibilite.cs`
  — CDC: DIS-01, DIS-02, DIS-03 Contract: **C-07** Test: T077, T079 (depends on T011)
- [X] T081 [US9] Implémenter les stratégies d'affectation paramétrables par file (Round-Robin,
  charge, disponibilité, compétence/habilitation) dans
  `src/CIT.Plateforme.Domain/Affectation/{RoundRobinStrategy.cs,ChargeStrategy.cs,DisponibiliteStrategy.cs,CompetenceStrategy.cs}`
  — CDC: AFF-03, AFF-04, AFF-05, AFF-06 Contract: — Test: T079 (depends on T080)
- [X] T082 [US9] Implémenter « Distribuer automatiquement les tickets d'une file » en
  `BackgroundService` dans
  `src/CIT.Plateforme.Infrastructure/TraitementsDeFond/DistributionAutomatiqueService.cs`
  — CDC: AFF-07 Contract: **C-23** Test: T078, T079 [D5] (depends on T081, T016)
- [X] T083 [P] [US9] Écrans d'administration disponibilité + stratégie de file dans
  `src/CIT.Plateforme.Web/Pages/Administration/{Disponibilite.razor,StrategieFile.razor}`
  — CDC: AFF-07 Contract: C-07, C-23 (depends on T080, T082)

**Checkpoint** : US1–US9 fonctionnelles indépendamment.

---

## Phase 12: User Story 10 — Supervision opérationnelle, priorités et actions de masse (Priority: P5)

**Goal** : offrir au superviseur une vue temps réel de son périmètre, des actions directes, et
piloter la priorité des tickets.

**Independent Test** : consulter les vues de supervision sur des tickets déjà créés/traités
(stories précédentes), déclencher une action superviseur et une action de masse.

### Tests for User Story 10 ⚠️

- [X] T084 [P] [US10] [TEST-CDC] REC-SCE-13 de bout en bout : vue temps réel des files/charge,
  alertes, affectation d'un ticket non pris en charge, déblocage — dans
  `tests/CIT.Plateforme.Web.Tests/RecetteCdc/RecSce13SupervisionTests.cs`
  — CDC couverts : SUP-01, SUP-02, SUP-03, SUP-04, SUP-05, ALE-01, ALE-02, ALE-03, MAS-01 (Should)
- [X] T085 [P] [US10] [TEST-SPEC] Référentiel de priorité paramétrable, influence sur
  ordre/SLA/alertes, traçabilité de toute modification manuelle, client ne peut pas s'auto-déclarer
  urgent — dans `tests/CIT.Plateforme.Domain.Tests/Tickets/PrioriteTests.cs`
  — Exigence CDC: PRI-01, PRI-02, PRI-03, PRI-04 → US10 → scénario d'acceptation 3 de US10 → Test
  *(aucun scénario CDC direct dans ce périmètre)*
- [X] T086 [US10] [TEST-SPEC] Toute action de masse est tracée **ticket par ticket** (conditionnée
  à MAS-01) — dans `tests/CIT.Plateforme.Application.Tests/Supervision/ActionsDeMasseTests.cs`
  — Exigence CDC: MAS-02 → US10 → scénario d'acceptation 4 de US10 → Test *(aucun scénario CDC
  direct dans ce périmètre — conditionnée à MAS-01, Should)*
- [X] T087 [US10] [TEST-TECH] Filtrage strict par périmètre de supervision configuré (CLAR-05) :
  un superviseur ne voit et n'agit que sur son périmètre — dans
  `tests/CIT.Plateforme.Application.Tests/Supervision/PerimetreSupervisionTests.cs`
  — Decision: CLAR-05 (spec.md § Clarifications), Contract C-04

### Implementation for User Story 10

- [X] T088 [US10] Implémenter « Gérer les périmètres de supervision » (rattachement
  superviseur → files/services, borné et configurable — CLAR-05) dans
  `src/CIT.Plateforme.Application/Administration/GererPerimetresSupervisionCommand.cs`
  — CDC: SUP-01 à SUP-05 (cités) Contract: **C-04** Test: T087
- [X] T089 [US10] Implémenter la Priorité (référentiel paramétrable, attribution auto/manuelle,
  traçabilité) dans `src/CIT.Plateforme.Domain/Tickets/Priorite.cs`
  — CDC: PRI-01, PRI-02, PRI-03, PRI-04 Contract: **C-06** Test: T085 (depends on T009)
- [X] T090 [US10] Implémenter les vues de supervision (files, charge, disponibilité, alertes)
  filtrées par périmètre dans
  `src/CIT.Plateforme.Application/Supervision/{VueFilesQuery.cs,VueChargeQuery.cs}`
  — CDC: SUP-01, SUP-02, SUP-03, SUP-05 Contract: **C-24** Test: T084, T087
  (depends on T080, T075, T088)
- [X] T091 [US10] Implémenter les actions superviseur (affecter non-PEC, réaffecter, transférer,
  équilibrer, débloquer, recherche transverse) dans
  `src/CIT.Plateforme.Application/Supervision/ActionsSuperviseurCommand.cs`
  — CDC: SUP-04 Contract: C-24 Test: T084 (depends on T034, T035, T090)
- [X] T092 [US10] Implémenter les actions de masse (sélection multiple, traçage individuel par
  ticket) dans `src/CIT.Plateforme.Application/Supervision/ActionDeMasseCommand.cs`
  — CDC: MAS-01, MAS-02 Contract: — Test: T086 (depends on T091)
- [X] T093 [US10] Implémenter l'export des indicateurs et listes de tickets (Excel, PDF) dans
  `src/CIT.Plateforme.Application/Supervision/ExporterIndicateursQuery.cs`
  — CDC: SOC-14 (Could) Contract: — Test: — (depends on T090)
- [X] T094 [P] [US10] Tableau de bord supervision temps réel via hub SignalR dans
  `src/CIT.Plateforme.Web/Pages/Supervision/TableauDeBord.razor`,
  `src/CIT.Plateforme.Web/Hubs/SupervisionHub.cs`
  — CDC: SUP-01, SUP-02, SUP-03 Contract: C-24 [D2] (depends on T090)

**Checkpoint** : US1–US10 fonctionnelles indépendamment.

---

## Phase 13: User Story 11 — Audit des actions (Priority: P6)

**Goal** : garantir que toute action sensible du socle est tracée dans un journal d'audit
inviolable, distinct de l'historique fonctionnel.

**Independent Test** : exercer une action socle quelconque (produite par n'importe quelle story
précédente) et vérifier l'entrée d'audit correspondante, non modifiable.

### Tests for User Story 11 ⚠️

- [X] T095 [P] [US11] [TEST-CDC] REC-SCE-14 (portion audit/cloisonnement — hors canaux de
  notification et continuité digital/présentiel, hors périmètre F001) : auteur/action/horodatage
  tracés pour les actions sensibles, cloisonnement contrôlé — dans
  `tests/CIT.Plateforme.Web.Tests/RecetteCdc/RecSce14TransverseTests.cs` (complète T065)
  — CDC couverts (portion socle) : SOC-13 ; SEC-01 à SEC-12 cités comme critères transverses
  *(règle métier critique : audit)*
- [X] T096 [US11] [TEST-TECH] Le journal d'audit est append-only : aucune modification ni
  suppression n'est possible par un utilisateur métier ordinaire — dans
  `tests/CIT.Plateforme.Infrastructure.Tests/Persistence/JournalAuditAppendOnlyTests.cs`
  — Decision: D8 *(règle métier critique : audit)*

### Implementation for User Story 11

- [X] T097 [US11] Implémenter « Consulter le journal d'audit » (administrateur technique, distinct
  de l'historique) dans `src/CIT.Plateforme.Application/Securite/ConsulterJournalAuditQuery.cs`
  — CDC: SOC-13 Contract: **C-25** Test: T095, T096 (depends on T011)
- [X] T098 [US11] Renforcer l'append-only du journal d'audit (garde applicative + droits base,
  aucun UPDATE/DELETE métier) dans
  `src/CIT.Plateforme.Infrastructure/Persistence/JournalAuditAppendOnlyGuard.cs`
  — CDC: SOC-13 Contract: C-25 Test: T096
- [X] T099 [P] [US11] Écran de consultation du journal d'audit (administrateur technique) dans
  `src/CIT.Plateforme.Web/Pages/Administration/JournalAudit.razor`
  — CDC: SOC-13 Contract: C-25 (depends on T097)

**Checkpoint** : les 11 User Stories sont fonctionnelles indépendamment — les 74 exigences du
socle sont implémentées.

---

## Phase 14: Hardening / Recette

**Purpose** : validation transverse, non-régression, et configuration des paramètres métier
encore ouverts — sans jamais inventer de valeur à leur place.

- [X] T100 [P] Rejouer REC-SCE-09 à REC-SCE-14 intégralement via `WebApplicationFactory` (bout en
  bout, tous ensemble) dans `tests/CIT.Plateforme.Web.Tests/RecetteCdc/` — confirme T019, T029,
  T030, T040, T070, T084, T095
- [X] T101 [P] Exécuter manuellement le parcours de validation de `quickstart.md` § 5 (non-régression)
- [X] T102 Vérifier que chacun des 25 contrats C-01→C-25 est rattaché à au moins une tâche
  d'implémentation (contrôle de non-orphelinage) et consigner le résultat dans ce fichier §
  Contrôle final ci-dessous
- [X] T103 [P] Durcissement sécurité : HTTPS/TLS forcé, expiration de session paramétrable,
  verrouillage après échecs répétés (SEC-01, SEC-04, SEC-10 — vérifiés en frontière du socle) dans
  `src/CIT.Plateforme.Web/Program.cs`
- [X] T104 [P] Observabilité des traitements de fond (SLA, distribution, relances, alertes) :
  journalisation structurée, sans donnée sensible en clair (Principe XIV) dans
  `src/CIT.Plateforme.Infrastructure/TraitementsDeFond/`
- [X] T105 [P] Mettre à jour `quickstart.md` si des écarts sont constatés entre l'implémentation
  et le parcours documenté
- [ ] T106 **BLOCKED BY PC-07** — Configurer le contenu réel de la matrice des transitions
  (files, destinations, rôles initiateurs, motifs) pour l'environnement de recette — nécessite
  l'atelier métier ; le mécanisme lui-même (T033) n'est pas bloqué, seules les valeurs le sont
- [ ] T107 **BLOCKED BY PC-12** — Configurer les valeurs cibles SLA réelles (seuils, horaires
  ouvrés, jours fériés) pour l'environnement de recette ; le moteur (T072–T074) n'est pas bloqué
- [ ] T108 **BLOCKED BY PC-04** — Configurer la composition réelle des périmètres de supervision
  pour l'environnement de recette ; le mécanisme (T088) n'est pas bloqué
- [ ] T109 **BLOCKED BY PC-20** — Décider du/des rôle(s) habilité(s) à clôturer pour absence de
  réponse par module, pour configuration de recette ; l'habilitation paramétrable (T043) n'est
  pas bloquée
- [ ] T110 **BLOCKED BY PC-01** — Dimensionner l'infrastructure (indexation, pagination,
  rétention) selon la volumétrie cible avant tout test de charge réel
- [ ] T111 **CLARIFICATION REQUIRED** — Environnement SQL de test (conteneur vs instance DSI
  dédiée) à trancher avant exécution CI complète des tests d'intégration (point ouvert,
  `research.md` § Points ouverts, non rattaché à un PC-xx numéroté)
- [X] T112 Exécuter la checklist Definition of Done (constitution.md, Principe XVI) pour chacune
  des 11 User Stories et consigner le résultat

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** : aucune dépendance.
- **Fondations (Phase 2)** : dépend de Setup — **bloque toutes les User Stories**.
- **US1 (Phase 3, P1)** : dépend des Fondations uniquement — c'est le MVP.
- **US2, US3, US4 (P2)** : dépendent des Fondations ; US2 et US3 dépendent aussi de T022 (créer
  ticket, US1) et T023 (prendre en charge, US1) pour disposer d'un ticket sur lequel agir — elles
  restent testables indépendamment une fois US1 livrée.
- **US5, US6 (P3)** : dépendent des Fondations + T022 (US1).
- **US7 (P4)** : dépend de US4 (T050/T051, état dérivé du dossier — CLAR-01).
- **US8 (P4)** : dépend de US3 (T042, mise en attente client, pour la suspension SLA).
- **US9 (P5)** : dépend des Fondations + T016 (gestion des files).
- **US10 (P5)** : dépend de US2 (T034/T035, actions à superviser), US8 (T075, alertes), US9
  (T080, disponibilité).
- **US11 (P6)** : dépend des Fondations (T011, double journal) — peut être testée dès que
  n'importe quelle autre story produit des actions à auditer.
- **Hardening (Phase 14)** : dépend de toutes les User Stories livrées.

### Parallel Opportunities

- Toutes les tâches `[P]` de Setup et Fondations sont parallélisables entre elles.
- Une fois les Fondations terminées, US1, US5, US6 et US9 peuvent démarrer en parallèle (aucune
  dépendance mutuelle avant leurs tâches d'implémentation respectives).
- Au sein de chaque story, tous les tests marqués `[P]` sont parallélisables entre eux (fichiers
  différents), de même que les tâches de modèles/entités marquées `[P]`.

### Implementation Strategy

**MVP (US1 seule)** : Setup → Fondations → US1 → valider indépendamment → démontrable
(création → file → prise en charge, avec numérotation, audit et cloisonnement déjà en place).

**Livraison incrémentale** : US1 (MVP) → US2 → US3 → US4 → US5 → US6 → US7 → US8 → US9 → US10 →
US11, chaque story ajoutant de la valeur sans casser les précédentes, dans l'ordre de priorité de
`spec.md`.

---

## Contrôle final

### 1. Couverture des 74 exigences par des tâches (100 %)

Chaque exigence FR-001→FR-074 de `spec.md` est portée par au moins une tâche d'implémentation
ci-dessus (bloc `CDC:`). Groupes vérifiés : SOC-01→14 (T016, T022–T028, T050–T051, T057, T062,
T067–T068, T093, T097–T098), CYC-01→07 (T043, T049, T052), MSG-01→05 (T057–T058), WKF-01→03
(T016, T022, T033–T034, T037, T039), PEC-01→05 (T023, T026, T036), TRF-01→06 (T033–T034, T036–T037),
TPS-01→05 (T042, T072), SLA-01→05 (T073–T074), ALE-01→03 (T075), SUP-01→05 (T088, T090–T091),
DIS-01→03 (T080), AFF-01→07 (T016, T026, T081–T082), PRI-01→04 (T089), MAS-01→02 (T092).
**→ 74/74.**

### 2. Chaque Must possède au moins un test (150 exigences Must globales ; 62 Must dans ce
périmètre — voir `01-matrice-exigences.md` §A+B)

- 33 Must directement couverts par REC-SCE-09→14 (TEST-CDC) : T019, T029, T030, T040/T070,
  T070, T084, T095.
- 5 Must partiellement couverts par REC-SCE-09→14 (TEST-CDC partiel) : T019 (AFF-01), T029/T039
  (TRF liés), T084 (AFF-02, MAS-01 le cas échéant), T077/T080 (DIS-01/02).
- **24 Must sans scénario CDC direct** (liste exacte : `recipe-scope-audit.md`) : chacun reçoit au
  moins un TEST-SPEC — SOC-02/03/06/08/WKF-01 → T021 ; SOC-05/MSG-01/02/03/05 → T054, T055, T056 ;
  SOC-07 → T060 ; SOC-09 → T046 ; CYC-01/02/03 → T045 ; CYC-04/05 → T041 ; DIS-03 → T077 ;
  AFF-07 → T078 ; PRI-01/02/03/04 → T085 ; MAS-02 → T086.
  **→ 24/24 Must sans scénario CDC couverts par un TEST-SPEC.**

### 3. Couverture des contrats C-01→C-25 (100 %, aucun orphelin)

C-01→T016 · C-02→T033 · C-03→T013 · C-04→T088 · C-05→T073–T074 · C-06→T089 · C-07→T080, T083 ·
C-08→T022, T027 · C-09→T025, T067, T069 · C-10→T057–T059 · C-11→T062–T064 · C-12→T049, T053 ·
C-13→T052, T053 · C-14→T023, T025–T027 · C-15→T034, T038–T039 · C-16→T035–T036, T038–T039 ·
C-17→T042, T044, T072, T076 · C-18→T050, T053 · C-19→T043 · C-20→T049, T053 · C-21→T075, T076 ·
C-22→T043 · C-23→T082–T083 · C-24→T090–T091, T094 · C-25→T097–T099.
**→ 25/25, aucun contrat orphelin.**

### 4. Scénarios de recette

- **REC-SCE-09→14 couverts** : T019 (09), T029 (10), T030 (11), T040+T070 (12), T084 (13),
  T065+T095 (14, portion socle). **→ 6/6.**
- **REC-SCE-01→08 absents** de toute tâche de cette feature — vérifié par relecture : aucune
  occurrence hors des notes explicites de périmètre (en-tête de ce fichier).

---

## Notes

- [P] = fichiers différents, aucune dépendance non résolue.
- [Story] rattache chaque tâche à sa User Story pour la traçabilité.
- Écrire et faire échouer les tests avant l'implémentation correspondante (particulièrement les 9
  règles métier critiques listées en tête de fichier).
- Aucune règle métier n'a été inventée pour produire un test : les 24 TEST-SPEC s'appuient
  exclusivement sur les critères d'acceptation déjà rédigés dans `spec.md`.
- Les points PC-01, PC-04, PC-07, PC-12, PC-20 restent non résolus (BLOCKED BY, Phase 14) ; PC-02
  a une valeur technique par défaut documentée (non bloquante) ; PC-03 ne bloque aucune tâche du
  socle (aucun rôle métier n'y est nommé) ; PC-25 est hors périmètre (feature Notifications future).
