# RESEARCH & ANALYSE TECHNIQUE — FEATURE 013

**Feature** : Feature 013 — Continuité Opérationnelle, Brouillons de Saisie, Exposition d'API & Clôture Démarches  
**Date** : 2026-09-01  

---

## 1. AUDIT DU CODE EXISTANT

### 1.1 ERR-01 — État des formulaires et gestion des saisies
- **Formulaires audités** :
  - `src/CIT.Plateforme.Web/Components/Pages/BillingImport/SoumettreDemande.razor` (multi-BL, documents, société).
  - `src/CIT.Plateforme.Web/Components/Pages/BillingExport/SoumettreDemandeExport.razor` (demande export, type client).
  - `src/CIT.Plateforme.Web/Components/Pages/CustomerComplaint/NouvelleReclamation.razor` (catégories, description, pièces).
  - `src/CIT.Plateforme.Web/Components/Pages/Societe/` / `Enrolement` (dossier transporteur).
- **Constat** :
  - Les composants stockent leur état dans des modèles C# locaux in-memory liés au cycle de vie du composant Blazor.
  - En cas de rafraîchissement (F5), fermeture d'onglet, panne réseau ou expiration de session HTTP, l'état in-memory est réinitialisé et les champs saisis sont perdus.
- **Classification Code-First** : **R3 — Développement applicatif ciblé & modèle de persistance de brouillon**.

---

### 1.2 NFR-08 — État de l'architecture API & Interopérabilité
- **Audit `Program.cs`** :
  - L'application configure Razor Components, Identity Cookie, SignalR Hub (`SupervisionHub`).
  - Aucun contrôleur API ni endpoint REST dédié n'est actuellement exposé pour des tiers.
  - Aucun explorateur d'API ni générateur de contrat OpenAPI / Swagger n'est enregistré.
- **Constat** :
  - L'architecture ASP.NET Core .NET 10 permet nativement d'ajouter des Minimal APIs ou des contrôleurs d'API sans perturber le pipeline Blazor SSR.
  - Les cas d'usage Application (`ConsulterStatutTicketQuery`, `ConsulterDossierQuery`, `CalculerSimulationProformaCommand`) sont déjà découplés et immédiatement réutilisables par une couche d'API.
- **Classification Code-First** : **R3 — Développement d'une façade d'API sécurisée et documentée**.

---

### 1.3 SIM-11 — État du moteur de simulation proforma
- **Audit `Tarification`** :
  - `CalculerSimulationProformaCommand.cs` et `SimulateurTarifaire.cs` calculent déjà les montants de manière déterministe.
  - Les contrôles de cohérence interactifs (compatibilité taille/type, catégorie/sens, dates de séjour, exclusivité Reefer) ne sont pas encapsulés dans un validateur dédié renvoyant des messages d'aide contextuels à l'utilisateur.
- **Classification Code-First** : **R2 — Extension minimale du moteur de simulation et enrichissement UI**.

---

### 1.4 ENR-05 — État du module Enrôlement & Cycle de vie des badges
- **Audit `Enrolement`** :
  - `ConstaterRemiseBadgeCommand.cs` clôture le ticket d'enrôlement avec l'action `BadgeRemisEtDossierCloture` et horodatage `DateClotureUtc`.
  - La date de délivrance est matérialisée par `DateClotureUtc`.
  - L'entité `Ticket` et son `Dossier` associé contiennent déjà toutes les informations nécessaires.
  - Le renouvellement peut être initié comme une nouvelle commande `RenouvelerBadgeCommand` dans le même `DossierId`, et la détection d'expiration `DetecterBadgesAExpirerQuery` peut émettre l'événement `RappelExpirationBadge` vers F009.
- **Classification Code-First** : **R2/R3 — Extension applicative sur modèle existant (0 migration nécessaire pour ENR-05)**.

---

## 2. COMPARATIF DES STRATÉGIES DE PERSISTANCE POUR ERR-01

| Critère | Option A : Persistance Serveur Dédiée | Option B : Persistance Navigateur (`localStorage`) | Option C : Hybride (Client Cache + Sync Serveur) (Retenue) |
|:---|:---:|:---:|:---:|
| **Résilience fermeture / crash** | Totale | Totale (sur même poste) | **Totale** |
| **Résilience cross-device / mobilité** | Totale | Nulle | **Totale** |
| **Sécurité & Cloisonnement CPT-04** | Vérifié côté serveur (100% sûr) | Vulnérable aux manipulations locales | **Contrôlé côté serveur (100% sûr)** |
| **Nettoyage après soumission** | Transactionnel et déterministe | Dépendant du client JS | **Déterministe** |
| **Impact Base de données** | 1 table `BrouillonsSaisie` | 0 table | **1 table `BrouillonsSaisie`** |
| **Complexité d'implémentation** | Faible à modérée | Faible | **Modérée et robuste** |
| **Conformité CDC ERR-01** | 100 % | Partielle (perte en cas de changement de poste) | **100 %** |

**Décision retenue** : **Option A / C (Persistance serveur sécurisée avec gestion de debounce)**.
- Une table `BrouillonsSaisie` dédiée stocke le payload JSON de chaque formulaire par `(UtilisateurId, SocieteId, TypeFormulaire)`.
- Enregistrement atomique, cloisonnement multi-tenant garanti côté serveur, purge automatique sur soumission finale réussie.

---

## 3. STRATÉGIE D'ARCHITECTURE API POUR NFR-08

- **Technologie retenue** : **ASP.NET Core Minimal APIs & OpenAPI** intégrées dans le pipeline web.
- **Points d'accès exposés** :
  1. `GET /api/v1/tickets/{numero}` : Statut public/partenaire du ticket.
  2. `GET /api/v1/dossiers/{reference}` : Informations synthétiques du dossier.
  3. `POST /api/v1/simulations/calculer` : Calcul de proforma tarifaire.
  4. `GET /swagger` / OpenAPI schema : Documentation interactive et contrat d'interface.
- **Sécurité** :
  - Les endpoints protégés vérifient l'authentification et l'autorisation via `IAutorisationService` et `IContexteUtilisateurWeb`.
  - Cloisonnement CPT-04 / SEC-03 strict (interdiction absolue d'accéder aux données d'une société tierce).
