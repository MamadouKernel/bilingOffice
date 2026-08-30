---

description: "Task list template for feature implementation"
---

# Tasks: Comptes clients, sociétés et utilisateurs rattachés

**Input**: Design documents from `/specs/002-comptes-clients-societes/`
**Prerequisites**: `plan.md`, `spec.md` (11 exigences, 6 User Stories, CLAR-01→03), `research.md`
(D1→D12), `data-model.md`, `contracts/use-cases.md` (C2-01→C2-12), `quickstart.md`

> **Cadre** : chaque tâche fonctionnelle porte, lorsque applicable, l'annotation
> `CDC: ... Contract: C2-xx Test: ...`. Chaque tâche de test porte l'étiquette `[TEST-CDC]`
> (dérivée exclusivement de la portion CPT-04 de REC-SCE-14 — seul scénario CDC §22 applicable à
> cette feature), `[TEST-SPEC]` (dérivée d'un critère d'acceptation de `spec.md`) ou `[TEST-TECH]`
> (dérivée d'une décision `research.md`/`plan.md`). **REC-SCE-01→08 et REC-SCE-09→13 n'apparaissent
> dans aucune tâche** (hors périmètre de cette feature — `spec.md` § Testabilité). Aucune valeur
> métier n'est inventée : PC-08 (justificatifs), PC-14 (conservation) et PC-01 (volumétrie) restent
> des paramètres non fixés ; `BLOCKED BY PC-xx` n'est utilisé que si une tâche ne peut réellement
> pas être implémentée de manière configurable — aucun cas de ce type n'a été identifié ici.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Peut s'exécuter en parallèle (fichiers différents, aucune dépendance)
- **[Story]**: User Story concernée (US1→US6, `spec.md`) — absente en Setup/Fondations/Hardening
- Chemins de fichiers exacts inclus dans chaque description

## Path Conventions

Extension de la solution .NET existante (`plan.md` § Project Structure) — aucun nouveau projet :
`src/CIT.Plateforme.{Domain,Application,Infrastructure,Web}/`, `tests/CIT.Plateforme.
{Domain,Application,Infrastructure,Web}.Tests/`.

---

## Phase 1 : Extension minimale de Feature 001 (D11, research.md)

**Objectif** : la seule modification de Feature 001 autorisée par le plan — strictement additive,
sans régression. **Bloque** toutes les phases suivantes (Feature 002 dépend de `Dossier.AuteurId`).

- [X] T001 Ajouter le champ nullable `AuteurId : Guid?` à `Dossier` et un paramètre optionnel
  `Guid? auteurId = null` (dernier paramètre, valeur par défaut) à `Dossier.Creer(...)`, dans
  `src/CIT.Plateforme.Domain/Tickets/Dossier.cs`
  — CDC: (extension technique, cf. CLAR-01) Contract: — Test: T004, T005 [D5, D11]
- [X] T002 Mapper la nouvelle colonne nullable `AuteurId` sur l'entité `Dossier` dans
  `src/CIT.Plateforme.Infrastructure/Persistence/PlateformeDbContext.cs` (bloc
  `builder.Entity<Dossier>`) — CDC: — Contract: — Test: T006 [D5] (depends on T001)
- [X] T003 Créer la migration EF Core additive `AjoutAuteurIdDossier` (une seule colonne nullable,
  aucune donnée existante affectée) — CDC: — Contract: — Test: T006 [D5, D11] (depends on T002)
- [X] T004 [P] [TEST-TECH] Test de non-régression : `Dossier.Creer(numero, clientId, maintenantUtc)`
  (signature à 3 arguments, tous les appels existants de Feature 001) produit toujours un dossier
  avec `AuteurId == null` — dans `tests/CIT.Plateforme.Domain.Tests/Tickets/DossierAuteurTests.cs`
  — Décision: D5/D11 de `research.md` (depends on T001)
- [X] T005 [P] [TEST-TECH] Test : `Dossier.Creer(numero, clientId, maintenantUtc, auteurId)` avec un
  `auteurId` fourni initialise correctement `AuteurId` — dans le même fichier que T004
  — Décision: D5/D11 de `research.md` (depends on T001)
- [X] T006 [TEST-TECH] Exécuter la suite de tests complète de Feature 001 (`dotnet test`) et
  consigner le résultat : 0 régression attendue sur les tests existants après T001→T003
  — Décision: D11 de `research.md` (depends on T002, T003, T004, T005)

**Checkpoint** : `Dossier` étend son schéma sans casser aucun appel ni test de Feature 001 — la
fondation de CLAR-01 est en place.

---

## Phase 2 : Fondations transverses de Feature 002

**Objectif** : entités, ports, permissions et câblage DI communs à toutes les User Stories.
**⚠️ CRITIQUE** : aucune User Story ne peut démarrer avant la fin de cette phase.

- [X] T007 [P] Créer l'enum `TypeClient { Normal, EnCompte, Deposit, IES }` dans
  `src/CIT.Plateforme.Domain/Comptes/TypeClient.cs` — CDC: CPT-02 Contract: — Test: — [D3]
- [X] T008 [P] Créer l'enum `EtatSociete { EnAttenteValidation, Active, Refusee }` dans
  `src/CIT.Plateforme.Domain/Comptes/EtatSociete.cs` — CDC: CPT-01 Contract: — Test: — [D2]
- [X] T009 [P] Créer l'enum `DroitConsultation { PropresDemandesUniquement, ToutesDemandesSociete }`
  dans `src/CIT.Plateforme.Domain/Comptes/DroitConsultation.cs` — CDC: ENT-03 Contract: — Test: — [D4]
- [X] T010 [P] Créer l'exception `DernierAdministrateurActifException` dans
  `src/CIT.Plateforme.Domain/Comptes/DernierAdministrateurActifException.cs`
  — CDC: ENT-02 (CLAR-03) Contract: — Test: — [D6]
- [X] T011 Créer l'entité `SocieteCliente` (agrégat racine) — propriétés, état, profil,
  `NombreAdministrateursActifs`, méthodes `Soumettre`/`Valider`/`Rejeter` (sans encore la garde du
  dernier administrateur, ajoutée en T014) dans
  `src/CIT.Plateforme.Domain/Comptes/SocieteCliente.cs`
  — CDC: CPT-01, CPT-02 Contract: — Test: T013 [D2] (depends on T007, T008)
- [X] T012 [P] Créer l'entité `UtilisateurSociete` (rattachement) — `UtilisateurId`, `SocieteId`,
  `EstAdministrateur`, `DroitConsultation`, `DroitSoumission`, `EstActif` dans
  `src/CIT.Plateforme.Domain/Comptes/UtilisateurSociete.cs`
  — CDC: ENT-01, ENT-02 Contract: — Test: — [D2] (depends on T009)
- [X] T013 [P] [TEST-SPEC] Transitions d'état de `SocieteCliente` : `EnAttenteValidation` →
  `Active` (validation, profil confirmé) ou `Refusee` (rejet, motif obligatoire) ; aucune autre
  transition possible — dans `tests/CIT.Plateforme.Domain.Tests/Comptes/SocieteClienteTests.cs`
  — Exigence CDC: CPT-01, CPT-02 → US1 → critères d'acceptation 2-3 de US1 (FR-001, FR-002) → Test
  (depends on T011)
- [X] T014 [TEST-SPEC écrit en T015, implémenté ici] Implémenter les méthodes
  `SocieteCliente.DesactiverAdministrateur(Guid utilisateurSocieteId)` et
  `SocieteCliente.RetirerRoleAdministrateur(Guid utilisateurSocieteId)`, qui lèvent
  `DernierAdministrateurActifException` si `NombreAdministrateursActifs == 1`, sinon décrémentent le
  compteur — dans `src/CIT.Plateforme.Domain/Comptes/SocieteCliente.cs`
  — CDC: ENT-02 Contract: — Test: T015 [D6] (depends on T010, T011, T015)
- [X] T015 [TEST-SPEC — DOIT ÊTRE ÉCRIT ET ÉCHOUER AVANT T014] Invariant du dernier administrateur
  actif (CLAR-03, règle métier critique) : avec `NombreAdministrateursActifs == 1`,
  `DesactiverAdministrateur`/`RetirerRoleAdministrateur` lève `DernierAdministrateurActifException` ;
  avec `NombreAdministrateursActifs == 2`, l'opération réussit et décrémente le compteur à 1 — dans
  `tests/CIT.Plateforme.Domain.Tests/Comptes/InvariantDernierAdministrateurTests.cs`
  — Exigence CDC: ENT-02 → US3 → critère d'acceptation 6 de US3 (FR-009) → Test — **CLAR-03**
  (depends on T010, T011)
- [X] T016 [P] Créer les ports `ISocieteClienteRepository`, `IUtilisateurSocieteRepository` dans
  `src/CIT.Plateforme.Application/Abstractions/Repositories.cs` (extension additive : nouvelles
  interfaces, aucune interface existante modifiée) — CDC: — Contract: — Test: — [D2, D11]
  (depends on T011, T012)
- [X] T017 [P] Implémenter `SocieteClienteRepository`, `UtilisateurSocieteRepository` (EF Core) dans
  `src/CIT.Plateforme.Infrastructure/Persistence/Repositories.cs`
  — CDC: — Contract: — Test: — [D2] (depends on T016)
- [X] T018 Mapper `SocieteCliente` (clé, `RowVersion` — jeton de concurrence, D6/D7 — index sur
  état) et `UtilisateurSociete` (clé, index unique `UtilisateurId` — un seul rattachement actif à
  la fois, D2 Assumptions ; conversions `HasConversion<string>()` pour les 3 enums) dans
  `src/CIT.Plateforme.Infrastructure/Persistence/PlateformeDbContext.cs`
  — CDC: — Contract: — Test: — [D2, D3, D6] (depends on T011, T012)
- [X] T019 Ajouter `DbSet<SocieteCliente> Societes`, `DbSet<UtilisateurSociete>
  UtilisateursSociete` à `PlateformeDbContext` — CDC: — Contract: — Test: — (depends on T018)
- [X] T020 Créer la migration EF Core additive `AjoutComptesSocietesUtilisateursRattaches` (2
  nouvelles tables, aucune table existante modifiée) — CDC: — Contract: — Test: T072
  (depends on T018, T019)
- [X] T021 [P] Ajouter les nouveaux codes de permission `Permission.CompteValider`,
  `Permission.CompteTypeModifier`, `Permission.CompteGererCollaborateurs` dans la classe `Permission`
  existante (extension additive, aucune constante existante modifiée), dans
  `src/CIT.Plateforme.Domain/Securite/NatureUtilisateur.cs`
  — CDC: — Contract: C2-02, C2-06→09, C2-12 Test: — [D4]
- [X] T022 Enregistrer `ISocieteClienteRepository`/`IUtilisateurSocieteRepository` dans
  `src/CIT.Plateforme.Infrastructure/DependencyInjection.cs` (les commandes/requêtes des User
  Stories suivantes y seront ajoutées au fil de leur implémentation) — CDC: — Contract: — Test: —
  (depends on T017)

**Checkpoint** : le module `Comptes` compile, migre proprement (T001→T003 puis T020), et l'invariant
du dernier administrateur est prouvé au niveau Domain (T015) avant toute implémentation applicative.

---

## Phase 3 : User Story 1 — Création et validation d'un compte société (Priority P1) 🎯 MVP

**Objectif** (`spec.md`) : un représentant de société soumet une demande de compte avec un profil
déclaré ; un administrateur fonctionnel CIT la valide ou la rejette avant toute activation.

**Test indépendant** : soumettre une demande, vérifier son état « en attente », la valider (ou la
rejeter), vérifier l'activation (ou le refus) — sans dépendre d'aucune autre User Story.

- [X] T023 [P] [US1] [TEST-SPEC] Soumission valide → société créée à l'état `EnAttenteValidation`,
  profil déclaré non encore confirmé, non utilisable pour s'authentifier — dans
  `tests/CIT.Plateforme.Application.Tests/Comptes/SoumettreDemandeCompteTests.cs`
  — Exigence CDC: CPT-01, CPT-02 → US1 → critère d'acceptation 1 de US1 (FR-001, FR-002) → Test
- [X] T024 [US1] Implémenter `SoumettreDemandeCompteCommand` dans
  `src/CIT.Plateforme.Application/Comptes/SoumettreDemandeCompteCommand.cs`
  — CDC: CPT-01, CPT-02 Contract: C2-01 Test: T023 (depends on T017, T023)
- [X] T025 [P] [US1] [TEST-SPEC] Validation d'une demande en attente → état `Active`, profil
  confirmé, demandeur devenu premier administrateur (`NombreAdministrateursActifs == 1`) — dans
  `tests/CIT.Plateforme.Application.Tests/Comptes/ValiderDemandeCompteTests.cs`
  — Exigence CDC: CPT-01, CPT-02 → US1 → critère d'acceptation 2 de US1 (FR-001, FR-002) → Test
- [X] T026 [P] [US1] [TEST-SPEC] Rejet d'une demande en attente sans motif → refusé ; rejet avec
  motif obligatoire → état `Refusee`, motif tracé via `IJournalisationService` — dans le même
  fichier que T025 — Exigence CDC: CPT-01 → US1 → critère d'acceptation 3 de US1 (FR-001) → Test
- [X] T027 [P] [US1] [TEST-SPEC] Appelant sans la permission `compte.valider` → `AccesRefuseException`
  (réutilise `IAutorisationService`/`AccesRefuseException` de Feature 001, inchangés) — dans le
  même fichier que T025 — Exigence CDC: CPT-01 → US1 → critère d'acceptation 4 de US1 (FR-001) → Test
- [X] T028 [US1] Implémenter `ValiderOuRejeterDemandeCompteCommand` (vérifie
  `IAutorisationService.ExigerAsync(..., Permission.CompteValider)`, consigne l'audit via
  `IJournalisationService.ConsignerAuditAsync`) dans
  `src/CIT.Plateforme.Application/Comptes/ValiderDemandeCompteCommand.cs`
  — CDC: CPT-01, CPT-02 Contract: C2-02 Test: T025, T026, T027 [D8]
  (depends on T017, T021, T025, T026, T027)
- [X] T029 [P] [US1] [TEST-TECH] **ÉCART SIGNALÉ ET RÉSOLU EN IMPLÉMENTATION (2026-08-29)** :
  cette tâche prévoyait un test de concurrence via EF Core **InMemory**. Or `ContexteDeTest.cs`
  (Feature 001) documente explicitement que le fournisseur InMemory **ne reproduit pas fidèlement
  le verrouillage optimiste transactionnel** — un tel test serait non représentatif (faux positif
  possible). Feature 001 n'a d'ailleurs jamais testé de concurrence avec InMemory : toute preuve de
  concurrence y est réservée à `Infrastructure.Tests` (SQL Server réel). **Résolution** : aucun test
  InMemory ajouté ici — la preuve de concurrence sur la validation d'une demande de compte est
  couverte, sur SQL Server réel, par **T071** (Phase 9), qui couvre exactement ce scénario. Décision
  cohérente avec D6/D7 de `research.md` et le Principe XII (validation réelle, pas cosmétique).
  Aucune règle métier modifiée — ajustement de méthode de test uniquement.
- [X] T030 [US1] Enregistrer `SoumettreDemandeCompteCommand`, `ValiderOuRejeterDemandeCompteCommand`
  dans `DependencyInjection.cs` (depends on T024, T028)
- [X] T031 [P] [US1] Page Blazor Server : formulaire de soumission de demande de compte dans
  `src/CIT.Plateforme.Web/Components/Pages/Comptes/SoumettreDemande.razor`
  — CDC: CPT-01, CPT-02 Contract: C2-01 Test: — (depends on T024)
- [X] T032 [P] [US1] Page Blazor Server : file des demandes en attente pour l'administrateur
  fonctionnel CIT (valider/rejeter, motif) dans
  `src/CIT.Plateforme.Web/Components/Pages/Comptes/ValiderDemandes.razor`
  — CDC: CPT-01, CPT-02 Contract: C2-02 Test: — (depends on T028)

**Checkpoint** : User Story 1 fonctionnelle et testable indépendamment — un compte société peut être
soumis, validé ou rejeté, avec audit et concurrence garantis.

---

## Phase 4 : User Story 2 — Authentification et accès au compte (Priority P1)

**Objectif** (`spec.md`) : le titulaire d'un compte validé s'authentifie ; récupération autonome du
mot de passe en cas d'oubli.

**Test indépendant** : connexion avec compte validé (succès), compte non validé/désactivé (échec),
récupération de mot de passe — sans dépendre d'aucun autre module.

- [X] T033 [P] [US2] [TEST-SPEC] Société `Active` + rattachement `UtilisateurSociete.EstActif`
  → accès autorisé ; société `EnAttenteValidation`/`Refusee`, ou rattachement désactivé → accès
  refusé, message générique ne révélant pas l'état exact — dans
  `tests/CIT.Plateforme.Application.Tests/Comptes/VerifierAccesCompteTests.cs`
  — Exigence CDC: CPT-03 → US2 → critères d'acceptation 1-2 de US2 (FR-003) → Test
- [X] T034 [US2] Implémenter `VerifierAccesCompteQuery` dans
  `src/CIT.Plateforme.Application/Comptes/VerifierAccesCompteQuery.cs`
  — CDC: CPT-03 Contract: C2-03 Test: T033 [D1, D10] (depends on T017, T033)
- [X] T035 [US2] Page Blazor Server `Connexion.razor` : authentification via `SignInManager`
  (Identity, Feature 001, réutilisé tel quel), puis `VerifierAccesCompteQuery` avant d'accorder
  l'accès dans `src/CIT.Plateforme.Web/Components/Pages/Comptes/Connexion.razor`
  — CDC: CPT-03 Contract: C2-03 Test: T033 (depends on T034)
- [X] T036 [P] [US2] [TEST-SPEC] Nouveau mot de passe ne respectant pas la politique de robustesse
  déjà configurée (Feature 001, `AddIdentityCore`) → refus avec message explicite — dans
  `tests/CIT.Plateforme.Web.Tests/Comptes/RecuperationMotDePasseTests.cs`
  — Exigence CDC: CPT-03 → US2 → critère d'acceptation 4 de US2 (FR-003) → Test
- [X] T037 [P] [US2] Pages Blazor Server `MotDePasseOublie.razor` et
  `ReinitialiserMotDePasse.razor` : déclenchement autonome via `UserManager.
  GeneratePasswordResetTokenAsync`/`ResetPasswordAsync` (Identity natif, Feature 001) dans
  `src/CIT.Plateforme.Web/Components/Pages/Comptes/`
  — CDC: CPT-03 Contract: C2-04 Test: T036 (depends on T036)

**Checkpoint** : User Story 2 fonctionnelle et testable indépendamment (nécessite un compte `Active`
issu de US1 pour un test bout-en-bout, mais la règle elle-même est testable isolément).

---

## Phase 5 : User Story 3 — Gestion des utilisateurs rattachés à une société (Priority P2)

**Objectif** (`spec.md`) : l'administrateur d'une société rattache, active/désactive des
collaborateurs, et paramètre leurs droits de consultation et de soumission.

**Test indépendant** : rattacher plusieurs utilisateurs à une société validée (US1), faire varier
leurs droits, vérifier activation/désactivation par l'administrateur — sans dépendre d'un module
métier futur.

- [X] T038 [P] [US3] [TEST-SPEC] Rattachement réussi : nouvel utilisateur associé à la société avec
  droit de consultation initial `PropresDemandesUniquement` (moindre privilège) et droit de
  soumission initial — dans
  `tests/CIT.Plateforme.Application.Tests/Comptes/RattacherUtilisateurTests.cs`
  — Exigence CDC: ENT-01, ENT-02, CPT-05 (renvoi) → US3 → critère d'acceptation 1 de US3 (FR-005,
  FR-008, FR-009) → Test
