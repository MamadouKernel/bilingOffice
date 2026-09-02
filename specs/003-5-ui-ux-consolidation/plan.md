# Implementation Plan: Consolidation UI/UX des Features 001 à 003

**Feature Branch**: `003-5-ui-ux-consolidation`  
**Date**: 2026-08-30  
**Phase**: `/speckit.plan`

---

## 1. Résumé Exécutif et Alignement Projet

Ce plan d'implémentation détaille la conception technique pour transformer l'application Web Blazor (`CIT.Plateforme.Web`) en une interface d'entreprise navigable, fluide et directement utilisable par les différents profils d'utilisateurs (Clients, Administrateurs Société, Agents de file CIT, Superviseurs, Administrateurs plateforme).

### Objectifs Principaux
1. **Shell Applicatif & Navigation Contextuelle** : Mise en place d'un layout unifié avec en-tête complet (profil, déconnexion, sélecteur d'espace) et barre latérale dynamique filtrée par persona.
2. **Éradication des GUIDs Manuels** : Remplacement systématique de tous les champs de saisie manuelle de GUIDs par la résolution automatique via `IContexteUtilisateurWeb`.
3. **Implémentation Formelle d'ACR-01 ("Mes actions requises")** : Dérivation dynamique et projection des actions bloquantes du client avec navigation directe.
4. **Création des 3 Grands Tableaux de Bord** : Dashboard Client (`/client/dashboard`), Dashboard Agent (`/agent/dashboard`) et Dashboard Admin Société (`/societe/dashboard`).
5. **Fiche Ticket & Dossier Métier Enrichie** : Restitution complète des métadonnées, pièces téléchargeables, messagerie et actions autorisées côté serveur.
6. **Préservation du Baseline de Non-Régression** : Préservation intégrale des **260 tests automatisés existants** et ajout de tests E2E / bUnit dédiés.

---

## 2. Contraintes Techniques et Choix d'Architecture

- **Stack Technique** : .NET 10, C# 13, Blazor Server (`InteractiveServer`), ASP.NET Core Identity natif, SignalR (SupervisionHub existant), Bootstrap 5 + Variables CSS personnalisées (`app.css`), bUnit, `WebApplicationFactory`.
- **Zéro Nouveau Framework Externe** : Aucune dépendance lourde tierce (pas de Node.js, pas de React/Vue, pas de framework CSS externe non justifié).
- **Architecture Applicative (CQRS Read-Models)** :
  - Le Domaine reste pur et strictement inchangé (0 nouvelle entité, 0 modification de règle métier).
  - Les besoins de données pour l'UI sont satisfaits par de nouvelles **Queries Applicatives et Read Models dédiés** dans `CIT.Plateforme.Application`.

---

## 3. Plan Détaillé des Composants et Fichiers

### 3.1 Couche Application (Nouvelles Queries et Read Models)

| Composant | Fichier | Description |
|---|---|---|
| `IContexteUtilisateurWeb` | `Application/Web/IContexteUtilisateurWeb.cs` | Contrat de résolution de session pour Blazor |
| `ContexteUtilisateurVue` | `Application/Web/ContexteUtilisateurVue.cs` | DTO contenant l'identité, société, persona et habilitations |
| `ObtenirContexteUtilisateurQuery` | `Application/Web/ObtenirContexteUtilisateurQuery.cs` | Query extrayant le contexte connecté complet |
| `DashboardClientVue` | `Application/Client/DashboardClientVue.cs` | DTO du tableau de bord client avec widget ACR-01 |
| `ObtenirDashboardClientQuery` | `Application/Client/ObtenirDashboardClientQuery.cs` | Query calculant ACR-01 et les demandes récentes |
| `DashboardAgentVue` | `Application/Agent/DashboardAgentVue.cs` | DTO des files, compteurs et disponibilité de l'agent |
| `ObtenirDashboardAgentQuery` | `Application/Agent/ObtenirDashboardAgentQuery.cs` | Query de pilotage des files attribuées |
| `DashboardSocieteVue` | `Application/Societe/DashboardSocieteVue.cs` | DTO de l'espace entreprise et des collaborateurs |
| `ObtenirDashboardSocieteQuery` | `Application/Societe/ObtenirDashboardSocieteQuery.cs` | Query de synthèse pour l'administrateur société |
| `TicketDetailVue` | `Application/Tickets/TicketDetailVue.cs` | DTO enrichi pour la fiche détaillée du ticket |
| `ObtenirDetailTicketQuery` | `Application/Tickets/ObtenirDetailTicketQuery.cs` | Query assemblant données, documents, chat et actions permises |
| `DossierDetailVue` | `Application/Dossiers/DossierDetailVue.cs` | DTO de la fiche dossier multi-BL |
| `ObtenirDetailDossierQuery` | `Application/Dossiers/ObtenirDetailDossierQuery.cs` | Query de restitution du dossier et de ses tickets |

