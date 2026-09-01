# Plan de Tâches Exécutables — Feature 010
## Continuité Guichet & Rendez-vous

> **Document** : `specs/010-continuite-guichet-rendez-vous/tasks.md`  
> **Date** : 1er septembre 2026  
> **Statut** : Validé pour exécution  
> **Branche** : `master`  
> **Discipline** : **Strict Test-First (TDD Red-Green-Refactor)**, zéro code métier sans test unitaire préalable, zéro DbContext en Razor, zéro Guid manuel côté client, 100% additif.

---

## 1. Baseline Certifiée & Valeurs de Configuration

### Baseline de Référence
- **Tests** : 747 / 747 verts (Domain : 129, Application : 400, Infrastructure : 77, Web : 141)
- **Avertissements de build** : 0
- **Changements de modèle EF en attente** : 0

### Audit des Valeurs de Configuration (Non Contractuelles)

| Paramètre | Valeur par défaut | Source | Nature | Emplacement |
|:---|:---:|:---:|:---:|:---:|
| **Plafond recherche guichet** | 50 résultats | Décision d'architecture (D1) | Non contractuelle (Configurable) | `Guichet:RendezVous:LimiteResultatsRecherche` |
| **Capacité par créneau** | 5 usagers | Hypothèse dimensionnement (D4) | Non contractuelle (Configurable) | `Guichet:RendezVous:CapaciteParCreneau` |
| **Durée d'un créneau** | 30 minutes | Organisationnelle standard (D4) | Non contractuelle (Configurable) | `Guichet:RendezVous:DureeCreneau` |
| **Heure ouverture accueil** | 08:00 | Horaires bureau de facturation (D4)| Non contractuelle (Configurable) | `Guichet:RendezVous:HeureOuverture` |
| **Heure fermeture accueil** | 16:30 | Horaires bureau de facturation (D4)| Non contractuelle (Configurable) | `Guichet:RendezVous:HeureFermeture` |

---

## 2. Tableau de Traçabilité Exigences / Contrats / Tâches / Tests

| Exigence | Contrat(s) C10 | Tâches d'implémentation | Tests Unitaires / Intégration | Scénario Recette |
|:---|:---:|:---|:---|:---:|
| **`CON-01`** | `C10-02` | T021, T022, T031 | `DossierGuichetTests`, `RepriseSansRessaisieTests` | `REC-SCE-14` (Étape 8) |
| **`CON-02`** | `C10-01` | T011, T012, T032, T041 | `RechercheGuichetTests`, `SecRechercheTests` | `REC-SCE-14` (Étape 6) |
| **`CON-03`** | `C10-03` à `C10-09` | T001 à T010, T013 à T020, T033, T042 à T045 | `RendezVousTests`, `RendezVousUseCaseTests`, `RendezVousRepositoryTests` | `REC-SCE-14` (Étapes 3, 4, 11) |
| **`CON-04`** | `C10-07` | T017, T043, T046 | `TraitementPresentielTests`, `EvenementSocleTests` | `REC-SCE-14` (Étape 9) |
| **`IMP-15`** | `C10-10` | T023, T024, T047 | `AlternativePresentielleTests` | `REC-SCE-14` (Étape 1) |
| **`ERR-04`** | `C10-11` | T025, T026, T048 | `ModeDegradeTests`, `AuditRegularisationTests` | `REC-SCE-14` (Transverse) |

---

## 3. Plan Détaillé des Tâches par Phase

```
Phase 1 : Domaine Guichet & Sécurité (T001 - T010)
Phase 2 : Couche Application & Cas d'Usage C10 (T011 - T030)
Phase 3 : Infrastructure, EF Core & Tests SQL Server Réel (T031 - T040)
Phase 4 : Interface Utilisateur Blazor (T041 - T050)
Phase 5 : Clôture Scénario REC-SCE-14 & Tests E2E (T051 - T055)
Phase 6 : Audit Post-Implémentation, Non-Régression & Finalisation (T056 - T060)
```

---

### Phase 1 : Domaine Guichet & Sécurité (Test-First)

