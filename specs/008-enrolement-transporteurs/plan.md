# Implementation Plan: Feature 008 — Enrôlement Transporteurs

**Branch**: `008-enrolement-transporteurs` | **Date**: 31 août 2026 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/008-enrolement-transporteurs/spec.md` (statut CLARIFIED), `checklists/requirements.md` (READY FOR /speckit.plan)

**Note**: Ce plan est produit exclusivement à partir de spec.md, checklists/requirements.md, docs/00-analyse-cdc.md, docs/01-matrice-exigences.md, docs/02-points-a-clarifier.md, docs/04-matrice-recette.md, `.specify/memory/constitution.md`, et du code réel F001-F006 (vérifié fichier par fichier, cf. research.md). Aucun code n'est produit par ce document ; `/speckit.tasks` n'est pas invoqué.

---

## Summary

F008 (Enrôlement Transporteurs) permet à un Transporteur/Chauffeur — une seule personne physique (CLAR-ENR-01, RÉSOLU) — de soumettre à distance un dossier d'enrôlement, à l'Agent Gate de l'instruire (vérification, complément, validation), puis de tracer la convocation et la remise physique du badge jusqu'à la clôture. L'approche technique retenue (research.md) est une **réutilisation maximale du socle F001/F002** : aucune nouvelle table, aucune nouvelle colonne, aucun nouvel état de cycle de vie. Le seul travail de conception consiste à (1) ajouter 2 permissions RBAC dédiées et 3 branches modulaires à des commandes génériques déjà éprouvées (patron F006/Customer Complaint), (2) créer 4 commandes Application dédiées et légères (soumission, validation, convocation, constatation de badge), (3) ajouter 1 méthode de Domain additive (réutilisation des colonnes `Objet`/`Description` déjà existantes) et 1 méthode de repository générique (`ListerParClientAsync`), et (4) construire l'UI Blazor correspondante en respectant les règles déjà établies (zéro GUID manuel, zéro DbContext/Repository dans Razor, permissions serveur).

---

## Technical Context

**Language/Version** : C# 13 / .NET 10 (imposé, Constitution Principe I — décision de projet, pas une exigence CDC)

**Primary Dependencies** : ASP.NET Core Blazor Server (InteractiveServer), Entity Framework Core 10, ASP.NET Core Identity, SignalR (notifications UI temps réel déjà utilisées par le socle), xUnit, bUnit, Moq — **aucune nouvelle dépendance introduite par F008**

**Storage** : SQL Server (Docker en développement, existant) — **aucune migration de schéma requise** (data-model.md §7) ; seed de données uniquement (File "Agent Gate", permissions, comptes de démonstration)

**Testing** : xUnit (Domain/Application/Infrastructure), bUnit (Web, interactions UI réelles), tests d'intégration EF Core sur SQL Server réel (jamais InMemory pour les invariants de concurrence/cloisonnement, cohérent avec le patron déjà établi F003/F006)

**Target Platform** : Application Web responsive, hébergée comme les modules existants (aucune contrainte de plateforme nouvelle)

**Project Type** : Web application multi-couches existante (`CIT.Plateforme.Domain` / `.Application` / `.Infrastructure` / `.Web`) — F008 s'insère dans cette structure, n'en crée pas de nouvelle

**Performance Goals** : NON SPÉCIFIÉ par le CDC pour ENR (Constitution Principe XIII — pas de valeur inventée) ; le SLA générique F001 reste disponible si des cibles sont fixées ultérieurement

**Constraints** : Aucune intégration externe (pas de Gate physique, pas de fabrication de badge, pas de SMS/WhatsApp — Constitution Principe XI, frontières spec.md §22) ; fonctionnement autonome strict

**Scale/Scope** : NON SPÉCIFIÉ par le CDC (pas de volumétrie ENR fournie) — architecture dimensionnée comme les modules existants, sans hypothèse chiffrée nouvelle

---

## Constitution Check

*GATE : évalué avant Phase 0 (recherche) et re-évalué après Phase 1 (conception) — voir §"Post-Design Constitution Check" en fin de document.*

| Principe | Évaluation | Justification |
|:---|:---:|:---|
| I. Stack Technique Imposée | ✅ PASS | Aucune nouvelle technologie ; C#/.NET/SQL Server/EF Core strictement respectés |
| II. Source de Vérité et Traçabilité | ✅ PASS | Chaque contrat (`contracts/use-cases.md`) référence son exigence ENR exacte et son étape REC-SCE-08 ; aucune exigence Must silencieusement ignorée (matrice de couverture spec.md §23 confirmée) |
| III. Priorisation Must/Should/Could | ✅ PASS | ENR-01/02/03 (Must) intégralement couverts par des contrats (C8-01→C8-10) ; ENR-04 (Should)/ENR-05 (Could) explicitement différés sans être supprimés (research.md D12) |
| IV. Principes d'Architecture | ✅ PASS | Aucune abstraction nouvelle sans besoin démontré (research.md justifie chaque décision par un examen du code réel avant d'introduire quoi que ce soit) ; logique métier dans Application/Domain, jamais dans Razor (§UI ci-dessous) |
| V. Domaine Métier : Ticket et Capacités Transverses | ✅ PASS | Ticket/Dossier/Documents/Messages/Files/Audit intégralement réutilisés (data-model.md §1) ; aucune duplication de règle métier |
| VI. Workflows Paramétrables | ✅ PASS | Pièces/informations exigées restent configurables (`ReglesDepotDocument`, réutilisé sans modification) ; aucune liste figée en dur (PC-08 respecté) |
| VII. Sécurité (NON-NÉGOCIABLE) | ✅ PASS | Toute permission vérifiée côté serveur (`IAutorisationService.ExigerAsync`) ; identité toujours dérivée du contexte authentifié (research.md D11) ; cloisonnement générique réutilisé (`CloisonnementService`) |
| VIII. Données et Persistance | ✅ PASS | EF Core, SQL Server ; aucune opération destructrice (clôture = horodatage, jamais suppression) |
| IX. Audit et Traçabilité Opérationnelle | ✅ PASS | Chaque contrat précise son événement d'historique/audit nommé (C8-01→C8-08) |
| X. Gestion des Documents | ✅ PASS | Mécanisme F001 réutilisé sans extension (antivirus, contrôle de type/taille déjà en place) |
| XI. Intégrations et Périmètre Autonome | ✅ PASS | Aucune intégration Gate/badge/SMS/WhatsApp implémentée ; événements exposés pour consommation future uniquement (NFR-08, architecture ouverte respectée sans anticipation) |
| XII. Tests et Stratégie de Validation | ✅ PASS | Chaque contrat a un test prévu (unitaire + intégration EF réelle) ; REC-SCE-08 rejoué intégralement en quickstart.md |
| XIII. Performance et Exploitation | ✅ PASS | Aucune valeur de SLA/volumétrie inventée ; configurable si besoin futur |
| XIV. Observabilité | ✅ PASS | Historique + audit systématiques ; aucune donnée sensible journalisée en clair (aucune pièce d'identité en clair dans les logs, seule la référence de stockage) |
| XV. Gestion des Points Non Résolus | ✅ PASS | CLAR-ENR-07 (expiration badge) explicitement non traité (research.md D12), pas résolu arbitrairement ; PC-08/PC-14/PC-16 référencés et non contournés |
| XVI. Definition of Done | ✅ PASS (à vérifier en implémentation) | Grille applicable telle quelle lors de `/speckit.tasks`/`/speckit.implement` |
| XVII. Gouvernance des Écarts | ✅ PASS | Aucun écart au CDC introduit ; les 2 permissions nouvelles et 4 commandes dédiées sont des choix techniques justifiés (research.md), jamais des redéfinitions de règle métier |

**Aucune violation** — la section "Complexity Tracking" en fin de document reste vide.

---

## Project Structure

### Documentation (this feature)

```text
specs/008-enrolement-transporteurs/
├── spec.md                # Entrée (CLARIFIED)
├── checklists/requirements.md
├── plan.md                # Ce fichier
├── research.md            # Phase 0 — 12 décisions (D1-D12)
├── data-model.md          # Phase 1 — entités réutilisées, 1 extension additive
├── contracts/
│   └── use-cases.md       # Phase 1 — 10 contrats (C8-01→C8-10)
├── quickstart.md          # Phase 1 — 5 parcours de recette manuelle
└── tasks.md               # Phase 2 (/speckit.tasks — NON produit ici)
```

### Source Code (repository root, structure existante réutilisée telle quelle)

```text
src/
├── CIT.Plateforme.Domain/
│   └── Tickets/
│       └── Ticket.cs                          # +1 méthode additive : DefinirInformationsEnrolement (aucune nouvelle propriété)
│
├── CIT.Plateforme.Application/
│   ├── Abstractions/
│   │   └── Repositories.cs                    # IDossierRepository += ListerParClientAsync
│   ├── Enrolement/                             # NOUVEAU dossier applicatif (mirroring CustomerComplaint/)
│   │   ├── SoumettreEnrolementCommand.cs       # NOUVELLE (C8-01)
│   │   ├── ValiderEnrolementCommand.cs         # NOUVELLE (C8-05)
│   │   ├── DeclencherConvocationCommand.cs     # NOUVELLE (C8-07)
│   │   ├── ConstaterRemiseBadgeCommand.cs      # NOUVELLE (C8-08)
│   │   └── ListerMesEnrolementsQuery.cs        # NOUVELLE (C8-09)
│   ├── Tickets/
│   │   ├── PrendreEnChargeCommand.cs           # ÉTENDUE (+1 branche, C8-02)
│   │   ├── AttenteClientCommand.cs             # ÉTENDUE (+1 branche, C8-04)
│   │   └── ConsulterFileQuery.cs               # ÉTENDUE (+1 test de permission, C8-10)
│   └── Messagerie/
│       └── PublierMessageCommand.cs            # ÉTENDUE (+1 branche ×2 méthodes, C8-03)
│
├── CIT.Plateforme.Domain/Securite/
│   └── NatureUtilisateur.cs                    # Permission += EnrolementSoumettre, EnrolementInstruire
│
├── CIT.Plateforme.Infrastructure/
│   ├── Persistence/
│   │   ├── Repositories.cs                     # DossierRepository += ListerParClientAsync (implémentation EF)
│   │   └── DevelopmentSeeder.cs                # += File "Agent Gate", comptes/habilitations de démonstration
│   └── DependencyInjection.cs                  # += enregistrement des 5 nouvelles commandes/queries Enrolement
│
└── CIT.Plateforme.Web/
    └── Components/Pages/Enrolement/            # NOUVEAU dossier (mirroring CustomerComplaint/)
        ├── MesEnrolements.razor                # Transporteur/Chauffeur — liste
        ├── NouvelEnrolement.razor              # Transporteur/Chauffeur — soumission
        ├── FicheEnrolement.razor                # Détail (les deux acteurs, vue adaptée par permission)
        └── FileAgentGate.razor                 # Agent Gate — file d'instruction

