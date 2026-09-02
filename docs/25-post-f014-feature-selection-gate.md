# POST-F014 FEATURE SELECTION GATE — NO IMPLEMENTATION

---

## 1. BASELINE TECHNIQUE VALIDÉE (POST-F014)

- **Dernier commit officiel** : `490c8c83e20697ce79c6560da42aa376f9fe419c` (`feat: close bilingual portal internationalization`)
- **Compilation .NET 10** : **SUCCESS** (0 avertissement, 0 erreur)
- **Suite de tests sérialisée** : **904 / 904 tests verts (100 %)**
  - `CIT.Plateforme.Domain.Tests` : 158 / 158
  - `CIT.Plateforme.Application.Tests` : 461 / 461
  - `CIT.Plateforme.Infrastructure.Tests` : 89 / 89
  - `CIT.Plateforme.Web.Tests` : 196 / 196
- **Migrations EF Core** : **0 pending model changes** (0 table ajoutée, 0 entité ajoutée)
- **Régressions constatées** : **0**
- **Baseline CDC certifiée post-F014** : **169 / 188 (89,89 %)**

---

## 2. RECONSTRUCTION NOMINATIVE DE LA MATRICE DES 188 EXIGENCES

```text
================================================================================
RÉPARTITION NOMINATIVE DES 188 EXIGENCES DU CDC
================================================================================
Exigences CONFORMES (certifiées)      : 169
Exigences RESTANTES (à traiter)       :  19
  - PARTIELLES                        :   0
  - DIFFÉRÉES                         :  19
  - ABSENTES                          :   0
  - À CLARIFIER                       :   0
--------------------------------------------------------------------------------
TOTAL                                 : 188 / 188 (100 %)
================================================================================
```

---

## 3. INVENTAIRE NOMINATIF DES 19 EXIGENCES RESTANTES

| ID | Priorité | Texte CDC & Objet | Dépendance / PC | Effort | Risque | Statut |
|:---|:---:|:---|:---:|:---:|:---:|:---:|
| **`SEC-02`** | **Must** | Chiffrement des données sensibles au repos (AES-256 / DPAPI / TDE) | Aucune | Moyen | Faible | **READY** |
| **`SEC-04`** | **Must** | Politique de rétention et purge des données (RGPD / cycle de vie) | PC-13 | Moyen | Faible | **READY** |
| **`SEC-06`** | **Must** | Détection et prévention des intrusions (WAF / Rate limiting / IDS) | PC-14 | Élevé | Moyen | Différé |
| **`SEC-07`** | **Must** | Analyse antivirale et assainissement des fichiers téléversés | PC-05 | Moyen | Faible | **READY** |
| **`SEC-09`** | **Must** | Audit de sécurité tiers et pentest d'homologation externe | Prestataire | Élevé | Élevé | Différé |
| **`ENR-04`** | **Must** | Notification SMS / WhatsApp de validation avec QR code | PC-16 | Moyen | Moyen | Différé |
| **`NFR-03`** | **Must** | Architecture modulaire & SLA Haute Disponibilité (99.5%) | Hébergement | Élevé | Moyen | Différé |
| **`NFR-04`** | **Must** | Temps de réponse optimaux (<2s pages critiques, <1s API) | PC-01 | Moyen | Faible | **READY** |
| **`NFR-05`** | **Must** | Capacité de montée en charge et gestion des pics | PC-01 | Élevé | Moyen | Différé |
| **`NFR-06`** | **Must** | Sauvegardes automatiques et PRA (RPO < 1h, RTO < 4h) | Hébergement | Élevé | Moyen | Différé |
| **`NFR-09`** | **Must** | Journalisation des erreurs et monitoring temps réel | Aucune | Faible | Faible | **READY** |
| **`NFR-10`** | **Must** | Compatibilité navigateurs modernes (Chrome, Edge, Firefox, Safari) | Aucune | Faible | Faible | **READY** |
| **`IA-01`** | **Must** | Assistant virtuel intelligent pour guider l'usager | PC-18 | Élevé | Élevé | Différé |
| **`IA-02`** | **Must** | Réponse automatique aux questions fréquentes (FAQ dynamique) | PC-18 | Moyen | Moyen | Différé |
| **`IA-03`** | **Must** | Suggestion de documents selon la démarche | PC-18 | Moyen | Moyen | Différé |
| **`IA-04`** | **Must** | Contrôle préliminaire de conformité documentaire | PC-18 | Élevé | Élevé | Différé |
| **`IA-05`** | **Must** | Escalade automatique vers un agent humain | PC-18 | Moyen | Faible | Différé |
| **`IA-06`** | **Should**| Synthèse automatique de dossier pour l'agent | PC-18 | Moyen | Moyen | Différé |
| **`IA-07`** | **Should**| Apprentissage continu à partir des corrections | PC-18 | Élevé | Élevé | Différé |

---

## 4. ANALYSE DES EXIGENCES "MUST" RESTANTES (17 MUST)

Parmi les 19 exigences restantes, **17 sont classées Must**. Elles se répartissent en 4 blocs naturels :

1. **Sécurité Applicative & Données (3 Must)** : `SEC-02` (Chiffrement au repos), `SEC-04` (Rétention / purge), `SEC-07` (Scan antiviral).
   - *Autonomie* : Totale côté applicatif via Clean Architecture (chiffrement de colonnes, worker de purge, scanner antiviral extensible).
   - *Statut* : **100 % READY**.
