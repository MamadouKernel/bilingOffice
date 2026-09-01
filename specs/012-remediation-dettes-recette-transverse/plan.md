# Implementation Plan: Feature 012 — Remédiation Dettes Métier, Recette Transverse & Clôture Réserves

> **Branch**: `012-remediation-dettes-recette-transverse` | **Date**: 1er septembre 2026 | **Spec**: [spec.md](./spec.md)  
> **Input**: `spec.md` (CLARIFIÉE), `checklists/requirements.md`, `docs/20-preflight-feature-012.md`, `research.md`, `data-model.md`, `contracts/use-cases.md`, `quickstart.md`, Constitution projet.  
> **Note** : Ce document constitue le plan technique d'exécution. Aucun code n'est modifié, aucune migration n'est créée pendant `/speckit.plan`.

---

## 1. Summary

La **Feature 012** clôture l'ensemble des réserves fonctionnelles et dettes de recette accumulées depuis le début du projet, certifie 7 exigences CDC (`PRI-02`, `IMP-03`, `REC-02`, `REC-03`, `SEC-08`, `SOC-14`, `COM-06`) et produit la preuve de recette transverse pour le scénario contractuel **REC-SCE-14**.

### Caractéristiques Majeures :
- **4 Must** (`PRI-02`, `IMP-03`, `REC-02`, `REC-03`), **1 Should** (`SEC-08`), **2 Could** (`SOC-14`, `COM-06`).
- **1 Scénario E2E transverse** (`REC-SCE-14`) couvrant 7 volets sans gain de couverture unitaire (+0).
- **Gain maximal projeté** : **+7 exigences** (157 → 164 / 188 = 87,23 %).
- **0 nouvelle entité métier, 0 nouvelle table, 0 migration EF Core**.
- **0 dépendance externe / 0 fournisseur tiers requis**.

---

## 2. Technical Context

- **Language / Framework** : C# 13 / .NET 10 (Constitution Principe I).
- **Dependencies** : ASP.NET Core Blazor Server, Entity Framework Core, ASP.NET Core Identity, xUnit, bUnit, `Microsoft.AspNetCore.Mvc.Testing` — **aucune nouvelle dépendance**.
- **Storage** : SQL Server LocalDB / in-memory pour les tests — **0 migration, 0 nouvelle table**.
- **Testing Strategy** : Suites TEST-SPEC nominatives xUnit / bUnit, isolation des tests Web xUnit pour stabiliser la LocalDB partagée, suite E2E transverse.
- **Constraints** : Respect absolu du cloisonnement multi-tenant (CPT-04 / SEC-03), audit inviolable append-only (SEC-05).

---

## 3. Constitution Check (17 / 17 PASS)

| Principe | Évaluation | Justification |
|:---|:---:|:---|
| **I. Stack technique imposée** | ✅ PASS | C# 13 / .NET 10, ASP.NET Core Blazor Server, EF Core, aucune dépendance exotique |
| **II. Source de vérité et traçabilité** | ✅ PASS | Chaque exigence cite son texte littéral du CDC, traçabilité nominative intégrale |
| **III. Priorisation Must/Should/Could** | ✅ PASS | 4 Must, 1 Should, 2 Could. Aucune exigence hors périmètre réintroduite |
| **IV. Principes d'architecture** | ✅ PASS | Non-duplication stricte (REC-02/03 certifiées sans nouveau code, réutilisation F009/F001) |
| **V. Domaine métier** | ✅ PASS | Aucune nouvelle entité ; entités existantes enrichies de méthodes ciblées si nécessaire |
| **VI. Workflows paramétrables** | ✅ PASS | Priorités, catégories de réclamations et canaux restent entièrement paramétrables |
| **VII. Sécurité (NON-NÉGOCIABLE)** | ✅ PASS | Cloisonnement multi-tenant garanti sur l'export CSV (SOC-14) et vérification 2FA (SEC-08) |
| **VIII. Données et persistance** | ✅ PASS | **0 migration, 0 nouvelle table, 0 modification de schéma** |
| **IX. Audit et traçabilité** | ✅ PASS | Toutes les opérations (création ticket, priorité, exports) consignées dans `JournalAudit` |
| **X. Gestion des documents** | ✅ PASS | Téléchargement CSV via flux sécurisé et JS helper existant |
| **XI. Intégrations et périmètre autonome** | ✅ PASS | Aucun compte tiers externe (Twilio, SendGrid, SMTP externe) imposé |
| **XII. Tests et stratégie de validation** | ✅ PASS | Test-first rigoureux, isolation du harnais xUnit, 10 runs 2FA stables exigés |
| **XIII. Performance et exploitation** | ✅ PASS | Export CSV léger en mémoire sans surcharge serveur |
| **XIV. Observabilité** | ✅ PASS | Messages d'erreurs explicites et traçage standardisé |
| **XV. Gestion des points non résolus** | ✅ PASS | Aucun point de clarification bloquant rouvert |
| **XVI. Definition of Done** | ✅ PASS | 10 critères de complétude stricts |
| **XVII. Gouvernance des écarts** | ✅ PASS | Alignement complet avec la baseline CDC officielle |

