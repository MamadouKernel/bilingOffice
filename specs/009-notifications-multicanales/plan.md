# Implementation Plan: Feature 009 — Notifications Multicanales & Centre de Notifications In-App

**Branch**: `009-notifications-multicanales` | **Date**: 1er septembre 2026 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/009-notifications-multicanales/spec.md` (statut **CLARIFIÉE**, §25 Session 2026-09-01), `checklists/requirements.md` (**READY FOR /speckit.plan**, 21/21 catégories)

**Note**: Ce plan est produit exclusivement à partir de `spec.md`, `checklists/requirements.md`, `docs/00` à `docs/04`, `docs/13-preflight-feature-009.md`, `.specify/memory/constitution.md`, et du **code réel F001→F008 audité fichier par fichier** (cf. [research.md](./research.md)). Aucun code n'est produit par ce document ; `/speckit.tasks` n'est pas invoqué.

---

## Summary

F009 livre le **moteur de notification** de la plateforme et son **centre de notifications in-app**. L'approche retenue ([research.md](./research.md)) est une **consommation du journal d'événements existant** plutôt qu'un couplage aux commandes métier : `EvenementSocle` porte, écrit par F001, le commentaire *« la future feature Notifications s'y abonnera »* — F009 réalise exactement cette intention. Conséquence majeure : **les 21 sites qui publient déjà des événements — répartis sur 19 fichiers — ne sont pas modifiés**, et aucune régression n'est possible par ce chemin.

Le travail de conception se réduit à cinq blocs : (1) **quatre entités nouvelles** — `Notification` et `EnvoiNotification` (portant les quatre attributs exigés par le CDC §20), `ModeleNotification` et `RegleDiffusion` (référentiels administrables, patron `CategorieReclamation` de F006) — plus `PreferenceCanalUtilisateur` pour COM-05 ; (2) **deux traitements de fond** (distribution, reprise) suivant à l'identique le patron `RelancesAbsenceReponseService` de F001 ; (3) **une abstraction de canal** avec une implémentation journalisée, **sans aucun fournisseur nommé** ; (4) **deux résolveurs de destinataires nommés**, et deux seulement ; (5) le **câblage de la cloche déjà présente** dans `HeaderBar.razor`, plus le centre de notifications et deux écrans d'administration.

Le raccordement **MSG-04** exigé par la décision **Option B** ne coûte **aucune ligne de code métier** : `MentionnerCollaborateurCommand` publie déjà `CollaborateurMentionne` avec `collaborateurId` dans sa charge utile. Le raccordement se fait par **résolveur + règle de diffusion + modèle**. Symétriquement, le **point d'intégration ALE-03 existe déjà** (`SurveillerEcheancesUseCase` publie `SlaBientotDepasse`/`SlaDepasse`) et reste **délibérément non activé** : aucun résolveur superviseur, aucune règle semée, aucune notification de supervision émise.

Trois extensions additives touchent des features fermées, toutes justifiées et sans modification de règle métier : une colonne nullable sur `EvenementSocle` (curseur de distribution), la publication de `FactureEditee` dans les deux commandes d'édition de facture (**exigée par IMP-11**, qui tracent aujourd'hui sans publier), et le câblage de la cloche.

---

## Technical Context

**Language/Version** : C# 13 / .NET 10 (imposé, Constitution Principe I — décision de projet, pas une exigence CDC)

**Primary Dependencies** : ASP.NET Core Blazor Server (InteractiveServer), Entity Framework Core 10, ASP.NET Core Identity, xUnit, bUnit, Moq — **aucune nouvelle dépendance introduite par F009**. En particulier : **aucun client SMTP, aucun SDK de messagerie, aucun bus de messages, aucune bibliothèque de résilience** (research.md D10)

**Storage** : SQL Server (Docker en développement, existant) — **une migration additive requise** (data-model.md §7) : 5 tables créées, 1 colonne nullable ajoutée, aucune suppression, aucune donnée existante modifiée

**Testing** : xUnit (Domain/Application/Infrastructure), bUnit (Web, interactions UI réelles), tests d'intégration EF Core sur SQL Server réel — jamais InMemory pour les invariants de cloisonnement et d'unicité, conformément au patron établi F003/F006/F008

**Target Platform** : Application Web responsive, hébergée comme les modules existants — aucune contrainte de plateforme nouvelle

**Project Type** : Web application multi-couches existante (`CIT.Plateforme.Domain` / `.Application` / `.Infrastructure` / `.Web`) — F009 s'y insère, n'en crée pas de nouvelle

**Performance Goals** : **NON SPÉCIFIÉ** par le CDC pour COM/NOT. Le CDC ne fixe **aucune latence de notification** — PC-02 ne couvre que la supervision temps réel. L'intervalle de distribution est une **valeur technique configurable**, jamais une règle métier (Constitution Principe XIII : aucune valeur inventée)

**Constraints** : **Aucun fournisseur externe** (e-mail, SMS, WhatsApp) — Principe XI, `spec.md` §8, PC-16 ouvert · **Aucune valeur de reprise codée en dur** — CLAR-NOT-02 · **Aucun jeton d'accès dans un lien** — SEC-12 · **Aucun contenu de message journalisé** — Principe XIV · **Aucune suppression ni archivage de notification** — CLAR-NOT-03

**Scale/Scope** : **NON SPÉCIFIÉ** par le CDC (aucune volumétrie de notifications fournie — PC-01 ouvert). Dimensionnement aligné sur les modules existants, sans hypothèse chiffrée nouvelle. Les index de sélection (`DistribueLeUtc`, `(Statut, DerniereTentativeUtc)`) bornent le coût de scrutation sans préjuger d'un volume

---

## Constitution Check

*GATE : évalué avant Phase 0 et re-évalué après Phase 1 — voir §"Post-Design Constitution Check".*

| Principe | Évaluation | Justification |
|:---|:---:|:---|
| I. Stack Technique Imposée | ✅ PASS | Aucune technologie nouvelle ; C#/.NET/SQL Server/EF Core/Blazor strictement respectés |
| II. Source de Vérité et Traçabilité | ✅ PASS | Chaque contrat (`contracts/use-cases.md` C9-01→C9-11) cite son exigence CDC exacte. La conception **suit l'intention écrite par F001** dans `EvenementSocle` (research.md D1) plutôt que celle du preflight `docs/13`, hiérarchie de sources respectée |
| III. Priorisation Must/Should/Could | ✅ PASS | 6 Must couverts par C9-01→C9-08 ; 2 Should (COM-05, NOT-03) par C9-03/C9-09 ; 1 Could (COM-06) par C9-10, **explicitement conditionnel et non supprimé** |
| IV. Principes d'Architecture | ✅ PASS | Chaque abstraction est justifiée par un besoin démontré (research.md). L'alternative « dispatcher appelé depuis 21 commandes » est **écartée au nom de YAGNI**. Zéro logique métier dans Razor |
| V. Domaine Métier : Ticket et Capacités Transverses | ✅ PASS | Ticket/Dossier/Message/Audit réutilisés sans modification (data-model.md §1). **Aucun second journal de messages** (COM-04), **aucun second moteur d'actions requises** (ACR-01), **aucun second journal d'audit** |
| VI. Workflows Paramétrables | ✅ PASS | Déclencheurs, modèles et règles de diffusion **administrables sans développement** (C9-06, C9-07). Aucune règle de diffusion codée en dur ; `EstObligatoire` est une donnée, pas une constante |
| VII. Sécurité (NON-NÉGOCIABLE) | ✅ PASS | Filtre serveur par **égalité stricte** sur le destinataire (C9-02, C9-04) ; identité toujours dérivée du contexte authentifié ; SEC-12 sans jeton (research.md D12) ; anti-IDOR testé (`TEST-NOT-01` cas 6) ; MSG-02 préservé (C9-11) |
| VIII. Données et Persistance | ✅ PASS | Migration **strictement additive** ; **aucune suppression physique** de notification (research.md D13) |
| IX. Audit et Traçabilité Opérationnelle | ✅ PASS | Chaque contrat nomme son événement d'audit ; journal SOC-13 existant réutilisé (`spec.md` §16) |
| X. Gestion des Documents | ✅ PASS | Aucun document manipulé. **Aucun lien direct vers une pièce protégée** (SEC-12) |
| XI. Intégrations et Périmètre Autonome | ✅ PASS | **Aucun fournisseur nommé ni codé** ; une abstraction, une implémentation journalisée. L'absence de souscription **ne rend aucun Must inatteignable** (research.md D10, PC-16 préservé) |
| XII. Tests et Stratégie de Validation | ✅ PASS | **5 TEST-SPEC** de `spec.md` §18 bis — `TEST-NOT-01` (8 cas), `TEST-NOT-02` (8), `TEST-MSG-04` (8), `TEST-ERR-02` (6), `TEST-COM-06` (4, conditionnel) = **34 cas** — rattachés à leurs contrats ; 7 parcours de recette manuelle (`quickstart.md`) ; non-régression des 3 features touchées imposée avant commit |
| XIII. Performance et Exploitation | ✅ PASS | **Aucune valeur de latence, de volumétrie ni de reprise inventée** ; intervalles et paramètres en configuration |
| XIV. Observabilité | ✅ PASS | Émissions, échecs et actes d'administration tracés ; **le contenu intégral du message n'est jamais journalisé** (data-model.md §3.2) |
| XV. Gestion des Points Non Résolus | ✅ PASS | PC-16, PC-17, PC-25, PC-14 et CLAR-COM-01→04 / CLAR-NOT-02→05 **référencés, jamais comblés** (research.md, §"Points ouverts"). Le seed est borné aux règles **CDC-EXIGENCE** ; aucune proposition PC-17 n'est semée |
| XVI. Definition of Done | ✅ PASS (à vérifier en implémentation) | Grille applicable telle quelle ; **aucune exigence Must silencieusement ignorée** — c'est précisément ce qui impose la correction D7 |
| XVII. Gouvernance des Écarts | ✅ PASS | **Trois écarts additifs** sur features fermées (D2, D7, D14), chacun explicite, justifié, sans modification de règle métier, et soumis au rejeu de sa suite de régression |

**Aucune violation** — la section « Complexity Tracking » reste vide.

---

## Project Structure

### Documentation (this feature)

```text
specs/009-notifications-multicanales/
├── spec.md                     # Entrée (CLARIFIÉE — §25 Session 2026-09-01)
├── checklists/requirements.md  # READY FOR /speckit.plan (21/21)
├── plan.md                     # Ce fichier
├── research.md                 # Phase 0 — 14 décisions (D1→D14)
├── data-model.md               # Phase 1 — 5 entités nouvelles, 1 extension additive
├── contracts/
│   └── use-cases.md            # Phase 1 — 11 contrats (C9-01→C9-11)
├── quickstart.md               # Phase 1 — 7 parcours de recette manuelle
└── tasks.md                    # Phase 2 (/speckit.tasks — NON produit ici)
```

### Source Code (repository root, structure existante réutilisée telle quelle)

```text
src/
├── CIT.Plateforme.Domain/
│   ├── Notifications/                              # NOUVEAU dossier (mirroring Reclamations/)
│   │   ├── Notification.cs                         # NOUVELLE — destinataire, modèle, Lu/Non lu (CDC §20)
│   │   ├── EnvoiNotification.cs                    # NOUVELLE — canal, statut d'envoi (CDC §20, ERR-02)
│   │   ├── ModeleNotification.cs                   # NOUVELLE — COM-03 (patron CategorieReclamation)
│   │   ├── RegleDiffusion.cs                       # NOUVELLE — NOT-02, porte EstObligatoire
│   │   ├── PreferenceCanalUtilisateur.cs           # NOUVELLE — COM-05
│   │   ├── CanalNotification.cs                    # NOUVELLE — InApp | Email | Sms | WhatsApp
│   │   └── StatutEnvoi.cs                          # NOUVELLE — EnAttente | Envoye | Echec | NonDelivrable
│   ├── Audit/EvenementSocle.cs                     # ÉTENDUE — +DistribueLeUtc (nullable, D2)
│   └── Securite/NatureUtilisateur.cs               # ÉTENDUE — +Permission.AdministrationNotifications
│
├── CIT.Plateforme.Application/
│   ├── Abstractions/
│   │   ├── Repositories.cs                         # ÉTENDUE — 5 contrats de repository
│   │   ├── ICanalNotification.cs                   # NOUVELLE — abstraction d'envoi (D10)
│   │   └── IResolveurDestinataires.cs              # NOUVELLE — résolution de destinataires (D11)
│   └── Notifications/                              # NOUVEAU dossier applicatif
│       ├── DistribuerNotificationsUseCase.cs       # NOUVELLE (C9-01) — cœur du moteur
│       ├── ReessayerEnvoisNotificationUseCase.cs   # NOUVELLE (C9-08) — ERR-02
│       ├── ConsulterMesNotificationsQuery.cs       # NOUVELLE (C9-02)
│       ├── CompterNonLuesQuery.cs                  # NOUVELLE (C9-03)
│       ├── MarquerNotificationLueCommand.cs        # NOUVELLE (C9-04)
│       ├── AdministrerModelesNotificationCommand.cs# NOUVELLE (C9-06)
│       ├── AdministrerReglesDiffusionCommand.cs    # NOUVELLE (C9-07)
│       ├── GererPreferencesCanauxCommand.cs        # NOUVELLE (C9-09)
│       └── Resolveurs/
│           ├── ResolveurClientDuDossier.cs         # NOUVELLE (D11)
│           └── ResolveurCollaborateurMentionne.cs  # NOUVELLE (D11, C9-11 — raccordement MSG-04)
│
├── CIT.Plateforme.Application/BillingImport/
│   └── EditerFactureCommand.cs                     # ÉTENDUE — +1 ligne PublierEvenementAsync (D7, IMP-11)
├── CIT.Plateforme.Application/BillingExport/
│   └── EditerFactureExportCommand.cs               # ÉTENDUE — +1 ligne PublierEvenementAsync (D7)
│
├── CIT.Plateforme.Infrastructure/
│   ├── Persistence/
│   │   ├── PlateformeDbContext.cs                  # ÉTENDUE — 5 DbSet + configurations d'index
│   │   ├── Repositories.cs                         # ÉTENDUE — 5 repositories
│   │   ├── Migrations/…_AjoutNotifications.cs      # NOUVELLE — migration additive
│   │   └── DevelopmentSeeder.cs                    # ÉTENDUE — règles CDC-EXIGENCE uniquement
│   ├── Notifications/
│   │   └── CanalJournalise.cs                      # NOUVELLE — implémentation sans fournisseur (D10)
│   ├── TraitementsDeFond/
│   │   ├── DistributionNotificationsService.cs     # NOUVELLE (patron F001, D8)
│   │   └── ReessaiEnvoisNotificationService.cs     # NOUVELLE (patron F001, D8)
│   └── DependencyInjection.cs                      # ÉTENDUE — enregistrements F009
│
└── CIT.Plateforme.Web/
    ├── Components/Layout/HeaderBar.razor           # ÉTENDUE — câblage cloche + badge (D14, NOT-03)
    └── Components/Pages/Notifications/             # NOUVEAU dossier
        ├── CentreNotifications.razor               # NOT-01 — liste, état, lien
        ├── MesPreferencesNotification.razor        # COM-05
        ├── AdministrationModeles.razor             # COM-03
        └── AdministrationReglesDiffusion.razor     # NOT-02

