# Analyse Exhaustive du Cahier des Charges
## Plateforme Digitale de Gestion des Parcours et Demandes Clients — CIT

> **Document** : `docs/00-analyse-cdc.md`  
> **Référence CDC** : CIT-CIV-DSI-RFP-XXXX v1.0 — 12 août 2026  
> **Analysé le** : 28 août 2026  
> **Révisé le** : 28 août 2026 (révision post-audit — corrections appliquées)  
> **Statut** : Pré-analyse — en attente de validation métier  
> **Aucune implémentation ne commence avant validation de cette analyse.**

---

## 1. Contexte métier

Côte d'Ivoire Terminal (CIT) est un terminal portuaire dont le **Billing Office** gère les
formalités import/export : contrôle documentaire BL/bookings, facturation, paiement,
délivrance des documents de sortie (Bon de Sortie, BADT, BEE).

Le parcours actuel est **entièrement physique** : guichets successifs Contrôle BL →
Facturation → Caisse → Contrôle Documentaire, liasse papier, files d'attente, échanges
dispersés et traçabilité limitée.

Le projet crée un **portail digital unique** pour dématérialiser ces parcours tout en
garantissant la **continuité digital/présentiel**. Le portail est **volontairement autonome** :
aucune interface automatisée avec les systèmes existants (IPAKI, IRIS, SydamWorld, IES,
plateformes de paiement, Douane, GED).

---

## 2. Principes directeurs (non négociables)

| # | Principe | Traduction technique |
|---|----------|---------------------|
| P1 | **Le ticket est central** | Tout traitement passe par un ticket unique numéroté |
| P2 | **Workflow par défaut, non rigide** | Circuit nominal paramétrable + dérogations tracées |
| P3 | **Fonctionnement autonome** | Zéro interfaçage automatique avec les SI métiers |
| P4 | **Rôles et champs réservés** | RBAC fin jusqu'au niveau du champ (contexte traitement parallèle IES — IMP-40, SEC-02) |
| P5 | **Tout est tracé et mesuré** | Journal d'audit inviolable + mesure des temps par étape |
| P6 | **Communication proactive** | Notification multicanale à chaque événement significatif |
| P7 | **Paramétrable sans développement** | Files, SLA, alertes, catégories, modèles administrables |
| P8 | **Continuité digital/présentiel** | Le N° de ticket suffit pour reprendre au guichet |

---

## 3. Périmètre

### 3.1 Inclus

- Portail web client responsive (création de compte, authentification, profil typé)
- **Module 1 — Billing Office** : IMPORT, EXPORT, Simulation Proforma, Avoir/Exonération
- **Module 2 — Customer Complaint** : réclamations et assistance, catégories paramétrables
- **Module 3 — Enrôlement Transporteurs** : dossier à distance, instruction Gate
- **Socle transverse** : tickets, files, statuts, chat, GED, liaisons, notifications, archives
- Notifications multicanales : e-mail, WhatsApp, SMS, portail
- Back-office agents par rôle
- Administration fonctionnelle complète
- Tableaux de bord + journal d'audit
- Assistant virtuel IA (FAQ, orientation, escalade)
- Prise de rendez-vous (continuité digital/physique)

### 3.2 Hors périmètre (explicitement exclus)

| Exclusion | Justification |
|-----------|--------------|
| Interfaces automatisées avec IPAKI, IRIS, SydamWorld, IES, GED, Douane | Fonctionnement autonome voulu |
| Paiement en ligne débité par le portail | Paiement externe (Wave, banque agréée) |
| Édition automatique des documents métiers (facture, BS, BADT, BEE) | Produits dans les SI actuels, déposés manuellement par agents |
| Intégration automatique du système de file d'attente physique | Évolution future possible via API |
| Intégration manifestes Back Office import | Processus inchangé, non repris par la plateforme |

---

## 4. Acteurs et rôles

> **Important — Classification des rôles :**
> Les rôles sont classés selon leur statut dans le CDC (section 6 — Parties prenantes) :
> - **DEFINED_ROLE** : rôle explicitement listé et défini dans la section 6 du CDC
> - **FUNCTIONAL_RESPONSIBILITY** : responsabilité fonctionnelle nommée dans le texte, mais non listée comme rôle applicatif distinct
> - **ROLE_TO_CONFIRM** : rôle implicite dont l'existence comme profil applicatif distinct est à confirmer en phase de cadrage

### 4.1 Côté client (DEFINED_ROLE)

| Acteur | Type de compte | Workflow spécifique |
|--------|---------------|---------------------|
| **Client normal** | Société | Import/Export avec étape de paiement complète |
| **Client en compte** | Société | Import/Export sans paiement au ticket (règlement fin de mois) |
| **Client deposit** | Société | Import/Export avec lettrage Comptabilité Client |
| **Client IES** | Société | Traitement parallèle Manifeste + Contrôle Documentaire |
| **Transporteur/Chauffeur** | Personne physique | Enrôlement uniquement |
| **Utilisateur de société** | Rattaché à compte entreprise | Droits paramétrables (ses demandes ou toutes celles de la société) |
| **Administrateur de société** | Rattaché à compte entreprise | Gère les utilisateurs de sa société |

### 4.2 Côté CIT — agents et responsabilités

| Rôle | Statut CDC | File(s) traitée(s) | Responsabilités principales |
|------|-----------|-------------------|------------------------------|
| **Pilote Manifeste (Front Office)** | DEFINED_ROLE | Contrôle BL | Vérifie documents import : valider / rejeter / demander complément |
| **Back Office (Export)** | DEFINED_ROLE | Codification export | Crée et codifie le booking dans IPAKI (hors portail), trace le résultat |
| **Pilote Facturation Terre** | DEFINED_ROLE | Facturation | Contrôles IPAKI (hors portail), édite facture, la joint au ticket |
| **Comptable Client** | DEFINED_ROLE | Lettrage | Lettrage dans IRIS (hors portail), confirme dans le ticket |
| **Pilote Contrôle Documentaire** | DEFINED_ROLE | Contrôle Documentaire | Vérifie dossier, dépose BS/BADT/BEE, clôture le ticket |
| **Support Opérationnel** | DEFINED_ROLE | Customer Complaint | Instruit les réclamations, oriente vers les services concernés |
| **Agent Gate** | DEFINED_ROLE | Enrôlement | Vérifie dossier, demande compléments, valide, convoque pour photo/badge |
| **Superviseur** | DEFINED_ROLE | Vue transverse périmètre | Pilotage opérationnel, réaffectation, déblocage, supervision temps réel |
| **Administrateur fonctionnel** | DEFINED_ROLE | Administration | Comptes, rôles, paramétrage workflows, catégories, SLA |
| **Administrateur technique** | DEFINED_ROLE | Infrastructure | Sécurité, sauvegardes, journal d'audit |
| **Caisse/Finance** | ROLE_TO_CONFIRM | Validation paiement | Valide la preuve de paiement client avant passage au Contrôle Doc. (IMP-13) — rôle nommé entre parenthèses dans le CDC, non listé en section 6 — **cf. PC-03** |
| **Agent de guichet** | FUNCTIONAL_RESPONSIBILITY | Accueil physique | Reprend un dossier au guichet via N° de ticket (CON-01, CON-02) — responsabilité pouvant être assurée par un des rôles existants — **cf. PC-03** |
| **Intervenant Avoir/Exo** | ROLE_TO_CONFIRM | Validation Avoir/Exo | Instruit et décide des demandes d'avoir et d'exonération (AVE-02, AVE-03) — rôle non nommé dans le CDC — **cf. PC-03 et PC-15** |