2. **Performance, Observabilité & Compatibilité (3 Must)** : `NFR-04` (Temps de réponse), `NFR-09` (Monitoring OpenTelemetry/HealthChecks), `NFR-10` (Compatibilité cross-browser).
   - *Autonomie* : Totale via instrumentation applicative et benchmarks.
   - *Statut* : **100 % READY**.
3. **Assistant Virtuel & IA (5 Must + 2 Should)** : `IA-01` à `IA-07`.
   - *Dépendance* : Bloqué par PC-18 au niveau du cadrage de souveraineté et du choix de modèle/hébergement.
   - *Statut* : **NOT READY pour implémentation immédiate sans validation de scope spécifique**.
4. **SLA Infrastructure & Exploitation (6 Must)** : `SEC-06`, `SEC-09`, `ENR-04`, `NFR-03`, `NFR-05`, `NFR-06`.
   - *Dépendance* : Prestataire externe, souscription télécom (PC-16), infrastructure cluster/cloud d'hébergement.
   - *Statut* : **Reporté au jalon final de Production Readiness (F018)**.

---

## 5. AUDIT DES POINTS À CLARIFIER (PC)

- **`PC-01`** : *Dimensionnement de charge et volumétrie cible* — Bloque recette de charge NFR-04/05.
- **`PC-05`** : *Choix du moteur d'analyse antivirale* — Résoluble par abstraction et scanner local.
- **`PC-13`** : *Définition des délais de rétention et purge des dossiers* — Résoluble par paramétrage applicatif.
- **`PC-14`** : *Choix WAF / IDS et auditeur externe* — Bloque homologation de production SEC-06/09.
- **`PC-16`** : *Souscription opérateur SMS / WhatsApp* — Bloque émission réelle ENR-04.
- **`PC-18`** : *Cadre de déploiement et souveraineté de l'Assistant IA* — Bloque cadrage IA-01..07.

---

## 6. ÉVALUATION COMPARATIVE DES CANDIDATS DE FEATURE (SCORING / 100)

| Candidat | Périmètre | Valeur (25) | Gain Must (20) | Gain CDC (15) | Risque (10) | Dette (10) | Autonomie (10) | Effort (10) | Pénalité | TOTAL | Statut |
|:---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| **A — Sécurité Applicative, Rétention & Assainissement Fichiers** | `SEC-02`, `SEC-04`, `SEC-07` | 24 | 20 | 15 | 9 | 10 | 10 | 9 | 0 | **97 / 100** | **ÉLIGIBLE (Top 1)** |
| **B — Performance, Observabilité & Monitoring Temps Réel** | `NFR-04`, `NFR-09`, `NFR-10` | 22 | 18 | 14 | 9 | 9 | 10 | 9 | 0 | **91 / 100** | **ÉLIGIBLE (Top 2)** |
| **C — Assistant Virtuel & FAQ Interactive** | `IA-01..07` | 20 | 15 | 15 | 5 | 5 | 4 | 5 | -15 (PC-18) | **49 / 100** | NOT READY |
| **D — Production Readiness, SLA & Télécoms** | `NFR-03`, `NFR-05`, `NFR-06`, `SEC-06`, `SEC-09`, `ENR-04` | 20 | 20 | 15 | 4 | 5 | 3 | 4 | -25 (Infra/PC) | **41 / 100** | NOT READY |

---

## 7. PROPOSITION OFFICIELLE POUR LA FEATURE 015

```text
FEATURE :
015 — Sécurité Applicative Avancée, Rétention des Données & Assainissement Documentaire

PÉRIMÈTRE :
- SEC-02 (Must) : Chiffrement des données sensibles au repos (données d'identité, documents et secrets)
- SEC-04 (Must) : Politique de rétention, cycle de vie et purge automatique des données
- SEC-07 (Must) : Assainissement et analyse antivirale des fichiers téléversés

GAIN POTENTIEL :
+3 exigences (3 Must)

BASELINE PROJETÉE APRÈS F015 :
169 + 3 = 172 / 188 (91,49 %)

MIGRATION EF CORE :
1 migration additive (champs de métadonnées de rétention/chiffrement ou statut d'assainissement)

JUSTIFICATION :
1. Clôture 3 exigences Must critiques du bloc Sécurité.
2. Élimine les risques de fuite de données et de corruption par fichiers malveillants.
3. 100 % autonome dans l'architecture applicative existante sans prestataire externe bloquant.
```

---

## 8. ROADMAP COMPLÈTE VERS LES 188 EXIGENCES DU CDC

| Feature | Exigences couvertes | Gain | Baseline cible | Type de travail | Prérequis / PC |
|:---|:---|:---:|:---:|:---|:---|
| **F015** | `SEC-02`, `SEC-04`, `SEC-07` | **+3** | **172 / 188 (91,49 %)** | Dév Applicatif & Sécurité | Résolution interne PC-05, PC-13 |
| **F016** | `NFR-04`, `NFR-09`, `NFR-10` | **+3** | **175 / 188 (93,09 %)** | Observabilité, Monitoring, Perf | Métriques PC-01 |
| **F017** | `IA-01`, `IA-02`, `IA-03`, `IA-04`, `IA-05`, `IA-06`, `IA-07` | **+7** | **182 / 188 (96,81 %)** | Assistant Virtuel & FAQ | Cadrage préalable PC-18 |
| **F018** | `NFR-03`, `NFR-05`, `NFR-06`, `SEC-06`, `SEC-09`, `ENR-04` | **+6** | **188 / 188 (100 %)** | Production Readiness, SLA, Pentest | PC-14, PC-16, Infra cible |

- **Contrôle mathématique** : `169 + 3 + 3 + 7 + 6 = 188 / 188 (100 %)`. Zéro double comptage.
