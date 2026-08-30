# Audit UI/UX Complet — Features 001, 002 et 003
## Plateforme Digitale CIT — v1.0

> **Document** : `docs/06-audit-ui-ux-f001-f003.md`  
> **Date** : 2026-08-30  
> **Statut** : Rapport d'audit UI/UX post-Feature 003  
> **Périmètre audité** : Frontend Blazor InteractiveServer (`src/CIT.Plateforme.Web/`), composants Razor, navigation, layouts, parcours utilisateurs réels vs. capacités backend livrées dans F001, F002 et F003.  
> **Principe directeur** : Constat objectif des écarts sans modification de code ni réinterprétation des règles métier.

---

## 1. Résumé Exécutif

L'audit approfondi du projet Web Blazor (`CIT.Plateforme.Web`) met en évidence un **décalage critique** entre l'excellence de la couche Domaine/Application (110 exigences CDC implémentées, 180 tests unitaires/intégration validés à 100 %, 0 warning) et la maturité de la couche de présentation UI/UX.

```
╔════════════════════════════════════════════════════════════════════════════════════════════════════╗
║                                 SYNTHÈSE DE L'AUDIT UI/UX                                         ║
╠════════════════════════════════════════════════════════════════════════════════════════════════════╣
║  • Couverture Backend (F001 + F002 + F003)        : 100 % (110 exigences couvertes, 180 tests)     ║
║  • Pages / Vues Razor existantes                  : 32 fichiers (29 pages fonctionnelles/demo)    ║
║  • Navigation & Menus (Navbar, Sidebar, Liens)    :   0 % (AUCUN menu, 100 % pages orphelines)    ║
║  • Dashboard Client / Agent / Superviseur         :   0 % (Tableaux de bord métier inexistants)   ║
║  • Pages en mode "Test Technique" (saisie GUIDs)  :  86 % (25 / 29 pages exigent des GUIDs bruts) ║
║  • Découvrabilité sans saisie d'URL manuelle     : < 5 % (Seul Mot de passe oublié est lié)       ║
╚════════════════════════════════════════════════════════════════════════════════════════════════════╝
```

### Constat Majeur
Les pages actuelles ont été conçues comme des **harnais de test de démonstration unitaire** pour valider les contrats d'API / Application, et non comme une **application Web exploitable par un utilisateur final**. 

En l'état :
1. **Un visiteur ou un client connecté arrivant sur la racine `/` voit une page blanche avec "Hello, world!"**.
2. **Aucune barre de navigation, aucun menu, aucun pied de page n'existe dans `MainLayout.razor`**.
3. **Pour exécuter le moindre parcours, l'utilisateur doit connaître les URLs exactes par cœur et copier-coller manuellement des identifiants GUID (`userId`, `societeId`, `agentId`, `fileId`, `ticketId`) dans des formulaires de démonstration**.
4. **La page de détail d'un ticket (`/tickets/{guid}`) n'affiche même pas les données du ticket** (numéro, BL, statut, dates, client), mais uniquement des champs de saisie pour déclencher des transitions.

---

## 2. Inventaire Détaillé des Vues et Pages Existantes

L'application Web compte actuellement **32 composants Razor** dans `src/CIT.Plateforme.Web/Components/`.

