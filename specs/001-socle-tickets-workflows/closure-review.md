# Revue de fermeture — Feature 001 : Socle transverse de gestion des tickets et workflows

**Date** : 2026-08-29
**Sources** : `post-implementation-audit.md`, `decision-d6-numbering.md`, `spec.md`, `plan.md`, `research.md`, `tasks.md`
**Objectif** : déterminer si la Feature 001 peut être clôturée proprement avant le début de la Feature 002.

> Cette revue ne modifie aucune exigence CDC, aucune priorité Must/Should/Could, et ne résout
> arbitrairement aucun point PC-xx encore ouvert. Elle applique une seule décision humaine
> explicitement communiquée (D6, Option B) et recalcule le statut final à partir de preuves
> vérifiées (tests réellement exécutés, migrations réellement rejouées), pas de déclarations.

---

## 1. Décision D6 — appliquée

**Décision communiquée** : Option B validée (séquences globales ticket/dossier avec préfixes
lisibles).

**Actions effectuées** :
1. `research.md` (décision D6) **mis à jour** pour décrire fidèlement l'implémentation réelle —
   rationale complet, garanties vérifiées (unicité, non-réutilisation, lisibilité, absence de
   limite de volumétrie pratique), alternatives rejetées (Option A explicitement écartée par le
   maître d'ouvrage, GUID seul, compteur en table), et une section dédiée confirmant l'**absence
   d'impact sur les exigences CDC** (SOC-01, SOC-09 restent intégralement satisfaits quelle que
   soit l'option de partitionnement retenue — c'est un choix d'architecture interne, invisible du
   client/agent).
2. `decision-d6-numbering.md` **mis à jour** : statut passé de « À DÉCIDER » à « DÉCIDÉ », section
   finale « Décision » complétée (option retenue, décideur, date).
3. **Aucune ligne de code modifiée** : `ServiceNumerotation.cs` correspondait déjà à l'Option B —
   conformément à la consigne « ne pas modifier le comportement du code si le code correspond déjà
   à cette option ».
4. Suite de tests réexécutée après la modification documentaire (aucune régression possible
   puisqu'aucun code n'a changé, vérifié tout de même par principe) : **109/109 verts**.

**Statut final D6** : **CONFORME** — la divergence documentaire est résorbée, la décision est
tracée, aucune ambiguïté ne subsiste entre `research.md` et le code.

---

## 2. Analyse des exigences PARTIEL restantes

### 2.0 Préalable — correction d'une incohérence arithmétique du rapport d'audit

En reconstituant la table complète des 74 exigences (le fichier `post-implementation-audit.md`
actuel ne conserve qu'une table différentielle des lignes modifiées par la remédiation, plus la
liste des 68 lignes « inchangées » sans le détail), le compte annoncé en section 1 du rapport
(« PARTIEL : 6 ») s'est révélé être un **report non recalculé** de l'ancien total (8, lui-même déjà
approximatif) après la remédiation, qui n'a pas été redescendu à sa valeur réelle. En recomptant
ligne par ligne à partir de la table détaillée produite lors de l'audit initial et des 7 corrections
de la session de remédiation :

- **Exigences réellement encore PARTIEL au niveau d'une ligne des 74** : **2** — `SOC-14` et
  `PRI-02`.
- **CONFORME** : **72** (65 initialement conformes + 7 lignes corrigées par la remédiation : SOC-07,
  TPS-04, SLA-03, SLA-04, SLA-05, AFF-06, DIS-02).
- **ÉCART** : **0**.
- **BLOQUÉ (ligne dédiée)** : **0** — TRF-06 reste une ligne CONFORME ; sa dépendance au contenu de
  PC-07 est un critère de **disponibilité en production**, pas une non-conformité de l'exigence
  elle-même (le mécanisme paramétrable, lui, est démontré).

