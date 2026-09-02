# ATELIER DSI / PC CLARIFICATION GATE — POST-F016

**Date** : 2026-09-02  
**Baseline technique** : 991 / 991 tests verts (100 %) | 0 warning | 0 pending model changes  
**Baseline CDC officielle** : **172 / 188 (91,49 %)**  

---

## 1. TRAITEMENT & RÉSOLUTION FORMELLE DES POINTS À CLARIFIER (PC)

### 1.1 PC-05 — SEC-02 : Contrôle d'Accès Granulaire par Champ (RBAC IES)
- **Exigence** : `SEC-02` (Must)
- **Décision d'Atelier DSI / Métier** :
  1. *Champs Manifeste* (BL, Armateur, Port de chargement, Navire, Poids brut) : Modifiables exclusivement par l'Agent Manifeste et le Superviseur ; consultables en lecture seule par la Facturation et le Contrôle Documentaire.
  2. *Champs Financiers & Facturation* (Montant HT/TTC, Lignes tarifaires, Avoirs appliqués) : Modifiables exclusivement par l'Agent Facturation et le Comptable ; masqués pour l'Agent Manifeste externe.
  3. *Champs Contrôle Documentaire* (Statut conformité douanière, Pièces validées) : Modifiables uniquement par l'Agent Contrôle Documentaire.
  4. *Règles de masquage & export* : Les masquages s'appliquent à l'UI Blazor, à l'API REST NFR-08 et aux exports CSV/PDF.
  5. *Traçabilité* : Toute tentative d'accès non autorisé à un champ restreint est bloquée (fail-secure) et journalisée en audit `SEC-03`.
- **Statut PC-05** : **RESOLVED** $\implies$ **`SEC-02` READY FOR SPEC**.

---

### 1.2 PC-14 — SEC-06 : Politique de Rétention & Conservation Réglementaire
- **Exigence** : `SEC-06` (Must)
- **Décision d'Atelier DSI / Conformité Légale (Code de Commerce / OHADA / ARTCI)** :
  1. *Dossiers opérationnels & Tickets clôturés* : Conservation active 5 ans à compter de la date de clôture définitive.
  2. *Pièces comptables & Factures* : Conservation archivée 10 ans avec scellement d'intégrité (SHA-256).
  3. *Journaux d'audit de sécurité (`SEC-03`)* : Conservation inaltérable pendant 3 ans.
  4. *Brouillons abandonnés (`ERR-01`)* : Purge automatique après 30 jours d'inactivité sans validation.
  5. *Données à caractère personnel* : Anonymisation irréversible après expiration du délai légal de conservation.
  6. *Mécanisme de purge* : Service d'arrière-plan avec validation Superviseur et traçabilité forensique du certificat de purge.
- **Statut PC-14** : **RESOLVED** $\implies$ **`SEC-06` READY FOR SPEC**.

---

### 1.3 PC-01 — NFR-05 : Volumétrie & Capacité Nominale / Crête
- **Exigence** : `NFR-05` (Must) — 500 utilisateurs simultanés, pics saisonniers à 50 RPS.
- **Statut** : **OPEN** (Nécessite banc de test de charge distribué en environnement externe).

---

### 1.4 PC-13 — NFR-09 : Homologation Hébergement Groupe AGL
- **Exigence** : `NFR-09` (Must) — Architecture cible datacenter Groupe / Cloud souverain.
- **Statut** : **OPEN** (Processus d'audit d'homologation DSI en cours).

---

### 1.5 PC-16 — ENR-04 : Notifications Mobiles SMS / WhatsApp
- **Exigence** : `ENR-04` (Must) — Passerelle SMS & WhatsApp.
- **Statut** : **OPEN** (En attente de contractualisation du fournisseur passerelle et clés API sandbox).

---

### 1.6 PC-18 — IA-01 à IA-07 : Stratégie & Services IA
- **Exigences** : `IA-01` à `IA-05` (Should), `IA-06` & `IA-07` (Could).
- **Statut** : **OPEN** (Cadrage éthique, souveraineté des données et budget LLM en attente d'arbitrage).

---

### 1.7 F016 Environment Certification Gate
- **Exigences** : `NFR-03`, `NFR-04`, `NFR-10`.
- **Statut** : **DEFERRED** (En attente d'URL Staging/UAT externe déployée).

---

## 2. SYNTHÈSE DES CANDIDATS & SÉLECTION DE LA FEATURE 017

```
TABLEAU COMPARATIF DES CANDIDATS :

1. Feature 017 — Sécurité Granulaire Métier & Rétention Documentaire (SEC-02, SEC-06)
   - Exigences CDC : SEC-02 (Must), SEC-06 (Must)
   - Statut PC     : PC-05 (RESOLVED), PC-14 (RESOLVED)
   - Nature        : Feature DEV (Applicatif + Domaine + UI)
   - Score         : 90 / 100
   - Gain CDC Max  : +2 (Projection : 174 / 188 — 92,55 %)
   - Statut        : READY FOR SPECIFICATION

2. F016 Environment Certification Gate (NFR-03, NFR-04, NFR-10)
   - Nature        : Certification Gate (0 code nouveau)
   - Statut PC     : DEFERRED (Attente d'instance externe)
   - Gain CDC Max  : +3 (Projection : 175 / 188)

3. Feature Canaux Mobiles (ENR-04)
   - Statut PC     : OPEN (PC-16 bloquée fournisseur)

4. Feature IA (IA-01 à IA-07)
   - Statut PC     : OPEN (PC-18 bloquée DSI)
```

---

## 3. PROCHAINE FEATURE RETENUE

### **Feature 017 — Sécurité Granulaire Métier & Rétention Documentaire Réglementaire**
- **Périmètre contractuel** :
  - **`SEC-02`** (Must) : Contrôle d'accès RBAC au niveau champ pour les traitements parallèles IES et masquage contextuel.
  - **`SEC-06`** (Must) : Politique de rétention, archivage légal décennal et purge sécurisée des données périmées.
- **Gain maximal** : **+2** (Passage de 172 à **174 / 188**).
- **Projection globale après F017 + F016 Environment Gate** : **177 / 188 (94,15 %)**.
