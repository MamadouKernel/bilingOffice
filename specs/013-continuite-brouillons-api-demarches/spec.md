# SPECIFICATION ET CLARIFICATIONS FONCTIONNELLES — FEATURE 013

**Feature** : Feature 013 — Continuité Opérationnelle, Brouillons de Saisie, Exposition d'API & Clôture Démarches  
**Statut** : CLARIFIED  
**Date** : 2026-09-01  
**Baseline CDC d'entrée** : **164 / 188 exigences conformes (87,23 %)**  
**Périmètre fonctionnel** : **ERR-01** (Must), **NFR-08** (Should), **SIM-11** (Could), **ENR-05** (Could)  
**Gain maximal attendu** : **+4 exigences**  
**Projection cible** : **168 / 188 (89,36 %)** *(non acquise avant Closure Review)*  

---

## 1. BASELINE ET CADRAGE CONTRACTUEL

### 1.1 Baseline technique
- **Build** : 0 erreur, 0 avertissement.
- **Pending EF Model Changes** : 0.
- **Tests** : 867 / 867 verts (Domain: 158, Application: 447, Infrastructure: 83, Web: 179).
- **Commit HEAD** : `cbc42bddd17734d47893bd730834c8bd05a94120` (F012 CLOSED).

### 1.2 Périmètre des 4 exigences CDC
| ID | Prio | Texte contractuel exact CDC | Module CDC | Statut CDC |
|:---|:---:|:---|:---|:---:|
| **ERR-01** | **Must** | *« Aucune perte de données saisies en cas d'incident lors d'une soumission (brouillon ou reprise de saisie). »* | 21. Gestion des erreurs et continuité | ABSENTE |
| **NFR-08** | **Should** | *« Architecture ouverte permettant à CIT d'ajouter des intégrations API si elle le décide ultérieurement. »* | 26. Risques projet et mesures de maîtrise | ABSENTE |
| **SIM-11** | **Could** | *« Contrôles de cohérence à la saisie (ex. taille et type reconnus, catégorie compatible avec le sens) avec messages d'aide. »* | 11.5 Simulation de facturation proforma | DIFFÉRÉE |
| **ENR-05** | **Could** | *« Gestion du renouvellement des badges avec rappel automatique avant expiration. »* | 11.7 Enrôlement transporteur et chauffeur | DIFFÉRÉE |

---

## 2. CLARIFICATIONS DÉTAILLÉES PAR EXIGENCE

### 2.1 ERR-01 — Continuité de saisie & Résilience des soumissions (Must)

#### A. Périmètre des formulaires éligibles
Le périmètre F013 couvre les formulaires de démarches majeures de la plateforme :
1. **Demande Import** (`/billing-import/soumettre`)
2. **Demande Export** (`/billing-export/soumettre`)
3. **Réclamation Client** (`/customer-complaint/nouvelle-reclamation`)
4. **Enrôlement Transporteur / Chauffeur** (`/enrolement/nouveau` ou composant de soumission associé)

#### B. Matrice des comportements contractuels
| Scénario / Comportement | Exigé par le CDC ? | Comportement spécifié |
|:---|:---:|:---|
| **Sauvegarde automatique** | **OUI** | Préservation transparente des champs au fil de la saisie sans obligation d'action utilisateur. |
| **Sauvegarde explicite / manuelle** | **OUI** | Bouton « Enregistrer le brouillon » permettant de quitter volontairement la démarche. |
| **Reprise après rechargement / fermeture** | **OUI** | À la réouverture, détection du brouillon et proposition de restauration immédiate. |
| **Reprise après expiration de session** | **OUI** | Après réauthentification, la saisie en cours reste accessible. |
| **Reprise après incident réseau** | **OUI** | En cas d'erreur HTTP/serveur lors de la soumission, les champs restent peuplés sans perte. |
| **Multi-device / Mobilité** | **À ARBITRER AU PLAN** | La synchronisation cross-device dépend du mode de persistance retenu (serveur vs local/hybride). |

#### C. Politique de purge et cycle de vie
1. **Purge sur succès** : Dès que la soumission finale aboutit à la création effective du ticket, le brouillon est automatiquement effacé.
2. **Abandon volontaire** : L'utilisateur peut supprimer explicitement son brouillon pour réinitialiser le formulaire.
3. **Durée de conservation** : Si persistance serveur, purge automatique des brouillons inactifs selon délai de rétention configurable (décision de plan).

