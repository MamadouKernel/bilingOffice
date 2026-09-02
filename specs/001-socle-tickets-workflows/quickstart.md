# Quickstart — Socle transverse de gestion des tickets et workflows

**Feature** : `001-socle-tickets-workflows` | **Date** : 2026-08-28 | **Statut** : Implémenté et
vérifié (`/speckit.implement`, 112/112 tâches, 77/77 tests passants — voir rapport final)

> **Cadre** : ce document décrit le parcours de mise en route et de validation du socle **tel
> qu'il fonctionne réellement** à l'issue de `/speckit.implement`. **Aucune valeur métier n'est
> préconfigurée** : le socle se paramètre, il ne s'invente pas (Principes VI et XIII).

## 1. Prérequis

- **SDK .NET 10** et **SQL Server 2019+** (LocalDB, instance locale ou instance DSI).
- Outil `dotnet-ef` (`dotnet tool install --global dotnet-ef`) pour les migrations versionnées.
- Un navigateur récent (Blazor Server — aucun prérequis côté client, 100 % .NET).
- Point de vigilance non résolu (T111 — CLARIFICATION REQUIRED) : l'environnement SQL de test pour
  la CI (conteneur dédié vs instance DSI partagée vs LocalDB) reste à trancher ; LocalDB a été
  utilisé pour la validation de cette session, ce qui n'engage pas ce choix.

## 2. Installation

```bash
dotnet restore
dotnet user-secrets set "ConnectionStrings:PlateformeDb" "<votre chaîne de connexion>" --project src/CIT.Plateforme.Web
dotnet ef database update --project src/CIT.Plateforme.Infrastructure --startup-project src/CIT.Plateforme.Web
dotnet run --project src/CIT.Plateforme.Web
```

1. Restaure les dépendances de la solution (`src/` : Domain, Application, Infrastructure, Web).
2. Renseigne la chaîne de connexion SQL Server via `user-secrets` (jamais dans le code — le
   fichier `appsettings.json` ne porte que la clé, valeur vide).
3. Applique les 7 migrations EF Core (schéma : tickets, dossiers, files, matrice des transitions,
   messages, documents, alertes, disponibilité, priorités, périmètres de supervision, deux
   journaux).
4. Lance l'application Web (Blazor Server + hub SignalR de supervision + 3 `BackgroundService` de
   fond démarrés avec le process : relances, surveillance SLA, distribution automatique).

## 3. Paramétrage initial obligatoire (avant tout usage métier)

À réaliser dans l'interface d'administration (contrats C-01 à C-07) :

| # | Paramétrage | Contrat | Point ouvert |
|---|---|---|---|
| 1 | Créer les files par module + types de tickets + stratégie d'affectation | C-01 | — |
| 2 | Importer/valider la **matrice des transitions** (destinations, rôles initiateurs, motifs) | C-02 | **PC-07 — atelier métier** |
| 3 | Créer rôles/habilitations et rattacher les agents aux files | C-03 | PC-03, PC-20 |
| 4 | Définir les périmètres de supervision | C-04 | PC-04 |
| 5 | Saisir cibles SLA, seuils, **horaires ouvrés, jours fériés** | C-05 | **PC-12 — à valider** |
| 6 | Saisir les règles de priorité (module, type, impact) | C-06 | PRI-04 |
| 7 | Relances d'absence de réponse (nombre/délai) | C-19/param. | CYC-04 |

> Sans ce paramétrage, le socle démarre mais **aucun comportement métier significatif** (SLA,
> transitions, affectation automatique, clôture pour absence de réponse) n'est actif — par conception.

## 4. Comptes de validation

Créer via Identity (D4) des comptes de test représentatifs : 1 client, 2 agents (files différentes),
1 superviseur (périmètre borné), 1 administrateur. Vérifier le cloisonnement client/agent (SEC-01).

## 5. Parcours de validation rapide (scénarios CDC §22 applicables à ce socle)

> **Correction de périmètre (voir `recipe-scope-audit.md`)** : le CDC §22 définit 14 scénarios de
> recette contractuels (`docs/04-matrice-recette.md`). **REC-SCE-01 à REC-SCE-08** portent sur des
> parcours métier complets — Billing Office IMPORT/EXPORT, Simulation de Proforma, Customer
> Complaint, Enrôlement Transporteurs — qui n'existent pas encore : ils appartiennent aux features
> qui consommeront ce socle et ne sont **pas** des scénarios de recette de la Feature 001, même
> lorsqu'ils exercent accessoirement une exigence du socle (ex. création de ticket) au passage.
> Seuls **REC-SCE-09 à REC-SCE-14** sont exécutables et pertinents dans le seul périmètre de ce
> socle ; ce sont les 6 scénarios ci-dessous, repris avec leur intitulé exact du CDC.

| Scénario CDC | Intitulé exact (`docs/04-matrice-recette.md`) | US |
|---|---|---|
| REC-SCE-09 | Prise en charge : un ticket arrive en file « attente de prise en charge », un opérateur l'accepte ; les horodatages sont enregistrés et exploités dans les indicateurs | US1 |
| REC-SCE-10 | Transfert de file : un opérateur transfère un ticket avec motif obligatoire ; conservation numéro/dossier/documents/commentaires/historique ; transfert vers destination non autorisée refusé | US2 |
| REC-SCE-11 | Réaffectation entre opérateurs (A → B) selon habilitations, tracée dans l'historique | US2 |
| REC-SCE-12 | Temps et SLA : mesure attente/traitement par étape, distinction temps interne/temps client (SLA suspendu en « attente client »), déclenchement des seuils 50 % / 80 % / dépassement / escalade | US3, US8 |
| REC-SCE-13 | Supervision : vue temps réel des files et de la charge, alertes, affectation d'un ticket non pris en charge, déblocage d'un ticket | US9, US10 |
| REC-SCE-14 (partiel) | Transverse : notifications, continuité digital/présentiel, regroupement en dossier, contrôle des habilitations, journal d'audit — **seule la portion socle** (dossier SOC-11, audit SOC-13, cloisonnement) relève de la Feature 001 ; les canaux/notifications (COM-*), la continuité (CON-*) et le cloisonnement client (CPT-04) relèvent d'autres features | US1, US6, US7, US11 |

*(Le détail contractuel intégral des 14 scénarios — y compris REC-SCE-01→08, hors périmètre ici —
se trouve dans `docs/04-matrice-recette.md` ; le mapping exigence → scénario → User Story figure
dans la « Matrice de couverture » de `spec.md`.)*

## 6. Tests automatisés

- `dotnet test` sur la solution : Domain (moteur temps/SLA, transitions, affectation), Application
  (cas d'usage, autorisation), Infrastructure (EF Core, numérotation, append-only), Web
  (WebApplicationFactory = **REC-SCE-09→14** en bout-en-bout — REC-SCE-01→08 hors périmètre ; bUnit
  pour les composants critiques). Les 24 exigences Must sans scénario CDC exécutable dans ce
  périmètre (voir `recipe-scope-audit.md`) reçoivent des tests complémentaires issus des critères
  d'acceptation de `spec.md`, non facultatifs pour autant.

## 7. Points de vigilance

1. **Aucune valeur SLA/seuil/volumétrie n'est livrée** : elles viennent du paramétrage (PC-01, PC-12).
2. La matrice des transitions doit être **validée en atelier métier** avant recette (PC-07).
3. Les canaux de notification externes sont **hors socle** (PC-25 — feature Notifications future) ;
   le socle fournit les événements persistés (D10).
4. La volumétrie PC-01 doit être cadrée avec la DSI avant les tests de charge.
