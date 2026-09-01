---
description: "Task list for Feature 012 — Remédiation Dettes Métier, Recette Transverse & Clôture Réserves"
---

# Tasks: Feature 012 — Remédiation Dettes Métier, Recette Transverse & Clôture Réserves

**Input** : `specs/012-remediation-dettes-recette-transverse/{spec.md, plan.md, research.md, data-model.md, contracts/use-cases.md, quickstart.md, checklists/requirements.md}`, `docs/20-preflight-feature-012.md`, CDC original.  
**Nature** : **FEATURE DE REMÉDIATION, CERTIFICATION & RECETTE TRANSVERSE.**  
**Statut** : **EXÉCUTÉ & VALIDÉ.**  
**Régimes** : **R1** Preuve existante / certification · **R2** Tests complémentaires / E2E · **R3** Correction minimale / harnais · **R4** Développement ciblé.

---

## Invariants Opposables à Chaque Tâche

| # | Invariant | Source |
|:--|:---|:---|
| **I1** | **Aucune migration.** Aucune tâche ne doit exécuter `dotnet ef migrations add` | data-model.md · Principe VIII |
| **I2** | **0 entité nouvelle, 0 table nouvelle, 0 colonne nouvelle** | data-model.md §1 |
| **I3** | **REC-02 et REC-03 sont en certification seule (0 code produit)** | research.md D3 |
| **I4** | **SEC-08 porte sur l'assainissement du harnais de test (aucun changement Auth/2FA produit non justifié)** | research.md D4 |
| **I5** | **SOC-14 est strictement cloisonné (CPT-04 / SEC-03) et léger (CSV UTF-8 BOM)** | research.md D5 |
| **I6** | **COM-06 réutilise F009 (aucun fournisseur externe SMS/WhatsApp tant que PC-16 est ouvert)** | research.md D6 |
| **I7** | **REC-SCE-14 apporte +0 gain CDC (scénario transverse)** | spec.md §2 |
| **I8** | **`git add -A` proscrit.** Les modifications préexistantes du worktree ne sont jamais absorbées | plan.md §5 |

---

## Phase 1 — Baseline & Worktree Gate

**Objectif** : Établir la baseline réelle et sceller le périmètre du worktree.

- [x] T001 **Exécuter la baseline technique complète** :
  - `dotnet build` (0 avertissement, 0 erreur)
  - `dotnet test -- NUnit.NumberOfTestWorkers=1 xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=1`
  - `dotnet ef migrations has-pending-model-changes --project src/CIT.Plateforme.Infrastructure --startup-project src/CIT.Plateforme.Web` (0 pending)
- [x] T002 **Vérifier le franchissement du gate** : Baseline conforme à ≥ 858 tests verts, 0 warning, 0 pending model changes.
- [x] T003 **Relever et caractériser l'état du worktree** : `git status --short`. Consigner les fichiers préexistants (`App.razor`, thèmes UI, profils) comme strictement exclus du commit F012.

---

## Phase 2 — Déduction et Tri de Priorité (PRI-02 / US1, R3)

**TEST-SPEC** : `PRI-CERT-01`  
**Objectif** : Attribution automatique de la priorité standard par défaut à la création et ordonnancement prioritaire dans `ConsulterFileQuery`.

- [x] T004 [US1] [TEST-RED] Créer `tests/CIT.Plateforme.Application.Tests/Tickets/PriCert01PrioriteTests.cs` avec les cas :
  - Création d'un ticket sans priorité explicite → assignation du niveau de priorité par défaut configuré (rang = 1 / "Normale").
  - Consultation d'une file contenant des tickets de priorités différentes → ordonnancement par `Priorite.Rang DESC`, puis `DateCreationUtc ASC`.
  - Modification manuelle tracée dans l'audit (PRI-03) et mise à jour de la position dans la file.
