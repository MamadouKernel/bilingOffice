# RAPPORT DE REGRESSION GATE AUDIT — FEATURE 016

## Disponibilité Opérationnelle, Performance Applicative & Résilience Continue

---

### 1. BASELINE TECHNIQUE & FORENSIQUE

- **Build** : SUCCESS (0 erreur, 0 avertissement).
- **EF Core Model** : 0 pending model changes (`No changes have been made to the model since the last migration`).
- **Tests exécutés** : **976 / 976 tests verts (100 %)**
  - Domain : 158 / 158
  - Application : 515 / 515
  - Infrastructure : 94 / 94
  - Web : 209 / 209
- **Régressions constatées** : 0.

---

### 2. NIVEAUX DE PREUVE & VERDICT PAR EXIGENCE

#### NFR-03 (Must) — Disponibilité Cible $\ge 99\%$ en Heures Ouvrées
- **Preuves L2** : **PASS**
  - Sonde vivacité `/healthz` : 200 OK immédiat (processus hôte vivant, aucune dépendance externe).
  - Sonde readiness `/ready` : 200 OK (DB, Stockage GED hermétique, Scanner Antivirus, Mode Maintenance).
  - `CalculateurDisponibilite.cs` : Formule pure exacte validée unitairement avec déduction des maintenances et prise en compte de `CalendrierOuvre` (`NFR-11`).
- **Preuve L3** (`evidence/availability-report.md`) : **NOT EXECUTED** (Attente d'infrastructure d'hébergement déployée).
- **Verdict CDC NFR-03** : **MÉCANISME LIVRÉ / NOT PROVEN** $\implies$ **Gain CDC = +0**.

#### NFR-04 (Must) — Temps de Réponse $< 3\text{ s}$ sur Écrans Courants
- **Preuves L2** : **PASS**
  - Middleware `MetrologiePerformanceMiddleware` : Actif et chronométrant chaque requête HTTP avec en-tête `Server-Timing: total;dur=...`.
  - Script k6 reproductible : `specs/016-disponibilite-performance-resilience/evidence/scripts/benchmark-k6.js` paramétrable.
- **Preuve L3** (`evidence/performance-report.md`) : **NOT EXECUTED** (Attente de tir réel contre URL de recette déployée).
- **Verdict CDC NFR-04** : **MÉCANISME LIVRÉ / NOT PROVEN** $\implies$ **Gain CDC = +0**.

#### NFR-10 (Must) — Accessibilité Permanente 24/7 & Mode Maintenance
- **Preuves L2** : **PASS**
  - Absence de verrou horaire applicatif : Validé (accès et soumission hors heures ouvrées sans restriction).
  - `MaintenanceMiddleware` : Configurable sans recompilation, renvoie `503 Service Unavailable` avec en-tête `Retry-After` sur les pages usager tout en conservant `/healthz` à `200 OK`.
  - Reprise après maintenance : Immédiate et vérifiée.
- **Preuve L3/L4** (`evidence/continuous-access-report.md`) : **NOT EXECUTED** (Attente de suivi continu en production).
- **Verdict CDC NFR-10** : **MÉCANISME LIVRÉ / NOT PROVEN** $\implies$ **Gain CDC = +0**.

---

### 3. STATUT DES CONTRATS & TÂCHES

- **Contrats C16** : 5 / 5 IMPLEMENTED (C16-01 à C16-05) | 0 / 5 ENVIRONMENT-PROVEN.
- **TEST-SPEC** : 3 / 3 L2 PASS (NFR-CERT-03, NFR-CERT-04, NFR-CERT-10) | 0 / 3 L3.
- **Tâches tasks.md** : 22 DONE | 3 BLOCKED_ENVIRONMENT (T020, T021, T022) | 4 AUDITS_PENDING (T026..T029).

---

### 4. PRÉSERVATION DES FEATURES HISTORIQUES & ANTI-SCOPE

- **F015 (Sécurité & Antivirus)** : PRÉSERVÉE (Defender réel, fail-closed préservé, 0 fake antivirus).
- **ERR-01 & Formulaires** : PRÉSERVÉS (Aucune corruption des brouillons).
- **Auth / 2FA / Rôles** : PRÉSERVÉS (100% conformes).
- **API REST NFR-08** : PRÉSERVÉE.
- **F009 (Notifications)** : PRÉSERVÉE.
- **Anti-Scope** : CONFORME (0 code sur NFR-05, NFR-06, NFR-09, SEC-02, SEC-06, ENR-04, IA).

---

### 5. BILAN DU GAIN CDC AU REGRESSION GATE

- **Baseline CDC avant F016** : **172 / 188 (91,49 %)**.
- **Gain certifiable à ce stade** : **+0** (Mécanismes L2 complets, preuves L3/L4 bloquées par environnement).
- **Baseline CDC projetée** : **172 / 188 (91,49 %)**.
- **Projection théorique maximale après campagnes L3** : **175 / 188 (93,09 %)** (NON ACQUISE).
- **Environment Evidence** : **BLOCKED** (Attente d'instance Staging déployée).
- **Verdict Regression Gate** : **READY FOR POST-IMPLEMENTATION AUDIT**.
