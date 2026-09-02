# TASKS — FEATURE 013

**Feature** : Feature 013 — Continuité Opérationnelle, Brouillons de Saisie, Exposition d'API & Clôture Démarches  
**Statut** : PLANNED & AUDITED (Ready for Implementation)  
**Date** : 2026-09-01  
**Périmètre** : **ERR-01** (Must), **NFR-08** (Should), **SIM-11** (Could), **ENR-05** (Could)  
**Gain maximal attendu** : **+4 exigences**  
**Projection cible** : **168 / 188 (89,36 %)** *(non acquise avant Closure Review)*  

---

## INVARIANTS TECHNIQUES ET ARCHITECTURAUX

- **I1** : Aucune Feature étrangère abordée ni anticipée.
- **I2** : Aucune exigence hors F013 revendiquée (IA-01..07, SEC-02, SEC-07, ENR-04, NFR-07 restent hors périmètre).
- **I3** : Aucune dépendance externe payante ni connecteur tiers introduit (pas de Twilio, SendGrid, SMTP direct).
- **I4** : Aucune valeur métier ENR-05 en dur (durée de validité et préavis de rappel strictement paramétrables).
- **I5** : Aucune fuite de données inter-tenant sur les brouillons (cloisonnement strict `UtilisateurId` + `SocieteId`, CPT-04).
- **I6** : Aucun flux binaire lourd dans le payload du brouillon (métadonnées seules, pas de Stream/byte[]/base64).
- **I7** : Aucune entité EF Core brute exposée directement via la façade d'API (DTOs publics stricts).
- **I8** : Aucun canal SMS/WhatsApp réel (PC-16 respecté).
- **I9** : Migration de schéma strictement additive (`AjouterBrouillonsSaisie`), 0 DROP, 0 perte de données.
- **I10** : TEST-FIRST strict : chaque tâche de développement produit est précédée par son test RED unitaire ou d'intégration.
- **I11** : Préservation intégrale de la baseline précédente (867 tests verts, 0 warning, 0 pending model changes).
- **I12** : Aucun commit ni push avant la validation formelle de la Closure Review.

---

## TABLE DE CORRESPONDANCE CONTRATS & TRAÇABILITÉ

| Contrat | Exigence CDC | Priorité | Famille TEST-SPEC | Tâches Associées | Code Produit Cible | Preuve Attendue |
|:---|:---:|:---:|:---|:---:|:---|:---|
| **C13-01** | **ERR-01** | **Must** | `ERR-CERT-01` | T006, T007, T008 | `EnregistrerBrouillonCommand.cs` | Sauvegarde atomique du payload JSON sans binaire |
| **C13-02** | **ERR-01** | **Must** | `ERR-CERT-01` | T009, T010 | `ObtenirBrouillonQuery.cs` | Restauration fidèle des champs saisis |
| **C13-03** | **ERR-01** | **Must** | `ERR-CERT-01` | T011, T012 | `SupprimerBrouillonCommand.cs` | Purge sur soumission réussie ou abandon |
| **C13-04** | **NFR-08** | **Should** | `NFR-CERT-08` | T024, T025 | `GET /api/v1/tickets/{numero}` | Code 200/401/403/404 + DTO sécurisé CPT-04 |
| **C13-05** | **NFR-08** | **Should** | `NFR-CERT-08` | T026, T027 | `GET /api/v1/dossiers/{reference}` | DTO dossier sans fuite multi-tenant |
| **C13-06** | **NFR-08** | **Should** | `NFR-CERT-08` | T028, T029 | `POST /api/v1/simulations/calculer` | Réutilisation du moteur F005 sans duplication |
| **C13-07** | **SIM-11** | **Could** | `SIM-CERT-11` | T031, T032, T033 | `ValiderCoherenceSimulationQuery.cs` | 4 règles CDC + messages contextuels |
| **C13-08** | **ENR-05** | **Could** | `ENR-CERT-05` | T036, T037, T038 | `RappelerExpirationBadgesCommand.cs` | Détection échéance via IHorloge + rappel F009 |
| **C13-09** | **ENR-05** | **Could** | `ENR-CERT-05` | T039, T040 | `DemanderRenouvellementBadgeCommand.cs`| Nouveau ticket dans même dossier existant |

---

## BACKLOG TECHNIQUE D'EXÉCUTION (50 TÂCHES ATOMIQUES)

### PHASE 1 — Baseline / Worktree / Invariants
- [x] **T001** : Vérifier la baseline technique initiale (`dotnet build`, `dotnet ef migrations has-pending-model-changes`, tests).
- [x] **T002** : Auditer l'état du worktree (`git status --short`) et figer la whitelist des fichiers F013. Interdiction stricte de `git add .` et `git add -A`.
- [x] **T003** : Valider les invariants d'architecture I1 à I12 et l'isolation des dépendances.