- [ ] **T001** [P] : Écrire les tests unitaires du domaine `CIT.Plateforme.Domain.Tests/Guichet/RendezVousTests.cs` (RED) :
  - Création nominale à l'état `Reserve` avec `TicketId`, `DemandeurId`, `Date`, `HeureDebut`, `HeureFin`, `Motif`.
  - Invariant : exception si `TicketId == Guid.Empty` ou `DemandeurId == Guid.Empty`.
  - Invariant : exception si `HeureDebut >= HeureFin`.
  - Invariant : exception si `Motif` est vide ou dépasse 200 caractères.
- [ ] **T002** [P] : Écrire les tests unitaires des transitions d'états dans `RendezVousTests.cs` (RED) :
  - Transition `Reserve` -> `Honore` avec enregistrement de l'agent et date de passage.
  - Transition `Reserve` -> `Annule` avec motif d'annulation.
  - Transition `Reserve` -> `NonPresente` avec commentaire.
  - Transition `Reserve` -> `Reserve` lors du report avec nouvelles coordonnées temporelles.
  - Invariant : exceptions lors de tentatives de transitions depuis `Honore`, `Annule`, `NonPresente` (états terminaux immuables).
- [ ] **T003** : Implémenter l'enum `CIT.Plateforme.Domain.Guichet.StatutRendezVous` (`Reserve`, `Honore`, `Annule`, `NonPresente`) (GREEN).
- [ ] **T004** : Implémenter l'entité domaine `CIT.Plateforme.Domain.Guichet.RendezVous` avec méthodes métier `Creer`, `Honorer`, `Annuler`, `Reporter`, `MarquerNonPresente` et jeton de concurrence `VersionConcurrence` (GREEN).
- [ ] **T005** : Étendre `CIT.Plateforme.Domain.Securite.NatureUtilisateur.cs` avec les permissions `Permission.GuichetRecherche = "guichet.recherche"` et `Permission.RendezVousGerer = "rendezvous.gerer"`.
- [ ] **T006** : Valider que la suite `Domain.Tests` est 100% verte.

---

### Phase 2 : Couche Application & Cas d'Usage C10 (Test-First)

- [ ] **T011** [P] : Définir les interfaces de persistance et de recherche dans `CIT.Plateforme.Application.Abstractions` :
  - `IRendezVousRepository.cs` (Ajouter, ObtenirParId, ObtenirActifParTicket, ListerParDemandeur, ListerParDate, CompterReservationsCreneau, PossedeRdvActif).
  - `IRechercheGuichetRepository.cs` (RechercherAsync avec projection `ResultatRechercheGuichetDto`).
- [ ] **T012** [P] : Implémenter l'objet de configuration `CIT.Plateforme.Application.Guichet.ParametresRendezVous.cs` avec validation des plages et capacités par défaut.
- [ ] **T013** : Écrire les tests applicatifs pour `RechercherGuichetQuery` (C10-01) dans `CIT.Plateforme.Application.Tests/Guichet/RechercheGuichetTests.cs` (RED) :
  - Recherche par N° ticket exact et partiel.
  - Recherche par N° dossier, N° BL, N° booking.
  - Recherche par nom client insensible à la casse et sans espaces résiduels.
  - Cas aucun résultat, cas plusieurs résultats bornés au plafond configuré (50).
  - Contrôle RBAC : rejet si agent non porteur de `Permission.GuichetRecherche`.
- [ ] **T014** : Implémenter `RechercherGuichetQuery` et son handler dans `CIT.Plateforme.Application.Guichet` (GREEN).
- [ ] **T015** : Écrire les tests applicatifs pour `ConsulterDossierGuichetQuery` (C10-02 / CON-01) dans `CIT.Plateforme.Application.Tests/Guichet/ConsulterDossierGuichetTests.cs` (RED) :
  - Restitution intégrale de la liasse documentaire et des métadonnées du ticket existant (0 duplication).
  - Ajout automatique d'une trace de consultation guichet.
