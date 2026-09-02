# Spécification Fonctionnelle — Feature 004 : Billing Office EXPORT & Unicité Booking

> **Dossier** : `specs/004-billing-office-export/`  
> **Date** : 30 Août 2026  
> **Version** : 1.3 (Précision Technique CLAR-BOOK-01)  
> **Statut** : **CLARIFIÉ & VALIDÉ**  
> **Auteur** : Antigravity Specification Agent  
> **Sources de vérité** : CIT-CIV-DSI-RFP-XXXX v1.0 (§10.2, §11, §22), `.specify/memory/constitution.md`, `docs/01-matrice-exigences.md`, `docs/04-matrice-recette.md`, `docs/07-roadmap-post-ui.md`

---

## 1. Contexte & Objectifs Métier

Le module **Billing Office EXPORT** constitue le second volet opérationnel fondamental de la gestion des flux portuaires de CIT. Complémentaire au flux IMPORT (traité dans la Feature 003), il permet aux clients exportateurs de soumettre leurs demandes d'embarquement sur la base de confirmations de booking, d'assurer la codification opérationnelle par le Back Office dans le système IPAKI, d'orienter les dossiers selon les quatre profils clients établis (Normal, En Compte, Deposit, IES), de générer le *Draft IN* en facturation, et de délivrer le document final officiel d'embarquement : le **BEE** (*Bordereau d'Embarquement Électronique* ou *Bon d'Entrée Export*).

Cette Feature 004 capitalise directement sur les acquis des itérations précédentes :
- **Feature 001** : Moteur de tickets, dossiers, files de traitement, matrice de transitions, SLA, audit append-only et gestion documentaire.
- **Feature 002** : Gestion des comptes entreprises, habilitations multi-utilisateurs et snapshot immuable du `TypeClient`.
- **Feature 003** : Patterns de files Billing (Facturation, Paiement, Contrôle Documentaire, IES), gestion des compléments et rejets motivés.
- **Feature 003.5** : Ergonomie consolidée, shell applicatif, fiches 360° et zéro saisie manuelle de GUID.

---

## 2. Périmètre des Exigences Contractuelles

La Feature 004 couvre **strictement 5 exigences contractuelles** :

| Référence | Priorité | Module | Description Synthétique (CDC) |
| :--- | :---: | :--- | :--- |
| **EXP-01** | **Must** | Billing EXPORT | Soumission client d'un ou plusieurs bookings avec documents (confirmation de booking, déclaration export, autres pièces) ; génération d'un ticket par booking. |
| **EXP-02** | **Must** | Billing EXPORT | Les tickets arrivent dans la file du Back Office, qui codifie le booking dans IPAKI (hors portail) et prononce : Codifié (validé), Rejeté (motif obligatoire) ou En attente de complément. |
| **EXP-03** | **Must** | Billing EXPORT | Après codification, orientation selon les mêmes variantes par type de client que l'import ; le Pilote Facturation crée le Draft IN avant l'édition de la facture ; le document final est le BEE. |
| **EXP-04** | **Must** | Billing EXPORT | Client IES export : la facture et le BEE sont générés côté plateforme IES après traitement parallèle et remise des documents dans le ticket. |
| **UNI-04** | **Should** | Billing EXPORT | Pour l'EXPORT, une règle équivalente d'unicité sur le numéro de booking pourra être mise en œuvre après validation métier (cf. PC-09). |

---

## 3. Scénario de Recette Contractuel (REC-SCE-05)

Le scénario de recette contractuel applicable est extrait littéralement de `docs/04-matrice-recette.md` (§22 du CDC) :

> **`REC-SCE-05` — EXPORT (4 types de clients)**  
> *Description exacte* : Codification Back Office, Draft IN à la facturation, BEE en document final ; vérifier les 4 variantes de clients.  
> *Exigences Must couvertes* : `EXP-01`, `EXP-02`, `EXP-03`, `EXP-04`  
> *Exigences Should/Could couvertes* : `UNI-04`  
> *Préconditions* : Comptes activés pour les 4 types de clients (`Normal`, `EnCompte`, `Deposit`, `Ies`) · File Back Office configurée.  
> *Acteurs* : Client (4 types), Back Office (Agent habilité), Pilote Facturation, Caisse/Finance, Pilote CD (Contrôle Documentaire).

---

## 4. Règles Métier & Principes de Conception