---

### PHASE 2 — ERR-01 : Socle de Persistance des Brouillons (Domaine, Application, Tests unitaires)
- [x] **T004** : Créer l'entité de domaine `BrouillonSaisie` dans `src/CIT.Plateforme.Domain/Formulaires/BrouillonSaisie.cs`.
- [x] **T005** : Définir l'interface `IBrouillonRepository` dans `src/CIT.Plateforme.Application/Abstractions/IBrouillonRepository.cs` (dépend de T004).
- [x] **T006** : Rédiger le test unitaire RED `ErrCert01EnregistrerBrouillonTests` dans `tests/CIT.Plateforme.Application.Tests/Formulaires/ErrCert01BrouillonsTests.cs`.
- [x] **T007** : Implémenter le contrat C13-01 `EnregistrerBrouillonCommand` dans `src/CIT.Plateforme.Application/Formulaires/EnregistrerBrouillonCommand.cs` avec gestion Last-Write-Wins (passage au GREEN).
- [x] **T008** : Rédiger les tests négatifs de cloisonnement multi-tenant (CPT-04 : utilisateur tiers, société tierce, ID manipulé) et d'incompatibilité de type de formulaire.
- [x] **T009** : Rédiger le test unitaire RED pour `ObtenirBrouillonQuery` (contrat C13-02).
- [x] **T010** : Implémenter `ObtenirBrouillonQuery` dans `src/CIT.Plateforme.Application/Formulaires/ObtenirBrouillonQuery.cs` (passage au GREEN).
- [x] **T011** : Rédiger le test unitaire RED pour `SupprimerBrouillonCommand` (contrat C13-03) validant la purge sur soumission réussie et l'abandon manuel.
- [x] **T012** : Implémenter `SupprimerBrouillonCommand` dans `src/CIT.Plateforme.Application/Formulaires/SupprimerBrouillonCommand.cs` (passage au GREEN).

---

### PHASE 3 — ERR-01 : Migration EF Core & Intégration Infrastructure
- [x] **T013** : Configurer le mapping Entity Framework `BrouillonSaisieConfiguration` dans `src/CIT.Plateforme.Infrastructure/Persistence/Configurations/BrouillonSaisieConfiguration.cs`.
- [x] **T014** : Implémenter `BrouillonRepository` dans `src/CIT.Plateforme.Infrastructure/Persistence/Repositories.cs` et enregistrer dans `DependencyInjection.cs`.
- [x] **T015** : Créer la migration strictement additive `AjouterBrouillonsSaisie` via `dotnet ef migrations add`.
- [x] **T016** : Inspecter le code généré `Up()` et `Down()` de la migration pour s'assurer de l'absence de toute opération destructive (0 DROP TABLE, 0 DROP COLUMN).
- [x] **T017** : Vérifier que `dotnet ef migrations has-pending-model-changes` renvoie strictement 0 change.

---