- [ ] **T016** : Implémenter `ConsulterDossierGuichetQuery` et son handler (GREEN).
- [ ] **T017** : Écrire les tests applicatifs pour `ListerCreneauxDisponiblesQuery` (C10-03) et `ReserverRendezVousCommand` (C10-04 / CON-03) dans `CIT.Plateforme.Application.Tests/Guichet/ReserverRendezVousTests.cs` (RED) :
  - Calcul des places restantes par tranche horaire.
  - Réservation nominale, persistance et publication de l'événement `RendezVousReserve` sur `EvenementSocle`.
  - Rejet si le ticket possède déjà un RDV actif.
  - Rejet si le ticket est clôturé ou rejeté.
  - Rejet si tentative de prise de RDV sur le ticket d'un tiers (Anti-IDOR).
  - Rejet avec exception métier `CreneauCompletException` si la capacité maximale du créneau est atteinte.
- [ ] **T018** : Implémenter `ListerCreneauxDisponiblesQuery`, `ReserverRendezVousCommand` et leurs handlers (GREEN).
- [ ] **T019** : Écrire les tests applicatifs pour `AnnulerRendezVousCommand` (C10-05) et `ReporterRendezVousCommand` (C10-06) dans `CIT.Plateforme.Application.Tests/Guichet/GestionRendezVousTests.cs` (RED) :
  - Annulation par le client propriétaire : libération immédiate de la place et publication `RendezVousAnnule`.
  - Report atomique : libération de l'ancien créneau et réservation du nouveau sur le même RDV.
  - Rejet du report si le nouveau créneau est complet (rollback intégral).
- [ ] **T020** : Implémenter `AnnulerRendezVousCommand`, `ReporterRendezVousCommand` et leurs handlers (GREEN).
- [ ] **T021** : Écrire les tests applicatifs pour `MarquerRendezVousHonoreCommand` (C10-07 / CON-04), `MarquerRendezVousNonPresenteCommand` (C10-08) et `ListerMesRendezVousQuery` (C10-09) dans `CIT.Plateforme.Application.Tests/Guichet/TraitementGuichetTests.cs` (RED) :
  - Validation de présence par agent avec `Permission.RendezVousGerer`, trace d'audit et émission `RendezVousHonore`.
  - Marquage non présenté.
  - Consultation des RDV filtrée par `DemandeurId` du contexte serveur.
- [ ] **T022** : Implémenter `MarquerRendezVousHonoreCommand`, `MarquerRendezVousNonPresenteCommand`, `ListerMesRendezVousQuery` et leurs handlers (GREEN).
- [ ] **T023** : Écrire les tests applicatifs pour `BasculerAlternativePresentielleCommand` (C10-10 / IMP-15) dans `CIT.Plateforme.Application.Tests/Guichet/AlternativePresentielleTests.cs` (RED) :
  - Vérification de l'étape Contrôle BL validé.
  - Ajout de la trace dans l'historique et émission `BasculePresentielleEffectuee`.
- [ ] **T024** : Implémenter `BasculerAlternativePresentielleCommand` et son handler (GREEN).
- [ ] **T025** : Écrire les tests applicatifs pour `RegulariserModeDegradeCommand` (C10-11 / ERR-04) dans `CIT.Plateforme.Application.Tests/Guichet/ModeDegradeTests.cs` (RED) :
  - Consignation rétroactive d'une opération physique après incident avec trace explicite dans le `JournalAudit`.
  - Idempotence et non-duplication du ticket.
- [ ] **T026** : Implémenter `RegulariserModeDegradeCommand` et son handler (GREEN).
- [ ] **T030** : Valider que la suite `Application.Tests` est 100% verte.

---

### Phase 3 : Infrastructure, EF Core & Tests SQL Server Réel (Test-First)

- [ ] **T031** : Configurer l'entité `RendezVous` dans `CIT.Plateforme.Infrastructure.Persistence.PlateformeDbContext.cs` :
  - `DbSet<RendezVous> RendezVous`
  - Mapping table `RendezVous`, FK `Tickets` (Restrict) et FK `Dossiers` (Restrict).
  - Index unique filtré `IX_RendezVous_TicketId_ActifUnique` (`WHERE Statut = 'Reserve'`).
  - Index composite `IX_RendezVous_Date_HeureDebut` (`Date`, `HeureDebut`, `Statut`).
  - Index `IX_RendezVous_DemandeurId`.
  - Jeton de concurrence `VersionConcurrence` (RowVersion).