---

## 4. Plan de Modification Détaillé par Exigence

### 1. PRI-02 (Must) — Priorité Automatique & Tri en File
- **Nature** : Correction minimale (R3).
- **Fichiers modifiés** :
  - `src/CIT.Plateforme.Application/Tickets/CreerTicketCommand.cs` (attribution initiale priorité standard)
  - `src/CIT.Plateforme.Application/BillingImport/SoumettreDemandeImportCommand.cs` (attribution initiale priorité standard)
  - `src/CIT.Plateforme.Application/Tickets/ConsulterFileQuery.cs` (tri `OrderByDescending(t => t.Priorite.Rang).ThenBy(t => t.DateCreationUtc)`)
- **Suite de test** : `tests/CIT.Plateforme.Application.Tests/Tickets/PriCert01PrioriteTests.cs` (PRI-CERT-01).

### 2. IMP-03 (Must) — Communication Ticket Import par E-mail
- **Nature** : Correction minimale (R3).
- **Fichiers modifiés** :
  - `src/CIT.Plateforme.Application/BillingImport/SoumettreDemandeImportCommand.cs` (ajout de `PublierEvenementAsync("TicketCree", ...)`)
- **Suite de test** : `tests/CIT.Plateforme.Application.Tests/BillingImport/ImpCert03NotificationEmailTests.cs` (IMP-CERT-03).

### 3. REC-02 (Must) — Catégories de Réclamations Dynamiques
- **Nature** : Certification seule (R1).
- **Fichiers modifiés** : **AUCUN** fichier de production.
- **Suite de test** : `tests/CIT.Plateforme.Application.Tests/CustomerComplaint/RecCert02CategoriesTests.cs` (REC-CERT-02).

### 4. REC-03 (Must) — Orientation Support Opérationnel et Instruction
- **Nature** : Certification seule (R1).
- **Fichiers modifiés** : **AUCUN** fichier de production.
- **Suite de test** : `tests/CIT.Plateforme.Application.Tests/CustomerComplaint/RecCert03OrientationSupportTests.cs` (REC-CERT-03).

### 5. SEC-08 (Should) — Second Facteur (2FA) & Stabilisation Harnais
- **Nature** : Correction de harnais (R3).
- **Fichiers modifiés** :
  - `tests/CIT.Plateforme.Web.Tests/Comptes/ParcoursConnexion2FaTests.cs` (utilisation de `UserManager.GenerateTwoFactorTokenAsync` et attribution à la collection isolée)
  - `tests/CIT.Plateforme.Web.Tests/CIT.Plateforme.Web.Tests.csproj` (ou configuration xUnit pour sérialisation)
- **Suite de test** : `tests/CIT.Plateforme.Web.Tests/Comptes/SecCert08DoubleAuthentificationTests.cs` (SEC-CERT-08) — 10 runs consécutifs verts.

