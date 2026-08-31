---
description: "Task list for Feature 008 — Enrôlement Transporteurs"
---

# Tasks: Feature 008 — Enrôlement Transporteurs

**Input** : `specs/008-enrolement-transporteurs/{spec.md, plan.md, research.md, data-model.md, contracts/use-cases.md, quickstart.md, checklists/requirements.md}`, `.specify/memory/constitution.md`, code réel F001-F006.

**Tests** : INCLUS (TEST-FIRST obligatoire — Constitution Principe XII, confirmé par plan.md §"Phases d'Implémentation Prévues").

**Organisation** : tâches groupées par phase fonctionnelle (Preflight → Fondations → User Stories → UI → REC-SCE-08 → Hardening → Non-régression), chaque tâche liée à son exigence ENR, son contrat C8 (si applicable) et son test.

**Statut** : IMPLÉMENTÉE (exécution du 31 août 2026 — voir `implementation-report.md` pour le rapport complet).

---

## Phase 1 : Preflight / Baseline (réelle, exécutée — pas simulée)

**But** : établir la baseline réelle avant toute modification. Aucune tâche de code dans cette phase.

- [X] T001 **Preflight réel exécuté le 31 août 2026 (via PowerShell natif — le sandbox Bash ne joint pas correctement LocalDB)** :
  - `dotnet build` → 0 avertissement, 0 erreur.
  - `CIT.Plateforme.Domain.Tests` → **82/82 verts**.
  - `CIT.Plateforme.Application.Tests` → **263/263 verts**.
  - `CIT.Plateforme.Infrastructure.Tests` → **65/65 verts**.
  - `CIT.Plateforme.Web.Tests` → **115/116** — 1 échec : `DashboardAgentTests.DashboardAgent_RenduComplet_FilesEtTickets` (assertion `"Tableau de bord Opérateur"` non trouvée dans le markup rendu).
  - `dotnet ef migrations has-pending-model-changes` → *"No changes have been made to the model since the last migration."* (0 changement en attente — cohérent avec plan.md "MIGRATION PRÉVUE : NON").
- [X] T002 **Remédiation ciblée de la baseline exécutée le 31 août 2026 (avant toute tâche F008)** :
  - **Reproduction** : le test isolé passait déjà seul (`--filter`) ; le run complet de `Web.Tests` a d'abord reproduit 116/116 (3 exécutions consécutives) — la panne T001 était **intermittente**, pas systématique.
  - **Analyse du contrat fonctionnel** (`DashboardAgentTests.cs`, `DashboardAgent.razor`, `git diff` du fichier) : le texte `"Tableau de bord Opérateur"` est présent littéralement dans le `.razor` de la refonte UI non commitée (`<span>⚡</span> Tableau de bord Opérateur`) — **aucun comportement fonctionnel n'a été supprimé ou cassé**. Le test appelait `Render<DashboardAgent>()` puis assertait immédiatement sur `cut.Markup`, **sans attendre la fin de `OnInitializedAsync`** — pendant que `_chargement == true`, le composant affiche `<IndicateurChargement Message="Chargement de votre espace de travail..." />` (`DashboardAgent.razor` lignes 16-18), qui ne contient pas le texte attendu. Sous charge (suite complète, contention du pool de threads), la continuation asynchrone pouvait ne pas être terminée au retour de `Render`, provoquant une capture intermittente de l'état "Chargement...".
  - **CLASSIFICATION : CAS B** — comportement fonctionnel intact ; le test dépendait d'une hypothèse de timing implicite (rendu synchrone complet), pas d'un détail esthétique obsolète au sens strict, mais relève bien de la même famille ("corriger le test, pas le produit").
  - **Remédiation minimale** : ajout d'un `cut.WaitForState(() => cut.Markup.Contains("Tableau de bord Opérateur"), TimeSpan.FromSeconds(5))` avant les assertions, dans [tests/CIT.Plateforme.Web.Tests/Pages/Agent/DashboardAgentTests.cs](../../tests/CIT.Plateforme.Web.Tests/Pages/Agent/DashboardAgentTests.cs) — patron déjà en usage ailleurs dans la suite (`RecSce07CustomerComplaintE2ETests`, `NouvelleReclamationTests`). **`DashboardAgent.razor` et la refonte UI en cours n'ont pas été modifiés.**
  - **Vérification** : `Web.Tests` → **116/116**, 3 exécutions consécutives après correctif ; solution complète → **526/526** (Domain 82 + Application 263 + Infrastructure 65 + Web 116) ; build 0 avertissement/0 erreur ; `has-pending-model-changes` → aucun changement.
  - **Baseline désormais 100 % verte.** Aucune tolérance restante — les tâches de code F008 (Phase 2 et suivantes) peuvent démarrer sur cette base.