---

### 3.2 Couche Présentation Web (Composants Razor & Layout)

#### A. Layout & Navigation
- `Components/Layout/MainLayout.razor` ➜ Intègre la Topbar (Header), la Sidebar (`NavMenu.razor`), le conteneur toast de notifications et le responsive mobile offcanvas.
- `Components/Layout/NavMenu.razor` ➜ Navigation latérale contextuelle filtrée selon l'espace actif et les habilitations réelles.
- `Components/Layout/HeaderBar.razor` ➜ Barre supérieure avec logo CIT, identité connectée, sélecteur d'espace (`PersonaSwitcher.razor`), statut disponibilité agent et bouton déconnexion.
- `Components/Layout/PersonaSwitcher.razor` ➜ Sélecteur déroulant permettant aux utilisateurs multi-habilités de basculer d'un espace à l'autre.

#### B. Nouveaux Tableaux de Bord & Vues Principales
- `Components/Pages/Home.razor` ➜ Routeur intelligent orientant vers le bon dashboard ou page d'accueil d'accès.
- `Components/Pages/Client/DashboardClient.razor` ➜ Tableau de bord client avec widget `<ZoneActionsRequises />` (`ACR-01`) et demandes récentes.
- `Components/Pages/Client/MesDemandes.razor` ➜ Liste complète des dossiers/tickets avec filtres et pagination.
- `Components/Pages/Agent/DashboardAgent.razor` ➜ Tableau de bord agent avec sélecteur de disponibilité et accès aux files attribuées.
- `Components/Pages/Societe/DashboardSociete.razor` ➜ Tableau de bord entreprise avec synthèse collaborateurs.
- `Components/Pages/Tickets/DetailTicket.razor` ➜ Fiche métier unifiée (métadonnées, documents téléchargeables, messages, actions autorisées).
- `Components/Pages/Dossiers/DetailDossier.razor` ➜ Fiche dossier multi-BL démontrant l'indépendance de chaque BL.

#### C. Refactoring des Pages Existantes (Suppression des GUIDs)
- `BillingImport/SoumettreDemande.razor` ➜ Préremplissage automatique société/auteur depuis la session.
- `BillingImport/FilePiloteManifeste.razor` ➜ Résolution automatique de l'agent connecté ; modale pour motif de rejet.
- `BillingImport/FileFacturation.razor` ➜ Résolution automatique de l'agent connecté ; téléversement direct de facture.
- `BillingImport/ValidationPaiement.razor` ➜ File agent de validation paiement sans saisie de GUID.
- `BillingImport/FileControleDocumentaire.razor` ➜ Résolution de l'agent connecté ; dépôt BS/BADT.
- `BillingImport/FileComptabiliteClient.razor` ➜ Résolution de l'agent connecté ; confirmation de lettrage.
- `BillingImport/FileIes.razor` ➜ Résolution de l'agent connecté ; achèvement des branches et jointure.
- `Comptes/ValiderDemandes.razor` ➜ Déduction automatique de l'administrateur CIT.
- `Comptes/GestionCollaborateurs.razor` ➜ Déduction automatique de l'administrateur d'entreprise et de sa société.
- `Comptes/ProfilSociete.razor` ➜ Déduction de la société de l'utilisateur connecté.
- `Supervision/TableauDeBord.razor` ➜ Déduction automatique du superviseur connecté.

#### D. Composants Partagés (`Components/Shared/`)
- `StatutBadge.razor` ➜ Badges visuels harmonisés pour tous les statuts (`EnAttente`, `EnCours`, `Cloture`, `Rejete`, `Annule`).
- `ZoneActionsRequises.razor` ➜ Composant du widget `ACR-01` avec boutons d'action rapide.
- `EtatVide.razor` ➜ Affichage convivial pour listes vides.
- `IndicateurChargement.razor` ➜ Spinner / skeleton de chargement asynchrone.
- `ModaleConfirmation.razor` ➜ Modale accessible pour actions engageantes.
- `PermissionDenied.razor` ➜ Composant d'erreur 403 propre.

---

## 4. Implémentation Détaillée de l'Exigence `ACR-01` ("Mes actions requises")

L'exigence `ACR-01` (Must, CDC Section 17.1) est implémentée par le flux suivant :
1. Lors du chargement de `/client/dashboard`, la query `ObtenirDashboardClientQuery` analyse les tickets actifs de la société de l'utilisateur.
2. Pour chaque ticket nécessitant une intervention client :
   - *Statut `EnAttenteClient` (motif attente complément)* ➜ Génère un item avec bouton « Déposer les pièces complémentaires ».
   - *Statut `EnAttentePaiement` (client Normal)* ➜ Génère un item avec bouton « Transmettre la preuve de paiement ».
   - *Statut `EnAttenteClient` (motif refus preuve)* ➜ Génère un item avec bouton « Déposer une nouvelle preuve de paiement ».
