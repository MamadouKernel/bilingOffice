# Plan de Tâches Exécutables — Feature 007
## Module Avoir & Exonération

> **Document** : `specs/007-avoir-exoneration/tasks.md`  
> **Date** : 1er septembre 2026  
> **Statut** : Validé pour exécution  
> **Branche** : `master`  
> **Discipline** : **Strict Test-First (TDD Red-Green-Refactor)**, zéro code métier sans test préalable, zéro DbContext en Razor, zéro Guid manuel côté client, 100% additif.

---

## 1. Invariant Absolu Opposable & Baseline

### Invariant Opposable I-FINANCE
> **I-FINANCE** : La Feature 007 **ne digitalise PAS** le traitement financier interne.
> Sont formellement interdits dans le code :
> - Tout workflow hiérarchique comptable interne (Finance N1, N2, DAF).
> - Tout interfaçage / synchronisation avec les logiciels comptables (IRIS / ERP).
> - Toute génération automatique d'avoirs ou de documents comptables officiels.
> - Tout statut Finance spécifique non présent dans le socle F001.
>
> Le portail se limite strictement à : (1) recevoir la demande, (2) instruire, (3) gérer les compléments, (4) attendre le résultat hors application, (5) enregistrer la décision finale d'accord ou de rejet motivé, (6) joindre le document officiel externe déposé manuellement par l'agent, (7) notifier le client via F009, (8) clôturer le ticket.

### Baseline de Référence Certifiée
- **Tests** : 777 / 777 verts (Domain : 139, Application : 411, Infrastructure : 82, Web : 145)
- **Avertissements de build** : 0
- **Changements de modèle EF en attente** : 0

### Audit des Valeurs de Configuration (Non Contractuelles)

| Paramètre | Valeur par défaut | Source | Nature | Emplacement |
|:---|:---:|:---:|:---:|:---:|
| **Taille max pièce jointe** | 10 Mo | Transverse SOC-07 | Non contractuelle (Configurable) | `Documents:TailleMaximaleOctets` |
| **Formats autorisés** | PDF, JPEG, PNG | Transverse SOC-07 | Non contractuelle (Configurable) | `Documents:ExtensionsAutorisees` |
| **Nom de la file par défaut** | "Avoir & Exonération" | Paramétrage F001 | Non contractuelle (Configurable) | `DevelopmentSeeder.cs` |
| **Plafond pagination file** | 50 résultats | Standard UI | Non contractuelle (Configurable) | `CriteresFiltreFileAvoirExo` |

---

## 2. Tableau de Traçabilité Exigences / Contrats / Tâches / Tests

| Exigence | Contrat(s) C7 | Tâches d'implémentation | Tests Unitaires / Intégration | Scénario Recette / Sécurité |
|:---|:---:|:---|:---|:---:|
| **`AVE-01`** | `C7-01`, `C7-02`, `C7-04` | T001, T003, T004, T011, T012, T014, T015, T031, T041, T042 | `SoumettreDemandeAvoirTests`, `SoumettreDemandeExonerationTests`, `ListerMesDemandesAvoirExoTests` | `AVE-SPEC-01`, `AVE-SPEC-02`, `SEC-AVE-01` |
| **`AVE-02`** | `C7-03`, `C7-05`, `C7-06` | T005, T013, T016, T017, T032, T034, T043, T044 | `PrendreEnChargeAvoirExoTests`, `DemanderComplementAvoirExoTests`, `ConsulterFileAvoirExoTests` | `AVE-SPEC-04`, `AVE-SPEC-05`, `AVE-SPEC-06`, `SEC-AVE-03` |
| **`AVE-03`** | `C7-04`, `C7-07` | T002, T004, T005, T018, T019, T031, T044 | `DeciderDemandeAvoirExoTests`, `ObtenirFicheAvoirExoTests` | `AVE-SPEC-07`, `AVE-SPEC-08`, `AVE-SPEC-09`, `SEC-AVE-04` |
| **`AVE-04`** | `C7-01`, `C7-04` | T011, T012, T041 | `LiaisonTicketAvoirExoTests` | `AVE-SPEC-03`, `SEC-AVE-02` |

---

## 3. Plan Détaillé des Tâches par Phase

```
Phase 1 : Domaine Avoir & Exonération & Sécurité (T001 - T006)
Phase 2 : Couche Application & Cas d'Usage C7-01..07 (T011 - T025)
Phase 3 : Infrastructure, EF Core & Persistance Additive (T031 - T038)
Phase 4 : Interface Utilisateur Blazor (T041 - T048)
Phase 5 : Clôture Scénarios Recette TEST-SPEC & Sécurité E2E (T051 - T055)
Phase 6 : Audit Post-Implémentation, Non-Régression & Finalisation (T056 - T060)
```

---