**Checkpoint** : baseline réelle 526/526, remédiation tracée, aucune tolérance résiduelle. ✅ ATTEINT.

---

## Phase 2 : Fondations (bloquant — requis par toutes les User Stories)

**⚠️ CRITIQUE** : aucune tâche de User Story ne doit démarrer avant la fin de cette phase.

### Domaine (research.md D8, data-model.md §2.1)

- [X] T003 [P] TEST RED — `tests/CIT.Plateforme.Domain.Tests/Tickets/TicketEnrolementTests.cs` : `DefinirInformationsEnrolement` doit renseigner `Objet`/`Description` existants (nominal), lever `ArgumentException` si `objet` ou `description` vide. (ENR-01) — 4 tests créés, RED confirmé (erreur de compilation) avant implémentation.
- [X] T004 Implémentation minimale GREEN — `src/CIT.Plateforme.Domain/Tickets/Ticket.cs` : méthode `DefinirInformationsEnrolement(string objet, string description)` ajoutée, réutilisant `Objet`/`Description` déjà existants (aucune nouvelle propriété, aucune migration).
- [X] T005 Vérifié : `CIT.Plateforme.Domain.Tests` → **86/86 verts** (82 existants + 4 nouveaux), non-régression Domain confirmée.

### Sécurité / RBAC (research.md D3, D9, D11)

- [X] T006 [P] `src/CIT.Plateforme.Domain/Securite/NatureUtilisateur.cs` : constantes `Permission.EnrolementSoumettre = "enrolement.soumettre"` et `Permission.EnrolementInstruire = "enrolement.instruire"` ajoutées.
- [X] T007 [P] TEST RED — `tests/CIT.Plateforme.Infrastructure.Tests/Persistence/DossierRepositoryTests.cs` (nouveau fichier) : `ListerParClientAsync` — 2 tests (isolation deux clients, liste vide), RED confirmé sur SQL Server réel (LocalDB) avant implémentation.
- [X] T008 `src/CIT.Plateforme.Application/Abstractions/Repositories.cs` : `ListerParClientAsync(Guid clientId, ...)` ajoutée à `IDossierRepository`.
- [X] T009 Implémentation minimale GREEN — `src/CIT.Plateforme.Infrastructure/Persistence/Repositories.cs` : `DossierRepository.ListerParClientAsync` implémentée (`Where(d => d.ClientId == clientId)`).
- [X] T010 Vérifié : `CIT.Plateforme.Infrastructure.Tests` → **67/67 verts** (65 existants + 2 nouveaux) sur SQL Server réel.

### Persistance / Seed (research.md D4, data-model.md §7 — donnée, pas de migration)

- [X] T011 `src/CIT.Plateforme.Infrastructure/Persistence/DevelopmentSeeder.cs` : `FileDeTraitement.Creer("Enrolement", "Agent Gate")` ajoutée dans `SeedEnrolementAsync` (nouvelle méthode). Aucune migration.
- [X] T012 [P] `DevelopmentSeeder.cs` : compte Agent Gate de démonstration (`agent.gate@test.local`) habilité `EnrolementInstruire` sur la file "Agent Gate".
- [X] T013 [P] `DevelopmentSeeder.cs` : compte Transporteur/Chauffeur de démonstration (`transporteur.chauffeur@test.local`, `Nature=Client`, **sans société**) habilité `EnrolementSoumettre`. **Décision d'attribution tracée dans le code** (commentaire `SeedEnrolementAsync`) : accordée par défaut à tout compte `Nature=Client` non rattaché à une société, mirroring `ComplaintSoumettre`/`BillingImportSoumettre`.
- [X] T014 `src/CIT.Plateforme.Infrastructure/DependencyInjection.cs` : section "FEATURE 008 — Enrôlement Transporteurs" ouverte, complétée au fil des phases 3-7 (11 enregistrements au total à l'issue de l'implémentation : `SoumettreEnrolementCommand`, `ListerMesEnrolementsQuery`, `ValiderEnrolementCommand`, `DeclencherConvocationCommand`, `ConstaterRemiseBadgeCommand`, `ConsulterFileAgentGateQuery`, `ObtenirFicheEnrolementQuery`).

**Checkpoint** : Domaine, RBAC, seed en place. ✅ ATTEINT — build 0 warning à ce stade.

---

