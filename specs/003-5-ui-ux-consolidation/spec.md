# Feature Specification: Consolidation UI/UX des Features 001 à 003

**Feature Branch**: `003-5-ui-ux-consolidation`

**Created**: 2026-08-30

**Status**: Clarified — Spécification formelle de consolidation de l'expérience utilisateur et de la navigation post-`/speckit.clarify` (22 clarifications enregistrées, 0 nouvelle règle métier inventée).

**Input**: User description : « Consolidation UI/UX F001-F003 — Transformer l'application actuelle d'un ensemble de pages techniques isolées en une application métier navigable, fluide et directement utilisable dans le navigateur, sans modifier aucune règle métier, sans inventer de nouveaux rôles, et en éliminant toute saisie manuelle d'identifiants techniques (GUIDs). »

---

## 1. Sources de Vérité et Alignement Projet

- **Constitution** : `.specify/memory/constitution.md` (v1.0.0 — Principes I à XVII, notamment Principe VII sur le moindre privilège et la sécurité côté serveur, Principe VIII sur l'audit, Principe IX sur l'immutabilité).
- **Rapport d'audit UI/UX** : `docs/06-audit-ui-ux-f001-f003.md` (Diagnostic exhaustif des 32 vues Razor, des ruptures de parcours et des manques d'intégration).
- **Cahier des charges & Matrices** : `docs/CDC_Plateforme_Digitale_CIT_v3.docx`, `docs/01-matrice-exigences.md` (notamment `ACR-01` pour le widget « Mes actions requises » et `NFR-01`/`NFR-02` pour le responsive et l'ergonomie en français).
- **Features de référence fermées** :
  - `specs/001-socle-tickets-workflows/` (Socle transverse de tickets, dossiers, files, transitions, SLA, alertes, supervision, audit).
  - `specs/002-comptes-clients-societes/` (Comptes sociétés, 2FA, multi-utilisateurs, gestion des droits, profil client Normal/EnCompte/Deposit/IES, non-rétroactivité).
  - `specs/003-billing-office-import/` (Parcours de facturation Import multi-BL, 4 variantes de workflow, unicité BL, contrôle documentaire).

### Baseline de Non-Régression
La suite de tests actuelle compte **260 tests automatisés** validés à 100 % (37 Domain, 159 Application, 36 Infrastructure avec SQL Server réel, 28 Web), 0 avertissement, 0 échec. Cette suite constitue le **baseline minimum non négociable** devant être préservé intact tout au long de la consolidation UI/UX.

---

## 2. Objectif Principal et Périmètre

### 2.1 Objectif
Rendre les **110 exigences fonctionnelles déjà implémentées** dans le backend (plus `ACR-01` couverte par cette feature, soit **111 exigences au total**) réellement praticables et navigables dans le navigateur web par les utilisateurs finaux (clients, collaborateurs d'entreprise, agents de file CIT, superviseurs et administrateurs), en supprimant tout besoin de :
- Connaître ou saisir manuellement une URL technique dans la barre d'adresse ;
- Saisir manuellement des identifiants techniques sous forme de chaînes GUID (`societeId`, `auteurId`, `agentId`, `fileId`, `ticketId`, `appelantId`) ;
- Deviner l'état d'avancement d'un dossier faute de tableau de bord structuré.

### 2.2 Périmètre Négatif Strict (Non-Goals)
Cette feature de consolidation d'expérience utilisateur :
- ❌ **NE crée AUCUNE nouvelle règle métier** ;
- ❌ **NE modifie AUCUNE exigence fonctionnelle déjà validée** de F001, F002 ou F003 ;
- ❌ **NE réimplémente AUCUN composant backend** du Domaine ni des Services de base de données existants ;
- ❌ **NE modifie AUCUN workflow métier** ni aucune transition d'état ;
- ❌ **N'invente AUCUN nouveau rôle utilisateur** (utilise strictement les rôles et habilitations existants) ;
- ❌ **NE commence PAS la Feature 004** (Export), ni la Simulation Proforma (F005), ni les Avoirs (F006), ni les Réclamations (F007), ni l'Enrôlement (F008), ni l'Assistant IA ;
- ❌ **N'ajoute aucun framework CSS lourd externe** (s'appuie sur le CSS Vanilla / Bootstrap déjà inclus dans le projet).

---

## 3. Clarifications Fonctionnelles et Décisions de Cadrage (`/speckit.clarify`)

### 3.1 CLAR-UX-01 — Landing Page et Redirection Post-Connexion
- **Question** : Quel est le comportement immédiat après connexion ou à l'accès à la racine `/` ?
- **Décision** :
  - La racine `/` oriente automatiquement un utilisateur connecté vers son tableau de bord principal selon son contexte fonctionnel et ses habilitations :
    - `Superviseur / Administrateur CIT` ➜ `/supervision` (ou `/administration/comptes-a-valider`)
    - `Agent CIT` ➜ `/agent/dashboard`
    - `Administrateur Société` ➜ `/societe/dashboard`
    - `Client Société (collaborateur standard)` ➜ `/client/dashboard`
    - `Visiteur non connecté` ➜ Présentation d'accueil CIT épurée avec bouton d'accès vers `/comptes/connexion` et `/comptes/soumettre`.
  - **Gestion multi-capacités** : Si un utilisateur possède des compétences transverses (ex. Agent et Superviseur, ou Client et Admin Société), l'application le dirige vers son espace le plus élevé et affiche dans le Header un sélecteur d'espace (« Basculer d'espace ») permettant de passer d'un dashboard à l'autre en un clic.

### 3.2 CLAR-UX-02 — Persona ≠ Rôle Technique Unique
- **Question** : Comment l'interface détermine-t-elle l'affichage et les menus ?
- **Décision** : Le persona est une synthèse fonctionnelle déduite de la session :
  - Société rattachée (`UtilisateurSociete.SocieteId != null`)
  - Statut administrateur d'entreprise (`EstAdministrateur == true`)
  - Permissions CIT attribuées (`IAutorisationService.AAccesAsync(...)`)
  - Périmètre de supervision attribué
- L'interface adapte dynamiquement la visibilité des menus selon la combinaison exacte des habilitations détenues par l'utilisateur connecté.

### 3.3 CLAR-UX-03 — Navigation Contextuelle et Sécurité Côté Serveur (Principe VII)
- **Question** : Comment concilier masquage ergonomique et contrôle d'accès ?
- **Décision** : Les sections non autorisées sont masquées dans la sidebar pour ne pas encombrer l'utilisateur. **Toutefois, le masquage ne constitue jamais une mesure de sécurité**. Tout composant et toute route Blazor exécutent systématiquement la vérification d'autorisation côté serveur. L'accès direct non autorisé déclenche un écran **403 Accès Refusé**.

### 3.4 CLAR-UX-04 — Scope de Visibilité des Demandes Client (`ENT-03` / `CLAR-02`)
- **Question** : Que doit afficher « Mes demandes » pour un utilisateur client ?
- **Décision** : Application stricte des règles validées dans F002 :
  - Utilisateur avec droit `PropresDemandesUniquement` ➜ Liste filtrée sur `Dossier.AuteurId == utilisateur.Id`.
  - Utilisateur avec droit `ToutesDemandesSociete` ou `Administrateur de Société` ➜ Liste de toutes les demandes de sa société (`Dossier.SocieteId == utilisateur.SocieteId`).
  - L'écran propose un filtre interactif pour les utilisateurs autorisés à la visibilité globale (« Mes demandes » vs. « Toutes les demandes de l'entreprise »).

### 3.5 CLAR-UX-05 — Dérivation Rigoureuse de « Mes Actions Requises » (`ACR-01`)
- **Question** : Quelles actions précises apparaissent dans le widget `ACR-01` ?
- **Décision** : Aucune machine d'état spécifique n'est créée. Les actions requises sont **strictement dérivées des états réels existants** des tickets de la société :
  1. **Complément d'information / pièce demandé** (Statut `EnAttenteClient`, motif attente complément émis par le Manifeste).  
     ➜ *Libellé* : « Complément demandé pour le BL {NumeroBl} » ➜ *Action* : bouton ouvrant directement la zone de dépôt sur le ticket.
  2. **Preuve de paiement attendue suite à facture émise** (Statut `EnAttentePaiement` pour un client Normal).  
     ➜ *Libellé* : « Facture disponible — Preuve de paiement attendue pour le BL {NumeroBl} » ➜ *Action* : bouton ouvrant la zone de téléversement de preuve sur le ticket.
  3. **Nouvelle preuve requise suite à refus Caisse** (Statut `EnAttenteClient`, motif refus paiement).  
     ➜ *Libellé* : « Preuve de paiement refusée — Nouvelle pièce attendue pour le BL {NumeroBl} » ➜ *Action* : bouton de re-dépôt de preuve.
  - Dès qu'une action est exécutée par le client, le statut change en base et le ticket disparaît automatiquement du widget `ACR-01`.

### 3.6 CLAR-UX-06 — Dashboard Agent : Périmètre de "Mes Files"
- **Question** : Quelles files afficher et comment structurer le dashboard agent ?
- **Décision** :
  - Affiche toutes les files pour lesquelles l'agent dispose de la permission correspondante.
  - Présente pour chaque file le compteur de tickets « En attente de prise en charge » et « En cours ».
  - Isole une section claire « Mes tickets pris en charge » (assignés nominativement à l'agent connecté).
  - Fournit un sélecteur de disponibilité opérationnelle en haut de page (`Disponible`, `En pause`, `Absent` — `DIS-01`).

### 3.7 CLAR-UX-07 — Dashboard Administrateur Société
- **Question** : Quel est le rôle du dashboard entreprise ?
- **Décision** : Synthèse de l'entreprise (profil, statut), compteurs des collaborateurs (actifs/inactifs, admins), et liens directs vers la page dédiée `/societe/collaborateurs` et la liste globale des demandes. Il évite de dupliquer les formulaires complexes sur la page d'accueil.

### 3.8 CLAR-UX-08 — Fiche Ticket : Actions Contextuelles Conditionnées
- **Question** : Comment présenter les boutons d'action sur la fiche ticket ?
- **Décision** : La fiche charge les données du ticket, l'état courant et l'habilitation de l'utilisateur. Seuls les boutons réellement autorisés dans cet état sont rendus (ex. « Valider / Rejeter » uniquement pour le Pilote Manifeste sur un ticket en attente manifeste ; « Déposer facture » uniquement pour le Pilote Facturation). Aucun bouton non opérant n'est affiché.

### 3.9 CLAR-UX-09 — Fiche Ticket : Cloisonnement des Données et Confidentialité
- **Question** : Quelles informations sont visibles selon le persona ?
- **Décision** :
  - **Client** : Voit son numéro, son BL, le statut grand public, les documents téléchargeables (facture, BS, BADT) et les messages publics. **Ne voit JAMAIS les notes internes CIT (`MSG-02`)**.
  - **Agent** : Voit l'ensemble des métadonnées opérationnelles, les pièces, les messages publics ET les notes internes.
  - **IES (`PC-05`)** : Respecte l'isolation des branches Manifeste et Contrôle Documentaire.

### 3.10 CLAR-UX-10 — Distinction Historique Métier vs Journal d'Audit (`SOC-13`)
- **Question** : Où et pour qui afficher l'audit ?
- **Décision** :
  - **Historique métier** (jalons du ticket : création, étapes franchies, horodatages) : visible directement sur la fiche ticket par le client et l'agent.
  - **Journal d'audit technique inviolable (`SOC-13`)** : accessible uniquement aux administrateurs techniques et superviseurs via `/administration/audit`, jamais affiché sur la fiche client.

### 3.11 CLAR-UX-11 — Éradication Catégorisée des GUIDs Manuels
- **Classification** :
  - *Catégorie A (Contexte de session)* : `userId`, `societeId`, `auteurId`, `agentId`, `appelantId`, `superviseurId` ➜ 100 % déduits côté serveur.
  - *Catégorie B (Navigation)* : `ticketId`, `dossierId` ➜ transmis dans l'URL `/tickets/{id}` ou `/dossiers/{id}` via des liens cliquables.
  - *Catégorie C (Sélection métier)* : `fileId`, `typeClient`, etc. ➜ choisis via des sélecteurs visuels ou boutons.
  - **Résultat** : 0 champ de saisie texte de GUID dans tous les parcours utilisateurs finaux.

### 3.12 CLAR-UX-12 — Convention de Retour Après Action
- **Question** : Quel est le comportement après une action réussie ?
- **Décision** :
  - *Action depuis la fiche ticket* ➜ Reste sur la fiche ticket, rafraîchit les données sur place et affiche un toast/alerte de succès.
  - *Action depuis une file de traitement* ➜ Rafraîchit la file de traitement et affiche un message de succès (« Ticket {numéro} traité avec succès »).
  - *Création d'une nouvelle demande Import* ➜ Redirige immédiatement vers le dossier créé (`/dossiers/{id}`).

### 3.13 CLAR-UX-13 — Traçabilité Contractuelle de l'Exigence `ACR-01`
- **Exigence CDC** : `ACR-01` (Must, Section 17.1 du CDC « Espace client »).
- **Statut dans cette feature** : Formellement couverte par le composant `<ZoneActionsRequises />` sur le tableau de bord client. La couverture globale du CDC passe de 110 à **111 exigences** (70 Must F001/002 + 23 Must F003 + 1 Must ACR-01 = 94 Must réalisés).

---

## 4. Inventaire des Vues et Stratégie d'Évolution

### 4.1 Vues à Conserver (Intactes ou Ajustements Mineurs)
1. `/comptes/connexion` (`Comptes/Connexion.razor`) — Conserver la logique Identity + 2FA ; ajouter la redirection automatique post-login vers le tableau de bord contextuel.
2. `/comptes/verifier-code` (`Comptes/VerifierCode.razor`) — Conserver le défi TOTP ; ajouter la redirection post-validation.
3. `/comptes/soumettre` (`Comptes/SoumettreDemande.razor`) — Conserver le formulaire d'inscription société ; enrichir le retour visuel.
4. `/comptes/mot-de-passe-oublie` & `/comptes/reinitialiser-mot-de-passe` — Conserver les mécanismes Identity natifs.
5. `/comptes/double-authentification` (`Comptes/ActiverDoubleAuthentification.razor`) — Conserver la gestion TOTP via claims de session.
6. `/error` & `/not-found` — Pages système de base.

### 4.2 Vues à Refactorer (Éradication des GUIDs manuels & Intégration Navigation)
1. `/tickets/{TicketId:guid}` (`Tickets/DetailTicket.razor`) ➜ **Transformer en vraie fiche ticket métier complète** : afficher le numéro, le BL, le statut coloré, la file, le profil client, l'historique, les documents téléchargeables, la messagerie intégrée et les boutons d'action conditionnés aux habilitations de l'utilisateur connecté.
2. `/dossiers/{DossierId:guid}` (`Dossiers/DetailDossier.razor`) ➜ Afficher les métadonnées du dossier, l'état global dérivé, et la liste cliquable des tickets rattachés avec indicateur d'indépendance de traitement.
3. `/societe/collaborateurs` (ex-`/comptes/collaborateurs`, `Comptes/GestionCollaborateurs.razor`) ➜ Extraire automatiquement `societeId` et `appelantId` depuis l'utilisateur connecté ; afficher la liste des collaborateurs avec actions directes (activer/désactiver, changer visibilité, nommer admin).
4. `/administration/comptes-a-valider` (ex-`/comptes/valider`, `Comptes/ValiderDemandes.razor`) ➜ Déduire l'identité de l'administrateur CIT connecté ; charger automatiquement la liste des demandes en attente avec validation/rejet direct.
5. `/comptes/profil` (`Comptes/ProfilSociete.razor`) ➜ Résoudre la société de l'utilisateur connecté sans saisie de GUID.
6. `/billing-import/soumettre` (`BillingImport/SoumettreDemande.razor`) ➜ Préremplir automatiquement `societeId` et `auteurId` depuis la session active ; conserver la saisie des BLs (1 à 5).
7. `/billing-import/manifeste` (`BillingImport/FilePiloteManifeste.razor`) ➜ Déduire l'agent connecté ; lister les tickets en instance ; actions valider, rejeter avec modale, demander complément.
8. `/billing-import/facturation` (`BillingImport/FileFacturation.razor`) ➜ Déduire l'agent connecté ; téléversement direct de la facture par ligne de ticket.
9. `/billing-import/validation-paiement` (`BillingImport/ValidationPaiement.razor`) ➜ File de validation des preuves avec décision Valider / Refuser (avec motif) pour les agents habilités. La zone de dépôt client est intégrée sur la fiche ticket / dashboard client.
10. `/billing-import/controle-documentaire` (`BillingImport/FileControleDocumentaire.razor`) ➜ Déduire l'agent connecté ; dépôt du BS/BADT déclenchant la clôture.
11. `/billing-import/comptabilite-client` (`BillingImport/FileComptabiliteClient.razor`) ➜ Déduire l'agent connecté ; confirmation du lettrage en un clic.
12. `/billing-import/ies` (`BillingImport/FileIes.razor`) ➜ Déduire l'agent connecté ; progression indépendante des deux branches et constat de jointure.
13. `/supervision` (`Supervision/TableauDeBord.razor`) ➜ Déduire le superviseur connecté ; afficher les files et la charge avec liens de navigation vers les files.

### 4.3 Nouvelles Vues à Créer (Composants Maquants Clés)
1. `/` (`Home.razor`) ➜ **Landing Page / Routeur Intelligent** : redirige immédiatement un utilisateur connecté vers son tableau de bord dédié selon son persona fonctionnel, ou présente une page d'accueil CIT professionnelle avec bouton de connexion.
2. `/client/dashboard` (`Pages/Client/DashboardClient.razor`) ➜ **Tableau de Bord Client** comprenant :
   - Widget contractuel `ACR-01` (« Mes actions requises ») avec badges d'urgence ;
   - Liste des « Demandes récentes » avec statut en temps réel et liens vers les dossiers/tickets ;
   - Bouton d'action rapide « Nouvelle demande Import » ;
   - Raccourcis vers les documents disponibles.
3. `/agent/dashboard` (`Pages/Agent/DashboardAgent.razor`) ➜ **Tableau de Bord Agent CIT** comprenant :
   - Liste des files de traitement attribuées à l'agent avec compteurs de tickets (en attente vs. en cours) ;
   - Sélecteur de disponibilité opérationnelle (Disponible / En pause / Absent — `DIS-01`) ;
   - Accès rapide aux files métier (`Manifeste`, `Facturation`, `Paiement`, `Contrôle Documentaire`, `Comptabilité`, `IES`).
4. `/societe/dashboard` (`Pages/Societe/DashboardSociete.razor`) ➜ **Tableau de Bord Administrateur Société** comprenant :
   - Résumé de l'entreprise (Raison sociale, profil déclaré, statut) ;
   - Compteurs de collaborateurs actifs / administrateurs ;
   - Accès direct à la gestion des collaborateurs et des droits de consultation ;
   - Vue globale des demandes de la société (`CLAR-02`).
5. `/client/demandes` (`Pages/Client/MesDemandes.razor`) ➜ Liste filtrable et paginée de toutes les demandes du client ou de sa société (selon son droit de visibilité `ENT-03`).

---

## 5. Architecture du Shell Applicatif et Design System

### 5.1 Shell Global (`Layout/MainLayout.razor` & `Layout/AppShell.razor`)
Le layout global fournit une structure applicative moderne et réactive :
- **Top Header Bar** :
  - Logo CIT & Titre de la plateforme ;
  - Badge indiquant l'environnement et l'entité de l'utilisateur (ex. *Société TRANS-AFRIQUE | Profil Deposit*) ;
  - Indicateur de statut de disponibilité pour les agents (pastille verte/orange/grise) ;
  - Menu profil utilisateur avec nom affiché, statut 2FA, sélecteur d'espace si multi-habilité, et bouton explicite de **Déconnexion**.
- **Sidebar Desktop (Menu Latéral)** :
  - Navigation contextuelle filtrée en fonction des habilitations et du persona fonctionnel de l'utilisateur (`Client`, `Agent`, `Superviseur`, `Administrateur`) ;
  - Groupes logiques repliables avec icônes distinctes et compteurs de badges (ex. nombre d'actions requises).
- **Navigation Mobile Responsive** :
  - Header compact avec bouton menu burger déclenchant un panneau latéral coulissant (offcanvas) adapté aux smartphones et tablettes (`NFR-02`).
- **Zone de Contenu Principale** :
  - Fil d'Ariane (Breadcrumbs) pour situer la page dans l'arborescence ;
  - Conteneur de notifications et alertes temporaires (succès, erreurs, avertissements).

### 5.2 Design System Léger & Composants Partagés
Sans ajouter de framework lourd, le projet standardise ses composants d'interface dans `Components/Shared/` :
1. `<StatutBadge Statut="@ticket.Statut" />` : Badges visuels harmonisés avec code couleur :
   - 🔵 *Bleu info* : En attente de prise en charge, En attente de validation ;
   - 🟡 *Jaune/Orange attention* : En attente client (complément ou preuve de paiement), En cours de traitement ;
   - 🟢 *Vert succès* : Clôturé, Validé, Payé confirmé, Jointure constatée ;
   - 🔴 *Rouge danger* : Rejet Manifeste, Preuve de paiement refusée, Rejet de compte ;
   - ⚫ *Gris neutre* : Annulé par le client.
2. `<ZoneActionsRequises />` : Composant réutilisable pour afficher les actions bloquantes du client avec lien direct d'action (`ACR-01`).
3. `<EtatVide Message="..." />` : Affichage d'états vides conviviaux avec icône explicative et bouton d'action suggéré.
4. `<IndicateurChargement />` : Spinner / skeleton fluide pendant le chargement des requêtes asynchrones.
5. `<ModaleConfirmation />` : Modale accessible pour confirmer les actions irréversibles (rejet motivé, clôture, annulation).

---

## 6. User Stories et Critères d'Acceptation E2E

### User Story 1 — Shell Applicatif, Authentification & Redirections (Priority: P1)
**En tant qu'** utilisateur de la plateforme (client, collaborateur, agent ou administrateur),  
**Je veux** disposer d'un environnement Web complet avec barre d'en-tête, identité connectée, navigation latérale adaptée à mes droits et bouton de déconnexion,  
**Afin de** naviguer naturellement dans l'application sans jamais avoir à taper d'URL technique.

#### Scénarios d'acceptation E2E :
1. **Given** un visiteur non authentifié accédant à `/` ou à une route protégée, **When** la page est rendue, **Then** il est orienté vers la page de connexion `/comptes/connexion` avec un message d'accueil clair.
2. **Given** un client s'authentifiant avec succès sur `/comptes/connexion`, **When** la connexion est validée, **Then** il est automatiquement redirigé vers `/client/dashboard`.
3. **Given** un agent de traitement s'authentifiant avec succès, **When** la connexion est validée, **Then** il est automatiquement redirigé vers `/agent/dashboard`.
4. **Given** un utilisateur connecté, **When** il consulte le menu de navigation, **Then** seules les entrées correspondant à ses habilitations réelles sont affichées (aucune entrée agent visible pour un client ; aucune entrée de validation de compte visible pour un agent non admin).
5. **Given** un utilisateur connecté, **When** il clique sur "Déconnexion", **Then** sa session est invalidée côté serveur et il est renvoyé sur la page de connexion.

---

### User Story 2 — Dashboard Client & Widget "Mes actions requises" (ACR-01) (Priority: P1)
**En tant que** client d'une société,  
**Je veux** visualiser immédiatement sur mon tableau de bord mes dossiers en cours et les actions précises attendues de ma part,  
**Afin de** débloquer mes dossiers sans retard (fournir un complément ou déposer une preuve de paiement).

#### Scénarios d'acceptation E2E :
1. **Given** un client ayant un ticket à l'état « En attente client » (motif complément manifeste ou nouvelle preuve demandée), **When** il ouvre son dashboard, **Then** le widget « Mes actions requises » affiche ce ticket avec le motif exact et un bouton d'action directe (`ACR-01`).
2. **Given** un client n'ayant aucune action bloquante, **When** il consulte son dashboard, **Then** un message vert « Aucune action requise de votre part » est affiché.
3. **Given** un client sur son dashboard, **When** il consulte ses demandes récentes, **Then** il voit ses numéros de dossier/BL, le statut courant avec code couleur, et peut cliquer pour ouvrir le dossier complet.
4. **Given** un utilisateur société A tentant d'ouvrir manuellement l'URL d'un ticket société B, **When** la requête est traitée, **Then** le serveur refuse l'accès et affiche un écran d'erreur 403 propre sans fuite de données.

---

### User Story 3 — Fiche Ticket & Fiche Dossier Métier Unifiée (Priority: P1)
**En tant qu'** utilisateur habilité (client propriétaire ou agent en charge),  
**Je veux** ouvrir une fiche de ticket complète et centralisée,  
**Afin de** consulter l'ensemble des informations métier (BL, statut, dates, profil), télécharger les documents, échanger sur le fil de discussion et exécuter les actions permises sans ressaisir d'identifiant.

#### Scénarios d'acceptation E2E :
1. **Given** un client ou un agent ouvrant `/tickets/{id}`, **When** la page se charge, **Then** elle affiche le numéro unique du ticket, le numéro de BL associé, le statut courant, le dossier parent, la liste des documents avec bouton de téléchargement, et le fil de commentaires.
2. **Given** un client sur son ticket en attente de paiement (facture disponible), **When** il consulte la fiche, **Then** il dispose d'une zone de dépôt dédiée pour téléverser sa preuve de paiement, sans aucun champ de saisie de GUID.
3. **Given** un client sur son ticket clôturé, **When** il consulte la fiche, **Then** les documents officiels finaux (Facture acquittée, BS, BADT) sont immédiatement téléchargeables.

---

### User Story 4 — Parcours Billing Import 100 % Navigable E2E (Priority: P1)
**En tant que** client et agents des différentes files,  
**Je veux** exécuter l'intégralité du cycle de vie d'une demande Import (soumission multi-BL ➜ instruction manifeste ➜ facturation ➜ paiement ➜ contrôle documentaire) uniquement via la navigation de l'interface,  
**Afin de** valider les 4 scénarios de recette (`REC-SCE-01` à `04`) dans des conditions d'exploitation réelles.

#### Scénarios d'acceptation E2E :
1. **Given** un client connecté, **When** il clique sur « Nouvelle demande IMPORT », **Then** le formulaire s'affiche avec sa société préremplie ; il saisit jusqu'à 5 BLs et valide.
2. **Given** la soumission validée, **When** les tickets sont créés, **Then** le client est automatiquement redirigé vers la vue du dossier créé (`/dossiers/{id}`) montrant la liste des tickets générés.
3. **Given** un Pilote Manifeste ouvrant sa file `/billing-import/manifeste`, **When** il clique sur « Valider » ou « Rejeter », **Then** l'action s'exécute directement pour l'agent connecté sans demander d'identifiant, et la liste se rafraîchit.
4. **Given** un Pilote Facturation ouvrant sa file, **When** il dépose le fichier facture sur une ligne de ticket, **Then** le ticket est automatiquement aiguillé selon le profil client (Normal, En compte, Deposit) et disparaît de sa file.

---

### User Story 5 — Tableaux de Bord Agent & Administrateur Société (Priority: P2)
**En tant qu'** agent de traitement CIT ou administrateur d'une société cliente,  
**Je veux** disposer d'un tableau de bord synthétisant mes responsabilités opérationnelles,  
**Afin de** piloter efficacement mes files de traitement ou mes collaborateurs.

#### Scénarios d'acceptation E2E :
1. **Given** un agent CIT connecté, **When** il consulte son dashboard agent, **Then** il voit la liste de ses files de compétences avec le nombre de tickets en attente, et peut basculer son statut de disponibilité (`Disponible`, `En pause`, `Absent`).
2. **Given** un administrateur de société sur son dashboard entreprise, **When** il consulte la gestion des collaborateurs, **Then** il peut activer/désactiver un collaborateur, modifier sa visibilité (« ses demandes » vs « toute la société »), ou le nommer administrateur en un clic.

---

### User Story 6 — Responsive, Ergonomie Mobile et Design Harmonisé (Priority: P2)
**En tant qu'** utilisateur sur smartphone ou tablette (chauffeur, client nomade, agent de guichet),  
**Je veux** une interface fluide et adaptée à mon écran,  
**Afin de** consulter mes demandes et effectuer mes actions sans distorsion visuelle.

#### Scénarios d'acceptation E2E :
1. **Given** un écran mobile (< 768px), **When** l'utilisateur navigue, **Then** la sidebar est repliée dans un menu offcanvas accessible via le burger button.
2. **Given** un tableau de tickets affiché sur smartphone, **When** la page est rendue, **Then** les colonnes s'adaptent sans casser le layout.

---

## 7. Stratégie de Test et Critères de Validation

La validation de la consolidation UI/UX s'appuiera sur les tests suivants :

### 7.1 Tests Composants & Intégration Web (bUnit & `WebApplicationFactory`)
- **`E2E-01`** : Client : login ➜ dashboard ➜ nouvelle demande IMPORT ➜ dossier ➜ ticket.
- **`E2E-02`** : Client avec action requise : dashboard ➜ « Mes actions requises » ➜ ticket ➜ téléversement pièce ➜ disparition de l'action requise.
- **`E2E-03`** : Agent : login ➜ dashboard ➜ sélection file ➜ fiche ticket ➜ exécution action ➜ retour file rafraîchie.
- **`E2E-04`** : Admin société : login ➜ dashboard société ➜ gestion collaborateurs ➜ modification rôle/droit ➜ retour dashboard.
- **`E2E-05`** : Utilisateur société A tente URL directe ticket société B ➜ refus serveur 403.
- **`E2E-06`** : Utilisateur non habilité tente URL directe d'une file agent ➜ refus serveur 403.

### 7.2 Contrôle de Non-Régression
- La suite existante de **260 tests automatisés** (Domaine, Application, Infrastructure SQL Server, Identity) DOIT continuer de passer à **100 % (260/260 passés, 0 échec, 0 warning)**.

---
*(Fin de la spécification clarifiée — Aucun code généré, en attente de passage à `/speckit.plan`).*