- [ ] **T032** : Implémenter `CIT.Plateforme.Infrastructure.Persistence.RendezVousRepository.cs` et `RechercheGuichetRepository.cs`.
- [ ] **T033** : Enregistrer les services et repositories F010 dans `CIT.Plateforme.Infrastructure.DependencyInjection.cs`.
- [ ] **T034** : Mettre à jour `DevelopmentSeeder.cs` pour attribuer les permissions `GuichetRecherche` et `RendezVousGerer` aux comptes de test opérationnels (`agent.billing`, `agent.gate`, `m.konate`).
- [ ] **T035** : Écrire les tests d'intégration et de concurrence sous SQL Server réel dans `CIT.Plateforme.Infrastructure.Tests/Persistence/RendezVousRepositoryTests.cs` (RED) :
  - `CAS-RDV-01` : Deux réservations concurrentes sur la dernière place disponible -> exactement 1 succès et 1 rejet propre `CreneauCompletException`.
  - `CAS-RDV-02` : Réservations multiples jusqu'à la capacité limite acceptées.
  - `CAS-RDV-03` : Dépassement de capacité -> conflit métier propre.
  - `CAS-RDV-04` : Annulation concurrente libère immédiatement la place pour un second demandeur.
  - `CAS-RDV-05` : Report vers un créneau plein -> rollback complet sans altérer l'ancien créneau.
  - `CAS-RDV-06` : Report réussi -> ancien créneau libéré, nouveau créneau occupé.
  - `CAS-RDV-07` : Deux reports concurrents vers la dernière place -> cohérence garantie (exactement 1 accepté).
- [ ] **T036** : Exécuter et valider `RendezVousRepositoryTests.cs` sous SQL Server réel (GREEN).
- [ ] **T037** : Générer la migration EF Core `AjoutContinuiteGuichetRendezVous` (`dotnet ef migrations add AjoutContinuiteGuichetRendezVous`).
- [ ] **T038** : Vérifier que la migration est 100% additive (1 table créée, 2 FK, 4 index, 0 opération destructive) et que `has-pending-model-changes` retourne 0.
- [ ] **T040** : Valider que la suite `Infrastructure.Tests` est 100% verte.

---

### Phase 4 : Interface Utilisateur Blazor & Raccordement

- [ ] **T041** [P] : Écrire les tests bUnit pour la recherche guichet dans `CIT.Plateforme.Web.Tests/Pages/Guichet/RechercheGuichetTests.cs` (RED) :
  - Rendu du formulaire de recherche, soumission, affichage des résultats paginés.
  - Rejet d'affichage si l'utilisateur ne possède pas `Permission.GuichetRecherche`.
- [ ] **T042** : Implémenter la page Blazor Agent `CIT.Plateforme.Web/Components/Pages/Guichet/RechercheGuichet.razor` (`/guichet/recherche`) (GREEN).
- [ ] **T043** [P] : Écrire les tests bUnit pour l'agenda guichet dans `CIT.Plateforme.Web.Tests/Pages/Guichet/AgendaGuichetTests.cs` (RED) :
  - Affichage des créneaux de la journée et des rendez-vous réservés.
  - Action « Valider présence usager » (`Honore`).
- [ ] **T044** : Implémenter la page Blazor Agent `CIT.Plateforme.Web/Components/Pages/Guichet/AgendaGuichet.razor` (`/guichet/agenda`) (GREEN).
- [ ] **T045** [P] : Écrire les tests bUnit pour la prise et la consultation de RDV client dans `CIT.Plateforme.Web.Tests/Pages/Guichet/MesRendezVousTests.cs` (RED) :
  - Sélection de créneau, confirmation, liste « Mes rendez-vous », action « Annuler ».
