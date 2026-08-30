# Tasks: Billing Office IMPORT & Unicité BL

**Input**: Design documents from `/specs/003-billing-office-import/`
**Prerequisites**: `plan.md`, `spec.md` (24 exigences, 9 User Stories, CLAR-BL-01/02, CLAR-PAIEMENT-01,
CLAR-DEPOSIT-01, clarification normalisation BL), `research.md` (D1→D16), `data-model.md`,
`contracts/use-cases.md` (C3-01→C3-19), `quickstart.md`

> **Cadre** : chaque tâche fonctionnelle porte, lorsque applicable, l'annotation
> `CDC: ... Contract: C3-xx Test: ...`. Chaque tâche de test porte l'étiquette `[TEST-CDC]` (dérivée
> exclusivement des portions IMP/UNI de REC-SCE-01→04 — seuls scénarios CDC §22 applicables à cette
> feature), `[TEST-SPEC]` (dérivée d'un critère d'acceptation de `spec.md`) ou `[TEST-TECH]` (dérivée
> d'une décision `research.md`/`plan.md`). **UNI-04, REC-SCE-05 et suivants n'apparaissent dans
> aucune tâche** (hors périmètre de cette feature — `spec.md` §0.2, §14). Aucune valeur métier n'est
> inventée : PC-03 (rôle paiement), PC-05 (champs IES exacts), PC-08 (pièces exactes), PC-19 (limite
> BL) restent des paramètres/mécanismes génériques non fixés ; `BLOCKED BY PC-xx` n'est utilisé que si
> une tâche ne peut réellement pas être implémentée de manière configurable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Peut s'exécuter en parallèle (fichiers différents, aucune dépendance)
- **[Story]**: User Story concernée (US1→US9, `spec.md`) — absente en Setup/Fondations/Hardening
- Chemins de fichiers exacts inclus dans chaque description

## Path Conventions

Extension de la solution .NET existante (`plan.md` § Project Structure) — aucun nouveau projet :
`src/CIT.Plateforme.{Domain,Application,Infrastructure,Web}/`, `tests/CIT.Plateforme.
{Domain,Application,Infrastructure,Web}.Tests/`.

---

## Phase 1 : Extensions additives de `Ticket` (research.md D2, D12, D13bis)

**Objectif** : les trois seules extensions de Feature 001 autorisées par le plan — strictement
additives, sans régression. **Bloque** toutes les phases suivantes.

- [X] T001 [P] Créer l'enum nullable `TypeAnnulationImport { RejetManifeste }` dans
  `src/CIT.Plateforme.Domain/Tickets/TypeAnnulationImport.cs` — CDC: — Contract: — Test: T004 [D13bis]
- [X] T002 Ajouter les champs additifs `NumeroBl : string?`, `DeclarationPresentielle : bool`
  (défaut `false`), `TypeAnnulationImport : TypeAnnulationImport?` à `Ticket`, avec deux paramètres
  optionnels supplémentaires par défaut `null`/`false` sur `Ticket.Creer(...)` (`numeroBl`,
  `declarationPresentielle` reste positionné uniquement via une méthode dédiée, voir T003), dans
  `src/CIT.Plateforme.Domain/Tickets/Ticket.cs` — CDC: — Contract: — Test: T004, T005 [D2, D12]
  (depends on T001)
- [X] T003 Ajouter les méthodes `Ticket.DeclarerFinalisationPresentielle()` (positionne
  `DeclarationPresentielle = true`) et `Ticket.RejeterImport(string motif, DateTime maintenantUtc)`
  (appelle en interne le comportement de `Annuler`, positionne
  `TypeAnnulationImport = RejetManifeste`, motif transmis à l'appelant pour traçabilité — **aucune
  modification de `StatutTicket`**) dans `src/CIT.Plateforme.Domain/Tickets/Ticket.cs`
  — CDC: IMP-04, IMP-15 Contract: C3-04, C3-19 Test: T004, T005 [D12, D13bis] (depends on T002)
- [X] T004 [P] [TEST-TECH] Non-régression : `Ticket.Creer(numero, module, typeDemande, fileCibleId,
  dossierId, maintenantUtc)` (signature à 6 arguments, tous les appels existants de Feature 001/002)
  produit toujours un ticket avec `NumeroBl == null`, `DeclarationPresentielle == false`,
  `TypeAnnulationImport == null` — dans
  `tests/CIT.Plateforme.Domain.Tests/Tickets/TicketExtensionsBillingImportTests.cs`
  — Décision: D2/D12/D13bis de `research.md` (depends on T002, T003)
- [X] T005 [P] [TEST-TECH] `Ticket.Creer(..., numeroBl)` initialise correctement `NumeroBl` ;
  `RejeterImport(motif, ...)` positionne `Statut = Annule` et `TypeAnnulationImport = RejetManifeste`
  sans jamais passer par `Cloturer` ; `Ticket.Rouvrir()` continue de refuser tout ticket `Annule`
  (y compris un Rejeté) — dans le même fichier que T004 — Décision: D2/D12/D13bis (depends on T002,
  T003)
- [X] T006 Mapper les trois nouveaux champs sur `Ticket` dans
  `src/CIT.Plateforme.Infrastructure/Persistence/PlateformeDbContext.cs` (bloc
  `builder.Entity<Ticket>`) : `NumeroBl` avec collation explicite fixe
  (`.UseCollation("Latin1_General_CI_AS")`, jamais héritée du défaut de l'instance — D3),
  `TypeAnnulationImport` avec `.HasConversion<string>()` — CDC: — Contract: — Test: T019 (collation
  vérifiée par CAS9→CAS12) [D2, D3, D13bis] (depends on T002, T003)
- [X] T007 Créer la migration EF Core additive `ExtensionTicketBillingImport` (3 colonnes nullables
  sur `Tickets`, collation explicite sur `NumeroBl`, **aucun index unique à ce stade** — l'index
  filtré est ajouté en Phase 2 après écriture des tests CAS1→CAS12, voir T015/T017) — CDC: — Contract:
  — Test: T008 [D2, D3] (depends on T006)
- [X] T008 [TEST-TECH] Exécuter la suite de tests complète de Feature 001/002 (`dotnet test`) et
  consigner le résultat : 0 régression attendue après T001→T007 (depends on T004, T005, T007)

**Checkpoint** : `Ticket` porte ses trois extensions sans casser aucun appel ni test existant.

---

## Phase 2 : Fondations — Unicité BL, entité satellite IES, permissions (TEST-FIRST obligatoire)

**Objectif** : le mécanisme critique d'unicité globale du BL (research.md D3, TEST-FIRST imposé),
l'entité satellite de suivi des branches IES (D8), les nouveaux codes de permission (D15). **⚠️
CRITIQUE** : aucune User Story ne peut démarrer avant la fin de cette phase.

### Unicité BL — tests d'abord, mécanisme SGBD ensuite

- [X] T009 [P] [TEST-SPEC] (ÉCRIRE EN PREMIER, DOIT ÉCHOUER avant T012/T013) Soumettre un ticket pour
  un BL déjà associé à un ticket actif (même société) → refus explicite, aucune création — via EF
  InMemory, dans
  `tests/CIT.Plateforme.Application.Tests/BillingImport/ControleUniciteBlTests.cs`
  — Exigence CDC: UNI-01 → US2 → critère d'acceptation 1 (FR-002, FR-003) → Test
- [X] T010 [P] [TEST-SPEC] (ÉCRIRE EN PREMIER) BL déjà actif appartenant à une **autre société** →
  refus, accès à l'existant proposé uniquement si l'appelant en a les droits (cloisonnement CPT-04)
  — dans le même fichier que T009 — Exigence CDC: UNI-01 → US2 → critère d'acceptation 2 (FR-003)
- [X] T011 [P] [TEST-SPEC] (ÉCRIRE EN PREMIER) BL dont l'unique ticket est Clôturé, Annulé, ou Rejeté
  (`TypeAnnulationImport = RejetManifeste`) → nouvelle soumission acceptée sans action d'agent — dans
  le même fichier que T009 — Exigence CDC: UNI-02 → US2 → critère d'acceptation 4 (FR-004) —
  **CLAR-BL-02**
- [X] T012 [P] [TEST-SPEC] (ÉCRIRE EN PREMIER) Un numéro de BL déjà référencé par un ticket d'un
  **autre module** (`Module != "BillingImport"`) n'entre jamais dans le contrôle d'unicité IMPORT —
  dans le même fichier que T009 — Exigence CDC: UNI-03 → US2 → critère d'acceptation 3 (FR-005)
- [X] T013 [P] Ajouter la méthode `ObtenirTicketActifParNumeroBlAsync(string numeroBl, CancellationToken)`
  à `ITicketRepository` dans `src/CIT.Plateforme.Application/Abstractions/Repositories.cs` (extension
  additive de l'interface existante) — CDC: UNI-01 Contract: C3-02 Test: — [D2] (depends on T002)
- [X] T014 [P] Créer l'exception `BlDejaActifException` dans
  `src/CIT.Plateforme.Domain/Tickets/BlDejaActifException.cs` — CDC: UNI-01 Contract: C3-02 Test: —
- [X] T015 Implémenter le contrôle d'unicité (pré-vérification applicative, avant toute tentative
  d'insertion) dans `SoumettreDemandeImportCommand` (voir T033) — lève `BlDejaActifException` si un
  ticket actif existe pour le BL, autorise sinon (Clôturé/Annulé/Rejeté) — fait passer T009-T012
  — CDC: UNI-01, UNI-02, UNI-03 Contract: C3-02 Test: T009, T010, T011, T012 (depends on T013, T014,
  T009, T010, T011, T012)
- [X] T016 [P] [TEST-TECH] (ÉCRIRE EN PREMIER — DOIT DÉMONTRER UN COMPORTEMENT INCORRECT avant
  T018/T019, correct après) Rédiger les tests CAS1→CAS12 de `research.md` D3 sur SQL Server réel
  (LocalDB, jamais InMemory) dans
  `tests/CIT.Plateforme.Infrastructure.Tests/BillingImport/ConcurrenceUniciteBlSqlTests.cs` : CAS1
  (deux sociétés, même BL, simultané → une seule réussite), CAS2 (même société, deux utilisateurs),
  CAS3 (BL actif → refus), CAS4 (BL clôturé → autorisé), CAS5 (BL annulé → autorisé), CAS6 (BL
  rejeté Manifeste → autorisé), CAS7 (échec transactionnel après insertion → aucun verrou fantôme),
  CAS8 (multi-BL : A accepté, B doublon, C accepté → A/C persistent), CAS9 (`"ABC123"`/`"abc123"` →
  doublon), CAS10 (`" ABC123 "`/`"ABC123"` → doublon après trim), CAS11 (`"ABC-123"`/`"ABC123"` et
  `"ABC 123"`/`"ABC123"` → distincts), CAS12 (même résultat que CAS9 sous une collation par défaut
  d'instance différente) — Décision: D3 de `research.md` (depends on T007)
- [X] T017 [P] Ajouter le prédicat conceptuel de l'index unique filtré sur `Ticket.NumeroBl` dans le
  mapping EF Core (`.HasFilter(...)`) — **inclusion explicite des statuts bloquants**
  (`EnAttenteDePriseEnCharge`, `EnCours`, `EnAttenteClient`), **jamais** `NOT IN`, dans
  `src/CIT.Plateforme.Infrastructure/Persistence/PlateformeDbContext.cs` (bloc
  `builder.Entity<Ticket>`) — CDC: UNI-01 Contract: C3-02 Test: T018 [D3] (depends on T006)
- [X] T018 Créer la migration EF Core additive `IndexUniqueFiltreNumeroBl` (un seul index unique
  filtré ajouté, aucune table/colonne existante modifiée) — CDC: UNI-01 Contract: C3-02 Test: T019
  [D3] (depends on T017)
- [X] T019 [TEST-TECH] Appliquer la migration T018 et rejouer intégralement T016 (CAS1→CAS12) : tous
  les cas produisent désormais le résultat attendu (avant T018, CAS1/2/9/10 auraient révélé un
  comportement incorrect — deux succès au lieu d'un) — consigner le résultat — Décision: D3
  (depends on T016, T018)
- [X] T020 [P] Traduire la violation de contrainte SQL Server (`DbUpdateException` sur l'index
  unique filtré) en `BlDejaActifException` au niveau du dépôt/de l'unité de travail, dans
  `src/CIT.Plateforme.Infrastructure/Persistence/Repositories.cs` — couvre la fenêtre de course que
  la pré-vérification applicative (T015) ne peut pas éliminer seule — CDC: UNI-01 Contract: C3-02
  Test: T019 [D3] (depends on T014, T018)

### Entité satellite IES (research.md D8)

- [X] T021 [P] Créer l'enum `BrancheIes { Manifeste, ControleDocumentaire }` dans
  `src/CIT.Plateforme.Domain/Tickets/BrancheIes.cs` — CDC: IMP-40 Contract: C3-16, C3-17 Test: — [D8]
- [X] T022 [P] Créer l'entité satellite `SuiviBrancheIes` (`TicketId`, `Branche`, `EstTerminee`,
  `AuteurId`, `DateAchevementUtc`, méthode `Achever(Guid auteurId, DateTime maintenantUtc)`) dans
  `src/CIT.Plateforme.Domain/Tickets/SuiviBrancheIes.cs` — CDC: IMP-40, IMP-41 Contract: C3-16, C3-17,
  C3-18 Test: T091, T092 [D8, D9] (depends on T021)
- [X] T023 [P] Créer le port `ISuiviBrancheIesRepository` (`AjouterAsync`, `ListerParTicketAsync`)
  dans `src/CIT.Plateforme.Application/Abstractions/Repositories.cs` — CDC: — Contract: — Test: —
  [D8] (depends on T022)
- [X] T024 [P] Implémenter `SuiviBrancheIesRepository` (EF Core) dans
  `src/CIT.Plateforme.Infrastructure/Persistence/Repositories.cs` — CDC: — Contract: — Test: — [D8]
  (depends on T023)
- [X] T025 Mapper `SuiviBrancheIes` (clé, index sur `TicketId`, entité autonome sans relation EF
  configurée vers `Ticket` — même style que `EntreeHistorique`/`PeriodeAttenteClient`) et ajouter
  `DbSet<SuiviBrancheIes>` dans
  `src/CIT.Plateforme.Infrastructure/Persistence/PlateformeDbContext.cs`
  — CDC: — Contract: — Test: — [D8] (depends on T022)
- [X] T026 Créer la migration EF Core additive `AjoutSuiviBrancheIes` (une nouvelle table, aucune
  table existante modifiée) — CDC: — Contract: — Test: — [D8] (depends on T025)

### Permissions et paramétrage (research.md D15, D6)

- [X] T027 [P] Ajouter les nouveaux codes de permission `billing_import.soumettre`,
  `billing_import.manifeste.instruire`, `billing_import.facturation.editer`,
  `billing_import.paiement.valider`, `billing_import.comptabilite.confirmer_lettrage`,
  `billing_import.controle_documentaire.traiter`, `billing_import.ies.manifeste.instruire`,
  `billing_import.ies.controle_documentaire.instruire` dans la classe `Permission` existante
  (extension additive) — `src/CIT.Plateforme.Domain/Securite/NatureUtilisateur.cs`
  — CDC: — Contract: C3-01→C3-19 Test: — [D15]
- [X] T028 Documenter dans `quickstart.md` §3 le paramétrage requis (déjà rédigé) des 6
  `FileDeTraitement` (Pilote Manifeste, Facturation, Validation Paiement, Comptabilité Client,
  Contrôle Documentaire, File IES) et des `TransitionAutorisee` correspondant aux 4 circuits de
  `data-model.md` §5 — **aucune ligne de code** : réutilisation intégrale de `FileDeTraitement`/
  `TransitionAutorisee` existants (Principe VI) — CDC: — Contract: — Test: — [D6]
- [X] T029 Enregistrer `ISuiviBrancheIesRepository` et les commandes/requêtes des User Stories
  suivantes (au fil de leur implémentation) dans
  `src/CIT.Plateforme.Infrastructure/DependencyInjection.cs` (depends on T024)

**Checkpoint** : unicité BL garantie et **prouvée sur SQL Server réel** (CAS1→CAS12), entité
satellite IES en place, permissions ajoutées — aucune régression sur `StatutTicket` ni sur le socle
transverse.

---

## Phase 3 : User Story 1 — Soumettre une demande IMPORT multi-BL (Priority P1) 🎯 MVP

**Objectif** (`spec.md`) : un utilisateur habilité soumet une demande avec un ou plusieurs BL ; un
ticket indépendant est créé par BL accepté, regroupés sous un même dossier.

**Test indépendant** : soumettre 3 BL distincts, vérifier 3 tickets indépendants regroupés sous un
même dossier, chacun visible séparément.

- [X] T030 [P] [US1] [TEST-SPEC] Soumission de 2 BL valides par un utilisateur habilité → 2 tickets
  indépendants créés, regroupés sous un même dossier (réutilisation SOC-11), chacun avec son propre
  `NumeroBl` — dans
  `tests/CIT.Plateforme.Application.Tests/BillingImport/SoumettreDemandeImportTests.cs`
  — Exigence CDC: IMP-01, IMP-02 → US1 → critère d'acceptation 1 (FR-001, FR-006) → Test
- [X] T031 [P] [US1] [TEST-SPEC] Utilisateur sans droit de soumission (Feature 002, ENT-02) → refus
  serveur systématique (`AccesRefuseException`, réutilisée telle quelle) — dans le même fichier que
  T030 — Exigence CDC: IMP-01 → US1 → critère d'acceptation 3 (FR-001) → Test
- [X] T032 [P] [US1] [TEST-SPEC] Soumission de 5 BL dont 1 doublon (BL déjà actif) → les 4 BL valides
  génèrent chacun un ticket créé normalement, seul le doublon est refusé, sans effet secondaire sur
  les autres (transaction indépendante par BL, D4) — dans le même fichier que T030 — Exigence CDC:
  IMP-02 → US1 → Edge Cases (FR-006) → Test (depends on T015)
- [X] T033 [US1] Implémenter `SoumettreDemandeImportCommand` : crée le dossier (société, auteur,
  instantané `Dossier.TypeClient` — réutilisation Feature 002 sans extension) puis, pour chaque BL
  soumis (jusqu'à 5 par défaut, paramétrable — PC-19), tente indépendamment la création du ticket
  (contrôle d'unicité T015, aucune transaction globale — D4) dans
  `src/CIT.Plateforme.Application/BillingImport/SoumettreDemandeImportCommand.cs`
  — CDC: IMP-01, IMP-02, IMP-03 Contract: C3-01 Test: T030, T031, T032 (depends on T013, T015, T033)
- [X] T034 [US1] Enregistrer `SoumettreDemandeImportCommand` dans `DependencyInjection.cs`
  (depends on T033)
- [X] T035 [P] [US1] Page Blazor Server : formulaire de soumission multi-BL (jusqu'à 5 BL, dépôt de
  documents par BL) dans `src/CIT.Plateforme.Web/Components/Pages/BillingImport/SoumettreDemande.razor`
  — CDC: IMP-01 Contract: C3-01 Test: — (depends on T033)

**Checkpoint** : User Story 1 fonctionnelle et testable indépendamment — un dossier multi-BL peut
être soumis, chaque BL générant un ticket indépendant.

---

## Phase 4 : User Story 2 — Contrôler l'unicité du BL à la soumission (Priority P1)

**Objectif** (`spec.md`) : intégration bout-en-bout du contrôle d'unicité (déjà prouvé au niveau
mécanisme en Phase 2) dans le parcours de soumission, avec la bonne expérience de refus/accès.

**Test indépendant** : soumettre deux fois le même BL, vérifier le blocage et la proposition d'accès
conditionnée aux droits ; libérer le BL puis resoumettre avec succès.

- [X] T036 [P] [US2] [TEST-SPEC] Bout-en-bout : BL déjà actif (même société) → soumission bloquée,
  accès au ticket existant proposé — dans
  `tests/CIT.Plateforme.Web.Tests/BillingImport/ControleUniciteBlWebTests.cs`
  — Exigence CDC: UNI-01 → US2 → critère d'acceptation 1 (FR-002, FR-003) → Test (depends on T033)
- [X] T037 [P] [US2] [TEST-SPEC] Bout-en-bout : BL déjà actif (autre société) → message générique,
  aucun accès direct (cloisonnement CPT-04 absolu) — dans le même fichier que T036 — Exigence CDC:
  UNI-01 → US2 → critère d'acceptation 2 (FR-003) → Test
- [X] T038 [P] [US2] [TEST-SPEC] Bout-en-bout : BL rejeté par le Pilote Manifeste → resoumission
  immédiatement acceptée, sans action d'agent — dans le même fichier que T036 — Exigence CDC: UNI-02
  → US2 → critère d'acceptation 4 (FR-004) → Test — **CLAR-BL-02**. *Dépendance inter-story
  explicite* : ce scénario bout-en-bout ne peut être exécuté qu'une fois `RejeterTicketImportCommand`
  disponible (T048, Phase 5/US3) — la règle unitaire équivalente est déjà prouvée dès la Phase 2 par
  T011/T015 ; ce test-ci ne fait que la vérifier à nouveau bout-en-bout (depends on T048)
- [X] T039 [US2] Finaliser l'expérience de refus dans `SoumettreDemandeImportCommand` (T033) :
  message générique quand l'accès n'est pas autorisé, redirection vers le ticket existant sinon
  (`ConsulterProfilSocieteQuery`/cloisonnement Feature 002 réutilisés) — CDC: UNI-01 Contract: C3-02
  Test: T036, T037 (depends on T020, T033)
- [X] T040 [P] [US2] UI : affichage du refus/accès conditionnel sur
  `SoumettreDemande.razor` (T035) (depends on T039)

**Checkpoint** : User Story 2 fonctionnelle et testable indépendamment — le contrôle d'unicité est
visible et correct du point de vue utilisateur, cloisonnement société respecté.

---

## Phase 5 : User Story 3 — Instruire au Pilote Manifeste (Priority P1)

**Objectif** (`spec.md`) : le Pilote Manifeste valide, rejette (motif obligatoire), ou demande un
complément sur chaque ticket individuellement.

**Test indépendant** : présenter 3 tickets d'un même dossier, appliquer 3 décisions différentes,
vérifier l'indépendance totale des trajectoires (REC-SCE-01).

- [X] T041 [P] [US3] [TEST-SPEC] Valider un ticket → éligible à l'aiguillage (US4) — dans
  `tests/CIT.Plateforme.Application.Tests/BillingImport/InstructionManifesteTests.cs`
  — Exigence CDC: IMP-04 → US3 → critère d'acceptation 1 (FR-008) → Test
- [X] T042 [P] [US3] [TEST-SPEC] Rejeter avec motif obligatoire → `Statut = Annule`,
  `TypeAnnulationImport = RejetManifeste`, motif tracé dans `EntreeHistorique.Detail`, client
  notifié, BL libéré (UNI-02) — dans le même fichier que T041 — Exigence CDC: IMP-04 → US3 → critère
  d'acceptation 2 (FR-008) → Test
- [X] T043 [P] [US3] [TEST-SPEC] Rejet sans motif → opération refusée (validation applicative) — dans
  le même fichier que T041 — Exigence CDC: IMP-04 → US3 → critère d'acceptation 2 (FR-008) → Test
- [X] T044 [P] [US3] [TEST-SPEC] Demander un complément → `Ticket.MettreEnAttenteClient()`, client
  notifié (SLA suspendu, TPS-02/04 réutilisés tels quels) — dans le même fichier que T041 —
  Exigence CDC: IMP-05 → US3 → critère d'acceptation 3 (FR-009) → Test
- [X] T045 [P] [US3] [TEST-SPEC] Client dépose les éléments de complément → dépôt de document
  (réutilisation `Document`/`IDocumentRepository`), `Ticket.RependreApresReponseClient()`, retour en
  file Pilote Manifeste — dans le même fichier que T041 — Exigence CDC: IMP-05 → US3 → critère
  d'acceptation 3 (FR-009) → Test
- [X] T046 [P] [US3] [TEST-SPEC] Dossier de 3 tickets (BL distincts) : 1 validé, 1 rejeté (motif),
  1 complément → les trois trajectoires sont totalement indépendantes, aucune n'affecte les autres
  (portion IMP/UNI de REC-SCE-01) — dans le même fichier que T041 — Exigence CDC: IMP-02, IMP-04 →
  US3 → critère d'acceptation 4 → Test
- [X] T047 [US3] Implémenter `ValiderTicketImportCommand` (permission
  `billing_import.manifeste.instruire`, déclenche l'aiguillage US4 en fin de traitement) dans
  `src/CIT.Plateforme.Application/BillingImport/ValiderTicketImportCommand.cs`
  — CDC: IMP-04 Contract: C3-03 Test: T041, T046 (depends on T027, T041)
- [X] T048 [US3] Implémenter `RejeterTicketImportCommand` (motif obligatoire, appelle
  `Ticket.RejeterImport`, audit + historique) dans
  `src/CIT.Plateforme.Application/BillingImport/RejeterTicketImportCommand.cs`
  — CDC: IMP-04 Contract: C3-04 Test: T042, T043 (depends on T003, T027, T042, T043)
- [X] T049 [US3] Implémenter `DemanderComplementManifesteCommand` et `DeposerComplementCommand` dans
  `src/CIT.Plateforme.Application/BillingImport/GestionComplementManifesteCommand.cs`
  — CDC: IMP-05 Contract: C3-05, C3-06 Test: T044, T045 (depends on T027, T044, T045)
- [X] T050 [US3] Enregistrer les 3 commandes dans `DependencyInjection.cs`
  (depends on T047, T048, T049)
- [X] T051 [P] [US3] Page Blazor Server : file d'instruction Pilote Manifeste (valider/rejeter/
  complément) dans `src/CIT.Plateforme.Web/Components/Pages/BillingImport/FilePiloteManifeste.razor`
  — CDC: IMP-04, IMP-05 Contract: C3-03→C3-06 Test: — (depends on T047, T048, T049)

**Checkpoint** : User Story 3 fonctionnelle et testable indépendamment — les 3 décisions Manifeste
sont opérationnelles et prouvées indépendantes sur un dossier multi-BL.

---

## Phase 6 : User Story 4 — Aiguiller automatiquement selon le type de client (Priority P1)

**Objectif** (`spec.md`) : à l'issue d'une validation, le ticket rejoint automatiquement le circuit
correspondant au type de client **figé à la création du dossier**.

**Test indépendant** : valider un ticket pour chacun des 4 profils, vérifier qu'il rejoint la
première étape du circuit correspondant ; vérifier la non-rétroactivité du type.

- [X] T052 [P] [US4] [TEST-SPEC] Ticket validé, dossier « Normal »/« En Compte »/« Deposit » →
  aiguillage vers la file Facturation ; dossier « IES » → aiguillage vers la File IES — dans
  `tests/CIT.Plateforme.Application.Tests/BillingImport/AiguillageTypeClientTests.cs`
  — Exigence CDC: IMP-06 → US4 → critères d'acceptation 1-2 (FR-011) → Test
- [X] T053 [P] [US4] [TEST-SPEC] Dossier créé quand la société était « Normal », société changeant
  de type vers « Deposit » (Feature 002, CPT-06) **après** la création du dossier mais **avant**
  l'aiguillage → l'aiguillage utilise l'instantané figé (« Normal »), jamais le type courant — dans
  le même fichier que T052 — Exigence CDC: IMP-06 → US4 → critère d'acceptation 3 (FR-012) → Test —
  non-rétroactivité CPT-06 (jamais rouverte)
- [X] T054 [US4] Implémenter la logique d'aiguillage (lit exclusivement `Dossier.TypeClient`, jamais
  `SocieteCliente.Profil` — D5) dans
  `src/CIT.Plateforme.Application/BillingImport/AiguillerTicketValideCommand.cs`, invoquée
  automatiquement en fin de `ValiderTicketImportCommand` (T047) — CDC: IMP-06 Contract: C3-07 Test:
  T052, T053 (depends on T047, T052, T053)
- [X] T055 [US4] Enregistrer `AiguillerTicketValideCommand` dans `DependencyInjection.cs` et le
  câbler dans `ValiderTicketImportCommand` (T047) (depends on T054)

**Checkpoint** : User Story 4 fonctionnelle et testable indépendamment — l'aiguillage est prouvé
non-rétroactif, condition d'entrée des 4 circuits suivants.

---

## Phase 7 : User Story 5 — Traiter un dossier Client Normal jusqu'à clôture (Priority P2)

**Objectif** (`spec.md`) : facturation, paiement avec preuve, validation, dépôt BS/BADT, clôture.

**Test indépendant** : dérouler un ticket Client Normal validé jusqu'à sa clôture (REC-SCE-01
portion IMP/UNI complète).

- [X] T056 [P] [US5] [TEST-SPEC] Client demande sa facture → ticket transféré en file Facturation
  — dans `tests/CIT.Plateforme.Application.Tests/BillingImport/CircuitClientNormalTests.cs`
  — Exigence CDC: IMP-10 → US5 → critère d'acceptation 1 (FR-013) → Test
- [X] T057 [P] [US5] [TEST-SPEC] Pilote Facturation édite la facture (`Document`), la joint au
  ticket, client notifié, ticket transféré en file Validation Paiement — dans le même fichier que
  T056 — Exigence CDC: IMP-11 → US5 → critère d'acceptation 2 (FR-014) → Test
- [X] T058 [P] [US5] [TEST-SPEC] Client dépose une preuve de paiement (`Document`) — dans le même
  fichier que T056 — Exigence CDC: IMP-12 → US5 → critère d'acceptation 3 (FR-015) → Test
- [X] T059 [P] [US5] [TEST-SPEC] Utilisateur habilité (`billing_import.paiement.valider`, PC-03
  neutre) valide la preuve → statuts En attente paiement → En attente validation → Payé confirmé,
  transfert au Contrôle Documentaire — dans le même fichier que T056 — Exigence CDC: IMP-13 → US5 →
  critère d'acceptation 4 (FR-016) → Test
- [X] T060 [P] [US5] [TEST-SPEC] Preuve jugée invalide → `Ticket.MettreEnAttenteClient()`, motif
  tracé, nouvelle preuve possible (retour à T058) — CLAR-PAIEMENT-01 — dans le même fichier que T056
  — Exigence CDC: IMP-13 → US5 → critère d'acceptation 6 (FR-016) → Test
- [X] T061 [P] [US5] [TEST-SPEC] Pilote Contrôle Documentaire dépose BS + BADT, renvoie au client,
  clôture (`Ticket.Cloturer`) — dans le même fichier que T056 — Exigence CDC: IMP-14 → US5 → critère
  d'acceptation 5 (FR-017) → Test
- [X] T062 [US5] Implémenter `DemanderFactureCommand` dans
  `src/CIT.Plateforme.Application/BillingImport/DemanderFactureCommand.cs`
  — CDC: IMP-10 Contract: C3-08 Test: T056 (depends on T027, T054, T056)
- [X] T063 [US5] Implémenter `EditerFactureCommand` (permission
  `billing_import.facturation.editer`) dans
  `src/CIT.Plateforme.Application/BillingImport/EditerFactureCommand.cs`
  — CDC: IMP-11 Contract: C3-09 Test: T057 (depends on T027, T057)
- [X] T064 [US5] Implémenter `DeposerPreuvePaiementCommand` dans
  `src/CIT.Plateforme.Application/BillingImport/DeposerPreuvePaiementCommand.cs`
  — CDC: IMP-12 Contract: C3-10 Test: T058 (depends on T058)
- [X] T065 [US5] Implémenter `ValiderPreuvePaiementCommand` (permission
  `billing_import.paiement.valider`, gère validation et refus — CLAR-PAIEMENT-01) dans
  `src/CIT.Plateforme.Application/BillingImport/ValiderPreuvePaiementCommand.cs`
  — CDC: IMP-13 Contract: C3-11 Test: T059, T060 (depends on T027, T059, T060)
- [X] T066 [US5] Implémenter `DeposerDocumentsControleEtCloturerCommand` (permission
  `billing_import.controle_documentaire.traiter`) dans
  `src/CIT.Plateforme.Application/BillingImport/DeposerDocumentsControleEtCloturerCommand.cs`
  — CDC: IMP-14 Contract: C3-12 Test: T061 (depends on T027, T061)
- [X] T067 [US5] Enregistrer les 5 commandes dans `DependencyInjection.cs`
  (depends on T062, T063, T064, T065, T066)
- [X] T068 [P] [US5] Page Blazor Server : file Facturation (édition facture) dans
  `src/CIT.Plateforme.Web/Components/Pages/BillingImport/FileFacturation.razor`
  (depends on T063)
- [X] T069 [P] [US5] Page Blazor Server : dépôt/validation de la preuve de paiement dans
  `src/CIT.Plateforme.Web/Components/Pages/BillingImport/ValidationPaiement.razor`
  (depends on T064, T065)
- [X] T070 [P] [US5] Page Blazor Server : file Contrôle Documentaire (dépôt BS/BADT, clôture) dans
  `src/CIT.Plateforme.Web/Components/Pages/BillingImport/FileControleDocumentaire.razor`
  (depends on T066)
- [X] T071 [US5] [TEST-CDC] Rejeu intégral de la portion IMP/UNI de REC-SCE-01 : 3 BL soumis
  ensemble → 1 validé (circuit Normal complet jusqu'à clôture), 1 rejeté (motif), 1 complément puis
  validé → aucune interférence entre les trois — dans
  `tests/CIT.Plateforme.Web.Tests/BillingImport/RecSce01ImportNormalTests.cs`
  — CDC: IMP-01→06, IMP-10→14, UNI-01→03 (depends on T033, T047, T048, T049, T054, T062-T066)

**Checkpoint** : User Story 5 fonctionnelle et testable indépendamment — circuit Client Normal
complet, REC-SCE-01 (portion Feature 003) intégralement rejoué.

---

## Phase 8 : User Story 6 — Traiter un dossier Client En Compte jusqu'à clôture (Priority P2)

**Objectif** (`spec.md`) : parcours identique au Client Normal jusqu'à l'édition de la facture, puis
transmission directe au Contrôle Documentaire, sans étape de paiement.

**Test indépendant** : dérouler un ticket En Compte jusqu'à clôture, vérifier l'absence de toute
étape de paiement (REC-SCE-02).

- [X] T072 [P] [US6] [TEST-SPEC] Facture éditée pour un dossier « En Compte » → transfert **direct**
  vers le Contrôle Documentaire, aucun passage par la file Validation Paiement — dans
  `tests/CIT.Plateforme.Application.Tests/BillingImport/CircuitClientEnCompteTests.cs`
  — Exigence CDC: IMP-20, IMP-21 → US6 → critère d'acceptation 1 (FR-018, FR-019) → Test
- [X] T073 [P] [US6] [TEST-SPEC] Ticket En Compte clôturé → client récupère l'ensemble des documents
  (facture, BS, BADT) — dans le même fichier que T072 — Exigence CDC: IMP-22 → US6 → critère
  d'acceptation 2 (FR-020) → Test
- [X] T074 [US6] Étendre `EditerFactureCommand` (T063) : sélection de la file de destination
  (Validation Paiement vs Contrôle Documentaire direct) déterminée par `Dossier.TypeClient` — aucun
  nouveau moteur, une seule branche conditionnelle exprimant la donnée de transition déjà paramétrée
  (D6) — CDC: IMP-20, IMP-21 Contract: C3-13 Test: T072 (depends on T063, T072)
- [X] T075 [P] [US6] Page Blazor Server : consultation du ticket En Compte clôturé (documents) dans
  `src/CIT.Plateforme.Web/Components/Pages/BillingImport/ConsulterTicketCloture.razor`
  — CDC: IMP-22 Contract: C3-14 Test: T073 (depends on T073)
- [X] T076 [US6] [TEST-CDC] Rejeu de REC-SCE-02 (portion Feature 003) — dans
  `tests/CIT.Plateforme.Web.Tests/BillingImport/RecSce02ImportEnCompteTests.cs`
  — CDC: IMP-20, IMP-21, IMP-22 (depends on T074)

**Checkpoint** : User Story 6 fonctionnelle et testable indépendamment — absence d'étape de paiement
prouvée, REC-SCE-02 rejoué.

---

## Phase 9 : User Story 7 — Traiter un dossier Client Deposit jusqu'à clôture (Priority P2)

**Objectif** (`spec.md`) : parcours identique au Client Normal jusqu'à la facture, puis lettrage
Comptabilité Client (IRIS, hors portail) avant Contrôle Documentaire.

**Test indépendant** : dérouler un ticket Deposit jusqu'à clôture en vérifiant le passage obligatoire
par la confirmation du Comptable Client (REC-SCE-03).

- [X] T077 [P] [US7] [TEST-SPEC] Facture éditée pour un dossier « Deposit » → transfert vers la file
  Comptabilité Client — dans
  `tests/CIT.Plateforme.Application.Tests/BillingImport/CircuitClientDepositTests.cs`
  — Exigence CDC: IMP-30 → US7 → critère d'acceptation 1 (FR-021) → Test
- [X] T078 [P] [US7] [TEST-SPEC] Comptable Client confirme le lettrage (effectué hors portail dans
  IRIS, D7) → transfert vers le Contrôle Documentaire — dans le même fichier que T077 — Exigence
  CDC: IMP-31 → US7 → critère d'acceptation 2 (FR-022) → Test
- [X] T079 [P] [US7] [TEST-SPEC] Lettrage impossible ou à reprendre → **aucune action**, le ticket
  reste dans la file Comptabilité Client, aucune clôture automatique, aucun retour vers la
  Facturation — CLAR-DEPOSIT-01 — dans le même fichier que T077 — Exigence CDC: IMP-31 → US7 →
  critère d'acceptation 4 (FR-022) → Test
- [X] T080 [P] [US7] [TEST-SPEC] Après lettrage confirmé, Pilote CD dépose les documents, retourne au
  client, clôture — dans le même fichier que T077 — Exigence CDC: IMP-32 → US7 → critère
  d'acceptation 3 (FR-023) → Test
- [X] T081 [US7] Étendre `EditerFactureCommand` (T063, déjà étendu par T074 pour la variante En
  Compte) : branche Deposit → transfert vers Comptabilité Client (donnée de transition paramétrée,
  D6) — CDC: IMP-30 Contract: C3-09 Test: T077 (depends on T063, T074, T077)
- [X] T082 [US7] Implémenter `ConfirmerLettrageCommand` (permission
  `billing_import.comptabilite.confirmer_lettrage` ; aucune action si lettrage impossible —
  CLAR-DEPOSIT-01) dans
  `src/CIT.Plateforme.Application/BillingImport/ConfirmerLettrageCommand.cs`
  — CDC: IMP-31 Contract: C3-15 Test: T078, T079 (depends on T027, T078, T079)
- [X] T083 [US7] Enregistrer `ConfirmerLettrageCommand` dans `DependencyInjection.cs`
  (depends on T082)
- [X] T084 [P] [US7] Page Blazor Server : file Comptabilité Client (confirmation de lettrage) dans
  `src/CIT.Plateforme.Web/Components/Pages/BillingImport/FileComptabiliteClient.razor`
  — CDC: IMP-31 Contract: C3-15 Test: — (depends on T082)
- [X] T085 [US7] [TEST-CDC] Rejeu de REC-SCE-03 (portion Feature 003) — dans
  `tests/CIT.Plateforme.Web.Tests/BillingImport/RecSce03ImportDepositTests.cs`
  — CDC: IMP-30, IMP-31, IMP-32 (depends on T074, T077, T078, T079, T080, T081, T082)

**Checkpoint** : User Story 7 fonctionnelle et testable indépendamment — lettrage bloqué géré sans
invention de règle, REC-SCE-03 rejoué.

---

## Phase 10 : User Story 8 — Traiter un dossier Client IES en parcours parallèle (Priority P3)

**Objectif** (`spec.md`) : traitement simultané par le Pilote Manifeste et le Pilote Contrôle
Documentaire, jointure obligatoire avant retour au client, confidentialité par branche.

**Test indépendant** : instruire les deux branches dans un ordre arbitraire, vérifier que le ticket
n'est jamais restitué avant jointure complète, quel que soit l'ordre (REC-SCE-04).

- [X] T086 [P] [US8] [TEST-SPEC] Ticket IES aiguillé → deux `SuiviBrancheIes` créés (Manifeste,
  Contrôle Documentaire), toutes deux non achevées — dans
  `tests/CIT.Plateforme.Application.Tests/BillingImport/CircuitClientIesTests.cs`
  — Exigence CDC: IMP-40 → US8 → critère d'acceptation 1 (FR-024) → Test (depends on T022)
- [X] T087 [P] [US8] [TEST-SPEC] Pilote Manifeste achève sa branche → `EstTerminee = true` sur la
  ligne Manifeste uniquement ; le ticket n'est **pas** restitué au client (une seule branche
  achevée) — dans le même fichier que T086 — Exigence CDC: IMP-41 → US8 → critère d'acceptation 2
  (FR-025) → Test
- [X] T088 [P] [US8] [TEST-SPEC] Symétrique : Pilote Contrôle Documentaire achève sa branche en
  premier → même comportement (ticket non restitué tant que l'autre branche n'est pas achevée) —
  dans le même fichier que T086 — Exigence CDC: IMP-41 → US8 → critère d'acceptation 2 (FR-025) →
  Test
- [X] T089 [P] [US8] [TEST-TECH] Ordre A→B : Manifeste achevée puis Contrôle Documentaire achevée →
  jointure constatée uniquement après la seconde, ticket disponible pour finalisation IES externe —
  dans le même fichier que T086 — Décision: D9 de `research.md`
- [X] T090 [P] [US8] [TEST-TECH] Ordre B→A : Contrôle Documentaire achevée puis Manifeste achevée →
  même résultat final, jointure indépendante de l'ordre — dans le même fichier que T086 — Décision:
  D9 de `research.md`
- [X] T091 [P] [US8] [TEST-SPEC] Confidentialité (sens 1) : un agent ne détenant que
  `billing_import.ies.manifeste.instruire` ne peut ni agir sur, ni consulter les champs réservés à
  la branche Contrôle Documentaire — refus serveur — dans le même fichier que T086 — Exigence CDC:
  IMP-40 → US8 → Sécurité (transversal) → Test — mécanisme générique (PC-05 : liste exacte des
  champs **BLOCKED BY PC-05**, non requise pour ce test structurel)
- [X] T092 [P] [US8] [TEST-SPEC] Confidentialité (sens 2, symétrique) : un agent ne détenant que
  `billing_import.ies.controle_documentaire.instruire` ne peut ni agir sur, ni consulter les champs
  réservés à la branche Manifeste — refus serveur — dans le même fichier que T086 — Exigence CDC:
  IMP-40 → US8 → Sécurité (transversal) → Test
- [X] T093 [US8] Implémenter `InstruireBrancheIesCommand` (paramétré par `BrancheIes`, vérifie la
  permission correspondante avant toute mutation — D10) dans
  `src/CIT.Plateforme.Application/BillingImport/InstruireBrancheIesCommand.cs`
  — CDC: IMP-40 Contract: C3-16, C3-17 Test: T086, T087, T088, T091, T092 (depends on T022, T023,
  T027, T086, T087, T088, T091, T092)
- [X] T094 [US8] Implémenter `ConstaterJointureIesEtFinaliserCommand` (dérive la jointure des deux
  `SuiviBrancheIes`, jamais un état persisté séparé — D9) dans
  `src/CIT.Plateforme.Application/BillingImport/ConstaterJointureIesEtFinaliserCommand.cs`
  — CDC: IMP-41, IMP-42 Contract: C3-18 Test: T089, T090 (depends on T023, T089, T090)
- [X] T095 [US8] Enregistrer les 2 commandes dans `DependencyInjection.cs`
  (depends on T093, T094)
- [X] T096 [P] [US8] Page Blazor Server : File IES (double instruction par rôle) dans
  `src/CIT.Plateforme.Web/Components/Pages/BillingImport/FileIes.razor`
  (depends on T093)
- [X] T097 [US8] [TEST-CDC] Rejeu de REC-SCE-04 (portion Feature 003), y compris champs réservés par
  rôle (mécanisme générique) et remise des documents — dans
  `tests/CIT.Plateforme.Web.Tests/BillingImport/RecSce04ImportIesTests.cs`
  — CDC: IMP-40, IMP-41, IMP-42, SEC-02 (cité en dépendance transversale) (depends on T086, T087,
  T088, T089, T090, T091, T092, T093, T094)

**Checkpoint** : User Story 8 fonctionnelle et testable indépendamment — jointure prouvée dans les
deux ordres, confidentialité prouvée dans les deux sens, REC-SCE-04 rejoué.

---

## Phase 11 : User Story 9 — Basculer vers une finalisation présentielle (Priority P4, Should)

**Objectif** (`spec.md`) : à l'issue du contrôle BL, le client peut déclarer son intention de
finaliser physiquement ; le ticket porte la trace, sans mécanisme de RDV (hors périmètre, point
d'extension F009).

**Test indépendant** : après validation Manifeste, déclarer l'intention, vérifier la trace, sans
qu'aucun mécanisme de rendez-vous ne soit requis.

- [X] T098 [P] [US9] [TEST-SPEC] Ticket validé par le Pilote Manifeste, client déclare vouloir
  finaliser physiquement → `Ticket.DeclarationPresentielle = true`, trace horodatée — dans
  `tests/CIT.Plateforme.Application.Tests/BillingImport/DeclarationPresentielleTests.cs`
  — Exigence CDC: IMP-15 → US9 → critère d'acceptation 1 (FR-027) → Test
- [X] T099 [P] [US9] [TEST-SPEC] Déclaration tentée sur un ticket **non encore validé** par le Pilote
  Manifeste → refus (précondition non remplie) — dans le même fichier que T098 — Exigence CDC:
  IMP-15 → US9 → critère d'acceptation implicite → Test
- [X] T100 [US9] Implémenter `DeclarerFinalisationPresentielleCommand` (appelle
  `Ticket.DeclarerFinalisationPresentielle()`, T003) dans
  `src/CIT.Plateforme.Application/BillingImport/DeclarerFinalisationPresentielleCommand.cs` —
  **aucun mécanisme de RDV/guichet implémenté** (hors périmètre, point d'extension F009) — CDC:
  IMP-15 Contract: C3-19 Test: T098, T099 (depends on T003, T098, T099)
- [X] T101 [US9] Enregistrer `DeclarerFinalisationPresentielleCommand` dans
  `DependencyInjection.cs` (depends on T100)
- [X] T102 [P] [US9] Action « finaliser au Billing Office » sur la page de consultation du ticket
  client (composant partagé, pas de nouvelle page dédiée) — CDC: IMP-15 Contract: C3-19 Test: —
  (depends on T100)

**Checkpoint** : les 9 User Stories sont désormais fonctionnelles et indépendamment testables.

---

## Phase 12 (finale) : Hardening / recette

- [X] T103 [TEST-TECH] Exécuter `quickstart.md` intégralement : paramétrage des 6 files/transitions
  (T028), 4 sociétés de test (une par profil), parcours de validation §5.1→§5.5 — consigner le
  résultat (depends on T028, T033-T102)
- [X] T104 [TEST-TECH] Vérification mécanique : 24/24 exigences retenues ↔ tâches (IMP-01→06 :
  T030-T055 ; IMP-10→15 : T056-T102 ; UNI-01→03 : T009-T020, T036-T039), UNI-04 absente de toute
  tâche d'implémentation (depends on T103)
- [X] T105 [TEST-TECH] Vérification mécanique : 23/23 Must avec au moins un test — IMP-01→06 (T030,
  T041, T052), IMP-10→14 (T056-T061), IMP-20→22 (T072-T073), IMP-30→32 (T077-T080), IMP-40→42
  (T086-T092), UNI-01→03 (T009-T012, T036-T038) ; IMP-15 (Should) testée par T098-T099
  (depends on T103)
- [X] T106 [TEST-TECH] Vérification mécanique : 19/19 contrats C3-01→C3-19 couverts (voir table de
  couverture de `contracts/use-cases.md`) (depends on T103)
- [X] T107 [TEST-CDC] Vérification mécanique : REC-SCE-01→04 (portions IMP/UNI) tous rejoués et
  verts (T071, T076, T085, T097) ; aucune mention de REC-SCE-05 ou suivant dans ce fichier
  (depends on T071, T076, T085, T097)
- [X] T108 [TEST-TECH] Vérification mécanique : CAS1→CAS12 tous prévus et verts sur SQL Server réel
  (T016, T019) (depends on T019)
- [X] T109 [TEST-TECH] Vérification mécanique : chaque règle critique possède son test écrit avant
  son implémentation (unicité BL T009-T012→T015/T016→T017-T019 ; rejet/libération BL T042→T048 ;
  aiguillage snapshot T052-T053→T054 ; paiement invalide T060→T065 ; Deposit/lettrage T079→T082 ;
  branches IES T086-T092→T093-T094) (depends on T103)
- [X] T110 [TEST-TECH] Vérification mécanique : aucun rôle PC-03 nommé en dur (grep
  `billing_import.paiement.valider`, formulation neutre partout) ; aucun champ IES PC-05 inventé
  (mécanisme générique par permission uniquement) ; aucune intégration IRIS créée (grep, aucun appel
  HTTP/SDK externe dans le code Feature 003) ; IMP-15 ne déborde pas vers un mécanisme CON-xx (T100
  relu) (depends on T103)
- [X] T111 [TEST-TECH] Vérification mécanique (`git diff`/lecture) : `Ticket.cs` (T002-T003) reste
  compatible avec tous les appels existants de Feature 001/002 (T004-T005) ; `StatutTicket.cs` **non
  modifié** ; `CloisonnementService`, `IAutorisationService`, `IJournalisationService`,
  `IUniteDeTravail` **non modifiés** — seuls des appels et des ajouts additifs (depends on T110)
- [X] T112 Exécuter la suite de tests complète (`dotnet test`) — Feature 001 (baseline existant) +
  Feature 002 (baseline existant, 180 tests) + Feature 003 (T004-T005, T008, T009-T012, T016/T019,
  T030-T032, T036-T038, T041-T046, T052-T053, T056-T061, T072-T073, T077-T080, T086-T092, T098-T099,
  T071, T076, T085, T097) — 0 régression attendue, consigner le résultat final (nombre de tests
  passants par catégorie TEST-CDC/TEST-SPEC/TEST-TECH) (depends on T111)

**Checkpoint final** : Feature 003 entièrement implémentée, testée, sans régression sur Feature
001/002, prête pour `/speckit.implement`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1** (extensions `Ticket`) : aucune dépendance — démarre immédiatement, **bloque** tout le
  reste (toutes les phases suivantes dépendent de `NumeroBl`/`DeclarationPresentielle`/
  `TypeAnnulationImport`).
- **Phase 2** (fondations — unicité BL, satellite IES, permissions) : dépend de Phase 1 — **bloque**
  toutes les User Stories. C'est la phase la plus critique (TEST-FIRST imposé pour l'unicité BL,
  CAS1→CAS12).
- **Phases 3→11** (US1→US9) : dépendent toutes de Phase 2.
  - US1, US2, US3 (P1) forment le tronc commun — US2 dépend des mêmes commandes qu'US1 (finalise
    l'expérience utilisateur du mécanisme déjà prouvé en Phase 2) ; US3 est indépendante d'US1/US2
    au niveau unitaire mais nécessite un ticket soumis pour un test bout-en-bout.
  - US4 dépend d'US3 (aiguillage déclenché après validation Manifeste).
  - US5, US6, US7 (P2) dépendent toutes d'US4 (aiguillage) mais sont indépendantes entre elles —
    peuvent être développées en parallèle par 3 personnes différentes.
  - US8 (P3) dépend d'US4 (aiguillage vers la File IES) mais pas d'US5/US6/US7.
  - US9 (P4, Should) dépend uniquement d'US3 (ticket validé par le Manifeste) — indépendante des
    circuits US5→US8.
- **Phase 12** (Hardening) : dépend de toutes les phases précédentes.

### Parallel Opportunities

- Toutes les tâches `[P]` de la Phase 2 (T009-T012, T013-T014, T016-T017, T021-T024, T027) peuvent
  s'exécuter en parallèle une fois leurs dépendances immédiates satisfaites.
- US5, US6, US7 peuvent être développées en parallèle par des personnes différentes dès la fin d'US4
  (aucun fichier partagé au-delà de l'extension conditionnelle de `EditerFactureCommand`, T063/T075/
  T082 — à séquencer entre elles si un seul développeur).
- US8 et US9 peuvent être développées en parallèle de US5→US7 (fichiers distincts).
- Au sein de chaque User Story, les tâches de test `[P]` (ex. T056-T061, T086-T092) peuvent s'écrire
  en parallèle avant l'implémentation qui les fait passer.

## Implementation Strategy

### MVP (User Stories 1→4 uniquement)

1. Phase 1 (extensions) → Phase 2 (fondations, unicité BL prouvée SQL Server réel) → Phase 3 (US1) →
   Phase 4 (US2) → Phase 5 (US3) → Phase 6 (US4)
2. **STOP et VALIDER** : une demande multi-BL peut être soumise, contrôlée en unicité, instruite par
   le Pilote Manifeste, et aiguillée automatiquement — sans encore aller jusqu'à la clôture d'un
   circuit complet.

### Livraison incrémentale

Phase 1+2 → US1+US2+US3+US4 (tronc commun, MVP) → US5 (circuit Normal, le plus complet et le plus
fréquent) → US6+US7 (variantes En Compte/Deposit) → US8 (IES, le plus complexe) → US9 (présentiel,
Should) → Phase 12 (Hardening).

---

## Contrôle de couverture *(vérification mécanique — à confirmer en Phase 12, T104→T111)*

| # | Contrôle | Résultat attendu |
|---|---|---|
| 1 | 24/24 exigences couvertes par au moins une tâche | Voir T104 |
| 2 | 23/23 Must avec au moins un test | Voir T105 |
| 3 | 19/19 contrats C3-01→C3-19 couverts | Voir T106 |
| 4 | REC-SCE-01→04 (portions IMP/UNI) rejoués et verts | T071, T076, T085, T097 |
| 5 | REC-SCE-05 et suivants absents | Aucune tâche ne les rejoue ni ne les implémente — les 3 seules occurrences du texte dans ce fichier sont des rappels explicites de leur exclusion (en-tête et cette table) |
| 6 | UNI-04 absente de toute tâche d'implémentation | Aucune tâche ne l'implémente — les 3 seules occurrences du texte dans ce fichier confirment son exclusion (voir `spec.md` §0.2) |
| 7 | CAS1→CAS12 tous prévus et verts sur SQL Server réel | T016, T019 |
| 8 | Règles critiques test-first (ID de test < ID d'implémentation) | Voir T109 |
| 9 | Aucun rôle PC-03 inventé | T027 (permission neutre `billing_import.paiement.valider`), T110 |
| 10 | Aucun champ IES PC-05 inventé | T094 (mécanisme générique par permission), T110 |
| 11 | Aucune intégration IRIS créée | T083 (transfert de file uniquement), T110 |
| 12 | IMP-15 ne déborde pas vers CON | T100 (déclaration/trace uniquement, aucun RDV) |
| 13 | Extensions Feature 001/002 additives, aucune régression | T004-T005, T008, T111, T112 |
| 14 | Aucun code généré par cette commande | Confirmé — uniquement `tasks.md` produit |
