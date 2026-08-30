# Tasks: Consolidation UI/UX des Features 001 à 003

**Feature Branch**: `003-5-ui-ux-consolidation`  
**Date**: 2026-08-30  
**Status**: Ready — Plan de tâches atomique et séquencé post-`/speckit.tasks`.

---

## 1. Vue d'Ensemble et Baseline de Non-Régression

### Baseline de Référence : **260 tests automatisés existants**
- 37 tests Domaine (`CIT.Plateforme.Domain.Tests`)
- 159 tests Application (`CIT.Plateforme.Application.Tests`)
- 36 tests Infrastructure SQL Server réel (`CIT.Plateforme.Infrastructure.Tests`)
- 28 tests Web (`CIT.Plateforme.Web.Tests`)

> **Règle absolue** : Aucune tâche ne doit modifier les règles métier, workflows ou entités du Domaine F001/F002/F003. Les 260 tests existants doivent demeurer 100 % verts tout au long de l'implémentation.

---

## 2. Inventaire et Découpage des Tâches par Phase

```
Phase 1  : Fondations Applicatives & Contexte Utilisateur de Session (C4-01)
Phase 2  : Design System Léger & Composants Partagés
Phase 3  : Shell Applicatif & Navigation Contextuelle par Persona
Phase 4  : Authentification, Landing Page ("/") & Redirections Intelligentes
Phase 5  : Dashboard Client & Implémentation d'ACR-01 ("Mes actions requises") (C4-02)
Phase 6  : Dashboard Agent CIT & Gestion de Disponibilité (C4-03)
Phase 7  : Dashboard Administrateur Société & Collaborateurs (C4-04)
Phase 8  : Fiche Ticket Métier Unifiée & Actions Contextuelles (C4-05)
Phase 9  : Fiche Dossier Multi-BL & Indépendance des Tickets (C4-06)
Phase 10 : Refactoring des Pages Billing Import & Éradication Complète des GUIDs
Phase 11 : Parcours E2E, Durcissement de Sécurité & Non-Régression Finale
```

---

## Phase 1 : Fondations Applicatives & Contexte Utilisateur de Session (Contrat C4-01)

- [ ] **T001** [P] Créer les DTOs et ViewModels de session `ContexteUtilisateurVue` et `EspaceFonctionnel` dans `src/CIT.Plateforme.Application/Web/ContexteUtilisateurVue.cs` conformément à `data-model.md §2.1`.
- [ ] **T002** [P] Écrire les tests unitaires TDD pour `ObtenirContexteUtilisateurQuery` dans `tests/CIT.Plateforme.Application.Tests/Web/ObtenirContexteUtilisateurQueryTests.cs` (cas client standard, client admin société, agent simple, agent superviseur, utilisateur non rattaché, compte désactivé).
- [ ] **T003** Implémenter la query `ObtenirContexteUtilisateurQuery` dans `src/CIT.Plateforme.Application/Web/ObtenirContexteUtilisateurQuery.cs` (Contrat `C4-01` : extraction `UtilisateurId`, résolution `UtilisateurSociete`, chargement permissions via `IAutorisationService`, vérification statut actif).
- [ ] **T004** Créer l'interface et le service `IContexteUtilisateurWeb` dans `src/CIT.Plateforme.Web/Services/ContexteUtilisateurWebService.cs` s'appuyant sur `AuthenticationStateProvider` et `ObtenirContexteUtilisateurQuery`, avec gestion de l'espace actif courant et commutation d'espace.
- [ ] **T005** Enregistrer `IContexteUtilisateurWeb` et les nouvelles queries dans `src/CIT.Plateforme.Web/Program.cs` et `src/CIT.Plateforme.Application/ApplicationServiceCollectionExtensions.cs`.
- [ ] **T006** Écrire un test d'intégration pour `IContexteUtilisateurWeb` dans `tests/CIT.Plateforme.Web.Tests/Services/ContexteUtilisateurWebServiceTests.cs` vérifiant la résolution de session depuis un cookie d'authentification simulé.

---

## Phase 2 : Design System Léger & Composants Partagés