- [x] T005 [US1] [CODE] Modifier `src/CIT.Plateforme.Application/Tickets/CreerTicketCommand.cs` pour assigner automatiquement le niveau de priorité par défaut résolu via `IPrioriteRepository` lors de la création d'un ticket.
- [x] T006 [US1] [CODE] Modifier `src/CIT.Plateforme.Application/BillingImport/SoumettreDemandeImportCommand.cs` pour assigner le niveau de priorité par défaut aux tickets créés lors de la soumission Import.
- [x] T007 [US1] [CODE] Modifier `src/CIT.Plateforme.Application/Tickets/ConsulterFileQuery.cs` pour trier les tickets retournés par `Priorite.Rang DESC`, puis `DateCreationUtc ASC`.
- [x] T008 [US1] [TEST-GREEN] Exécuter `PriCert01PrioriteTests.cs` et valider 100% de succès.

---

## Phase 3 — Numéro de Ticket Import par E-mail (IMP-03 / US2, R3)

**TEST-SPEC** : `IMP-CERT-03`  
**Objectif** : Émission de l'événement `TicketCree` lors d'une soumission Import et persistance de la notification/envoi e-mail via F009.

- [x] T009 [US2] [TEST-RED] Créer `tests/CIT.Plateforme.Application.Tests/BillingImport/ImpCert03NotificationEmailTests.cs` :
  - Soumission d'une demande Import avec un BL valide → insertion de `TicketCree` dans `JournalEvenements`.
  - Exécution du moteur de distribution `DistribuerNotificationsUseCase` → création d'une `Notification` client et d'un `EnvoiNotification` avec `Canal = CanalNotification.Email`.
  - Vérification de la présence du numéro de ticket dans le corps/titre du modèle de notification.
- [x] T010 [US2] [CODE] Modifier `src/CIT.Plateforme.Application/BillingImport/SoumettreDemandeImportCommand.cs` :
  - Ajouter après `tickets.AjouterAsync(ticket, ct)` et `uniteDeTravail.EnregistrerAsync(ct)` :
    `await journalisation.PublierEvenementAsync("TicketCree", $"{{\"ticketId\":\"{ticket.Id}\",\"module\":\"{Module}\"}}", ct).ConfigureAwait(false);`
- [x] T011 [US2] [TEST-GREEN] Exécuter `ImpCert03NotificationEmailTests.cs` et valider 100% de succès.
- [x] T012 [US2] [NON-REGRESSION] Exécuter l'ensemble des tests du module `BillingImport` (`CIT.Plateforme.Application.Tests/BillingImport/`).
- [x] T013 [US2] [CONTRÔLE] Vérifier qu'aucun appel direct SMTP ou fournisseur tiers externe n'a été introduit.

---

## Phase 4 — Certification Catégories de Réclamation (REC-02 / US3, R1)

**TEST-SPEC** : `REC-CERT-02`  
**Objectif** : Certification nominative de l'administration dynamique des catégories sans code produit nouveau.

- [x] T014 [US3] [AUDIT] Relire `src/CIT.Plateforme.Domain/Reclamations/CategorieReclamation.cs`, `AdministrationCategories.razor` et `AdministrationCategoriesTests.cs` pour confirmer la couverture intégrale du CRUD et des activations/désactivations.
- [x] T015 [US3] [TEST] Créer `tests/CIT.Plateforme.Application.Tests/CustomerComplaint/RecCert02CategoriesTests.cs` :
  - Création dynamique d'une catégorie sans intervention technique.
  - Modification du libellé et de la file cible par défaut.
  - Activation / désactivation dynamique et vérification du filtrage lors de la soumission client.
  - Contrôle d'habilitation (`Permission.ComplaintCategoriesModifier`).
- [x] T016 [US3] [TEST-GREEN] Exécuter `RecCert02CategoriesTests.cs` et constater 100% vert sans modifier aucun fichier produit.
- [x] T017 [US3] [CONTRÔLE] Vérifier que 0 ligne de code produit n'a été ajoutée pour REC-02.

---

## Phase 5 — Certification Orientation Support Opérationnel (REC-03 / US4, R1)

**TEST-SPEC** : `REC-CERT-03`  
**Objectif** : Certification nominative du routage vers Support Opérationnel et du cycle d'instruction.

