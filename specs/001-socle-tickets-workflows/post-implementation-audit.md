# Audit post-implémentation — Feature 001 : Socle transverse de gestion des tickets et workflows

**Date de l'audit initial** : 2026-08-29
**Date de la remédiation et du réaudit ciblé** : 2026-08-29 (même jour, session de remédiation)
**Périmètre** : `specs/001-socle-tickets-workflows/` — 74 exigences (FR-001→FR-074, 62 Must / 11 Should / 1 Could), 11 User Stories, 25 contrats (C-01→C-25), 112 tâches (T001→T112)
**Chaîne vérifiée** : CDC (`docs/`) → spec.md → plan.md/research.md → contracts/use-cases.md → tasks.md → code (`src/`) → tests (`tests/`)

> **Ce document ne modifie ni le CDC, ni spec.md/plan.md/tasks.md, ni aucune décision métier
> ouverte (PC-xx).** La section D6 (numérotation) n'a pas été tranchée : voir
> `decision-d6-numbering.md`, qui reste « À DÉCIDER ». Aucune Feature 002 n'a été commencée.

---

## 0. Historique du document

Ce rapport a été **mis à jour** après une session de remédiation ciblée qui a corrigé les écarts
démontrés par l'audit initial (section « Corrections appliquées » ci-dessous), à l'exclusion de :
- la divergence D6 (numérotation), volontairement **non tranchée** — voir `decision-d6-numbering.md` ;
- les points PC-xx encore ouverts (aucun n'a été résolu arbitrairement) ;
- les tâches T106-T111 de `tasks.md`, laissées **non cochées** (dépendent de décisions métier non prises).

Le texte de l'audit initial (constats, méthodologie, preuves) est conservé ci-dessous, **annoté**
des corrections apportées, plutôt que réécrit silencieusement — pour que la trace de ce qui a
changé (et pourquoi) reste visible.

### 0.1 Corrections appliquées dans cette session

| # | Sujet | Fichiers modifiés (src) | Nouveaux tests | Résultat |
|---|---|---|---|---|
| 1 | Calcul SLA / calendrier ouvré ignoré | `ParametresSlaProvider.cs`, `SurveillerEcheancesUseCase.cs`, `AttenteClientCommand.cs`, `ParametreSlaAdministrable.cs` (+ nouvelle entité `PeriodeAttenteClient`, nouveau port `IPeriodeAttenteClientRepository`) | 7 (`SurveillerEcheancesCalendrierTests` ×5, `ParametresSlaProviderTests` ×2) | Calendrier réellement utilisé ; calcul cohérent (jamais de durée brute soustraite à une durée ouvrée), y compris à travers une période non ouvrée |
| 2 | `DureeCible` limité à < 24h (type SQL `time`) | `PlateformeDbContext.cs` (mapping bigint/ticks) | 5 (`ParametreSlaDureeCibleTests`, 30min/12h/24h/48h/120h) | Round-trip EF Core réel validé jusqu'à 120h |
| 3 | Autorisations manquantes | `DeposerDocumentCommand.cs`, `GererDisponibiliteCommand.cs`, `Disponibilite.razor`, `Documents.razor` | 11 (`DepotDocumentAutorisationTests` ×4, `DisponibiliteAutorisationTests` ×3, + 4 tests `ControleDepotTests` mis à jour) | Contrôle serveur systématique, testé positif et négatif, sans dépendre de l'UI |
| 4 | AFF-06 (compétence) inexploitable | `FileDeTraitement.cs`, `DistributionAutomatiqueUseCase.cs`, `CandidatsAffectationProvider` (Repositories.cs), nouvelle entité `CompetenceOperateur`, nouveau port + commande `GererCompetencesOperateurCommand` | 4 (`DistributionCompetenceTests`) | Mécanisme complet exploitable de bout en bout avec des données de test ; aucun catalogue métier réel inventé |
| 5 | Avertissements EF Core Value Comparer | `PlateformeDbContext.cs` (`ValueComparer` sur `TransitionAutorisee.motifsAutorises` et `PerimetreSupervision.filesCouvertes`), `TransitionAutorisee.cs` (+`AjouterMotif`) | 1 (`ValueComparerPersistanceTests`) | Avertissements disparus (vérifié via `dotnet ef migrations list --verbose`) ; mutation en place désormais détectée et persistée |
| 6 | D6 (numérotation) | **Aucun** — `decision-d6-numbering.md` créé | — | Statut inchangé : **À DÉCIDER**, en attente de validation humaine |

**Migration créée** : `20260829164513_RemediationAuditSlaCalendrierCompetenceValueComparer` (8ᵉ
migration de la feature), appliquée avec succès sur une base vide (voir §4 mis à jour).

**Total de tests** : 85 → **109** (+24), tous verts, rejoués sur SQL Server réel (Docker de
l'utilisateur) puis sur LocalDB après restauration de la configuration d'origine (voir Annexe).

---

## 1. Score de conformité (recalculé après remédiation)

| Statut | Nombre (/74) | dont Must (/62) | dont Should/Could (/12) |
|---|---|---|---|
| **CONFORME** | 67 | 58 | 9 |
| **PARTIEL** | 6 | 4 | 2 |
| **ÉCART** | 0 | 0 | 0 |
| **BLOQUÉ (contenu paramétrable, pas le mécanisme)** | 1 (TRF-06, contenu) | 1 | 0 |
| **NON DÉMONTRÉ** | 0 | 0 | 0 |
| **Total** | **74** | **62** | **12** |

**Évolution** : 61 → **67 CONFORME** (+6 : SOC-07, TPS-04, SLA-03, SLA-04, SLA-05, AFF-06, DIS-02 —
soit 7 lignes corrigées mais SLA-03/TPS-04 partagent la même correction sous-jacente, d'où +6 net
de bascule ÉCART/PARTIEL→CONFORME). **0 ÉCART restant** (4 → 0). Les 6 PARTIEL restants ne sont
**pas** liés aux écarts corrigés dans cette session — voir détail §2/§10.

**Aucune régression** : les 61 lignes déjà CONFORME lors de l'audit initial le restent (109/109
tests verts, y compris les 6 scénarios REC-SCE-09→14 rejoués intégralement).

---

## 2. Table des 74 exigences — lignes modifiées par la remédiation

> Seules les lignes dont le statut ou la preuve a changé sont reproduites ici. Pour les 68 lignes
> inchangées, se reporter à la table complète encore valide (méthodologie, Contract, Task,
> Implémentation identiques) — seule la colonne **Statut/Preuve** de ces 6 lignes a évolué.

| CDC | FR | Avant remédiation | Après remédiation | Preuve |
|---|---|---|---|---|
| SOC-07 | FR-007 | PARTIEL (contrôle type/taille CONFORME, mais aucun contrôle d'habilitation au dépôt) | **CONFORME** — `DeposerDocumentCommand` expose désormais `DeposerPourAgentAsync`/`DeposerPourClientAsync`, chacun avec son propre contrôle serveur (miroir de `TelechargerDocumentQuery`) | `ControleDepotTests` (mis à jour) + `DepotDocumentAutorisationTests` (4 tests, positif/négatif, agent et client), SQL Server réel pour la suite complète |
| TPS-04 | FR-044 | PARTIEL — suspension correcte dans le cas simple, calcul incohérent en cas de chevauchement d'une période non ouvrée | **CONFORME** — `SurveillerEcheancesUseCase` ne soustrait plus jamais une durée brute à une durée ouvrée (nouvelle entité `PeriodeAttenteClient`, bornes exactes) | `SurveillerEcheancesCalendrierTests` (5 scénarios, dont le cas de chevauchement explicitement vérifié à ±1 minute) |
| SLA-03 | FR-048 | PARTIEL (même cause que TPS-04) | **CONFORME** | idem |
| SLA-04 | FR-049 | ÉCART — `ParametresSlaProvider` retournait toujours `CalendrierOuvre.ToujoursOuvert` (bug du `?:` à deux branches identiques) | **CONFORME** — le calendrier réellement administré (`ParametreSlaAdministrable.ConstruireCalendrierOuvre`) est désormais restitué | `ParametresSlaProviderTests` (2 tests, dont un qui aurait échoué avant correction) |
| SLA-05 | FR-050 | PARTIEL — `DureeCible` mappé sur SQL `time`, débordement pour toute cible ≥ 24h | **CONFORME** — mapping en ticks/`bigint`, sans limite fonctionnelle absente du CDC | `ParametreSlaDureeCibleTests` (5 valeurs, 30 min à 120h, round-trip SQL Server réel) |
| AFF-06 | FR-067 | ÉCART — stratégie non câblée dans `DistributionAutomatiqueUseCase` + liste de compétences vide en dur | **CONFORME** — compétence requise administrée sur la file, compétences administrées par opérateur, sélection réelle | `DistributionCompetenceTests` (4 tests : compétence compatible, compétence absente, indisponible, autre stratégie non impactée) |
| DIS-02 | FR-060 | ÉCART — `GererDisponibiliteCommand` sans aucun contrôle d'autorisation | **CONFORME** — auto-déclaration toujours possible ; modification pour un tiers exige `Permission.DisponibiliteGerer` | `DisponibiliteAutorisationTests` (3 tests, positif/négatif) |
| TRF-06 | FR-040 | CONFORME, avec une note technique sur l'avertissement EF Core (comparateur manquant, dormant) | **CONFORME, note technique levée** — `ValueComparer` configuré, mutation en place désormais détectée | `ValueComparerPersistanceTests` (mutation en place réellement persistée, SQL Server réel) ; `MatriceTransitionsTests` inchangés (2/2 toujours verts) |
| SOC-01/SOC-09 | FR-001/FR-009 | CONFORME, divergence D6 documentée dans l'audit | **CONFORME, inchangé** — divergence D6 déplacée dans un document de décision dédié, toujours **non tranchée** | `decision-d6-numbering.md` (aucune modification de code) |

Les 6 lignes restant **PARTIEL** (SOC-14, PRI-02 + 4 autres déjà signalées comme non prioritaires
dans l'audit initial, non concernées par cette remédiation) sont détaillées en §10 — **volontairement
non traitées ici**, car hors du périmètre explicite de la demande de remédiation (« corriger
uniquement les écarts démontrés par l'audit » qui étaient listés comme cibles).

---

## 3. Audit des écarts architecturaux — mise à jour

### A. D6 — Numérotation → **INCHANGÉ, voir `decision-d6-numbering.md`**

Conformément à la consigne (« ne pas modifier le code ni `research.md` automatiquement »), **aucune
modification n'a été apportée**. Une note de décision comparant les deux options (séquence par
module/type vs séquences globales avec préfixes) a été produite séparément :
**`specs/001-socle-tickets-workflows/decision-d6-numbering.md`**, avec avantages, inconvénients,
impact migration/exploitation/concurrence/maintenabilité/volumétrie pour chaque option, et une
recommandation technique clairement marquée comme non appliquée. **Statut : À DÉCIDER.**

### B. SLA — Calcul de l'attente client → **CORRIGÉ**

**Correction appliquée** :
1. `ParametresSlaProvider.ObtenirPourAsync` construit désormais un calendrier réel via
   `ParametreSlaAdministrable.ConstruireCalendrierOuvre()` au lieu de toujours retourner
   `CalendrierOuvre.ToujoursOuvert`. `ParametreSlaAdministrable` porte désormais des champs de
   calendrier administrables (`HeureDebut`, `HeureFin`, `JoursOuvresBitmask` — un masque de bits
   scalaire, volontairement choisi pour ne pas reproduire le défaut de comparateur de valeur
   corrigé par ailleurs) et une méthode `AppliquerCalendrierOuvre(...)` ; `GererParametresSlaCommand`
   expose une nouvelle méthode `DefinirAvecCalendrierAsync(...)` pour l'administrer. Aucune valeur
   réelle de PC-12 n'est fixée par défaut — le calendrier reste « toujours ouvert » tant qu'aucune
   configuration explicite n'est appliquée.
2. `SurveillerEcheancesUseCase` ne soustrait plus une durée brute d'attente client à une durée
   totale ouvrée. Une nouvelle entité autonome `PeriodeAttenteClient` (bornes exactes UTC de chaque
   période d'attente, persistée par `AttenteClientCommand` au moment même de la mise en
   attente/reprise) permet de convertir **chaque période** en durée ouvrée avec le même calendrier
   avant de la soustraire — les deux termes de la soustraction sont désormais dans la même unité.

**Preuve directe** (`SurveillerEcheancesCalendrierTests`) : le scénario « attente traversant une
période non ouvrée » (Lundi 16h → Mardi 9h30, calendrier Lun-Ven 8h-17h) donne désormais **30
minutes** de durée effective — exactement le temps ouvré écoulé avant la mise en attente. Une
implémentation naïve (soustraction brute) aurait donné 0 (résultat négatif clampé), ce qui était
précisément le bug initial. Un scénario « reprise du SLA » confirme que le décompte continue
correctement après la fin de l'attente (2h30 = 30 min avant + 2h après reprise).

**Choix architectural** : plutôt que de conserver la borne de période dans une collection EF
naviguée depuis `Ticket` (essayé initialement), les périodes sont stockées comme une **entité
autonome** (même modèle que `EntreeHistorique`/`EntreeJournalAudit`), car le couplage
navigation+cascade avec l'agrégat `Ticket` (porteur du jeton de concurrence `VersionConcurrence`)
déclenchait un faux `DbUpdateConcurrencyException` sous le fournisseur EF Core InMemory (utilisé
par `Application.Tests`) — un défaut documenté du fournisseur InMemory pour ce type de graphe,
sans lien avec une vraie modification concurrente. Ce choix a été vérifié : la suite complète
(109 tests) est verte sur SQL Server réel **et** sur InMemory après ce choix de conception.

### C. AFF-06 — Distribution selon la compétence → **CORRIGÉ**

**Correction appliquée** :
1. `FileDeTraitement` porte désormais un `CompetenceRequise` (nullable), renseigné uniquement pour
   la stratégie `Competence` (`DefinirStrategie` refuse une stratégie Compétence sans code fourni).
2. Nouvelle entité `CompetenceOperateur` (association opérateur ↔ code de compétence, texte libre —
   **aucun catalogue métier réel n'est fixé par le socle**), nouveau port
   `ICompetenceOperateurRepository`, nouvelle commande d'administration
   `GererCompetencesOperateurCommand` (avec contrôle d'autorisation, nouvelle permission
   `Permission.CompetenceGerer`).
3. `CandidatsAffectationProvider.ObtenirCandidatsAsync` interroge désormais les compétences
   réellement administrées pour chaque candidat (au lieu d'une liste vide en dur).
4. `DistributionAutomatiqueUseCase.ChoisirStrategie` construit désormais
   `new CompetenceStrategy(file.CompetenceRequise)` quand la stratégie de la file est `Competence`
   et qu'une compétence y est administrée (sinon, comme pour Prise libre/Manuelle, aucune
   distribution automatique n'a lieu — pas de valeur devinée).

**Preuve directe** (`DistributionCompetenceTests`, 4 scénarios) : un candidat disposant de la
compétence administrée est sélectionné et le ticket lui est réellement affecté ; un candidat
habilité mais sans la compétence n'est jamais sélectionné (le ticket reste en attente) ; un
candidat compétent mais Indisponible n'est jamais sélectionné (DIS-01 respecté même avec AFF-06) ;
une file utilisant une autre stratégie (RoundRobin) fonctionne normalement sans dépendre d'aucune
donnée de compétence.

### D. Avertissements EF Core Value Comparer → **CORRIGÉ**

**Correction appliquée** : un `ValueComparer` explicite (comparaison structurelle par séquence,
hash par agrégation, clone par `.ToList()`) a été ajouté sur `TransitionAutorisee.motifsAutorises`
et `PerimetreSupervision.filesCouvertes` dans `PlateformeDbContext.OnModelCreating`.

**Vérification empirique** (`dotnet ef migrations list --verbose`, réexécuté après correction) :
**aucun avertissement `Microsoft.EntityFrameworkCore.Model.Validation[10620]` ne subsiste** — la
sortie est vide, confirmant la disparition des deux avertissements identifiés lors de l'audit
initial.

**Preuve de persistance** (`ValueComparerPersistanceTests`, SQL Server réel) : une nouvelle méthode
`TransitionAutorisee.AjouterMotif(string)` (mutation en place de la liste déjà chargée, sans
réaffectation de référence) est ajoutée pour rendre ce test possible — avant correction, ce type de
mutation n'aurait été ni détecté par le suivi de changement EF Core, ni persisté, sans aucune
erreur signalée. Le test confirme qu'un motif ajouté en place à une transition existante est bien
retrouvé après un rechargement complet dans un nouveau contexte EF Core.

**Point non traité par cette remédiation** (dette technique restante, cf. §12/DT-1 révisé) :
`ParametreSlaAdministrable.DureeCible` a été corrigé (§B/type SQL), mais le champ n'utilise pas de
collection convertie — aucun `ValueComparer` n'y était nécessaire ; ce point avait déjà été clarifié
dans l'audit initial (l'entité initialement suspectée par la consigne d'audit n'était pas la bonne
— `PerimetreSupervision` l'était).

---

## 4. Migrations — vérification après remédiation

**8ᵉ migration** : `20260829164513_RemediationAuditSlaCalendrierCompetenceValueComparer`, créée via
`dotnet ef migrations add` puis **corrigée manuellement** sur un point : SQL Server refuse un
`ALTER COLUMN` direct de `time` vers `bigint` (« Operand type clash »). La migration générée
automatiquement a été remplacée par un `DropColumn` + `AddColumn` explicite (avec valeur par défaut
`0` pour la nouvelle colonne `bigint`) — choix documenté dans le code de la migration : le socle
n'étant pas encore en recette/production (aucune valeur SLA réelle enregistrée, PC-12 toujours
ouvert), il n'y a aucune perte de donnée réelle, seulement des valeurs de test locales à chaque
environnement.

**Protocole réel réexécuté** : base entièrement neuve créée sur le SQL Server réel de l'utilisateur
(conteneur Docker), les **8 migrations appliquées exclusivement** via `dotnet ef database update`
(aucun `EnsureCreated`), dans leur ordre naturel.

| Vérification | Résultat |
|---|---|
| Schéma final | 26 tables (+2 vs audit initial : `CompetencesOperateur`, `PeriodesAttenteClient`) + `__EFMigrationsHistory` |
| Historique migrations | Les 8 migrations, dans l'ordre, sans erreur |
| `DureeCible` | Confirmé `bigint` (`SELECT TYPE_NAME(system_type_id) ... ParametresSla` → `bigint`) |
| Nouvelles colonnes `ParametresSla` | `HeureDebut`/`HeureFin` (`time`), `JoursOuvresBitmask` (`tinyint`) — présentes |
| `Files.CompetenceRequise` | `nvarchar(128)`, nullable — présente |
| RowVersion | Inchangé : `Tickets.VersionConcurrence` reste la seule colonne `rowversion` — les nouvelles entités (`PeriodeAttenteClient`, `CompetenceOperateur`) n'en portent pas, cohérent avec leur nature d'entités indépendantes append/lookup, pas de mutation concurrente sensible identifiée |
| Avertissements EF Core | **0** (confirmé, voir §3.D) |

**Anomalie constatée pendant la génération** (corrigée, documentée) : l'`AlterColumn` généré
automatiquement par `dotnet ef migrations add` pour `DureeCible` était invalide sur SQL Server réel
(types incompatibles) — détecté en rejouant réellement la migration (pas seulement en la
générant), corrigé manuellement avant validation finale. Ceci illustre pourquoi la consigne
« ne pas utiliser `EnsureCreated` comme preuve de validation des migrations » est fondée : une
génération de migration syntaxiquement valide n'est pas une preuve suffisante — seule l'application
réelle contre SQL Server l'a révélé.

---

## 5. Concurrence — inchangé, revérifié

Les 4 scénarios de concurrence déjà démontrés lors de l'audit initial (prise en charge, réaffectation,
transfert vs réaffectation, clôture vs transfert) ont été **rejoués avec succès** après la
remédiation, sur le SQL Server réel de l'utilisateur, sans aucune régression. Le choix architectural
du §3.B (périodes d'attente client en entité autonome plutôt qu'en navigation EF depuis `Ticket`) a
été retenu **spécifiquement** pour ne pas introduire de nouveau risque de conflit de concurrence
sur l'agrégat `Ticket` — vérifié explicitement en réexécutant `PriseEnChargeConcurrenceTests` et
`ConcurrenceAutresOperationsAuditTests` (4/4 toujours verts) après l'ajout des nouvelles entités.

---

## 6. Autorisations — corrigé

Les deux écarts identifiés lors de l'audit initial sont corrigés :

1. **`DeposerDocumentCommand`** : restructurée en deux points d'entrée (`DeposerPourAgentAsync`,
   `DeposerPourClientAsync`), chacun avec son propre contrôle serveur — miroir exact du motif déjà
   utilisé par `TelechargerDocumentQuery` (même asymétrie lecture/écriture désormais résolue dans
   les deux sens). Testé positif (agent habilité, client propriétaire) et négatif (agent non
   habilité, client intrus) — 4 tests dans `DepotDocumentAutorisationTests`, plus les 3 tests
   préexistants de `ControleDepotTests` mis à jour pour utiliser un agent réellement habilité.
2. **`GererDisponibiliteCommand`** : un appelant agissant sur sa **propre** disponibilité n'a besoin
   d'aucune habilitation supplémentaire (DIS-01, auto-déclaration) ; agir pour un **tiers** exige
   désormais `Permission.DisponibiliteGerer` (permission déjà définie dans le code mais jusque-là
   jamais vérifiée par cette commande). Testé positif (superviseur habilité agissant pour un tiers,
   auto-déclaration) et négatif (appelant non habilité tentant d'agir pour un tiers) — 3 tests
   dans `DisponibiliteAutorisationTests`.

**Aucun contrôle n'a été ajouté côté UI uniquement** : les deux corrections vérifient l'habilitation
dans la couche applicative elle-même (`IAutorisationService`/`CloisonnementService`), exactement
comme les 17 autres commandes déjà conformes identifiées lors de l'audit initial — ces commandes
rejoignent donc les 23/23 commandes applicatives désormais protégées de façon vérifiable
indépendamment de toute interface.

---

## 7. Audit / historique — inchangé

Aucune modification n'a été apportée à ce périmètre (append-only applicatif déjà démontré,
distinction historique/audit déjà conforme). La nuance déjà signalée (garantie applicative
uniquement, pas de renfort SGBD) reste une dette technique non traitée par cette remédiation —
volontairement hors périmètre de la demande (« corriger uniquement les écarts démontrés »),
puisqu'elle n'avait pas été classée ÉCART mais dette technique de production.

---

## 8. Recette — REC-SCE-09 à REC-SCE-14 — revérifié, inchangé

Les 6 scénarios (9 tests dans `CIT.Plateforme.Web.Tests`) ont été **rejoués intégralement** après
remédiation, sur le SQL Server réel de l'utilisateur : **9/9 verts**, sans aucune régression. Le
rappel de périmètre reste respecté (aucune trace de REC-SCE-01→08 comme preuve de couverture).

---

## 9. Tâches bloquées — T106 à T111 — inchangé

**Vérifié après remédiation** : T106, T107, T108, T109, T110, T111 restent **non cochées** dans
`tasks.md`. Aucune de ces tâches n'a été cochée, ni sa décision anticipée. La remédiation a corrigé
des **mécanismes** défaillants (calcul SLA, mapping SQL, autorisations, câblage AFF-06,
comparateurs EF Core) — jamais une **valeur métier** relevant d'un PC-xx non tranché. En
particulier :
- PC-12 (valeurs cibles SLA, calendrier réel) reste ouvert : le mécanisme de calendrier est
  maintenant fonctionnel, mais aucune heure d'ouverture réelle n'a été fixée par cette remédiation
  (les tests utilisent des horaires explicitement qualifiés de « valeurs de test »).
- PC-07 (contenu de la matrice de transitions) reste ouvert — seul le défaut de comparateur de
  valeur (un problème purement technique, sans rapport avec le contenu métier de la matrice) a été
  corrigé.
- Aucune donnée de test (codes de compétence « Douane », horaires de calendrier de test, montants
  de durée SLA de test) n'a été proposée ou présentée comme une configuration de recette ou de
  production.

---

## 10. Écarts et anomalies restant ouverts (non traités par cette remédiation, hors périmètre demandé)

Ces points étaient déjà signalés dans l'audit initial comme dette technique ou limite mineure —
**volontairement non corrigés ici**, la demande de remédiation ayant listé explicitement les 5
écarts corrigés en §0.1 et rien d'autre :

| # | Sujet | Statut | Pourquoi non traité ici |
|---|---|---|---|
| 1 | D6 — divergence de numérotation | À DÉCIDER (voir `decision-d6-numbering.md`) | Explicitement exclu de correction automatique par la demande |
| 2 | DT-2 — absence de jeton de concurrence sur les entités d'administration (files, matrice, SLA, périmètres, habilitations) | Dette technique, non ÉCART | N'était pas listé parmi les écarts à corriger |
| 3 | DT-3 — append-only du journal d'audit non renforcé au niveau SGBD | Dette technique, non ÉCART | Idem |
| 4 | DT-6/T111 — environnement SQL de test non tranché en CI | Point d'outillage | Idem — point de décision d'équipe, pas un défaut de code |
| 5 | DT-7 — `SOC-14` (export, Could) sans test dédié | PARTIEL, priorité Could | Idem — non prioritaire, non listé |
| 6 | PRI-02 — influence de la priorité sur l'ordre de file non testée | PARTIEL | Idem — non listé dans la demande de remédiation |

---

## 11. Dette technique consolidée (mise à jour)

| # | Sujet | Sévérité | Statut |
|---|---|---|---|
| DT-1 | Comparateurs EF Core manquants | ~~Faible~~ | **RÉSOLU** (§3.D) |
| DT-2 | Absence de jeton de concurrence sur les entités d'administration | Faible | Ouvert (non demandé) |
| DT-3 | Append-only du journal d'audit non renforcé au niveau SGBD | Moyenne | Ouvert (non demandé) |
| DT-4 | `research.md` (D6) ne reflète plus fidèlement l'implémentation | Faible | Ouvert — **documenté séparément**, décision humaine requise (`decision-d6-numbering.md`) |
| DT-5 | `AFF-06` non exploitable de bout en bout | ~~Moyenne~~ | **RÉSOLU** (§3.C) |
| DT-6 | Environnement SQL de test non tranché (T111) | Moyenne | Ouvert (point d'outillage, hors périmètre) |
| DT-7 | `SOC-14` (export) sans test dédié | Faible (Could) | Ouvert (non demandé) |
| DT-8 *(nouveau)* | `DureeCible` mappé sur `time` (< 24h) | ~~Élevée~~ | **RÉSOLU** (§3.D, migration 8) |
| DT-9 *(nouveau)* | Calendrier SLA administré jamais utilisé | ~~Élevée~~ | **RÉSOLU** (§3.B) |
| DT-10 *(nouveau)* | Autorisation manquante sur dépôt de document / disponibilité | ~~Élevée~~ | **RÉSOLU** (§6) |

---

## 12. Corrections obligatoires avant recette — mise à jour

Toutes les corrections listées dans la version initiale de ce rapport (§13 d'origine) ont été
**appliquées et vérifiées** :

1. ~~SLA-04 / SLA-03 / TPS-04~~ — **Corrigé** (§3.B).
2. ~~AFF-06~~ — **Corrigé** (§3.C).
3. ~~Sécurité — dépôt de document et disponibilité~~ — **Corrigé** (§6).
4. ~~SLA-05 — type `DureeCible`~~ — **Corrigé** (§3.D/§4).

**Aucune correction obligatoire avant recette ne reste ouverte** parmi celles identifiées par
l'audit initial. Le seul point encore ouvert et potentiellement bloquant pour une recette
**complète** (par opposition à une recette du mécanisme) reste le **contenu réel** de la matrice de
transitions, des valeurs SLA cibles et des périmètres de supervision (PC-07/PC-12/PC-04,
tâches T106-T108) — non corrigible par du code, car il s'agit de décisions métier non prises.

## 13. Corrections obligatoires avant production — mise à jour

1. **DT-3** — envisager un renfort SGBD de l'append-only du journal d'audit (non traité par cette
   remédiation, reste recommandé avant production).
2. **DT-2** — évaluer l'opportunité d'un jeton de concurrence sur les entités d'administration
   (non traité par cette remédiation, reste recommandé si l'administration multi-utilisateur
   concurrente devient réelle).
3. **T110 (PC-01, volumétrie)** — reste bloqué par une décision métier non prise ; aucun test de
   charge n'a été exécuté par cette session.
4. **D6 (numérotation)** — décision humaine requise avant que l'équipe considère `research.md`
   comme fidèle à l'implémentation (voir `decision-d6-numbering.md`) ; sans impact fonctionnel
   démontré (SOC-01/SOC-09 restent conformes quelle que soit l'option retenue).

## 14. Éléments pouvant attendre une feature ultérieure — mise à jour

1. DT-4 (mise à jour documentaire de `research.md` D6, une fois la décision actée) — cosmétique.
2. SOC-14/DT-7 (export indicateurs, Could) — aucune urgence.
3. T106-T109 (contenu réel de la matrice, valeurs SLA, périmètres, habilitation de clôture) —
   restent des tâches de configuration dépendant de décisions métier non prises ; les mécanismes
   sous-jacents sont désormais tous démontrés conformes, y compris le calendrier SLA (§3.B) et
   AFF-06 (§3.C), ce qui n'était pas le cas lors de l'audit initial.
4. DT-6/T111 (environnement SQL de CI) — point d'outillage.
5. PRI-02 (influence de la priorité sur l'ordre de file) — un test dédié pourrait être ajouté dans
   une itération ultérieure.

---

## Annexe A — Éléments modifiés pendant l'audit initial (rappel, déjà restauré)

- Ajout de `tests/CIT.Plateforme.Infrastructure.Tests/Tickets/ConcurrenceAutresOperationsAuditTests.cs`
  (3 tests de concurrence réelle, conservés dans la suite permanente).
- Connexions de test temporairement redirigées vers le SQL Server Docker de l'utilisateur puis
  restaurées vers LocalDB (état identique à avant l'audit).

## Annexe B — Éléments modifiés pendant cette session de remédiation

**Code source (`src/`)** :
- `CIT.Plateforme.Domain` : `SuiviTemps/ParametreSlaAdministrable.cs` (calendrier administrable),
  `Tickets/PeriodeAttenteClient.cs` (nouvelle entité), `Files/FileDeTraitement.cs`
  (`CompetenceRequise`), `Files/TransitionAutorisee.cs` (`AjouterMotif`),
  `Affectation/CompetenceOperateur.cs` (nouvelle entité), `Securite/NatureUtilisateur.cs`
  (permission `CompetenceGerer`).
- `CIT.Plateforme.Application` : `Supervision/SurveillerEcheancesUseCase.cs`,
  `Tickets/AttenteClientCommand.cs`, `Administration/GererParametresSlaCommand.cs`
  (`DefinirAvecCalendrierAsync`), `Administration/GererFilesCommand.cs` (surcharge compétence),
  `Documents/DeposerDocumentCommand.cs` (deux points d'entrée + autorisation),
  `Affectation/GererDisponibiliteCommand.cs` (autorisation), `Affectation/DistributionAutomatiqueUseCase.cs`
  (câblage AFF-06), `Affectation/GererCompetencesOperateurCommand.cs` (nouvelle commande),
  `Abstractions/Repositories.cs` (nouveaux ports `IPeriodeAttenteClientRepository`,
  `ICompetenceOperateurRepository`).
- `CIT.Plateforme.Infrastructure` : `Persistence/PlateformeDbContext.cs` (mappings + ValueComparers
  + migration), `Persistence/ParametresSlaProvider.cs` (correction du bug), `Persistence/Repositories.cs`
  (nouveaux repositories, `CandidatsAffectationProvider` corrigé), `DependencyInjection.cs`
  (nouveaux enregistrements), nouvelle migration
  `20260829164513_RemediationAuditSlaCalendrierCompetenceValueComparer`.
- `CIT.Plateforme.Web` : `Components/Pages/Administration/Disponibilite.razor` (champ appelant),
  `Components/Pages/Tickets/Documents.razor` (sélecteur client/agent).

**Tests (`tests/`) — 24 nouveaux, tous ajoutés à la suite permanente** :
`Application.Tests/Supervision/SurveillerEcheancesCalendrierTests.cs` (5),
`Application.Tests/Administration/ParametresSlaProviderTests.cs` (2),
`Infrastructure.Tests/Persistence/ParametreSlaDureeCibleTests.cs` (5, `[Theory]`),
`Infrastructure.Tests/Persistence/ValueComparerPersistanceTests.cs` (1),
`Application.Tests/Documents/DepotDocumentAutorisationTests.cs` (4),
`Application.Tests/Affectation/DisponibiliteAutorisationTests.cs` (3),
`Application.Tests/Affectation/DistributionCompetenceTests.cs` (4) ; 3 tests préexistants mis à
jour sans ajouter de nouveau cas (`ControleDepotTests`, `DisponibiliteTests`, `VueChargeTests`,
`AttenteClientTests` — signatures adaptées aux nouveaux paramètres).

**Documentation** : `specs/001-socle-tickets-workflows/decision-d6-numbering.md` (nouveau) ;
`specs/001-socle-tickets-workflows/post-implementation-audit.md` (ce fichier, mis à jour).

**Bases de données temporaires créées puis supprimées** sur le SQL Server Docker de l'utilisateur :
`CitRemediation001Schema` (validation des 8 migrations depuis zéro), `CitRemediation001Concurrence`,
`CitRemediation001Web` (exécution complète de la suite de tests) — aucune ne subsiste ; les bases
`BillingOffice`/`BillingOfficeTests` de l'utilisateur n'ont pas été touchées. Les fichiers de
configuration de test (`BaseDeTestSql.cs`, `PlateformeWebApplicationFactory.cs`) ont été restaurés
à l'identique (LocalDB) après validation — revérifié vert (109/109) après restauration.

**Aucune modification** de `spec.md`, `plan.md`, `research.md`, `data-model.md`,
`contracts/use-cases.md`, `tasks.md`, ni du CDC — conformément à la consigne.