#### D. Gestion des pièces jointes
- Les métadonnées des pièces jointes (noms de fichiers, types, tailles) sont conservées dans le brouillon.
- Les flux binaires non encore téléversés peuvent nécessiter une re-sélection en cas de rechargement complet du navigateur si aucun upload temporaire serveur n'est retenu.

#### E. Cloisonnement (CPT-04 / SEC-03)
- Un brouillon est strictement rattaché à l'`UtilisateurId` et à la `SocieteId` de son auteur.
- Aucun utilisateur d'une autre société ni aucun agent non habilité ne peut accéder aux brouillons d'un tiers.

---

### 2.2 NFR-08 — Architecture ouverte et exposition d'API sécurisées (Should)

#### A. Clarification de la portée
F013 fournit la démonstration concrète de l'architecture ouverte par :
1. La mise à disposition d'une **façade d'API sécurisée** exposant les opérations de consultation clés :
   - Consultation de l'état d'un ticket (par numéro de ticket ou numéro BL).
   - Consultation synthétique d'un dossier client.
   - Exécution programmatique du calcul de simulation tarifaire proforma.
2. La mise à disposition d'une **documentation de contrat d'interface formelle** consommable par des systèmes partenaires.
3. Le respect strict de l'authentification et du cloisonnement multi-tenant (CPT-04) sur tous les endpoints exposés.

#### B. Découplage technique (réservé à `plan.md`)
La spécification impose le **contrat de service**, la sécurité et la documentation. Le choix d'implémentation (ex: ASP.NET Core Minimal APIs / Controllers, Swagger/OpenAPI, format JSON) est laissé au plan technique.

---

### 2.3 SIM-11 — Contrôles de cohérence à la saisie de simulation (Could)

#### A. Liste stricte des contrôles contractuels du CDC
| Réf Contrôle | Entrée | Règle de cohérence CDC | Type | Message d'aide attendu |
|:---|:---|:---|:---:|:---|
| **SIM-CTRL-01** | Taille + Type conteneur | Vérification que le type (Dry, Reefer) est compatible avec la taille (20', 40') selon le référentiel tarifaire. | Avertissement | *« Type ou taille de conteneur non reconnu dans la grille tarifaire active. »* |
| **SIM-CTRL-02** | Catégorie + Sens trafic | Cohérence entre le sens sélectionné (Import / Export) et les règles applicables à la catégorie de marchandise. | Avertissement | *« La catégorie sélectionnée nécessite une vérification des conditions spécifiques au sens {Sens}. »* |
| **SIM-CTRL-03** | Dates de séjour | Date d'enlèvement prévisionnel postérieure ou égale à la date d'arrivée. | Bloquant | *« La date d'enlèvement prévisionnel ne peut pas être antérieure à la date d'arrivée. »* |
| **SIM-CTRL-04** | Branchement frigorifique | Branchement applicable uniquement aux conteneurs Reefer. | Avertissement | *« Le branchement frigorifique est réservé aux conteneurs sous température dirigée (Reefer). »* |

*Aucun seuil arbitraire ni nouvelle règle tarifaire non sourcée n'est introduit dans F013.*

---

### 2.4 ENR-05 — Renouvellement de badge et rappel automatique (Could)