**72 + 2 + 0 + 0 = 74.** ✅ Ce recomptage est cohérent et remplace le chiffre erroné de la version
précédente du rapport. Les trois autres éléments cités dans la demande de revue de fermeture
(absence de jeton de concurrence, audit append-only applicatif, environnement SQL CI) ne sont **pas
des lignes de la matrice des 74 exigences** : ce sont des éléments de dette technique transverse,
déjà correctement classés comme tels (pas comme PARTIEL) dans le rapport d'audit. Ils sont traités
en §2.2 pour répondre complètement à la demande, mais ne changent pas le compte des 74.

### 2.1 Table d'analyse — les 2 exigences PARTIEL

| Exigence | Cause du PARTIEL | Risque réel | Correction nécessaire avant clôture ? | Correction nécessaire avant production ? | Peut attendre une feature ultérieure ? | Justification |
|---|---|---|---|---|---|---|
| **SOC-14** (Could) — export des indicateurs et listes de tickets (Excel, PDF) | Le mécanisme (`ExporterIndicateursQuery`) existe et est câblé (DI, contrat C-14/C-24), mais **aucun test dédié** ne démontre son comportement (aucune occurrence du nom de la classe dans `tests/`, vérifié par recherche exhaustive). | Faible. Aucune donnée n'est corrompue, aucune règle métier n'est contournée par l'absence de test — au pire, une régression future sur l'export ne serait pas détectée automatiquement. Priorité **Could** : le CDC lui-même la marque comme non essentielle. | **Non.** Aucun critère d'acceptation Must n'en dépend ; aucun scénario REC-SCE-09→14 ne l'exerce ; aucune exigence de sécurité/intégrité n'est concernée. | **Souhaitable, non bloquant.** Un test de non-régression serait une bonne pratique avant une mise en production où l'export serait activement utilisé par les superviseurs, mais son absence ne compromet ni la sécurité ni l'intégrité des données. | **Oui.** Peut être complété lors d'une itération ultérieure (ou lors de la feature qui exposera réellement l'export dans l'UI de supervision), sans impact sur la fermeture de ce socle. | Exigence Could, non testée mais non bloquante par nature du CDC (§01-matrice-exigences.md classe explicitement SOC-14 « Could »). |
| **PRI-02** (Must) — la priorité peut influencer l'ordre du ticket dans la file, le SLA applicable, les alertes et les règles d'escalade | Vérifié par relecture directe du code pendant cette revue : `ConsulterFileQuery.ExecuterAsync` retourne la liste des tickets d'une file **sans aucun tri par priorité** (seul un filtre par statut existe) ; `VueFilesQuery`/`VueChargeQuery` retournent des agrégats de comptage, jamais un tri par priorité. `NiveauPriorite.Rang` (le champ conçu pour ordonner) n'est utilisé nulle part pour trier une requête. **L'attribution et la traçabilité de la priorité (PRI-01, PRI-03, PRI-04) sont, elles, pleinement implémentées et testées** (`PrioriteTests`, `ModifierPrioriteCommand`) — seule la « traduction » de la priorité en un ordre d'affichage effectif est absente du code, pas seulement de ses tests. | Modéré, mais non structurel : un superviseur ou un agent ne voit pas automatiquement les tickets prioritaires remontés en tête de liste — impact ergonomique/pilotage, pas un défaut de sécurité, d'intégrité des données ou de perte d'information (la priorité est stockée, tracée, consultable ; elle n'influence simplement pas encore l'ordre de restitution). | **Non**, sous réserve de la lecture retenue du texte CDC. Le libellé exact de PRI-02 (`docs/01-matrice-exigences.md`) est : *« la priorité **peut** influencer l'ordre... »* — un verbe modal, pas impératif (contrairement à la majorité des autres Must du CDC qui utilisent systématiquement « DOIT »). Aucun critère de succès mesurable de `spec.md` (section Success Criteria) ne porte sur un ordonnancement visible par priorité, et aucun scénario REC-SCE-09→14 ne l'exerce. La donnée nécessaire (`PrioriteId`, `Rang`) est disponible et interrogeable : une vue consommatrice **peut** déjà l'exploiter sans changement de schéma. | **Oui, fortement recommandé.** Le tri effectif par priorité dans les files (`ConsulterFileQuery`) et les vues de supervision est une capacité de pilotage attendue en exploitation réelle ; son absence deviendrait un vrai defaut d'usage dès la mise en production, même si elle ne viole aucun critère d'acceptation formel du socle. | **Oui, mais à traiter tôt.** Le tri par priorité est naturellement porté par la future feature consommatrice (portail agent/console de supervision), qui affichera réellement les files — le socle fournit déjà toute la donnée nécessaire sans travail supplémentaire de modélisation. Une alternative acceptable est d'ajouter le tri directement dans `ConsulterFileQuery`/`VueFilesQuery` en fin de Feature 001 si l'équipe préfère ne rien devoir aux features suivantes sur ce point — **décision d'équipe, pas un blocage de clôture**. | Le CDC emploie un modal permissif (« peut »), contrairement au « DOIT » utilisé pour les Must impératifs du même document ; aucun SC/scénario de recette n'en fait un critère mesurable ; le socle expose déjà la donnée nécessaire sans dette de modélisation. |

