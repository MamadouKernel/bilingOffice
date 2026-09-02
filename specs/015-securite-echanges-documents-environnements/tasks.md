# BACKLOG TECHNIQUE D'EXÉCUTION — FEATURE 015

**Feature** : Feature 015 — Sécurité des Échanges, Protection Documentaire & Cloisonnement d'Environnement  
**Date** : 2026-09-02  
**Baseline CDC d'entrée** : **169 / 188 (89,89 %)**  
**Périmètre** : **`SEC-04`** (Must), **`SEC-07`** (Must), **`SEC-09`** (Should)  
**Gain visé** : **+3 exigences (2 Must, 1 Should)**  
**Projection cible** : **172 / 188 (91,49 %)** *(non acquise avant Closure Review)*  

---

## 1. INVARIANTS D'ARCHITECTURE & SÉCURITÉ F015

- **I1** : Périmètre restreint strictement à **`SEC-04`**, **`SEC-07`**, et **`SEC-09`**.
- **I2** : **Zéro migration Entity Framework Core** (0 migration).
- **I3** : **Zéro nouvelle table SQL** en base de données.
- **I4** : **Zéro nouvelle entité métier** de domaine.
- **I5** : **Aucun fake / stub antivirus comme preuve de Closure Review**.
- **I6** : **Moteur antivirus réel obligatoire pour certifier `SEC-07`** (Microsoft Defender CLI `MpCmdRun.exe` sur Windows, ou ClamAV `clamscan` sur Linux).
- **I7** : **Moteur absent, erreur système ou timeout = FAIL-CLOSED** (`StatutAnalyseAntivirus.Infecte` / rejet systématique du fichier).
- **I8** : **Aucun fichier non analysé ou infecté n'entre dans le stockage GED final**.
- **I9** : **Aucun secret de production versionné en clair** dans Git ou dans `appsettings.*.json`.
- **I10** : **`SEC-02` (PC-05) et `SEC-06` (PC-14) strictement exclus de F015**.
- **I11** : `TestServer` ne prouve pas la terminaison TLS d'infrastructure de l'hébergeur.
- **I12** : Approche **RED avant GREEN** sur chaque tâche.
- **I13** : Aucun test historique affaibli ni régression autorisée.
- **I14** : Aucun commit avant la validation complète de la Closure Review.

---

## 2. PHASAGE DU BACKLOG TECHNIQUE (T001 → T040)

### Phase 1 — Baseline, Worktree & Invariants de Sécurité
- [x] **T001** : Vérifier la baseline technique (Build 0 warning, 0 pending model changes, suite de tests verte).
  - *Fichier* : Projet racine
  - *Preuve* : `dotnet build` & `dotnet test`
  - *Gate* : Baseline Gate.
- [x] **T002** : Figer le périmètre du worktree et la whitelist des fichiers F015.
  - *Fichier* : `specs/015-securite-echanges-documents-environnements/tasks.md`
  - *Preuve* : `git status --short` audité
  - *Gate* : Worktree Safety Gate.

---

### Phase 2 — Module SEC-04 : Transport Sécurisé & Headers Applicatifs
- [x] **T003** [P] : Créer les tests RED pour la redirection HTTPS et l'en-tête HSTS (`SEC-CERT-04-01`).
  - *Fichier* : `tests/CIT.Plateforme.Web.Tests/Securite/SecCert04TransportTests.cs`
  - *Preuve* : Test échoue avant middleware.
- [x] **T004** [P] : Créer les tests RED pour les en-têtes de sécurité (`nosniff`, `SAMEORIGIN`, `Referrer-Policy`, CSP) (`SEC-CERT-04-02`).
  - *Fichier* : `tests/CIT.Plateforme.Web.Tests/Securite/SecCert04TransportTests.cs`
  - *Preuve* : Test échoue avant middleware.
- [x] **T005** : Implémenter `SecurityHeadersMiddleware` pour injecter les headers de défense en profondeur compatibles Blazor Server.
  - *Fichier* : `src/CIT.Plateforme.Web/Middlewares/SecurityHeadersMiddleware.cs`
  - *Preuve* : Headers HTTP présents sur toutes les réponses.
- [x] **T006** : Activer `UseHttpsRedirection()`, `UseHsts()` et `UseSecurityHeaders()` dans `Program.cs`.
  - *Fichier* : `src/CIT.Plateforme.Web/Program.cs`
  - *Preuve* : `T003` et `T004` passent au vert.
  - *Gate* : Contrat `C15-01` Validé.

---

### Phase 3 — Module SEC-04 : Protection Documentaire & Intégrité GED
- [x] **T007** [P] : Créer les tests RED pour l'isolation physique hors `wwwroot` et le renommage opaque des fichiers (`SEC-CERT-04-03`).
  - *Fichier* : `tests/CIT.Plateforme.Infrastructure.Tests/Documents/StockageSecuriseTests.cs`
  - *Preuve* : Test échoue si fichier accessible publiquement ou non sécurisé.