tests/
├── CIT.Plateforme.Domain.Tests/Notifications/
│   ├── NotificationTests.cs                        # Lu/Non lu, idempotence, immutabilité du rendu
│   ├── ModeleNotificationTests.cs
│   └── RegleDiffusionTests.cs
├── CIT.Plateforme.Application.Tests/Notifications/
│   ├── DistribuerNotificationsUseCaseTests.cs      # TEST-NOT-02 1→4, idempotence, silence sans règle
│   ├── ReessayerEnvoisNotificationUseCaseTests.cs  # TEST-ERR-02 1→6
│   ├── ConsulterMesNotificationsQueryTests.cs      # TEST-NOT-01 1, 5, 6, 7
│   ├── MarquerNotificationLueCommandTests.cs       # TEST-NOT-01 2, 3, 6
│   ├── AdministrationNotificationsTests.cs         # TEST-NOT-02 5, 6
│   ├── PreferencesCanauxTests.cs                   # TEST-NOT-02 7
│   └── RaccordementMentionTests.cs                 # TEST-MSG-04 1→8
├── CIT.Plateforme.Infrastructure.Tests/Persistence/
│   └── NotificationRepositoryTests.cs              # SQL Server réel — index uniques, cloisonnement
└── CIT.Plateforme.Web.Tests/Pages/Notifications/
    ├── CentreNotificationsTests.cs                 # bUnit — liste, marquage, badge
    ├── AdministrationNotificationsTests.cs         # bUnit — règles et modèles
    └── RecSce14NotificationsE2ETests.cs            # bUnit — volets F009 de REC-SCE-14 uniquement