tests/
├── CIT.Plateforme.Domain.Tests/Tickets/
│   └── TicketEnrolementTests.cs                 # DefinirInformationsEnrolement (nominal, erreurs)
├── CIT.Plateforme.Application.Tests/Enrolement/
│   ├── SoumettreEnrolementCommandTests.cs
│   ├── ValiderEnrolementCommandTests.cs
│   ├── DeclencherConvocationCommandTests.cs
│   ├── ConstaterRemiseBadgeCommandTests.cs
│   └── ListerMesEnrolementsQueryTests.cs
├── CIT.Plateforme.Application.Tests/Tickets/
│   └── (extension des suites existantes PrendreEnChargeCommandTests, AttenteClientCommandTests,
│         PublierMessageCommandTests, ConsulterFileQueryTests — cas Enrolement ajoutés, pas de nouveau fichier)
├── CIT.Plateforme.Infrastructure.Tests/Persistence/
│   └── DossierRepositoryTests.cs                # extension : ListerParClientAsync
└── CIT.Plateforme.Web.Tests/Pages/Enrolement/
    ├── NouvelEnrolementTests.cs                  # bUnit, formulaire + dépôt de pièce
    ├── FileAgentGateTests.cs                     # bUnit, prise en charge → validation → convocation → badge
    └── RecSce08EnrolementE2ETests.cs             # bUnit, rejeu intégral REC-SCE-08 (patron RecSce07CustomerComplaintE2ETests)