### 4.3 Acteur non humain

| Acteur | Rôle |
|--------|------|
| **Assistant virtuel IA** | FAQ, orientation client, aide au remplissage, escalade vers opérateur humain |

---

## 5. Architecture fonctionnelle

```
┌─────────────────────────────────────────────────────────────────────┐
│                       PORTAIL CLIENT DIGITAL                         │
│                                                                       │
│  ┌────────────────┐  ┌────────────────┐  ┌────────────────────────┐ │
│  │   MODULE 1     │  │   MODULE 2     │  │       MODULE 3          │ │
│  │  Billing       │  │  Customer      │  │  Enrôlement             │ │
│  │  Office        │  │  Complaint     │  │  Transporteurs          │ │
│  │                │  │                │  │                          │ │
│  │  - IMPORT      │  │  - Réclamation │  │  - Dossier à distance   │ │
│  │  - EXPORT      │  │  - Assistance  │  │  - Instruction Gate     │ │
│  │  - Proforma    │  │  (catégories   │  │  - Photo / Badge        │ │
│  │  - Avoir/Exo   │  │   paramétr.)   │  │                          │ │
│  └────────────────┘  └────────────────┘  └────────────────────────┘ │
│                                                                       │
│  ┌───────────────────────────────────────────────────────────────┐   │
│  │                       SOCLE TRANSVERSE                         │   │
│  │  Tickets · Files · Statuts · Chat · GED · Liaisons/Dossiers   │   │
│  │  SLA · Alertes · Escalades · Notifications multicanales       │   │
│  │  Audit · Supervision · Administration · Reporting              │   │
│  │  Assistant IA · Rendez-vous · Continuité digital/présentiel   │   │
│  └───────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 6. Analyse détaillée par module

---

### 6.1 Socle de gestion des tickets (SOC / CYC / MSG)

**Couche** : Transverse à tous les modules.

#### Données principales du ticket

| Champ | Description |
|-------|-------------|
| Numéro unique | Communiqué au client (portail + e-mail) à la création |
| Module | Billing Office / Customer Complaint / Enrôlement |
| Type de demande | Import, Export, Avoir, Exonération, Réclamation, Enrôlement |
| Profil client | Normal, En compte, Deposit, IES |
| Statut | Selon workflow propre au module |
| Priorité | Référentiel paramétrable |
| File courante | Rôle/file en charge du ticket |
| Agent assigné | Opérateur ayant pris en charge |
| Dossier de rattachement | Regroupement multi-tickets |
| Liens vers autres tickets | Liaisons inter-tickets / inter-modules |
| Horodatages | Arrivée en file, prise en charge, début traitement, dernière activité, clôture |

#### Cycle de vie du ticket (états)

```
[CRÉÉ]
  → [EN ATTENTE DE PRISE EN CHARGE]   (ticket dans la file, non accepté)
  → [PRIS EN CHARGE / EN TRAITEMENT]  (opérateur a accepté)
  → [EN ATTENTE CLIENT]               (complément / document / action attendue)
       ↕ (le client répond → retour traitement)
  → [VALIDÉ / CODIFIÉ / REJETÉ / EN ATTENTE PAIEMENT / ...]
  → [CLÔTURÉ]
  → [ARCHIVÉ]                          (archivage automatique post-clôture)

Chemins alternatifs :
  - [ANNULATION LIBRE]           (avant traitement engageant, paramétrable)
  - [DEMANDE D'ANNULATION]       (si traitement engageant — décision agent habilité)
  - [SUGGESTION CLÔTURE ABSENCE RÉPONSE] → décision manuelle agent (jamais automatique)
  - [ROUVERT]                    (agent habilité, motif obligatoire, conserve numéro/historique)
```

#### Règles métier clés

| Réf. | Règle |
|------|-------|
| CYC-01 | Les étapes bloquant l'annulation libre sont **paramétrables par workflow** (non codées en dur) |
| CYC-04 | Relances automatiques paramétrables (nombre, cadence) → suggestion de clôture manuelle uniquement |
| CYC-05 | Jamais de suggestion de clôture après état engageant (paiement confirmé, document officiel produit) |
| MSG-02 | Notes internes CIT **jamais visibles du client** |
| MSG-05 | Une mention @Nom ne réaffecte pas le ticket |
| SOC-09 | Les archives sont consultables en permanence par le client et les agents habilités |

#### Couche concernée
`Domaine` · `Application` · `Exploitation`

---

### 6.2 Moteur de workflow (WKF / PEC / TRF / AFF / PRI / DIS / MAS)

**Couche** : Transverse à tous les modules.

#### Circuit nominal
Déterminé automatiquement par : **module + type de demande + profil client** (WKF-01).
Non rigide : les utilisateurs habilités peuvent déroger (WKF-02), avec traçabilité obligatoire (WKF-03).

#### Prise en charge (PEC)

```
Arrivée en file
  → ÉTAT: "En attente de prise en charge"    [SOC-12 — heure arrivée enregistrée]
  → Action: opérateur accepte                [PEC-02]
  → ÉTAT: "En traitement"
  Enregistré: { heure_arrivée, heure_pec, opérateur, début_traitement, dernière_activité } [PEC-03]
```

> **Note de dépendance** : PEC-01 à PEC-05 précisent le mécanisme décrit par SOC-12. SOC-12 est l'exigence fondatrice ; PEC-01-05 l'opérationnalisent.

#### Transfert vs Réaffectation

| Mécanisme | Portée | Qui peut l'initier |
|-----------|--------|---------------------|
| **Transfert de file** | Le ticket change de file/rôle | Opérateur habilité + Superviseur |
| **Réaffectation** | Le ticket change d'opérateur dans la même file | Opérateur habilité **sous réserve des droits configurés** (pas réservé au superviseur) + Superviseur |

Lors d'un transfert : le ticket **conserve son numéro, son dossier, ses documents, ses commentaires et son historique** (TRF-02). Le motif est **obligatoire** (TRF-03).

#### Matrice des transitions (valeurs illustratives CDC — à valider métier, cf. PC-07)

| File source | Destinations autorisées |
|------------|------------------------|
| Contrôle BL (Manifeste) | Facturation ; Contrôle documentaire |
| Facturation | Contrôle BL ; Contrôle documentaire |
| Contrôle documentaire | Facturation |
| Codification (Back Office export) | Facturation ; Contrôle documentaire |
| Comptabilité (lettrage) | Contrôle documentaire ; Facturation |

#### Modes d'affectation (paramétrables par file, sans développement)

| Mode | Priorité CDC |
|------|------------|
| Prise libre (opérateur prend un ticket dans sa file) | Must (AFF-01) |
| Affectation manuelle par superviseur | Must (AFF-02) |
| Round-Robin automatique | Should (AFF-03) — *dépend de DIS-01 (déduit : un opérateur indisponible ne doit pas recevoir de ticket)* |
| Distribution selon charge actuelle des agents | Should (AFF-04) |
| Distribution selon disponibilité des agents | Should (AFF-05) |
| Distribution selon compétence/habilitation | Should (AFF-06) |

#### Disponibilité opérateur

| Statut | Effet |
|--------|-------|
| Disponible | Peut recevoir des affectations automatiques |
| Indisponible | Aucune nouvelle affectation automatique |
| Absent | Idem ; ses tickets sont réaffectables en masse par le superviseur |

#### Priorités tickets (PRI)

- Attributée automatiquement (règles métier) ou modifiée par un habilité
- Peut influencer : ordre file, SLA applicable, alertes, escalades
- Le client **ne peut pas** s'auto-déclarer "Urgent/Critique" sauf processus métier spécifique (PRI-04)
- Toute modification manuelle est tracée : ancienne valeur, nouvelle valeur, auteur, date/heure, motif (PRI-03)

#### Actions de masse superviseur (MAS)

En masse sur une sélection de tickets : affecter, réaffecter, transférer de file, modifier priorité (MAS-01 — Should).  
Toute action de masse est tracée **ticket par ticket** (MAS-02 — Must).

> **Tension logique à noter** : MAS-02 (Must) présuppose que MAS-01 existe. MAS-02 est testable uniquement si MAS-01 (Should) est implémenté. Si MAS-01 est différé, MAS-02 doit être marqué « non applicable » dans la recette correspondante.

#### Couche concernée
`Domaine` · `Application` · `Infrastructure` (performance temps réel)

---

### 6.3 SLA et mesure des temps (TPS / SLA / ALE)

**Couche** : Transverse.

#### Mesure des temps par étape

| Durée mesurée | Définition |
|---------------|-----------| 
| **Temps d'attente (par étape)** | Arrivée en file → prise en charge |
| **Temps de traitement (par étape)** | Prise en charge → sortie de l'étape |
| **Temps d'attente client** | Durée en état "En attente client" |
| **Temps interne CIT** | Ticket sous responsabilité CIT |
| **Temps total parcours** | Création → clôture |

> **Règle clé (TPS-04)** : Le temps d'attente client n'est **pas imputé** à la performance de l'opérateur ni de la file. Les SLA internes peuvent être suspendus pendant cet état.

#### Structure d'un SLA

| Paramètre | Valeurs possibles |
|-----------|------------------|
| Périmètre | Module, type de demande, étape, file, priorité, catégorie client |
| Seuils | Information (ex. 50%), Alerte (ex. 80%), Dépassement (100%), Escalade |
| Suspension | Pendant "En attente client" |
| Horaires | Possibilité de calcul en heures ouvrées (Should — SLA-04) |

#### Alertes déclenchées (ALE-01)

- Ticket non pris en charge depuis X minutes
- Ticket sans activité depuis X minutes/heures
- SLA bientôt dépassé (seuil alerte)
- SLA dépassé → escalade paramétrable (notification superviseur, remontée priorité, réaffectation)
- Backlog file > seuil
- Volume anormal de tickets
- Opérateur indisponible avec tickets affectés

#### Couche concernée
`Domaine` · `Application` · `Exploitation` · `Infrastructure` (temps réel)

---

### 6.4 Notifications et communications (COM / NOT)

**Couche** : Transverse.

#### Canaux

| Canal | Priorité CDC |
|-------|------------|
| E-mail | Must |
| Notifications portail | Must |
| Au moins un canal mobile (WhatsApp et/ou SMS) | Must (COM-01) |

#### Règles

- Événements déclencheurs et modèles paramétrables par module/étape/canal (COM-03)
- Chaque notification comporte un lien vers le ticket ; l'accès au ticket est conditionnel à l'authentification (SEC-12, COM-02)
- Échec d'un canal : retry automatique + trace, sans bloquer les autres canaux (ERR-02)
- Préférences de canaux par client (Should — COM-05), sauf notifications obligatoires
- Relances automatiques paramétrables en attente d'action client (Could — COM-06)

> **Relation CYC-04 / COM-06** : CYC-04 (Must) définit le mécanisme de relances automatiques et la suggestion de clôture. COM-06 (Could) précise la dimension « canal de notification » de ces relances. Le mécanisme lui-même est Must (CYC-04) ; les canaux de notification des relances sont Could (COM-06). Les deux exigences sont complémentaires et non redondantes.

#### Centre de notifications portail

- État Lu / Non lu par notification
- Lien direct vers le ticket concerné
- Compteur de notifications non lues visible en permanence (Should — NOT-03)

#### Couche concernée
`Application` · `Infrastructure` (connecteurs WhatsApp/SMS) · `Sécurité` (liens authentifiés)

---

### 6.5 Gestion documentaire (SOC-07 / SEC-07 / ERR-03)

**Couche** : Transverse.

#### Règles

- Dépôt par le client ET par les agents
- Types de pièces **paramétrables par étape** et par parcours
- Formats acceptés et taille maximale **configurables**
- Contrôle obligatoire à l'upload : intégrité, type, taille, **analyse antivirale** (SEC-07)
- Documents produits (facture, BS, BADT, BEE) centralisés dans le ticket, déposés manuellement par les agents
- Chiffrement des documents stockés (SEC-04)

#### Documents métiers (déposés manuellement par les agents)

| Document | Étape | Module/Parcours |
|----------|-------|-----------------|
| Facture | Facturation | Import / Export |
| Bon de Sortie (BS) | Contrôle Documentaire | Import |
| BADT | Contrôle Documentaire | Import |
| BEE (Bon d'Entrée Export) | Contrôle Documentaire | Export |

#### Couche concernée
`Application` · `Sécurité` · `Infrastructure` (dimensionnement stockage — cf. PC-01)

---

### 6.6 Audit (SOC-13 / SEC-05)

**Couche** : Transverse.

> **Note de cohérence** : SOC-13 et SEC-05 décrivent le **même journal d'audit unique**. SOC-13 l'ancre comme exigence du socle ticket (must transverse à tous les modules). SEC-05 le qualifie comme exigence de sécurité (non modifiable). Ces deux entrées se complètent ; elles ne désignent pas deux journaux distincts.

#### Événements tracés dans le journal d'audit (non exhaustif)

- Connexion / déconnexion utilisateur
- Création, prise en charge, changement de statut d'un ticket
- Dépôt, téléchargement de document
- Décision (validation, rejet, demande de complément)
- Transfert / réaffectation (ticket par ticket, y compris en masse)
- Annulation / demande d'annulation / réouverture
- Modification de disponibilité d'un opérateur
- Actions administrateur (comptes, rôles, paramétrage)

#### Propriétés

- **Inviolable** (non modifiable — SEC-05)
- Contient : auteur + date/heure + détail pour chaque action
- Distinct de l'historique ticket (visible aux agents habilités)
- Consultable par l'administrateur technique

#### Couche concernée
`Sécurité` · `Exploitation` (conformité, investigation)

---

### 6.7 Authentification et autorisation (SEC)

**Couche** : Transverse.

| Mécanisme | Priorité |
|-----------|---------| 
| Authentification obligatoire pour tout accès | Must |
| Politique de robustesse des mots de passe | Must |
| Verrouillage après échecs répétés | Must |
| Récupération autonome de mot de passe | Must |
| RBAC (Role-Based Access Control) | Must |
| RBAC au niveau du champ | Must — **uniquement dans le contexte du traitement parallèle IES** (IMP-40, SEC-02) |
| Cloisonnement données client | Must |
| Cloisonnement données agent (ses files uniquement) | Must |
| Session expirante (durée paramétrable) | Must |
| Désactivation compte agent sans suppression historique | Must |
| HTTPS/TLS + chiffrement stockage | Must |
| Conformité ARTCI (données personnelles) | Must |
| Contrôle upload (type, taille, antivirus) | Must |
| Liens notifications → ticket après authentification uniquement | Must |
| Second facteur agents/admins | Should (SEC-08 — indépendant de CPT-07) |
| Cloisonnement environnements prod/recette | Should |
| Second facteur clients | Could (CPT-07 — indépendant de SEC-08) |

> **Note de dépendance** : SEC-08 (second facteur agents) et CPT-07 (second facteur clients) sont deux exigences **indépendantes**. SEC-08 ne dépend pas de CPT-07.

#### Couche concernée
`Sécurité` · `Application` · `Infrastructure`

---

### 6.8 Comptes clients et sociétés (CPT / ENT)

**Couche** : Application.

#### Modèle multi-utilisateurs

```
Société cliente (type : normal | en compte | deposit | IES)
  ├── Administrateur de société
  │     └── Gère : activation/désactivation, droits consultation/soumission
  └── Utilisateurs rattachés
        └── Visibilité : ses demandes uniquement OU toutes demandes de la société
```

#### Processus de création de compte

Soumission par le client (déclaration du type) → **validation manuelle par CIT** (admin fonctionnel) → activation.
Les informations et justificatifs exigés sont **paramétrables** (CPT-01).

#### Règles clés

| Réf. | Règle |
|------|-------|
| CPT-02 | Le type de client est déclaré par le client à la création et validé/confirmé par CIT ; il détermine **automatiquement** le workflow Billing Office |
| CPT-06 | Modification du type par admin fonctionnel CIT : tracée, sans effet rétroactif sur tickets en cours |
| ENT-03 | Cloisonnement selon les droits accordés par l'admin de société |
| ENT-04 | Journalisation des actions des administrateurs de société (Should) |

#### Couche concernée
`Domaine` · `Application` · `Sécurité`

---

### 6.9 Billing Office — IMPORT

**Couche** : Domaine · Application.

#### Tronc commun (IMP-01 à IMP-06)

```
Formulaire soumission client
  → Saisie : jusqu'à 5 BL + documents par BL
  → Vérification unicité BL actif (UNI-01)
  → Génération : 1 ticket / BL, regroupés dans un dossier
  → File Pilote Manifeste
      ├── Validé         → orientation selon type client
      ├── Rejeté         → motif obligatoire + notification client
      └── Complément     → client notifié → dépose éléments → retour file Manifeste
```

#### Contrôle d'unicité BL (UNI-01 à UNI-04)

| Règle | Description |
|-------|-------------|
| UNI-01 | Bloque si un ticket **actif** existe pour le même BL |
| UNI-02 | Autorise si le ticket existant est clôturé ou annulé |
| UNI-03 | Ne s'applique pas aux autres modules (Customer Complaint peut référencer le même BL) |
| UNI-04 | Règle équivalente sur numéro de booking pour l'EXPORT (Should — à valider métier) |

#### Variantes par type de client

| Étape | Normal | En compte | Deposit | IES |
|-------|--------|-----------|---------|-----|
| Contrôle BL | Manifeste | Manifeste | Manifeste | Manifeste (parallèle) |
| Post-validation | → Facturation | → Facturation | → Facturation | → Contrôle Doc (parallèle) |
| Facturation | Pilote Facturation | Pilote Facturation | Pilote Facturation | Via IES |
| Paiement | Client paie + joint preuve | Non (fin de mois) | Non (lettrage) | Via IES |
| Validation paiement | Caisse/Finance valide (*) | — | — | — |
| Lettrage | — | — | Comptable Client (IRIS) | — |
| Contrôle Documentaire | Pilote CD | Pilote CD (direct après facture) | Pilote CD (après lettrage) | Pilote CD (parallèle) |
| Documents remis | BS, BADT | BS, BADT, Facture | BS, BADT | Via IES |

> (*) Caisse/Finance : ROLE_TO_CONFIRM — cf. PC-03.

#### Traitement parallèle IES (IMP-40 à IMP-42)

- Manifeste et Contrôle Documentaire traitent **simultanément**
- **Champs réservés par rôle** (RBAC au niveau du champ — IMP-40 + SEC-02)
- Jointure obligatoire avant retour au client (les deux traitements doivent être terminés)
- Client finalise facturation/paiement/documents sur plateforme IES
- Ticket portail clôturé à la remise des documents

#### Statuts Billing Office IMPORT (paramétrables)

```
Créé → En cours de traitement → En attente client → Validé
     → Facturé → En attente paiement → En attente validation paiement
     → Payé confirmé → Clôturé
```

> Certains statuts sont sans objet selon la branche (ex. pas de statuts de paiement pour
> clients en compte et IES ; statut lettrage possible pour clients deposit).

---

### 6.10 Billing Office — EXPORT

**Couche** : Domaine · Application.

#### Spécificités export vs import

| Élément | Import | Export |
|---------|--------|--------|
| Objet traité | BL | Booking |
| Acteur contrôle initial | Pilote Manifeste | Back Office |
| Action spécifique facturation | — | Draft IN (simulation entrée plein export) |
| Document final | BS, BADT | BEE (Bon d'Entrée Export) |
| Contrôle unicité | Actif (UNI-01) | À valider (UNI-04 — Should, cf. PC-09) |
| Statut post-contrôle | Validé | Codifié |

#### Post-codification
Orientation selon le même modèle de variantes par type de client que l'import (EXP-03).

---

### 6.11 Simulation de Proforma

**Couche** : Domaine · Application · UX.

> **SIM-01** : La simulation est optionnelle — le client « peut » la lancer avant son parcours,
> « si nécessaire ». Elle n'est pas un pré-requis obligatoire de la soumission d'un ticket.

#### Données d'entrée

| Niveau | Champs |
|--------|--------|
| Dossier | Navire, voyage, dates, BL/booking, client, date arrivée navire, date enlèvement prévisionnel |
| Conteneur | Numéro, sens (Import Local/Transit/Export Local/Transit), taille (20"/40"), type (Dry/Reefer), poids (t), OOG (oui/non), dangereux (oui/non), catégorie marchandise |

#### Postes de facturation calculés

| Poste | Règle |
|-------|-------|
| Acconage | Grille tarifaire selon Sens + Taille + Catégorie de marchandise |
| Relevage | Tarif fixe selon même clé |
| Branchement frigorifique | Reefer uniquement : tarif/j × (séjour − franchise 2j) |
| Majoration lourd | 30% acconage si poids > seuil (15t/20", 26t/40") |
| Majoration dangereux | 50% acconage si déclaré dangereux |
| Majoration OOG | 50% acconage si hors gabarit |
| Stationnement | Par tranches hors franchise (11j import local, 21j import transit) |
| Répartition stationnement | CIT 30% / OIC 20% / PAA 50% |

#### Paramètres (tous administrables sans développement — SIM-06)

Grilles tarifaires · taux majorations · seuils de poids · franchises ·
tranches de stationnement · quotes-parts répartition · référentiels (sens, tailles, types, catégories).

#### Versionnement (SIM-07)
Les grilles sont versionnées avec une **date d'effet**. Une simulation référence la version de tarif appliquée.

#### Jeux de tests de référence (recette à passer au franc près — Annexe A du CDC)

| Cas | Sens | Taille | Type | Poids | OOG | Dang. | Cat. | Total XOF |
|-----|------|--------|------|-------|-----|-------|------|-----------|
| test01 | Import Transit | 20" | Dry | 100t | Oui | Oui | C4 | 199 129 |
| test02 | Import Local | 40" | Reefer | 200t | Non | Non | C1 | 2 054 980 |
| test03 | Export Local | 20" | Dry | 300t | Oui | Non | C2 | 245 500 |
| test04 | Export Transit | 40" | Reefer | 100t | Non | Oui | C5 | 2 053 400 |
| test06 | Import Local | 40" | Reefer | 300t | Non | Non | C3 | 2 245 300 |

> Hypothèse commune : durée de séjour 29 jours (arrivée 10/02, enlèvement 10/03).

---

### 6.12 Avoir et Exonération

**Couche** : Domaine · Application.

| Réf. | Règle |
|------|-------|
| AVE-01 | Chaque demande génère un ticket |
| AVE-02 | Workflow de validation propre à chaque type (étapes et intervenants paramétrables) |
| AVE-03 | Décision + documents déposés dans le ticket, client notifié |
| AVE-04 | La demande peut être liée au ticket de parcours concerné (Should) |

> **Point à clarifier** : circuit de validation interne Finance à formaliser (cf. **PC-15**).  
> **Acteur AVE-02/AVE-03** : le CDC ne nomme pas d'acteur précis pour l'instruction et la décision — cf. PC-03 (ROLE_TO_CONFIRM).

---

### 6.13 Customer Complaint

**Couche** : Domaine · Application · UX.

#### Flux

```
Client sélectionne catégorie → [CRÉÉ]
  → File Support Opérationnel
  → Instruit / demande complément / oriente vers service concerné
  → Réponse formalisée → [CLÔTURÉ]
  (+ enquête satisfaction si activée — Could)
```

#### Catégories paramétrables (exemples CDC)
Facturation · Assistance plateforme · Problème opérationnel · Sujet documentaire · Juridique · Autre

#### Règles d'orientation automatique
Par catégorie, la file ou le service destinataire est paramétrable (Should — REC-05).

---

### 6.14 Enrôlement Transporteurs

**Couche** : Domaine · Application · UX (mobile-first chauffeurs).

#### Flux

```
Chauffeur/Transporteur saisit dossier + dépose pièces (à distance)
  → [CRÉÉ] → File Agent Gate
  → Vérifie / demande complément / valide
  → Convocation pour photo/badge (déplacement physique requis)
  → Badge remis → [CLÔTURÉ]
```

#### Règles

| Réf. | Règle |
|------|-------|
| ENR-01 | Liste des informations et pièces est **paramétrable** |
| ENR-03 | Déplacement physique limité à la photo et la remise du badge |
| ENR-04 | Notifications SMS/WhatsApp prioritaires pour les chauffeurs |
| ENR-05 | Renouvellement de badge avec rappel avant expiration (Could) |

---

### 6.15 Prise de rendez-vous (CON)

**Couche** : Application · UX.

- Le client prend un RDV lorsque la présence physique est requise
- Le RDV est rattaché au ticket/dossier
- À l'arrivée, le client prend un ticket file d'attente physique ; son RDV lui donne accès au bureau de facturation
- La plateforme et le système de file d'attente existant restent **deux systèmes distincts** (CON-04)
- Une intégration API est une évolution future, pas un must actuel

> **Agent de guichet (CON-01, CON-02)** : FUNCTIONAL_RESPONSIBILITY. La capacité de recherche rapide d'un ticket au guichet peut être assurée par un des rôles agents existants — cf. PC-03.

---

### 6.16 Assistant IA (IA)

**Couche** : Application · Infrastructure.

| Réf. | Exigence |
|------|----------|
| IA-01 | Réponses en langage naturel, **en français** |
| IA-02 | Orientation client : choix du parcours, module, catégorie |
| IA-03 | Escalade vers opérateur humain **sans perte de contexte** (fil conservé dans le ticket) |
| IA-04 | Base de connaissances administrable (FAQ, procédures) — Should |
| IA-05 | Accessible depuis le portail et, si possible, depuis les canaux de messagerie configurés — Should |
| IA-06 | Identifie les situations nécessitant une intervention humaine |
| IA-07 | Le client peut demander une intervention humaine explicitement |

---

### 6.17 Espace client — Tableau de bord et écrans (ACR)

**Couche** : UX · Application.

#### Exigence ACR-01

| Réf. | Priorité | Exigence |
|------|----------|----------|
| ACR-01 | Must | Le tableau de bord client met clairement en évidence les actions attendues de sa part (« Mes actions requises »), distinctes de la simple liste des tickets ouverts (ex. : fournir un document, paiement attendu, rendez-vous à confirmer), chacune renvoyant au ticket concerné. |

**Source CDC** : Section 17.1 « Espace client ».

#### Autres écrans décrits (section 17 — non numérotés dans le CDC)

- Formulaire de soumission Billing Office : choix IMPORT/EXPORT, saisie multi-BL/booking (jusqu'à 5), dépôt documents par BL/booking, récapitulatif avant validation
- Détail d'un ticket : statut et frise d'avancement, documents, fil d'échanges, historique, actions disponibles selon l'étape
- Simulation de proforma ; demandes d'avoir/exonération ; création d'une réclamation ; suivi global d'un dossier

**Note** : Les écrans décrits en section 17.2 (espace agent) et 17.3 (administration) ne comportent pas d'exigence numérotée supplémentaire — ils illustrent les fonctionnalités déjà capturées dans les sections précédentes.

---

### 6.18 Supervision (SUP)

**Couche** : Application · Exploitation · Infrastructure.

#### Vue temps réel par ticket
Non affectés · Pris en charge · En cours · En attente client · Proches SLA · En dépassement SLA · Sans activité

#### Vue temps réel par file
Volume · Backlog · Ancienneté ticket le plus ancien · Délai moyen · Respect SLA

#### Vue par opérateur
Disponibilité/connexion · Tickets affectés · Tickets en cours · Charge · Dernière activité

#### Indicateurs (3 familles)

| Famille | Indicateurs principaux |
|---------|----------------------|
| **Processus** | Reçus, clôturés, backlog, délai moyen PEC, délai moyen traitement, délai moyen par étape, durée totale parcours, taux SLA, taux rejet, taux complément, taux transfert |
| **Files** | Volume entrant/sortant, backlog, ancienneté max, délai moyen, SLA |
| **Opérateur** | Tickets pris en charge/traités/clôturés, délai moyen PEC, délai moyen traitement, dernière activité |

#### Actions superviseur
Affecter un ticket non pris en charge · Réaffecter entre opérateurs · Transférer de file ·
Équilibrer la charge · Intervenir sur un ticket bloqué · Actions de masse · Recherche transverse

---

### 6.19 Administration fonctionnelle

**Couche** : Application · Domaine · Exploitation.

Tous les éléments suivants sont administrables **sans développement** :

| Domaine | Éléments |
|---------|---------|
| Utilisateurs | Comptes clients (validation, typage), comptes agents (rôles) |
| Organisation | Files, rôles, rattachements opérateurs/files |
| Workflow | Matrice transitions, statuts, libellés, étapes de blocage annulation |
| Affectation | Règles et modes d'affectation par file, priorités |
| SLA | Valeurs cibles, seuils, horaires ouvrés |
| Alertes/Escalades | Règles, seuils, destinataires, canaux |
| Documents | Types de pièces par étape, formats, taille max |
| Notifications | Modèles de messages, événements déclencheurs, canaux par événement |
| Catégories | Catégories de réclamation, catégories de marchandise |
| Simulation | Grilles tarifaires (versionnées), taux majorations, franchises, tranches |
| Motifs | Motifs de transfert, motifs d'annulation, motifs de rejet |
| Référentiels | Sens, tailles, types, catégories, niveaux de priorité |

---

### 6.20 Reporting

**Couche** : Application · Exploitation.

- Tableaux de bord temps réel (superviseur) + historiques
- Export indicateurs et listes de tickets : Excel, PDF (Could — SOC-14)
- Journal d'audit consultable (admin technique)
- Archives consultables (client + agents habilités)

---

### 6.21 Règles de conservation

| Donnée | Règle | Statut |
|--------|-------|--------|
| Tickets clôturés et archivés | Consultables en permanence (SOC-09) | Must — figé |
| Historique complet du ticket | Conservé après clôture | Must — figé |
| Documents déposés | Durée légale à valider | À décider (PC-14) |
| Journal d'audit | Durée légale à valider | À décider (PC-14) |
| Grilles tarifaires versionnées | Historique conservé (SIM-07) | Must — figé |
| Données personnelles | Conformité ARTCI, minimisation, droits des personnes | Must — figé |

---

## 7. Workflows : états et transitions

### 7.1 Import — Client Normal

```
Soumission → [CRÉÉ]
  → Pilote Manifeste : Valide / Rejette (motif) / Demande complément
  → [VALIDÉ]
  → Client demande facture → Pilote Facturation → [FACTURÉ]
  → Client paie (hors portail) + joint preuve → [EN ATTENTE PAIEMENT]
  → Rôle habilité (Caisse/Finance) valide → [EN ATTENTE VALIDATION PAIEMENT] → [PAYÉ CONFIRMÉ]
  → Pilote Contrôle Doc. → dépose BS + BADT → [CLÔTURÉ] → [ARCHIVÉ]
```

### 7.2 Import — Client En Compte

```
... (identique Normal jusqu'à [FACTURÉ])
  → Passage direct au Contrôle Documentaire (pas d'étape paiement)
  → Pilote CD dépose BS + BADT + Facture → [CLÔTURÉ]
```

### 7.3 Import — Client Deposit

```
... (identique Normal jusqu'à [FACTURÉ])
  → File Comptabilité Client → lettrage IRIS → confirme dans ticket
  → Contrôle Documentaire → dépose BS + BADT → [CLÔTURÉ]
```

### 7.4 Import / Export — Client IES (traitement parallèle)

```
Soumission → [CRÉÉ]
  ┌─ Pilote Manifeste       (champs réservés Manifeste)
  └─ Pilote Contrôle Doc.  (champs réservés CD)  ← simultané
  → Jointure (les deux traitements terminés)
  → Documents remis → [CLÔTURÉ]
  (Client finalise facturation/paiement/documents sur plateforme IES)
```

### 7.5 Export (tronc commun)

```
Soumission bookings → [CRÉÉ]
  → Back Office : Codifie dans IPAKI → [CODIFIÉ] / Rejette / Complément
  → Pilote Facturation : Draft IN → édite facture → [FACTURÉ]
  → (paiement selon type client)
  → Pilote CD dépose BEE → [CLÔTURÉ]
```

### 7.6 Customer Complaint

```
Client choisit catégorie → [CRÉÉ]
  → Support Opérationnel : Instruit / Complément / Oriente
  → Réponse formalisée → [CLÔTURÉ]
```

### 7.7 Enrôlement Transporteur

```
Chauffeur saisit dossier + pièces → [CRÉÉ]
  → Agent Gate : Vérifie / Complément / Valide
  → Convocation photo/badge → Déplacement physique
  → Badge remis → [CLÔTURÉ]
```

### 7.8 Avoir / Exonération

```
Client soumet demande + justificatifs → [CRÉÉ]
  → Workflow validation paramétrable (intervenants à confirmer — cf. PC-15)
  → Accord ou Rejet motivé
  → Documents déposés dans ticket → [CLÔTURÉ]
```

---

## 8. Exigences transverses (utilisées par plusieurs modules)

| Exigence | Modules concernés |
|----------|-------------------|
| SOC-01 : Génération ticket numéroté | Tous |
| SOC-02 : Centralisation docs/statuts/historique/échanges | Tous |
| SOC-05 / MSG-01-05 : Chat + notes internes | Tous |
| SOC-07 / ERR-03 : Dépôt de documents + contrôles | Tous |
| SOC-09 : Clôture + archivage automatique | Tous |
| SOC-10 / SOC-11 : Liaison tickets / Dossier | Billing Office (multi-BL) + transverse |
| SOC-12 / PEC-01-05 : Prise en charge horodatée | Tous |
| SOC-13 : Journal d'audit inviolable | Tous |
| CYC-01-07 : Cycle de vie (annulation, relances, réouverture) | Tous |
| WKF-01-03 : Moteur de workflow + dérogations | Tous |
| TRF-01-06 : Transfert / réaffectation | Tous |
| TPS-01-05 / SLA-01-05 : Mesure des temps + SLA | Tous |
| ALE-01-03 : Alertes et escalades | Tous |
| COM-01-06 / NOT-01-03 : Notifications multicanales | Tous |
| DIS-01-03 : Disponibilité opérateur | Tous |
| AFF-01-07 : Modes d'affectation | Tous |
| PRI-01-04 : Priorités tickets | Tous |
| CON-01-04 : Continuité digital/présentiel | Billing Office · Enrôlement |
| CPT-04 / ENT-03 : Cloisonnement client | Tous |
| SEC-01-12 : Sécurité (RBAC, auth, audit, chiffrement) | Tous |
| ACR-01 : Tableau de bord « Mes actions requises » | Espace client (portail) |

---

## 9. Dépendances entre exigences

> Légende : **Explicite** = formulée textuellement dans le CDC. **Déduite** = non formulée explicitement mais logiquement nécessaire.

| Exigence | Dépend de | Nature | Commentaire |
|----------|-----------|--------|-------------|
| SOC-03 (workflow propre) | CPT-02 · WKF-01 | Déduite | Le type client déclenche le workflow |
| SOC-12 (horodatages) | — | Fondatrice | PEC-01-05 opérationnalisent SOC-12 (ordre correct : SOC-12 précède PEC) |
| PEC-01-05 (prise en charge) | SOC-12 | Déduite | SOC-12 est l'exigence fondatrice |
| IMP-06 (orientation post-contrôle BL) | CPT-02 · WKF-01 | Déduite | Le type client déclenche le workflow |
| IMP-13 (validation paiement) | ROLE_TO_CONFIRM Caisse/Finance — PC-03 | Déduite | Dépend de la création du rôle |
| IMP-40 (traitement parallèle IES) | SEC-02 (RBAC champ) | Explicite | Impossible sans RBAC niveau champ |
| AFF-02 (affectation manuelle superviseur) | — | Autonome | Must indépendant — ne dépend pas de PEC-05 |
| AFF-03 (Round-Robin) | DIS-01 | Déduite | Un opérateur indisponible ne doit pas recevoir de ticket |
| SIM-01-11 | SIM-06 (paramétrabilité) · SIM-07 (versionnement) | Déduite | Pré-requis fonctionnels |
| ALE-03 (escalade SLA) | SLA-01-05 · ALE-01-02 | Déduite | Escalade suppose SLA configurés |
| TPS-04 (suspension SLA) | SLA-03 | Déduite | Même règle, deux exigences liées |
| CYC-04 (suggestion clôture) | CYC-05 (état engageant) | Déduite | CYC-05 définit l'exception de CYC-04 |
| COM-06 (relances canal — Could) | CYC-04 (relances — Must) | Déduite | COM-06 précise la dimension canal de CYC-04 |
| MAS-02 (traçabilité masse) | MAS-01 (actions de masse — Should) | Déduite | MAS-02 Must présuppose MAS-01 implémenté |
| MSG-04 (mention @Nom) | MSG-05 (pas de réaffectation) | Déduite | Complément de la même règle |
| CON-03 (RDV) | Module agenda (non décrit) | Déduite | Dépendance sur un composant à spécifier |
| IA-03 (escalade IA) | SOC-05 (fil chat ticket) | Déduite | Le fil doit exister pour conserver le contexte |
| ENR-03 (convocation) | COM-01 (notifications SMS/WhatsApp) | Déduite | Notifications mobiles nécessaires |
| UNI-01 (unicité BL) | SOC-01 · SOC-09 | Déduite | Contrôle porte sur les tickets actifs uniquement |
| SEC-08 (2FA agents) | — | Autonome | Indépendant de CPT-07 (2FA clients) |
| CPT-07 (2FA clients) | — | Autonome | Indépendant de SEC-08 (2FA agents) |

---

## 10. Contraintes de performance et non fonctionnelles

| Contrainte | Référence | Valeur |
|-----------|-----------|--------|
| Temps de réponse écrans courants | NFR-04 | < 3 secondes (charge normale) |
| Disponibilité en heures ouvrées | NFR-03 | ≥ 99% |
| Disponibilité portail client | NFR-10 | 24h/24, 7j/7 (hors maintenance) |
| Langue principale | NFR-01 | Français |
| Langue secondaire | NFR-07 | Anglais (Should) |
| Interface responsive (mobile) | NFR-02 | Obligatoire |
| Volumétrie | NFR-05 | À confirmer en cadrage (PC-01) |
| Sauvegardes + restauration testée | NFR-06 | Must |
| Architecture ouverte (API) | NFR-08 | Should |
| Hébergement | NFR-09 | À décider selon scénario retenu (PC-13) |

---

## 11. Éléments explicitement paramétrables (32 items)

> Référentiel des **éléments administrables sans développement**.

1. Grilles tarifaires simulation (versionnées, date d'effet)
2. Taux et seuils de majoration (lourd, dangereux, OOG)
3. Franchises (branchement frigorifique, stationnement)
4. Tranches et tarifs de stationnement
5. Quotes-parts de répartition stationnement
6. Référentiels simulation (sens, tailles, types, catégories de marchandise)
7. Files de traitement (création, modification)
8. Rôles et rattachements opérateurs/files
9. Matrice des transitions autorisées entre files
10. Modes et règles d'affectation par file
11. Niveaux de priorité
12. Règles d'attribution automatique de priorité
13. SLA (périmètre, durées cibles, seuils)
14. Horaires de fonctionnement (pour calcul SLA)
15. Règles d'alerte et d'escalade (seuils, destinataires, canaux)
16. Statuts et libellés des tickets (par module)
17. Étapes à partir desquelles l'annulation libre n'est plus possible
18. Délais et cadence des relances pour absence de réponse
19. Types de documents exigés par étape et par parcours
20. Formats de fichiers acceptés et taille maximale
21. Catégories de réclamation
22. Modèles de messages de notification (par événement, par canal)
23. Événements déclencheurs de notifications
24. Motifs de transfert, rejet, annulation
25. Workflows d'avoir/exonération (étapes, intervenants)
26. Informations et pièces exigées pour l'enrôlement transporteur
27. Informations et pièces exigées pour la création de compte client
28. Règles d'orientation automatique (Customer Complaint)
29. Base de connaissances IA (FAQ, procédures)
30. Politique de mots de passe (robustesse)
31. Durée d'expiration des sessions
32. Seuils de verrouillage de compte (tentatives échouées)

---

## 12. Stratégie de réalisation (CDC §23)

### Scénarios d'acquisition (§23)

| Scénario | Description |
|----------|-------------|
| **A — Développement interne** | Réalisation par la DSI de CIT |
| **B — Développement externalisé** | Réalisation sur mesure par un prestataire |
| **C — Solution du marché adaptée** | Acquisition d'une plateforme existante de gestion de tickets configurée |

### Grille d'évaluation des scénarios (§23.1)

| Critère | Attendu |
|---------|---------|
| Couverture fonctionnelle | Taux de couverture des exigences Must sans développement spécifique |
| Paramétrabilité | Workflows, statuts, catégories, pièces, modèles, champs par rôle administrables sans code |
| Multicanal | Capacité native ou par connecteur (e-mail, WhatsApp, SMS) |
| Ouverture / API | API documentées pour intégrations futures |
| Ergonomie et mobile | Expérience client simple, responsive |
| Sécurité et conformité | RBAC, audit, hébergement, conformité ARTCI |
| Coût total (TCO) | Licence, mise en œuvre, exploitation, évolution sur 3-5 ans |
| Délai de mise en œuvre | Délai réaliste jusqu'à la mise en production |
| **Réversibilité** | **Capacité à exporter l'intégralité des données et documents dans un format exploitable** |
| Support et pérennité | Qualité du support, feuille de route, références |

> **Réversibilité** : Il s'agit d'un **critère d'évaluation des scénarios et d'une exigence contractuelle d'acquisition** (particulièrement pour le scénario C — solution du marché). Elle figure dans la grille §23.1 et dans la liste des livrables §24 (« Plan de réversibilité — scénarios B et C »). Elle n'est pas une exigence fonctionnelle numérotée du CDC. Elle doit figurer dans les critères de sélection et dans les livrables contractuels. La contrainte associée est : toute solution retenue doit permettre d'exporter tickets, historiques et pièces dans un format exploitable.

### Priorité de livraison (§23.2)

| Incrément | Contenu |
|-----------|---------|
| **1 — Socle prioritaire** | Billing Office digital (IMPORT + EXPORT, 4 types de clients, tickets, simulation de proforma) |
| **2** | Customer Complaint, Enrôlement Transporteurs |
| **3** | Supervision avancée, Assistant IA |

Chaque incrément : recette avant mise en production + pilote avec périmètre restreint de clients.

---

## 13. Scénarios d'acceptation — Recette (CDC §22)

> **Nature** : Les éléments ci-dessous sont les **scénarios de recette fonctionnelle** définis par le CDC (section 22). Ce ne sont **pas des exigences numérotées**. Ils constituent des **critères d'acceptation contractuels** que chaque incrément devra satisfaire. Ils serviront de base aux Test Plans dans Spec Kit.

| ID Scénario | Intitulé (CDC §22) | Exigences principales couvertes |
|-------------|---------------------|----------------------------------|
| REC-SCE-01 | IMPORT client normal : soumission 3 BL → 3 tickets ; 1 validé, 1 rejeté (motif), 1 complément puis validation ; facturation ; paiement + preuve ; validation paiement ; dépôt BS + BADT ; clôture et archivage | IMP-01-06, IMP-10-14, SOC-01, SOC-09, UNI-01 |
| REC-SCE-02 | IMPORT client en compte : facture éditée → passage direct au Contrôle Documentaire → remise documents sans étape de paiement | IMP-20-22 |
| REC-SCE-03 | IMPORT client deposit : facture → lettrage Comptabilité → Contrôle Documentaire → clôture | IMP-30-32 |
| REC-SCE-04 | IMPORT client IES : traitement parallèle Manifeste/CD avec champs réservés par rôle ; jointure ; remise documents ; clôture | IMP-40-42, SEC-02 |
| REC-SCE-05 | EXPORT (4 types de clients) : codification Back Office, Draft IN facturation, BEE document final | EXP-01-04 |
| REC-SCE-06 | Simulation de proforma : calcul multi-conteneurs contrôlé au franc près contre l'outil Excel de référence ; vérification qu'une modification de paramètre sans développement se répercute sur le calcul | SIM-01-07, Annexe A |
| REC-SCE-07 | Customer Complaint : création par catégorie, instruction, complément, orientation, clôture avec réponse | REC-01-04 |
| REC-SCE-08 | Enrôlement transporteur : dossier à distance, complément, validation, convocation photo/badge, clôture | ENR-01-03 |
| REC-SCE-09 | Prise en charge : ticket arrive en file « attente de PEC », opérateur l'accepte ; horodatages enregistrés et exploités dans les indicateurs | SOC-12, PEC-01-04, TPS-01 |
| REC-SCE-10 | Transfert de file : opérateur transfère avec motif ; ticket conserve numéro, dossier, documents, commentaires, historique ; client notifié ; transfert vers destination non autorisée refusé | TRF-01-06, WKF-03 |
| REC-SCE-11 | Réaffectation entre opérateurs (A → B) selon habilitations, tracée dans l'historique | TRF-04, WKF-03 |
| REC-SCE-12 | Temps et SLA : mesure attente/traitement par étape ; distinction temps interne/temps client (SLA suspendu en « En attente client ») ; déclenchement des seuils 50% / 80% / dépassement / escalade | TPS-01-04, SLA-01-05, ALE-01-03 |
| REC-SCE-13 | Supervision : vue temps réel des files et de la charge ; alertes (ticket non PEC, sans activité, SLA) ; affectation d'un ticket non PEC ; déblocage d'un ticket | SUP-01-05, ALE-01-03 |
| REC-SCE-14 | Transverse : notifications sur chaque canal avec lien vers ticket ; continuité digital/présentiel ; regroupement tickets/dossier ; contrôle habilitations ; journal d'audit | COM-01-04, CON-01-03, SOC-11, SOC-13, CPT-04, SEC-01-12 |

**Critères d'acceptation contractuels (CDC §22)** :
- 100 % des scénarios Must exécutés avec succès
- Aucune anomalie bloquante ou majeure ouverte
- Exigences de sécurité vérifiées
- Documentation livrée

---

## 14. Risques projet (CDC §26)

> **Nature** : Les éléments ci-dessous sont les **risques projet** identifiés par le CDC (section 26). Ce ne sont **pas des exigences fonctionnelles**. Ils documentent les facteurs de risque reconnus par CIT et leurs mesures de maîtrise.

| # | Risque | Mesure de maîtrise | Exigences liées |
|---|--------|-------------------|-----------------|
| RSQ-01 | **Adoption insuffisante par les clients** | Communication et accompagnement, simplicité du portail, maintien du double parcours, pilote avec clients volontaires | NFR-01, NFR-02, CON-01, ACR-01 |
| RSQ-02 | **Ressaisie et charge agents** (absence d'interfaçage avec les SI métiers) | Ergonomie back-office optimisée et champs pré-remplis ; architecture ouverte permettant d'ajouter des intégrations API ultérieurement | NFR-08 (API REST) |
| RSQ-03 | **Divergence ticket / réalité systèmes** (IPAKI, IRIS) | Procédures opératoires claires ; contrôles de cohérence en recette ; l'agent trace systématiquement dans le ticket le résultat de ses actions hors portail | SOC-02, SOC-13 |
| RSQ-04 | **Retard de paramétrage des canaux WhatsApp/SMS** | Démarrage possible avec e-mail + notifications portail ; ajout des canaux mobiles sans impact sur le workflow | COM-01 — cf. PC-16 |
| RSQ-05 | **Sous-estimation de la volumétrie documentaire** | Dimensionnement du stockage validé en cadrage ; quotas et politique de purge/archivage | NFR-05 — cf. PC-01, PC-14 |
| RSQ-06 | **Dépendance à un éditeur** (scénario C) | Exigence de réversibilité contractuelle et d'export complet des données | §23.1 Réversibilité, §24 Plan de réversibilité |

---

## 15. Points restant à valider en phase de cadrage (CDC §25)

> Le CDC §25 distingue explicitement les points à consolider de ceux déjà figés.

**Figés dans le CDC (non à clarifier)** :
- Règles IES (traitement parallèle) : confirmées par le métier
- Validation des preuves de paiement (rôle Caisse/Finance) : confirmée par le métier
- Valeurs tarifaires de la simulation (Annexe A) : confirmées par le métier

**À consolider en cadrage** (correspondance avec les points du document `02-points-a-clarifier.md`) :

| Point CDC §25 | Point à clarifier correspondant |
|--------------|--------------------------------|
| Volumétrie cible | PC-01 |
| Liste exacte des pièces exigées | PC-08 |
| Choix des canaux de notification actifs au lancement | PC-16 |
| Valeurs cibles des SLA | PC-12 |
| Matrice des transitions autorisées entre files | PC-07 |
| Statut exact du parcours Avoir et Exonération | PC-15 |
| Politique de conservation et d'archivage | PC-14 |
| Décision sur l'hébergement | PC-13 |

---

*Document d'analyse pré-implémentation. Aucune implémentation ne commence avant validation.*
