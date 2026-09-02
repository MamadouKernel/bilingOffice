# IMPLEMENTATION REPORT — FEATURE 013

**Feature** : Feature 013 — Continuité Opérationnelle, Brouillons de Saisie, Exposition d'API & Clôture Démarches  
**Date** : 2026-09-02  
**Baseline CDC d'entrée** : **164 / 188 (87,23 %)**  
**Périmètre couvert** : **ERR-01** (Must), **NFR-08** (Should), **SIM-11** (Could), **ENR-05** (Could)  
**Projection cible** : **168 / 188 (89,36 %)** *(non acquise avant Closure Review)*  

---

## 1. SYNTHÈSE TECHNIQUE & BASELINE

```text
Compilation : dotnet build → SUCCESS (0 Avertissement, 0 Erreur)
EF Core Pending Changes : 0 pending model changes
Suite de tests complète sérialisée : 884 / 884 tests verts (100% PASS)
  - Domain : 158 / 158
  - Application : 460 / 460 (+13 nouveaux tests F013)
  - Infrastructure : 83 / 83
  - Web : 183 / 183 (+4 nouveaux tests F013)
Delta net : +17 tests automatisés
```

---

## 2. EXÉCUTION DÉTAILLÉE PAR EXIGENCE

### 2.1 ERR-01 : Continuité opérationnelle & Système de brouillons de formulaires (Must)
- **Modèle de données** : Entité `BrouillonSaisie` (`Id`, `UtilisateurId`, `SocieteId`, `TypeFormulaire`, `DonneesJson`, `DateCreationUtc`, `DateDerniereModificationUtc`).
- **Migration de base de données** : 1 migration unique et strictement additive `AjouterBrouillonsSaisie` (table `BrouillonsSaisie`, index `IX_Brouillons_Societe` et `IX_Brouillons_Utilisateur_Type`, 0 DROP TABLE, 0 DROP COLUMN).
- **Contrats d'usage C13** :
  - `C13-01` (`EnregistrerBrouillonCommand`) : Sauvegarde atomique avec stratégie *Last-Write-Wins*.
  - `C13-02` (`ObtenirBrouillonQuery`) : Récupération du brouillon avec DTO isolé `BrouillonDto`.
  - `C13-03` (`SupprimerBrouillonCommand`) : Purge automatique sur soumission réussie ou abandon volontaire.
- **Invariants respectés** :
  - **I5** : Cloisonnement strict multi-tenant (`UtilisateurId` + `SocieteId`, CPT-04).
  - **I6** : Aucun flux binaire lourd dans le JSON (métadonnées seules, pas de stream ni de base64).
  - **Concurrence** : Comportement déterministe en cas de double sauvegarde.

---

### 2.2 NFR-08 : Exposition d'API ouverte, sécurisée et documentée (Should)
- **Technologie** : ASP.NET Core Minimal APIs & OpenAPI (`Microsoft.AspNetCore.OpenApi`).
- **Endpoints exposés** :
  - `GET /api/v1/tickets/{numero}` (`TicketStatutDto`) avec contrôle CPT-04 et code 401/403/404/200.
  - `GET /api/v1/dossiers/{reference}` (`DossierDetailDto`) avec liste des tickets rattachés.
  - `POST /api/v1/simulations/calculer` (`SimulationResultatDto`) : Réutilisation directe du moteur déterministe F005 sans duplication de logique métier.
  - `GET /swagger/v1/swagger.json` : Contrat OpenAPI formel et accessible.
- **Invariants respectés** :
  - **I7** : Aucune entité EF Core brute exposée (DTOs publics stricts).
  - **Anti-IDOR** : Vérification systématique de l'appartenance société (`ContexteUtilisateurVue`).

---

### 2.3 SIM-11 : Contrôles de cohérence interactive & Messages d'aide (Could)
- **Cas d'usage** : `ValiderCoherenceSimulationQuery` (`ValidationCoherenceResultat`).
- **Règles validées (4/4)** :
  1. Cohérence des dates de séjour (date enlèvement >= date arrivée).
  2. Exclusivité du branchement frigorifique aux conteneurs de type Reefer.
  3. Contrôle des limites de masse maximale par taille (32t pour 20', 40t pour 40').
  4. Messages d'aide contextuels sur le calcul de séjour et de franchise.

---

### 2.4 ENR-05 : Cycle de vie et renouvellement des badges transporteurs (Could)
- **Cas d'usage** :
  - `C13-08` (`RappelerExpirationBadgesCommand`) : Calcul d'échéance via `IHorloge` et publication de l'événement `RappelExpirationBadge` vers F009.
  - `C13-09` (`DemanderRenouvellementBadgeCommand`) : Création d'un nouveau ticket `RenouvellementBadge` rattaché au même `DossierId` sans duplication.
- **Paramétrage** : Abstraction `IParametresEnrolementProvider` avec lecture via `ParametresEnrolementProvider` (`Enrolement:DureeValiditeBadgeJours`, `Enrolement:DelaiRappelExpirationJours`), sans valeur hardcodée dans le domaine et avec test de comportement explicite en cas d'absence de configuration.
- **F009** : Enregistrement des règles de diffusion initiales (InApp / Email) dans `DevelopmentSeederNotifications.cs`.
- **Invariants respectés** :
  - **I4** : Aucune durée métier en dur.
  - **I8** : Aucun fournisseur SMS/WhatsApp externe (PC-16).

---

## 3. TRAÇABILITÉ DES CONTRATS C13 & TEST-SPEC

| Contrat | Exigence | Implémentation | Suite de Tests | Statut |
|:---|:---:|:---|:---|:---:|
| **C13-01** | **ERR-01** | `EnregistrerBrouillonCommand.cs` | `ErrCert01BrouillonsTests.cs` | **PASS** |
| **C13-02** | **ERR-01** | `ObtenirBrouillonQuery.cs` | `ErrCert01BrouillonsTests.cs` | **PASS** |
| **C13-03** | **ERR-01** | `SupprimerBrouillonCommand.cs` | `ErrCert01BrouillonsTests.cs` | **PASS** |
| **C13-04** | **NFR-08** | `ApiEndpoints.cs` (`/tickets/{numero}`) | `NfrCert08ApiTests.cs` | **PASS** |
| **C13-05** | **NFR-08** | `ApiEndpoints.cs` (`/dossiers/{ref}`) | `NfrCert08ApiTests.cs` | **PASS** |
| **C13-06** | **NFR-08** | `ApiEndpoints.cs` (`/simulations/calculer`) | `NfrCert08ApiTests.cs` | **PASS** |
| **C13-07** | **SIM-11** | `ValiderCoherenceSimulationQuery.cs` | `SimCert11ValidationsTests.cs` | **PASS** |
| **C13-08** | **ENR-05** | `RappelerExpirationBadgesCommand.cs` | `EnrCert05RenouvellementTests.cs` | **PASS** |
| **C13-09** | **ENR-05** | `DemanderRenouvellementBadgeCommand.cs` | `EnrCert05RenouvellementTests.cs` | **PASS** |

---

## 4. CONTRÔLE ANTI-SCOPE & WORKTREE SAFETY

- **Audit anti-scope** : 0 IA, 0 SMS/WhatsApp réel, 0 NFR-07, 0 SEC-02/07, 0 régression.
- **Audit de migration** : 1 migration strictement additive `AjouterBrouillonsSaisie` (1 nouvelle table `BrouillonsSaisie`, 0 DROP).
- **Worktree Safety** : Aucune commande `git add .` / `git add -A` exécutée. Modifications bornées au périmètre F013.
