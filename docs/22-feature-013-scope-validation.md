# FEATURE 013 — SCOPE VALIDATION GATE

**Auteur** : Scrum Master & Product Owner Technique CIT Digital Platform  
**Date** : 2026-09-01  
**Baseline CDC d'entrée** : **164 / 188 exigences conformes (87,23 %)**  
**Commit de référence** : `cbc42bddd17734d47893bd730834c8bd05a94120` (F012 CLOSED)

---

## 1. ÉVALUATION INDIVIDUELLE DES 4 EXIGENCES CANDIDATES

### 1.1 ERR-01 — Système de brouillons et continuité de saisie
- **Texte CDC** : *« Aucune perte de données saisies en cas d'incident lors d'une soumission (brouillon ou reprise de saisie). »*
- **Priorité** : **Must**
- **Acteurs concernés** : Clients externes (Import, Export, Réclamations, Enrôlement).
- **Problème métier résolu** : En cas de déconnexion inopinée, de fermeture accidentelle du navigateur, de timeout ou d'incident réseau lors du remplissage d'un formulaire complexe (multi-BL, multi-conteneurs, déclarations), le client ne perd pas ses données et peut restaurer immédiatement sa saisie.
- **Périmètre applicatif** : Formulaires majeurs de la plateforme (`/billing-import/soumettre`, `/billing-export/soumettre`, `/customer-complaint/nouvelle-reclamation`, `/enrolement/nouveau`).
- **Modèle de données & Persistance** :
  - Entité `BrouillonSaisie` (`Id`, `UtilisateurId`, `SocieteId`, `TypeDemarche`, `DonneesJson`, `DateCreationUtc`, `DateDerniereModificationUtc`).
  - Sauvegarde automatique (auto-save périodique / à la saisie) et sauvegarde explicite "Enregistrer le brouillon".
  - Restauration proactive à l'ouverture du formulaire si un brouillon existe, avec option d'abandon / suppression.
  - Suppression automatique lors de la soumission définitive réussie.
  - Cloisonnement multi-tenant strict (`UtilisateurId` + `SocieteId`).
- **Migration EF Core** : **OUI** (1 migration pour la table `BrouillonsSaisie`).
- **Bloqueurs / PC** : Aucun.
- **Verdict** : **GO (Must prioritaire)**.

---

### 1.2 NFR-08 — Architecture ouverte et exposition d'API sécurisées
- **Texte CDC** : *« Architecture ouverte permettant à CIT d'ajouter des intégrations API si elle le décide ultérieurement. »*
- **Priorité** : **Should**
- **Acteurs concernés** : Partenaires logistiques, transitaires, systèmes tiers (IPAKI, douanes), intégrateurs.
- **Problème métier résolu** : Permettre l'interopérabilité de la plateforme CIT avec des systèmes externes sécurisés sans passer obligatoirement par l'interface web Blazor.
- **État existant** : L'application web Blazor Server n'expose actuellement aucun contrôleur API REST ni documentation Swagger/OpenAPI.
- **Périmètre à livrer** :
  - Activation d'Endpoints API Explorer et Swagger / OpenAPI (`Swashbuckle.AspNetCore` / `Microsoft.AspNetCore.OpenApi`).
  - Façade d'API REST sécurisée (`/api/v1/tickets/{numero}`, `/api/v1/tickets/statut`, `/api/v1/dossiers/{reference}`, `/api/v1/simulations/calculer`).
  - Sécurité et autorisation des API (respect strict du cloisonnement multi-tenant CPT-04 et du RBAC SEC-03).
  - Documentation interactive OpenAPI générée automatiquement (`/swagger`).
- **Migration EF Core** : NON (0 migration).
- **Bloqueurs / PC** : Aucun.
- **Verdict** : **GO (Architecture ouverte documentée)**.

---

### 1.3 SIM-11 — Contrôles de cohérence et messages d'aide à la simulation
- **Texte CDC** : *« Contrôles de cohérence à la saisie (ex. taille et type reconnus, catégorie compatible avec le sens) avec messages d'aide. »*
- **Priorité** : **Could**
- **Acteurs concernés** : Clients simulant des factures proforma, agents de facturation.
- **Problème métier résolu** : Guider l'utilisateur en temps réel lors de la configuration de conteneurs dans la simulation proforma (ex: incompatibilité sens Export / franchises spécifiques, conformité des dimensions 20'/40' avec les types Dry/Reefer, alertes contextuelles).
- **État existant** : Le moteur de tarification F005 calcule les montants, mais les validations croisées interactives avec messages d'aide contextuels n'étaient pas certifiées.
- **Migration EF Core** : NON (0 migration).
- **Bloqueurs / PC** : Aucun.
- **Verdict** : **GO (Clôture Tarification)**.

---

