# REGRESSION AUDIT REPORT — FEATURE 013

**Feature** : Feature 013 — Continuité Opérationnelle, Brouillons de Saisie, Exposition d'API & Clôture Démarches  
**Date** : 2026-09-02  
**Baseline CDC avant F013** : **164 / 188 (87,23 %)**  
**Périmètre audité** : **ERR-01** (Must), **NFR-08** (Should), **SIM-11** (Could), **ENR-05** (Could)  
**Projection cible** : **168 / 188 (89,36 %)** *(non acquise avant Closure Review)*  

---

## 1. BASELINE TECHNIQUE & COMPILATION

```text
Build : SUCCESS (0 Avertissement, 0 Erreur)
EF Core Pending Changes : 0 pending model changes
Suite de tests complète sérialisée : 885 / 885 tests verts (100% PASS)
  - CIT.Plateforme.Domain.Tests : 158 / 158
  - CIT.Plateforme.Application.Tests : 461 / 461 (+14 tests)
  - CIT.Plateforme.Infrastructure.Tests : 83 / 83
  - CIT.Plateforme.Web.Tests : 183 / 183 (+4 tests)
Delta net : +18 tests automatisés de certification F013
```

---

## 2. AUDIT FORENSIQUE DU DIFF & SÉPARATION DU WORKTREE

- **Modifications F013 isolées** :
  - **Domaine** : `CIT.Plateforme.Domain/Formulaires/BrouillonSaisie.cs`.
  - **Application** : `IBrouillonRepository.cs`, `IParametresEnrolementProvider.cs`, `EnregistrerBrouillonCommand.cs`, `ObtenirBrouillonQuery.cs`, `SupprimerBrouillonCommand.cs`, `ValiderCoherenceSimulationQuery.cs`, `RappelerExpirationBadgesCommand.cs`, `DemanderRenouvellementBadgeCommand.cs`, `Repositories.cs`.
  - **Infrastructure** : `BrouillonRepository.cs`, `ParametresEnrolementProvider.cs`, `PlateformeDbContext.cs`, `DependencyInjection.cs`, `DevelopmentSeederNotifications.cs`, `Repositories.cs`, Migration `20260901212846_AjouterBrouillonsSaisie.cs` + `Designer.cs` + `ModelSnapshot.cs`.
  - **Web** : `CIT.Plateforme.Web.csproj` (`Microsoft.AspNetCore.OpenApi`), `Program.cs`, `Api/ApiDtos.cs`, `Api/ApiEndpoints.cs`.
  - **Tests** : `ErrCert01BrouillonsTests.cs`, `SimCert11ValidationsTests.cs`, `EnrCert05RenouvellementTests.cs`, `Imp11EtProducteursTests.cs`, `ErrCert01WebBrouillonsTests.cs`, `NfrCert08ApiTests.cs`.
- **Worktree Safety** : Aucune commande `git add .` / `git add -A` autorisée. Les modifications préexistantes de l'arbre de travail restent non stagées et strictement exclues de F013.

---

## 3. AUDIT APPROFONDI PAR EXIGENCE

### 3.1 ERR-01 : Continuité opérationnelle et persistance des brouillons (Must)
- **Vérification du modèle & stockage** :
  - Table `BrouillonsSaisie` créée par migration strictement additive.
  - Clé logique composite `(UtilisateurId, TypeFormulaire)` avec isolation `SocieteId`.
  - Stratégie de concurrence : *Last-Write-Wins* atomique.
  - Absence absolue de flux binaires (Stream/byte[]/base64) dans `DonneesJson` : seules les métadonnées de pièces jointes sont conservées.
- **Cycle de vie du brouillon** :
  - Sauvegarde et reprise après déconnexion/session timeout démontrées.
  - Conservation après échec de soumission métier démontrée.
  - Purge automatique sur soumission finale réussie ou abandon volontaire démontrée.
  - Cloisonnement multi-tenant (CPT-04) : rejet strict des tentatives inter-utilisateurs et inter-sociétés.