- [ ] **T046** : Implémenter les composants Blazor Client :
  - `CIT.Plateforme.Web/Components/Pages/Guichet/PrendreRendezVous.razor` (`/rendez-vous/prendre/{ticketId:guid}`).
  - `CIT.Plateforme.Web/Components/Pages/Guichet/MesRendezVous.razor` (`/rendez-vous/mes-rendez-vous`).
- [ ] **T047** : Câbler le bouton « Finaliser au guichet » (`IMP-15`) sur la fiche de contrôle documentaire `FileControleDocumentaire.razor`.
- [ ] **T048** : Intégrer les alertes RDV dans la section « Mes actions requises » du tableau de bord client (`DashboardClient.razor` — `ACR-01`).
- [ ] **T049** : Ajouter les entrées de menu dans `NavMenu.razor` :
  - Espace Client : « 📅 Mes rendez-vous » (`/rendez-vous/mes-rendez-vous`).
  - Espace Agent / Supervision : « 🔍 Recherche guichet » (`/guichet/recherche`) et « 📅 Agenda guichet » (`/guichet/agenda`).
- [ ] **T050** : Valider que la suite `Web.Tests` est 100% verte.

---

### Phase 5 : Clôture Scénario Contractuel REC-SCE-14 (E2E)

- [ ] **T051** : Concevoir le test de bout en bout `CIT.Plateforme.Web.Tests/E2E/RecSce14E2ETests.cs` rejouant l'intégralité des 14 étapes du scénario de recette contractuel :
  - Étape 1 : Création de la demande en ligne par le Client A.
  - Étape 2 : Contrôle documentaire et émission facture proforma.
  - Étape 3 : Choix de l'alternative présentielle et réservation d'un créneau RDV (`CON-03`).
  - Étape 4 : Émission de l'événement `RendezVousReserve` et notification F009.
  - Étape 5 : Tentative de prise de RDV sur le créneau par d'autres usagers jusqu'à saturation.
  - Étape 6 : Arrivée physique de l'usager et recherche immédiate au guichet par l'Agent (`CON-02`).
  - Étape 7 : Ouverture du dossier au guichet sans ressaisie de la liasse (`CON-01`).
  - Étape 8 : Enregistrement du règlement physique en caisse (`ValiderPaiementCaisseCommand`).
  - Étape 9 : Validation de la présence de l'usager (`MarquerRendezVousHonoreCommand` / `CON-04`).
  - Étape 10 : Émission de l'événement `RendezVousHonore` et mise à jour de l'historique (`SOC-13`).
  - Étape 11 : Mise à jour en temps réel de l'espace client (RDV honoré, ticket à l'étape suivante).
  - Étape 12 : Vérification du cloisonnement strict (le Client B ne voit aucun élément du Client A).
  - Étape 13 : Vérification du journal d'audit complet de la transaction.
  - Étape 14 : Vérification de la résilience et du mode dégradé (`ERR-04`).
- [ ] **T052** : Exécuter et certifier le passage à 100% vert de `RecSce14E2ETests.cs`.

---

### Phase 6 : Audit Post-Implémentation, Non-Régression & Finalisation

- [ ] **T056** : Exécuter la suite complète de validation :
  - `dotnet build` (0 avertissement)
  - `dotnet test` (100% vert, >= 780 tests)
  - `dotnet ef migrations has-pending-model-changes` (0 pending model change)
- [ ] **T057** : Scanner le code de production pour valider l'absence totale de :
  - Fournisseurs de calendrier externes (Google Calendar, Outlook, Exchange).
  - Fournisseurs directs d'envoi de notification (Twilio, SendGrid, SmtpClient).
  - Mode offline PWA déconnecté.
  - Violation d'architecture Razor (`DbContext`, `Repository`, `Guid.Parse` manuels).
- [ ] **T058** : Rédiger le rapport d'implémentation `specs/010-continuite-guichet-rendez-vous/implementation-report.md`.
- [ ] **T059** : Rédiger l'audit post-implémentation indépendant `specs/010-continuite-guichet-rendez-vous/post-implementation-audit.md`.
- [ ] **T060** : Rédiger la revue de clôture finale `specs/010-continuite-guichet-rendez-vous/closure-review.md`.