### 4.1. Soumission Multi-Bookings & Indépendance des Tickets (EXP-01)
- Le client soumet un formulaire contenant une liste de **1 à N numéros de booking**.
- Chaque demande génère un **Dossier unique** (`DOS-EXP-YYYY-XXXXX`) et **un Ticket indépendant par booking** (`TCK-EXP-YYYY-XXXXX`).
- **Indépendance totale de cycle de vie (Décision A2)** : chaque ticket booking d'un même dossier suit son propre cycle de vie (Back Office, Facturation, CD) sans que le rejet, l'attente de complément ou le délai sur un booking ne bloque les autres tickets du dossier.
- Les documents rattachés à la soumission (confirmation de booking, déclaration douanière export) sont stockés et liés aux tickets concernés.

### 4.2. Unicité du Booking Actif (UNI-04, PC-09 & CLAR-BOOK-01)
- **Unicité globale stricte (Décision A1)** : un numéro de booking ne peut avoir qu'un seul ticket actif sur l'ensemble de la plateforme.
- Un numéro de booking est considéré comme **actif** s'il est associé à un ticket dans un état autre que `Cloture`, `Rejete` ou `Annule`.
- **Règle de stockage & comparaison (CLAR-BOOK-01)** :
  - **Stockage & Affichage** : Suppression des espaces externes uniquement (`NumeroBooking = NumeroBooking.Trim()`). La valeur stockée conserve la casse de saisie et devient la valeur affichée.
  - **Comparaison d'Unicité** : Insensible à la casse via la collation SQL Server déterministe `Latin1_General_CI_AS` (`ABC123` == `abc123` == `AbC123`).
  - **Caractères conservés** : Les tirets et espaces internes sont strictement préservés (`ABC-123` != `ABC123`, `ABC 123` != `ABC123`).
- Si un client soumet un numéro de booking déjà actif dans la plateforme :
  - La soumission du booking en conflit est bloquée avec un message explicite.
  - Si le ticket actif appartient à la même société, un lien vers le ticket existant est proposé.
  - Si le ticket actif appartient à une société tierce, l'accès est refusé sans fuite d'informations (multi-tenant strict).
- Dès qu'un ticket atteint l'état `Cloture`, `Rejete` ou `Annule`, le numéro de booking est immédiatement libéré pour une réutilisation ultérieure.
- La détection de doublon est protégée contre la concurrence par contrainte d'intégrité / index unique filtré en base.

### 4.3. File Back Office & Codification IPAKI (EXP-02)
- À la création, chaque ticket arrive dans la file **`Back Office Export`** à l'état `EnAttenteDePriseEnCharge`.
- L'opérateur Back Office prend en charge le ticket et effectue la codification du booking dans le logiciel d'exploitation externe **IPAKI** (opération hors portail).
- Dans le portail, l'opérateur consigne le résultat :
  - **Codifié / Validé** : Le ticket est approuvé et avance vers la file suivante selon le type de client.
  - **Rejeté** : Motif obligatoire renseigné, le ticket passe à l'état `Rejete` / `Annule` avec typage `TypeAnnulationExport.RejetBackOffice` (terminé et libère le booking).
  - **Demande de complément** : Motif renseigné, le ticket passe en `EnAttenteClient` et déclenche une action dans le bandeau **ACR-01** du client. Dès dépôt du complément par le client, le ticket revient dans la file Back Office.

### 4.4. Aiguillage selon le Profil Client & Draft IN (EXP-03)
Le circuit s'oriente selon le snapshot immuable `Dossier.TypeClient` :
1. **Client Normal** :
   - *Back Office* ➔ Validation.
   - *Facturation* : L'agent génère et joint le **Draft IN** (simulation d'entrée plein export, hors portail), puis édite et dépose la **Facture Export** dans le ticket.
   - *Paiement* : Le client dépose sa preuve de paiement (hors portail).
   - *Validation Paiement* : La Caisse/Finance (rôle `BillingPaiementValider` — **Décision A3**) valide l'encaissement.
   - *Contrôle Documentaire* : L'agent vérifie le dossier, génère et dépose le **BEE** (*Bordereau d'Embarquement Électronique* — **Décision A4**), remet les documents au client et clôture le ticket.
