# Implementation Plan: Comptes clients, sociétés et utilisateurs rattachés

**Branch**: `002-comptes-clients-societes` | **Date**: 2026-08-29 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-comptes-clients-societes/spec.md` (11 exigences
FR-001→FR-011, 6 User Stories P1→P4, clarifications CLAR-01→03 validées le 29/08/2026)

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Fournir le référentiel des **comptes clients (sociétés)** et des **utilisateurs qui y sont
rattachés** : création et validation d'un compte société par CIT (CPT-01/02), authentification et
récupération de mot de passe (CPT-03), gestion multi-utilisateurs par un administrateur de société
(ENT-01/02), cloisonnement et visibilité propre/société (CPT-04, ENT-03), modification tracée du
type de client (CPT-06), authentification renforcée optionnelle (CPT-07). Ce référentiel sera
consommé, sans refonte, par les futures features métier (Billing Office, Simulation de Proforma,
Customer Complaint, Enrôlement Transporteurs).

Approche technique (`research.md`) : **extension de la solution .NET existante de Feature 001**
(aucune nouvelle solution, aucun microservice, aucune technologie ajoutée) — deux entités Domain
nouvelles (`SocieteCliente`, `UtilisateurSociete`), une extension strictement additive de `Dossier`
(nouveau champ nullable `AuteurId`), réutilisation intégrale d'ASP.NET Core Identity (D1), du moteur
d'habilitations `IAutorisationService`/`Permission` (D4, complété de nouveaux codes), de
`CloisonnementService` (D5, appelé tel quel), du journal d'audit `IJournalisationService` (D8) et de
la concurrence optimiste EF Core (D6/D7) de Feature 001. Les points ouverts PC-08 (justificatifs) et
PC-14 (conservation) restent des paramètres non fixés (Principes VI et XIII) ; NOUVEAU-03 (changement
de société de rattachement) reste hors périmètre (non implémenté).

## Technical Context

**Language/Version**: C# / .NET 10 (LTS) — ASP.NET Core 10, EF Core 10 (identique à Feature 001, D1
de `research.md` de Feature 001, non rediscuté ici — aucune raison de diverger)

**Primary Dependencies**: ASP.NET Core Identity (authentification, réutilisé tel quel — D1), SignalR
et Blazor Server (UI, réutilisés — aucun nouvel usage temps réel requis par cette feature), EF Core
10 (persistance) — **aucune dépendance ajoutée**

**Storage**: SQL Server 2019+ via EF Core 10, migration additive (2 nouvelles tables, 1 colonne
nullable ajoutée à `Dossier`) — aucune migration destructive (D2, D5, D11)

**Testing**: xUnit (unitaire + intégration), WebApplicationFactory (bout-en-bout, portion CPT-04 de
REC-SCE-14 — seul scénario CDC §22 applicable, voir `spec.md` § Testabilité), bUnit (composants
Blazor), tests de concurrence réels contre LocalDB pour l'invariant du dernier administrateur (D6/D7,
D12) — même structure à 4 projets que Feature 001, aucun nouveau projet de test

**Target Platform**: Application Web serveur (identique à Feature 001, même processus, aucun
déploiement séparé)

**Project Type**: Extension de la solution .NET en couches existante (Domain / Application /
Infrastructure / Web + 4 projets de tests) — pas de nouveau projet, pas de nouvelle solution

**Performance Goals**: NEEDS CLARIFICATION — volumétrie (nombre de sociétés/utilisateurs rattachés)
non définie par le CDC (même point ouvert PC-01 que Feature 001) ; la conception n'est pas bloquée :
pagination systématique sur les listes d'utilisateurs rattachés et de demandes filtrées (D5)

**Constraints**: 100 % stack .NET/SQL Server (aucune technologie ajoutée, D1→D12) ; aucune extension
de Feature 001 au-delà de celle strictement documentée en D11 ; aucune valeur métier figée dans le
code (PC-08, PC-14 — Principe VI) ; autorisation systématiquement vérifiée côté serveur (Principe
VII) ; historique jamais supprimé (Principe VIII) ; audit réutilisé, jamais dupliqué (Principe IX)

**Scale/Scope**: 11 exigences, 6 User Stories, 12 décisions de recherche D1→D12, 12 contrats
C2-01→C2-12, 2 entités nouvelles + 1 extension additive de Feature 001

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Vérification effectuée contre les 17 principes de la constitution (v1.0.0) :

| # | Principe | Statut | Justification (référence research.md) |
|---|---|---|---|
| I | Stack Technique Imposée | ✅ PASS | C#/.NET 10, ASP.NET Core, Blazor Server, EF Core, SQL Server, xUnit/bUnit — 100 % stack .NET déjà en place, aucune technologie ajoutée (D1→D12) |
| II | Source de Vérité et Traçabilité (NON-NÉGOCIABLE) | ✅ PASS | Le plan ne crée aucune règle métier ; les 11 exigences et CLAR-01/02/03 restent la référence ; chaque décision D1→D12 est ancrée sur une exigence CDC ou une clarification validée |
| III | Priorisation Must / Should / Could | ✅ PASS | Aucune priorité modifiée ; CPT-06 (Should) et CPT-07 (Could) conçus comme tels, jamais traités comme des Must déguisés (D9) |
| IV | Principes d'Architecture | ✅ PASS | Aucune dépendance tierce ajoutée ; aucune abstraction sans besoin identifié (entités Feature 002 séparées justifiées cas par cas en D2/D4/D6, alternatives rejetées documentées) |
| V | Domaine Métier : Ticket et Capacités Transverses | ✅ PASS | Aucune règle de ticket/dossier/workflow redéfinie ; l'extension de `Dossier` (D5/D11) est purement additive, la logique métier des comptes reste dans son propre module Domain `Comptes` |
| VI | Workflows Paramétrables par Conception | ✅ PASS | Informations/justificatifs de création (PC-08) et politique de conservation (PC-14) explicitement non fixés, laissés paramètres (data-model.md §6) |
| VII | Sécurité (NON-NÉGOCIABLE) | ✅ PASS | Cloisonnement inter-société réutilisé tel quel de Feature 001 (`CloisonnementService`, D5) ; droit de consultation par défaut restrictif (invariant §5.4 de data-model.md) ; toute action sensible vérifiée côté serveur (C2-01→C2-12) |
| VIII | Données et Persistance | ✅ PASS | SQL Server + EF Core, migration additive uniquement ; aucune suppression physique (désactivation = drapeau, D10) ; GUID comme identifiant technique (cohérent avec Feature 001) |
| IX | Audit et Traçabilité Opérationnelle | ✅ PASS | `IJournalisationService` de Feature 001 réutilisé à l'identique (D8) ; aucun second journal créé |
| X | Gestion des Documents | N/A | Cette feature ne traite aucun document (aucune FR ne le requiert — `spec.md` confirme ce périmètre) |
| XI | Intégrations et Périmètre Autonome | ✅ PASS | Aucune intégration externe introduite ; aucun service externe requis |
| XII | Tests et Stratégie de Validation | ✅ PASS | Chaque Must dispose d'une stratégie de test (TEST-SPEC dérivés des critères d'acceptation, `quickstart.md` §5.2) ; la portion CPT-04 de REC-SCE-14 est reproduite en bout-en-bout ; tests de concurrence réels pour l'invariant CLAR-03 (D12) |
| XIII | Performance et Exploitation | ✅ PASS | Aucune valeur métier non définie inventée ; volumétrie non fixée (PC-01, comme Feature 001), pagination systématique prévue (D5) |
| XIV | Observabilité | N/A à ce stade | Aucun nouveau traitement de fond introduit par cette feature ; l'audit existant (D8) suffit à l'observabilité fonctionnelle requise |
| XV | Gestion des Points Non Résolus (NON-NÉGOCIABLE) | ✅ PASS | PC-08, PC-14 explicitement non tranchés (paramètres) ; NOUVEAU-03 explicitement non implémenté (hors périmètre, `spec.md`) ; rien n'est présenté comme décidé à tort |
| XVI | Definition of Done | ✅ PASS | Pris en compte par la stratégie de tests et les tasks à venir (D12) |
| XVII | Gouvernance des Écarts d'Implémentation | ✅ PASS | La seule extension de Feature 001 (D11) est explicitement identifiée, justifiée et strictement additive — aucun écart silencieux |

**Re-check après Phase 1 (design)** : effectué le 29/08/2026 — `data-model.md`, `contracts/use-cases.md`
et `quickstart.md` ne dégradent aucun PASS ; aucun nouveau risque de conformité introduit ; le
Principe X reste N/A (confirmé : aucune entité Feature 002 ne porte de document).

## Project Structure

### Documentation (this feature)

```text
specs/002-comptes-clients-societes/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output — 12 décisions D1→D12 (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output — C2-01→C2-12 (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

**Extension de la solution .NET existante de Feature 001 — aucun nouveau projet, aucune nouvelle
solution.**

```text
cit-digital-platform/
├── src/
│   ├── CIT.Plateforme.Domain/
│   │   ├── Comptes/                      # NOUVEAU — SocieteCliente, UtilisateurSociete, TypeClient,
│   │   │                                 # EtatSociete, DroitConsultation, DernierAdministrateurActifException
│   │   ├── Tickets/Dossier.cs             # ÉTENDU (additif) — champ AuteurId (D5, D11)
│   │   └── Securite/                      # ÉTENDU (additif) — nouveaux codes Permission (D4) : compte.valider,
│   │                                       # compte.type_modifier, compte.gerer_collaborateurs
│   ├── CIT.Plateforme.Application/
│   │   └── Comptes/                      # NOUVEAU — cas d'usage C2-01→C2-12 (commandes/requêtes)
│   ├── CIT.Plateforme.Infrastructure/
│   │   └── Persistence/                  # ÉTENDU — SocieteClienteRepository, UtilisateurSocieteRepository,
│   │                                       # migration additive (2 tables + 1 colonne nullable sur Dossier)
│   └── CIT.Plateforme.Web/
│       └── Components/Pages/Comptes/     # NOUVEAU — pages Blazor Server (demande de compte, gestion
│                                          # des collaborateurs, profil société)
└── tests/
    ├── CIT.Plateforme.Domain.Tests/Comptes/           # NOUVEAU — invariants purs (dernier administrateur, etc.)
    ├── CIT.Plateforme.Application.Tests/Comptes/      # NOUVEAU — cas d'usage, autorisation, visibilité
    ├── CIT.Plateforme.Infrastructure.Tests/Comptes/   # NOUVEAU — EF Core réel, concurrence (LocalDB)
    └── CIT.Plateforme.Web.Tests/Comptes/              # NOUVEAU — portion CPT-04 de REC-SCE-14
```

**Structure Decision**: aucune nouvelle couche, aucun nouveau projet — Feature 002 ajoute un module
`Comptes` dans chacune des 4 couches déjà en place (Domain / Application / Infrastructure / Web) et
leurs 4 projets de tests correspondants, conformément à la promesse de `plan.md` de Feature 001
(« les futures features ajouteront des pages Blazor et des modules Domain/Application sans refonte
du socle »). L'unique extension du socle lui-même (`Dossier.AuteurId`) est strictement additive
(D11).

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

**Aucune violation constatée** — table volontairement vide.

Le plan privilégie systématiquement l'option la plus simple conforme : aucune dépendance tierce
ajoutée, aucun nouveau projet/solution, aucune abstraction sans besoin identifié (chaque entité
Feature 002 est justifiée par une exigence CDC précise ou une clarification validée — CLAR-01/02/03),
et une seule extension de Feature 001, strictement additive et documentée (D11).