| # | Route | Composant | Feature | Rôle cible | Objectif métier | Données affichées | Actions possibles | État de la vue |
|---|---|---|:---:|---|---|---|---|:---:|
| 1 | `/` | `Home.razor` | — | Tous | Page d'accueil | "Hello, world!" (template) | Aucune | **MANQUANT / NON ACCESSIBLE** |
| 2 | `/error` | `Error.razor` | — | Tous | Page d'erreur | RequestId | Recharger | **TECHNIQUE** |
| 3 | `/not-found` | `NotFound.razor` | — | Tous | Page 404 | Message introuvable | Aucune | **TECHNIQUE** |
| 4 | `/comptes/connexion` | `Connexion.razor` | F002 | Tous | Authentification Identity | Formulaire email/pwd | Se connecter, aller sur mot de passe oublié | **COMPLET** *(manque redirection post-login)* |
| 5 | `/comptes/verifier-code` | `VerifierCode.razor` | F002 | Client / Agent | Défi 2FA TOTP | Formulaire code 6 chiffres | Valider le code TOTP | **COMPLET** |
| 6 | `/comptes/mot-de-passe-oublie` | `MotDePasseOublie.razor` | F002 | Client | Demande reset mot de passe | Champ email, lien généré en clair | Générer le lien de reset | **TECHNIQUE/DEMO** |
| 7 | `/comptes/reinitialiser-mot-de-passe` | `ReinitialiserMotDePasse.razor` | F002 | Client | Saisie nouveau mot de passe | Formulaire nouveau mot de passe | Réinitialiser | **COMPLET** |
| 8 | `/comptes/double-authentification` | `ActiverDoubleAuthentification.razor` | F002 | Client / Agent | Gestion 2FA TOTP | État 2FA (actif/inactif) | Activer / Désactiver 2FA | **COMPLET** *(utilise AuthenticationState)* |
| 9 | `/comptes/soumettre` | `Comptes/SoumettreDemande.razor` | F002 | Visiteur / Client | Création de compte société | Formulaire profil, infos, email, pwd | Soumettre demande compte | **COMPLET** |
| 10 | `/comptes/valider` | `Comptes/ValiderDemandes.razor` | F002 | Admin CIT | Validation demandes de compte | Tableau des demandes en attente | Valider, Rejeter avec motif | **TECHNIQUE/DEMO** *(saisie appelantId GUID)* |
| 11 | `/comptes/profil` | `Comptes/ProfilSociete.razor` | F002 | Client / Admin Sté | Consultation profil société | Profil, infos, table collaborateurs | Consulter | **TECHNIQUE/DEMO** *(saisie appelantId, societeId)* |
| 12 | `/comptes/collaborateurs` | `Comptes/GestionCollaborateurs.razor` | F002 | Admin Société | Gestion des utilisateurs rattachés | Tableau collaborateurs, rôles, droits | Activer/Désactiver, Nommer admin, Visibilité | **TECHNIQUE/DEMO** *(saisie GUIDs bruts)* |
| 13 | `/comptes/modifier-type` | `Comptes/ModifierTypeClient.razor` | F002 | Admin CIT | Modification type client | Formulaire nouveau type | Modifier type société | **TECHNIQUE/DEMO** *(saisie GUIDs bruts)* |
| 14 | `/tickets/soumettre` | `Tickets/Soumettre.razor` | F001 | Client | Soumission ticket socle | Formulaire générique module/type | Créer un ticket générique | **TECHNIQUE/DEMO** *(saisie clientId, fileId)* |
| 15 | `/tickets/ma-file` | `Tickets/MaFile.razor` | F001 | Agent | Consultation file générique | Tableau tickets (numéro, statut, date) | Prendre en charge (PEC-02) | **TECHNIQUE/DEMO** *(saisie agentId, fileId)* |
| 16 | `/tickets/{TicketId:guid}` | `Tickets/DetailTicket.razor` | F001 | Agent / Client | Détail ticket & actions cycle de vie | Aucun détail affiché ! (champs inputs) | Transférer, Réaffecter, Attente, Clôturer... | **TECHNIQUE/DEMO** *(n'affiche pas le ticket)* |
| 17 | `/tickets/{TicketId:guid}/documents` | `Tickets/Documents.razor` | F001 | Agent / Client | Dépôt et liste documents | Tableau documents (nom, taille, antivirus) | Déposer un document | **TECHNIQUE/DEMO** *(saisie deposantId GUID)* |
| 18 | `/tickets/{TicketId:guid}/messages` | `Tickets/FilMessages.razor` | F001 | Agent / Client | Fil de chat et notes internes | Liste messages (publics / internes) | Publier message client, agent, note | **TECHNIQUE/DEMO** *(saisie auteurId GUID)* |
| 19 | `/tickets/{TicketId:guid}/temps` | `Tickets/IndicateursTemps.razor` | F001 | Agent / Superviseur | Mesure temps & SLA | Décomposition des durées | Aucune (lecture seule) | **PARTIEL** |
| 20 | `/dossiers/{DossierId:guid}` | `Dossiers/DetailDossier.razor` | F001 | Agent / Client | Regroupement multi-BL | Numéro dossier, état dérivé, tickets | Aucune (lecture seule) | **PARTIEL** |
| 21 | `/supervision` | `Supervision/TableauDeBord.razor` | F001 | Superviseur | Vue temps réel supervision | Tableau files (backlog) + Tableau charge | Charger vue | **TECHNIQUE/DEMO** *(saisie superviseurId)* |
| 22 | `/administration/disponibilite` | `Administration/Disponibilite.razor` | F001 | Superviseur / Agent | Disponibilité agent (DIS-01) | Statut sélectionné | Mettre à jour disponibilité | **TECHNIQUE/DEMO** *(saisie GUIDs bruts)* |
| 23 | `/administration/strategie-file` | `Administration/StrategieFile.razor` | F001 | Admin CIT | Stratégie d'affectation file | Select stratégie | Appliquer stratégie file | **TECHNIQUE/DEMO** *(saisie GUIDs bruts)* |
| 24 | `/administration/audit` | `Administration/JournalAudit.razor` | F001 | Admin Technique | Journal d'audit (SOC-13) | Tableau des entrées d'audit | Charger audit | **TECHNIQUE/DEMO** *(saisie adminId GUID)* |
| 25 | `/billing-import/soumettre` | `BillingImport/SoumettreDemande.razor` | F003 | Client | Soumission Import multi-BL | Formulaire BLs, résultats et refus | Soumettre jusqu'à 5 BLs | **TECHNIQUE/DEMO** *(saisie societeId, auteurId)* |
| 26 | `/billing-import/manifeste` | `BillingImport/FilePiloteManifeste.razor` | F003 | Pilote Manifeste | File instruction BL | Tableau tickets BL en instance | Valider, Rejeter (motif), Complément | **TECHNIQUE/DEMO** *(saisie agentId GUID)* |
| 27 | `/billing-import/facturation` | `BillingImport/FileFacturation.razor` | F003 | Pilote Facturation | File édition facture | Tableau tickets BL | Téléverser facture (déclenche aiguillage) | **TECHNIQUE/DEMO** *(saisie agentId GUID)* |
| 28 | `/billing-import/validation-paiement` | `BillingImport/ValidationPaiement.razor` | F003 | Caisse / Client | Validation paiement (IMP-12/13) | Formulaire dépôt client + File agent | Déposer preuve (client), Valider/Refuser | **TECHNIQUE/DEMO** *(vue hybride à scinder)* |
| 29 | `/billing-import/controle-documentaire` | `BillingImport/FileControleDocumentaire.razor` | F003 | Pilote CD | Remise BS/BADT & clôture | Tableau tickets BL | Téléverser BS/BADT (clôture ticket) | **TECHNIQUE/DEMO** *(saisie agentId GUID)* |
| 30 | `/billing-import/comptabilite-client` | `BillingImport/FileComptabiliteClient.razor` | F003 | Comptable Client | Lettrage IRIS (Deposit) | Tableau tickets Deposit | Confirmer le lettrage | **TECHNIQUE/DEMO** *(saisie agentId GUID)* |
| 31 | `/billing-import/ies` | `BillingImport/FileIes.razor` | F003 | Pilotes IES / CD | Traitement parallèle IES | Tableau tickets et statut 2 branches | Achever Manifeste, Achever CD, Jointure | **TECHNIQUE/DEMO** *(saisie agentId GUID)* |
| 32 | `/billing-import/ticket-cloture` | `BillingImport/ConsulterTicketCloture.razor` | F003 | Client En Compte | Consultation documents finaux | Tableau documents (facture, BS, BADT) | Charger documents | **TECHNIQUE/DEMO** *(saisie clientId, ticketId)* |

---

## 3. Analyse de la Navigation et de la Découvrabilité

### 3.1 Structure du Layout (`MainLayout.razor`)
```razor
@inherits LayoutComponentBase

@Body

<div id="blazor-error-ui" data-nosnippet>
    An unhandled error has occurred.
    <a href="." class="reload">Reload</a>
    <span class="dismiss">🗙</span>
</div>
```
- **Diagnostic** : Absence totale de structure d'application. Aucun header, aucune navbar, aucun menu latéral, aucun pied de page.
- **Conséquence** : L'application n'est pas navigable. Pour passer d'une page à l'autre, l'utilisateur doit modifier manuellement l'URL dans la barre d'adresse de son navigateur.

### 3.2 Découvrabilité des Pages
- **Pages accessibles par lien direct dans l'interface** :
  - `/comptes/connexion` ➜ `/comptes/mot-de-passe-oublie` (lien hypertexte dans le formulaire).
  - `/comptes/connexion` ➜ `/comptes/verifier-code` (redirection automatique si 2FA requis).
- **Pages orphelines (100 % des autres vues)** :
  - **30 pages sur 32** sont totalement impossibles à atteindre sans saisie manuelle de l'URL dans la barre d'adresse.
  - Même après une connexion réussie sur `/comptes/connexion`, aucune redirection vers un dashboard n'est effectuée : l'utilisateur reste sur la page de connexion !

---

## 4. Parcours Utilisateur par Persona & Ruptures Constatées

### Persona 1 : Client Société (Demandeur Import)
```
[Connexion] ──(1)──> [Accueil / Dashboard ?] ──(2)──> [Soumission BL] ──(3)──> [Suivi Demande] ──(4)──> [Paiement / Documents]
```
- *(1) Rupture Immédiate* : Après saisie des identifiants valides sur `/comptes/connexion`, l'utilisateur reste sur la page de connexion. Aucun tableau de bord ne s'affiche.
- *(2) Rupture Saisie* : L'utilisateur doit taper `/billing-import/soumettre`. Sur cette page, on lui demande de saisir son `societeId` GUID et son `auteurId` GUID au lieu de les extraire automatiquement de son compte connecté.
- *(3) Rupture Suivi* : Une fois le dossier créé, les numéros s'affichent, mais **aucun lien cliquable ne permet d'accéder au dossier ou aux tickets**. Il n'existe aucune page "Mes demandes" / "Mes dossiers".
- *(4) Rupture Paiement/Finaux* : Pour déposer sa preuve de paiement, le client doit aller sur `/billing-import/validation-paiement` (qui mélange formulaire client et file agent !) et ressaisir son `clientId` et `ticketId` à la main. Pour voir ses documents finaux, il doit aller sur `/billing-import/ticket-cloture` et ressaisir manuellement ses GUIDs.

### Persona 2 : Administrateur Société Cliente
- **Parcours attendu** : Connexion ➜ Tableau de bord entreprise ➜ Gestion des collaborateurs ➜ Rattachement / Droits / 2FA.
- **Réalité constatée** :
  - Doit taper `/comptes/collaborateurs`.
  - Doit saisir son propre `appelantId` GUID et son `societeId` GUID.
  - Pour rattacher un collaborateur, il doit saisir le GUID `nouvelUtilisateurId` d'un utilisateur déjà créé au préalable dans la base (aucun formulaire de création/invitation de collaborateur par email/nom).

### Persona 3 : Pilote Manifeste (Agent CIT)
- **Parcours attendu** : Connexion Agent ➜ Dashboard des files ➜ Sélection de la file Manifeste ➜ Instruction des tickets (Valider / Rejeter / Complément) ➜ Prise en charge du ticket suivant.
- **Réalité constatée** :
  - Doit taper `/billing-import/manifeste`.
  - Doit renseigner son `agentId` GUID et cliquer sur "Charger les tickets".
  - Voit la liste des tickets avec boutons d'action. Les actions fonctionnent parfaitement, mais le ticket n'offre aucun lien pour consulter le dossier complet, l'historique ou les pièces jointes associées.

### Persona 4 : Pilote Facturation / Caisse / Contrôle Documentaire / Comptabilité
- **Réalité constatée** : Même constat. Chaque agent doit se rendre sur son URL spécifique (`/billing-import/facturation`, `/billing-import/validation-paiement`, etc.), saisir son `agentId` GUID pour charger sa liste, téléverser sa pièce ou valider. Aucun aiguillage automatique ni navigation inter-files.

### Persona 5 : Superviseur Opérationnel
- **Réalité constatée** : Doit taper `/supervision`, saisir son `superviseurId` GUID. Le tableau affiche les volumes par file et par opérateur, mais aucun lien ne permet de cliquer sur une file pour voir ses tickets, ni de réaffecter un opérateur directement depuis la vue.

---

## 5. Matrice Backend vs UI (Fonctionnalités Métier vs Présentation)

| Domaine / Capacité Métier | Backend Implémenté | UI Présente | Accessible via Nav | Utilisable sans GUID manuel | Diagnostic & Écart |
|---|:---:|:---:|:---:|:---:|---|
| **Socle - Création Ticket générique** | ✅ 100% | ⚠️ Oui (`Tickets/Soumettre.razor`) | ❌ Non | ❌ Non | Formulaire technique demandant GUIDs |
| **Socle - File d'attente générique & PEC** | ✅ 100% | ⚠️ Oui (`Tickets/MaFile.razor`) | ❌ Non | ❌ Non | Demande fileId et agentId |
| **Socle - Consultation Détail Ticket** | ✅ 100% | ❌ Non (`DetailTicket.razor` n'affiche rien) | ❌ Non | ❌ Non | **Page vide de contenu**, que des boutons d'action |
| **Socle - Gestion Documentaire Ticket** | ✅ 100% | ⚠️ Oui (`Tickets/Documents.razor`) | ❌ Non | ❌ Non | Demande deposantId et radio type |
| **Socle - Messagerie & Notes Internes** | ✅ 100% | ⚠️ Oui (`Tickets/FilMessages.razor`) | ❌ Non | ❌ Non | Demande auteurId |
| **Socle - Suivi des Temps & SLA** | ✅ 100% | ⚠️ Oui (`Tickets/IndicateursTemps.razor`) | ❌ Non | ⚠️ Partiel | Lecture seule, calcul fonctionnel |
| **Socle - Dossier multi-BL** | ✅ 100% | ⚠️ Oui (`Dossiers/DetailDossier.razor`) | ❌ Non | ⚠️ Partiel | Affiche liste tickets, pas de liens |
| **Socle - Supervision temps réel** | ✅ 100% | ⚠️ Oui (`Supervision/TableauDeBord.razor`) | ❌ Non | ❌ Non | Demande superviseurId |
| **Socle - Audit & Disponibilité** | ✅ 100% | ⚠️ Oui (`Administration/*.razor`) | ❌ Non | ❌ Non | Vues techniques isolées |
| **Comptes - Inscription Société** | ✅ 100% | ✅ Oui (`Comptes/SoumettreDemande.razor`) | ❌ Non | ✅ Oui | Formulaire complet et propre |
| **Comptes - Connexion & 2FA** | ✅ 100% | ✅ Oui (`Connexion.razor`, `VerifierCode.razor`) | ⚠️ Partiel | ✅ Oui | Conforme, manque redirection post-login |
| **Comptes - Validation Société CIT** | ✅ 100% | ⚠️ Oui (`Comptes/ValiderDemandes.razor`) | ❌ Non | ❌ Non | Demande appelantId administrateur |
| **Comptes - Gestion Collaborateurs** | ✅ 100% | ⚠️ Oui (`Comptes/GestionCollaborateurs.razor`) | ❌ Non | ❌ Non | Demande adminId, societeId, nouvelUserId |
| **Comptes - Modification Type Client** | ✅ 100% | ⚠️ Oui (`Comptes/ModifierTypeClient.razor`) | ❌ Non | ❌ Non | Demande adminId, societeId |
| **Comptes - Tableau de bord Société** | ✅ 100% | ❌ **NON (Inexistant)** | ❌ Non | ❌ Non | **Page manquante** |
| **Import - Soumission multi-BL** | ✅ 100% | ⚠️ Oui (`BillingImport/SoumettreDemande.razor`) | ❌ Non | ❌ Non | Demande societeId et auteurId |
| **Import - File Pilote Manifeste** | ✅ 100% | ⚠️ Oui (`BillingImport/FilePiloteManifeste.razor`) | ❌ Non | ❌ Non | Demande agentId |
| **Import - File Facturation** | ✅ 100% | ⚠️ Oui (`BillingImport/FileFacturation.razor`) | ❌ Non | ❌ Non | Demande agentId |
| **Import - Dépôt Preuve Paiement Client** | ✅ 100% | ⚠️ Oui (`ValidationPaiement.razor`) | ❌ Non | ❌ Non | Mélangé avec la file agent |
| **Import - Validation Paiement Agent** | ✅ 100% | ⚠️ Oui (`ValidationPaiement.razor`) | ❌ Non | ❌ Non | Demande agentId |
| **Import - Contrôle Documentaire** | ✅ 100% | ⚠️ Oui (`FileControleDocumentaire.razor`) | ❌ Non | ❌ Non | Demande agentId |
| **Import - Lettrage Comptabilité** | ✅ 100% | ⚠️ Oui (`FileComptabiliteClient.razor`) | ❌ Non | ❌ Non | Demande agentId |
| **Import - Traitement Parallèle IES** | ✅ 100% | ⚠️ Oui (`FileIes.razor`) | ❌ Non | ❌ Non | Demande agentId |
| **Import - Consultation Ticket Clôturé** | ✅ 100% | ⚠️ Oui (`ConsulterTicketCloture.razor`) | ❌ Non | ❌ Non | Demande clientId et ticketId |
| **Dashboard Client ("Mes Actions")** | ⚠️ Partiel | ❌ **NON (Inexistant - ACR-01)** | ❌ Non | ❌ Non | **Page manquante** |
| **Dashboard Agent ("Mes Tâches")** | ⚠️ Partiel | ❌ **NON (Inexistant)** | ❌ Non | ❌ Non | **Page manquante** |

---

## 6. Analyse de la Qualité UX & Ergonomie

1. **Cohérence Visuelle & Charte Graphique** :
   - Les pages utilisent le CSS Bootstrap basique non personnalisé.
   - Les formulaires manquent d'espacement, d'icônes, de badges de statut colorés et de hiérarchie visuelle claire.
   - Aucun composant d'en-tête (Hero header, profil connecté, statut de session).
2. **Gestion des États Vides & Chargement** :
   - Présence de messages "Aucun ticket" ou tables vides basiques.
   - Pas de skeletons loaders ni de spinners élégants pendant les requêtes asynchrones.
3. **Formulaires & Validations** :
   - Les formulaires d'authentification et de soumission de compte sont validés proprement via `EditForm`.
   - Les pages de démonstration utilisent de simples balises `<input>` sans validation Blazor native.
4. **Composants d'Action & Modales** :
   - Les rejets (Manifeste, Paiement) utilisent un mini champ texte sous la ligne du tableau, sans modale de confirmation propre.
   - Les confirmations d'actions engageantes (Clôture, Rejet) ne demandent pas de validation explicite avant soumission.
5. **Responsive & Mobile** :
   - Les tableaux HTML simples ne sont pas adaptés aux smartphones (scroll horizontal ou coupure de colonnes).

---

## 7. Analyse du Dashboard (Tableaux de Bord Manquants)

L'audit révèle l'absence complète de **véritables tableaux de bord** par profil utilisateur :

### A. Dashboard Client (Exigence CDC `ACR-01`) — MANQUANT
- **Ce qui manque** :
  - Widget « Mes actions requises » (ex. Déposer un complément demandé par le Manifeste, Déposer la preuve de paiement suite à facture éditée, Récupérer mon bon de sortie BS/BADT).
  - Liste de « Mes demandes en cours » avec statut dynamique, numéro de dossier, date de mise à jour.
  - Raccourci rapide « Nouvelle demande Billing Import ».
  - Notifications récentes.

### B. Dashboard Agent CIT — MANQUANT
- **Ce qui manque** :
  - Liste des files de traitement attribuées à l'agent avec compteur de tickets en instance.
  - Liste des tickets actuellement pris en charge par l'agent.
  - Alertes SLA prioritaires (tickets proches de l'échéance ou dépassés).
  - Indicateur de disponibilité opérationnelle (Disponible / En pause / Absent).

### C. Dashboard Administrateur Société — MANQUANT
- **Ce qui manque** :
  - Vue synthétique de l'entreprise (Raison sociale, profil, statut de validation).
  - Compteur des collaborateurs actifs et administrateurs.
  - Accès direct à l'ajout et la gestion des habilitations.
  - Vue d'ensemble de toutes les demandes soumises par les collaborateurs de la société.

---

## 8. Options de Traitement et Comparatif Stratégique

Deux approches sont envisageables pour combler cette dette UI/UX :

| Dimension | OPTION A : Feature dédiée "UI/UX Consolidation F001-F003" | OPTION B : Correction progressive au fil des features futures |
|---|---|---|
| **Description** | Créer une feature transverse immédiate (ex. `003.1-ui-ux-consolidation` ou `004-ui-ux-plateforme`) dédiée à l'expérience utilisateur, la navigation, les dashboards et la suppression des GUIDs manuels. | Reporter la reprise des écrans au fil du développement des features suivantes (F004 Export, F005 Proforma, etc.). |
| **Coût / Effort** | **Modéré et ciblé** (3-4 jours) : le backend est 100 % prêt, il ne s'agit que d'assembler les composants Razor dans un shell applicatif cohérent. | **Élevé et dispersé** : chaque nouvelle feature devra concevoir son propre morceau d'UI sans socle de navigation commun, multipliant le refactoring. |
| **Risque technique** | **Très faible** : n'impacte pas le Domaine ni les règles métier déjà validées (180 tests préservés). | **Élevé** : accumulation de dette technique UI, tests d'intégration Web difficiles et incohérents. |
| **Valeur Démo / Métier** | **Immédiate et maximale** : permet de tester et démontrer immédiatement à CIT l'intégralité des parcours Import, Comptes et Socle de bout en bout dans un vrai navigateur. | **Nulle à court terme** : les fonctionnalités restent inexploitables sans manipulation manuelle de GUIDs. |
| **Impact Roadmap** | Clarifie et stabilise définitivement le shell Web avant d'attaquer l'Export et la Simulation. | Ralentit toutes les features futures par manque de socle visuel commun. |

---

## 9. Recommandation Finale et Plan d'Action Proposé

### Recommandation Formelle : **OPTION A — Feature dédiée de Consolidation UI/UX**

Il est recommandé de consolider l'interface utilisateur avant d'engager la Feature suivante.

### Périmètre Proposé pour la Consolidation UI/UX :

1. **Shell Applicatif & Navigation Globale** :
   - Mise en place d'un vrai layout (`AppShell` / `MainLayout`) avec Navbar supérieure (Logo CIT, statut connexion, profil utilisateur, déconnexion) et Sidebar latérale contextuelle selon le rôle de l'utilisateur connecté.
   - Redirection automatique post-connexion vers le tableau de bord approprié selon les claims de l'utilisateur.
2. **Résolution Automatique du Contexte Connecté (Éradication des GUIDs manuels)** :
   - Remplacement de tous les champs de saisie manuelle (`agentId`, `societeId`, `userId`, `clientId`) par l'injection de `AuthenticationStateProvider` et la lecture des claims de session.
3. **Création des 3 Grands Tableaux de Bord** :
   - **Dashboard Client** (`/client/dashboard`) avec widget `ACR-01` (« Mes actions requises »), liste des dossiers actifs et bouton « Nouvelle demande Import ».
   - **Dashboard Agent / Espace Métier** (`/agent/dashboard`) avec sélecteur de files attribuées, compteurs de tickets et état de disponibilité.
   - **Dashboard Admin Société** (`/societe/dashboard`) avec gestion intégrée des collaborateurs et vue d'ensemble des dossiers de la société.
4. **Fiches et Vues Détaillées Riches** :
   - Vraie fiche détaillée de ticket (`/tickets/{guid}`) affichant le statut, l'historique, les documents téléchargeables et le fil de discussion intégré.
   - Vue dossier multi-BL unifiée.
5. **Parcours Import Fluide** :
   - Parcours complet de soumission ➜ instruction manifeste ➜ facture ➜ preuve paiement (séparée côté client) ➜ BS/BADT sans jamais saisir un seul identifiant technique.

---

*(Fin du rapport d'audit — En attente de validation humaine).*