- [X] T039 [P] [US3] [TEST-SPEC] Un administrateur ne peut rattacher un utilisateur qu'à SA propre
  société (cloisonnement CPT-04) — scénario négatif — dans le même fichier que T038
  — Exigence CDC: ENT-01, CPT-04 → US3 → critère d'acceptation 4 de US3 (FR-008) → Test
- [X] T040 [US3] Implémenter `RattacherUtilisateurCommand` (vérifie `compte.gerer_collaborateurs`
  + appartenance de l'appelant à la société cible) dans
  `src/CIT.Plateforme.Application/Comptes/RattacherUtilisateurCommand.cs`
  — CDC: ENT-01, ENT-02 Contract: C2-06 Test: T038, T039 [D4] (depends on T017, T021, T038, T039)
- [X] T041 [P] [US3] [TEST-SPEC] Modification des droits de consultation/soumission appliquée
  immédiatement — dans
  `tests/CIT.Plateforme.Application.Tests/Comptes/ModifierDroitsUtilisateurTests.cs`
  — Exigence CDC: ENT-02 → US3 → critère d'acceptation 2 de US3 (FR-009) → Test
- [X] T042 [US3] Implémenter `ModifierDroitsUtilisateurCommand` dans
  `src/CIT.Plateforme.Application/Comptes/ModifierDroitsUtilisateurCommand.cs`
  — CDC: ENT-02 Contract: C2-08 Test: T041 (depends on T017, T021, T041)
- [X] T043 [P] [US3] [TEST-SPEC — DOIT ÊTRE ÉCRIT ET ÉCHOUER AVANT T044] Désactivation du dernier
  administrateur actif d'une société → refus explicite (`DernierAdministrateurActifException`,
  T015) ; désactivation d'un administrateur alors qu'un autre reste actif → réussit — dans
  `tests/CIT.Plateforme.Application.Tests/Comptes/DesactivationDernierAdministrateurTests.cs`
  — Exigence CDC: ENT-02 → US3 → critère d'acceptation 6 de US3 (FR-009) → Test — **CLAR-03**