### Phase 1 : Domaine Avoir & Exonération & Sécurité (Test-First)

- [x] **T001** [P] : Écrire les tests unitaires du domaine pour la création d'une demande Avoir/Exonération dans `CIT.Plateforme.Domain.Tests/AvoirExoneration/DemandeAvoirExoTests.cs` (RED) :
  - Création nominale Avoir (`TypeDemande == "Avoir"`) avec `Objet`, `Description`, `MontantConteste` optionnel.
  - Création nominale Exonération (`TypeDemande == "Exoneration"`).
  - Invariant : exception si `Numero` vide.
  - Invariant : exception si `TypeDemande` != `"Avoir"` et != `"Exoneration"`.
  - Invariant : exception si `Objet` ou `Description` vide.
  - Invariant : exception si `MontantConteste < 0`.
- [x] **T002** [P] : Écrire les tests unitaires des transitions de décision dans `DemandeAvoirExoTests.cs` (RED) :
  - Transition Accord (`DecisionAvoirExo.Accorde`) : statut passe à `Valide`, `MontantAccorde` enregistré, `DateClotureUtc` renseignée.
  - Transition Rejet (`DecisionAvoirExo.Rejete`) avec motif : statut passe à `Rejete`, `MotifRejetAvoirExo` enregistré, `DateClotureUtc` renseignée.
  - Invariant : exception si décision de Rejet sans motif (motif vide ou espaces) (`AVE-03`).
  - Invariant : exception si tentative de décision sur un ticket déjà `Cloture` ou `Annule`.
  - Persistance de la décision : vérification que `DecisionAvoirExo` reste accessible même après clôture.
- [x] **T003** : Implémenter l'enum `CIT.Plateforme.Domain.AvoirExoneration.DecisionAvoirExo` (`Accorde`, `Rejete`) (GREEN).
- [x] **T004** : Étendre l'entité `CIT.Plateforme.Domain.Tickets.Ticket` avec les propriétés additives F007 (`DecisionAvoirExo`, `MotifRejetAvoirExo`, `MontantConteste`, `MontantAccorde`, `DateDecisionAvoirExoUtc`, `DecideurAvoirExoId`) et les méthodes métier `CreerDemandeAvoirExo` et `EnregistrerDecisionAvoirExo` (GREEN).
- [x] **T005** : Étendre `CIT.Plateforme.Domain.Securite.NatureUtilisateur.cs` avec les permissions `Permission.AvoirExonerationInstruire = "avoirexoneration.instruire"` et `Permission.AvoirExonerationDecider = "avoirexoneration.decider"`.
- [x] **T006** : Valider que la suite `Domain.Tests` est 100% verte.

---

### Phase 2 : Couche Application & Cas d'Usage C7-01..07 (Test-First)

- [x] **T011** [P] : Écrire les tests applicatifs pour `SoumettreDemandeAvoirExonerationCommand` (C7-01 / AVE-01, AVE-04) dans `CIT.Plateforme.Application.Tests/AvoirExoneration/SoumettreDemandeAvoirExoTests.cs` (RED) :
  - Soumission d'une demande d'avoir avec pièces jointes.
  - Soumission d'une demande d'exonération avec attestation.
  - Soumission avec liaison optionnelle à un ticket Billing Import de la même société (`AVE-04`).
  - Rejet Anti-IDOR (`SEC-AVE-02`) si le ticket parent lié appartient à une autre société (`AccesRefuseException`).
  - Rejet si aucune pièce justificative n'est fournie.
  - Vérification de la création du Ticket, Dossier, pièces dans `Document` et publication de `DemandeAvoirSoumise` / `DemandeExonerationSoumise`.
- [x] **T012** : Implémenter `SoumettreDemandeAvoirExonerationCommand` et ses DTOs dans `CIT.Plateforme.Application/AvoirExoneration/DemandeAvoirExoCommands.cs` (GREEN).
- [x] **T013** [P] : Écrire les tests applicatifs pour les requêtes de consultation dans `CIT.Plateforme.Application.Tests/AvoirExoneration/ConsulterAvoirExoTests.cs` (RED) :
  - `ListerMesDemandesAvoirExonerationQuery` (C7-02) : ne retourne que les demandes de la société du demandeur (`SEC-AVE-01`).
  - `ConsulterFileAvoirExonerationQuery` (C7-03) : vérifie `Permission.AvoirExonerationInstruire` (`SEC-AVE-03`) et filtre par type/statut.
  - `ObtenirFicheAvoirExonerationQuery` (C7-04) : restitue liasse, ticket lié, statut et décision ; masque les notes internes aux clients.