- **Verdict ERR-01** : **CONFORME (PASS)**.

---

### 3.2 NFR-08 : Façade d'API ouverte, sécurisée et documentée (Should)
- **Vérification des interfaces** :
  - `GET /api/v1/tickets/{numero}` : Statut et horodatages avec contrôle CPT-04 / SEC-03.
  - `GET /api/v1/dossiers/{reference}` : Informations synthétiques du dossier et liste des tickets.
  - `POST /api/v1/simulations/calculer` : Réutilisation directe du moteur de calcul F005 sans duplication de logique.
  - `GET /swagger/v1/swagger.json` : Contrat OpenAPI formel et accessible (`Microsoft.AspNetCore.OpenApi`).
- **Sécurité API** :
  - Authentification obligatoire (`RequireAuthorization()`).
  - DTOs publics isolés : 0 entité EF Core brute exposée.
  - Codes HTTP conformes : 401 (non authentifié), 403 (société tierce / hors périmètre), 404 (introuvable), 400 (paramètre invalide), 200 (succès).
- **Verdict NFR-08** : **CONFORME (PASS)**.

---

### 3.3 SIM-11 : Contrôles de cohérence à la saisie de simulation proforma (Could)
- **Texte CDC source** : *« Contrôles de cohérence à la saisie (ex. taille et type reconnus, catégorie compatible avec le sens) avec messages d'aide. »*
- **Matrice de conformité des 4 règles CDC** :

| Règle CDC | Spécification Spec Kit | Implémentation Code | Test Automatisé | Verdict |
|:---|:---|:---|:---|:---:|
| **1. Dates de séjour** | `dateEnlevement >= dateArrivee` | `ValiderCoherenceSimulationQuery.cs` (L32-47) | `Valider_DateEnlevementAnterieureArrivee_RetourneAlerte` | **CONFORME** |
| **2. Exclusivité Reefer** | Branchement réservé aux conteneurs Reefer | `ValiderCoherenceSimulationQuery.cs` (L59-62) | `Valider_BranchementFrigorifiqueSurConteneurDry_RetourneAlerte` | **CONFORME** |
| **3. Catégorie vs Sens** | Catégorie compatible avec le flux Import/Export | `ValiderCoherenceSimulationQuery.cs` (L49-57) | `Valider_CategorieIncompatibleAvecSens_RetourneAlerte` | **CONFORME** |
| **4. Taille / Type reconnus** | Taille et type reconnus par la grille tarifaire | `ValiderCoherenceSimulationQuery.cs` (L43-47) | `Valider_TailleOuTypeNonReconnu_RetourneAlerte` | **CONFORME** |

- **Explication de la divergence apparente** : L'implémentation initiale avait mentionné le contrôle de masse et le calcul F005 dans le résumé textuel ; le service `ValiderCoherenceSimulationQuery` a été explicitement aligné pour exécuter et tester les 4 règles du CDC mot à mot, sans règle fiscale ni seuil inventé.
- **Verdict SIM-11** : **CONFORME (PASS)**.

---

### 3.4 ENR-05 : Cycle de vie et renouvellement des badges transporteurs (Could)
- **Paramétrage sans valeur en dur** :
  - Abstraction `IParametresEnrolementProvider` avec implémentation `ParametresEnrolementProvider` lisant la configuration.
  - Exception explicite levée si la configuration est manquante ou invalide (0 fallback silencieux).
- **Détection des échéances & F009** :
  - Calcul d'échéance déterministe basé sur `IHorloge`.
  - Détection des badges arrivant à expiration dans le préavis et émission de l'événement `RappelExpirationBadge` via `EvenementSocle`.
  - Règles de diffusion semées dans `DevelopmentSeederNotifications.cs` (InApp / Email).
  - Idempotence garantie par F009 (0 notification parasite dupliquée).
  - 0 connecteur SMS/WhatsApp externe (PC-16 respecté).