- [X] T044 [US3] Implémenter `ActiverDesactiverUtilisateurCommand` — bascule `EstActif` ; si la
  cible est administratrice, appelle `SocieteCliente.DesactiverAdministrateur(...)` (T014) — dans
  `src/CIT.Plateforme.Application/Comptes/ActiverDesactiverUtilisateurCommand.cs`
  — CDC: ENT-02 Contract: C2-07 Test: T043 [D6, D10] (depends on T014, T017, T021, T043)
- [X] T045 [P] [US3] [TEST-SPEC] Désactivation d'un utilisateur non-administrateur → réussit sans
  condition ; ne peut plus s'authentifier ensuite (via `VerifierAccesCompteQuery`, T034) ; son
  historique n'est jamais supprimé — dans le même fichier que T038
  — Exigence CDC: ENT-02 → US3 → critère d'acceptation 3 de US3 (FR-009) → Test
- [X] T046 [P] [US3] [TEST-SPEC] Appelant non administrateur de la société cible → refus côté
  serveur (moindre privilège) — dans le même fichier que T038
  — Exigence CDC: ENT-02 → US3 → critère d'acceptation 4 de US3 (FR-009) → Test
- [X] T047 [P] [US3] [TEST-SPEC — DOIT ÊTRE ÉCRIT ET ÉCHOUER AVANT T048] Retrait du rôle
  administrateur du dernier administrateur actif → refus (même invariant que T043, y compris
  auto-retrait) ; attribution du rôle à un nouvel utilisateur → réussit sans limite de nombre
  simultané — dans
  `tests/CIT.Plateforme.Application.Tests/Comptes/GererRoleAdministrateurTests.cs`
  — Exigence CDC: ENT-02 → US3 → critère d'acceptation 6 de US3 (FR-009) → Test — **CLAR-03**