- [x] **T008** : Renforcer `StockageDocumentsFichiers` avec contrôle anti-path-traversal et isolation étanche hors de la racine web.
  - *Fichier* : `src/CIT.Plateforme.Infrastructure/Documents/StockageDocumentsFichiers.cs`
  - *Preuve* : `T007` passe au vert.
  - *Gate* : Contrat `C15-02` Validé.

---

### Phase 4 — Module SEC-07 : Contrôle d'Admission des Fichiers (Type, Taille, Magic Bytes)
- [x] **T009** [P] : Définir la section de configuration unique pour les extensions et la taille max des fichiers.
  - *Fichier* : `src/CIT.Plateforme.Web/appsettings.json`
  - *Preuve* : Paramètres `Securite:Fichiers:ExtensionsAutorisees` et `TailleMaxOctets` centralisés.
- [x] **T010** [P] : Créer les tests RED pour la validation de type MIME et de taille de fichier (`SEC-CERT-07-01`, `SEC-CERT-07-03`).
  - *Fichier* : `tests/CIT.Plateforme.Application.Tests/Documents/ControleFichiersTests.cs`
  - *Preuve* : Tests échouent sur formats invalides ou dépassements.
- [x] **T011** [P] : Créer les tests RED pour la détection de masquage d'extension via Magic Bytes (`SEC-CERT-07-02`).
  - *Fichier* : `tests/CIT.Plateforme.Application.Tests/Documents/ControleFichiersTests.cs`
  - *Preuve* : Rejet d'un `.exe` maquillé en `.pdf`.
- [x] **T012** : Implémenter le service `ValidateurFichierDocument` vérifiant extensions et signatures Magic Bytes (PDF, PNG, JPEG, XLSX).
  - *Fichier* : `src/CIT.Plateforme.Application/Documents/ValidateurFichierDocument.cs`
  - *Preuve* : `T010` et `T011` passent au vert.
  - *Gate* : Contrat `C15-03` Validé.

---

### Phase 5 — Module SEC-07 : Adaptateur Antivirus & Sécurité Fail-Closed
- [x] **T013** : Créer les tests d'orchestration RED avec Mock pour les statuts `Sain`, `Infecte`, et `ErreurAnalyse`.
  - *Fichier* : `tests/CIT.Plateforme.Application.Tests/Documents/DeposerDocumentAntivirusOrchestrationTests.cs`
  - *Preuve* : Tests d'orchestration validant le comportement applicatif.
- [x] **T014** : Implémenter `ScannerAntivirusDefender` (Adaptateur Microsoft Defender CLI `MpCmdRun.exe`) avec politique stricte Fail-Closed.
  - *Fichier* : `src/CIT.Plateforme.Infrastructure/Documents/ScannerAntivirusDefender.cs`
  - *Preuve* : Code retour 0 -> Sain, Code retour 2 -> Infecté, Erreur -> Infecté/Rejet.
- [x] **T015** : Intégrer l'appel au scanner antivirus dans `DeposerDocumentCommand` avec destruction immédiate du fichier en cas de menace ou d'erreur.
  - *Fichier* : `src/CIT.Plateforme.Application/Documents/DeposerDocumentCommand.cs`
  - *Preuve* : `T013` passe au vert.
  - *Gate* : Contrat `C15-04` Validé.

---

### Phase 6 — Module SEC-07 : Gate de Preuve Moteur Antivirus Réel (EICAR & Clean File)
- [x] **T016** : Créer le test d'intégration exécutant `MpCmdRun.exe` réel sur un fichier sain (`SEC-CERT-07-04`).
  - *Fichier* : `tests/CIT.Plateforme.Infrastructure.Tests/Documents/ScannerAntivirusIntegrationTests.cs`
  - *Preuve* : Vrai processus Defender lancé, code retour 0, statut `Sain`.
- [x] **T017** : Créer le test d'intégration exécutant `MpCmdRun.exe` réel sur la signature standard EICAR (`SEC-CERT-07-05`).
  - *Fichier* : `tests/CIT.Plateforme.Infrastructure.Tests/Documents/ScannerAntivirusIntegrationTests.cs`
  - *Preuve* : Vrai processus Defender lancé, détection de menace (code retour 2), statut `Infecte`.
- [x] **T018** : Créer le test d'intégration validant le comportement Fail-Closed en cas de moteur manquant ou d'erreur (`SEC-CERT-07-06`).
  - *Fichier* : `tests/CIT.Plateforme.Infrastructure.Tests/Documents/ScannerAntivirusIntegrationTests.cs`
  - *Preuve* : Rejet immédiat du fichier sans entrée en GED.
  - *Gate* : **SEC-07 REAL ENGINE CERTIFICATION GATE**.