- [ ] **T007** [P] Définir les variables de design tokens CIT (couleurs de statuts, espacements, typographie, badges, alertes) dans `src/CIT.Plateforme.Web/wwwroot/app.css`.
- [ ] **T008** [P] Écrire les tests bUnit pour `<StatutBadge />` dans `tests/CIT.Plateforme.Web.Tests/Components/Shared/StatutBadgeTests.cs` validant le rendu visuel et le code couleur des 6 grands statuts (`EnAttente`, `EnCours`, `AttenteClient`, `Cloture`, `RejetManifeste`, `Annule`).
- [ ] **T009** Implémenter le composant `src/CIT.Plateforme.Web/Components/Shared/StatutBadge.razor` affichant les badges harmonisés avec icônes et classes CSS dédiées.
- [ ] **T010** [P] Implémenter le composant `src/CIT.Plateforme.Web/Components/Shared/EtatVide.razor` (état de liste vide avec titre, message explicatif, icône et bouton d'action contextuel).
- [ ] **T011** [P] Implémenter le composant `src/CIT.Plateforme.Web/Components/Shared/IndicateurChargement.razor` (skeleton loader / spinner asynchrone).
- [ ] **T012** [P] Implémenter le composant `src/CIT.Plateforme.Web/Components/Shared/ModaleConfirmation.razor` (dialogue modal accessible pour confirmer rejets, clôtures et actions engageantes).
- [ ] **T013** [P] Implémenter le composant `src/CIT.Plateforme.Web/Components/Shared/PermissionDenied.razor` (écran d'erreur 403 standardisé avec message sobre sans fuite de métadonnées).
- [ ] **T014** [P] Implémenter le composant `src/CIT.Plateforme.Web/Components/Shared/ToastNotification.razor` (feedback visuel temporaire pour confirmations de succès et erreurs).

---

## Phase 3 : Shell Applicatif & Navigation Contextuelle par Persona

- [ ] **T015** Écrire les tests bUnit pour la navigation contextuelle dans `tests/CIT.Plateforme.Web.Tests/Components/Layout/NavMenuTests.cs` (vérification que la sidebar n'affiche que les liens autorisés selon le persona actif).
- [ ] **T016** Implémenter le composant `src/CIT.Plateforme.Web/Components/Layout/PersonaSwitcher.razor` (menu déroulant permettant aux utilisateurs multi-habilités de basculer d'espace fonctionnel).
- [ ] **T017** Implémenter le composant `src/CIT.Plateforme.Web/Components/Layout/HeaderBar.razor` (Logo CIT, badge société/environnement, sélecteur de disponibilité agent `DIS-01`, sélecteur d'espace, menu profil et bouton Déconnexion).
- [ ] **T018** Implémenter la barre latérale `src/CIT.Plateforme.Web/Components/Layout/NavMenu.razor` avec regroupements repliables, compteurs de badges et filtrage dynamique par habilitations (`CLAR-UX-03`).
- [ ] **T019** Refactorer `src/CIT.Plateforme.Web/Components/Layout/MainLayout.razor` pour assembler HeaderBar, NavMenu (desktop + mobile offcanvas `NFR-02`), conteneur principal avec fil d'Ariane, et conteneur de toast.
- [ ] **T020** Écrire un test Web dans `tests/CIT.Plateforme.Web.Tests/Components/Layout/MainLayoutIntegrationTests.cs` validant le rendu complet du shell sous `WebApplicationFactory`.

---

## Phase 4 : Authentification, Landing Page ("/") & Redirections Intelligentes

- [ ] **T021** Écrire les tests d'intégration dans `tests/CIT.Plateforme.Web.Tests/Pages/AuthRedirectionTests.cs` vérifiant :
  - Accès visiteur sur `/` ➜ Affiche la page d'accueil d'accès ;
  - Accès connecté Client sur `/` ➜ Redirection `/client/dashboard` ;
  - Accès connecté Agent sur `/` ➜ Redirection `/agent/dashboard` ;
  - Accès connecté Superviseur sur `/` ➜ Redirection `/supervision` ;
  - Accès connecté Admin Société sur `/` ➜ Redirection `/societe/dashboard` ;
  - Redirection post-connexion dans `Connexion.razor` et `VerifierCode.razor` vers `/`.
- [ ] **T022** Modifier `src/CIT.Plateforme.Web/Components/Pages/Comptes/Connexion.razor` pour ajouter la redirection automatique vers `/` après succès de mot de passe (si 2FA inactif).
- [ ] **T023** Modifier `src/CIT.Plateforme.Web/Components/Pages/Comptes/VerifierCode.razor` pour ajouter la redirection automatique vers `/` après succès du défi TOTP.
- [ ] **T024** Remplacer le template "Hello, world!" dans `src/CIT.Plateforme.Web/Components/Pages/Home.razor` par le composant de routage intelligent et d'accueil CIT.
- [ ] **T025** Créer le point de terminaison de déconnexion sécurisé `/comptes/deconnexion` dans `src/CIT.Plateforme.Web/Program.cs` invalidant le cookie d'authentification et redirigeant vers `/comptes/connexion`.

---

## Phase 5 : Dashboard Client & Implémentation d'ACR-01 ("Mes actions requises") (Contrat C4-02)

- [ ] **T026** [P] Créer les DTOs `DashboardClientVue`, `ActionRequiseItemVue`, `DossierResumeVue`, `TicketResumeVue` dans `src/CIT.Plateforme.Application/Client/DashboardClientVue.cs` conformément à `data-model.md §3.1`.
- [ ] **T027** Écrire les tests unitaires TDD stricts pour `ObtenirDashboardClientQuery` dans `tests/CIT.Plateforme.Application.Tests/Client/ObtenirDashboardClientQueryTests.cs` couvrant :
  - **ACR-01 Test 1** : Ticket en attente complément Manifeste ➜ Action `ComplementManifeste` présente ;
  - **ACR-01 Test 2** : Ticket en attente paiement (client Normal) ➜ Action `PaiementAttendu` présente ;
  - **ACR-01 Test 3** : Ticket avec preuve paiement refusée ➜ Action `NouvellePreuveRequise` présente ;
  - **ACR-01 Test 4** : Ticket sans action bloquante ➜ Liste des actions requises vide ;
  - **Scope Test** : Filtrage `PropresDemandesUniquement` vs. `ToutesDemandesSociete` (`ENT-03` / `CLAR-UX-04`) ;
  - **Sécurité Test** : Tentative d'accès à une société tierce ➜ `AccesRefuseException`.
- [ ] **T028** Implémenter `ObtenirDashboardClientQuery` dans `src/CIT.Plateforme.Application/Client/ObtenirDashboardClientQuery.cs` (Contrat `C4-02`, projection LINQ optimisée sans table dédiée).
- [ ] **T029** Implémenter le composant partagé `src/CIT.Plateforme.Web/Components/Shared/ZoneActionsRequises.razor` affichant le widget `ACR-01` avec badges d'urgence, libellé explicite et boutons d'action rapide vers le ticket.
- [ ] **T030** Créer la page `src/CIT.Plateforme.Web/Components/Pages/Client/DashboardClient.razor` (Route `/client/dashboard` : en-tête profil société, widget `<ZoneActionsRequises />`, compteurs d'activité, tableau des demandes récentes avec badges de statut, bouton « Nouvelle demande Import »).
- [ ] **T031** Créer la page `src/CIT.Plateforme.Web/Components/Pages/Client/MesDemandes.razor` (Route `/client/demandes` : liste complète des dossiers de l'entreprise avec filtre interactif propre/société pour les administrateurs, recherche par BL et pagination).
- [ ] **T032** Écrire les tests bUnit et Web pour `DashboardClient.razor` dans `tests/CIT.Plateforme.Web.Tests/Pages/Client/DashboardClientTests.cs` (vérifiant l'affichage dynamique d'ACR-01 et l'absence de GUIDs manuels).

---

## Phase 6 : Dashboard Agent CIT & Gestion de Disponibilité (Contrat C4-03)

- [ ] **T033** [P] Créer les DTOs `DashboardAgentVue`, `FileAttributionVue`, `TicketPrisEnChargeVue` dans `src/CIT.Plateforme.Application/Agent/DashboardAgentVue.cs` conformément à `data-model.md §3.2`.
- [ ] **T034** Écrire les tests unitaires TDD pour `ObtenirDashboardAgentQuery` dans `tests/CIT.Plateforme.Application.Tests/Agent/ObtenirDashboardAgentQueryTests.cs` (résolution des files habilitées, calcul des compteurs attente/en cours, tickets pris en charge nominativement, statut disponibilité `DIS-01`).
- [ ] **T035** Implémenter `ObtenirDashboardAgentQuery` dans `src/CIT.Plateforme.Application/Agent/ObtenirDashboardAgentQuery.cs` (Contrat `C4-03`).
- [ ] **T036** Créer la page `src/CIT.Plateforme.Web/Components/Pages/Agent/DashboardAgent.razor` (Route `/agent/dashboard` : sélecteur de disponibilité opérationnelle, cartes des files de compétences avec compteurs et liens directs, liste des tickets pris en charge par l'agent connecté avec indicateur de temps).
- [ ] **T037** Écrire les tests Web pour `DashboardAgent.razor` dans `tests/CIT.Plateforme.Web.Tests/Pages/Agent/DashboardAgentTests.cs`.

---

## Phase 7 : Dashboard Administrateur Société & Collaborateurs (Contrat C4-04)

- [ ] **T038** [P] Créer les DTOs `DashboardSocieteVue` et `CollaborateurApercuVue` dans `src/CIT.Plateforme.Application/Societe/DashboardSocieteVue.cs` conformément à `data-model.md §3.3`.
- [ ] **T039** Écrire les tests unitaires TDD pour `ObtenirDashboardSocieteQuery` dans `tests/CIT.Plateforme.Application.Tests/Societe/ObtenirDashboardSocieteQueryTests.cs` (synthèse collaborateurs, statistiques, vérification du rôle administrateur de société `CLAR-02`).
- [ ] **T040** Implémenter `ObtenirDashboardSocieteQuery` dans `src/CIT.Plateforme.Application/Societe/ObtenirDashboardSocieteQuery.cs` (Contrat `C4-04`).
- [ ] **T041** Créer la page `src/CIT.Plateforme.Web/Components/Pages/Societe/DashboardSociete.razor` (Route `/societe/dashboard` : profil société, compteurs collaborateurs actifs/inactifs/admins, aperçu des dernières demandes entreprise, bouton d'accès à la gestion des collaborateurs).
- [ ] **T042** Refactorer `src/CIT.Plateforme.Web/Components/Pages/Comptes/GestionCollaborateurs.razor` (Route `/societe/collaborateurs`, suppression complète des champs de saisie `appelantId` et `societeId` remplacés par le contexte connecté).
- [ ] **T043** Écrire les tests Web pour le tableau de bord et la gestion des collaborateurs dans `tests/CIT.Plateforme.Web.Tests/Pages/Societe/DashboardSocieteTests.cs`.

---

## Phase 8 : Fiche Ticket Métier Unifiée & Actions Contextuelles (Contrat C4-05)

- [ ] **T044** [P] Créer les DTOs `TicketDetailVue`, `DocumentItemVue`, `MessageItemVue`, `EvenementHistoriqueVue`, `ActionsPermisesTicketVue` dans `src/CIT.Plateforme.Application/Tickets/TicketDetailVue.cs` conformément à `data-model.md §4.1`.
- [ ] **T045** Écrire les tests unitaires TDD pour `ObtenirDetailTicketQuery` dans `tests/CIT.Plateforme.Application.Tests/Tickets/ObtenirDetailTicketQueryTests.cs` couvrant :
  - Restitution complète des métadonnées (numéro, BL, statut, snapshot `TypeClient`) ;
  - **Cloisonnement messages** : Client ne voit pas les notes internes `MSG-02` ; Agent voit messages publics + internes ;
  - **Actions permises** : Calcul rigoureux des booléens selon le statut et les habilitations ;
  - **Sécurité 403** : Tentative d'accès par utilisateur non propriétaire / non habilité ➜ `AccesRefuseException`.
- [ ] **T046** Implémenter `ObtenirDetailTicketQuery` dans `src/CIT.Plateforme.Application/Tickets/ObtenirDetailTicketQuery.cs` (Contrat `C4-05`).
- [ ] **T047** Refactorer intégralement `src/CIT.Plateforme.Web/Components/Pages/Tickets/DetailTicket.razor` (Route `/tickets/{TicketId:guid}`) :
  - Affichage de l'en-tête métier avec `<StatutBadge />` et snapshot `TypeClient` ;
  - Section Documents avec tableau des pièces et liens de téléchargement direct ;
  - Section Fil de discussion avec distinction visuelle messages publics / notes internes (`MSG-03`) ;
  - Section Historique des événements du cycle de vie ;
  - Zone d'action contextuelle (zone de dépôt preuve paiement pour client si statut `EnAttentePaiement`, zone de réponse complément si `EnAttenteClient`, boutons d'instruction pour agents) **sans aucun champ de saisie de GUID**.
- [ ] **T048** Écrire les tests Web pour `DetailTicket.razor` dans `tests/CIT.Plateforme.Web.Tests/Pages/Tickets/DetailTicketTests.cs` (vérifiant l'affichage complet des données et le masquage des boutons non autorisés).

---

## Phase 9 : Fiche Dossier Multi-BL & Indépendance des Tickets (Contrat C4-06)

- [ ] **T049** [P] Créer les DTOs `DossierDetailVue` et `TicketDetailItemDansDossierVue` dans `src/CIT.Plateforme.Application/Dossiers/DossierDetailVue.cs` conformément à `data-model.md §4.2`.
- [ ] **T050** Écrire les tests unitaires TDD pour `ObtenirDetailDossierQuery` dans `tests/CIT.Plateforme.Application.Tests/Dossiers/ObtenirDetailDossierQueryTests.cs` (calcul de l'état dérivé, projection des tickets/BLs rattachés, contrôle de cloisonnement).
- [ ] **T051** Implémenter `ObtenirDetailDossierQuery` dans `src/CIT.Plateforme.Application/Dossiers/ObtenirDetailDossierQuery.cs` (Contrat `C4-06`).
- [ ] **T052** Refactorer `src/CIT.Plateforme.Web/Components/Pages/Dossiers/DetailDossier.razor` (Route `/dossiers/{DossierId:guid}`) : affichage des métadonnées du dossier, état global dérivé, et tableau des tickets/BLs démontrant l'indépendance de traitement (`IMP-02`) avec liens cliquables vers chaque fiche ticket.
- [ ] **T053** Écrire les tests Web pour `DetailDossier.razor` dans `tests/CIT.Plateforme.Web.Tests/Pages/Dossiers/DetailDossierTests.cs`.

---

## Phase 10 : Refactoring des Pages Billing Import & Éradication Complète des GUIDs

- [ ] **T054** Refactorer `src/CIT.Plateforme.Web/Components/Pages/BillingImport/SoumettreDemande.razor` :
  - Supprimer les champs de saisie manuelle `societe` et `auteur` ;
  - Préremplir automatiquement depuis `IContexteUtilisateurWeb` ;
  - Après soumission réussie des 1 à 5 BLs, rediriger automatiquement vers `/dossiers/{DossierId}`.
- [ ] **T055** Refactorer `src/CIT.Plateforme.Web/Components/Pages/BillingImport/FilePiloteManifeste.razor` :
  - Supprimer le champ `agentId` (déduit de session) ;
  - Charger automatiquement les tickets en instance au chargement de la page ;
  - Remplacer le mini input rejet par une modale de motif obligatoire (`<ModaleConfirmation />`) ;
  - Après action, rafraîchir la file sur place avec toast de succès.
- [ ] **T056** Refactorer `src/CIT.Plateforme.Web/Components/Pages/BillingImport/FileFacturation.razor` :
  - Supprimer le champ `agentId` (déduit de session) ;
  - Téléversement direct du fichier facture par ligne de ticket, déclenchant l'aiguillage automatique selon `TypeClient` ;
  - Rafraîchissement automatique de la file.
- [ ] **T057** Refactorer `src/CIT.Plateforme.Web/Components/Pages/BillingImport/ValidationPaiement.razor` :
  - Supprimer les champs `agentId`, `clientId`, `ticketIdClient` ;
  - Consacrer cette page exclusivement à la file de validation Caisse/Finance pour les agents habilités (Valider / Refuser avec modale) ;
  - Le dépôt côté client est intégré directement sur la fiche ticket (`/tickets/{id}`).
- [ ] **T058** Refactorer `src/CIT.Plateforme.Web/Components/Pages/BillingImport/FileControleDocumentaire.razor` :
  - Supprimer le champ `agentId` (déduit de session) ;
  - Dépôt de la pièce de sortie (BS/BADT) clôturant le ticket avec feedback immédiat.
- [ ] **T059** Refactorer `src/CIT.Plateforme.Web/Components/Pages/BillingImport/FileComptabiliteClient.razor` :
  - Supprimer le champ `agentId` (déduit de session) ;
  - Confirmation du lettrage IRIS en un clic sans saisie manuelle.
- [ ] **T060** Refactorer `src/CIT.Plateforme.Web/Components/Pages/BillingImport/FileIes.razor` :
  - Supprimer le champ `agentId` (déduit de session) ;
  - Visualisation des deux branches (Manifeste / Contrôle Documentaire) et actions d'achèvement/jointure contextualisées.
- [ ] **T061** Refactorer les pages d'administration existantes :
  - `Comptes/ValiderDemandes.razor` ➜ Supprimer `InputText appelant` ;
  - `Comptes/ProfilSociete.razor` ➜ Supprimer `InputText appelant`, `InputText societe` ;
  - `Comptes/ModifierTypeClient.razor` ➜ Supprimer `InputText appelant` ;
  - `Supervision/TableauDeBord.razor` ➜ Supprimer `input superviseurId` ;
  - `Administration/Audit.razor` ➜ Supprimer `input administrateurId`.
- [ ] **T062** Écrire un test d'analyse statique dans `tests/CIT.Plateforme.Web.Tests/Pages/EradicationGuidsManuelsTests.cs` scannant les composants Razor pour prouver mathématiquement l'absence de tout champ de saisie d'identifiant technique GUID dans les formulaires.

---

## Phase 11 : Parcours E2E, Durcissement de Sécurité & Non-Régression Finale

- [ ] **T063** Implémenter le test de parcours E2E complet `E2E-01` dans `tests/CIT.Plateforme.Web.Tests/E2E/ParcoursE2E01ClientNominalTests.cs` (Login client ➜ Dashboard ➜ Soumission multi-BL ➜ Vue Dossier ➜ Consultation Fiche Ticket).
- [ ] **T064** Implémenter le test de parcours E2E `E2E-02` dans `tests/CIT.Plateforme.Web.Tests/E2E/ParcoursE2E02Acr01DeblocageTests.cs` (Ticket en attente complément ➜ Détection widget `ACR-01` ➜ Dépôt pièce sur ticket ➜ Disparition du widget au retour dashboard).
- [ ] **T065** Implémenter le test de parcours E2E `E2E-03` dans `tests/CIT.Plateforme.Web.Tests/E2E/ParcoursE2E03AgentInstructionTests.cs` (Login Pilote Manifeste ➜ Dashboard agent ➜ Sélection file ➜ Fiche ticket ➜ Validation ➜ Retour file rafraîchie).
- [ ] **T066** Implémenter le test de parcours E2E `E2E-04` dans `tests/CIT.Plateforme.Web.Tests/E2E/ParcoursE2E04AdminSocieteCollaborateursTests.cs` (Login Admin Société ➜ Dashboard société ➜ Gestion collaborateurs ➜ Modification visibilité ➜ Retour dashboard).
- [ ] **T067** Implémenter le test de sécurité E2E `E2E-05` dans `tests/CIT.Plateforme.Web.Tests/E2E/SecuriteE2E05Cloisonnement403Tests.cs` (Utilisateur Société A tentant d'ouvrir `/tickets/{ticketSocieteB}` ➜ Assertion HTTP/Blazor du refus 403 propre).
- [ ] **T068** Implémenter le test de sécurité E2E `E2E-06` dans `tests/CIT.Plateforme.Web.Tests/E2E/SecuriteE2E06HabilitationFile403Tests.cs` (Utilisateur non habilité tentant d'ouvrir `/billing-import/facturation` ➜ Assertion du refus 403 propre).
- [ ] **T069** Exécuter la suite complète de tests de non-régression et vérifier l'atteinte des 260 tests de base + tous les nouveaux tests de présentation et E2E (0 échec, 0 warning).
- [ ] **T070** Mettre à jour `specs/003-5-ui-ux-consolidation/checklists/requirements.md` pour valider l'ensemble des critères d'acceptation et de conformité constitutionnelle.

---

## 3. Matrice de Traçabilité des Contrats et des Exigences

| Contrat / Exigence | Tâche(s) d'Implémentation | Tâche(s) de Test | Statut |
|---|---|---|:---:|
| **`C4-01` (Contexte Session)** | `T001`, `T003`, `T004`, `T005` | `T002`, `T006` | Planifié |
| **`C4-02` (Dashboard Client & ACR-01)** | `T026`, `T028`, `T029`, `T030`, `T031` | `T027`, `T032`, `T064` | Planifié |
| **`C4-03` (Dashboard Agent & Files)** | `T033`, `T035`, `T036` | `T034`, `T037`, `T065` | Planifié |
| **`C4-04` (Dashboard Société & Collaborateurs)** | `T038`, `T040`, `T041`, `T042` | `T039`, `T043`, `T066` | Planifié |
| **`C4-05` (Fiche Ticket Détaillée)** | `T044`, `T046`, `T047` | `T045`, `T048`, `T067` | Planifié |
| **`C4-06` (Fiche Dossier Multi-BL)** | `T049`, `T051`, `T052` | `T050`, `T053`, `T063` | Planifié |
| **`ACR-01` (Mes actions requises)** | `T028`, `T029`, `T030` | `T027`, `T032`, `T064` | Planifié |
| **`E2E-01` (Client Nominal)** | `T054`, `T047`, `T052` | `T063` | Planifié |
| **`E2E-02` (Déblocage ACR-01)** | `T029`, `T047` | `T064` | Planifié |
| **`E2E-03` (Agent Manifeste)** | `T036`, `T055` | `T065` | Planifié |
| **`E2E-04` (Admin Société)** | `T041`, `T042` | `T066` | Planifié |
| **`E2E-05` (Cloisonnement 403)** | `T013`, `T046` | `T067` | Planifié |
| **`E2E-06` (Habilitation File 403)** | `T013`, `T056` | `T068` | Planifié |
| **Éradication GUIDs Manuels** | `T054` à `T061` | `T062` | Planifié |

---

## 4. Bilan et Statistiques du Plan de Tâches

- **Nombre total de tâches** : **70 tâches atomiques** (`T001` à `T070`).
- **Tâches parallélisables `[P]`** : **14 tâches** (DTOs, tokens CSS, composants partagés isolés).
- **Tâches bloquées / ambiguïtés restantes** : **0**.
- **Contrats couverts** : **6 / 6 (100 %)** (`C4-01` à `C4-06`).
- **Scénarios E2E couverts** : **6 / 6 (100 %)** (`E2E-01` à `E2E-06`).
- **Exigence `ACR-01`** : Chaîne complète couverte (DTO ➜ Query ➜ Test TDD ➜ Composant Razor ➜ Dashboard ➜ Test E2E).
- **Baseline de non-régression** : 260 tests de base préservés.

---
*(Fin de tasks.md — Prêt pour `/speckit.implement`).*