```

**Structure Decision** : F009 s'insère dans les 4 projets existants sans nouveau projet ni nouvelle couche. Les seuls regroupements nouveaux sont un dossier `Notifications/` par couche, mirroring exactement `Reclamations/` (F006) et `Enrolement/` (F008) — aucune structure inédite.

---

## Phases d'Implémentation Prévues (TEST-FIRST) — indicatif pour `/speckit.tasks`

*Rappel : ce plan ne produit pas `tasks.md`.*

1. **Preflight** : `dotnet build` + `dotnet test` au vert, `has-pending-model-changes` à 0 — baseline de non-régression F001→F008.
2. **Domaine** : test-first des 5 entités et 2 énumérations. Invariants prioritaires : idempotence de `MarquerCommeLue`, immutabilité de `DestinataireId`, **figement de `Titre`/`Corps`**, absence de toute méthode de suppression.
3. **Extension `EvenementSocle`** (D2) : `DistribueLeUtc` + migration additive. **Rejouer la suite Audit de F001.**
4. **Persistance** : `DbSet`, configurations EF, **index uniques** (idempotence de distribution, unicité des couples `(TypeEvenement, Canal)`), repositories — test-first sur SQL Server réel.
5. **Abstractions** : `ICanalNotification`, `IResolveurDestinataires` ; `CanalJournalise` (**aucun fournisseur**) ; les 2 résolveurs (D11). Test-first, dont le fan-out société par `DroitConsultation`.
6. **Moteur de distribution** (C9-01) : test-first `DistribuerNotificationsUseCase` — silence sans règle, idempotence, respect de `EstObligatoire` vs préférence, `NonDelivrable` si coordonnée absente, **jamais de notification client depuis une note interne**.
7. **Raccordement MSG-04** (C9-11) : test-first `TEST-MSG-04` 1→8. **Aucune modification de `MentionnerCollaborateurCommand`** — si une modification s'avérait nécessaire, c'est que la conception a dévié (research.md D5). **Rejouer la suite Messagerie de F001.**
8. **Consultation, marquage et accès au ticket** (C9-02→C9-05) : test-first, cloisonnement serveur **d'abord** (anti-IDOR), affichage ensuite. **C9-05** (SEC-12) : lien relatif porté par `TicketId`, **aucun jeton** ; cas « objet devenu inaccessible » traité sans divulgation.
9. **Reprise sur échec** (C9-08) : test-first `TEST-ERR-02` 1→6. **Aucune valeur en dur** : l'absence de configuration doit échouer explicitement au démarrage.
10. **Administration** (C9-06, C9-07) : test-first, autorisation serveur **d'abord**, puis non-rétroactivité et audit avant/après.
11. **Préférences** (C9-09) : test-first, dont l'invariant « In-App non débrayable » et « obligatoire prime ».
12. **Événements manquants** (D7) : ajouter `FactureEditee` aux deux commandes de facture. **Rejouer intégralement les suites BillingImport et BillingExport avant commit.**
13. **Traitements de fond** (D8) : les 2 `BackgroundService`, chacun avec sa méthode `TraiterUneFoisAsync` testable sans minuteur.
14. **UI** : bUnit d'abord (patron F006/F008 : rendu réel, `IServiceScope` dédié si interactions chaînées), puis Razor — câblage de la cloche, centre, préférences, 2 écrans d'administration, navigation.
15. **Sécurité** : cas négatifs du parcours 6 de `quickstart.md` — IDOR, inter-sociétés, admin sans accès, lien non authentifié, ticket devenu inaccessible.
16. **REC-SCE-14 (volets F009)** : test E2E bUnit **limité aux volets F009** (`spec.md` §18.1). **Ne pas prétendre rejouer le scénario complet** — les volets CON relèvent de F010.
17. **Non-régression finale** : `dotnet build --no-incremental`, suite complète, `has-pending-model-changes` à 0 après migration, rejeu manuel d'un parcours F003 et d'un parcours F006 pour confirmer l'absence de régression sur les commandes étendues.

---

## Post-Design Constitution Check

Re-évaluation après Phase 1 : **aucune nouvelle violation introduite par la conception détaillée**.

Trois constats renforcent la conformité au-delà du niveau attendu :

1. **Le raccordement MSG-04 exigé par l'Option B ne coûte aucune ligne de code métier** (research.md D5). La charge utile de `CollaborateurMentionne` porte déjà `collaborateurId` : le raccordement est **entièrement paramétrique**. C'est le meilleur résultat possible au regard des Principes IV et V — et cela garantit mécaniquement que MSG-05 et MSG-02 ne peuvent pas être cassés.
2. **Le point d'intégration ALE-03 existait déjà** (research.md D6). Le « contrat » demandé par la décision Option B ne demande donc **aucun développement** ; la retenue exigée — ne pas activer — est obtenue en **ne semant aucune règle** et en **n'écrivant aucun résolveur superviseur**. L'abstention est vérifiable par un test (`TEST-MSG-04` cas 8), pas seulement affirmée.
3. **La conception a révélé une exigence Must inatteignable** en l'état : IMP-11 (*« le client est notifié »*) n'avait aucun événement publié. La corriger (D7) est imposé par le Principe XVI — l'ignorer aurait constitué une omission silencieuse d'un Must.

Le seul point où F009 est **plus coûteux** que F008 est la migration de schéma (5 tables). Elle est inévitable : le CDC §20 exige explicitement de modéliser l'objet `Notification`, et aucune entité existante ne porte ces attributs. Elle reste **strictement additive**.

**Points ouverts préservés** : aucune décision de ce plan ne comble PC-16, PC-17, PC-25 ou PC-14, ni CLAR-COM-01→04 / CLAR-NOT-02→05. Le seed borné aux seules règles **CDC-EXIGENCE** est le mécanisme concret qui garantit cette retenue (data-model.md §7).

---

## Complexity Tracking

*Aucune violation du Constitution Check — section laissée vide conformément au gabarit.*

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |
