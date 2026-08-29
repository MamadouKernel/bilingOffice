# Implementation Plan: Socle transverse de gestion des tickets et workflows

**Branch**: `001-socle-tickets-workflows` | **Date**: 2026-08-28 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-socle-tickets-workflows/spec.md` (74 exigences FR-001→FR-074, 11 User Stories P1→P6, clarifications CLAR-01→05 validées le 28/08/2026)

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Fournir le socle transverse de gestion des tickets et workflows de la plateforme digitale CIT : cycle de vie des tickets et dossiers (SOC, CYC), messagerie et documents (MSG, SOC-12), files, transferts, affectation paramétrable (WKF, TRF, AFF), suivi des temps et SLA (TPS, SLA), alertes (ALE), supervision temps réel (SUP, DIS), priorités, audit et sécurité (PRI, SEC, MAS). Ce socle sera consommé sans refonte par les futures applications (portail client, console interne, module statistiques).

Approche technique (research.md) : application Web **100 % C#/.NET et SQL Server** (directive du maître d'ouvrage du 28/08/2026, alignée sur le Principe I de la constitution) — ASP.NET Core 10 + **Blazor Server + SignalR** (aucun framework JavaScript), **EF Core 10 sur SQL Server**, **BackgroundService** pour SLA/alertes/affectation, architecture en couches (Domain / Application / Infrastructure / Web). Aucune valeur métier ouverte (PC-01, PC-02, PC-04, PC-07, PC-12, PC-20, PC-25) n'est figée dans le code : ce sont des paramètres administrables (Principes VI et XIII).

## Technical Context

**Language/Version**: C# / .NET 10 (LTS) — ASP.NET Core 10, EF Core 10 [D1, avec réserve DSI documentée]

**Primary Dependencies**: ASP.NET Core Identity (authentification), SignalR (temps réel), Blazor Server (UI), EF Core 10 (persistance), Bootstrap (UI responsive) — aucune dépendance hors stack .NET

**Storage**: SQL Server 2019+ via EF Core 10, migrations versionnées [D3]

**Testing**: xUnit (unitaire + intégration), WebApplicationFactory (bout-en-bout, scénarios REC-SCE-09→14 — seuls scénarios CDC §22 exécutables dans le périmètre du socle ; REC-SCE-01→08 relèvent de features métier futures), bUnit (composants Blazor) [D12]

**Target Platform**: Application Web serveur (Kestrel/IIS selon exploitation DSI), navigateurs récents desktop/tablette

**Project Type**: Web application mono-solution .NET en couches (Domain / Application / Infrastructure / Web + 4 projets de tests)

**Performance Goals**: NEEDS CLARIFICATION — volumétrie PC-01 volontairement non inventée (paramètre DSI avant recette/production) ; la conception n'est pas bloquée : pagination systématique, requêtes paginées [D3/D5]

**Constraints**: 100 % stack .NET/SQL Server (directive 28/08/2026) ; aucune valeur métier figée dans le code (Principe VI) ; historique fonctionnel et journal d'audit append-only (SOC-09, Principe IX) ; horodatages UTC ; autorisation systématique vérifiée côté serveur (Principe VII) ; fonctionnement autonome sans service externe (Principe XI)

**Scale/Scope**: Socle transverse : 74 exigences, 11 User Stories, 12 décisions de recherche D1→D12

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Vérification effectuée contre les 17 principes de la constitution (v1.0.0) :

| # | Principe | Statut | Justification (référence research.md) |
|---|---|---|---|
| I | Stack Technique Imposée | ✅ PASS | C#/.NET 10, ASP.NET Core, Blazor Server, EF Core, SQL Server, xUnit — 100 % stack .NET, renforcé par la directive du 28/08/2026 (D1–D3, D12) |
| II | Source de Vérité et Traçabilité (NON-NÉGOCIABLE) | ✅ PASS | Le plan ne crée aucune règle métier ; les 74 exigences du socle restent la référence ; décisions ancrées sur les références CDC (D1–D12) |
| III | Priorisation Must / Should / Could | ✅ PASS | Aucune priorité modifiée ; AFF-03..06 (Should) implémentées comme stratégies du moteur d'affectation, pas comme des Must déguisées (D11) |
| IV | Principes d'Architecture | ✅ PASS | Architecture simple et modulaire ; aucune abstraction sans besoin identifié ; aucune dépendance tierce superflue (pas de broker, pas de scheduler) — alternatives rejetées documentées (D5, D9–D11) |
| V | Domaine Métier : Ticket et Capacités Transverses | ✅ PASS | Règles de temps/SLA, matrice des transitions et stratégies d'affectation dans le Domain pur, pas en base ni en UI (D7, D11) |
| VI | Workflows Paramétrables par Conception | ✅ PASS | Matrice des transitions, stratégies d'affectation par file, habilitations, périmètres = données administrables (D4, D11) ; dérogations WKF-02/03 traçables via audit (D8) |
| VII | Sécurité (NON-NÉGOCIABLE) | ✅ PASS | Identity + policies/permissions vérifiées exclusivement côté serveur ; périmètres bornés configurables (D4) ; documents via flux autorisé avec antivirus configurable (D9) |
| VIII | Données et Persistance | ✅ PASS | SQL Server + EF Core, migrations versionnées ; intégrité transactionnelle ; aucune suppression d'historique (D3, D6) |
| IX | Audit et Traçabilité Opérationnelle | ✅ PASS | Deux stores distincts : historique fonctionnel vs journal d'audit append-only ; auteur/horodatage systématiques (D8) |
| X | Gestion des Documents | ✅ PASS | Finalité documentaire propre derrière port ; aucun lien de contournement d'autorisation ; conservation = paramètre PC-14 non inventé (D9) |
| XI | Intégrations et Périmètre Autonome | ✅ PASS | Aucune intégration IPAKI/IRIS/SydamWorld/IES ; aucun service externe requis : BackgroundService in-process + événements persistés (D5, D10) |
| XII | Tests et Stratégie de Validation | ✅ PASS | Chaque Must couvert par une stratégie de validation ; les 6 scénarios REC-SCE-09→14 applicables au périmètre du socle sont reproduits en bout-en-bout (REC-SCE-01→08 relèvent des futures features Billing Office/Simulation/Customer Complaint/Enrôlement) ; scénarios complémentaires pour les 24 Must sans scénario exécutable dans ce périmètre (D12, voir `recipe-scope-audit.md`) |
| XIII | Performance et Exploitation | ✅ PASS | Aucune valeur métier non définie inventée (SLA, volumétrie, seuils) : toutes restent des paramètres ; pagination systématique ; PC-01 à cadrer par la DSI (D3, D5, D7) |
| XIV | Observabilité | ✅ PASS | Les traitements de fond (SLA, affectation, alertes) produiront des informations exploitables (journal d'audit, événements persistés) — détaillé dans tasks (D5, D10) |
| XV | Gestion des Points Non Résolus (NON-NÉGOCIABLE) | ✅ PASS | PC-01, PC-02, PC-04, PC-07, PC-12, PC-20, PC-25 explicitement marqués paramètres/hors socle, avec hypothèses documentées ; rien n'est présenté comme décidé à tort |
| XVI | Definition of Done | ✅ PASS | Les 10 critères DoD sont pris en compte par la stratégie de tests et les tasks à venir (D12) |
| XVII | Gouvernance des Écarts d'Implémentation | ✅ PASS | Aucun écart à la constitution ni au CDC introduit par ce plan |

**Re-check après Phase 1 (design)** : effectué le 28/08/2026 — aucun PASS dégradé ; aucun nouveau
risque de conformité introduit par data-model.md, contracts/ et quickstart.md.

## Project Structure

### Documentation (this feature)

```text
specs/001-socle-tickets-workflows/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output — 12 décisions D1→D12 (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
cit-digital-platform/
├── src/
│   ├── CIT.Plateforme.Domain/            # Entités métier, règles pures, ports — zéro dépendance framework
│   │   ├── Tickets/                      # Ticket, Dossier, cycle de vie (SOC, CYC)
│   │   ├── Files/                        # File, rattachements, matrice des transitions (WKF, TRF)
│   │   ├── Affectation/                  # Stratégies d'affectation (AFF-01..07) et priorités (PRI)
│   │   ├── SuiviTemps/                   # Moteur temps/SLA pur : calendrier ouvré, échéances, seuils (TPS, SLA, ALE)
│   │   ├── Supervision/                  # Lectures de supervision et disponibilité (SUP, DIS)
│   │   ├── Documents/                    # Port de stockage documents (SOC-12, SEC-07/12)
│   │   └── Securite/                     # Permissions, habilitations, périmètres (SEC, MAS)
│   ├── CIT.Plateforme.Application/       # Cas d'usage (commandes/requêtes), orchestration des workflows
│   ├── CIT.Plateforme.Infrastructure/    # EF Core (DbContext, migrations), Identity, deux journaux, stockage documents, événements persistés
│   └── CIT.Plateforme.Web/               # Blazor Server + hubs SignalR + BackgroundService (SLA, affectation, alertes)
└── tests/
    ├── CIT.Plateforme.Domain.Tests/           # xUnit — moteurs purs (temps/SLA, transitions, affectation)
    ├── CIT.Plateforme.Application.Tests/      # xUnit — cas d'usage, autorisation, workflows
    ├── CIT.Plateforme.Infrastructure.Tests/   # xUnit — EF Core, numérotation, append-only
    └── CIT.Plateforme.Web.Tests/              # bUnit + WebApplicationFactory — REC-SCE-09→14 (01→08 hors périmètre)
```

**Structure Decision**: Architecture .NET en couches (style Clean Architecture), une seule solution :
- **Domain** : cœur métier pur et unitairement testable (moteur temps/SLA D7, matrice des transitions,
  stratégies d'affectation D11) — aucune dépendance framework (Principe IV).
- **Application** : cas d'usage et orchestration ; vérifie les permissions via abstractions (D4).
- **Infrastructure** : EF Core/SQL Server (D3), Identity (D4), historique + audit distincts (D8),
  documents derrière port (D9), journal d'événements persisté (D10), numérotation par séquences (D6).
- **Web** : Blazor Server + SignalR (D2) et services hébergés (D5).
Les futures features (portail client, console interne, statistiques) ajouteront des pages Blazor et des
modules Domain/Application sans refonte du socle (Principe III).

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

**Aucune violation constatée** — table volontairement vide.

Le plan privilégie systématiquement l'option la plus simple conforme : aucune dépendance tierce
(aucun broker, aucun scheduler externe, aucun framework JavaScript), aucune abstraction sans besoin
identifié, aucune structure de projet au-delà des 4 couches + 4 projets de tests (D1→D12).