2. **Client En Compte** :
   - *Back Office* ➔ Validation.
   - *Facturation* : Génération du Draft IN et édition de la Facture Export.
   - *Contrôle Documentaire* (pas d'étape paiement) : Dépôt du BEE et clôture avec remise des documents.
3. **Client Deposit** :
   - *Back Office* ➔ Validation.
   - *Facturation* : Génération du Draft IN et Facture Export.
   - *Comptabilité Client* : Lettrage du compte deposit dans IRIS (hors portail) et validation.
   - *Contrôle Documentaire* : Dépôt du BEE et clôture.
4. **Client IES Export (EXP-04)** :
   - Dès la soumission, le ticket est instruit en parallèle par le Back Office et le Contrôle Documentaire.
   - La facturation et l'émission du BEE sont finalisées sur la plateforme IES externe ; le ticket portail est clôturé à la confirmation de remise documentaire.

---

## 5. User Stories & Critères d'Acceptation (Given / When / Then)

### US1 : Soumission d'une Demande Export Multi-Bookings (`EXP-01`)
**En tant que** collaborateur d'une société cliente habilité à la soumission,  
**Je veux** saisir un ou plusieurs numéros de booking et déposer les pièces justificatives,  
**Afin de** démarrer le processus d'embarquement export auprès de CIT.

- **Critère 1.1 (Succès nominal multi-bookings)** :
  - *Given* un utilisateur connecté avec `DroitSoumission = true` pour une société active.
  - *When* il renseigne 2 numéros de booking distincts (`BKG-001`, `BKG-002`) et téléverse la confirmation de booking.
  - *Then* le système crée un `Dossier` de type `EXPORT` et 2 `Tickets` indépendants portant les numéros de booking correspondants (nettoyés des espaces externes), tous deux affectés à la file `Back Office Export`.
- **Critère 1.2 (Refus sans droit)** :
  - *Given* un utilisateur connecté avec `DroitSoumission = false`.
  - *When* il tente d'accéder au formulaire de soumission Export.
  - *Then* l'accès est refusé avec affichage du composant `PermissionDenied` (403).

### US2 : Contrôle d'Unicité du Booking Actif (`UNI-04` & `CLAR-BOOK-01`)
**En tant que** plateforme CIT,  
**Je veux** vérifier qu'aucun ticket actif n'existe pour les bookings soumis selon la règle CLAR-BOOK-01,  
**Afin d'** éviter les doublons d'instruction et les conflits de traitement.

- **Critère 2.1 (Blocage booking actif existant y compris variation de casse ou d'espaces externes)** :
  - *Given* un ticket actif existant pour le booking `BKG-ACTIVE-100`.
  - *When* un client soumet une nouvelle demande Export avec `bkg-active-100` ou `" BKG-ACTIVE-100 "`.
  - *Then* la création est bloquée avec le message "Le booking BKG-ACTIVE-100 fait déjà l'objet d'un ticket en cours de traitement".
- **Critère 2.2 (Autorisation après clôture, rejet ou annulation)** :
  - *Given* un ancien ticket sur le booking `BKG-HIST-200` dont l'état est `Cloture`, `Rejete` ou `Annule`.
  - *When* un client soumet une nouvelle demande avec `BKG-HIST-200`.
  - *Then* la soumission est acceptée et un nouveau ticket est créé avec succès.
- **Critère 2.3 (Conservation des tirets et espaces internes)** :
  - *Given* un ticket actif existant pour `BKG-100`.
  - *When* un client soumet `BKG100` ou `BKG 100`.
  - *Then* la création est acceptée car ces identifiants sont distincts selon `CLAR-BOOK-01`.

### US3 : Instruction en File Back Office & Codification IPAKI (`EXP-02`)
**En tant qu'** opérateur Back Office habilité,  
**Je veux** prendre en charge un ticket Export, procéder à sa codification IPAKI et statuer,  
**Afin d'** autoriser la poursuite du parcours d'embarquement ou demander des corrections.

- **Critère 3.1 (Validation / Codification)** :
  - *Given* un ticket en attente dans la file `Back Office Export`.
  - *When* l'opérateur Back Office prend en charge le ticket et sélectionne l'action "Valider la codification IPAKI".
  - *Then* le statut passe à `EnCours` / file suivante (`Facturation` ou `Contrôle Documentaire` selon le type client) et l'événement est tracé dans l'historique.
- **Critère 3.2 (Demande de complément & ACR-01)** :
  - *Given* un ticket en cours d'instruction Back Office.
  - *When* l'opérateur demande un complément en précisant le motif "Déclaration en douane illisible".
  - *Then* le ticket passe à `EnAttenteClient`, apparaît dans les "Actions Requises" du client, et revient en file Back Office dès que le client dépose le document rectifié.
- **Critère 3.3 (Rejet motivé)** :
  - *Given* un ticket en file Back Office.
  - *When* l'opérateur rejette la demande avec le motif obligatoire "Booking annulé par l'armateur".
  - *Then* le ticket est clôturé avec le statut `Annule` (typé `TypeAnnulationExport.RejetBackOffice`), le motif est consigné et le booking est immédiatement libéré pour l'unicité future.

### US4 : Circuit Facturation & Émission du Draft IN (`EXP-03`)
**En tant que** Pilote Facturation habilité,  
**Je veux** créer le Draft IN et éditer la Facture Export,  
**Afin de** notifier le montant à régler ou transmettre le dossier au Contrôle Documentaire.

- **Critère 4.1 (Génération Draft IN et Facture pour Client Normal)** :
  - *Given* un ticket d'un client de type `Normal` validé par le Back Office.
  - *When* l'agent de facturation dépose le `Draft IN` et la `Facture Export`.
  - *Then* le ticket passe à l'état `EnAttenteClient` (attente paiement) et notifie le client.
- **Critère 4.2 (Bascule directe pour Client En Compte)** :
  - *Given* un ticket d'un client `EnCompte`.
  - *When* l'agent dépose le `Draft IN` et la facture.
  - *Then* le ticket est orienté immédiatement vers la file `Contrôle Documentaire` sans attente de preuve de paiement.

### US5 : Contrôle Documentaire & Délivrance du BEE (`EXP-03`)
**En tant que** Pilote Contrôle Documentaire habilité,  
**Je veux** contrôler les pièces finales et déposer le BEE (*Bordereau d'Embarquement Électronique*),  
**Afin de** remettre le document officiel au client et clôturer la demande Export.

- **Critère 5.1 (Dépôt du BEE et Clôture)** :
  - *Given* un ticket Export ayant franchi avec succès les étapes préalables.
  - *When* le Pilote CD dépose le fichier PDF officiel `BEE` et valide la conformité.
  - *Then* le document `BEE` est rattaché au ticket comme document de sortie officiel, le ticket passe à l'état `Cloture` et le client peut télécharger son BEE.

### US6 : Circuit IES Export (`EXP-04`)
**En tant qu'** opérateur ou client IES Export,  
**Je veux** que le traitement parallèle Back Office / CD s'exécute selon les règles IES,  
**Afin de** permettre la finalisation de la facture et du BEE sur la plateforme IES.

- **Critère 6.1 (Traitement parallèle et clôture)** :
  - *Given* un ticket Export d'un client de profil `Ies`.
  - *When* les validations conjointes du Back Office et du Contrôle Documentaire sont actées.
  - *Then* le ticket est orienté vers la finalisation IES et clôturé dès mise à disposition de la confirmation d'embarquement.

---

## 6. Matrice de Traçabilité des Exigences

| Réf CDC | Priorité | User Story | Critères d'Acceptation | Scénario Recette | Décision Humaine de Clarification | Dépendances |
| :--- | :---: | :--- | :--- | :--- | :--- | :--- |
| **EXP-01** | Must | US1 | Critères 1.1, 1.2 | `REC-SCE-05` | **Décision A2** : Indépendance totale des tickets booking | F001 (Tickets), F002 (Comptes), F003.5 (UI) |
| **EXP-02** | Must | US3 | Critères 3.1, 3.2, 3.3 | `REC-SCE-05` | Traçage statut IPAKI sans API externe | F001 (Files, SLA), F003.5 (ACR-01) |
| **EXP-03** | Must | US4, US5 | Critères 4.1, 4.2, 5.1 | `REC-SCE-05` | **Décision A3** : `BillingPaiementValider` / **Décision A4** : Fichier BEE PDF | F002 (TypeClient), F003 (Facturation/CD) |
| **EXP-04** | Must | US6 | Critère 6.1 | `REC-SCE-05` | Parallélisme Back Office / CD | F003 (Moteur parallèle IES) |
| **UNI-04** | Should | US2 | Critères 2.1, 2.2, 2.3 | `REC-SCE-05` | **Décision A1** + **CLAR-BOOK-01** : Comparaison normalisée | F003 (Unicité concurrente) |

---

## 7. Points à Clarifier (PC) & Décisions Humaines Validées

### CLAR-BOOK-01 — Règle de Stockage et Comparaison du Numéro de Booking (UNI-04 / PC-09)
- **Source** : Clarification Projet validée (30 Août 2026).
- **Règle de Stockage & Affichage** :
  - Les espaces externes de saisie sont nettoyés avant persistance : `NumeroBooking = NumeroBooking.Trim()`.
  - La valeur persistée conserve la casse de saisie et constitue la valeur de référence affichée dans l'interface.
- **Règle de Comparaison d'Unicité** :
  - La comparaison d'unicité est **insensible à la casse** grâce à la collation SQL Server déterministe `Latin1_General_CI_AS` (`ABC123` == `abc123` == `AbC123`).
  - **Conservation stricte des tirets** (`ABC-123` != `ABC123`).
  - **Conservation stricte des espaces internes** (`ABC 123` != `ABC123`).
  - Aucune autre transformation destructive.
- **Portée** : Unicité globale stricte. Un numéro de booking ne peut avoir qu'un seul ticket actif sur l'ensemble de la plateforme. La clôture, le rejet motivé ou l'annulation libère immédiatement le booking pour une réutilisation ultérieure.

### EXP-01 — Indépendance des Billets dans un Dossier Multi-Bookings
- **Décision Validée (A2)** : **Indépendance totale**. Chaque ticket booking d'un dossier suit son propre cycle de vie (Back Office, Facturation, CD) sans que le rejet ou le complément sur un booking ne bloque les autres tickets du dossier.

### PC-03 — Rôle Habilité pour la Validation de Paiement Export (EXP-03)
- **Décision Validée (A3)** : **Réutilisation de l'habilitation transverse `BillingPaiementValider`** (Caisse / Finance) partagée avec l'Import.

### Matérialisation du BEE (EXP-03 / EXP-04)
- **Décision Validée (A4)** : **Document PDF officiel** téléversé par le Contrôle Documentaire (ou issu de la plateforme IES), rattaché au ticket comme document de sortie officiel téléchargeable par le client à la clôture.

---

## 8. Référentiel Documentaire (PC-08)

Les types de documents intervenant dans le cycle Export sont strictement cadrés par le CDC :
1. **ConfirmationBooking** (`EXP-01`) : Document justificatif téléversé par le client à la soumission.
2. **DeclarationDouaneExport** (`EXP-01`) : Déclaration d'exportation déposée par le client.
3. **DraftIn** (`EXP-03`) : Simulation d'entrée plein export émise par la Facturation.
4. **FactureExport** (`EXP-03`, `EXP-04`) : Facture officielle émise par la Facturation (ou IES).
5. **PreuvePaiement** (`EXP-03`) : Quittance / virement téléversé par le client Normal.
6. **Bee** (`EXP-03`, `EXP-04`) : Bon d'Entrée Export / Bordereau d'Embarquement Électronique officiel remis au client pour autoriser l'accès terminal.

---

## 9. Frontières du Périmètre

### In Scope
- Soumission d'une demande Export multi-bookings (1 à N) avec pièces justificatives.
- Contrôle d'unicité concurrente sur les numéros de booking actifs selon `CLAR-BOOK-01` (`UNI-04`).
- File `Back Office Export` avec traçage de codification IPAKI, validation, rejet motivé et complément.
- Aiguillage automatique selon `Dossier.TypeClient` (Normal, En Compte, Deposit, IES).
- File `Facturation` avec dépôt du Draft IN et de la Facture Export.
- File `Validation Paiement` pour le profil client Normal via `BillingPaiementValider`.
- File `Contrôle Documentaire` avec dépôt du BEE et clôture.
- Traitement parallèle spécifique `Client IES Export`.
- Intégration complète dans les tableaux de bord et la navigation Blazor existante (zéro GUID manuel).

### Out of Scope
- Intégration API temps réel automatisée avec IPAKI (le CDC spécifie une saisie manuelle / traçage de la codification par l'opérateur Back Office).
- Module Simulation de Proforma (`SIM-01..11` ➔ Feature 005).
- Customer Complaint & Avoirs (`REC-01..06`, `AVE-01..04` ➔ Feature 006).
- Enrôlement Transporteurs (`ENR-01..05` ➔ Feature 007).
- Notifications multicanales automatiques (`COM-01..06` ➔ Feature 008).