### 6. SOC-14 (Could) — Export CSV Tabulaire Cloisonné
- **Nature** : Développement ciblé (R4).
- **Fichiers créés / modifiés** :
  - `src/CIT.Plateforme.Application/Tickets/ExporterTicketsCsvQuery.cs` (Création)
  - `src/CIT.Plateforme.Web/Components/Pages/Client/MesDemandes.razor` ou `FileReclamations.razor` (Ajout bouton export CSV)
- **Suite de test** : `tests/CIT.Plateforme.Application.Tests/Tickets/SocCert14ExportCsvTests.cs` (SOC-CERT-14).

### 7. COM-06 (Could) — Canaux Configurables Relances Automatiques
- **Nature** : Correction minimale (R3).
- **Fichiers modifiés** :
  - `src/CIT.Plateforme.Infrastructure/Persistence/DevelopmentSeederNotifications.cs` (règles `RelanceAutomatique` InApp + Email)
- **Suite de test** : `tests/CIT.Plateforme.Application.Tests/Notifications/ComCert06CanauxRelanceTests.cs` (COM-CERT-06).

### 8. REC-SCE-14 — Scénario Transverse de Recette E2E
- **Nature** : Recette E2E (R2).
- **Fichiers créés** :
  - `tests/CIT.Plateforme.Web.Tests/E2E/RecSce14NotificationsE2ETests.cs` (7 volets contractuels intégrés)

---

## 5. Stratégie de Gestion du Worktree et Commit Sélectif

```text
Fichiers préexistants modifiés dans le worktree (À EXCLURE STRICTEMENT DU COMMIT F012) :
- App.razor, PersonaSwitcher.razor, DashboardAgent.razor, thèmes UI, pages de profil antérieures.

Fichiers STRICTEMENT rattachés à F012 (À COMMITER) :
- specs/012-remediation-dettes-recette-transverse/*
- docs/20-preflight-feature-012.md
- src/CIT.Plateforme.Application/BillingImport/SoumettreDemandeImportCommand.cs (patch IMP-03/PRI-02)
- src/CIT.Plateforme.Application/Tickets/ConsulterFileQuery.cs (patch PRI-02)
- src/CIT.Plateforme.Application/Tickets/CreerTicketCommand.cs (patch PRI-02)
- src/CIT.Plateforme.Application/Tickets/ExporterTicketsCsvQuery.cs (nouveau)
- src/CIT.Plateforme.Infrastructure/Persistence/DevelopmentSeederNotifications.cs (patch COM-06)
- tests/CIT.Plateforme.Application.Tests/Tickets/PriCert01PrioriteTests.cs (nouveau)
- tests/CIT.Plateforme.Application.Tests/BillingImport/ImpCert03NotificationEmailTests.cs (nouveau)
- tests/CIT.Plateforme.Application.Tests/CustomerComplaint/RecCert02CategoriesTests.cs (nouveau)
- tests/CIT.Plateforme.Application.Tests/CustomerComplaint/RecCert03OrientationSupportTests.cs (nouveau)
- tests/CIT.Plateforme.Application.Tests/Tickets/SocCert14ExportCsvTests.cs (nouveau)
- tests/CIT.Plateforme.Application.Tests/Notifications/ComCert06CanauxRelanceTests.cs (nouveau)
- tests/CIT.Plateforme.Web.Tests/Comptes/SecCert08DoubleAuthentificationTests.cs (nouveau / stabilisé)
- tests/CIT.Plateforme.Web.Tests/E2E/RecSce14NotificationsE2ETests.cs (nouveau)
```

---

## 6. Definition of Done (DoD)

1. **7 suites TEST-SPEC créées et 100% vertes**.
2. **Suite transverse `RecSce14NotificationsE2ETests.cs` 100% verte sur les 7 volets**.
3. **10 runs consécutifs de la suite 2FA sans aucun échec**.
4. **0 régression** sur la baseline technique existante (858 tests).
5. **0 avertissement compilateur .NET**.
6. **0 pending EF model change (0 migration créée)**.
7. **Rapports de clôture et audit indépendant complets**.
8. **Commit sélectif exclusif F012**.
