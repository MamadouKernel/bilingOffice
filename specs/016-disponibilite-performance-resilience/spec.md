# SPÉCIFICATION FONCTIONNELLE & TECHNIQUE — FEATURE 016

## Disponibilité Opérationnelle, Performance Applicative & Résilience Continue

---

### 1. CONTEXTE & OBJECTIF CONTRACTUEL

La **Feature 016** formalise les garanties contractuelles non-fonctionnelles liées à la disponibilité du service, aux temps de réponse des écrans courants et à l'accessibilité permanente du portail client selon les termes stricts du Cahier des Charges (CDC) :

- **`NFR-03` (Must)** : *« Disponibilité cible en heures ouvrées ≥ 99 % ; maintenance planifiée hors heures de pointe »*
- **`NFR-04` (Must)** : *« Temps de réponse < 3 secondes pour les écrans courants dans des conditions normales de charge »*
- **`NFR-10` (Must)** : *« La plateforme est accessible 24h/24 et 7j/7, hors périodes de maintenance planifiées »*

---

### 2. DÉCISIONS DE CLARIFICATION CONTRACTUELLES & TECHNIQUES (DEC-016-01 à 08)

- **DEC-016-01 (Heures ouvrées)** : Le calcul de disponibilité s'appuie sur le calendrier ouvré (`NFR-11` / `CalendrierOuvre` : 7h30-17h30 ou 7h00-19h00 paramétrable, du lundi au vendredi).
- **DEC-016-02 (Formule de Disponibilité NFR-03)** :
  $$\text{Disponibilité} = \frac{\text{Temps Nominal Ouvré} - \text{Temps d'Indisponibilité}}{\text{Temps Nominal Ouvré} - \text{Maintenance Planifiée}} \ge 99{,}00\%$$
  Les plages de maintenance planifiées sont déduites du dénominateur.
- **DEC-016-03 (Niveaux de Preuve L2 vs L3)** :
  - *Preuve L2 (Mécanisme Applicatif)* : Démontre la présence des sondes `/healthz` et `/ready`, le calcul de disponibilité, l'interception de latence par middleware, et l'accessibilité 24/7.
  - *Preuve L3 (Benchmark Reproductible)* : Démontre que sur une suite de benchmarks automatisés couvrant les 6 écrans courants, le temps d'exécution serveur reste strictement inférieur à 3 000 ms.
- **DEC-016-04 (Composants de Readiness `/ready`)** :
  - Base de données SQL (`CanConnectAsync`).
  - Stockage documentaire GED (`IStockageDocuments`).
  - Moteur antivirus (`IScannerAntivirus`).
- **DEC-016-05 (Écrans courants NFR-04)** :
  - 1. `/comptes/connexion` (Authentification)
  - 2. `/client/dashboard` (Tableau de bord client)
  - 3. `/client/demandes` (Liste des demandes)
  - 4. `/agent/dashboard` (Tableau de bord agent)
  - 5. `/tickets/consulter/{id}` (Consultation d'un dossier)
  - 6. `/api/v1/tickets` (Recherche REST NFR-08)
- **DEC-016-06 (Mesure de temps de réponse)** : Début à la réception de la requête HTTP, fin à l'émission complète de la réponse serveur.
- **DEC-016-07 (Gestion de Maintenance NFR-10)** : État de maintenance configurable affichant un bandeau informatif sans verrouillage horaire arbitraire en exploitation normale.
- **DEC-016-08 (Zéro Migration)** : Aucun impact sur le schéma de la base de données SQL (0 table, 0 entité, 0 migration EF Core).

---

### 3. SÉPARATION DES RESPONSABILITÉS (APPLICATION vs INFRASTRUCTURE)

1. **Responsabilité Applicative (.NET / Blazor / Infrastructure Code)** :
   - Exposition de sondes de santé normalisées (`/healthz` pour la liveness, `/ready` pour la readiness) vérifiant l'état de la base de données et du stockage.
   - Middleware de métrologie de latence HTTP et diagnostic des temps de traitement des requêtes.
   - Mécanisme de détection et de signalement des fenêtres de maintenance planifiée (bannière informative, état de service, non-blocage abusif des dépôts).
   - Optimisation des requêtes et benchmarks de performance reproductibles garantissant le respect du budget de 3 secondes sur les écrans nominaux.
2. **Responsabilité Infrastructure & Exploitation (Hors périmètre de code F016)** :
   - Redondance des serveurs, clustering, alimentation électrique et réseau de l'hébergeur.
   - Outils de supervision APM externe (Prometheus, Grafana, Datadog ou équivalent d'exploitation).

---

### 3. SPÉCIFICATION DÉTAILLÉE PAR EXIGENCE

#### 3.1 NFR-03 (Must) — Disponibilité Cible ≥ 99% en Heures Ouvrées & Maintenance Planifiée

- **Définition contractuelle** : La plateforme doit garantir une disponibilité applicative d'au moins 99% pendant les heures ouvrées (du lundi au vendredi, de 7h00 à 19h00, selon le calendrier ouvré `NFR-11` / `CalendrierOuvre`).
- **Sondes de santé (Health Checks)** :
  - Endpoint `/healthz` : Sonde de vivacité (Liveness) répondant `200 OK` si le processus hôte est opérationnel.
  - Endpoint `/ready` : Sonde d'état opérationnel (Readiness) vérifiant la connectivité à la base de données SQL et l'accès au stockage documentaire.
  - Format de réponse : JSON structuré indiquant l'état (`Healthy`, `Degraded`, `Unhealthy`) et la latence individuelle des composants.
- **Calcul de disponibilité** :
  $$\text{Disponibilité} = \frac{\text{Temps Disponible en Heures Ouvrées}}{\text{Temps Total Ouvré} - \text{Maintenance Planifiée}} \ge 99\%$$
- **Plages de maintenance planifiée** : Déclarables de manière non intrusive pour l'exploitation sans altérer les calculs de SLA métier.

#### 3.2 NFR-04 (Must) — Temps de Réponse < 3 Secondes sur les Écrans Courants

- **Définition contractuelle** : Le temps de réponse complet (requête, exécution serveur et rendu initial) des écrans courants de l'application doit être inférieur à 3 secondes dans des conditions normales de charge.
- **Périmètre des écrans et endpoints courants** :
  1. `/comptes/connexion` (Page d'authentification)
  2. `/client/dashboard` (Tableau de bord usager)
  3. `/client/demandes` (Liste des demandes client)
  4. `/agent/dashboard` (Tableau de bord agent d'exploitation)
  5. `/tickets/consulter/{id}` (Consultation d'un dossier / ticket)
  6. `/api/v1/tickets` (Recherche et consultation REST NFR-08)
- **Métrologie de latence** :
  - Middleware de mesure de performance enregistrant le temps d'exécution serveur de chaque requête.
  - En-tête de diagnostic optionnel `Server-Timing` ou log structuré de latence pour le monitoring de production.
  - Suite de tests de benchmarks automatisés validant que 95%+ des exécutions se situent très largement sous le plafond de 3 000 ms.

#### 3.3 NFR-10 (Must) — Accessibilité 24h/24 et 7j/7 du Portail Client

- **Définition contractuelle** : Le portail client ne comporte aucun verrouillage horaire artificiel et reste accessible 24/7 pour la soumission de dossiers, le suivi de statut et le dépôt de réclamations.
- **Interaction avec NFR-11** : Les dépôts effectués hors heures ouvrées sont enregistrés immédiatement avec accusé de réception ; le délai de traitement contractuel (SLA) de la file démarre dès la reprise des heures ouvrées.
- **Gestion du mode maintenance** : En cas de maintenance planifiée déclarée, l'interface affiche une information transparente pour l'usager sans perte de données.

---

### 4. USER STORIES & CRITÈRES D'ACCEPTATION

#### US-NFR-01 : Supervision et Sondes de Santé (NFR-03)
- **En tant qu'** administrateur technique ou système de supervision,
- **Je veux** interroger des endpoints `/healthz` et `/ready`,
- **Afin de** monitorer en temps réel la disponibilité du portail CIT et diagnostiquer immédiatement une défaillance de la base ou du stockage.
- **Critères Given/When/Then** :
  - *Given* la plateforme en cours d'exécution nominale,
  - *When* une sonde GET `/healthz` ou `/ready` est reçue,
  - *Then* la réponse renvoie HTTP 200 avec le statut `Healthy`.

#### US-NFR-02 : Temps de Réponse Garanti < 3s (NFR-04)
- **En tant qu'** usager ou agent portuaire,
- **Je veux** naviguer et soumettre mes demandes avec un temps de réponse inférieur à 3 secondes,
- **Afin de** réaliser mes formalités avec fluidité et efficacité.
- **Critères Given/When/Then** :
  - *Given* un utilisateur authentifié sur un parcours courant,
  - *When* il charge son tableau de bord ou consulte un ticket,
  - *Then* le temps d'exécution serveur reste strictement inférieur à 3 000 ms.

#### US-NFR-03 : Accessibilité Permanente 24/7 (NFR-10)
- **En tant que** client portuaire,
- **Je veux** soumettre une demande d'import/export à 23h00 le dimanche,
- **Afin que** mon dossier soit enregistré et pris en charge dès l'ouverture des services.
- **Critères Given/When/Then** :
  - *Given* un client se connectant hors heures ouvrées,
  - *When* il soumet une nouvelle demande,
  - *Then* la demande est enregistrée avec succès (HTTP 200/Redirect) et horodatée fidèlement.

---

### 5. MATRICE DE TRAÇABILITÉ DES CONTRATS & TEST-SPEC

| Identifiant Contrat | Exigence CDC | Intitulé Contrat / Test-Spec | Niveau de Preuve Requis |
|:---|:---:|:---|:---:|
| **`C16-01`** | `NFR-03` | `NFR-CERT-03-01` : Sondes Health Checks `/healthz` et `/ready` actives | L2 / L3 (TestServer & Intégration) |
| **`C16-02`** | `NFR-03` | `NFR-CERT-03-02` : Métrologie de calcul de disponibilité ≥ 99% | L2 (Règles horaires & exclusions) |
| **`C16-03`** | `NFR-04` | `NFR-CERT-04-01` : Middleware de métrologie de latence des requêtes | L2 (Capture durée d'exécution) |
| **`C16-04`** | `NFR-04` | `NFR-CERT-04-02` : Benchmarks des écrans courants < 3 secondes | L2 / L3 (Suites de tests de charge nominale) |
| **`C16-05`** | `NFR-10` | `NFR-CERT-10-01` : Accessibilité continue 24/7 et gestion des maintenances | L2 (Dépôt hors heures ouvrées & statut) |

---

### 6. ANTI-SCOPE STRICT

Les éléments suivants sont formellement exclus de la Feature 016 :
- `NFR-05` (Volumétrie dimensionnante - PC-01) -> Différé à F018.
- `NFR-06` (Sauvegardes et restauration testée) -> Différé à F018.
- `NFR-09` (Hébergement Groupe AGL - PC-13) -> Différé à F018.
- `SEC-02` (RBAC champs IES - PC-05) -> Différé à F018.
- `SEC-06` (Rétention légale ARTCI - PC-14) -> Différé à F018.
- `ENR-04` (SMS / WhatsApp - PC-16) -> Différé à F018.
- `IA-01` à `IA-07` (Services d'IA - PC-18) -> Différé à F017.
- Aucune migration de schéma de base de données relationnelle (0 table, 0 entité).
