# Plan Technique d'Implémentation — Feature 007
## Module Avoir & Exonération

> **Document** : `specs/007-avoir-exoneration/plan.md`  
> **Date** : 1er septembre 2026  
> **Statut** : Validé pour passage à `/speckit.tasks`  
> **Sources de vérité** : `spec.md`, `checklists/requirements.md`, `research.md`, `data-model.md`, `contracts/use-cases.md`, `quickstart.md`, `.specify/memory/constitution.md`

---

## 1. Synthèse Technique & Démarche de Réalisation

La Feature 007 met en œuvre les exigences **`AVE-01` à `AVE-04`** du Cahier des Charges.
Elle adopte une approche **frugale et 100% additive** :
1. **Zéro nouvelle table SQL** : réutilisation directe de l'entité centrale `Ticket` enrichie de colonnes optionnelles.
2. **Réutilisation maximale du socle F001-F006** : `Dossier`, `DocumentJoint`, `Message`, `LiaisonTicket`, `AttenteClient`, `JournalAudit`.
3. **Dissociation nette portail / finance interne** : le circuit hiérarchique et comptable interne Finance s'opère hors application ; le portail gère l'instruction, les échanges, l'enregistrement de la décision finale (accord/rejet motivé), le dépôt des pièces et la notification.
4. **Découplage événementiel F009** : publication exclusive sur `EvenementSocle` sans adhérence de transport.
5. **Sécurité RBAC stricte** : permissions modulaires `Permission.AvoirExonerationInstruire` et `Permission.AvoirExonerationDecider` sans rôles organisationnels rigides (résolution complète de PC-03 et PC-15).

---

## 2. Constitution Check (17 / 17 Principes Respectés)

| N° | Principe de la Constitution | Statut | Justification Concrète dans F007 |
|:---|:---|:---:|:---|
| **I** | Strict Scope & Non-Regression | ✅ PASS | Périmètre strictement limité à `AVE-01..04`. Zéro régression sur F001-F006, F008-F010. |
| **II** | Source of Truth & Traceability | ✅ PASS | Toutes les exigences sont indexées sur le CDC v1.0 §11.6. Zéro extrapolation ERP. |
| **III** | Test-First Discipline (TDD) | ✅ PASS | Suite complète `TEST-SPEC` (10 scénarios) planifiée avant tout code de production. |
| **IV** | Minimalist Architecture & YAGNI | ✅ PASS | 0 nouvelle table SQL, 0 nouveau moteur de workflow inventé, 0 connecteur externe. |
| **V** | Transversal Reuse | ✅ PASS | Réutilisation intégrale des abstractions socles (`Ticket`, `LiaisonTicket`, `Document`, `Message`). |
| **VI** | Separation of Concerns | ✅ PASS | Couches Domain, Application, Infrastructure et Web strictement cloisonnées. |
| **VII** | Explicit & Robust Error Handling | ✅ PASS | Exceptions typées (`AccesRefuseException`, `ArgumentException` si motif rejet vide). |
| **VIII** | Domain Encapsulation | ✅ PASS | Méthodes de domaine dédiées (`CreerDemandeAvoirExo`, `EnregistrerDecisionAvoirExo`). |
| **IX** | Relational DB & Migrations | ✅ PASS | Migration EF Core 100% additive, 0 opération destructive dans `Up()`. |
| **X** | Secure Multi-Tenancy & Anti-IDOR | ✅ PASS | Identités et `SocieteId` dérivés côté serveur (`IContexteUtilisateurWeb`). |
| **XI** | Production-Grade Logging & Audit | ✅ PASS | Double journalisation : `JournalAudit` immuable et `HistoriqueTickets`. |
| **XII** | Event-Driven Architecture | ✅ PASS | Production exclusive d'instances `EvenementSocle` consommées par F009. |
| **XIII** | Performance & Scalability | ✅ PASS | Index composite optimisé `IX_Tickets_Module_TypeDemande_Statut`. |
| **XIV** | Organizational Continuity | ✅ PASS | Continuité assurée par le numéro unique `SOC-01` et le suivi de statut. |
| **XV** | Rich & Accessible Web UI | ✅ PASS | Composants Razor réactifs intégrés au thème CIT avec retours contextuels clairs. |
| **XVI** | No Hardcoded Business Invariants | ✅ PASS | Types de documents et seuils paramétrables sans modification de code. |
| **XVII** | Strict Selective Commit Discipline | ✅ PASS | Commit sélectif strict excluant toute modification étrangère. |

---

## 3. Architecture Technique par Couche