#### A. Résolution de CLAR-ENR-07 (Durée et Délais)
- **Durée de validité** : Paramétrable dans la configuration / référentiel métier (valeur technique de démonstration configurable, aucune valeur rigide en dur).
- **Délai de rappel** : Paramétrable dans la configuration (anticipation configurable avant la date d'expiration).
- **Calcul de la date d'expiration** : `DateExpirationUtc = DateDelivranceUtc + DureeValiditeParametree`.

#### B. Mécanisme de rappel
- Détection des badges arrivant à expiration sous le délai de préavis configuré.
- Émission de l'événement de rappel via le socle événementiel F009 (`EvenementSocle` / `JournalEvenements`).
- Distribution automatique sur les canaux configurés (InApp / Email selon préférences COM-05/06).
- Aucun SMS ni WhatsApp réel tant que PC-16 n'est pas instruit.

#### C. Parcours de renouvellement
- Action de demande de renouvellement rattachée au dossier d'enrôlement initial.
- Création d'un ticket de renouvellement soumis à l'instruction des agents Gate.
- Traçabilité complète du cycle de vie du badge dans le journal d'audit.
- Le traitement physique (impression badge matériel) demeure hors application.

---

## 3. USER STORIES MÉTIER

### US1 — Continuité de saisie sur incident (ERR-01)
**En tant que** utilisateur saisissant une démarche sur la plateforme CIT,  
**Je veux** que mes saisies soient préservées en cas d'interruption inopinée,  
**Afin de** reprendre ma démarche sans perte de données.

### US2 — Interopérabilité par API sécurisée (NFR-08)
**En tant que** système tiers partenaire ou intégrateur,  
**Je veux** accéder aux services de la plateforme via des API sécurisées et documentées,  
**Afin d**'automatiser le suivi de mes opérations portuaires.

### US3 — Contrôles de cohérence et aide à la simulation (SIM-11)
**En tant que** client effectuant une simulation proforma,  
**Je veux** être alerté des incohérences de saisie par des messages d'aide contextuels,  
**Afin de** configurer correctement mes conteneurs.

### US4 — Renouvellement de badge et rappel d'échéance (ENR-05)
**En tant que** transporteur ou chauffeur disposant d'un badge portuaire,  
**Je veux** recevoir une alerte avant son expiration et pouvoir initier son renouvellement,  
**Afin de** conserver mes accès sans rupture.

---

## 4. FAMILLES DE TEST-SPEC

| Famille TEST-SPEC | Exigence CDC | Nature du contrôle | Précondition | Résultat Attendu | Résultat Interdit |
|:---|:---:|:---|:---|:---|:---|
| **`ERR-CERT-01`** | **ERR-01** | Résilience de saisie et brouillon | Saisie en cours, incident simulé (interruption/rechargement) | Données restaurées fidèlement à la réouverture | Perte des champs saisis, accès à un brouillon tiers |
| **`NFR-CERT-08`** | **NFR-08** | Exposition d'API sécurisées | Appelant authentifié et habilité | Données retournées selon contrat, documentation disponible, 401/403 si non autorisé | Fuite de données hors société, API non protégée |
| **`SIM-CERT-11`** | **SIM-11** | Contrôles de cohérence simulation | Saisie de paramètres de simulation | Incohérences détectées avec messages d'aide clairs | Traitement silencieux d'incohérences, blocage sans explication |
| **`ENR-CERT-05`** | **ENR-05** | Rappel et renouvellement badge | Badge existant approchant de l'expiration | Émission de la notification de rappel, démarche de renouvellement opérable | Expiration silencieuse sans alerte |

---

## 5. CLASSIFICATION DES DÉCISIONS

| ID Décision | Question / Sujet | Décision validée | Source | Échéance d'application |
|:---|:---|:---|:---|:---:|
| **DEC-013-01** | Périmètre des formulaires ERR-01 | Limité aux formulaires de soumission majeurs (Import, Export, Réclamation, Enrôlement) | CDC Section 21 | AVANT PLAN |
| **DEC-013-02** | Mode de persistance des brouillons | À comparer et arbitrer au plan technique (serveur, navigateur, hybride) | Architecture | AVANT PLAN |
| **DEC-013-03** | Façade API NFR-08 | Exposer statuts tickets, dossiers et calcul simulation avec sécurité et documentation | CDC Section 26 | AVANT PLAN |
| **DEC-013-04** | Règles de validation SIM-11 | Strictement limitées aux contrôles CDC (taille/type, sens/catégorie, dates, Reefer) | CDC Section 11.5 | AVANT IMPLEMENTATION |
| **DEC-013-05** | Durée de validité badge ENR-05 | Paramétrable sans valeur hardcodée dans le code | CDC Section 11.7 | AVANT IMPLEMENTATION |
| **DEC-013-06** | Canal de rappel ENR-05 | Distribution via socle F009 (InApp / Email) sans SMS/WhatsApp réel (PC-16) | CDC Section 14 | AVANT RECETTE |
| **DEC-013-07** | Valeurs finales d'exploitation badge | Figer la durée de validité cible (ex: 1 an) dans la configuration de production | Exploitation CIT | AVANT PRODUCTION |

---

## 6. MIGRATION ET ANTI-SCOPE

- **Migration de base de données** : **0 MIGRATION CRÉÉE LORS DE LA PHASE SPECIFY / CLARIFY**.
- **Anti-Scope strict** :
  - Zéro passerelle SMS / WhatsApp réelle (PC-16).
  - Zéro module IA (IA-01..07 — PC-18).
  - Zéro exigence d'exploitation ou de sécurité externe (SEC-02, SEC-07, NFR-07).
  - Zéro régression sur les 867 tests existants.
