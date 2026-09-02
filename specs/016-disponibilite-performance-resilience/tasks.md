# BACKLOG TECHNIQUE & TÂCHES D'EXÉCUTION — FEATURE 016

## Disponibilité Opérationnelle, Performance Applicative & Résilience Continue

---

## 1. INVARIANTS STRICTS DU PROJET

- **I1** : **Périmètre exclusif F016** : `NFR-03` (Must), `NFR-04` (Must), `NFR-10` (Must).
- **I2** : **Zéro migration EF Core** (0 table SQL, 0 entité Domaine ajoutée).
- **I3** : **Aucune table de métriques SQL créée**.
- **I4** : **Aucune volumétrie arbitraire inventée** (F016 ne certifie pas `NFR-05` / `PC-01`).
- **I5** : **TestServer / WebApplicationFactory = Preuve L2 uniquement** (mécanismes applicatifs).
- **I6** : **`NFR-03` nécessite une preuve L3 réelle $\ge 99{,}00\%$** (rapport archivé).
- **I7** : **`NFR-04` nécessite un benchmark L3 réel $< 3\text{ s}$** sur les 6 surfaces nominatives.
- **I8** : **`NFR-10` nécessite une preuve environnementale L3/L4** d'accessibilité continue.
- **I9** : **HealthCheck $\ne$ SLA** (la présence de `/healthz` ne prouve pas 99% de disponibilité).
- **I10** : **Middleware de latence $\ne$ preuve de performance** (la mesure n'est pas le benchmark).
- **I11** : **Test temporel unitaire simulé $\ne$ preuve 24/7 calendaire**.
- **I12** : **Aucun gain CDC acquis avant la validation formelle de la Closure Review**.
- **I13** : **Approche RED avant GREEN** sur chaque tâche de développement.
- **I14** : **Aucun test historique affaibli ni régression autorisée**.
- **I15** : **Aucun commit avant la fin complète de la Closure Review**.

---

## 2. PHASAGE DU BACKLOG TECHNIQUE (T001 → T029)

### Phase 1 — Baseline, Worktree & Invariants de Surveillance
- [x] **T001** : Vérifier la baseline technique (Build 0 warning, 0 pending model changes, 945 tests verts).
- [x] **T002** : Établir la whitelist des fichiers F016 et isoler les modifications du worktree.

### Phase 2 — NFR-03 : Health Checks & Sondes de Dépendances (L2)
- [x] **T003** [NFR-03][L2] : Écrire le test RED pour la sonde de vivacité `GET /healthz` (200 OK immédiat).
- [x] **T004** [NFR-03][L2] : Implémenter et mapper la sonde `/healthz` dans `Program.cs`.
- [x] **T005** [NFR-03][L2] : Écrire les tests RED pour la sonde de readiness `GET /ready` (Database, Storage, Antivirus).
- [x] **T006** [NFR-03][L2] : Implémenter `DatabaseHealthCheck.cs`, `StorageHealthCheck.cs` et `AntivirusHealthCheck.cs` dans `CIT.Plateforme.Infrastructure`.
- [x] **T007** [NFR-03][L2] : Enregistrer les sondes dans `DependencyInjection.cs` et mapper `/ready` avec formattage JSON structuré.

### Phase 3 — NFR-03 : Calculateur Pur de Disponibilité (L2)
- [x] **T008** [NFR-03][L2] : Écrire les tests unitaires RED `CalculateurDisponibiliteTests.cs` (100%, 99%, <99%, maintenance déduite, cas limite TempsEligible = 0 -> N/A).
- [x] **T009** [NFR-03][L2] : Implémenter `CalculateurDisponibilite.cs` dans `CIT.Plateforme.Application` en s'appuyant sur `CalendrierOuvre` (`NFR-11`).

### Phase 4 — NFR-04 : Instrumentation & Métrologie de Latence HTTP (L2)
- [x] **T010** [NFR-04][L2] : Écrire les tests unitaires RED pour `MetrologiePerformanceMiddleware` (capture durée, en-tête `Server-Timing`, zéro donnée sensible).
- [x] **T011** [NFR-04][L2] : Implémenter `MetrologiePerformanceMiddleware.cs` dans `CIT.Plateforme.Web` et l'enregistrer dans `Program.cs`.

### Phase 5 — NFR-04 : Profil de Benchmark & Suites Automatisées (L2/L3)
- [x] **T012** [NFR-04][L2] : Définir le profil nominal reproductible sur les 6 écrans courants nominatifs.
- [x] **T013** [NFR-04][L2] : Écrire la suite de tests d'intégration automatisés `NfrCert04PerformanceTests.cs` validant le seuil $< 3\text{ s}$ sur les 6 écrans courants.
- [x] **T014** [NFR-04][L3] : Rédiger le script de tirs reproductibles k6 (`evidence/scripts/benchmark-k6.js`) avec URL et authentification paramétrables.

### Phase 6 — NFR-10 : Accessibilité Permanente 24/7 & Dépôts Hors Heures (L2)
- [x] **T015** [NFR-10][L2] : Auditer l'absence de verrous horaires bloquants dans le portail client.
- [x] **T016** [NFR-10][L2] : Écrire les tests d'intégration RED `NfrCert10AccessibiliteTests.cs` (dépôt à 23h00 le dimanche, enregistrement immédiat, horodatage exact, démarrage SLA ouvré).

### Phase 7 — NFR-10 : Gestion du Mode Maintenance Planifiée (L2)
- [x] **T017** [NFR-10][L2] : Écrire les tests RED pour l'état de maintenance et le middleware `MaintenanceMiddleware` (503 Service Unavailable + Retry-After sur pages usager, /healthz = 200, /ready = 503).
- [x] **T018** [NFR-10][L2] : Implémenter `GestionnaireMaintenanceService.cs` et `MaintenanceMiddleware.cs` dans `CIT.Plateforme.Web`.
- [x] **T019** [NFR-10][L2] : Tester la reprise immédiate d'activité dès la désactivation du mode maintenance.

### Phase 8 — Preuves Environnementales & Evidence Gates L3/L4
- [ ] **T020** [NFR-03][L3] : Exécuter la campagne de sondes de disponibilité et archiver le rapport `specs/016-disponibilite-performance-resilience/evidence/availability-report.md` ($\ge 99{,}00\%$) [BLOCKED_ENVIRONMENT].
- [ ] **T021** [NFR-04][L3] : Exécuter la campagne de benchmark k6/HTTP et archiver le rapport `specs/016-disponibilite-performance-resilience/evidence/performance-report.md` ($< 3\text{ s}$) [BLOCKED_ENVIRONMENT].
- [ ] **T022** [NFR-10][L3] : Exécuter la vérification d'accessibilité continue et archiver le rapport `specs/016-disponibilite-performance-resilience/evidence/continuous-access-report.md` [BLOCKED_ENVIRONMENT].

### Phase 9 — Anti-Scope & Intégrité Transverse
- [x] **T023** : Vérifier le respect strict de l'anti-scope (0 code sur NFR-05, NFR-06, NFR-09, SEC-02, SEC-06, ENR-04, IA-01..07).
- [x] **T024** : Vérifier l'intégrité de l'API REST NFR-08, de l'authentification 2FA, des notifications F009 et du chiffrement F015.

### Phase 10 — Documentation, Audits & Clôture F016
- [x] **T025** : Rédiger le rapport d'implémentation `implementation-report.md`.
- [x] **T026** : Exécuter le Regression Gate et rédiger `regression-audit.md`.
- [x] **T027** : Exécuter le Post-Implementation Audit et rédiger `post-implementation-audit.md`.
- [x] **T028** : Exécuter la Closure Review finale et rédiger `closure-review.md`.
- [x] **T029** : Préparer le commit atomique F016 et valider le Commit Gate.

---

## 3. MATRICE DE TRAÇABILITÉ CONTRATS C16 & TEST-SPEC

| Contrat | Exigence CDC | Tâches Associées | Code Produit | Preuve Test L2 | Preuve L3/L4 |
|:---|:---:|:---|:---|:---|:---|
| **`C16-01`** | `NFR-03` | T003, T004 | `Program.cs` | `NfrCert03DisponibiliteTests` | `availability-report.md` |
| **`C16-02`** | `NFR-03` | T005, T006, T007 | `*HealthCheck.cs` | `NfrCert03DisponibiliteTests` | `availability-report.md` |
| **`C16-03`** | `NFR-03` | T008, T009, T020 | `CalculateurDisponibilite.cs` | `CalculateurDisponibiliteTests` | `availability-report.md` |
| **`C16-04`** | `NFR-04` | T010, T011, T013, T021 | `MetrologiePerformanceMiddleware.cs` | `NfrCert04PerformanceTests` | `performance-report.md` |
| **`C16-05`** | `NFR-10` | T015, T016, T017, T018, T022 | `MaintenanceMiddleware.cs` | `NfrCert10AccessibiliteTests` | `continuous-access-report.md` |

---

## 4. GESTION DU GAIN CDC CONDITIONNEL

- **Cas A (Nominal +3)** : T020, T021, T022 validés $\implies$ Gain = **+3** $\implies$ Baseline CDC = **175 / 188 (93,09 %)**.
- **Cas B (Partiel +1)** : Seul T021 validé $\implies$ Gain = **+1** $\implies$ Baseline CDC = **173 / 188 (92,02 %)**.
- **Cas C (Mécanismes +0)** : Tâches L2 seules validées sans preuves L3 $\implies$ Gain = **+0** $\implies$ Baseline CDC = **172 / 188 (91,49 %)**.