- [X] T048 [US3] Implémenter `GererRoleAdministrateurCommand` (attribution/retrait), appelle
  `SocieteCliente.RetirerRoleAdministrateur(...)` (T014) au retrait — dans
  `src/CIT.Plateforme.Application/Comptes/GererRoleAdministrateurCommand.cs`
  — CDC: ENT-02 Contract: C2-09 Test: T047 [D6] (depends on T014, T017, T021, T047)
- [X] T049 [P] [US3] [TEST-SPEC] Chacune des 4 actions de gestion (rattachement T040, activation/
  désactivation T044, modification de droits T042, attribution/retrait de rôle T048) produit une
  entrée vérifiable dans le journal d'audit existant de Feature 001 — dans
  `tests/CIT.Plateforme.Application.Tests/Comptes/AuditGestionCollaborateursTests.cs`
  — Exigence CDC: ENT-04 → US3 → critère d'acceptation 5 de US3 (FR-011) → Test [D8]
  (depends on T040, T042, T044, T048)
- [X] T050 [US3] Enregistrer les 4 commandes US3 dans `DependencyInjection.cs`
  (depends on T040, T042, T044, T048)
- [X] T051 [P] [US3] Page Blazor Server : gestion des collaborateurs (liste, rattacher, activer/
  désactiver, droits, rôle administrateur) dans
  `src/CIT.Plateforme.Web/Components/Pages/Comptes/GestionCollaborateurs.razor`
  — CDC: ENT-01, ENT-02 Contract: C2-06→09 Test: — (depends on T050)

