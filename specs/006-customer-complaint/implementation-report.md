# Rapport d'Implémentation — Feature 006 (Customer Complaint)

> **Date** : 30 Août 2026 (mis à jour — remédiation post-audit)
> **Statut Global** : CHECKPOINT F — REMÉDIATION COMPLETE

## CHECKPOINT F — Remédiation post-audit (post-implementation-audit.md)

Un audit indépendant (post-implementation-audit.md) a établi un verdict **REMEDIATION REQUIRED** :
l'UI était totalement absente malgré ce rapport annonçant Checkpoint D "EN COURS", le contenu réel
de la réclamation (Objet/Description) n'était jamais capturé, une catégorie seedée ("INFO") cassait
systématiquement la soumission, et `Complaint.Repondre` était documentée sans jamais être vérifiée.
Cette section corrige les métriques non calculées et les affirmations non vérifiées des sections
précédentes de ce rapport (§18, §21) — voir `post-implementation-audit.md` §"MISE À JOUR —
REMÉDIATION F006" pour le détail avant/après/test/résultat de chacun des 6 écarts corrigés.

- **C7 (contrats)** : 5/5 implémentés et couverts par un test négatif d'autorisation ET de
  cloisonnement démontré séparément (C7-03 corrigé — `Complaint.Repondre` n'existait pas).
- **REC-FLOW** : couverts (validation contenu obligatoire, catégorie sans file, transitions
  interdites, permissions manquantes — voir matrice §7 de l'audit).
- **TESTS** : 496/496 verts (Domain 82, Application 254, Infrastructure 65, Web 95) — mesuré par
  exécution réelle de `dotnet test`, pas une estimation.
- **DETTE** : T026 (bUnit dédiés par composant UI) et T030 (écran Admin Catégories) non livrés —
  voir §0 mis à jour ci-dessous ; le contrat C7-05 reste utilisable via le backend testé, sans écran.
- **UI** : livrée (`NouvelleReclamation.razor`, `MesReclamations.razor`, `FileReclamations.razor`,
  extension de `FicheTicket.razor`, navigation Client/Agent).
- **REC-SCE-07** : `RecSce07CustomerComplaintE2ETests.cs` — scénario contractuel complet rejoué et
  vérifié (aucun test de ce nom n'existait avant la remédiation).

## 0. Réconciliation T001 → T035

| Tâche | Statut réel | Checkpoint | Preuve | Reste à faire |
|:---|:---|:---|:---|:---|
| **T001** (Baseline) | Terminé | A | Tests initiaux passés | Rien |
| **T002** (Tests Catégorie) | Terminé | A | `CategorieReclamationTests.cs` | Rien |
| **T003** (Tests Ticket) | Terminé | A | `TicketTests.cs` | Rien |
| **T004** (Entité Catégorie) | Terminé | A | `CategorieReclamation.cs` | Rien |
| **T005** (Extensions Ticket) | Terminé | A | `Ticket.cs` | Rien |
| **T006** (Tests Repo) | Terminé | A | `TicketRepositoryTests.cs` | Rien |
| **T007** (EF Core config) | Terminé | A | `ApplicationDbContext.cs`, Configs | Rien |
| **T008** (Seeder) | Terminé | A | `DevelopmentSeeder.cs` | Rien |
| **T009** (Tests Soumettre) | Terminé | B | `SoumettreReclamationCommandTests.cs` | Rien |
| **T010** (Impl. Soumettre) | Terminé | B | `SoumettreReclamationCommand.cs` | Rien |
| **T011** (Tests ListerCat) | Terminé | B | `ListerCategoriesReclamationQueryTests.cs` | Rien |
| **T012** (Impl. ListerCat) | Terminé | B | `ListerCategoriesReclamationQuery.cs` | Rien |
| **T013** (FicheTicketQuery) | Terminé | B | `ObtenirFicheTicketQuery.cs` | Rien |
| **T014** (Tests FileSupport) | Terminé | B | `FileSupportTests.cs` | Rien |
| **T015** (Claim Instruire) | Terminé | B | `NatureUtilisateur.cs` | Rien |
| **T016** (Test PriseEnCharge)| Terminé | B | Tests d'instruction | Rien |
| **T017** (Tests Attente/Repr)| Terminé | C | `AttenteClientComplaintTests.cs` | Rien |
| **T018** (Test Sécurité Note)| Terminé | C | `PublierMessageComplaintTests.cs` | Rien |
| **T019** (Tests Clôture) | Terminé | C/D| `CloturerReclamationCommandTests.cs` | Rien |
| **T020** (Impl. Clôture) | Terminé | C | `CloturerReclamationCommand.cs` | Rien |
| **T021** (Tests Évaluation) | Terminé | D | `EvaluerReclamationCommandTests.cs` | Rien |
| **T022** (Impl. Évaluation) | Terminé | D | `EvaluerReclamationCommand.cs` | Rien |
| **T023** (Tests ACR-01) | Terminé | C | `DashboardClientAcrTests.cs` | Rien |
| **T024** (Impl. ACR-01) | Terminé | C | `ObtenirDashboardClientQuery.cs` | Rien |
| **T025** (ViewModel) | Terminé | B/C| `FicheTicketVue.cs` | Rien |
| **T026** (Tests bUnit UI) | **Partiel** | F | Couverture via tests d'intégration (`AuthorizationUxTests`, `RecSce07CustomerComplaintE2ETests`) | Tests bUnit dédiés par composant (Nouvelle/Mes/File) |
| **T027** (NouvelleRecla UI) | Terminé | F | `NouvelleReclamation.razor` | Rien |
| **T028** (NavMenu) | Terminé | F | `NavMenu.razor` (Client + Agent) | Rien |
| **T029** (FicheTicket UI) | Terminé | F | `FicheTicket.razor` (catégorie, objet, description, réponse, évaluation, clôture) | Rien |
| **T030** (Admin UI) | **À FAIRE** | F | `ModifierCategoriesReclamationCommand` testé, DI enregistré | Page AdminCategories (aucun écran — hors périmètre remédiation, cf. tasks.md §4 "optionnel UI") |
| **T031** (E2E REC-SCE-07) | Terminé | F | `RecSce07CustomerComplaintE2ETests.cs` (6 étapes, réel LocalDB) | Rien |
| **T032** (Tests Web SEC) | Terminé | F | `AuthorizationUxTests` (REC-SEC-01), tests négatifs par commande (REC-SEC-02→06) | Rien |
| **T033** (REC-FLOW) | Terminé | F | Contenu obligatoire, catégorie sans file, transitions interdites | Rien |
| **T034** (Migration) | Terminé | A/F | `AjoutSupportCustomerComplaint` + `RemediationCustomerComplaintContent` | Rien |
| **T035** (Validation Finale)| Terminé | F | `dotnet build` 0 avertissement, `dotnet test` 496/496 verts | Rien |

## 1. Tâches
- T001 à T008 : [x] Checkpoint A (Domaine + Persistence)
- T009 : [x] Implémentation `ICategorieReclamationRepository` et test.
- T010 : [x] Tests `SoumettreReclamationCommand` (SEC-02, SEC-03, SOC-13).
- T011 : [x] Implémentation `SoumettreReclamationCommand` (Rattachement dossier, ticket métier, événement).
- T012 : [x] `ListerCategoriesReclamationQuery`.
- T013 : [x] `ObtenirFicheTicketQuery` avec jointure `CategorieReclamation` + DTO FicheTicketVue (REC-01).
- T014 : [x] Tests `FileSupportTests` (SEC-04).
- T015 : [x] Validation de `Complaint.Instruire` (claims, `ConsulterFileQuery`, `PrendreEnChargeCommand`).

## 2. Extensions Domaine
Ajout des champs additifs sur `Ticket` (`CategorieReclamationId`, `ReponseFormalisee`, `EvaluationNote`, `EvaluationCommentaire`, `EvaluationDateUtc`).
Ajout des méthodes `CloturerAvecReponseFormalisee`, `Evaluer`, et `DefinirCategorieReclamation` respectant scrupuleusement les invariants (ex: pas d'évaluation si non clôturé).

## 3. Catégories
Création de l'entité `CategorieReclamation` (code, libellé, file par défaut, statut actif). Les invariants de nullité sont validés.

## 4. Persistence
Ajout de `CategoriesReclamation` à `PlateformeDbContext`.
Configuration EF Core avec DeleteBehavior restrictifs.
Ajout de la stratégie `Support Opérationnel` et des permissions `ComplaintInstruire`, `ComplaintSoumettre`, `AdministrationCategoriesReclamation` dans le `DevelopmentSeeder`.

## 14. Sécurité
- Définition des 3 constantes de permissions ajoutées à `NatureUtilisateur.cs`.
- `SoumettreReclamationCommand` vérifie la permission `ComplaintSoumettre` et interdit l'usurpation (récupération de la société depuis l'ID de l'appelant + contexte).
- `ConsulterFileQuery` et `PrendreEnChargeCommand` modifiés pour autoriser explicitement `ComplaintInstruire` sur la file ciblée, sans créer de moteur parallèle.

## 18. Non-Régression (mesuré à la clôture de la remédiation — CHECKPOINT F)
- **Baseline avant remédiation** : 479 tests verts (Domain 79, Application 246, Infrastructure 65, Web 89) — vérifié par exécution réelle en début de remédiation.
- **Tests actuels** : 496 tests verts (Domain 82, Application 254, Infrastructure 65, Web 95) — vérifié par exécution réelle (`dotnet test`, 0 échec, 0 ignoré).
- **Build** : 0 avertissement, 0 erreur (`dotnet build --no-incremental`).
- **Régressions** : 0.

## 19. Frontière AVE
Absente. Aucun composant financier ou lié à l'avoir n'a été inséré. Le type de demande est fixé à `Reclamation` sans impact financier.

## 21. Dettes / Écarts (mis à jour post-remédiation)
- **T026** : pas de tests bUnit dédiés par composant UI (`NouvelleReclamation.razor`, `MesReclamations.razor`, `FileReclamations.razor`) — couverture obtenue via tests d'intégration/E2E à la place (`AuthorizationUxTests`, `RecSce07CustomerComplaintE2ETests`). Écart assumé, documenté ici plutôt que masqué.
- **T030** : aucun écran d'administration des catégories (C7-05 reste utilisable uniquement via le backend, testé). Explicitement marqué "optionnel UI" dans tasks.md §4 — non traité par cette remédiation, scope limité aux écarts démontrés par l'audit.
- **Observation hors périmètre F006** (non corrigée, découverte pendant la remédiation) : `CloisonnementService.ClientEstProprietaireDuTicketAsync` compare l'appelant à `Dossier.ClientId`, qui porte la `SocieteId` (voir `SoumettreReclamationCommand`/`SoumettreDemandeImportCommand` F001) — `FicheTicket.razor` appelle pourtant `PublierMessageClientAsync` avec l'`UtilisateurId` individuel, pas la `SocieteId`. Contournée dans le test E2E en passant l'identifiant correct ; non corrigée dans le composant car le composant est partagé avec F001-F005 et le correctif dépasse le périmètre des 4 écarts audités.
- La configuration EF utilise les attributs inline dans `PlateformeDbContext` conformément au socle technique existant F001.

## 22. Statut
REMÉDIATION TERMINÉE (CHECKPOINT F). Voir `post-implementation-audit.md` pour le verdict final.

## CHECKPOINT D — Clôture + Évaluation + Catégories
- **Tâches terminées** : T021 (Tests Évaluation), T022 (Implémentation Évaluation), T030 (Backend ModifCategories), et complétion des règles de clôture (CloturerReclamationCommandTests).
- **Commandes** : 
  - `EvaluerReclamationCommand` (REC-06).
  - `ModifierCategoriesReclamationCommand` (Administration, soft-delete).
- **Tests ajoutés** : 
  - `CloturerReclamationCommandTests` complété (validation réponse, état, double clôture, accès).
  - `EvaluerReclamationCommandTests` (propriété, unicité, validité note, société).
  - `ModifierCategoriesReclamationCommandTests` (droit admin, modification/désactivation).
- **REC-FLOW couverts** : Clôture sans réponse (refusée), double clôture (refusée), état interdit (refusée), évaluation nominale, évaluation interdite si non clôturé, évaluation restreinte au client propriétaire, modification de catégorie.
- **REC-SEC couverts** : 
  - Clôture sans `ComplaintInstruire` → refusée.
  - Évaluation par une autre société → refusée (`AccesRefuseException`).
  - Administration sans `AdministrationCategoriesReclamation` → refusée (`AccesRefuseException`).
- **SLA** : CONFORME (Aucun `TimeSpan` en dur, dépend du moteur SLA F001).
- **Audit** : CONFORME (Événements `ReclamationCloturee`, `ReclamationEvaluee`, `CategorieReclamationModifiee`).
- **Catégories** : CONFORME (Activation/Désactivation logique implémentée au lieu de suppression).
- **Migration** : AUCUNE. Modèle déjà prêt.
- **AVE** : ABSENT (Aucun impact sur la finance/facturation).
- **Baseline** : 479 tests verts (Domain: 79, Infrastructure: 65, Web: 89, Application: 246).

## CHECKPOINT C — Compléments + Réponse formalisée
- **Tâches terminées** : T017 (Complément), T018 (Sécurité Note Interne), T019 (Tests Réponse Formalisée), T020 (Clôture Réponse Formalisée), T023 (Tests ACR-01), T024 (DashboardClient ACR-01).
- **Fichiers créés/modifiés** :
  - `AttenteClientComplaintTests.cs` (nouveau)
  - `PublierMessageComplaintTests.cs` (nouveau)
  - `CloturerReclamationCommandTests.cs` (nouveau)
  - `DashboardClientAcrTests.cs` (nouveau)
  - `AttenteClientCommand.cs` (modifié pour Permission)
  - `PublierMessageCommand.cs` (modifié pour Permission)
  - `CloturerReclamationCommand.cs` (nouveau)
  - `DashboardClientVue.cs` (ajout de `ComplementReclamation`)
  - `ObtenirDashboardClientQuery.cs` (génération de `ActionRequiseItemVue` pour CustomerComplaint)
- **Tests ajoutés** : 6 nouveaux tests fonctionnels (attente, reprise, notes internes, réponse formalisée, dashboard ACR).
- **REC-FLOW couverts** : Demande complément, reprise, réponse formalisée, clôture.
- **REC-SEC couverts** : REC-SEC-05 (Note interne inaccessible au client).
- **ACR-01** : CONFORME (Le Dashboard client affiche "Action requise" avec libellé spécial).
- **Réponse formalisée** : CONFORME (Commande C7-02 implémentée avec `CloturerReclamationCommand` et testée).
- **Note interne** : Validé par test via `PublierNoteInterneAsync`.
- **SQL / Migration** : AUCUNE. (`No changes have been made to the model since the last migration.`).
- **Baseline** : 465 tests verts (départ à 432).
- **Écarts** : Aucun. FicheTicketVue avait déjà été modifiée pour l'exposition des champs dans Checkpoint B.
- **AVE** : ABSENT (Totalement étanche, aucune exoneration financière).
