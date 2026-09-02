# BACKLOG TECHNIQUE D'EXÉCUTION — FEATURE 014

**Feature** : Feature 014 — Internationalisation & Ergonomie Bilingue (Français / Anglais)  
**Date** : 2026-09-02  
**Baseline CDC d'entrée** : **168 / 188 (89,36 %)**  
**Périmètre** : **NFR-07** (Should)  
**Projection cible** : **169 / 188 (89,89 %)** *(non acquise avant Closure Review)*  

---

## 1. INVARIANTS D'ARCHITECTURE & SÉCURITÉ F014

- **I1** : Périmètre fonctionnel restreint exclusivement à **NFR-07** (Should).
- **I2** : **Zéro migration Entity Framework Core** (0 migration générée).
- **I3** : **Zéro nouvelle table SQL** en base de données.
- **I4** : **Zéro nouvelle entité de domaine** métier.
- **I5** : **Aucune donnée métier persistée traduite** (noms de sociétés, numéros de BL/booking/tickets, commentaires libres restent intangibles).
- **I6** : **Contrats d'API REST NFR-08 100% inchangés** (propriétés JSON et DTOs préservés).
- **I7** : **Authentification, 2FA, claims et permissions strictement inchangés**.
- **I8** : **Modèles de communication F009/F010 hors scope F014** (contenus métier préservés).
- **I9** : **Zéro dépendance externe** (service de traduction cloud, API tierce, etc.).
- **I10** : **Routes et URLs métier inchangées** (aucun préfixe d'URL destructif).
- **I11** : **Continuité de saisie ERR-01 préservée** (le switch de langue ne vide aucun formulaire et ne purge aucun brouillon).
- **I12** : **Approche TEST-FIRST stricte** (test RED avant implémentation GREEN).
- **I13** : **Zéro clé EN manquante** sur les surfaces certifiées (`FR_KEYS == EN_KEYS`).
- **I14** : **Zéro chaîne UI statique FR résiduelle** non justifiée sur les surfaces certifiées.
- **I15** : **Zéro commit avant l'approbation formelle de la Closure Review**.

---

## 2. INVENTAIRE DES SURFACES ET COMPOSANTS CERTIFIÉS F014

| Surface | Fichier composant | Éléments UI à localiser | Groupe ressource | TEST associé |
|:---|:---|:---|:---|:---:|
| **Accueil** | `Home.razor` | Titres, accroches de service, boutons de démarrage | `Home.*` | `NFR-CERT-07-04` |
| **Layout & Header** | `MainLayout.razor`, `PersonaSwitcher.razor` | Menus, liens nav, boutons actions, switcher langue | `Nav.*`, `Common.*` | `NFR-CERT-07-03` |
| **Authentification** | `Connexion.razor` | Titre, labels login/password, boutons, messages d'erreur | `Auth.*` | `NFR-CERT-07-04` |
| **2FA** | `ActiverDoubleAuthentification.razor`, `VerifierCode.razor` | Instructions QR code, saisie OTP, alertes | `Auth.*` | `NFR-CERT-07-06` |
| **Demande Import** | `SoumettreDemande.razor` | Étapes, champs BL/conteneurs, aides, validation | `BillingImport.*` | `NFR-CERT-07-04` |
| **Demande Export** | `SoumettreDemandeExport.razor` | Étapes, champs booking/escales, déclarations | `BillingExport.*` | `NFR-CERT-07-04` |
| **Réclamation Client**| `NouvelleReclamation.razor`, `MesReclamations.razor` | Catégories, champs objet/description, suivi | `Complaint.*` | `NFR-CERT-07-04` |
| **Enrôlement** | `SoumettreDemande.razor` (Enrôlement) | Champs identité, documents, aides | `Enrollment.*` | `NFR-CERT-07-04` |
| **Simulation Proforma**| `SimulationProformaPage.razor` | Paramètres séjour, conteneurs, alertes, franchise | `Simulation.*` | `NFR-CERT-07-04` |
| **Fiches de Suivi** | `DetailTicket.razor`, `DetailDossier.razor` | Badges de statut, jalons chronologiques, sections | `Status.*`, `Ticket.*` | `NFR-CERT-07-04` |

---

## 3. MATRICE DE TRAÇABILITÉ DES CONTRATS C14 (5/5)

| Contrat | Type | Description | Tâches associées | Test unitaire / d'intégration | Preuve |
|:---|:---:|:---|:---|:---|:---:|
| **`C14-01`** | **Query** | Résolution culture courante (`fr-FR` / `en-US`) | T006, T007 | `NFR-CERT-07-01`, `NFR-CERT-07-02` | `NfrCert07CultureTests.cs` |
| **`C14-02`** | **Command**| Commutation de langue via cookie `.AspNetCore.Culture` | T014, T015 | `NFR-CERT-07-03` | `NfrCert07SwitcherTests.cs` |
| **`C14-03`** | **Query** | Localisation d'un libellé UI avec fallback `fr-FR` | T009, T010 | `NFR-CERT-07-06` | `NfrCert07LocalisationTests.cs` |
| **`C14-04`** | **Query** | Formatage culturel dates/nombres (monnaie `XOF`) | T029, T030 | `NFR-CERT-07-04` | `NfrCert07FormatageTests.cs` |
| **`C14-05`** | **Query** | Vérification de parité et complétude bilingue | T011, T012 | `NFR-CERT-07-05` | `NfrCert07CompletudeRessourcesTests.cs` |

---

## 4. BACKLOG TECHNIQUE D'EXÉCUTION (45 TÂCHES ATOMIQUES)

### PHASE 1 — Baseline Gate, Worktree Safety & Invariants
- [x] **T001** : Vérifier la baseline technique initiale (`dotnet build`, 0 pending model change, 885 tests verts).
- [x] **T002** : Auditer l'état du worktree (`git status --short`) et figer la whitelist des fichiers F014. Interdiction de `git add .` et `git add -A`.
- [x] **T003** : Valider les invariants I1 à I15 et l'absence totale de migration EF Core.

---

### PHASE 2 — Infrastructure de Localisation & Middleware (C14-01)
- [x] **T004** : Rédiger le test unitaire RED `NFR-CERT-07-01` (Culture par défaut `fr-FR` en l'absence de cookie ou d'en-tête) dans `tests/CIT.Plateforme.Web.Tests/Localisation/NfrCert07CultureTests.cs`.
- [x] **T005** : Rédiger le test unitaire RED `NFR-CERT-07-02` (Détection de l'en-tête `Accept-Language: en-US` à la première visite).
- [x] **T006** : Configurer dans `src/CIT.Plateforme.Web/Program.cs` les services `AddLocalization(options => options.ResourcesPath = "Resources")`.
- [x] **T007** : Configurer dans `Program.cs` le middleware `UseRequestLocalization` avec `SupportedCultures` = `["fr-FR", "en-US"]`, `DefaultRequestCulture` = `fr-FR` et priorité `CookieRequestCultureProvider` > `AcceptLanguageHeaderRequestCultureProvider` (passage au GREEN).
- [x] **T008** : Adapter dynamiquement l'attribut `lang` dans `src/CIT.Plateforme.Web/Components/App.razor` selon la culture active.

---

### PHASE 3 — Dictionnaires de Ressources Bilingues & Test de Parité (C14-03, C14-05)
- [x] **T009** : Rédiger le test unitaire RED `NFR-CERT-07-06` (Fallback vers le français si une clé anglaise est absente).
- [x] **T010** : Créer la classe marqueur `src/CIT.Plateforme.Web/Resources/SharedResources.cs` et le service de localisation associé.
- [x] **T011** : Rédiger le test de parité et de complétude RED `NFR-CERT-07-05` (`NfrCert07CompletudeRessourcesTests.cs`) échouant si `FR_KEYS != EN_KEYS`.
- [x] **T012** : Implémenter le dictionnaire bilingue complet couvrant les clés `Common.*`, `Nav.*`, `Auth.*`, `BillingImport.*`, `BillingExport.*`, `Complaint.*`, `Enrollment.*`, `Simulation.*`, `Status.*` (passage au GREEN du test de parité).

---

### PHASE 4 — Composant LanguageSwitcher & Endpoint de Culture (C14-02)
- [x] **T013** : Rédiger le test d'intégration HTTP RED `NFR-CERT-07-03` (Bascule de culture via `POST /api/v1/culture/set` écrivant le cookie `.AspNetCore.Culture`).
- [x] **T014** : Implémenter l'endpoint Minimal API `POST /api/v1/culture/set` dans `src/CIT.Plateforme.Web/Api/ApiEndpoints.cs` sans altérer les contrats existants NFR-08.
- [x] **T015** : Créer le composant Blazor `src/CIT.Plateforme.Web/Components/Layout/LanguageSwitcher.razor` avec sélection FR 🇫🇷 / EN 🇬🇧.
- [x] **T016** : Intégrer `LanguageSwitcher.razor` dans le header principal (`MainLayout.razor` / `PersonaSwitcher.razor`).

---

### PHASE 5 — Localisation Accueil, Authentification & Layout
- [x] **T017** [P] : Injecter `IStringLocalizer<SharedResources>` et localiser les libellés de `src/CIT.Plateforme.Web/Components/Pages/Home.razor`.
- [x] **T018** [P] : Localiser les menus, liens et boutons de navigation dans `src/CIT.Plateforme.Web/Components/Layout/PersonaSwitcher.razor`.
- [x] **T019** [P] : Localiser les champs, labels et boutons de `src/CIT.Plateforme.Web/Components/Pages/Comptes/Connexion.razor`.
- [x] **T020** [P] : Localiser les écrans 2FA `ActiverDoubleAuthentification.razor` et `VerifierCode.razor`.
- [x] **T021** : Rédiger le test d'intégration validant le rendu bilingue complet des pages publiques et d'authentification.

---

### PHASE 6 — Localisation des 4 Formulaires Majeurs de Démarches
- [x] **T022** [P] : Localiser le formulaire Demande Import (`src/CIT.Plateforme.Web/Components/Pages/BillingImport/SoumettreDemande.razor`) : titres, étapes, champs BL, conteneurs, types de livraison, aides.
- [x] **T023** [P] : Localiser le formulaire Demande Export (`src/CIT.Plateforme.Web/Components/Pages/BillingExport/SoumettreDemandeExport.razor`) : booking, escales, formalités.
- [x] **T024** [P] : Localiser les formulaires Réclamation Client (`NouvelleReclamation.razor`, `MesReclamations.razor`).
- [x] **T025** [P] : Localiser le formulaire Enrôlement Transporteur (`SoumettreDemande.razor` section Enrôlement).
- [x] **T026** : Rédiger les tests d'intégration UI validant la soumission nominale des 4 formulaires en anglais.

---

### PHASE 7 — Localisation Simulation Proforma & Fiches de Suivi
- [x] **T027** : Localiser l'interface de simulation proforma (`src/CIT.Plateforme.Web/Components/Pages/Tarification/SimulationProformaPage.razor`) sans toucher aux calculs tarifaires F005 / SIM-11.
- [x] **T028** : Localiser les fiches synthétiques `DetailTicket.razor` et `DetailDossier.razor` (badges de statut, libellés de jalons).

---

### PHASE 8 — Formatage Culturel, Libellés d'Enums & Validations (C14-04)
- [x] **T029** : Rédiger les tests unitaires RED pour `FormaterCulturellementQuery` (dates `dd/MM/yyyy` vs format EN, montants en `XOF`).
- [x] **T030** : Implémenter la méthode d'extension/helper de formatage culturel et de libellés d'enums (`En cours` / `In progress`, `Validé` / `Approved`).
- [x] **T031** : Localiser les messages de validation de formulaire (`Required`, format invalide, erreurs de saisie).

---

### PHASE 9 — Non-Régression Transverse, ERR-01, API & Sécurité
- [x] **T032** : Rédiger le test d'intégration démontrant la préservation de saisie et des brouillons ERR-01 lors d'une bascule de langue en cours de formulaire.
- [x] **T033** : Rédiger les tests de non-régression de sécurité validant que la langue ne modifie aucun claim, rôle, cookie d'authentification ou règle 2FA.
- [x] **T034** : Contrôler par test d'intégration que les endpoints d'API REST NFR-08 conservent leurs DTOs et schémas JSON stricts inchangés.
- [x] **T035** : Vérifier par scan statique l'absence de chaînes UI françaises hardcodées résiduelles sur les surfaces certifiées.

---

### PHASE 10 — Non-Régression Globale, Rapports d'Audit & Gates de Clôture
- [x] **T036** : Exécuter la compilation globale `dotnet build` (0 avertissement, 0 erreur).
- [x] **T037** : Vérifier que `dotnet ef migrations has-pending-model-changes` renvoie strictement 0 change.
- [x] **T038** : Exécuter l'intégralité de la suite de tests de façon sérialisée.
- [x] **T039** : Exécuter l'audit anti-scope mécanique sur `git diff` (0 IA, 0 NFR-03..06/09/10, 0 SEC-02..09, 0 SMS/WhatsApp, 0 migration).
- [x] **T040** : Rédiger le rapport d'implémentation dans `specs/014-internationalisation-ergonomie-bilingue/implementation-report.md`.
- [x] **T041** : Rédiger le rapport de Regression Gate dans `specs/014-internationalisation-ergonomie-bilingue/regression-audit.md`.
- [x] **T042** : Rédiger le rapport de Post-Implementation Audit dans `specs/014-internationalisation-ergonomie-bilingue/post-implementation-audit.md`.
- [x] **T043** : Rédiger le rapport de Closure Review dans `specs/014-internationalisation-ergonomie-bilingue/closure-review.md`.
- [x] **T044** : Vérifier l'éligibilité au Commit Gate (0 warning, 100% tests verts, 0 pending model change).
- [x] **T045** : Attester de la progression de la baseline CDC officielle à **169 / 188 (89,89 %)**.