**Checkpoint** : User Story 3 fonctionnelle et testable indépendamment (nécessite une société
`Active` issue de US1) — multi-utilisateurs, droits, garde-fou du dernier administrateur et audit
tous vérifiés.

---

## Phase 6 : User Story 4 — Cloisonnement et visibilité selon le compte et les droits (Priority P2)

**Objectif** (`spec.md`) : un utilisateur ne consulte jamais les données d'une autre société ; au
sein de sa société, sa visibilité dépend exactement du droit accordé.

**Test indépendant** : deux sociétés distinctes, vérifier qu'aucun utilisateur de l'une n'accède aux
données de l'autre, puis faire varier le droit de visibilité au sein d'une même société — données de
test suffisantes, sans dépendre d'un ticket réel.

- [X] T052 [P] [US4] [TEST-SPEC — DOIT ÊTRE ÉCRIT ET ÉCHOUER AVANT T053] Matrice de visibilité
  complète (CLAR-01, CLAR-02) — 5 cas obligatoires : **A** l'auteur d'une demande la voit selon son
  propre droit ; **B** un collègue de même société avec droit `PropresDemandesUniquement` ne voit
  **pas** la demande de A ; **C** un collègue avec droit `ToutesDemandesSociete` voit la demande de
  A ; **D** un administrateur de société voit la demande de A indépendamment de son propre droit
  individuel ; **E** un utilisateur d'une autre société n'y accède jamais — dans
  `tests/CIT.Plateforme.Application.Tests/Comptes/DeterminerVisibiliteDemandesTests.cs`
  — Exigence CDC: ENT-03 → US4 → critères d'acceptation 2, 3, 5 de US4 (FR-010) → Test —
  **CLAR-01, CLAR-02**