3. L'utilisateur clique sur le bouton de l'action ➜ Il est dirigé précisément vers la fiche du ticket concerné avec la zone de téléversement appropriée active.
4. Dès que l'action est complétée, le statut du ticket est mis à jour côté serveur et l'action disparaît immédiatement du widget `ACR-01`.

---

## 5. Stratégie de Test et Validation E2E

### 5.1 Suite de Tests E2E Automatisés (`ParcoursUiE2ETests.cs`)
- **`E2E-01` (Parcours Nominal Import Client)** :
  1. Authentification client normal via `Connexion.razor`.
  2. Redirection automatique vers `/client/dashboard`.
  3. Clic sur « Nouvelle demande IMPORT ».
  4. Saisie de 2 BLs sur `/billing-import/soumettre` sans saisie de GUID.
  5. Redirection automatique vers `/dossiers/{id}` et consultation des tickets créés.
- **`E2E-02` (Parcours Déblocage `ACR-01` Client)** :
  1. Client avec ticket en attente de complément.
  2. Chargement de `/client/dashboard` et assertion de la présence du widget « Mes actions requises » avec badge d'urgence.
  3. Clic sur l'action ➜ ouverture de la fiche ticket ➜ dépôt du complément.
  4. Retour dashboard ➜ assertion de la disparition de l'action requise.
- **`E2E-03` (Parcours Agent Manifeste & Facturation)** :
  1. Authentification agent Pilote Manifeste ➜ Redirection `/agent/dashboard`.
  2. Clic sur la file Manifeste ➜ Validation d'un BL sans saisie d'identifiant d'agent.
  3. Authentification Pilote Facturation ➜ Dépôt de la facture ➜ Vérification de l'aiguillage automatique.
- **`E2E-04` (Parcours Admin Société Collaborateurs)** :
  1. Authentification administrateur société ➜ Redirection `/societe/dashboard`.
  2. Navigation vers `/societe/collaborateurs` ➜ Modification de la visibilité d'un collaborateur sans saisie de GUID.
- **`E2E-05` (Vérification Cloisonnement 403)** :
  1. Utilisateur société A tente d'accéder par URL directe `/tickets/{ticketSocieteB}`.
  2. Assertion du retour 403 (Composant `PermissionDenied` affiché, aucune donnée fuitée).
- **`E2E-06` (Vérification Habilitation File 403)** :
  1. Client ou agent non habilité tente d'accéder à `/billing-import/facturation`.
  2. Assertion du refus d'accès serveur 403.

### 5.2 Tests Unitaires de Composants (bUnit)
- `StatutBadgeTests.cs` : Rendu correct des 5 classes de statuts et libellés.
- `ZoneActionsRequisesTests.cs` : Rendu des actions selon les items fournis.
- `NavMenuTests.cs` : Rendu des liens spécifiques à chaque persona et masquage des sections non autorisées.

### 5.3 Maintien du Baseline de Non-Régression
- Les **260 tests existants** (Domain, Application, Infrastructure, Web) doivent être exécutés et verts à 100 % à l'issue des développements.

---

## 6. Vérification de la Constitution (Constitution Check)

| Principe Constitutionnel | Vérification dans ce Plan | Statut |
|---|---|:---:|
| **I. Séparation des Couches** | Le Domaine et la Persistance restent 100 % purs et inchangés. Les nouvelles briques sont de pures Queries applicatives et composants Razor. | ✅ CONFORME |
| **II. Rôles et Autorisations** | Aucun nouveau rôle créé. Réutilisation stricte de `IAutorisationService` et des permissions existantes. | ✅ CONFORME |
| **VII. Moindre Privilège & Contrôle Serveur** | Aucun masquage UI ne remplace une sécurité serveur. 100 % des accès directs non autorisés sont bloqués en 403 côté serveur. | ✅ CONFORME |
| **VIII. Journal d'Audit** | L'audit technique inviolable (`SOC-13`) est isolé et réservé aux administrateurs. | ✅ CONFORME |
| **IX. Immutabilité & Non-Régression** | Les règles validées dans F001, F002 et F003 ne sont pas modifiées. Baseline de 260 tests préservé. | ✅ CONFORME |
| **X. Traçabilité CDC** | L'exigence contractuelle `ACR-01` (Section 17.1 CDC) est formellement couverte et testée. | ✅ CONFORME |
| **XI. Dépendances Minimales** | Utilisation exclusive des technologies déjà présentes (.NET 10, Blazor, Bootstrap, bUnit). | ✅ CONFORME |

---

## 7. État d'Avancement et Conclusion

- **Ambiguïtés levées** : 100 %
- **Spécifications, Contrats et Quickstart rédigés** : 100 %
- **Décision** : **`READY` pour `/speckit.tasks`**.

---
*(Fin du plan d'implémentation — Aucun code généré).*