- [x] **T014** : Implémenter `ListerMesDemandesAvoirExonerationQuery`, `ConsulterFileAvoirExonerationQuery` et `ObtenirFicheAvoirExonerationQuery` dans `CIT.Plateforme.Application/AvoirExoneration/DemandeAvoirExoQueries.cs` (GREEN).
- [x] **T015** [P] : Écrire les tests applicatifs pour `PrendreEnChargeAvoirExoCommand` (C7-05) et `DemanderComplementAvoirExoCommand` (C7-06) dans `CIT.Plateforme.Application.Tests/AvoirExoneration/InstructionAvoirExoTests.cs` (RED) :
  - Prise en charge par agent habilité (`Permission.AvoirExonerationInstruire`) -> statut passe à `EnCours`.
  - Demande de complément d'information -> bascule en `EnAttenteClient` (`SOC-06`), émission `AvoirComplementDemande` et alerte `ACR-01`.
- [x] **T016** : Implémenter `PrendreEnChargeAvoirExoCommand` et `DemanderComplementAvoirExoCommand` dans `DemandeAvoirExoCommands.cs` (GREEN).
- [x] **T017** [P] : Écrire les tests applicatifs pour `DeciderDemandeAvoirExoCommand` (C7-07 / AVE-03) dans `CIT.Plateforme.Application.Tests/AvoirExoneration/DeciderAvoirExoTests.cs` (RED) :
  - Accord par agent habilité (`Permission.AvoirExonerationDecider`) avec dépôt de document -> statut `Valide`, publication `AvoirAccorde` / `ExonerationAccordee`, clôture.
  - Rejet motivé par agent habilité -> statut `Rejete`, `MotifRejetAvoirExo` consigné, publication `AvoirRejete` / `ExonerationRejetee`, clôture.
  - Rejet sans motif -> exception métier immédiate.
  - Rejet de sécurité (`SEC-AVE-04`) si l'agent n'a pas `Permission.AvoirExonerationDecider`.
  - Rejet de sécurité (`SEC-AVE-08`) si le ticket n'appartient pas au module `"AvoirExoneration"`.
- [x] **T018** : Implémenter `DeciderDemandeAvoirExoCommand` dans `DemandeAvoirExoCommands.cs` (GREEN).
- [x] **T020** : Valider que la suite `Application.Tests` est 100% verte.

---

### Phase 3 : Infrastructure, EF Core & Persistance Additive

- [x] **T031** : Configurer les propriétés additives et l'index composite de `Ticket` dans `CIT.Plateforme.Infrastructure.Persistence.PlateformeDbContext.cs` :
  - `DecisionAvoirExo` (string 32), `MotifRejetAvoirExo` (1000), `MontantConteste` (18,2), `MontantAccorde` (18,2), `DateDecisionAvoirExoUtc`, `DecideurAvoirExoId`.
  - Index composite `IX_Tickets_Module_TypeDemande_Statut`.
- [x] **T032** : Enregistrer les use cases et services F007 dans `CIT.Plateforme.Infrastructure/DependencyInjection.cs`.
- [x] **T033** : Mettre à jour `DevelopmentSeeder.cs` pour affecter les permissions `AvoirExonerationInstruire` et `AvoirExonerationDecider` aux comptes de test (`agent.facturation@test.local`, `agent.paiement@test.local`, `m.konate`).
- [x] **T034** : Écrire les tests d'intégration de persistance sous SQL Server réel dans `CIT.Plateforme.Infrastructure.Tests/Persistence/AvoirExonerationPersistenceTests.cs` (RED) :
  - Persistance complète d'un ticket Avoir avec ses colonnes additives et son index.
  - Vérification de la persistance de `DecisionAvoirExo` et de `MotifRejetAvoirExo`.
- [x] **T035** : Valider `AvoirExonerationPersistenceTests.cs` sous SQL Server (GREEN).
- [x] **T036** : Générer la migration EF Core `AjoutModuleAvoirExoneration` (`dotnet ef migrations add AjoutModuleAvoirExoneration`).
- [x] **T037** : Vérifier que la migration est 100% additive (6 colonnes ajoutées, 1 index créé, 0 opération destructive dans `Up()`) et que `has-pending-model-changes` retourne 0.
- [x] **T038** : Valider que la suite `Infrastructure.Tests` est 100% verte.

---

### Phase 4 : Interface Utilisateur Blazor

- [x] **T041** [P] : Écrire les tests bUnit pour la soumission client dans `CIT.Plateforme.Web.Tests/Pages/AvoirExoneration/NouvelleDemandeAvoirExoTests.cs` (RED) :
  - Rendu du formulaire, sélection du type (Avoir / Exonération), téléversement justificatif, soumission.
- [x] **T042** : Implémenter la page Blazor Client `CIT.Plateforme.Web/Components/Pages/AvoirExoneration/NouvelleDemandeAvoirExo.razor` (`/billing-office/avoir-exoneration/demande`) (GREEN).
- [x] **T043** [P] : Écrire les tests bUnit pour la file agent dans `CIT.Plateforme.Web.Tests/Pages/AvoirExoneration/FileAvoirExonerationTests.cs` (RED) :
  - Affichage de la file, filtres par type, action prendre en charge.
  - Rejet d'accès si l'utilisateur ne possède pas `Permission.AvoirExonerationInstruire`.