- [X] T053 [US4] Implémenter `DeterminerVisibiliteDemandesQuery` selon `research.md` D5 (résout
  société, `EstAdministrateur`, `DroitConsultation` de l'appelant, renvoie le filtre applicable :
  société + auteur si droit restreint, société seule si droit total ou administrateur) dans
  `src/CIT.Plateforme.Application/Comptes/DeterminerVisibiliteDemandesQuery.cs`
  — CDC: ENT-03 Contract: C2-11 Test: T052 [D5] (depends on T017, T052)
- [X] T054 [US4] Vérifier explicitement, dans le même fichier que T053, que
  `CloisonnementService.ExigerProprieteClientAsync` de Feature 001 (INCHANGÉ, `src/CIT.
  Plateforme.Application/Securite/CloisonnementService.cs`) est appelé avec l'identifiant société
  pour garantir le cloisonnement inter-société dur (cas E de T052) — CDC: CPT-04 Contract: C2-11
  Test: T052 [D5, D11] (depends on T053)
- [X] T055 [P] [US4] [TEST-SPEC] Un utilisateur d'une société ne peut pas consulter le profil ni la
  liste des utilisateurs rattachés d'une autre société — dans
  `tests/CIT.Plateforme.Application.Tests/Comptes/ConsulterProfilSocieteTests.cs`
  — Exigence CDC: CPT-04, ENT-01, ENT-03 → US4 → critère d'acceptation 1 de US4 (FR-004) → Test
- [X] T056 [US4] Implémenter `ConsulterProfilSocieteQuery` (profil + utilisateurs rattachés,
  réservé aux membres de la société consultée ou à un administrateur fonctionnel CIT habilité) dans
  `src/CIT.Plateforme.Application/Comptes/ConsulterProfilSocieteQuery.cs`
  — CDC: CPT-04, ENT-01, ENT-03 Contract: C2-10 Test: T055 (depends on T017, T055)
- [X] T057 [P] [US4] [TEST-SPEC] Utilisateur désactivé (société refusée/en attente, ou rattachement
  désactivé) → accès refusé aux données normalement associées — dans le même fichier que T052
  — Exigence CDC: ENT-02 → US4 → critère d'acceptation 4 de US4 → Test
- [X] T058 [US4] [TEST-CDC] Portion CPT-04 de **REC-SCE-14** : « un client ne voit pas les tickets
  d'un autre » — bout-en-bout via `PlateformeWebApplicationFactory` (application réellement
  composée), deux sociétés de test A et B, un utilisateur de A tente de consulter une donnée
  (profil ou demande) de B via l'application → refus systématique, quel que soit le point d'entrée
  — dans `tests/CIT.Plateforme.Web.Tests/Comptes/RecSce14CloisonnementCompteTests.cs`
  — CDC couverts : CPT-04 (portion REC-SCE-14 uniquement — REC-SCE-14 hors cette portion reste hors
  périmètre de cette feature) (depends on T053, T054, T056)
- [X] T059 [US4] Enregistrer `DeterminerVisibiliteDemandesQuery`, `ConsulterProfilSocieteQuery` dans
  `DependencyInjection.cs` (depends on T053, T056)
- [X] T060 [P] [US4] Page Blazor Server : profil société + liste des collaborateurs (lecture) dans
  `src/CIT.Plateforme.Web/Components/Pages/Comptes/ProfilSociete.razor`
  — CDC: CPT-04, ENT-01, ENT-03 Contract: C2-10 Test: — (depends on T059)

**Checkpoint** : User Story 4 fonctionnelle et testable indépendamment — cloisonnement inter-société
et visibilité intra-société entièrement démontrés, y compris la portion applicable de REC-SCE-14.

---

## Phase 7 : User Story 5 — Modification tracée du type de client (Priority P3)

**Objectif** (`spec.md`) : un administrateur fonctionnel CIT modifie le profil déclaré d'une société,
de façon tracée et sans effet rétroactif sur les tickets déjà en cours.

**Test indépendant** : modifier le profil d'une société déjà validée (US1), vérifier la traçabilité
et l'absence d'effet rétroactif déclaré.