---

### Phase 7 — Module SEC-09 : Cloisonnement d'Environnements & Secrets
- [x] **T019** [P] : Créer les tests RED pour le bridage de `DevelopmentSeeder` et Swagger en mode Production (`SEC-CERT-09-01`).
  - *Fichier* : `tests/CIT.Plateforme.Web.Tests/Securite/SecCert09EnvironnementTests.cs`
  - *Preuve* : Test échoue si seeder démo actif en prod.
- [x] **T020** [P] : Créer le test statique RED vérifiant l'absence de secrets de production en clair dans les fichiers versionnés (`SEC-CERT-09-02`).
  - *Fichier* : `tests/CIT.Plateforme.Web.Tests/Securite/SecCert09EnvironnementTests.cs`
  - *Preuve* : Scan des `appsettings.*.json` versionnés.
- [x] **T021** : Créer `appsettings.Staging.json` et épurer `appsettings.Production.json` de tout secret en clair.
  - *Fichier* : `src/CIT.Plateforme.Web/appsettings.Staging.json` & `appsettings.Production.json`
  - *Preuve* : Placeholders propres sans secrets.
- [x] **T022** : Configurer `Program.cs` et `DependencyInjection.cs` pour conditionner strictement `DevelopmentSeeder` et Swagger au mode non-Production.
  - *Fichier* : `src/CIT.Plateforme.Web/Program.cs`
  - *Preuve* : `T019` et `T020` passent au vert.
  - *Gate* : Contrat `C15-05` Validé.

---

### Phase 8 — Audit de Sécurité, Logs & Hardening Transverse
- [x] **T023** : Vérifier que les journaux d'audit et logs applicatifs ne consignent aucun contenu binaire ni secret.
  - *Fichier* : `src/CIT.Plateforme.Application/Documents/DeposerDocumentCommand.cs`
  - *Preuve* : Métadonnées uniquement consignées.
- [x] **T024** : Vérifier la non-altération des contrats d'API REST `NFR-08` et des formulaires bilingues `NFR-07`.
  - *Fichier* : `tests/CIT.Plateforme.Web.Tests/Localisation/NfrCert07IntegrationTests.cs`
  - *Preuve* : Tests NFR-07 et NFR-08 100% verts.

---

### Phase 9 — Non-Régression Globale & Vérification de Schéma
- [x] **T025** : Exécuter la suite complète de tests en mode sérialisé (`dotnet test`).
  - *Preuve* : 100% des tests verts (Domain, Application, Infrastructure, Web).
- [x] **T026** : Vérifier l'absence de toute migration Entity Framework Core (`has-pending-model-changes`).
  - *Preuve* : 0 pending model changes.
- [x] **T027** : Vérifier l'absence de warning de compilation (`dotnet build`).
  - *Preuve* : 0 warning, 0 error.

---

### Phase 10 — Documentation Post-Implémentation & Gates de Clôture
- [x] **T028** : Rédiger le rapport d'implémentation `implementation-report.md`.
- [x] **T029** : Exécuter le Regression Gate et rédiger `regression-audit.md`.
- [x] **T030** : Exécuter le Post-Implementation Audit et rédiger `post-implementation-audit.md`.
- [x] **T031** : Exécuter la Closure Review finale et rédiger `closure-review.md`.
- [x] **T032** : Préparer le commit atomique F015 et valider le Commit Gate.

---

## 3. MATRICE DE TRAÇABILITÉ CONTRATS C15

| Contrat | Description | Tâches de Réalisation | Tests Associés | Statut Cible |
|:---|:---|:---:|:---:|:---:|
| **`C15-01`** | Transport HTTPS, HSTS & Security Headers (`SEC-04`) | T003, T004, T005, T006 | `SecCert04TransportTests` | ✅ COUVERT |
| **`C15-02`** | Protection GED hors wwwroot & Intégrité (`SEC-04`) | T007, T008 | `StockageSecuriseTests` | ✅ COUVERT |
| **`C15-03`** | Validation Type, Taille & Magic Bytes (`SEC-07`) | T009, T010, T011, T012 | `ControleFichiersTests` | ✅ COUVERT |
| **`C15-04`** | Adaptateur Defender & Scanner Réel (`SEC-07`) | T013, T014, T015, T016, T017, T018 | `ScannerAntivirusTests`, `ScannerAntivirusIntegrationTests` | ✅ COUVERT |
| **`C15-05`** | Cloisonnement Environnements & Secrets (`SEC-09`) | T019, T020, T021, T022 | `SecCert09EnvironnementTests` | ✅ COUVERT |