- [x] T018 [US4] [AUDIT] Relire `SoumettreReclamationCommand.cs` et constater l'utilisation de `categorie.FileCibleDefautId` (Support Opérationnel par défaut).
- [x] T019 [US4] [TEST] Créer `tests/CIT.Plateforme.Application.Tests/CustomerComplaint/RecCert03OrientationSupportTests.cs` :
  - Soumission d'une réclamation → affectation automatique à la file Support Opérationnel.
  - Prise en charge par l'agent Support et demande de complément documentaire.
  - Réponse client et retour du dossier en traitement Support.
- [x] T020 [US4] [TEST-GREEN] Exécuter `RecCert03OrientationSupportTests.cs` et constater 100% vert.
- [x] T021 [US4] [CONTRÔLE] Vérifier qu'aucun second moteur de routage ni logique hardcodée n'a été introduit.

---

## Phase 6 — Stabilisation Harnais & Certification 2FA (SEC-08 / US5, R3)

**TEST-SPEC** : `SEC-CERT-08`  
**Objectif** : Stabilisation du harnais de test xUnit et certification sur 10 exécutions consécutives à 100% de succès.

- [x] T022 [US5] [HARNAIS] Analyser `ParcoursConnexion2FaTests.cs` :
  - Aligner l'infrastructure de test xUnit avec `[Collection("PlateformeWebCollection")]` et isolation LocalDB (`SqlConnection.ClearAllPools()`).
  - Prober un point d'accès standard sans concurrence layout (`/client/demandes`).
- [x] T023 [US5] [TEST] Valider les 5 cas de `ParcoursConnexion2FaTests.cs` :
  - Mot de passe seul connecte un utilisateur sans 2FA.
  - Mot de passe seul redirige un utilisateur 2FA vers le défi (`/comptes/verifier-code`).
  - Code TOTP valide connecte et donne accès aux pages `[Authorize]`.
  - Code invalide rejeté avec message explicite.
  - Utilisateur désactivé rejeté.
- [x] T024 [US5] [GATE-10-RUNS] Exécuter la suite 2FA **10 fois consécutivement** en boucle et vérifier 10 / 10 PASS (0 échec).
- [x] T025 [US5] [CONTRÔLE] Vérifier qu'aucun code produit Auth/2FA (`ActiverDoubleAuthentification.razor`, `VerifierCode.razor`, middlewares) n'a été altéré.
- [x] T026 [US5] [NON-REGRESSION] Exécuter l'ensemble de la suite `Web.Tests` pour valider l'absence de régression globale.

---

## Phase 7 — Export Tabulaire CSV (SOC-14 / US6, R4)

**TEST-SPEC** : `SOC-CERT-14`  
**Objectif** : Développement d'un export CSV standardisé, encodé en UTF-8 avec BOM, appliquant strictement le cloisonnement multi-tenant.

- [x] T027 [US6] [TEST-RED] Créer `tests/CIT.Plateforme.Application.Tests/Tickets/SocCert14ExportCsvTests.cs` :
  - Génération d'un flux CSV avec séparateur `;`, encodage UTF-8 BOM (`0xEF, 0xBB, 0xBF`), format de date ISO.
  - En-tête : `NumeroTicket;Module;TypeDemande;Statut;FileCourante;DateCreationUtc;Priorite;ReferenceDossier`.
  - **Test négatif de sécurité (CPT-04)** : Un client ne peut exporter QUE les tickets rattachés à sa propre société.
  - **Test de sécurité agent (SEC-03)** : Un agent ne peut exporter QUE les tickets des files autorisées.
- [x] T028 [US6] [CODE] Créer `src/CIT.Plateforme.Application/Tickets/ExporterTicketsCsvQuery.cs` implémentant la logique d'extraction et de formatage CSV dans le respect strict des filtres et autorisations.
- [x] T029 [US6] [CODE] Enregistrer `ExporterTicketsCsvQuery` dans le conteneur d'injection de dépendances (`CIT.Plateforme.Infrastructure/DependencyInjection.cs`).
- [x] T030 [US6] [TEST-GREEN] Exécuter `SocCert14ExportCsvTests.cs` et valider 100% vert.
- [x] T031 [US6] [CONTRÔLE] Vérifier qu'aucun moteur BI, ni dépendance Excel/PDF propriétaire n'a été ajouté.