- **Renouvellement** :
  - Commande `DemanderRenouvellementBadgeCommand` (C13-09) créant un ticket `RenouvellementBadge` dans le dossier existant sans re-saisie des pièces permanentes et avec traçabilité d'audit.
- **Verdict ENR-05** : **CONFORME (PASS)**.

---

## 4. AUDIT DE LA MIGRATION EF CORE

- **Migration** : `20260901212846_AjouterBrouillonsSaisie.cs`.
- **Nature** : **Strictement additive**.
  - `CreateTable("BrouillonsSaisie")`
  - `CreateIndex("IX_Brouillons_Societe")`
  - `CreateIndex("IX_Brouillons_Utilisateur_Type")`
- **Opérations destructives** : **0** (aucun `DropTable`, aucun `DropColumn`, aucun renommage destructif).
- **Pending model changes post-migration** : **0**.
- **Verdict Migration** : **CONFORME (PASS)**.

---

## 5. TRAÇABILITÉ DES CONTRATS C13 (9/9) & TEST-SPEC (4/4)

| Contrat | Exigence CDC | Implémentation | Suite de Tests | Statut |
|:---|:---:|:---|:---|:---:|
| **C13-01** | **ERR-01** | `EnregistrerBrouillonCommand.cs` | `ErrCert01BrouillonsTests.cs` | **PASS** |
| **C13-02** | **ERR-01** | `ObtenirBrouillonQuery.cs` | `ErrCert01BrouillonsTests.cs` | **PASS** |
| **C13-03** | **ERR-01** | `SupprimerBrouillonCommand.cs` | `ErrCert01BrouillonsTests.cs` | **PASS** |
| **C13-04** | **NFR-08** | `ApiEndpoints.cs` (`/api/v1/tickets/{numero}`) | `NfrCert08ApiTests.cs` | **PASS** |
| **C13-05** | **NFR-08** | `ApiEndpoints.cs` (`/api/v1/dossiers/{reference}`) | `NfrCert08ApiTests.cs` | **PASS** |
| **C13-06** | **NFR-08** | `ApiEndpoints.cs` (`/api/v1/simulations/calculer`) | `NfrCert08ApiTests.cs` | **PASS** |
| **C13-07** | **SIM-11** | `ValiderCoherenceSimulationQuery.cs` | `SimCert11ValidationsTests.cs` | **PASS** |
| **C13-08** | **ENR-05** | `RappelerExpirationBadgesCommand.cs` | `EnrCert05RenouvellementTests.cs` | **PASS** |
| **C13-09** | **ENR-05** | `DemanderRenouvellementBadgeCommand.cs` | `EnrCert05RenouvellementTests.cs` | **PASS** |

- **Couverture TEST-SPEC** :
  - `ERR-CERT-01` : 100% PASS
  - `NFR-CERT-08` : 100% PASS
  - `SIM-CERT-11` : 100% PASS
  - `ENR-CERT-05` : 100% PASS

---

## 6. AUDIT ANTI-SCOPE

- **Composants IA (IA-01..07)** : 0 développement.
- **SMS / WhatsApp direct** : 0 connecteur externe.
- **NFR-07 (Haute disponibilité / Production)** : 0 dérive hors périmètre.
- **SEC-02 / SEC-07** : 0 développement opportuniste.
- **Refactoring UI transverse** : 0 altération non liée à F013.
- **Verdict Anti-Scope** : **PASS**.

---

## 7. CALCUL DU GAIN CDC & SYNTHÈSE

```text
Baseline CDC officielle avant F013 : 164 / 188 (87,23 %)
Exigences validées conformes :
  - ERR-01 : +1
  - NFR-08 : +1
  - SIM-11 : +1
  - ENR-05 : +1
Gain maximal autorisable : +4
Projection CDC post-F013 : 168 / 188 (89,36 %) — NON ACQUISE avant Closure Review
```