### 1.4 ENR-05 — Renouvellement de badges transporteur avec rappel automatique
- **Texte CDC** : *« Gestion du renouvellement des badges avec rappel automatique avant expiration. »*
- **Priorité** : **Could**
- **Acteurs concernés** : Transporteurs, chauffeurs, agents de sécurité Gate.
- **Problème métier résolu** : Anticiper l'expiration des badges d'accès portuaire et permettre une démarche de renouvellement simplifié sans recommencer l'enrôlement complet depuis zéro.
- **Résolution de CLAR-ENR-07** : La durée de validité du badge (365 jours par défaut) est paramétrable. La commande `RenouvelerBadgeCommand` initialise le renouvellement, et `RappelerExpirationBadgesCommand` identifie les badges proches de l'échéance (J-30) pour émettre l'événement `RappelExpirationBadge` via F009.
- **Migration EF Core** : NON (le modèle `Enrolement` dispose déjà des champs nécessaires ou extension mineure).
- **Bloqueurs / PC** : Aucun (clarification résolue par paramétrage).
- **Verdict** : **GO (Clôture Enrôlement)**.

---

## 2. ANALYSE DES OPTIONS DE SCOPE

| Critère | Option A : ERR-01 Seul | Option B : ERR-01 + NFR-08 | Option C : ERR-01 + NFR-08 + SIM-11 + ENR-05 (Recommandée) |
|:---|:---:|:---:|:---:|
| **Nom** | Continuité de saisie & Brouillons | Continuité & Façade API | Continuité Opérationnelle, Brouillons, API & Démarches |
| **Exigences** | ERR-01 | ERR-01, NFR-08 | ERR-01, NFR-08, SIM-11, ENR-05 |
| **Must / Should / Could** | 1 / 0 / 0 | 1 / 1 / 0 | 1 / 1 / 2 |
| **Gain CDC Maximal** | **+1** | **+2** | **+4** |
| **Baseline Projetée** | 165 / 188 (87,77 %) | 166 / 188 (88,30 %) | **168 / 188 (89,36 %)** |
| **Effort Estimé** | 2 jours | 3 jours | 4 jours |
| **Complexité / Risque** | Très faible | Faible | Maîtrisé |
| **Migrations requises** | 1 migration | 1 migration | 1 migration |
| **Cohérence Globale** | Forte (mono-sujet) | Forte (socle & intégration) | **Forte (continuité de parcours & interopérabilité)** |
| **Score Scrum /100** | 78 / 100 | 86 / 100 | **92 / 100** |

---

## 3. COHÉRENCE MÉTIER ET TECHNIQUE DU SCOPE F013

Le scope de l'**Option C** réunit les démarches de résilience de saisie, d'ouverture inter-systèmes et de complétion des démarches client :
1. **Résilience utilisateur (ERR-01, SIM-11)** : L'utilisateur bénéficie d'une saisie assistée sans perte de données, tant sur les formulaires de soumission lourds que sur les calculs de simulation tarifaire.
2. **Interopérabilité moderne (NFR-08)** : La plateforme s'ouvre aux intégrations externes par API REST documentées Swagger/OpenAPI.
3. **Cycle de vie usager (ENR-05)** : Le renouvellement annuel de badge boucle le parcours transporteur initié dans F008.

Toutes ces exigences sont **100% autonomes**, ne dépendent d'aucun fournisseur externe non contracté, et ne sont bloquées par aucun point à clarifier (PC-01, PC-05, PC-13, PC-14, PC-16, PC-18).

---

## 4. DÉFINITION OF DONE (DoD) CIBLE F013

1. **ERR-01** :
   - Table `BrouillonsSaisie` créée via EF Core migration.
   - Composant de gestion de brouillons (sauvegarde automatique, restauration, suppression après soumission).
   - Validation sur les formulaires Import, Export, Réclamations et Enrôlement.
   - Tests unitaires et d'intégration couvrant la reprise de session et le cloisonnement CPT-04.
2. **NFR-08** :
   - Endpoints REST `/api/v1/...` fonctionnels avec sérialisation JSON propre.
   - Interface Swagger UI active et documentée (`/swagger`).
   - Authentification et contrôle d'habilitation vérifiés par tests HTTP.
3. **SIM-11** :
   - Règles de validation croisée interactives dans la simulation proforma.
   - Messages d'aide contextuels explicites.
   - Tests unitaires `SimCert11ValidationsTests`.
4. **ENR-05** :
   - Commande de renouvellement de badge avec traçabilité.
   - Détection des badges proches de l'expiration et notification de rappel via F009.
   - Tests unitaires `EnrCert05RenouvellementTests`.
5. **Critères Qualité** :
   - 0 régression sur les 867 tests existants.
   - 0 avertissement de compilation.
   - 0 pending EF model changes.

---

## 5. RECOMMANDATION FINALE

- **Option retenue** : **Option C** (ERR-01, NFR-08, SIM-11, ENR-05).
- **Gain maximal attendu** : **+4 exigences**.
- **Projection CDC post-F013** : **168 / 188 exigences conformes (89,36 %)**.
- **Statut** : **READY FOR /speckit.specify**.