## Phase 3 : US1 — Soumettre un enrôlement (Must, ENR-01, C8-01) 🎯 MVP

**But** : le Transporteur/Chauffeur soumet un dossier d'enrôlement (informations + pièces) et le ticket est créé en file "Agent Gate".

### Tests pour US1 (RED, avant implémentation)

- [X] T015 [P] [US1] TEST RED — `tests/CIT.Plateforme.Application.Tests/Enrolement/SoumettreEnrolementCommandTests.cs` : cas nominal, RED confirmé (compilation) avant implémentation. (ENR-01, C8-01)
- [X] T016 [P] [US1] TEST RED — même fichier : permission refusée → `AccesRefuseException`.
- [X] T017 [P] [US1] TEST RED — même fichier : `Objet`/`Description` vide → `ArgumentException` (4 cas `[Theory]`).
- [X] T018 [P] [US1] TEST RED — même fichier : `FileAgentGate_NonParametree_LeveInvalidOperationException` (couvre le cas "file non paramétrée" à la place d'un fichier séparé `SoumettreEnrolementIntegrationTests.cs` — regroupé dans le même fichier de test unitaire, l'intégration EF réelle étant démontrée par T037 `CycleCompletEnrolementIntegrationTests`).

### Implémentation US1

- [X] T019 [US1] Implémentation minimale GREEN — `src/CIT.Plateforme.Application/Enrolement/SoumettreEnrolementCommand.cs` créée conformément à la spécification de la tâche.
- [X] T020 [US1] Vérifié T015-T018 GREEN (7/7).
- [X] T021 [US1] `SoumettreEnrolementCommand` enregistrée dans `DependencyInjection.cs`.
- [X] T022 [P] [US1] TEST GREEN direct (réutilisation pure, aucun RED nécessaire) — `tests/CIT.Plateforme.Application.Tests/Enrolement/DepotPieceEnrolementIntegrationTests.cs` : 2 tests (dépôt par le propriétaire, refus pour un non-propriétaire), confirmant `DeposerDocumentCommand` fonctionne sans aucune modification sur un ticket Enrolement.
- [X] T023 [US1] `src/CIT.Plateforme.Application/Enrolement/ListerMesEnrolementsQuery.cs` (C8-09) : TEST RED puis GREEN, `tests/CIT.Plateforme.Application.Tests/Enrolement/ListerMesEnrolementsQueryTests.cs` (3 tests, dont l'isolation anti-IDOR entre deux Transporteurs).
- [X] T024 [US1] `ListerMesEnrolementsQuery` enregistrée dans `DependencyInjection.cs`.

**Checkpoint** : US1 fonctionnelle et testable indépendamment (MVP). ✅ ATTEINT — 274/274 Application.Tests (263 + 11).

---

## Phase 4 : US3 — Instruire un enrôlement (Must, ENR-02, C8-02/C8-03/C8-05/C8-10)

**But** : l'Agent Gate consulte la file, prend en charge, vérifie (messages), et valide un dossier.

*Note d'ordonnancement* : US3 implémentée avant US2 (dépendance fonctionnelle réelle), comme anticipé par plan.md.

### Tests pour US3 (RED)

- [X] T025 [P] [US3] TEST RED — `tests/CIT.Plateforme.Application.Tests/Enrolement/FileAgentGateTests.cs` (fichier dédié, mirroring `FileSupportTests.cs` de F006 plutôt qu'extension littérale d'un fichier `ConsulterFileQueryTests.cs` qui n'existait pas — chaque module teste ses branches génériques dans son propre fichier, patron déjà établi) : 2 tests `ConsulterFileQuery`. (C8-10)
- [X] T026 [P] [US3] TEST RED — même fichier : 2 tests `PrendreEnChargeCommand`. (C8-02)
- [X] T027 [P] [US3] TEST RED — `tests/CIT.Plateforme.Application.Tests/Enrolement/PublierMessageEnrolementTests.cs` (fichier dédié, mirroring `PublierMessageComplaintTests.cs`) : 3 tests `PublierMessageCommand`. (C8-03)
- [X] T028 [P] [US3] TEST RED — `tests/CIT.Plateforme.Application.Tests/Enrolement/ValiderEnrolementCommandTests.cs` : 3 tests (nominal, permission refusée, module incorrect). (C8-05)

### Implémentation US3

- [X] T029 [US3] `ConsulterFileQuery.cs` étendue (3ᵉ test `peutEnrolement`). Suite `FileAgentGateTests` + `CIT.Plateforme.Application.Tests` complet vérifiés verts après extension.
- [X] T030 [US3] `PrendreEnChargeCommand.cs` étendue (branchement 3 voies). Idem.
- [X] T031 [US3] `PublierMessageCommand.cs` étendue (3 voies ×2 méthodes ; `PublierMessageClientAsync` non modifiée). Idem.
- [X] T032 [US3] `src/CIT.Plateforme.Application/Enrolement/ValiderEnrolementCommand.cs` créée (nouvelle, structure `CloturerReclamationCommand`, ne réutilise pas `ActionsWorkflowStandardCommand`).
- [X] T033 [US3] `ValiderEnrolementCommand` enregistrée dans `DependencyInjection.cs`.
- [X] T034 [US3] Vérifié : `CIT.Plateforme.Application.Tests` → **284/284 verts**, 0 warning — non-régression F001/F003/F004/F006 confirmée sur les 3 fichiers génériques étendus.

**Checkpoint** : US1 + US3 fonctionnelles ensemble. ✅ ATTEINT.

---

## Phase 5 : US2 — Suivre et compléter un dossier (Must, ENR-02, C8-04, CLAR-ENR-03)

### Tests pour US2 (RED)

- [X] T035 [P] [US2] TEST RED — `tests/CIT.Plateforme.Application.Tests/Enrolement/AttenteClientEnrolementTests.cs` (fichier dédié, mirroring `AttenteClientComplaintTests.cs`) : `MettreEnAttenteAsync` — 2 tests. (C8-04)
- [X] T036 [P] [US2] TEST — même fichier : `RependreApresReponseAsync` générique fonctionne déjà sans modification (1 test, GREEN direct — confirmation, pas RED, cette méthode n'étant pas branchée).
- [X] T037 [P] [US2] TEST — `tests/CIT.Plateforme.Application.Tests/Enrolement/CycleCompletEnrolementIntegrationTests.cs` : cycle complet réel (SQL Server réel via `ContexteDeTest`, EF InMemory pour la portion Application — cf. note ci-dessous) demande de complément → réponse (message + pièce) → reprise → visible en file. 1 test bout-en-bout couvrant T037.

### Implémentation US2

- [X] T038 [US2] `AttenteClientCommand.cs` étendue (branchement 3 voies sur `MettreEnAttenteAsync`). Suite `AttenteClientEnrolementTests` (7/7) + régression complète vérifiées.
- [X] T039 [US2] Confirmé : `RependreApresReponseAsync`, `PublierMessageClientAsync`, `DeposerDocumentCommand.DeposerPourClientAsync` fonctionnent sans aucune ligne de code modifiée (T036, T037, T022).
- [X] T040 [US2] Vérifié : `CIT.Plateforme.Application.Tests` → **289/289 verts**, 0 warning.
- [X] T041 [P] [US2] TEST — extension de `ListerMesEnrolementsQueryTests.cs` : `Reflete_le_changement_d_etat_En_attente_client` (1 test additionnel).
- [X] T042 [US2] Vérifié : aucun enregistrement DI dupliqué (`AttenteClientCommand`/`PublierMessageCommand`/`DeposerDocumentCommand` déjà enregistrées par F001).

**Checkpoint** : cycle complet demande de complément → réponse → reprise fonctionnel de bout en bout. ✅ ATTEINT.

---

## Phase 6 : US4 — Convoquer et clôturer (Must, ENR-03, C8-07/C8-08, CLAR-ENR-05/06)

### Tests pour US4 (RED)

- [X] T043 [P] [US4] TEST RED — `tests/CIT.Plateforme.Application.Tests/Enrolement/DeclencherConvocationCommandTests.cs` : 3 tests, dont l'assertion négative sur l'absence de canal SMS/WhatsApp/email dans le payload publié. (C8-07)
- [X] T044 [P] [US4] TEST RED — `tests/CIT.Plateforme.Application.Tests/Enrolement/ConstaterRemiseBadgeCommandTests.cs` : 5 tests, dont la double remise de badge (`InvalidOperationException`) et l'absence de toute propriété "Badge" sur `Ticket` (réflexion). (C8-08)

### Implémentation US4

- [X] T045 [US4] `src/CIT.Plateforme.Application/Enrolement/DeclencherConvocationCommand.cs` créée.
- [X] T046 [US4] `src/CIT.Plateforme.Application/Enrolement/ConstaterRemiseBadgeCommand.cs` créée.
- [X] T047 [US4] Les deux commandes enregistrées dans `DependencyInjection.cs`.
- [X] T048 [US4] Vérifié GREEN.
- [X] T049 [US4] Confirmation explicite : grep de contrôle sur `src/CIT.Plateforme.Application/Enrolement/` confirme l'absence de toute classe/méthode "Rejeter" (aucun résultat).

**Checkpoint** : cycle complet ENR-01→ENR-03 fonctionnel de bout en bout. ✅ ATTEINT — 297/297 Application.Tests, 0 warning.

---

## Phase 7 : ENR-04 / ENR-05 — Exigences différées (tracées, non implémentées)

- [X] T050 [P] Confirmé : l'événement `"ConvocationDeclenchee"` (générique, `PublierEvenementAsync`) est structurellement consommable par un futur module Notifications sans modification de F008 — vérifié par assertion négative dans `DeclencherConvocationCommandTests` (T043). Aucun canal implémenté (ENR-04, Should, dépend de PC-16).
- [X] T051 [P] Confirmé : aucune entité "expiration de badge" ni tâche de rappel automatique créée. **CLAR-ENR-07 reste DIFFÉRÉE — jamais résolue par F008** (ENR-05, Could).

**Checkpoint** : ENR-04/ENR-05 explicitement tracées comme différées, aucun Must impacté. ✅ ATTEINT.

---

## Phase 8 : UI (Blazor Server, US1/US2/US3/US4 combinées)

**Contraintes obligatoires — toutes respectées** : zéro GUID manuel (identité toujours dérivée de `IContexteUtilisateurWeb`), zéro `DbContext`/`Repository` dans les `.razor` (uniquement des Commands/Queries Application injectées), permissions vérifiées côté serveur par chaque commande (indépendamment de l'affichage UI), messages d'erreur explicites, classes `cit-*` existantes réutilisées.

**Écart découvert à l'implémentation** (documenté dans `implementation-report.md` §"Écarts") : `ObtenirFicheTicketQuery` (F001/F006) exige un `Dossier.ClientId` résolvable en `SocieteCliente` — toujours faux pour un ticket Enrolement (D1). Une query dédiée `ObtenirFicheEnrolementQuery` a été créée (TEST-FIRST, `tests/CIT.Plateforme.Application.Tests/Enrolement/ObtenirFicheEnrolementQueryTests.cs`, 3 tests) pour `FicheEnrolement.razor`, mirroring la structure de `ObtenirFicheTicketQuery` sans la dépendance société.

### Espace Transporteur/Chauffeur

- [X] T052 [P] [US1] TEST RED (bUnit) — `tests/CIT.Plateforme.Web.Tests/Pages/Enrolement/NouvelEnrolementTests.cs` : 4 tests (accès habilité, non authentifié, sans permission, soumission réelle via `.Input()`/`.Click()`).
- [X] T053 [US1] `src/CIT.Plateforme.Web/Components/Pages/Enrolement/NouvelEnrolement.razor` créée.
- [X] T054 [P] [US1] [US2] TEST RED (bUnit) — `tests/CIT.Plateforme.Web.Tests/Pages/Enrolement/MesEnrolementsTests.cs` : 2 tests (isolation entre comptes, état vide).
- [X] T055 [US1] [US2] `src/CIT.Plateforme.Web/Components/Pages/Enrolement/MesEnrolements.razor` créée.
- [X] T056 [P] [US2] TEST RED (bUnit) — `tests/CIT.Plateforme.Web.Tests/Pages/Enrolement/FicheEnrolementTests.cs` : 4 tests (réponse Transporteur, cloisonnement, actions Agent Gate visibles/masquées selon permission).
- [X] T057 [US2] [US3] [US4] `src/CIT.Plateforme.Web/Components/Pages/Enrolement/FicheEnrolement.razor` créée (actions Transporteur + Agent Gate, toutes revérifiées côté serveur).

### Espace Agent Gate

- [X] T058 [P] [US3] TEST RED (bUnit) — `tests/CIT.Plateforme.Web.Tests/Pages/Enrolement/FileAgentGateTests.cs` : 3 tests (accès habilité, 403, prise en charge réelle avec vérification DB).
- [X] T059 [US3] `src/CIT.Plateforme.Web/Components/Pages/Enrolement/FileAgentGate.razor` créée. Utilise `ConsulterFileAgentGateQuery` (glue UI additionnelle non prévue explicitement par contracts/use-cases.md, mirroring `ConsulterFileReclamationsQuery` de F006 — résout la file "Agent Gate" par nom avant de déléguer au contrôle générique).

### Navigation

- [X] T060 `src/CIT.Plateforme.Web/Components/Layout/NavMenu.razor` : 2 insertions isolées (section "Gestion des Dossiers" → Agent : lien "File Agent Gate" ; section "Administration & Sécurité" → Client : liens "Nouvel enrôlement"/"Mes enrôlements"). Aucune autre ligne du fichier modifiée — refonte UI préexistante préservée.
- [X] T061 [P] TEST (bUnit) — `tests/CIT.Plateforme.Web.Tests/Components/Layout/NavMenuEnrolementTests.cs` (emplacement réel du fichier `NavMenuTests.cs` existant, pas `Layout/` à la racine tests comme indiqué initialement) : 3 tests (Client, Agent, Supervision).

**Checkpoint** : parcours UI complet exécutable. ✅ ATTEINT — 131/131 Web.Tests (116 existants + 15 nouveaux), 0 warning.

---

## Phase 9 : REC-SCE-08 — Scénario Contractuel (phase dédiée)

- [X] T062 TEST E2E (bUnit, pattern `RecSce07CustomerComplaintE2ETests`) — `tests/CIT.Plateforme.Web.Tests/E2E/RecSce08EnrolementTransporteurE2ETests.cs` (nom final, pas `RecSce08EnrolementE2ETests.cs` comme indiqué initialement) : rejoue les 5 étapes REC-SCE-08 via rendu réel de `NouvelEnrolement` → `FileAgentGate` → `FicheEnrolement` (×4, une `BunitContext` distincte par étape — bUnit v2 verrouille sa collection de services dès le premier `Render`, en sus de la protection déjà nécessaire contre le partage de `PlateformeDbContext`/change-tracker EF Core entre les étapes). **Écart supplémentaire découvert et corrigé** : la relecture du statut du ticket entre étapes UI doit passer par une `IServiceScope`/`PlateformeDbContext` fraîche à chaque fois (`RelireTicketAsync`), sinon le change tracker EF Core du `ticketRepo` de portée longue masque silencieusement les écritures faites par les DbContext distincts des étapes UI. Stable sur 3 exécutions consécutives contre SQL Server réel (LocalDB).
- [X] T063 Vérifié GREEN. Traçabilité complète confirmée : étape 1 → `SoumettreEnrolementCommand`/`NouvelEnrolement.razor` ; étape 2 → `AttenteClientCommand`/`FicheEnrolement.razor` (agent + transporteur) ; étape 3 → `ValiderEnrolementCommand`/`FicheEnrolement.razor` ; étape 4 → `DeclencherConvocationCommand`/`FicheEnrolement.razor` ; étape 5 → `ConstaterRemiseBadgeCommand`/`FicheEnrolement.razor`.

**Checkpoint** : REC-SCE-08 COUVERT à 100 % (5/5 étapes) par un test rejouant le parcours UI réel. ✅ ATTEINT.

---

## Phase 10 : Hardening (Sécurité, research.md D11)

**Répartition réelle** (certains cas déjà couverts par les tests des phases précédentes, consolidés/complétés par `EnrolementSecuriteTests.cs`) :

- [X] T064 anti-IDOR — couvert par `ObtenirFicheEnrolementQueryTests.Transporteur_non_proprietaire_est_refuse` et `DepotPieceEnrolementIntegrationTests.Transporteur_non_proprietaire_est_refuse` (phases 3/8).
- [X] T065 permission manquante — couvert par `SoumettreEnrolementCommandTests`, `FileAgentGateTests`, `ValiderEnrolementCommandTests`, `DeclencherConvocationCommandTests`, `ConstaterRemiseBadgeCommandTests` (phases 3/4/6) : vérification serveur systématique démontrée indépendamment de l'UI.
- [X] T066 [P] TEST — `tests/CIT.Plateforme.Application.Tests/Enrolement/EnrolementSecuriteTests.cs` (nouveau) : `Transporteur_NePeutJamaisValiderConvoquerOuConstaterBadge` — accès croisé consolidé sur les 3 commandes Agent Gate.
- [X] T067 utilisateur non authentifié — couvert par `NouvelEnrolementTests.UtilisateurNonAuthentifie_AfficheMessageNonAutorise` (ajouté lors du hardening, gap identifié et comblé).
- [X] T068 manipulation d'identifiant — couvert structurellement (`appelantId` toujours le seul point d'entrée d'identité) + `EnrolementSecuriteTests.SoumettreEnrolementRequete_NeContientAucunChampIdentite`.
- [X] T069 [P] TEST — même fichier : `Transition_PrendreEnCharge_SurTicketDejaEnCours_LeveInvalidOperationException`.
- [X] T070 [P] TEST — même fichier : `DoubleValidation_NeProduitAucunEffetDeBordDangereux` — comportement documenté explicitement (aucune garde "déjà validé", cohérent avec le socle).
- [X] T071 [P] TEST — même fichier : `DoubleRemiseBadge_DeuxiemeAppelRefuse` (reconfirmation consolidée de `ConstaterRemiseBadgeCommandTests`).
- [X] T072 reprise après complément — couvert par `CycleCompletEnrolementIntegrationTests` (phase 5) et l'étape 2 de `RecSce08EnrolementTransporteurE2ETests` (phase 9).
- [X] T073 [P] TEST — même fichier : `AucuneClasseDeRejetDansLAssemblage` (réflexion sur l'assemblage, consolide T049).
- [X] T074 [P] TEST — même fichier : `AucuneReferenceAveDansLeNamespaceEnrolement` (réflexion sur l'assemblage).

**Checkpoint** : tous les cas négatifs de sécurité couverts. ✅ ATTEINT — 306/306 Application.Tests, 134/134 Web.Tests après hardening.

---

## Phase 11 : Non-Régression Finale (Polish & Cross-Cutting)

- [X] T075 `dotnet build` (solution complète) → **0 avertissement, 0 erreur**.
- [X] T076 `dotnet test` (solution complète, PowerShell natif) → **595/595 verts, 0 échec** (Domain 86 + Application 309 + Infrastructure 67 + Web 133) — sans tolérance, `DashboardAgentTests` inclus dans le compte vert.
- [X] T077 `dotnet ef migrations has-pending-model-changes` → *"No changes have been made to the model since the last migration."* Confirmé après implémentation complète — aucun écart, aucune migration créée.
- [X] T078 Scan des dépendances interdites : grep sur `src/CIT.Plateforme.Application/Enrolement/` et `src/CIT.Plateforme.Web/Components/Pages/Enrolement/` pour Billing/Simulation/Assistant/SMS/WhatsApp — 1 seule occurrence trouvée (`DeclencherConvocationCommand.cs`, commentaire XML documentant explicitement l'ABSENCE d'implémentation SMS/WhatsApp/email, conforme CLAR-ENR-06). Aucune logique hors périmètre.
- [X] T079 Rejeu — les suites de régression F001-F006 (`FileSupportTests`, `PublierMessageComplaintTests`, `AttenteClientComplaintTests`, `CloturerReclamationCommandTests`, `RecSce07CustomerComplaintE2ETests`, etc.) font partie intégrante du run complet T076 et sont toutes vertes ; aucune régression sur `PrendreEnChargeCommand`/`AttenteClientCommand`/`PublierMessageCommand`/`ConsulterFileQuery` malgré les branches Enrolement ajoutées.
- [ ] T080 [P] **NON EXÉCUTÉE** — parcours manuel de `quickstart.md` sur l'application démarrée (`dotnet run`). Non réalisée dans cette session (validation automatisée équivalente déjà apportée par `RecSce08EnrolementTransporteurE2ETests`, qui exerce le même chemin UI réel avec rendu Blazor + persistance SQL Server réelle) — recommandée avant mise en production, documentée comme dette mineure dans `implementation-report.md`.

**Checkpoint final** : F001-F006 protégées, F008 (ENR-01/02/03 Must) fonctionnelle de bout en bout, ENR-04/ENR-05 tracées et différées sans blocage. ✅ ATTEINT (hormis T080, non bloquante).

---

## Dependencies & Execution Order

### Dépendances de phase

- **Phase 1 (Preflight)** : aucune dépendance — exécutée.
- **Phase 2 (Fondations)** : dépend de Phase 1 — bloque toutes les User Stories. Exécutée.
- **Phase 3 (US1)** : dépend de Phase 2. Exécutée (MVP).
- **Phase 4 (US3)** : dépend de Phase 2 et de l'existence d'un ticket Enrolement (US1). Exécutée.
- **Phase 5 (US2)** : dépend de Phase 4. Exécutée.
- **Phase 6 (US4)** : dépend de Phase 4. Exécutée.
- **Phase 7 (ENR-04/05 tracées)** : dépend de Phase 6. Exécutée.
- **Phase 8 (UI)** : dépend de Phases 3-6. Exécutée.
- **Phase 9 (REC-SCE-08)** : dépend de Phase 8. Exécutée.
- **Phase 10 (Hardening)** : dépend de Phases 3-6. Exécutée.
- **Phase 11 (Non-régression finale)** : dépend de toutes les phases précédentes. Exécutée (hormis T080).

---

## Matrice de Traçabilité 1 — ENR-01..05

| Exigence | Priorité | Tâches (tests) | Tâches (implémentation) | Contrat C8 | Statut |
|:---|:---:|:---|:---|:---:|:---|
| ENR-01 | Must | T015-T018, T022 | T019-T024 | C8-01, C8-06, C8-09 | **Implémentée** |
| ENR-02 | Must | T025-T028, T035-T037 | T029-T034, T038-T042 | C8-02, C8-03, C8-04, C8-05, C8-10 | **Implémentée** |
| ENR-03 | Must | T043-T044 | T045-T049 | C8-07, C8-08 | **Implémentée** |
| ENR-04 | Should | — | T050 (tracée) | — | **Différée** (canal, dépend PC-16) |
| ENR-05 | Could | — | T051 (tracée) | — | **Différée** (CLAR-ENR-07 non résolue) |

## Matrice de Traçabilité 2 — REC-SCE-08

| Étape | Tâches (implémentation) | Tâches (test) | Statut |
|:---|:---|:---|:---|
| 1. Dossier à distance | T019, T053 | T015-T018, T052, T062 | ✅ |
| 2. Complément | T038, T057 | T035-T037, T056, T062 | ✅ |
| 3. Validation | T032, T057 | T028, T056, T062 | ✅ |
| 4. Convocation photo/badge | T045, T057 | T043, T056, T062 | ✅ |
| 5. Clôture | T046, T057 | T044, T056, T062 | ✅ |

**REC-SCE-08 : COUVERT (5/5 étapes), démontré par un test E2E rejouant le parcours UI réel, stable sur 3 exécutions.**

## Matrice de Traçabilité 3 — CLAR-ENR-01..07

| Clarification | Statut | Tâche / décision |
|:---|:---|:---|
| CLAR-ENR-01 | RÉSOLUE | T004, T013, T019 (Dossier.ClientId = personne physique, aucune SociétéTransporteur créée) |
| CLAR-ENR-02 | RÉSOLUE (paramétrage) | T003-T004 (Objet/Description libres, aucune liste figée) |
| CLAR-ENR-03 | RÉSOLUE | T038-T039 (réutilisation intégrale, aucun second moteur) |
| CLAR-ENR-04 | RÉSOLUE (hors périmètre) | T049, T073 (absence vérifiée mécaniquement) |
| CLAR-ENR-05 | RÉSOLUE | T044, T046 (badge = événement d'historique) |
| CLAR-ENR-06 | RÉSOLUE | T043, T050 (convocation = événement tracé, aucun canal) |
| **CLAR-ENR-07** | **DIFFÉRÉE — non résolue** | T051 (ENR-05/Could uniquement) |

**CLAR-ENR : 6 clarifications résolues (CLAR-ENR-01→06) ; 1 différée (CLAR-ENR-07), jamais présentée comme résolue.**

---

## Écarts découverts à l'implémentation (non prévus par tasks.md initial)

1. **`ObtenirFicheEnrolementQuery`** (nouvelle, Application/Enrolement/) : `ObtenirFicheTicketQuery` (F001/F006) suppose `Dossier.ClientId` résolvable en `SocieteCliente` — faux pour Enrolement (D1). TEST-FIRST, 3 tests.
2. **`ConsulterFileAgentGateQuery`** (nouvelle, Application/Enrolement/) : glue UI nécessaire pour résoudre la file "Agent Gate" par nom avant de déléguer à `ConsulterFileQuery`, mirroring `ConsulterFileReclamationsQuery` (F006). TEST-FIRST, 2 tests.
3. **`RelireTicketAsync` (E2E)** : nécessité de relire l'état du ticket via une `IServiceScope` fraîche entre chaque étape UI du test REC-SCE-08, pour éviter qu'un change tracker EF Core de portée longue ne masque les écritures faites par des DbContext distincts.
4. **`BunitContext` par étape (E2E)** : bUnit v2 verrouille sa collection de services dès le premier `Render` — une instance distincte est nécessaire pour chaque étape UI enchaînée dans un même test, au-delà de la seule protection déjà requise pour le DbContext.

Aucun de ces écarts n'a modifié une règle métier ni introduit de nouvelle exigence — tous sont des ajustements techniques mineurs, documentés et testés.

## Implementation Strategy

Livraison réalisée dans l'ordre : Fondations → US1 (MVP) → US3 (instruction) → US2 (complément) → US4 (convocation/badge) → ENR-04/05 tracées → UI → REC-SCE-08 → Hardening → Non-régression finale. Chaque phase a été vérifiée verte avant la suivante (T005, T010, T020, T034, T040, T048, T063, checkpoints hardening, T076).
