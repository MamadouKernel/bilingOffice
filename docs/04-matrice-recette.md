# Matrice de Recette — Scénarios d'acceptation
## Plateforme Digitale CIT — v1.0

> **Document** : `docs/04-matrice-recette.md`  
> **Référence CDC** : CIT-CIV-DSI-RFP-XXXX v1.0 — 12 août 2026 — §22 (Recette et critères d'acceptation)  
> **Créé le** : 28 août 2026  
> **Statut** : Pré-analyse — scénarios extraits du CDC §22, à compléter lors de la phase Spec Kit

---

## Important — Nature de ce document

> Les scénarios de recette ci-dessous sont extraits **textuellement** de la **section 22 du CDC** (« Recette et critères d'acceptation »). Ils constituent des **critères d'acceptation contractuels** définis par CIT.
>
> - Ce **ne sont pas** des exigences fonctionnelles numérotées du CDC.
> - Ils ne génèrent **pas** de nouvelles exigences.
> - Ils serviront de base aux **Test Plans** lors de la phase Spec Kit (`/speckit.specify`).
> - L'absence de scénario de recette dans le CDC pour certaines exigences Must **ne signifie pas** que ces exigences sont facultatives. Ces exigences devront recevoir des tests dans Spec Kit.

**Critères d'acceptation globaux (CDC §22)** :
- 100 % des scénarios Must exécutés avec succès
- Aucune anomalie bloquante ou majeure ouverte
- Exigences de sécurité vérifiées
- Documentation livrée

---

## Partie 1 — Scénarios de recette (extraits du CDC §22)

| ID | Intitulé | Description exacte (CDC §22) | Exigences Must couvertes | Exigences Should/Could couvertes | Préconditions | Acteurs | Statut |
|----|----------|------------------------------|--------------------------|----------------------------------|---------------|---------|--------|
| REC-SCE-01 | IMPORT client normal | Soumission 3 BL → 3 tickets ; 1 validé directement, 1 rejet motivé, 1 demande de complément puis validation ; facturation ; paiement + preuve ; validation paiement ; dépôt BS + BADT ; clôture et archivage | SOC-01 · SOC-02 · SOC-06 · SOC-09 · IMP-01 · IMP-02 · IMP-03 · IMP-04 · IMP-05 · IMP-06 · IMP-10 · IMP-11 · IMP-12 · IMP-13 · IMP-14 · UNI-01 · UNI-02 · UNI-03 | COM-01 · COM-02 | Compte client normal activé · Paramétrage fichiers exigés · Grille tarifaire chargée | Client normal · Pilote Manifeste · Pilote Facturation · Caisse/Finance · Pilote CD | À jouer en recette |
| REC-SCE-02 | IMPORT client en compte | Facture éditée → passage direct au Contrôle Documentaire → remise des documents sans étape de paiement | IMP-20 · IMP-21 · IMP-22 | — | Compte client en compte activé | Client en compte · Pilote Manifeste · Pilote Facturation · Pilote CD | À jouer en recette |
| REC-SCE-03 | IMPORT client deposit | Facture → lettrage Comptabilité → Contrôle Documentaire → clôture | IMP-30 · IMP-31 · IMP-32 | — | Compte client deposit activé | Client deposit · Pilote Manifeste · Pilote Facturation · Comptable Client · Pilote CD | À jouer en recette |
| REC-SCE-04 | IMPORT client IES | Traitement parallèle Manifeste/CD avec champs réservés par rôle ; jointure ; remise des documents ; clôture | IMP-40 · IMP-41 · IMP-42 · SEC-02 | — | Compte client IES activé · RBAC au niveau champ configuré | Client IES · Pilote Manifeste · Pilote CD | À jouer en recette |
| REC-SCE-05 | EXPORT (4 types de clients) | Codification Back Office, Draft IN à la facturation, BEE en document final ; vérifier les 4 variantes de clients | EXP-01 · EXP-02 · EXP-03 · EXP-04 | UNI-04 | Comptes activés pour les 4 types · Back Office configuré | Client (4 types) · Back Office · Pilote Facturation · Pilote CD | À jouer en recette |
| REC-SCE-06 | Simulation de proforma | Calcul multi-conteneurs (Dry et Reefer, avec majorations lourd/dangereux/OOG et stationnement par tranches) contrôlé au franc près contre l'outil Excel de référence sur les cas de l'Annexe A.8 ; vérification qu'une modification de paramètre (tarif, taux, franchise) sans développement se répercute correctement sur le calcul | SIM-01 · SIM-02 · SIM-03 · SIM-04 · SIM-05 · SIM-06 · SIM-07 | SIM-08 · SIM-09 · SIM-10 | Grille tarifaire initiale chargée (Annexe A) · Moteur de calcul déployé | Client · Admin fonctionnel | À jouer en recette |
| REC-SCE-07 | Customer Complaint | Création par catégorie, instruction, complément, orientation, clôture avec réponse | REC-01 · REC-02 · REC-03 · REC-04 | REC-05 | Catégories configurées · Compte Support Opérationnel activé | Client · Support Opérationnel | À jouer en recette |
| REC-SCE-08 | Enrôlement transporteur | Dossier à distance, complément, validation, convocation photo/badge, clôture | ENR-01 · ENR-02 · ENR-03 | ENR-04 | Liste pièces configurée · Compte Agent Gate activé | Transporteur/Chauffeur · Agent Gate | À jouer en recette |
| REC-SCE-09 | Prise en charge | Un ticket arrive en file en « attente de prise en charge », un opérateur l'accepte ; les horodatages (arrivée, prise en charge, dernière activité) sont enregistrés et exploités dans les indicateurs | SOC-12 · PEC-01 · PEC-02 · PEC-03 · PEC-04 · TPS-01 | TPS-05 | File configurée avec au moins un opérateur | Agent · Superviseur | À jouer en recette |
| REC-SCE-10 | Transfert de file | Un opérateur transfère un ticket mal orienté (ex. Contrôle BL → Facturation) avec motif obligatoire ; le ticket conserve numéro, dossier, documents, commentaires et historique ; le client est notifié ; un transfert vers une destination non autorisée par la matrice est refusé | TRF-01 · TRF-02 · TRF-03 · TRF-05 · TRF-06 · WKF-03 | — | Matrice de transitions configurée · Files configurées | Agent habilité · Client | À jouer en recette |
| REC-SCE-11 | Réaffectation entre opérateurs | Réaffectation A → B selon habilitations, tracée dans l'historique | TRF-04 · WKF-03 | — | Deux opérateurs dans la même file | Agent habilité · Superviseur | À jouer en recette |
| REC-SCE-12 | Temps et SLA | Vérification de la mesure attente/traitement par étape, de la distinction temps interne / temps client (SLA suspendu en « En attente client »), et du déclenchement des seuils 50 % / 80 % / dépassement / escalade | TPS-01 · TPS-02 · TPS-03 · TPS-04 · SLA-01 · SLA-02 · SLA-03 · SLA-05 · ALE-01 · ALE-02 · ALE-03 | SLA-04 | SLA configurés avec valeurs cibles (PC-12) · Seuils d'alerte configurés | Superviseur · Admin | À jouer en recette |
| REC-SCE-13 | Supervision | Vue temps réel des files et de la charge, alertes (ticket non pris en charge, sans activité, SLA), affectation d'un ticket non pris en charge, déblocage d'un ticket | SUP-01 · SUP-02 · SUP-03 · SUP-04 · SUP-05 · ALE-01 · ALE-02 · ALE-03 | MAS-01 | Files configurées · Opérateurs avec données de charge | Superviseur | À jouer en recette |
| REC-SCE-14 | Transverse | Notifications sur chaque canal configuré avec lien vers le ticket ; continuité digital/présentiel par recherche de ticket au guichet et rendez-vous donnant accès au bureau de facturation ; regroupement des tickets d'une même demande en dossier ; contrôle des habilitations (un client ne voit pas les tickets d'un autre ; un agent ne voit pas les files des autres rôles) ; journal d'audit | COM-01 · COM-02 · COM-03 · COM-04 · CON-01 · CON-02 · CON-03 · SOC-11 · SOC-13 · CPT-04 · SEC-01 · SEC-02 · SEC-03 · SEC-04 · SEC-05 · SEC-07 · SEC-10 · SEC-11 · SEC-12 · ACR-01 | COM-05 · NOT-03 | Canaux configurés · Comptes de test (client A, client B, agent rôle 1, agent rôle 2) | Client · Agent · Superviseur · Admin | À jouer en recette |

---

## Partie 2 — Cas de référence simulation (Annexe A.8 du CDC)

> Ces cas sont définis dans l'Annexe A.8 du CDC. Ils constituent la **référence de recette obligatoire** pour la simulation de proforma (REC-SCE-06). L'implémentation doit produire **exactement ces totaux**.

**Hypothèse commune** : durée de séjour de 29 jours (date d'arrivée 10/02, enlèvement prévisionnel 10/03).

| Cas | Sens | Taille | Type | Poids (t) | OOG | Dang. | Catégorie | Total XOF attendu |
|-----|------|--------|------|-----------|-----|-------|-----------|-------------------|
| test01 | Import Transit | 20" | Dry | 100 | Oui | Oui | C4 | **199 129** |
| test02 | Import Local | 40" | Reefer | 200 | Non | Non | C1 | **2 054 980** |
| test03 | Export Local | 20" | Dry | 300 | Oui | Non | C2 | **245 500** |
| test04 | Export Transit | 40" | Reefer | 100 | Non | Oui | C5 | **2 053 400** |
| test06 | Import Local | 40" | Reefer | 300 | Non | Non | C3 | **2 245 300** |

**Décomposition de référence test06** (Import Local, 40", Reefer, 300t, C3, séjour 29j) :
- Acconage : 280 000
- Relevage : 50 000
- Branchement frigorifique : 67 000 × (29 − 2) = 1 809 000
- Majoration lourd (30% × 280 000, poids 300t > seuil 26t) : 84 000
- Stationnement 18 jours facturables (29 − 11 franchise) : 5j × 850 + 10j × 1 250 + 3j × 1 850 = 22 300
  - Dont CIT (30%) : 6 690 · OIC (20%) : 4 460 · PAA (50%) : 11 150
- **Total : 2 245 300 XOF**

---

## Partie 3 — Matrice de couverture des exigences Must

> **Lecture** :
> - **Directe** : l'exigence est explicitement vérifiée par au moins un scénario de recette du CDC §22
> - **Partielle** : l'exigence est partiellement couverte (ex. vérifiable dans le cadre d'un scénario, mais sans focus dédié)
> - **Absente** : aucun scénario du CDC §22 ne couvre directement cette exigence — un test devra être créé dans Spec Kit

| Exigence Must | Scénario(s) CDC couvrant | Couverture |
|---------------|--------------------------|------------|
| SOC-01 | REC-SCE-01 à REC-SCE-14 | Directe |
| SOC-02 | REC-SCE-01 à REC-SCE-08 | Directe |
| SOC-03 | REC-SCE-01 à REC-SCE-08 | Directe |
| SOC-04 | REC-SCE-09 · REC-SCE-13 | Directe |
| SOC-05 | REC-SCE-01 | Partielle |
| SOC-06 | REC-SCE-01 · REC-SCE-07 | Directe |
| SOC-07 | REC-SCE-01 à REC-SCE-08 | Partielle |
| SOC-08 | REC-SCE-01 | Partielle |
| SOC-09 | REC-SCE-01 | Directe |
| SOC-10 | REC-SCE-14 | Partielle |
| SOC-11 | REC-SCE-01 · REC-SCE-14 | Directe |
| SOC-12 | REC-SCE-09 | Directe |
| SOC-13 | REC-SCE-14 | Directe |
| CYC-01 | Aucun | **Absente** |
| CYC-02 | Aucun | **Absente** |
| CYC-03 | Aucun | **Absente** |
| CYC-04 | Aucun | **Absente** |
| CYC-05 | Aucun | **Absente** |
| MSG-01 | REC-SCE-01 | Partielle |
| MSG-02 | Aucun | **Absente** |
| MSG-03 | Aucun | **Absente** |
| MSG-05 | Aucun | **Absente** |
| WKF-01 | REC-SCE-01 à REC-SCE-08 | Directe |
| WKF-02 | REC-SCE-10 · REC-SCE-11 | Directe |
| WKF-03 | REC-SCE-10 · REC-SCE-11 | Directe |
| PEC-01 | REC-SCE-09 | Directe |
| PEC-02 | REC-SCE-09 | Directe |
| PEC-03 | REC-SCE-09 | Directe |
| PEC-04 | REC-SCE-09 | Directe |
| TRF-01 | REC-SCE-10 | Directe |
| TRF-02 | REC-SCE-10 | Directe |
| TRF-03 | REC-SCE-10 | Directe |
| TRF-04 | REC-SCE-11 | Directe |
| TRF-05 | REC-SCE-10 | Directe |
| TRF-06 | REC-SCE-10 | Directe |
| TPS-01 | REC-SCE-09 · REC-SCE-12 | Directe |
| TPS-02 | REC-SCE-12 | Directe |
| TPS-03 | REC-SCE-12 | Directe |
| TPS-04 | REC-SCE-12 | Directe |
| SLA-01 | REC-SCE-12 | Directe |
| SLA-02 | REC-SCE-12 | Directe |
| SLA-03 | REC-SCE-12 | Directe |
| SLA-05 | REC-SCE-12 | Directe |
| ALE-01 | REC-SCE-12 · REC-SCE-13 | Directe |
| ALE-02 | REC-SCE-12 | Directe |
| ALE-03 | REC-SCE-12 | Directe |
| SUP-01 | REC-SCE-13 | Directe |
| SUP-02 | REC-SCE-13 | Directe |
| SUP-03 | REC-SCE-13 | Directe |
| SUP-04 | REC-SCE-13 | Directe |
| SUP-05 | REC-SCE-13 | Directe |
| DIS-01 | REC-SCE-13 | Partielle |
| DIS-02 | REC-SCE-13 | Partielle |
| DIS-03 | Aucun | **Absente** |
| AFF-01 | REC-SCE-09 | Partielle |
| AFF-02 | REC-SCE-13 | Partielle |
| AFF-07 | Aucun | **Absente** |
| PRI-01 | Aucun | **Absente** |
| PRI-02 | Aucun | **Absente** |
| PRI-03 | Aucun | **Absente** |
| PRI-04 | Aucun | **Absente** |
| MAS-02 | Aucun (conditionné à MAS-01 Should) | **Absente** |
| CPT-01 | Aucun | **Absente** |
| CPT-02 | REC-SCE-01 à REC-SCE-05 | Partielle |
| CPT-03 | REC-SCE-14 | Partielle |
| CPT-04 | REC-SCE-14 | Directe |
| CPT-05 | Aucun | **Absente** |
| ENT-01 | Aucun | **Absente** |
| ENT-02 | Aucun | **Absente** |
| ENT-03 | REC-SCE-14 | Partielle |
| IMP-01 | REC-SCE-01 | Directe |
| IMP-02 | REC-SCE-01 | Directe |
| IMP-03 | REC-SCE-01 | Directe |
| IMP-04 | REC-SCE-01 | Directe |
| IMP-05 | REC-SCE-01 | Directe |
| IMP-06 | REC-SCE-01 | Directe |
| IMP-10 | REC-SCE-01 | Directe |
| IMP-11 | REC-SCE-01 | Directe |
| IMP-12 | REC-SCE-01 | Directe |
| IMP-13 | REC-SCE-01 | Directe |
| IMP-14 | REC-SCE-01 | Directe |
| IMP-20 | REC-SCE-02 | Directe |
| IMP-21 | REC-SCE-02 | Directe |
| IMP-22 | REC-SCE-02 | Directe |
| IMP-30 | REC-SCE-03 | Directe |
| IMP-31 | REC-SCE-03 | Directe |
| IMP-32 | REC-SCE-03 | Directe |
| IMP-40 | REC-SCE-04 | Directe |
| IMP-41 | REC-SCE-04 | Directe |
| IMP-42 | REC-SCE-04 | Directe |
| UNI-01 | REC-SCE-01 | Directe |
| UNI-02 | REC-SCE-01 | Partielle |
| UNI-03 | Aucun | **Absente** |
| EXP-01 | REC-SCE-05 | Directe |
| EXP-02 | REC-SCE-05 | Directe |
| EXP-03 | REC-SCE-05 | Directe |
| EXP-04 | REC-SCE-05 | Directe |
| SIM-01 | REC-SCE-06 | Directe |
| SIM-02 | REC-SCE-06 | Directe |
| SIM-03 | REC-SCE-06 | Directe |
| SIM-04 | REC-SCE-06 | Directe |
| SIM-05 | REC-SCE-06 | Directe |
| SIM-06 | REC-SCE-06 | Directe |
| SIM-07 | REC-SCE-06 | Directe |
| AVE-01 | Aucun | **Absente** |
| AVE-02 | Aucun | **Absente** |
| AVE-03 | Aucun | **Absente** |
| REC-01 | REC-SCE-07 | Directe |
| REC-02 | REC-SCE-07 | Directe |
| REC-03 | REC-SCE-07 | Directe |
| REC-04 | REC-SCE-07 | Directe |
| ENR-01 | REC-SCE-08 | Directe |
| ENR-02 | REC-SCE-08 | Directe |
| ENR-03 | REC-SCE-08 | Directe |
| COM-01 | REC-SCE-14 | Directe |
| COM-02 | REC-SCE-14 | Directe |
| COM-03 | REC-SCE-14 | Partielle |
| COM-04 | REC-SCE-01 · REC-SCE-14 | Partielle |
| NOT-01 | Aucun | **Absente** |
| NOT-02 | Aucun | **Absente** |
| CON-01 | REC-SCE-14 | Directe |
| CON-02 | REC-SCE-14 | Directe |
| CON-03 | REC-SCE-14 | Directe |
| IA-01 | Aucun | **Absente** |
| IA-02 | Aucun | **Absente** |
| IA-03 | Aucun | **Absente** |
| IA-06 | Aucun | **Absente** |
| IA-07 | Aucun | **Absente** |
| SEC-01 | REC-SCE-14 | Directe |
| SEC-02 | REC-SCE-04 · REC-SCE-14 | Directe |
| SEC-03 | REC-SCE-14 | Directe |
| SEC-04 | REC-SCE-14 | Partielle |
| SEC-05 | REC-SCE-14 | Partielle |
| SEC-06 | Aucun | **Absente** |
| SEC-07 | REC-SCE-14 | Partielle |
| SEC-10 | Aucun | **Absente** |
| SEC-11 | Aucun | **Absente** |
| SEC-12 | REC-SCE-14 | Partielle |
| NFR-01 | Aucun | **Absente** |
| NFR-02 | Aucun | **Absente** |
| NFR-03 | Aucun | **Absente** |
| NFR-04 | Aucun (test de charge requis) | **Absente** |
| NFR-05 | Aucun (dépend du cadrage — PC-01) | **Absente** |
| NFR-06 | Aucun | **Absente** |
| NFR-10 | Aucun | **Absente** |
| NFR-11 | Aucun | **Absente** |
| ERR-01 | Aucun | **Absente** |
| ERR-02 | Aucun | **Absente** |
| ERR-03 | REC-SCE-14 | Partielle |
| ACR-01 | REC-SCE-14 | Partielle |

---

## Partie 4 — Synthèse de la couverture

> Recompte intégral ligne par ligne de la colonne « Couverture » de la Partie 3 (28 août 2026) — aucun report des anciens totaux.

| Couverture | Nombre d'exigences Must |
|-----------|------------------------|
| **Directe** | 86 |
| **Partielle** | 21 |
| **Absente** | 43 |
| **Total Must** | **150** |

> **Note sur le total** : La matrice de recette couvre l'intégralité des **150 exigences Must** recensées dans `01-matrice-exigences.md` (récapitulatif par priorité, recompté). 86 sont directement vérifiées par un scénario du CDC §22, 21 le sont partiellement, et 43 n'ont **aucun** scénario de recette dans le CDC. Conformément à la consigne de traçabilité : l'absence de scénario CDC ne rend pas ces 43 exigences facultatives — elles devront recevoir un Test Plan lors de `/speckit.specify`.

### Groupes d'exigences Must sans scénario de recette CDC

Les groupes suivants ont une couverture entièrement ou majoritairement **absente** et nécessiteront la création de scénarios de test dans Spec Kit :

| Groupe | Exigences Must sans couverture directe |
|--------|----------------------------------------|
| Cycle de vie (CYC) | CYC-01, CYC-02, CYC-03, CYC-04, CYC-05 |
| Messages internes (MSG) | MSG-02, MSG-03, MSG-05 |
| Disponibilité / Affectation (DIS, AFF) | DIS-03, AFF-07 |
| Priorités (PRI) | PRI-01, PRI-02, PRI-03, PRI-04 |
| Comptes / Multi-utilisateurs (CPT, ENT) | CPT-01, CPT-05, ENT-01, ENT-02 |
| Avoir / Exonération (AVE) | AVE-01, AVE-02, AVE-03 |
| Notifications (NOT) | NOT-01, NOT-02 |
| Assistant IA (IA) | IA-01, IA-02, IA-03, IA-06, IA-07 |
| Sécurité (SEC) | SEC-06, SEC-10, SEC-11 |
| Non fonctionnelles (NFR) | NFR-01 à NFR-06, NFR-10, NFR-11 |
| Gestion des erreurs (ERR) | ERR-01, ERR-02 |
| Unicité (UNI) | UNI-03 |
| Actions de masse (MAS) | MAS-02 *(conditionné à MAS-01, Should)* |

---

*Document de pré-analyse. Les scénarios de recette CDC sont reproduits tels quels sans modification ni ajout d'exigences. Les tests complémentaires seront créés lors de la phase /speckit.specify.*
