# Architecture cible .NET 10 — archivage du prompt & analyse d'écart vs code

> **Document** : `docs/109-cible-dotnet10-analyse-ecart.md`
> **Objet** : archiver la spécification d'architecture cible « YABA full .NET 10 / SQL Server 2022 »
> fournie en prompt, et la confronter au code **déjà livré**. Couvre : (1) rapport d'écart,
> (2) archivage de la cible, (3) **plan** de réconciliation du modèle de statut (non exécuté).
> **Statut** : analyse — **aucune ligne de code modifiée par ce document**.

---

## 0. Point de départ : YABA existe déjà

La plateforme est **déjà construite** sur cette stack. Ce document ne redéfinit rien : il **cartographie
la cible sur l'existant** et isole les rares écarts. Recréer les entités/DbContext/enum serait un
doublon destructeur — à proscrire.

---

## 1. Rapport d'écart — stack & architecture

| Exigence cible (prompt) | État réel | Écart |
| --- | --- | --- |
| `net10.0`, C# 14 | ✅ en place | — |
| Clean Architecture / DDD (Domain / Application / Infrastructure / Web) | ✅ 4 projets | — |
| Présentation : ASP.NET Core 10, **Blazor Web App interactive Server** | ✅ `@rendermode InteractiveServer` | Pas de WASM ni d'API REST publique (choix assumé ; `NFR-08` « API » reste une évolution) |
| EF Core 10 + SQL Server provider, Fluent API | ✅ `PlateformeDbContext`, `OnModelCreating`, migrations | — |
| SQL Server 2022 | ✅ | — |
| Identity + RBAC fin, **jusqu'au champ** (traitement parallèle) | ✅ habilitations + `CloisonnementService` + champs réservés IES | — |
| SLA à la minute via **BackgroundServices** | ✅ `SurveillanceSlaService`, `DistributionAutomatiqueService`, services de relance | Quartz/Hangfire **non** utilisés (BackgroundService natif — équivalent, plus simple) |
| Audit trail en base | ✅ `EntreeJournalAudit` append-only + `JournalAuditAppendOnlyGuard` | **Serilog** : à confirmer (l'audit métier passe par `IJournalisationService`, pas nécessairement Serilog) |
| Canaux **Portail + Email only** | ✅ `CanalNotification` = In-App + Email | — |
| **Autonome** (pas d'API IPAKI/IRIS/IES/Douane) | ✅ | — |

### Référentiels / énumérations

| Enum cible | Correspondance réelle | Écart |
| --- | --- | --- |
| `ClientProfile { Normal, EnCompte, Deposit, IES }` | ✅ `TypeClient` (mêmes valeurs) | nommage seulement |
| `ModuleCategory { BillingImport, BillingExport, CustomerComplaint, EnrolementGate, AvoirExoneration }` | ✅ modules présents (constantes `Module`) | AvoirExoneration traité comme workflow de ticket, pas forcément comme `ModuleCategory` dédié |
| `TicketPriority { Standard, Urgent, Critique }` | ✅ référentiel de priorité **paramétrable** (`NiveauPriorite`) | le code rend la priorité **administrable** (table), pas un enum figé — **plus conforme au CDC** |
| `RoleType { … 12 rôles }` | ⚠️ pas d'enum unique : rôles = **habilitations/permissions** par file | modèle plus fin (moindre privilège par file), pas un enum global |
| `TicketStatus { 14 valeurs fines }` | ❌ **divergence majeure** — voir § 3 | **arbitrage d'architecture** |

---

## 2. Archivage de la cible (extrait fourni)

La spécification cible fournie couvre : §1 stack .NET 10 & architecture, §2.A énumérations métier
(`RoleType`, `ClientProfile`, `TicketPriority`, `TicketStatus`, `ModuleCategory`). Les sections
ultérieures (entités détaillées, configuration Fluent API) n'ont **pas** été fournies dans le collage
et ne sont donc pas archivées ici. La contrainte fondatrice est confirmée : **Portail + Email
uniquement** et **fonctionnement autonome** (aucune API terminale).

> Les énumérations cibles sont reproduites telles quelles dans le prompt source ; seul `TicketStatus`
> appelle une décision (§3).

---

## 3. Réconciliation du modèle de statut — PLAN (non exécuté)

### 3.1 La divergence

- **Cible (prompt)** : `TicketStatus` à 14 valeurs fines codées en dur
  (`Cree, EnAttentePriseEnCharge, EnCoursTraitement, EnAttenteClient, ValideManifeste, CodifieExport,
  Facture, EnAttentePaiement, EnAttenteValidationPaiement, PayeConfirme, EnAttenteLettrage,
  EnAttenteIES, Rejete, Cloture, Annule`).
- **Réel** : `StatutTicket` = **5 valeurs de cycle de vie**
  (`EnAttenteDePriseEnCharge, EnCours, EnAttenteClient, Cloture, Annule`) **+** l'étape portée par la
  **file courante** (`FileCouranteId`) **+** des **libellés de statut paramétrables**
  (`LibelleStatutTicket`, CDC §11.4).

### 3.2 Pourquoi le modèle réel a été choisi

- Le CDC impose des **statuts/libellés administrables sans développement** (§11.4, principe
  « paramétrable sans développement »). Un enum figé de 14 valeurs **contredit** ce principe.
- L'étape métier (Manifeste, Facturation, Validation Paiement, Comptabilité, Contrôle Doc, File IES)
  est déjà **matérialisée par la file** — l'information « où en est le ticket » n'est pas perdue,
  elle est portée par `FileCouranteId` + libellé.
- Conséquence : les statuts « fins » du prompt (`ValideManifeste`, `Facture`, `PayeConfirme`,
  `EnAttenteLettrage`, `EnAttenteIES`…) existent **fonctionnellement** (file + libellé), mais pas
  comme valeurs d'enum.

### 3.3 Options

| Option | Description | Impact | Recommandation |
| --- | --- | --- | --- |
| **A — Statu quo** | Garder 5 valeurs + file + libellés paramétrables | nul | **Recommandée** (conforme CDC §11.4) |
| **B — Statut métier dérivé** | Ajouter une **projection lecture seule** `StatutMetier` calculée (file + état) pour l'affichage/API, sans changer le stockage | faible, additif | Bon compromis si on veut les libellés fins « à la cible » sans casser le paramétrable |
| **C — Enum figé 14 valeurs** | Remplacer le modèle par l'enum du prompt | **lourd** : domaine, EF, migration, toutes les commandes de workflow, tests ; **casse** le paramétrable (anti-CDC) | **Déconseillée** sauf décision explicite d'abandonner §11.4 |

### 3.4 Décision requise

Le refactor (option C) **n'est pas lancé** : il est lourd, va à l'encontre d'une exigence CDC, et ne
doit pas être exécuté sans un **arbitrage explicite** (DSI/métier) confirmant l'abandon des statuts
paramétrables. Si l'intention est seulement d'**exposer des libellés d'étape fins** (affichage, futur
export API), **l'option B** l'obtient sans régression.

---

## 4. Synthèse

- La stack cible du prompt **= l'implémentation actuelle** (≈ 100 % aligné sur l'infrastructure).
- Seul vrai arbitrage : le **modèle de statut** (§3) — recommandation **A** ou **B**, pas **C**.
- Rappels d'écarts déjà connus (hors scope de ce prompt) : canaux mobiles SMS/WhatsApp absents
  (fournisseur non arbitré) ; NFR infra (hébergement, sauvegardes, mesures de perf) relèvent de
  l'exploitation. Voir `docs/33` (matrice de conformité) et `docs/108` (swimlanes).