---

## Phase 8 — Relances Automatiques Multicanales (COM-06 / US7, R3)

**TEST-SPEC** : `COM-CERT-06`  
**Objectif** : Paramétrage multicanal (InApp + Email) des relances automatiques via F009.

- [x] T032 [US7] [TEST-RED] Créer `tests/CIT.Plateforme.Application.Tests/Notifications/ComCert06CanauxRelanceTests.cs` :
  - Émission de l'événement `RelanceEmise` avec règles actives InApp + Email → génération des envois correspondants.
  - Traçage exhaustif dans `Notifications` (InApp) et `EnvoisNotification` (Email).
- [x] T033 [US7] [CODE] Modifier `src/CIT.Plateforme.Infrastructure/Persistence/DevelopmentSeederNotifications.cs` pour configurer les règles de diffusion par défaut pour `RelanceEmise` sur InApp et Email.
- [x] T034 [US7] [TEST-GREEN] Exécuter `ComCert06CanauxRelanceTests.cs` et constater 100% vert.
- [x] T035 [US7] [CONTRÔLE] Vérifier qu'aucun canal SMS/WhatsApp réel n'a été introduit (respect de PC-16).

---

## Phase 9 — Recette Transverse REC-SCE-14 E2E (US8, R2)

**TEST-SPEC** : `REC-SCE-14-E2E`  
**Objectif** : Scénario E2E transverse unifié démontrant les 7 volets contractuels du CDC (Gain CDC = +0).

- [x] T036 [US8] [TEST-E2E] Créer `tests/CIT.Plateforme.Web.Tests/E2E/RecSce14NotificationsE2ETests.cs` intégrant les 7 volets :
  - **Volet 1 (Notifications)** : Soumission client et distribution multicanale InApp + Email.
  - **Volet 2 (Lien sécurisé)** : Tentative d'accès non authentifié bloquée, accès accordé après connexion.
  - **Volet 3 (Recherche guichet)** : Retrouver le dossier au guichet par recherche de numéro.
  - **Volet 4 (Rendez-vous)** : Planification de rendez-vous pour bureau de facturation.
  - **Volet 5 (Dossier)** : Consultation du regroupement multi-tickets sous référence commune.
  - **Volet 6 (Habilitations)** : Vérification des barrières RBAC entre profils agents.
  - **Volet 7 (Journal d'audit)** : Vérification de l'intégrité et de la complétude du journal d'audit.
- [x] T037 [US8] [TEST-GREEN] Exécuter `RecSce14NotificationsE2ETests.cs` sur TestServer réel et valider 100% vert (3 runs consécutifs).
- [x] T038 [US8] [CONTRÔLE] Vérifier que REC-SCE-14 apporte bien 0 gain au décompte des 188 exigences.

---

## Phase 10 — Contrôles Anti-Scope, Non-Régression & Clôture

**Objectif** : Validation intégrale de la non-régression, vérification de l'anti-scope et préparation de la clôture sélective.

- [x] T039 **Contrôle Anti-Scope Négatif** :
  - Vérifier que SEC-02 (RBAC champs IES) n'est pas modifiée.
  - Vérifier que SEC-07 (Antivirus) n'est pas modifiée.
  - Vérifier que ERR-01 (Brouillons) n'est pas modifiée.
  - Vérifier que le module IA n'est pas touché.
  - Vérifier qu'aucune dépendance externe mobile n'est ajoutée.
- [x] T040 **Non-Régression Technique Complète** :
  - `dotnet build` → 0 avertissement, 0 erreur.
  - `dotnet test` → 867/867 tests verts (100% vert sur l'ensemble des 4 projets).
  - `dotnet ef migrations has-pending-model-changes` → 0 pending change (0 migration créée).
- [x] T041 **Génération des livrables de clôture F012** :
  - Rédiger `specs/012-remediation-dettes-recette-transverse/implementation-report.md`.
- [x] T042 **Préparation du Commit Sélectif F012** :
  - Auditer `git status --short` et `git diff --name-only`.
  - Vérifier l'exclusion absolue de tout fichier préexistant étranger à F012.