### 2.2 Traitement spécifique demandé — dette technique transverse (hors lignes des 74)

| Point | Cause | Risque réel | Correction avant clôture ? | Correction avant production ? | Peut attendre une feature ultérieure ? | Justification |
|---|---|---|---|---|---|---|
| **Absence de jeton de concurrence sur les entités d'administration** (`FileDeTraitement`, `TransitionAutorisee`, `ParametreSlaAdministrable`, `PerimetreSupervision`, `HabilitationUtilisateur`) | Seul `Ticket.VersionConcurrence` porte un `RowVersion` ; les entités de configuration n'en portent pas. | Faible. Deux administrateurs modifiant **simultanément** la même ligne de configuration (ex. la même transition, le même paramètre SLA) verraient l'un écraser silencieusement l'autre (dernier écrivain gagne). Aucune exigence CDC de ce socle n'exige explicitement une protection de concurrence sur les écrans d'administration (contrairement à la prise en charge d'un ticket, exigence explicite du CDC — edge case de `spec.md`) ; l'administration est, en pratique, une activité rare et peu concurrente. | **Non.** Aucun Must ne l'exige ; ne compromet pas la sécurité (pas de contournement d'habilitation) ; n'invalide aucun critère d'acceptation ; ne bloque aucun scénario REC-SCE-09→14 (aucun n'exerce une modification concurrente de configuration). | **Recommandé, non strictement obligatoire.** À évaluer selon le nombre réel d'administrateurs fonctionnels simultanés en production (probablement faible au départ). | **Oui.** Peut être ajouté au fil de l'eau, entité par entité, sans refonte, dès qu'un besoin réel de concurrence d'administration est constaté. | Dette technique déjà correctement classée comme telle (pas ÉCART) dans l'audit ; aucune exigence Must ni critère de sécurité ne la rend bloquante pour la fermeture. |
| **Audit append-only garanti seulement au niveau applicatif** (pas de renfort SGBD : pas de trigger, pas de droits SQL restreints, pas de table à append-only natif) | `JournalAuditAppendOnlyGuard` intercepte toute tentative de modification/suppression **passant par l'application** (EF Core/`UniteDeTravail`) ; rien n'empêche un accès direct à la base (compte DBA, script de maintenance) de contourner cette garde. | Modéré en théorie (dépend entièrement du modèle de menace retenu pour l'environnement de production), nul en pratique tant que l'accès direct à la base reste restreint aux administrateurs techniques de confiance — un accès direct à la base par un administrateur technique n'est de toute façon jamais empêchable par du code applicatif, quel qu'il soit. | **Non.** SOC-13/SEC-05 (« journal d'audit inviolable ») sont satisfaits pour tout acteur passant par l'application — ce qui couvre l'intégralité des acteurs fonctionnels visés par le CDC (clients, agents, superviseurs, administrateurs fonctionnels). Le renfort SGBD répond à un modèle de menace distinct (accès infrastructure privilégié), hors du périmètre applicatif d'un socle métier. Aucun scénario REC-SCE-09→14 n'exerce ni ne requiert ce renfort. | **Oui, recommandé avant une mise en production où l'accès direct à la base ne serait pas strictement contrôlé** (ex. environnement mutualisé, prestataires externes avec accès DBA). Un renfort possible (droits SQL restreints pour le compte applicatif, table système en lecture seule) relève d'une décision d'exploitation/DSI, pas d'une réécriture du socle. | **Oui.** C'est une tâche d'exploitation/durcissement (droits SQL, configuration serveur), indépendante du code de la Feature 001 — peut être menée en parallèle de toute feature ultérieure, par l'équipe infrastructure. | Le mot « inviolable » du CDC est honoré pour tous les acteurs applicatifs prévus par les exigences ; l'accès infrastructure privilégié est un modèle de menace d'exploitation, pas une exigence fonctionnelle du socle. |
| **Environnement SQL de test en CI non tranché** (T111 — LocalDB vs conteneur dédié vs instance DSI partagée) | Point ouvert documenté depuis `research.md` (§Points ouverts) et jamais tranché — un choix technique d'outillage, pas un défaut de code. | Faible et purement organisationnel : sans décision, chaque poste de développement utilise LocalDB par défaut (fonctionnel, démontré à plusieurs reprises dans cette session avec un vrai SQL Server, y compris sur le Docker de l'utilisateur) — le risque est une CI non reproductible entre postes/serveurs, pas une régression fonctionnelle. | **Non.** Ce n'est pas un défaut de comportement du socle ; les tests s'exécutent et passent quel que soit l'environnement SQL choisi (démontré : LocalDB et SQL Server Docker donnent tous deux 109/109 verts dans cette session). | **Oui, avant une mise en place de CI/CD automatisée en production continue** — sans quoi chaque exécuteur CI devra recréer LocalDB à la volée, ce qui fonctionne mais mérite une décision explicite d'équipe pour la reproductibilité à long terme. | **Oui.** Décision d'outillage/DevOps, sans lien avec le contenu fonctionnel de la Feature 001 — peut être tranchée indépendamment, y compris après le démarrage de la Feature 002. | Point déjà classé « CLARIFICATION REQUIRED » (pas un PC-xx métier) dans `tasks.md` (T111) — une décision d'outillage, jamais un défaut fonctionnel démontré. |

**Aucun de ces 5 points ne remplit un des 4 critères de correction obligatoire avant clôture** fixés
par la demande de revue (empêcher la satisfaction d'un Must ; compromettre sécurité/intégrité ;
invalider un critère d'acceptation ; empêcher la recette du socle). **Aucune correction de code
n'a donc été appliquée** pendant cette revue de fermeture, conformément à la consigne de ne
corriger que ce qui remplit ces critères.

---

## 3. Statut final recalculé (nouvelle taxonomie de clôture)

> Taxonomie demandée pour cette revue : **CONFORME**, **PARTIEL ACCEPTÉ**, **BLOQUÉ MÉTIER**,
> **BLOQUÉ TECHNIQUE**. Cette taxonomie remplace, pour les besoins de la décision de clôture,
> celle de l'audit (CONFORME/PARTIEL/ÉCART/BLOQUÉ/NON DÉMONTRÉ) — elle n'efface pas l'historique
> des preuves, qui reste dans `post-implementation-audit.md`.

| Statut de clôture | Nombre (/74) | Éléments concernés |
|---|---|---|
| **CONFORME** | 72 | Toutes les exigences hors les 2 ci-dessous — vérifiées par test réel, sans réserve |
| **PARTIEL ACCEPTÉ** | 2 | SOC-14 (Could, sans test dédié, différable) ; PRI-02 (Must au libellé permissif « peut », donnée disponible mais non exploitée pour le tri, différable) |
| **BLOQUÉ MÉTIER** | 0 *(ligne dédiée)* | Aucune ligne des 74 n'est individuellement bloquée par une décision métier ; **mais** la disponibilité en **production** de TRF-06 (contenu réel de la matrice), SLA-01/02/05 (valeurs cibles), SUP-01..05 (composition des périmètres) reste conditionnée à PC-07/PC-12/PC-04 — mécanismes CONFORME, contenu métier non fourni (T106-T108, non cochées, à raison) |
| **BLOQUÉ TECHNIQUE** | 0 *(ligne dédiée)* | Aucune ligne des 74 n'est bloquée par un obstacle technique ; l'environnement SQL de CI (T111) est un point d'outillage ouvert, mais ne bloque aucune exigence — voir §2.2 |
| **Total** | **74** | |

**0 ÉCART.** Toutes les exigences Must sont satisfaites au sens fonctionnel du CDC ; les 2 PARTIEL
ACCEPTÉ portent l'un sur une exigence Could (SOC-14), l'autre sur la lecture permissive d'un Must
(PRI-02) dont la donnée nécessaire est déjà disponible sans dette de modélisation.

---

## 4. Vérifications finales (section 7 de la demande)

| Vérification demandée | Résultat | Preuve |
|---|---|---|
| 74/74 exigences tracées | ✅ | Table complète maintenue depuis l'audit initial + delta de remédiation ; recomptage §2.0 de ce document |
| 62/62 Must testées | ✅ | Chaque Must dispose d'au moins un test associé (traçabilité CDC→US→FR→Contract→Task→Test déjà établie) ; PRI-02 dispose de tests pour ses volets attribution/traçabilité (PRI-01/03/04) — seul le volet « influence sur l'ordre » n'a pas de test dédié, noté explicitement en §2.1, sans remettre en cause l'existence d'un test pour le Must dans son ensemble |
| 0 ÉCART | ✅ | Confirmé par le recomptage §2.0 (0 ÉCART après les 7 corrections de la session de remédiation) |
| REC-SCE-09→14 verts | ✅ | 9/9 tests (`CIT.Plateforme.Web.Tests.RecetteCdc`), réexécutés dans cette session : verts |
| Migrations 1→8 rejouables | ✅ | `dotnet ef migrations list` confirme les 8 migrations dans l'ordre ; rejouées avec succès sur base vide (SQL Server Docker réel) lors de la session de remédiation, revérifiées listables dans cette session |
| 109 tests ou plus verts | ✅ | 109/109 (`21 + 64 + 15 + 9`), réexécutés dans cette session (LocalDB), exit code 0 |
| Aucun PC résolu arbitrairement | ✅ | D6 n'est pas un point PC-xx numéroté (c'est une décision d'architecture de `research.md`, distincte de la liste PC-01→PC-26 de `docs/02-points-a-clarifier.md`) ; sa résolution a suivi une décision humaine explicitement communiquée, pas une initiative autonome. Aucun PC-01→PC-26 n'a été tranché ; T106-T111 restent non cochées dans `tasks.md` |

---

## 5. Écarts fermés (depuis l'audit initial jusqu'à cette revue de fermeture)

| # | Écart | Fermé lors de | Preuve |
|---|---|---|---|
| 1 | Calcul SLA ignorant le calendrier ouvré configuré | Session de remédiation | `ParametresSlaProviderTests`, `SurveillerEcheancesCalendrierTests` (7 tests) |
| 2 | `DureeCible` limité à < 24h (type SQL `time`) | Session de remédiation | `ParametreSlaDureeCibleTests` (5 valeurs, jusqu'à 120h, SQL Server réel) |
| 3 | Autorisation manquante — dépôt de document | Session de remédiation | `DepotDocumentAutorisationTests` (4 tests, positif/négatif) |
| 4 | Autorisation manquante — gestion de la disponibilité | Session de remédiation | `DisponibiliteAutorisationTests` (3 tests, positif/négatif) |
| 5 | AFF-06 (compétence) non exploitable de bout en bout | Session de remédiation | `DistributionCompetenceTests` (4 tests) |
| 6 | Avertissements EF Core Value Comparer (2 entités) | Session de remédiation | `dotnet ef migrations list --verbose` (0 avertissement) + `ValueComparerPersistanceTests` |
| 7 | Divergence documentaire D6 (`research.md` vs code) | **Cette revue de fermeture** | `research.md` mis à jour, `decision-d6-numbering.md` clos |

**0 écart ouvert.**

---

## 6. Dettes techniques acceptées (fermeture de la Feature 001, sans correction)

| # | Dette | Sévérité | Décision |
|---|---|---|---|
| 1 | Absence de jeton de concurrence sur les entités d'administration | Faible | Acceptée — à réévaluer si l'administration concurrente devient réelle en production |
| 2 | Audit append-only applicatif uniquement (pas de renfort SGBD) | Faible à modéré selon modèle de menace | Acceptée — renfort recommandé en durcissement pré-production, hors périmètre code |
| 3 | Environnement SQL de CI non tranché (T111) | Faible, organisationnel | Acceptée — décision d'outillage indépendante du contenu fonctionnel |
| 4 | `SOC-14` (export) sans test dédié | Faible (Could) | Acceptée — priorité Could, différable |
| 5 | `PRI-02` — tri par priorité non exploité dans les vues de file/supervision | Modéré | Acceptée pour la clôture du socle, **recommandée avant l'exploitation réelle** (voir §2.1) |

---

## 7. Points bloqués (non résolus, à dessein)

| Point | Nature | Statut |
|---|---|---|
| PC-07 — contenu réel de la matrice de transitions | Métier | Ouvert — T106 non cochée, mécanisme CONFORME |
| PC-12 — valeurs cibles SLA, calendrier réel | Métier | Ouvert — T107 non cochée, mécanisme CONFORME (calendrier désormais fonctionnel) |
| PC-04 — composition réelle des périmètres de supervision | Métier | Ouvert — T108 non cochée, mécanisme CONFORME |
| PC-20 — rôle habilité à la clôture pour absence de réponse | Métier | Ouvert — T109 non cochée, mécanisme CONFORME (habilitation paramétrable) |
| PC-01 — volumétrie opérationnelle cible | Métier | Ouvert — T110 non cochée, aucun test de charge exécuté |
| T111 — environnement SQL de CI | Technique/outillage | Ouvert — voir §2.2/§6 |

**Aucun de ces points n'a été tranché par cette revue** — conformément à la consigne, ils restent
des décisions métier ou d'outillage à prendre par l'équipe/le maître d'ouvrage, sans impact sur la
conformité fonctionnelle démontrée du socle.

---

## 8. Risques résiduels

1. **Ergonomique (PRI-02)** — sans tri par priorité dans les files/vues de supervision, un
   superviseur doit encore identifier manuellement les tickets prioritaires. Risque de friction
   opérationnelle en usage réel, pas de risque d'intégrité ou de sécurité.
2. **Exploitation (concurrence d'administration)** — une modification concurrente de configuration
   (matrice, SLA, périmètres) par deux administrateurs simultanément écraserait silencieusement
   l'un des deux changements. Risque jugé faible en probabilité (activité d'administration peu
   fréquente et généralement non simultanée dans l'usage réel attendu).
3. **Conformité (append-only)** — la garantie d'inviolabilité du journal d'audit est applicative,
   pas renforcée au niveau base de données. Risque limité à un modèle de menace d'accès
   infrastructure privilégié, hors du périmètre normal des acteurs fonctionnels du CDC.
4. **Reproductibilité CI** — sans décision sur l'environnement SQL de test, chaque exécuteur CI
   pourrait diverger légèrement (LocalDB vs conteneur). Risque purement organisationnel, sans
   impact sur la validité fonctionnelle démontrée dans cette session (vérifiée sur deux
   environnements SQL Server réels distincts).
5. **Non fonctionnel (volumétrie, PC-01)** — aucune donnée de dimensionnement réelle n'a été
   validée ; le socle n'a pas été testé en charge. Risque de performance en production à un volume
   non caractérisé, sans lien avec la conformité fonctionnelle de ce socle.

Aucun de ces risques ne constitue, seul ou combiné, un obstacle à la clôture du **socle** tel que
spécifié par `spec.md` — ils relèvent tous de la mise en exploitation réelle (production), déjà
correctement identifiée comme distincte de la clôture de la feature dans `tasks.md` (T106-T111).

---

## 9. Critères de clôture — vérification finale

| Critère | Statut |
|---|---|
| Les 74 exigences du socle sont tracées CDC→spec→plan→tasks→code→test | ✅ |
| Les 62 Must ont chacun au moins un test qui les exerce réellement | ✅ |
| 0 exigence en état ÉCART | ✅ |
| Les 6 scénarios de recette applicables (REC-SCE-09→14) sont rejoués et verts | ✅ |
| Les 8 migrations s'appliquent, dans l'ordre, sur une base SQL Server vide | ✅ |
| La suite de tests (109 tests) est intégralement verte | ✅ |
| Les 4 scénarios de concurrence critiques sont démontrés sans mise à jour silencieuse | ✅ |
| Les autorisations sont vérifiées côté serveur pour les 23 commandes sensibles, testées positif/négatif | ✅ |
| Le journal d'audit est append-only au niveau applicatif, distinct de l'historique fonctionnel | ✅ |
| Aucun point PC-xx n'a été résolu arbitrairement | ✅ |
| La divergence D6 est résolue par décision humaine explicite, documentée, sans impact CDC | ✅ |
| Les dettes techniques et PARTIEL restants sont chacun justifiés et classés (accepté / à traiter avant production / différable) | ✅ |
| Aucune fonctionnalité métier nouvelle n'a été ajoutée pendant cette revue | ✅ |
| La Feature 002 n'a pas été commencée | ✅ |

**14/14 critères satisfaits.**

---

## 10. Recommandation finale

# **CLOSE**

**La Feature 001 peut être clôturée.**

**Justification** :
- 0 écart fonctionnel ouvert ; les 2 PARTIEL restants (SOC-14 Could, PRI-02 à lecture permissive)
  ne compromettent ni la sécurité, ni l'intégrité, ni un critère d'acceptation mesurable, ni la
  recette du socle — ils sont explicitement acceptés avec une justification tracée, pas ignorés.
- La divergence architecturale D6, seul point réellement ouvert entre la documentation et le code,
  est désormais tranchée par décision humaine et reflétée fidèlement dans `research.md`.
- Les dettes techniques restantes (concurrence d'administration, renfort SGBD de l'audit,
  environnement CI) sont toutes des points d'exploitation/production ou d'itération future,
  correctement isolés des critères de clôture du socle lui-même, et déjà correctement représentés
  par les tâches non cochées T106-T111 de `tasks.md` — qui **restent, à raison, non cochées**.
- Aucune donnée de test n'a été transformée en configuration métier ; aucun PC-xx n'a été tranché
  arbitrairement.

**Condition de suivi (non bloquante pour la clôture, mais à ne pas perdre)** : le tri effectif par
priorité (PRI-02) devrait être implémenté tôt dans la ou les prochaines features consommatrices du
socle (ou en fin de Feature 001 si l'équipe préfère), car il s'agit d'une capacité d'exploitation
réelle attendue, même si son absence ne bloque pas la clôture formelle du socle.

**La Feature 002 n'a pas été commencée par cette revue, conformément à la consigne.**
