# Quickstart — Comptes clients, sociétés et utilisateurs rattachés

**Feature** : `002-comptes-clients-societes` | **Date** : 2026-08-29 | **Statut** : Implémentée,
auditée et remédiée (`/speckit.implement` puis remédiation post-audit) — voir
`post-implementation-audit.md` et `closure-review.md` pour l'état final vérifié.

> **Cadre** : ce document décrit comment mettre en route et valider Feature 002 **une fois
> implémentée**, en réutilisant l'installation existante de Feature 001. **Aucune valeur métier
> n'est préconfigurée** : les informations/justificatifs (PC-08) et la politique de conservation
> (PC-14) restent des paramètres non fixés ici (Principes VI et XIII).

## 1. Prérequis

- Les prérequis de Feature 001 (SDK .NET 10, SQL Server 2019+/LocalDB, `dotnet-ef`) — aucun
  prérequis supplémentaire, aucune technologie ajoutée (`research.md`).
- Feature 001 déjà installée et migrée (`specs/001-socle-tickets-workflows/quickstart.md`) — Feature
  002 étend la même base, ne crée pas de nouvelle solution.

## 2. Installation / migration

```bash
dotnet ef database update --project src/CIT.Plateforme.Infrastructure --startup-project src/CIT.Plateforme.Web
dotnet run --project src/CIT.Plateforme.Web
```

Les migrations sont déjà créées (aucun `migrations add` à exécuter) — 4 migrations additives
composent Feature 002, appliquées avec les 8 migrations de Feature 001 (12 au total, voir
`post-implementation-audit.md` §9/`closure-review.md`) :

1. `AjoutAuteurIdDossier` — champ nullable `AuteurId` sur `Dossier` (CLAR-01).
2. `AjoutComptesSocietesUtilisateursRattaches` — tables `Societes`, `UtilisateursSociete`.
3. `AjoutDemandeurIdSocieteCliente` — champ `DemandeurId` sur `SocieteCliente` (premier administrateur).
4. `RemediationPostAuditFeature002` — champ nullable `TypeClient` sur `Dossier` (CPT-06) + contrainte
   `FK_UtilisateursSociete_Societes_SocieteId` (`ON DELETE RESTRICT`).

**Aucune migration destructive** : aucune table ni colonne existante de Feature 001 supprimée ou
renommée. Aucun nouveau service hébergé, aucun nouveau hub SignalR (`research.md` D1→D12 : aucune
technologie supplémentaire).

## 3. Paramétrage initial (avant tout usage métier)

| # | Paramétrage | Contrat | Point ouvert |
|---|---|---|---|
| 1 | Configurer les informations/justificatifs exigés à la création d'un compte, par type de client | C2-01 | **PC-08 — non fixé** |
| 2 | Configurer la politique de conservation d'un compte désactivé/refusé | — | **PC-14 — non fixé** |
| 3 | Créer au moins un compte « administrateur fonctionnel CIT » habilité (`compte.valider`, `compte.type_modifier`, `compte.gerer_collaborateurs`) | C2-02, C2-12 | — |

> Sans ce paramétrage, la création de compte reste possible mais le formulaire de demande
> n'affichera aucun champ obligatoire au-delà du strict minimum décrit par `data-model.md` — par
> conception, aucune information personnelle n'est inventée (`spec.md` § Données personnelles).

## 4. Comptes de validation

Créer, via Identity (réutilisé de Feature 001) : 2 sociétés clientes distinctes A et B (validées),
pour la société A — 3 utilisateurs rattachés (1 administrateur, 1 utilisateur droit « ses demandes
uniquement », 1 utilisateur droit « toutes les demandes de la société »), pour la société B — 1
utilisateur. Vérifier le cloisonnement inter-société (CPT-04) avant tout autre scénario.

## 5. Parcours de validation rapide

### 5.1 Scénario CDC applicable (REC-SCE-14, portion Feature 002 uniquement)

> Conformément à `spec.md` § Testabilité et à la méthode déjà appliquée par
> `specs/001-socle-tickets-workflows/recipe-scope-audit.md`, seule la portion de REC-SCE-14
> exerçant directement CPT-04 est applicable à cette feature — **jamais** son intégralité (canaux de
> notification, continuité digital/présentiel, dossier, files par rôle : hors périmètre Feature 002).

| Scénario CDC | Portion applicable | Contrat | US |
|---|---|---|---|
| REC-SCE-14 | « un client ne voit pas les tickets d'un autre » | C2-10, C2-11 | US4 |

**Étapes** : authentifier un utilisateur de la société A → tenter de consulter le profil ou les
demandes de la société B → vérifier un refus systématique côté serveur, quel que soit le point
d'entrée applicatif utilisé (SC-001 de `spec.md`).

### 5.2 TEST-SPEC — Must sans scénario CDC applicable (7 sur 8)

> Aucun REC-SCE n'existe pour ces exigences (nouvelles au sens du CDC §22 — `spec.md` § Testabilité).
> Chaque TEST-SPEC ci-dessous est dérivé **exclusivement** des critères d'acceptation déjà validés
> dans `spec.md`, jamais présenté comme un scénario officiel du CDC — à détailler lors de
> `/speckit.tasks`.

| Exigence | TEST-SPEC (dérivé de) | Contrat |
|---|---|---|
| CPT-01 | US1 scénarios 1-4 (soumission, validation, rejet, refus non habilité) | C2-01, C2-02 |
| CPT-02 | US1 scénarios 1-2 (déclaration et confirmation du profil) | C2-01, C2-02 |
| CPT-03 | US2 scénarios 1-4 (authentification, refus compte non validé/désactivé, récupération mot de passe, robustesse) | C2-03, C2-04 |
| CPT-05 | US3 scénario 1 (renvoi à ENT-01→04) | C2-06 |
| ENT-01 | US3 scénario 1 (rattachement) | C2-06 |
| ENT-02 | US3 scénarios 1-3, 6 (droits, désactivation, garde-fou dernier administrateur — **CLAR-03**) | C2-06, C2-07, C2-08, C2-09 |
| ENT-03 | US4 scénarios 2-3, 5 (visibilité propre/société/administrateur — **CLAR-01, CLAR-02**) | C2-11 |

**CPT-04** dispose d'une couverture directe partielle (§5.1 ci-dessus) et n'a donc pas besoin d'un
TEST-SPEC complémentaire pour sa portion cloisonnement inter-société ; un TEST-SPEC complémentaire
reste néanmoins nécessaire pour la portion « ses propres tickets » individuelle non couverte par
REC-SCE-14 (recoupe US4 scénarios 2-3 ci-dessus, ENT-03).

### 5.3 Scénarios de concurrence (invariant dernier administrateur — CLAR-03)

Deux transactions concurrentes tentent de désactiver/rétrograder simultanément les deux seuls
administrateurs actifs d'une société de test → vérifier qu'**une seule** réussit, l'autre reçoit un
refus explicite (`DernierAdministrateurActifException` ou conflit de concurrence, `research.md` D6),
et que la société conserve **toujours au moins un** administrateur actif à l'issue du scénario.

## 6. Ne jamais inventer

- Aucun REC-SCE au-delà de la portion CPT-04 de REC-SCE-14 (§5.1) — REC-SCE-01→08 restent hors
  périmètre (`spec.md` § Testabilité).
- Aucune valeur de PC-08 (justificatifs exacts) ni PC-14 (durée de conservation) n'est préconfigurée
  par ce guide — paramètres à renseigner par le métier avant recette/production.
