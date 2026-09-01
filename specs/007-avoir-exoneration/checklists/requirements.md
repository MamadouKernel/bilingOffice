# Checklist d'Exigences Fonctionnelles Clarifiées — Feature 007
## Module Avoir & Exonération

> **Document** : `specs/007-avoir-exoneration/checklists/requirements.md`  
> **Statut** : **CLARIFIÉ & PRÊT POUR /speckit.plan**  
> **Date** : 1er septembre 2026  

---

## 1. Exigences Cibles (AVE-01 à AVE-04)

| ID Exigence | Priorité | Description résumée | Critères d'acceptation définis | Statut spécification |
|:---|:---:|:---|:---:|:---:|
| **`AVE-01`** | **Must** | Soumission par le client d'une demande d'avoir ou d'exonération avec justificatifs ; génération d'un ticket | ✅ Oui (Section 4, US1 ; CLAR-AVE-02, 03, 04) | **CLARIFIÉE & VALIDÉE** |
| **`AVE-02`** | **Must** | Workflows de validation propres à chaque type avec étapes et intervenants paramétrables | ✅ Oui (Section 4, US2 & US3 ; CLAR-AVE-01, 05, 11) | **CLARIFIÉE & VALIDÉE** |
| **`AVE-03`** | **Must** | Décision (accord, rejet motivé) et documents associés déposés dans le ticket et notifiés au client | ✅ Oui (Section 4, US3 & US4 ; CLAR-AVE-06, 07, 08, 12) | **CLARIFIÉE & VALIDÉE** |
| **`AVE-04`** | **Should** | Liaison optionnelle de la demande au ticket de parcours client concerné | ✅ Oui (Section 4, US1 ; CLAR-AVE-09) | **CLARIFIÉE & VALIDÉE** |

---

## 2. Matrice des Clarifications Résolues (CLAR-AVE-01 à CLAR-AVE-12)

| Code Clarification | Thème | Décision Validée | Statut |
| :--- | :--- | :--- | :---: |
| **`CLAR-AVE-01`** | **Frontière Finance** | Validation interne Finance hors application, portail gère suivi/décision/docs/notif | ✅ **RÉSOLU** |
| **`CLAR-AVE-02`** | **Type de demande** | Exactement 2 types : `Avoir` et `Exoneration` | ✅ **RÉSOLU** |
| **`CLAR-AVE-03`** | **Données formulaire** | Formulaire unifié avec objet, description, montant optionnel, ref optionnelle | ✅ **RÉSOLU** |
| **`CLAR-AVE-04`** | **Justificatifs** | Mécanisme paramétrable (PDF/JPEG/PNG <= 10 Mo), 0 liste contractuelle durcie | ✅ **RÉSOLU** |
| **`CLAR-AVE-05`** | **Workflow & Statuts** | Réutilisation cycle F001 (`Cree`, `EnCours`, `EnAttenteClient`, `Valide`, `Rejete`, `Cloture`) | ✅ **RÉSOLU** |
| **`CLAR-AVE-06`** | **Accord** | Enregistrement décision favorable, dépôt document, notification, clôture | ✅ **RÉSOLU** |
| **`CLAR-AVE-07`** | **Rejet motivé** | Motif obligatoire non vide, dépôt doc optionnel, notification, clôture | ✅ **RÉSOLU** |
| **`CLAR-AVE-08`** | **Documents décision** | Dépôt manuel par l'agent, 0 générateur automatique d'avoir financier | ✅ **RÉSOLU** |
| **`CLAR-AVE-09`** | **Liaison ticket (AVE-04)** | Réutilisation directe de `LiaisonTicket` F001 sans impact sur parent | ✅ **RÉSOLU** |
| **`CLAR-AVE-10`** | **Compléments** | Réutilisation directe de `AttenteClientCommand`, `Message` et `ACR-01` | ✅ **RÉSOLU** |
| **`CLAR-AVE-11`** | **Permissions RBAC** | `Permission.AvoirExonerationInstruire` et `Permission.AvoirExonerationDecider` | ✅ **RÉSOLU** |
| **`CLAR-AVE-12`** | **Événements F009** | Publication exclusive d'événements sur `EvenementSocle` (0 canal direct) | ✅ **RÉSOLU** |

---

## 3. Synthèse de la Couverture des Scénarios de Test (TEST-SPEC)

| Code Test-Spec | Exigence(s) Couverte(s) | Description du Cas de Test | Statut |
|:---|:---:|:---|:---:|
| **`AVE-SPEC-01`** | `AVE-01`, `SOC-01` | Soumission nominale d'une demande d'Avoir avec justificatifs | ✅ Prêt |
| **`AVE-SPEC-02`** | `AVE-01`, `SOC-01` | Soumission nominale d'une demande d'Exonération avec attestation | ✅ Prêt |
| **`AVE-SPEC-03`** | `AVE-04`, `SOC-10` | Liaison optionnelle au ticket Billing Import parent | ✅ Prêt |
| **`AVE-SPEC-04`** | `AVE-02`, `SOC-02` | Prise en charge par l'agent instructeur et consultation liasse | ✅ Prêt |
| **`AVE-SPEC-05`** | `AVE-02`, `SOC-06` | Demande de complément d'information et bascule `EnAttenteClient` | ✅ Prêt |
| **`AVE-SPEC-06`** | `AVE-02`, `ACR-01` | Fourniture du complément par le client et reprise instruction | ✅ Prêt |
| **`AVE-SPEC-07`** | `AVE-03`, `SOC-07` | Enregistrement d'un accord avec dépôt de document de décision | ✅ Prêt |
| **`AVE-SPEC-08`** | `AVE-03` | Enregistrement d'un rejet avec motif obligatoire (rejet si vide) | ✅ Prêt |
| **`AVE-SPEC-09`** | `AVE-03`, `COM-01` | Émission d'événements socles et vérification notification F009 | ✅ Prêt |
| **`AVE-SPEC-10`** | `SEC-01` | Cloisonnement strict multi-société et rejet tentatives IDOR | ✅ Prêt |

---

## 4. Garde-fous et Invariants Architecturaux Validés

- [x] **Dissociation portail vs finance interne** : aucun moteur ERP comptable ni validation financière complexe codée en dur.
- [x] **Paramétrabilité des intervenants (PC-03)** : utilisation de permissions RBAC configurables (`Permission.AvoirExonerationInstruire`, `Permission.AvoirExonerationDecider`).
- [x] **Paramétrabilité des étapes (PC-15)** : réutilisation du moteur de transitions configurables de F001 sans altération de code durci.
- [x] **Aucun doublon de composants** : réutilisation directe de `Ticket`, `Dossier`, `Document`, `Message`, `LiaisonTicket`.
- [x] **Aucun appel direct de communication** : notification exclusive via `EvenementSocle` consommé par F009.
- [x] **Sécurité stricte (Anti-IDOR)** : dérivation serveur de l'identité demandeur et isolation étanche multi-société.
- [x] **Traçabilité totale** : toute décision (accord ou rejet avec motif) et tout dépôt de pièce consignés dans le journal d'audit et l'historique.
