# RAPPORT DE SÉLECTION POST-F016 — FEATURE SELECTION GATE

**Date** : 2026-09-02  
**État du dépôt** : Clôture technique de Feature 016 validée (`bf8c327`)  
**Baseline technique** : 991 / 991 tests verts (100 %) | 0 warning | 0 pending model changes  
**Baseline CDC officielle** : **172 / 188 (91,49 %)**  

---

## 1. CARTOGRAPHIE FORENSIQUE DES 188 EXIGENCES CONTRACTUELLES

| Statut d'Exigence | Nombre | Détail / Exigences associées |
|:---|:---:|:---|
| **CONFORMES & CERTIFIÉES** | **172** | Exigences couvertes par F001 à F015 |
| **TECHNICALLY IMPLEMENTED / NOT PROVEN** | **3** | `NFR-03` (Dispo ≥ 99%), `NFR-04` (Latence < 3s), `NFR-10` (24/7 & Maintenance) |
| **BLOQUÉES PAR POINT À CLARIFIER (PC)** | **10** | `SEC-02` (PC-05), `SEC-06` (PC-14), `ENR-04` (PC-16), `NFR-05` (PC-01), `IA-01..07` (PC-18) |
| **DÉPENDANCES INFRA / DSI / EXPLOITATION** | **3** | `NFR-06` (Sauvegarde/PRA), `NFR-09` (Hébergement Groupe / PC-13) + L3 de `NFR-03/04/10` |
| **TOTAL** | **188** | **Cohérence mathématique stricte** : $172 + 16 = 188$ |

---

## 2. AUDIT DÉTAILLÉ DES 16 EXIGENCES RESTANTES

| ID | Priorité | Titre CDC | Statut Actuel | Bloquant Principal | Nature | Ready Now ? |
|:---|:---:|:---|:---|:---|:---:|:---:|
| **`NFR-03`** | Must | Disponibilité $\ge 99\%$ heures ouvrées | Implémenté L2 | Attente Staging/Recette déployée | Certification Gate | **NON (Différé)** |
| **`NFR-04`** | Must | Temps de réponse $< 3\text{ s}$ sur écrans | Implémenté L2 | Attente Tir k6 Staging externe | Certification Gate | **NON (Différé)** |
| **`NFR-10`** | Must | Accessibilité 24/7 & Maintenance | Implémenté L2 | Attente Suivi Prod / Recette | Certification Gate | **NON (Différé)** |
| **`SEC-02`** | Must | RBAC granulaire aux champs IES | Absent | **PC-05** (Spécification champs sensibles) | Feature DEV | **NON** |
| **`SEC-06`** | Must | Rétention & Conservation réglementaire | Absent | **PC-14** (Durées de rétention DSI) | Feature DEV | **NON** |
| **`ENR-04`** | Must | Notifications SMS / WhatsApp | Absent | **PC-16** (Fournisseur passerelle externe) | Intégration Fournisseur | **NON** |
| **`NFR-05`** | Must | Capacité nominale (500 users simultanés) | Absent | **PC-01** (Profils de charge & volumétrie) | Certification Gate | **NON** |
| **`NFR-06`** | Must | Sauvegarde, RPO/RTO & Reprise (PRA) | Absent | Procédure Infra / Exploitation DSI | Infra / Procédure | **NON** |
| **`NFR-09`** | Must | Homologation Hébergement Groupe | Absent | **PC-13** (Standards d'infrastructure Groupe) | Audit / Homologation | **NON** |
| **`IA-01`** | Should | Assistant interactif usager | Absent | **PC-18** (Choix LLM, RGPD, Budget DSI) | Feature IA | **NON** |
| **`IA-02`** | Should | Pré-remplissage intelligent formulaires | Absent | **PC-18** (Choix LLM, RGPD, Budget DSI) | Feature IA | **NON** |
| **`IA-03`** | Should | Extraction automatique de justificatifs | Absent | **PC-18** (Choix LLM, RGPD, Budget DSI) | Feature IA | **NON** |
| **`IA-04`** | Should | Synthèse automatique de dossier agent | Absent | **PC-18** (Choix LLM, RGPD, Budget DSI) | Feature IA | **NON** |
| **`IA-05`** | Should | Recommandation d'actions & réponses | Absent | **PC-18** (Choix LLM, RGPD, Budget DSI) | Feature IA | **NON** |
| **`IA-06`** | Could | Détection préventive d'anomalies | Absent | **PC-18** (Choix LLM, RGPD, Budget DSI) | Feature IA | **NON** |
| **`IA-07`** | Could | Analyse des tendances & rapports IA | Absent | **PC-18** (Choix LLM, RGPD, Budget DSI) | Feature IA | **NON** |

---

## 3. ANALYSE CRITIQUE & SCORING DES CANDIDATS F017

```
1. Feature IA (IA-01 à IA-07)
   - Score : 25 / 100
   - Bloqué par PC-18 (Absence d'arbitrage DSI sur le fournisseur LLM, budget et souveraineté des données).

2. Feature Sécurité Métier & Rétention (SEC-02, SEC-06)
   - Score : 35 / 100
   - Bloqué par PC-05 (Champs IES) et PC-14 (Durées légales de conservation).

3. Feature Canaux Mobiles (ENR-04)
   - Score : 30 / 100
   - Bloqué par PC-16 (Absence de contrat / API de passerelle SMS/WhatsApp).

4. Certification Environnementale F016 (NFR-03, NFR-04, NFR-10)
   - Score : 50 / 100
   - Nature : Certification Gate pur (0 code à développer).
   - Bloqué temporairement par l'absence d'environnement d'hébergement déployé (Staging/UAT externe).
```

---

## 4. DÉCISION DE GOUVERNANCE & RECOMMANDATIONS

1. **Aucune Feature de développement n'est actuellement READY NOW** sans levée préalable des Points à Clarifier (PC-01, PC-05, PC-13, PC-14, PC-16, PC-18).
2. **Recommandation immédiate pour la DSI / Direction Projet** :
   - **Atelier Métier 1** : Arbitrer **PC-05** (Champs IES) et **PC-14** (Durées de rétention) pour débloquer la **Feature 017 — Sécurité Granulaire Métier & Rétention Documentaire (`SEC-02`, `SEC-06`)**.
   - **Atelier Technique 2** : Mettre à disposition l'environnement d'hébergement Staging/Recette pour exécuter le **`F016 ENVIRONMENT CERTIFICATION GATE`** (Gain potentiel immédiat : **+3**, soit **175 / 188 (93,09 %)**).
   - **Atelier Stratégique 3** : Cadrer **PC-18** (Stratégie IA / LLM souverain) pour planifier les exigences `IA-01..07`.

---

## 5. BILAN DE LA TRAJECTOIRE VERS 188 / 188

$$\text{Baseline Actuelle} = 172 \quad\longrightarrow\quad \text{Environment Gate F016 (+3)} = 175 \quad\longrightarrow\quad \text{Sec & Rétention (+2)} = 177 \quad\longrightarrow\quad \text{SMS (+1)} = 178 \quad\longrightarrow\quad \text{Infra/Charge (+3)} = 181 \quad\longrightarrow\quad \text{IA (+7)} = 188$$
