# PLAN TECHNIQUE D'IMPLÉMENTATION DURCI — FEATURE 016

## Disponibilité Opérationnelle, Performance Applicative & Résilience Continue

---

### 1. VUE D'ENSEMBLE & NIVEAUX DE PREUVE

Le plan d'implémentation sépare rigoureusement les **mécanismes applicatifs (Preuve L2)** des **campagnes de mesure environnementales (Preuve L3/L4)** nécessaires à la certification contractuelle CDC.

```mermaid
flowchart TD
    subgraph L2 : Mécanismes Applicatifs (.NET Core)
        HC["Health Checks (/healthz, /ready)"]
        CALC["CalculateurDisponibilite (Formule exacte)"]
        PERF["MetrologiePerformanceMiddleware (Server-Timing)"]
        MAINT["MaintenanceMiddleware (503 + Retry-After)"]
    end

    subgraph L3 : Preuves Environnementales (Recette / Staging)
        EVID_DISPO["Campagne Disponibilité L3\n(evidence/availability-report.md)"]
        EVID_PERF["Campagne Performance k6 L3\n(evidence/performance-report.md)"]
        EVID_ACCES["Campagne Accessibilité 24/7 L3\n(evidence/continuous-access-report.md)"]
    end

    L2 --> L3
```

---

### 2. ARCHITECTURE TECHNIQUE PAR EXIGENCE

#### 2.1 NFR-03 (Must) — Disponibilité Cible $\ge 99\%$ en Heures Ouvrées

- **Sonde `/healthz` (Liveness)** : Répond `200 OK` (`{"status":"Healthy"}`) tant que le runtime Kestrel/processus .NET est vivant.
- **Sonde `/ready` (Readiness)** : Inspecte les 3 dépendances critiques :
  1. Base SQL Server : `DbContext.Database.CanConnectAsync()`.
  2. Stockage documentaire GED hermétique : `IStockageDocuments` (vérification de lecture/écriture).
  3. Moteur antivirus local : `IScannerAntivirus` (vérification de la présence de l'exécutable configuré).
  - *Comportement dégradé* : Si une dépendance critique échoue, `/ready` renvoie `503 Service Unavailable`.
- **Formule de Disponibilité Rigoureuse** :
  $$\text{TempsEligible} = \text{TempsOuvre} - \text{MaintenancePlanifiee}$$
  $$\text{IndisponibiliteNonPlanifiee} = \text{Périodes d'indisponibilité intersectant TempsEligible}$$
  $$\text{TempsDisponible} = \text{TempsEligible} - \text{IndisponibiliteNonPlanifiee}$$
  $$\text{Disponibilite} = \frac{\text{TempsDisponible}}{\text{TempsEligible}} \ge 99{,}00\%$$
  - *Cas limite* : Si $\text{TempsEligible} = 0 \implies \text{Résultat N/A}$ (jamais 100% artificiel).
- **Conditions de Clôture NFR-03** :
  - L2 : Tests d'intégration unitaires sur `/healthz`, `/ready` et `CalculateurDisponibilite`.
  - L3 : Exécution d'une campagne de sondes horodatées sur instance déployée avec rapport archivé dans `evidence/availability-report.md` démontrant $\text{Disponibilité} \ge 99{,}00\%$.

#### 2.2 NFR-04 (Must) — Temps de Réponse $< 3\text{ s}$ sur les Écrans Courants

- **Écrans et Endpoints Courants Nominatifs** :
  1. `/comptes/connexion` (Authentification usager/agent)
  2. `/client/dashboard` (Tableau de bord client)
  3. `/client/demandes` (Liste des dossiers et demandes)
  4. `/agent/dashboard` (Tableau de bord agent d'exploitation)
  5. `/tickets/consulter/{id}` (Fiche de consultation d'un dossier/ticket)
  6. `/api/v1/tickets` (Recherche et consultation REST NFR-08)
- **Outil de Benchmark Principal** : **k6** (scripts de tirs reproductibles avec export JSON/CSV) couplé aux tests de charge HTTP .NET.
- **Conditions Normales de Charge** : Profil nominal technique reproductible (F016 ne certifie aucune volumétrie maximale NFR-05 / PC-01).
- **Métrologie & Diagnostic** : `MetrologiePerformanceMiddleware` injectant l'en-tête standard `Server-Timing: total;dur=<ms>`.
- **Conditions de Clôture NFR-04** :
  - L2 : Middleware actif et tests d'intégration.
  - L3 : Rapport de benchmark k6 / HTTP réel archivé dans `evidence/performance-report.md` démontrant que 100% des surfaces retenues répondent en $< 3\text{ s}$ (3 000 ms).

#### 2.3 NFR-10 (Must) — Accessibilité Continue 24/7 & Maintenance Planifiée

- **Accessibilité Permanente** : Aucun verrouillage fonctionnel horaire dans l'application ; les soumissions hors heures ouvrées sont enregistrées immédiatement avec accusé de réception et le délai SLA démarre selon `NFR-11` / `CalendrierOuvre`.
- **Mode Maintenance Planifiée** : `MaintenanceMiddleware` configurable sans recompilation (`appsettings.json` / variable d'environnement).
  - *En maintenance active* : `/healthz` = `200 OK`, `/ready` = `503 Service Unavailable`, requêtes usager = `503 Service Unavailable` avec en-tête `Retry-After` et message informatif transparent.
- **Conditions de Clôture NFR-10** :
  - L2 : Tests de soumission hors heures ouvrées et activation/désactivation de maintenance.
  - L3/L4 : Rapport d'accessibilité continue archivé dans `evidence/continuous-access-report.md`.

---

### 3. DOSSIER DE PREUVES ENVIRONNEMENTALES (`evidence/`)

Le répertoire `specs/016-disponibilite-performance-resilience/evidence/` héberge les 3 rapports réels :
- **`availability-report.md`** : Journal des sondes et calcul de disponibilité $\ge 99\%$.
- **`performance-report.md`** : Résultats du benchmark k6/HTTP montrant les temps d'exécution $< 3\text{ s}$.
- **`continuous-access-report.md`** : Relevé de soumissions 24/7 et gestion de maintenance.

---

### 4. GESTION DU GAIN CDC CONDITIONNEL

- **Cas Nominal (+3)** : Preuves L2 + Rapports L3 démontrés pour NFR-03, NFR-04 et NFR-10 $\implies$ Baseline CDC = **175 / 188 (93,09 %)**.
- **Cas Partiel Performance (+1)** : Seule NFR-04 démontrée en L3 $\implies$ Baseline CDC = **173 / 188 (92,02 %)**.
- **Cas Mécanismes Seuls (+0)** : Preuves L2 démontrées mais campagnes L3 non exécutées $\implies$ Baseline CDC = **172 / 188 (91,49 %)**.

---

### 5. SYNTHÈSE DES COMPOSANTS DE CODE & MIGRATION

- **`src/CIT.Plateforme.Application/Supervision/CalculateurDisponibilite.cs`** : [NEW] Service pur de calcul mathématique.
- **`src/CIT.Plateforme.Application/Supervision/GestionnaireMaintenanceService.cs`** : [NEW] Gestionnaire d'état de maintenance.
- **`src/CIT.Plateforme.Infrastructure/HealthChecks/*HealthCheck.cs`** : [NEW] Sondes SQL, Stockage et Antivirus.
- **`src/CIT.Plateforme.Web/Middlewares/MetrologiePerformanceMiddleware.cs`** : [NEW] Chronométrage des requêtes.
- **`src/CIT.Plateforme.Web/Middlewares/MaintenanceMiddleware.cs`** : [NEW] Gestion des réponses en maintenance.
- **Migrations EF Core** : **0 (0 table SQL, 0 entité)**.