- [X] T061 [P] [US5] [TEST-SPEC] Modification appliquée + entrée d'audit avec ancien type, nouveau
  type, auteur, date/heure — dans
  `tests/CIT.Plateforme.Application.Tests/Comptes/ModifierTypeClientTests.cs`
  — Exigence CDC: CPT-06 → US5 → critère d'acceptation 1 de US5 (FR-006) → Test
- [X] T062 [P] [US5] [TEST-SPEC] Un `Dossier` créé avant la modification conserve son `ClientId`
  (société d'attribution) inchangé après la modification — aucun effet rétroactif — dans le même
  fichier que T061 — Exigence CDC: CPT-06 → US5 → critère d'acceptation 2 de US5 (FR-006) → Test
- [X] T063 [P] [US5] [TEST-SPEC] Appelant sans la permission `compte.type_modifier` → refus côté
  serveur — dans le même fichier que T061
  — Exigence CDC: CPT-06 → US5 → critère d'acceptation 3 de US5 (FR-006) → Test
- [X] T064 [US5] Implémenter `ModifierTypeClientCommand` (vérifie `compte.type_modifier`, consigne
  ancien/nouveau type via `IJournalisationService`, ne touche jamais `Dossier.ClientId` des
  dossiers existants) dans
  `src/CIT.Plateforme.Application/Comptes/ModifierTypeClientCommand.cs`
  — CDC: CPT-06 Contract: C2-12 Test: T061, T062, T063 [D3, D7, D8]
  (depends on T017, T021, T061, T062, T063)
- [X] T065 [US5] Enregistrer `ModifierTypeClientCommand` dans `DependencyInjection.cs`
  (depends on T064)
- [X] T066 [P] [US5] Page Blazor Server : modification du type de client (interface CIT) dans
  `src/CIT.Plateforme.Web/Components/Pages/Comptes/ModifierTypeClient.razor`
  — CDC: CPT-06 Contract: C2-12 Test: — (depends on T065)

**Checkpoint** : User Story 5 fonctionnelle et testable indépendamment.

---

## Phase 8 : User Story 6 — Authentification renforcée pour les comptes clients (Priority P4)

**Objectif** (`spec.md`) : un titulaire de compte peut activer un second facteur d'authentification
(Could — capacité optionnelle, jamais imposée).

**Test indépendant** : activer un second facteur sur un compte déjà authentifiable (US2), vérifier
qu'il est requis lors des connexions suivantes.

- [X] T067 [P] [US6] [TEST-SPEC] Après activation, la connexion suivante exige le second facteur —
  dans `tests/CIT.Plateforme.Web.Tests/Comptes/DoubleAuthentificationTests.cs`
  — Exigence CDC: CPT-07 → US6 → critère d'acceptation 1 de US6 (FR-007) → Test
- [X] T068 [P] [US6] [TEST-SPEC] Un compte sans activation explicite n'exige jamais le second
  facteur (Could, jamais imposé par défaut) — dans le même fichier que T067
  — Exigence CDC: CPT-07 → US6 → critère d'acceptation 1 de US6 (FR-007) → Test
- [X] T069 [US6] Page Blazor Server : activation du second facteur via les capacités natives
  Identity (`UserManager.SetTwoFactorEnabledAsync`, `GenerateTwoFactorTokenAsync` — D9, aucune
  dépendance ajoutée) dans
  `src/CIT.Plateforme.Web/Components/Pages/Comptes/ActiverDoubleAuthentification.razor`
  — CDC: CPT-07 Contract: C2-05 Test: T067, T068 [D9] (depends on T035, T067, T068)

**Checkpoint** : les 6 User Stories sont désormais fonctionnelles et indépendamment testables.

---

## Phase 9 (finale) : Hardening / recette

- [X] T070 [TEST-TECH] Test de concurrence **SQL Server réel** (LocalDB, `Infrastructure.Tests`,
  pas InMemory) : société avec exactement 2 administrateurs actifs, deux transactions concurrentes
  (deux `DbContext` distincts) tentent respectivement de désactiver l'un (T044) et de désactiver ou
  rétrograder l'autre (T044/T048) — vérifier qu'**une seule** réussit et que la société conserve
  toujours au moins 1 administrateur actif à l'issue — dans
  `tests/CIT.Plateforme.Infrastructure.Tests/Comptes/ConcurrenceDernierAdministrateurTests.cs`
  — Décision: D6/D7 de `research.md` — **CLAR-03** (depends on T044, T048)
- [X] T071 [TEST-TECH] Test de concurrence SQL Server réel : deux validations concurrentes de la
  même demande de compte (T028) — une seule réussit — dans
  `tests/CIT.Plateforme.Infrastructure.Tests/Comptes/ConcurrenceValidationCompteSqlTests.cs`
  — Décision: D6/D7 de `research.md` (depends on T028)
- [X] T072 Appliquer la migration `AjoutComptesSocietesUtilisateursRattaches` (T020) et exécuter
  intégralement `quickstart.md` (2 sociétés + utilisateurs de test, 5 cas de visibilité A-E,
  désactivation, dernier administrateur, changement de type, portion CPT-04 de REC-SCE-14) —
  consigner le résultat (depends on T020, T038-T069)
- [X] T073 Vérification mécanique de couverture : 11/11 exigences ↔ tâches, 8/8 Must avec au moins
  un test, 12/12 contrats C2-01→C2-12 couverts, CLAR-01/02/03 chacune couverte par au moins un test
  explicite (T015, T043, T047, T052) (depends on T072)
- [X] T074 Vérification mécanique : `git diff`/lecture de `src/CIT.Plateforme.Application/
  Securite/CloisonnementService.cs`, `Application/Securite/IAutorisationService.cs`,
  `Domain/Securite/HabilitationUtilisateur.cs`, `Application/Abstractions/
  IJournalisationService.cs` confirmant **zéro modification de comportement** — seuls des appels
  et des ajouts additifs (D11) (depends on T073)
- [X] T075 Vérification mécanique : absence de toute logique de workflow Billing Office/Proforma/
  Customer Complaint/Enrôlement Transporteurs dans le code de Feature 002 (`TypeClient` exposé,
  jamais exploité pour un routage métier) (depends on T073)
- [X] T076 Exécuter la suite de tests complète (`dotnet test`) — Feature 001 (tous les tests
  existants, aucune régression, cf. T006) + Feature 002 (T004-T005, T013, T015, T023-T029, T033,
  T036, T038-T049, T052, T055, T057-T058, T061-T063, T067-T068, T070-T071) — consigner le résultat
  dans un rapport final (nombre de tests passants par catégorie TEST-CDC/TEST-SPEC/TEST-TECH)
  (depends on T074, T075)

**Checkpoint final** : Feature 002 entièrement implémentée, testée, sans régression sur Feature 001,
prête pour `/speckit.implement`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1** (extension Feature 001) : aucune dépendance — démarre immédiatement, **bloque** tout
  le reste (Feature 002 dépend de `Dossier.AuteurId`).
- **Phase 2** (fondations) : dépend de Phase 1 — **bloque** toutes les User Stories.
- **Phases 3→8** (US1→US6) : dépendent toutes de Phase 2. US1 et US2 (P1) peuvent démarrer en
  parallèle. US3 et US4 (P2) nécessitent une société `Active` (issue d'US1) pour un test
  bout-en-bout, mais leurs règles unitaires (T038-T058) sont testables sans attendre l'UI d'US1.
  US5 (P3) nécessite également une société `Active`. US6 (P4) nécessite l'authentification d'US2
  (T035).
- **Phase 9** (Hardening) : dépend de toutes les phases précédentes.

### Parallel Opportunities

- Toutes les tâches `[P]` de la Phase 2 (T007-T010, T012, T016-T017, T021) peuvent s'exécuter en
  parallèle une fois leurs dépendances immédiates satisfaites.
- US1 et US2 peuvent être développées en parallèle par deux personnes différentes dès la fin de la
  Phase 2 (aucun fichier partagé, aucune dépendance logique entre elles).
- Au sein de chaque User Story, les tâches de test `[P]` (ex. T025-T027, T038-T039, T045-T046)
  peuvent s'écrire en parallèle avant l'implémentation qui les fait passer.

## Implementation Strategy

### MVP (User Story 1 uniquement)

1. Phase 1 (extension Feature 001) → Phase 2 (fondations) → Phase 3 (US1)
2. **STOP et VALIDER** : une société peut être soumise et validée/rejetée de façon autonome.

### Livraison incrémentale

Phase 1+2 → US1 (MVP) → US2 (authentification) → US3+US4 (multi-utilisateurs et visibilité,
cœur métier de cette feature) → US5 (type client) → US6 (2FA, Could) → Phase 9 (Hardening).

---

## Contrôle de couverture *(vérification mécanique — à confirmer en Phase 9, T073)*

| # | Contrôle | Résultat attendu |
|---|---|---|
| 1 | 11/11 exigences couvertes par au moins une tâche | CPT-01→07 (T023-T037, T061-T069), ENT-01→04 (T038-T060) |
| 2 | 8/8 Must avec au moins un test | CPT-01/02 (T023, T025-T027), CPT-03 (T033, T036), CPT-04 (T039, T055, T057-T058), CPT-05 (T038, renvoi ENT), ENT-01 (T038-T039), ENT-02 (T038, T041, T043, T045-T047), ENT-03 (T052, T057) |
| 3 | 12/12 contrats C2-01→C2-12 couverts | C2-01 (T023-T024), C2-02 (T025-T029), C2-03 (T033-T035), C2-04 (T036-T037), C2-05 (T067-T069), C2-06 (T038-T040), C2-07 (T043-T045), C2-08 (T041-T042), C2-09 (T047-T048), C2-10 (T055-T056, T060), C2-11 (T052-T054, T057-T058), C2-12 (T061-T064) |
| 4 | CLAR-01/02/03 testées explicitement | CLAR-01/02 : T052 (matrice A-E) ; CLAR-03 : T015, T043, T047, T070 (concurrence SQL réelle) |
| 5 | REC-SCE-14 limité à sa portion CPT-04 | Seule tâche TEST-CDC : T058 — aucune autre mention de REC-SCE-01→13 dans ce fichier |
| 6 | Aucune exigence Feature 001 dupliquée | T074 (vérification mécanique dédiée) ; D11 de `research.md` |
| 7 | Aucune exigence future absorbée | Aucune tâche Billing Office/Proforma/Complaint/Enrôlement/Notifications — T075 |
| 8 | Aucun contrat orphelin | Voir ligne 3 — les 12 contrats sont tous rattachés à ≥1 tâche |
