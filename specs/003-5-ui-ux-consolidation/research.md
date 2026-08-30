# Architecture Research & Technical Decisions: Consolidation UI/UX F001-F003

**Feature Branch**: `003-5-ui-ux-consolidation`  
**Date**: 2026-08-30  
**Phase**: `/speckit.plan`

---

## 1. Vue d'Ensemble Architecturale

Cette feature de consolidation d'expérience utilisateur est une couche de **présentation et d'intégration** (Presentation / Application Read-Models) qui unifie les capacités métier déjà livrées dans les Features 001, 002 et 003.

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    COUCHE PRÉSENTATION (Blazor Server)                  │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │ Shell Applicatif (MainLayout, Header, NavMenu, PersonaSwitcher)   │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────┐ ┌────────────────────┐ ┌─────────────────────┐  │
│  │  Dashboard Client  │ │  Dashboard Agent   │ │  Dashboard Société  │  │
│  │   (ACR-01 Widget)  │ │   (Files & Charge) │ │  (Collaborateurs)   │  │
│  └────────────────────┘ └────────────────────┘ └─────────────────────┘  │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │ Fiches Métier Unifiées (/tickets/{id}, /dossiers/{id})            │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │ Files Métier Refactorées (/billing-import/* sans GUIDs manuels)   │  │
│  └───────────────────────────────────────────────────────────────────┘  │
└────────────────────────────────────┬────────────────────────────────────┘
                                     │ Appels Read-Models (Queries)
                                     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                     COUCHE APPLICATION (Read Models & CQRS)             │
│                                                                         │
│  • ObtenirContexteUtilisateurQuery                                      │
│  • ObtenirDashboardClientQuery (ACR-01)                                 │
│  • ObtenirDashboardAgentQuery                                           │
│  • ObtenirDashboardSocieteQuery                                         │
│  • ObtenirDetailTicketQuery                                             │
│  • ObtenirDetailDossierQuery                                            │
│                                                                         │
│  (Réutilise strictement IAutorisationService, CloisonnementService,    │
│   IContexteUtilisateur, et les Commands existantes F001-F003)          │
└────────────────────────────────────┬────────────────────────────────────┘
                                     │ Lecture Entity Framework Core
                                     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                 DOMAINE & PERSISTANCE (Inchangés à 100 %)               │
│                                                                         │
│  • Tickets, Dossiers, Messages, Documents, SLA, Supervision (F001)     │
│  • Societes, UtilisateursSociete, Identity, 2FA, TypesClient (F002)    │
│  • DemandesImport, UniciteBl, Facturation, IES, Paiement (F003)        │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Décisions Techniques Structurantes (D1 à D15)

### D1 — Contexte Utilisateur & Persona Dynamique
- **Décision** : Création d'un service applicatif `IContexteUtilisateurWeb` injecté dans les composants Blazor et s'appuyant sur `AuthenticationStateProvider`.
- **Mécanisme** :
  - Extrait l'identifiant Identity (`UtilisateurId`) du `ClaimsPrincipal`.
  - Résout le rattachement société via `IUtilisateurSocieteRepository` (`SocieteId`, `EstAdministrateur`, `DroitConsultation`, `DroitSoumission`).
  - Résout les habilitations internes via `IAutorisationService` (droits de file, statut de superviseur, droits d'administration CIT).
  - Détermine les personas disponibles : `Client`, `AdministrateurSociete`, `Agent`, `Superviseur`, `AdministrateurCit`.
  - Mémorise l'espace actuellement actif (par défaut : l'espace le plus élevé, commutable via le Header).
- **Sécurité** : Aucun identifiant technique de sécurité (`SocieteId`, `UtilisateurId`) n'est modifiable ou injecté par le client dans les formulaires. Tout est déduit côté serveur.

### D2 — Construction Dynamique de la Navigation (NavMenu)
- **Décision** : Le composant `NavMenu.razor` interroge `IContexteUtilisateurWeb` pour construire son arborescence de liens.
- **Règles d'affichage** :
  - Si `EspaceCourant == Client` ➜ Tableau de bord (`/client/dashboard`), Mes demandes (`/client/demandes`), Nouvelle demande Import (`/billing-import/soumettre`), Profil & Sécurité (`/comptes/profil`, `/comptes/double-authentification`).
  - Si `EspaceCourant == AdministrateurSociete` ➜ Tableau de bord entreprise (`/societe/dashboard`), Collaborateurs (`/societe/collaborateurs`), Demandes entreprise (`/client/demandes?vue=societe`), Profil.
  - Si `EspaceCourant == Agent` ➜ Tableau de bord agent (`/agent/dashboard`), Files de traitement attribuées (Manifeste, Facturation, Paiement, Contrôle Documentaire, Comptabilité, IES, Prise libre).
  - Si `EspaceCourant == Supervision` ➜ Supervision (`/supervision`), Disponibilités (`/administration/disponibilite`), Stratégie files (`/administration/strategie-file`), Audit (`/administration/audit`), Demandes de compte (`/administration/comptes-a-valider`), Modification type client (`/administration/modifier-type-client`).
- **Garde-fou (Principe VII)** : Le masquage visuel n'est qu'un confort. Chaque page exécute son contrôle d'autorisation serveur.

### D3 — Redirection Post-Login & Landing Page (`/`)
- **Décision** : La page racine `/` (`Home.razor`) est convertie en routeur intelligent Blazor.
- **Algorithme d'orientation** :
  1. Si visiteur non authentifié ➜ Affiche la page de bienvenue CIT épurée avec boutons « Se connecter » et « Créer un compte ».
  2. Si utilisateur authentifié ➜ Résolution du persona prioritaire :
     - `Superviseur / Admin CIT` ➜ `/supervision`
     - `Agent CIT` ➜ `/agent/dashboard`
     - `Administrateur Société` ➜ `/societe/dashboard`
     - `Client Société` ➜ `/client/dashboard`
  3. Dans `Connexion.razor`, après succès du mot de passe (et du défi 2FA si activé), `NavigationManager.NavigateTo("/")` est exécuté, déclenchant l'orientation automatique.

### D4 — Read-Model du Dashboard Client
- **Décision** : Création de la Query `ObtenirDashboardClientQuery` produisant le ViewModel `DashboardClientVue`.
- **Contenu du Read-Model** :
  - `SocieteNom`, `SocieteProfil` (Normal, En Compte, Deposit, IES).
  - `ActionsRequises` : liste des éléments `ActionRequiseItemVue` (`ACR-01`).
  - `DemandesRecentes` : 5 derniers dossiers avec état dérivé, date, nombre de BL, statut des tickets rattachés.
  - `Compteurs` : Total demandes actives, Demandes en cours CIT, Demandes nécessitant une action client.
- **Filtrage de sécurité** : Filtré strictement sur `SocieteId` de l'utilisateur connecté (et sur `AuteurId` si droit restreint à ses propres demandes).

### D5 — Implémentation et Calcul d'ACR-01 ("Mes actions requises")
- **Décision** : Dérivation à la volée via une projection LINQ efficace (sans table dédiée, sans risque de désynchronisation).
- **Règles de dérivation** :
  1. Ticket avec statut `StatutTicket.EnAttenteClient` ET motif attente complément ➜ Type `ComplementManifeste`, Libellé *« Complément demandé sur le BL {NumeroBl} »*, Action *« Déposer les pièces »*, Route `/tickets/{Id}#complement`.
  2. Ticket avec statut `StatutTicket.EnAttentePaiement` pour client Normal ➜ Type `PaiementAttendu`, Libellé *« Facture émise — Preuve de paiement requise pour le BL {NumeroBl} »*, Action *« Déposer la preuve de paiement »*, Route `/tickets/{Id}#paiement`.
  3. Ticket avec statut `StatutTicket.EnAttenteClient` ET motif refus preuve paiement ➜ Type `NouvellePreuveRequise`, Libellé *« Preuve de paiement refusée — Nouvelle preuve attendue pour le BL {NumeroBl} »*, Action *« Déposer une nouvelle preuve »*, Route `/tickets/{Id}#paiement`.
- **Disparition** : Dès que la commande correspondante (`RepondreComplement`, `DeposerPreuvePaiement`) est exécutée, le statut du ticket évolue et l'action disparaît instantanément de la liste.

### D6 — Read-Model du Dashboard Agent
- **Décision** : Création de la Query `ObtenirDashboardAgentQuery` produisant `DashboardAgentVue`.
- **Contenu** :
  - `DisponibiliteCourante` (Disponible / En pause / Absent).
  - `Files` : Liste des files autorisées avec nom métier, permission associée, compteur de tickets « En attente » et « En cours », lien direct vers la page de file.
  - `TicketsPrisEnCharge` : Liste des tickets actuellement affectés à l'agent connecté (`OperateurId == agentId`).
  - `AlertesSla` : Tickets en dépassement ou proches du seuil 80% (calculés via les dates d'arrivée en file).

### D7 — Read-Model du Dashboard Administrateur Société
- **Décision** : Création de la Query `ObtenirDashboardSocieteQuery` produisant `DashboardSocieteVue`.
- **Contenu** :
  - `Societe` : Raison sociale, profil (Normal/EnCompte/Deposit/IES), statut de validation, informations déclarées.
  - `StatistiquesCollaborateurs` : Total, Actifs, Inactifs, Administrateurs.
  - `ApercuCollaborateurs` : 5 derniers utilisateurs rattachés avec statut et rôles.
  - `DossiersSocieteRecents` : Derniers dossiers soumis par l'ensemble des collaborateurs (`CLAR-02`).

### D8 — Fiche Ticket Métier Unifiée (`/tickets/{id}`)
- **Décision** : Création de la Query `ObtenirDetailTicketQuery` produisant `TicketDetailVue`.
- **Contenu complet** :
  - En-tête : Numéro ticket, BL associé, Statut coloré, Dossier parent, Module, Type demande, Profil client snapshot (`TypeClient`).
  - Suivi opérationnel : File actuelle, Opérateur en charge, Date arrivée, Date prise en charge, Dernière activité.
  - Documents : Liste complète des pièces avec nom, taille, date, statut antivirus et lien de téléchargement direct.
  - Messagerie : Fil des messages horodatés (sélection stricte : messages publics pour le client ; messages publics + notes internes `MSG-02` pour l'agent).
  - Historique : Chronologie des événements du cycle de vie.
  - Actions permises : Liste `ActionsPermisesTicket` calculée côté serveur selon le statut et l'habilitation de l'appelant.

### D9 — Fiche Dossier Multi-BL (`/dossiers/{id}`)
- **Décision** : Enrichissement de la Query `ConsulterDossierQuery` / nouveau ViewModel `DossierDetailVue`.
- **Contenu** :
  - Numéro de dossier, Société propriétaire, Auteur, Date création, État global dérivé (`EnAttentePriseEnCharge`, `EnCours`, `Cloture`, `Rejete`, `PartiellementTraite`).
  - Tableau des tickets/BL rattachés démontrant l'indépendance de chaque BL (IMP-02) avec statut individuel, montant facture, et bouton d'accès à chaque fiche ticket.

### D10 — Convention de Sécurité 403 / 404
- **Décision** :
  - **401 Non Authentifié** ➜ Redirection automatique vers `/comptes/connexion?returnUrl=...`.
  - **403 Accès Refusé** ➜ Utilisateur connecté tentant d'accéder à un ticket/dossier d'une autre société, ou à une file pour laquelle il ne détient pas la permission requise. La page rend le composant `<PermissionDenied />` avec message sobre, sans révéler d'information sur la ressource ciblée.
  - **404 Non Trouvé** ➜ Ressource dont l'identifiant GUID n'existe pas en base (`NotFound.razor`).

### D11 — Design System Léger & CSS Tokens
- **Décision** : Exploitation de Bootstrap 5 existant complété par des variables CSS (`app.css`) pour l'identité visuelle CIT.
- **Composants partagés standardisés** :
  - `<StatutBadge />` (badges colorés harmonisés pour tous les statuts).
  - `<ZoneActionsRequises />` (widget ACR-01).
  - `<EtatVide />` (empty state élégant avec icône et action).
  - `<IndicateurChargement />` (skeleton loader).
  - `<ModaleConfirmation />` (dialogue de confirmation accessible pour rejets/clôtures).
  - `<ToastNotification />` (feedback visuel temporaire succès/erreur).

### D12 — Responsive & Ergonomie Mobile (`NFR-02`)
- **Décision** :
  - *Sidebar Desktop* (`d-none d-lg-block`) fixe à gauche.
  - *Menu Mobile Offcanvas* (`d-lg-none`) ouvert via bouton burger dans le header mobile.
  - *Tableaux* : Encapsulés dans `.table-responsive` avec typographie dense et actions compactes. Sur mobile, le dashboard client privilégie l'affichage en cartes empilées (`TicketSummaryCard`).

### D13 — Plan de Migration des Routes
- **Routes Cibles Principales** :
  - `/` (Router d'accueil)
  - `/client/dashboard` & `/client/demandes`
  - `/societe/dashboard` & `/societe/collaborateurs`
  - `/agent/dashboard`
  - `/tickets/{TicketId:guid}` & `/dossiers/{DossierId:guid}`
  - `/billing-import/soumettre` (alias `/billing-import/nouvelle`)
  - `/billing-import/manifeste`, `/billing-import/facturation`, `/billing-import/validation-paiement`, `/billing-import/controle-documentaire`, `/billing-import/comptabilite`, `/billing-import/ies`, `/billing-import/ticket-cloture`
  - `/supervision`, `/administration/audit`, `/administration/disponibilite`, `/administration/strategie-file`, `/administration/comptes-a-valider`, `/administration/modifier-type-client`
- Les anciennes URLs techniques sont conservées en redirection/alias transparent pour assurer la non-régression des tests d'intégration existants.

### D14 — Stratégie de Test E2E & Composants
- **Tests bUnit** : Validation unitaire des composants isolés (`StatutBadgeTests`, `ActionsRequisesWidgetTests`, `NavMenuContextuelTests`).
- **Tests WebApplicationFactory** : Validation des parcours E2E complets (`E2E-01` à `E2E-06`) simulant des contextes authentifiés avec cookies réels et bases de données SQL Server réelles.

### D15 — Éradication Complète des Saisies de GUIDs
- **Inventaire de suppression** :
  - `BillingImport/SoumettreDemande.razor` ➜ Supprimer `InputText societe` et `InputText auteur` (résolus depuis session).
  - `BillingImport/FilePiloteManifeste.razor` ➜ Supprimer `InputText agent` (résolu depuis session).
  - `BillingImport/FileFacturation.razor` ➜ Supprimer `input agent` (résolu depuis session).
  - `BillingImport/ValidationPaiement.razor` ➜ Supprimer `input agent`, `input clientId`, `input ticketIdClient` (scindé en dépôt fiche ticket et file agent contextuelle).
  - `BillingImport/FileControleDocumentaire.razor` ➜ Supprimer `input agent`.
  - `BillingImport/FileComptabiliteClient.razor` ➜ Supprimer `input agent`.
  - `BillingImport/FileIes.razor` ➜ Supprimer `input agent`.
  - `Comptes/ValiderDemandes.razor` ➜ Supprimer `InputText appelant`.
  - `Comptes/GestionCollaborateurs.razor` ➜ Supprimer `InputText appelant`, `InputText societe`.
  - `Comptes/ModifierTypeClient.razor` ➜ Supprimer `InputText appelant`.
  - `Tickets/DetailTicket.razor` ➜ Supprimer `input initiateurId`, `input nouvelOperateurId`, `input fileCibleId` (remplacés par modales et formulaires contextualisés).
  - `Supervision/TableauDeBord.razor` ➜ Supprimer `input superviseurId`.

---
*(Fin des décisions de recherche — Prêt pour plan.md).*