### PHASE 4 — ERR-01 : Intégration aux Formulaires Métier (Web / Blazor)
- [x] **T018** : Rédiger les tests d'intégration Web `ErrCert01WebBrouillonsTests` validant la non-persistance des flux binaires de pièces jointes (métadonnées seules).
- [x] **T019** : Intégrer la gestion de brouillon dans le formulaire Demande Import (`SoumettreDemande.razor`) avec debounce, reprise proactive et purge sur soumission.
- [x] **T020** : Intégrer la gestion de brouillon dans le formulaire Demande Export (`SoumettreDemandeExport.razor`).
- [x] **T021** : Intégrer la gestion de brouillon dans le formulaire Réclamation (`NouvelleReclamation.razor`).
- [x] **T022** : Intégrer la gestion de brouillon dans le formulaire Enrôlement Transporteur (`SoumettreDemande.razor` ou composant d'enrôlement).

---

### PHASE 5 — NFR-08 : Façade d'API Ouverte, Sécurisée et Documentée
- [x] **T023** : Configurer les services API Explorer et Swagger / OpenAPI dans `src/CIT.Plateforme.Web/Program.cs`.
- [x] **T024** : Rédiger le test d'intégration HTTP RED `NfrCert08ApiStatutTicketTests` dans `tests/CIT.Plateforme.Web.Tests/Api/NfrCert08ApiTests.cs`.
- [x] **T025** : Implémenter l'endpoint C13-04 `GET /api/v1/tickets/{numero}` avec DTO public `TicketStatutDto` et contrôles CPT-04 / SEC-03 (passage au GREEN).
- [x] **T026** : Rédiger le test d'intégration HTTP RED pour C13-05 `GET /api/v1/dossiers/{reference}`.
- [x] **T027** : Implémenter l'endpoint C13-05 `GET /api/v1/dossiers/{reference}` avec DTO public `DossierDetailDto` (passage au GREEN).
- [x] **T028** : Rédiger le test d'intégration HTTP RED pour C13-06 `POST /api/v1/simulations/calculer`.
- [x] **T029** : Implémenter l'endpoint C13-06 `POST /api/v1/simulations/calculer` en réutilisant directement le moteur F005 sans duplication (passage au GREEN).
- [x] **T030** : Rédiger le test de conformité OpenAPI validant l'accessibilité et la structure du contrat Swagger (`/swagger/v1/swagger.json`).

---

### PHASE 6 — SIM-11 : Contrôles de Cohérence & Messages d'Aide à la Simulation
- [x] **T031** : Rédiger la suite de tests unitaires RED `SimCert11CoherenceTests` dans `tests/CIT.Plateforme.Application.Tests/Tarification/SimCert11ValidationsTests.cs` couvrant les 4 règles CDC (taille/type, sens/catégorie, dates séjour, Reefer).
- [x] **T032** : Implémenter le contrat C13-07 `ValiderCoherenceSimulationQuery` dans `src/CIT.Plateforme.Application/Tarification/ValiderCoherenceSimulationQuery.cs` (passage au GREEN).
- [x] **T033** : Intégrer les messages d'aide contextuels dans `src/CIT.Plateforme.Web/Components/Pages/Tarification/SimulationProformaPage.razor`.
- [x] **T034** : Valider par test UI que le calcul mathématique reste 100% conforme à l'outil de référence F005.

---

### PHASE 7 — ENR-05 : Cycle de Renouvellement de Badge & Rappels Automatiques F009
- [x] **T035** : Configurer les clés de paramétrage `Enrolement:DureeValiditeBadgeJours` et `Enrolement:DelaiRappelExpirationJours` avec gestion explicite du cas d'absence de configuration.
- [x] **T036** : Rédiger le test unitaire RED `EnrCert05RappelsTests` dans `tests/CIT.Plateforme.Application.Tests/Enrolement/EnrCert05RenouvellementTests.cs` (calcul d'échéance via `IHorloge` et test d'idempotence des rappels).
- [x] **T037** : Implémenter le contrat C13-08 `RappelerExpirationBadgesCommand` émettant l'événement `RappelExpirationBadge` vers F009 (passage au GREEN).
- [x] **T038** : Ajouter les règles de diffusion initiales `RappelExpirationBadge` (InApp / Email) dans `DevelopmentSeederNotifications.cs`.
- [x] **T039** : Rédiger le test unitaire RED pour C13-09 `DemanderRenouvellementBadgeCommand`.
- [x] **T040** : Implémenter `DemanderRenouvellementBadgeCommand` créant le ticket dans le dossier initial avec traçabilité d'audit (passage au GREEN).

---

### PHASE 8 — Intégration Transverse, Sécurité Multi-Tenant & Anti-IDOR
- [x] **T041** : Rédiger les tests négatifs de sécurité transversaux : tentative d'accès API non authentifié (401), tentative cross-tenant (403), falsification d'identifiant brouillon.
- [x] **T042** : Vérifier l'absence totale de logs de données sensibles ou de payloads de brouillons dans les traces applicatives.
- [x] **T043** : Vérifier la concurrence d'accès sur les brouillons (gestion Last-Write-Wins / idempotence des enregistrements).

---

### PHASE 9 — Non-Régression Globale & Vérification Baseline
- [x] **T044** : Exécuter la suite complète de compilation : `dotnet build` (0 avertissement, 0 erreur).
- [x] **T045** : Exécuter l'intégralité des tests unitaires et d'intégration (`dotnet test -- NUnit.NumberOfTestWorkers=1 xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=1`).
- [x] **T046** : Vérifier que `dotnet ef migrations has-pending-model-changes` renvoie strictement 0 change.

---

### PHASE 10 — Anti-Scope Audit & Livrables de Clôture
- [x] **T047** : Exécuter l'audit anti-scope mécanique sur `git diff` (0 IA, 0 SMS réel, 0 NFR-07, 0 SEC-02/07, 0 dérive).
- [x] **T048** : Rédiger le rapport d'implémentation dans `specs/013-continuite-brouillons-api-demarches/implementation-report.md`.
- [x] **T049** : Rédiger l'audit de non-régression dans `specs/013-continuite-brouillons-api-demarches/regression-audit.md`.
- [x] **T050** : Rédiger le rapport d'audit post-implémentation dans `specs/013-continuite-brouillons-api-demarches/post-implementation-audit.md`.
