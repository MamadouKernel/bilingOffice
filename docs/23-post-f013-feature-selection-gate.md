# POST-F013 FEATURE SELECTION GATE — NO IMPLEMENTATION

**Auteur** : Scrum Master & Lead QA Platform CIT  
**Date** : 2026-09-02  
**Baseline technique d'entrée** :  
- **Commit** : `7fda32734bc028f5666d622c1bd8b935e4a0eac1` (Feature 013 CLOSED)  
- **Build** : SUCCESS (0 erreur, 0 avertissement)  
- **Tests** : **885 / 885 tests verts (100% PASS)**  
- **EF Core Pending Changes** : **0 pending model changes**  
- **Baseline CDC officielle** : **168 / 188 exigences conformes (89,36 %)**  

---

## 1. RECONSTRUCTION NOMINATIVE DE L'ÉTAT DES 188 EXIGENCES CDC

```text
CONFORMES   : 168 (89,36 %)
PARTIELLES  : 0
DIFFÉRÉES   : 2 (ENR-04, SEC-09)
ABSENTES    : 10 (NFR-07, IA-01..IA-07, SEC-02, NFR-03)
À CLARIFIER : 8 (NFR-04, NFR-05, NFR-06, NFR-09, NFR-10, SEC-04, SEC-06, SEC-07)
TOTAL       : 188 / 188
```

---

## 2. INVENTAIRE EXHAUSTIF DES 20 EXIGENCES RESTANTES (188 - 168 = 20)

| ID | Priorité | Module CDC | Libellé synthétique CDC | Statut actuel | Cause / Dépendance / PC |
|:---|:---:|:---|:---|:---:|:---|
| **NFR-07** | **Should** | Socle / UX | Interface multilingue (version anglaise) | **ABSENTE** | **READY** — Dictionnaires de localisation et culture switching |
| **NFR-03** | **Must** | Architecture | Architecture modulaire, découplée et hautement scalable | **ABSENTE** | **READY** — Validation formelle du découplage Clean Architecture |
| **SEC-02** | **Must** | Sécurité | Masquage des données sensibles selon le profil (IES) | **ABSENTE** | Bloqué par **PC-05** (matrice des champs réservés IES) |
| **SEC-04** | **Must** | Sécurité | Chiffrement des données au repos et en transit | **À CLARIFIER** | Dépendance infrastructure TLS / SQL TDE (**PC-13**) |
| **SEC-06** | **Must** | Sécurité / Légal | Politique d'archivage légal et rétention ARTCI / RGPD | **À CLARIFIER** | Bloqué par **PC-14** (cadre d'archivage légal ARTCI) |
| **SEC-07** | **Must** | Sécurité / SSI | Audit de sécurité / Tests d'intrusion (Pentest) avant PROD | **À CLARIFIER** | Processus d'homologation externe SSI / Prestataire audit |
| **SEC-09** | **Should** | Exploitation | Cloisonnement strict des environnements DEV/QUAL/PROD | **DIFFÉRÉE** | Dépendance infrastructure DevOps / CI-CD / hébergeur |
| **NFR-04** | **Must** | Performance | Temps de réponse global inférieur à 3 secondes | **À CLARIFIER** | Bloqué par **PC-01** (campagne de tir de charge cible) |
| **NFR-05** | **Must** | Dimensionnement | Dimensionnement selon la volumétrie opérationnelle | **À CLARIFIER** | Bloqué par **PC-01** (volumétrie cible validée par le métier) |
| **NFR-06** | **Must** | PRA / Exploitation| Plan de sauvegarde et procédure de restauration testée | **À CLARIFIER** | Dépendance hébergeur / PRA DSI (**PC-13**) |
| **NFR-09** | **Should** | Groupe / DSI | Hébergement conforme aux exigences Groupe AGL | **À CLARIFIER** | Décision DSI / Groupe AGL (**PC-13**) |
| **NFR-10** | **Must** | Exploitation | Disponibilité de la plateforme 24/7 avec supervision | **À CLARIFIER** | Dépendance exploitation hébergeur / SLA infogérance |
| **ENR-04** | **Should** | Enrôlement | Notification chauffeur par SMS / WhatsApp à l'enrôlement | **DIFFÉRÉE** | Bloqué par **PC-16** (passerelle SMS/WhatsApp opérateur) |
| **IA-01** | **Must** | Assistant IA | Chatbot d'assistance interactive multilingue aux clients | **ABSENTE** | Bloqué par **PC-18** (choix LLM, souveraineté, budget IA) |
| **IA-02** | **Must** | Assistant IA | Orientation vers la bonne démarche et aide guidée | **ABSENTE** | Bloqué par **PC-18** |
| **IA-03** | **Must** | Assistant IA | Consultation du statut de démarche par dialogue naturel | **ABSENTE** | Bloqué par **PC-18** |
| **IA-04** | **Should** | Assistant IA | Synthèse automatique du dossier pour l'agent | **ABSENTE** | Bloqué par **PC-18** |
| **IA-05** | **Should** | Assistant IA | Suggestion de réponses pré-rédigées pour l'agent | **ABSENTE** | Bloqué par **PC-18** |
| **IA-06** | **Must** | Assistant IA | Escalade et bascule fluide vers un agent humain | **ABSENTE** | Bloqué par **PC-18** |
| **IA-07** | **Must** | Assistant IA | Apprentissage continu et base de connaissances (RAG/KB) | **ABSENTE** | Bloqué par **PC-18** |