- [x] **T044** : Implémenter la page Blazor Agent `CIT.Plateforme.Web/Components/Pages/AvoirExoneration/FileAvoirExoneration.razor` (`/billing-office/avoir-exoneration/file`) (GREEN).
- [x] **T045** [P] : Écrire les tests bUnit pour le détail et la décision dans `CIT.Plateforme.Web.Tests/Pages/AvoirExoneration/FicheAvoirExonerationTests.cs` (RED) :
  - Affichage fiche, onglets documents/messages, formulaire de décision (accord avec document, rejet avec motif obligatoire).
- [x] **T046** : Implémenter les composants Blazor :
  - `CIT.Plateforme.Web/Components/Pages/AvoirExoneration/FicheAvoirExoneration.razor` (`/billing-office/avoir-exoneration/ticket/{TicketId:guid}`).
  - `CIT.Plateforme.Web/Components/Pages/AvoirExoneration/MesDemandesAvoirExo.razor` (`/billing-office/avoir-exoneration/mes-demandes`).
- [x] **T047** : Ajouter les entrées de navigation dans `NavMenu.razor` :
  - Espace Client : « 📑 Avoir & Exonération » (`/billing-office/avoir-exoneration/mes-demandes`).
  - Espace Agent : « 📑 File Avoir & Exonération » (`/billing-office/avoir-exoneration/file`).
- [x] **T048** : Valider que la suite `Web.Tests` est 100% verte.

---

### Phase 5 : Clôture Scénarios Recette TEST-SPEC & Sécurité E2E

- [x] **T051** : Concevoir la suite de tests de bout en bout `CIT.Plateforme.Web.Tests/E2E/AveSpecTests.cs` couvrant l'intégralité des 10 scénarios `AVE-SPEC-01` à `AVE-SPEC-10` et les 8 contrôles de sécurité `SEC-AVE-01` à `SEC-AVE-08` :
  - `AVE-SPEC-01` : Soumission Avoir nominale avec justificatif -> Ticket `AVO-YYYY-XXXXX` au statut `Cree`.
  - `AVE-SPEC-02` : Soumission Exonération nominale avec attestation -> Ticket `EXO-YYYY-XXXXX` au statut `Cree`.
  - `AVE-SPEC-03` : Liaison optionnelle au ticket Billing Import parent (`LiaisonTicket` créé, `AVE-04`).
  - `AVE-SPEC-04` : Prise en charge par l'agent instructeur habilité -> statut `EnCours`.
  - `AVE-SPEC-05` : Demande de complément documentaire -> statut `EnAttenteClient` et alerte `ACR-01`.
  - `AVE-SPEC-06` : Dépôt du complément par le client -> reprise du traitement à `EnCours`.
  - `AVE-SPEC-07` : Prononcé d'un Accord par agent décideur avec dépôt de document -> statut `Valide`, clôture.
  - `AVE-SPEC-08` : Prononcé d'un Rejet avec motif obligatoire -> statut `Rejete`, motif consigné, clôture.
  - `AVE-SPEC-09` : Publication des événements sur `EvenementSocle` pour notification F009.
  - `AVE-SPEC-10` : Cloisonnement strict multi-société (SEC-01).
  - `SEC-AVE-01` à `SEC-AVE-08` : Rejet des accès non habilités, IDOR, manipulation de Guid et tickets non-AVE.
- [x] **T052** : Exécuter et certifier le passage à 100% vert de `AveSpecTests.cs`.

---

### Phase 6 : Audit Post-Implémentation, Non-Régression & Finalisation

- [x] **T056** : Exécuter la suite complète de validation :
  - `dotnet build` (0 avertissement)
  - `dotnet test` (100% vert, >= 790 tests)
  - `dotnet ef migrations has-pending-model-changes` (0 pending model change)
- [x] **T057** : Scanner le code de production pour valider l'absence totale de :
  - Moteur ERP comptable interne ou calculs comptables inventés.
  - Fournisseurs directs d'envoi de notification (Twilio, SendGrid, SmtpClient).
  - Violation d'architecture Razor (`DbContext`, `Repository`, `Guid.Parse` manuels).
- [x] **T058** : Rédiger le rapport d'implémentation `specs/007-avoir-exoneration/implementation-report.md`.
- [x] **T059** : Rédiger l'audit post-implémentation indépendant `specs/007-avoir-exoneration/post-implementation-audit.md`.
- [x] **T060** : Rédiger la revue de clôture finale `specs/007-avoir-exoneration/closure-review.md`.