```
┌────────────────────────────────────────────────────────────────────────┐
│ CIT.Plateforme.Web                                                     │
│  - Pages/AvoirExoneration/NouvelleDemandeAvoirExo.razor                │
│  - Pages/AvoirExoneration/MesDemandesAvoirExo.razor                    │
│  - Pages/AvoirExoneration/FileAvoirExoneration.razor                   │
│  - Pages/AvoirExoneration/FicheAvoirExoneration.razor                  │
│  - NavMenu.razor (liens Client / Agent)                                │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ Dépend de
┌───────────────────────────────────▼────────────────────────────────────┐
│ CIT.Plateforme.Application                                             │
│  - AvoirExoneration/DemandeAvoirExoCommands.cs (C7-01, C7-05, C7-06, C7-07)│
│  - AvoirExoneration/DemandeAvoirExoQueries.cs (C7-02, C7-03, C7-04)   │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ Dépend de
┌───────────────────────────────────▼────────────────────────────────────┐
│ CIT.Plateforme.Domain                                                  │
│  - AvoirExoneration/DecisionAvoirExo.cs (Enum Accorde/Rejete)          │
│  - Tickets/Ticket.cs (Méthodes & propriétés additives F007)           │
│  - Securite/NatureUtilisateur.cs (Permissions AvoirExoneration)        │
└───────────────────────────────────▲────────────────────────────────────┘
                                    │ Implémenté par
┌───────────────────────────────────┴────────────────────────────────────┐
│ CIT.Plateforme.Infrastructure                                          │
│  - Persistence/PlateformeDbContext.cs (Mapping EF Core)                │
│  - Persistence/Migrations/20260901XXXXXX_AjoutModuleAvoirExoneration.cs│
│  - Persistence/DevelopmentSeeder.cs (Seed permissions tests)           │
│  - DependencyInjection.cs (Enregistrement use cases F007)              │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Matrice de Traçabilité des Contrats Applicatifs (C7-01 à C7-07)

| Contrat | Type | Fichier Cible | Tests Unitaires & Intégration |
| :--- | :---: | :--- | :--- |
| **`C7-01`** | Command | `Application/AvoirExoneration/DemandeAvoirExoCommands.cs` | `SoumettreDemandeAvoirTests.cs`, `SoumettreDemandeExonerationTests.cs` |
| **`C7-02`** | Query | `Application/AvoirExoneration/DemandeAvoirExoQueries.cs` | `ListerMesDemandesAvoirExoTests.cs` |
| **`C7-03`** | Query | `Application/AvoirExoneration/DemandeAvoirExoQueries.cs` | `ConsulterFileAvoirExoTests.cs` |
| **`C7-04`** | Query | `Application/AvoirExoneration/DemandeAvoirExoQueries.cs` | `ObtenirFicheAvoirExoTests.cs` |
| **`C7-05`** | Command | `Application/AvoirExoneration/DemandeAvoirExoCommands.cs` | `PrendreEnChargeAvoirExoTests.cs` |
| **`C7-06`** | Command | `Application/AvoirExoneration/DemandeAvoirExoCommands.cs` | `DemanderComplementAvoirExoTests.cs` |
| **`C7-07`** | Command | `Application/AvoirExoneration/DemandeAvoirExoCommands.cs` | `DeciderDemandeAvoirExoTests.cs` |

---

## 5. Matrice des Scénarios de Test Recette (`AVE-SPEC-01` à `AVE-SPEC-10`)

| Code Test | Exigence CDC | Description & Objectif de Vérification |
| :--- | :---: | :--- |
| **`AVE-SPEC-01`** | `AVE-01` | **Soumission Demande Avoir** : création ticket `AVO-YYYY-XXXXX`, liasse documentaire persistée, statut initial `Cree`. |
| **`AVE-SPEC-02`** | `AVE-01` | **Soumission Demande Exonération** : création ticket `EXO-YYYY-XXXXX`, attestation stockée, statut `Cree`. |
| **`AVE-SPEC-03`** | `AVE-04` | **Liaison Ticket Parent** : liaison optionnelle à un ticket Billing Import existant (`LiaisonTicket` créé). |
| **`AVE-SPEC-04`** | `AVE-02` | **Instruction Agent** : prise en charge par agent habilité, passage à `EnCours`. |
| **`AVE-SPEC-05`** | `AVE-02` | **Demande de Complément** : bascule en `EnAttenteClient` et alerte `ACR-01` émise. |
| **`AVE-SPEC-06`** | `AVE-02` | **Fourniture du Complément** : dépôt par le client et reprise automatique à `EnCours`. |
| **`AVE-SPEC-07`** | `AVE-03` | **Décision Accord** : enregistrement accord, dépôt du document de décision, clôture du ticket. |
| **`AVE-SPEC-08`** | `AVE-03` | **Décision Rejet Motivé** : motif obligatoire (exception si vide), enregistrement rejet et clôture. |
| **`AVE-SPEC-09`** | `AVE-03`, `COM-01`| **Publication Événements F009** : vérification des instances `EvenementSocle` publiées sur `JournalEvenements`. |
| **`AVE-SPEC-10`** | `SEC-01` | **Cloisonnement & Anti-IDOR** : rejet strict de tentative de consultation ou liaison d'un ticket appartenant à une société tierce. |

---

## 6. Découpage en Phases d'Exécution

```
Phase 1 : Domaine Avoir & Exonération & Sécurité (TDD Test-First)
  - Enum DecisionAvoirExo
  - Méthodes et propriétés additives de Ticket.cs
  - Permissions NatureUtilisateur.cs
  - Tests unitaires Domain.Tests

Phase 2 : Couche Application & Cas d'Usage C7-01..07 (TDD Test-First)
  - Commandes et requêtes Avoir & Exonération
  - Gestion des pièces et liaisons parent
  - Tests applicatifs Application.Tests

Phase 3 : Infrastructure, EF Core & Persistance
  - Configuration PlateformeDbContext
  - Migration EF Core 100% additive
  - DevelopmentSeeder pour comptes de test
  - Enregistrement DI dans DependencyInjection.cs
  - Tests d'intégration Infrastructure.Tests

Phase 4 : Interface Utilisateur Blazor
  - Formulaire de soumission client NouvelleDemandeAvoirExo.razor
  - Tableau de suivi MesDemandesAvoirExo.razor
  - File d'attente agent FileAvoirExoneration.razor
  - Fiche détaillée de traitement FicheAvoirExoneration.razor
  - Navigation NavMenu.razor
  - Tests bUnit Web.Tests

Phase 5 : Clôture Scénarios Recette TEST-SPEC & E2E
  - Suite de tests de bout en bout AveSpecTests.cs (10 scénarios complets)

Phase 6 : Audit Post-Implémentation, Non-Régression & Clôture
  - Vérification baseline (build 0 warning, 0 pending model change, >= 790 tests verts)
  - Rapports implementation-report.md, post-implementation-audit.md, closure-review.md
```