- **Ventilation par priorité** :
  - **Must** : **14** exigences
  - **Should** : **6** exigences
  - **Could** : **0** exigence
  - **Total restant** : **20** exigences

---

## 3. AUDIT DES POINTS À CLARIFIER (PC) & DETTES

### 3.1 Clarification formelle des PC sources
- **PC-16** : *Souscription aux services WhatsApp/SMS (§25 · COM-01 · ENR-04)* — Statut: **Ouvert**. Bloque: Production ENR-04.
- **PC-17** : *Événements déclencheurs de notifications : liste initiale (COM-03 · NOT-02)* — Statut: **Ouvert**.
- **PC-18** : *Définition fonctionnelle de l'assistant IA et conditions de déploiement (IA-01 à IA-07)* — Statut: **Ouvert**. Bloque: **SPÉCIFICATION IA-01..IA-07**. *(Souveraineté des données, hébergement local vs cloud, choix LLM et budget d'exploitation).*

### 3.2 État des dettes techniques ouvertes
- `DETTE-REC-NFR02` : Recette responsive multi-terminaux physiques (Campagne manuelle en cours de recette).
- `DETTE-CFG-SEC10` : Externalisation configuration session/secrets en production (Déploiement cible).
- `DETTE-F010-01` : Impression matérielle tickets borne guichet (Option matérielle).

---

## 4. ÉVALUATION COMPARATIVE DES CANDIDATS DE FEATURE

### Candidat A — Internationalisation & Ergonomie Multilingue (NFR-07)
- **Périmètre** : `NFR-07` (Should).
- **Description** : Support complet bilingue (Français / Anglais) de l'interface portail client et agent via `IStringLocalizer` et middleware de culture ASP.NET Core.
- **Autonomie** : **100% autonome** (0 dépendance externe, 0 PC bloquante).
- **Score** : **88 / 100** — **READY**.

### Candidat B — Certification de Scalabilité & Découplage Modulaire (NFR-03)
- **Périmètre** : `NFR-03` (Must).
- **Description** : Validation formelle et bench de scalabilité de l'architecture découplée Clean Architecture.
- **Score** : **85 / 100** — **READY**.

### Candidat C — Assistant IA / Chatbot Intégré (IA-01..07)
- **Périmètre** : `IA-01` à `IA-07` (5 Must, 2 Should).
- **Statut** : **NOT READY** (Bloqué formellement par **PC-18** au niveau Spécification).

### Candidat D — Homologation Sécurité & Chiffrement (SEC-02, SEC-04, SEC-06, SEC-07, SEC-09)
- **Statut** : **NOT READY** (Bloqué par PC-05, PC-13, PC-14 et processus d'audit tiers).

### Candidat E — Production Readiness & PRA (NFR-04, NFR-05, NFR-06, NFR-09, NFR-10)
- **Statut** : **NOT READY** (Dépendance infrastructure hébergeur et tirs de charge réels PC-01).

---

## 5. PROPOSITION OFFICIELLE POUR LA FEATURE 014

```text
FEATURE :
014 — Internationalisation, Ergonomie Bilingue & Modularité Scalable

OBJECTIF :
Doter la plateforme CIT d'une interface bilingue (Français / Anglais) complète (NFR-07)
et certifier formellement l'architecture modulaire et découplée (NFR-03).

EXIGENCES COUVERTES :
- NFR-07 (Should) — Interface multilingue (version anglaise)
- NFR-03 (Must) — Architecture modulaire et scalable

GAIN POTENTIEL :
+2 exigences

PROJECTION CDC :
168 + 2 = 170 / 188 (90,43 %)

MIGRATION EF CORE :
0 migration (couche purement applicative, ressources de localisation et middleware)

DÉPENDANCES / RISQUES :
0 dépendance bloquante, 0 fournisseur tiers requis.
```

---

## 6. ROADMAP DE CLÔTURE VERS LES 188 EXIGENCES

| Incrément | Nom / Périmètre | Exigences | Gain | Projection | Dépendance / Prérequis |
|:---|:---|:---:|:---:|:---:|:---|
| **F014** | **Internationalisation & Modularité Scalable** | `NFR-07`, `NFR-03` | **+2** | **170 / 188 (90,43 %)** | **READY (Autonome)** |
| **F015** | **Assistant IA & Interaction Client Dialoguée** | `IA-01` à `IA-07` | **+7** | **177 / 188 (94,15 %)** | Levée de **PC-18** (Choix modèle LLM & hébergement) |
| **F016** | **Sécurité Avancée & Rétention Légale** | `SEC-02`, `SEC-04`, `SEC-06`, `SEC-09` | **+4** | **181 / 188 (96,28 %)** | Levée de **PC-05**, **PC-13**, **PC-14** |
| **F017** | **Canaux Mobiles & Notifications Chauffeur** | `ENR-04` | **+1** | **182 / 188 (96,81 %)** | Levée de **PC-16** (Passerelle SMS/WhatsApp) |
| **F018** | **Production Readiness, SLA & Homologation** | `NFR-04`, `NFR-05`, `NFR-06`, `NFR-09`, `NFR-10`, `SEC-07` | **+6** | **188 / 188 (100,00 %)** | Déploiement cible, Pentest et tirs de charge **PC-01** |

- **Contrôle mathématique** :
  `168 + 2 (F014) + 7 (F015) + 4 (F016) + 1 (F017) + 6 (F018) = 188 / 188 (100,00 %)`