```

**Structure Decision** : F008 s'insère dans les 4 projets existants (`Domain`/`Application`/`Infrastructure`/`Web`) sans nouveau projet ni nouvelle couche. Le seul nouveau regroupement est un dossier `Enrolement/` par couche (Application, Web), mirroring exactement `CustomerComplaint/` (F006) — aucune structure inédite.

---

## Phases d'Implémentation Prévues (TEST-FIRST) — indicatif pour `/speckit.tasks`

*Rappel : ce plan ne produit pas `tasks.md`. Les phases ci-dessous cadrent l'ordre logique attendu, à charge pour `/speckit.tasks` de les détailler en tâches numérotées.*

1. **Preflight** : vérifier build/tests F001-F006 au vert avant toute modification (baseline de non-régression).
2. **Domaine** : test-first `Ticket.DefinirInformationsEnrolement` (Domain.Tests) → implémentation.
3. **Sécurité/RBAC** : ajout des constantes `Permission.EnrolementSoumettre`/`EnrolementInstruire` (aucun test dédié requis — constantes pures) ; extension `IDossierRepository`/`DossierRepository` (`ListerParClientAsync`) test-first (Infrastructure.Tests, SQL Server réel).
4. **Persistance/Seed** : `FileDeTraitement` "Agent Gate", comptes de démonstration (`DevelopmentSeeder`).
5. **Soumission** (C8-01) : test-first `SoumettreEnrolementCommand` (unitaire + intégration EF).
6. **Instruction Agent Gate** (C8-02, C8-03, C8-10) : test-first des branches ajoutées à `PrendreEnChargeCommand`/`PublierMessageCommand`/`ConsulterFileQuery` — **exécuter aussi la suite de régression existante de ces 3 fichiers avant de committer** (protection F001/F003/F004/F006, cf. §Non-Régression spec.md).
7. **Compléments** (C8-04) : test-first extension `AttenteClientCommand` + suite de régression existante.
8. **Validation** (C8-05) : test-first `ValiderEnrolementCommand`.
9. **Convocation** (C8-07) : test-first `DeclencherConvocationCommand`.
10. **Remise badge / Clôture** (C8-08) : test-first `ConstaterRemiseBadgeCommand`.
11. **UI** : bUnit d'abord (patron F006 : rendu réel + interactions `.Input()`/`.Click()`, jamais de mock DbContext partagé — cf. le gotcha de scope EF documenté lors de la clôture F006) puis Razor (`MesEnrolements`, `NouvelEnrolement`, `FicheEnrolement`, `FileAgentGate`), navigation (`NavMenu.razor` — nouveaux liens Espace Client/Agent).
12. **REC-SCE-08** : test E2E bUnit rejouant intégralement le scénario (patron `RecSce07CustomerComplaintE2ETests`, avec `IServiceScope` dédié pour la portion UI si plusieurs interactions chaînées sur le même rendu, cf. gotcha F006).
13. **Hardening** : cas négatifs de sécurité (Parcours 5 quickstart.md) — IDOR, permissions manquantes, tentative de rejet (doit être absente de l'UI et impossible via les commandes exposées).
14. **Non-régression finale** : `dotnet build`, suite complète `dotnet test`, `dotnet ef migrations has-pending-model-changes` (doit rester à 0 — aucune migration F008), rejeu manuel d'un parcours F006 pour confirmer l'absence de régression sur les commandes étendues.

---

## Post-Design Constitution Check

Re-évaluation après Phase 1 (data-model.md, contracts/use-cases.md, quickstart.md) : **aucune nouvelle violation introduite par la conception détaillée**. Le constat le plus significatif de cette phase — zéro migration de schéma — renforce le respect du Principe IV (YAGNI) et du Principe V (réutilisation transverse) au-delà du niveau déjà atteint par F003/F004/F006. Les 2 permissions et 4 commandes nouvelles sont proportionnées à ce que le CDC décrit (ENR-01→03) et n'anticipent aucun besoin non exprimé.

---

## Complexity Tracking

*Aucune violation du Constitution Check — section laissée vide conformément au gabarit.*

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |
