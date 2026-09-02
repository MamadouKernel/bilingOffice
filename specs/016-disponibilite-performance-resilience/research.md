# RECHERCHE TECHNIQUE & ARCHITECTURE — FEATURE 016

## Disponibilité Opérationnelle, Performance Applicative & Résilience Continue

---

### 1. ÉTAT DE L'EXISTANT & GAPS ARCHITECTURAUX

- **ASP.NET Core Health Checks** : Absent de `Program.cs`. Les endpoints `/healthz` et `/ready` doivent être enregistrés via `AddHealthChecks()` et mappés via `MapHealthChecks`.
- **Métrologie de latence HTTP** : Aucun middleware global ne chronomètre les requêtes serveur et n'injecte de trace de latence.
- **Gestion des fenêtres de maintenance** : Aucun service n'expose l'état de maintenance planifiée aux sondes de supervision.
- **Réutilisation de `CalendrierOuvre`** : Le calendrier ouvré existe dans le domaine (`src/CIT.Plateforme.Domain/Services/CalendrierOuvre.cs`) et gère déjà les plages d'ouverture, les jours fériés et les week-ends (`NFR-11`). Il sera réutilisé pour calculer la disponibilité en heures ouvrées.

---

### 2. DÉCISIONS D'ARCHITECTURE (RESEARCH DECISIONS)

#### D1 : Architecture des Sondes de Santé (`/healthz` et `/ready`)
- **Endpoint `/healthz` (Liveness)** : Vérifie uniquement la vivacité du processus .NET (réponse `200 OK` immédiate sans solliciter les dépendances).
- **Endpoint `/ready` (Readiness)** : Vérifie les 3 dépendances critiques :
  1. Base de données SQL : `DbContext.Database.CanConnectAsync()`.
  2. Stockage documentaire GED : `IStockageDocuments` (vérification de l'accès au répertoire sécurisé).
  3. Moteur antivirus : `IScannerAntivirus` (vérification de la présence de l'exécutable configuré).
  - En cas d'échec d'une dépendance critique, `/ready` renvoie `503 Service Unavailable` avec le rapport JSON détaillé.

#### D2 : Calcul de Disponibilité Opérationnelle (NFR-03)
- Le service `CalculateurDisponibilite` implémente la formule mathématique rigoureuse :
  $$\text{TempsEligible} = \text{TempsOuvre} - \text{MaintenancePlanifiee}$$
  $$\text{TempsDisponible} = \text{TempsEligible} - \text{IndisponibiliteNonPlanifiee}$$
  $$\text{Disponibilite} = \frac{\text{TempsDisponible}}{\text{TempsEligible}} \ge 99{,}00\%$$
- Réutilisation stricte de `CalendrierOuvre` pour isoler les heures ouvrées.

#### D3 : Métrologie de Performance & Temps de Réponse (NFR-04)
- **Middleware `MetrologiePerformanceMiddleware`** :
  - Interception des requêtes entrantes, démarrage d'un chronomètre haute précision (`Stopwatch.GetTimestamp()`).
  - Fin d'exécution : calcul de la durée en millisecondes, ajout de l'en-tête de diagnostic `Server-Timing: total;dur=<ms>` et journalisation structurée si la durée approche ou dépasse le seuil d'alerte.
  - Zéro enregistrement de données sensibles (BL, booking, credentials masqués).
- **Suite de Benchmarks Automatisés** :
  - Tests d'intégration HTTP exécutant 100+ requêtes séquentielles et concurrentes sur les 6 écrans courants.
  - Assertions strictes : `TempsMoyen < 1000ms`, `TempsP95 < 2000ms`, `TempsMax < 3000ms`.

#### D4 : Continuité de Service 24/7 & Maintenance Planifiée (NFR-10)
- **`GestionnaireMaintenanceService`** :
  - Gestion en mémoire/configuration de l'état de maintenance planifiée (`EstEnMaintenance`, `DateDebut`, `DateFin`, `Message`).
  - Pendant une maintenance planifiée, `/ready` peut signaler le mode maintenance et un bandeau informatif est affiché.
  - Hors maintenance, le portail est ouvert 24/7 ; les dépôts hors heures ouvrées sont enregistrés immédiatement avec accusé de réception et transmission automatique au calcul SLA selon `NFR-11`.

---

### 3. IMPACT SUR LE MODÈLE DE DONNÉES

- **Zéro table SQL**
- **Zéro entité Domaine**
- **Zéro migration EF Core**
