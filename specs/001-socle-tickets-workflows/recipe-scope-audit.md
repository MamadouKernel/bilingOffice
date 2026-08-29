# Audit de périmètre — Scénarios de recette CDC §22 vs Feature 001

**Feature** : `001-socle-tickets-workflows` | **Date** : 2026-08-28 | **Statut** : Correction appliquée

## Erreur détectée

Les artefacts de la Feature 001 (`quickstart.md`, `plan.md`, `research.md`, et la matrice de
couverture de `spec.md`) traitaient tout ou partie des **14 scénarios de recette** du CDC §22
(REC-SCE-01 à REC-SCE-14, définis dans `docs/04-matrice-recette.md`) comme applicables à cette
feature.

Or **REC-SCE-01 à REC-SCE-08** portent sur des parcours métier complets qui n'existent pas encore
dans le périmètre livré par cette feature :

| Scénario | Parcours métier concerné | Feature qui le spécifiera |
|---|---|---|
| REC-SCE-01 | IMPORT client normal | Billing Office IMPORT |
| REC-SCE-02 | IMPORT client en compte | Billing Office IMPORT |
| REC-SCE-03 | IMPORT client deposit | Billing Office IMPORT |
| REC-SCE-04 | IMPORT client IES | Billing Office IMPORT |
| REC-SCE-05 | EXPORT (4 types de clients) | Billing Office EXPORT |
| REC-SCE-06 | Simulation de proforma | Simulation de Proforma |
| REC-SCE-07 | Customer Complaint | Customer Complaint |
| REC-SCE-08 | Enrôlement transporteur | Enrôlement Transporteurs |

Un deuxième problème, plus fin, a été découvert en creusant la correction demandée : **`quickstart.md`
attribuait aux identifiants REC-SCE-01→14 des intitulés fabriqués** (« Création de ticket »,
« Documents », « Dossier », « Cloisonnement »…) qui ne correspondent **pas** aux intitulés réels du
CDC §22 pour ces mêmes identifiants dans `docs/04-matrice-recette.md`. Ce n'était pas seulement une
erreur de périmètre mais un début de dérive de contenu sous des références CDC réelles — corrigé en
même temps, par retour aux intitulés exacts du CDC.

Un troisième effet, mécanique celui-là, a été identifié en recalculant la couverture : neuf
exigences Must (`SOC-02, SOC-03, SOC-05, SOC-06, SOC-07, SOC-08, SOC-09, MSG-01, WKF-01`) étaient
marquées « Directe » ou « Partielle » dans `spec.md` uniquement parce qu'un scénario Billing Office
(le plus souvent REC-SCE-01) les exerce accessoirement au passage — mais ce scénario n'est pas
exécutable dans le seul périmètre de la Feature 001. Reclassées « Absente » pour cette feature.

## Fichiers corrigés

| Fichier | Correction appliquée |
|---|---|
| `specs/001-socle-tickets-workflows/quickstart.md` | Section 5 réécrite : suppression des 8 lignes REC-SCE-01→08 (contenu fabriqué) ; les 6 lignes REC-SCE-09→14 restantes reprennent l'intitulé exact du CDC (`docs/04-matrice-recette.md`) au lieu d'un intitulé inventé ; section 6 corrigée (« REC-SCE-01→14 » → « REC-SCE-09→14 ») |
| `specs/001-socle-tickets-workflows/spec.md` | Section « Testabilité » : liste des Must sans scénario direct passée de 15 à **24** items (recalcul mécanique) ; section « Matrice de couverture » : 11 lignes corrigées (SOC-01 recentré sur REC-SCE-09→14 ; SOC-02, SOC-03, SOC-05, SOC-06, SOC-07, SOC-08, SOC-09, MSG-01, WKF-01 reclassées **Absente** ; SOC-11 recentrée sur REC-SCE-14 seul) |
| `specs/001-socle-tickets-workflows/plan.md` | 3 occurrences de « REC-SCE-01→14 » corrigées en « REC-SCE-09→14 » (Technical Context, Constitution Check §XII, arborescence des tests) |
| `specs/001-socle-tickets-workflows/research.md` | 1 occurrence corrigée dans la décision D12 (stratégie de tests) |
| `specs/001-socle-tickets-workflows/contracts/use-cases.md` | **Aucune correction nécessaire** — vérifié : ce fichier ne mentionne aucun REC-SCE, donc aucun contrat n'était artificiellement rattaché à REC-SCE-01→08 |
| `specs/001-socle-tickets-workflows/data-model.md` | **Aucune correction nécessaire** — vérifié : aucune mention de REC-SCE |
| `docs/04-matrice-recette.md` | **Non modifié** — reste la source de vérité globale du CDC §22, valable pour l'ensemble du projet (toutes features), volontairement hors du périmètre de cette correction |

## REC-SCE applicables à la Feature 001

**REC-SCE-09 à REC-SCE-14** (6 scénarios) :

| Scénario | Intitulé exact (CDC §22 / `docs/04-matrice-recette.md`) |
|---|---|
| REC-SCE-09 | Prise en charge : ticket en attente de PEC, opérateur l'accepte, horodatages enregistrés |
| REC-SCE-10 | Transfert de file avec motif obligatoire, conservation intégrale du ticket, refus des destinations non autorisées |
| REC-SCE-11 | Réaffectation entre opérateurs (A → B) selon habilitations, tracée |
| REC-SCE-12 | Temps et SLA : mesure attente/traitement, distinction interne/client, seuils 50 %/80 %/dépassement, escalade |
| REC-SCE-13 | Supervision : vue temps réel des files et de la charge, alertes, affectation, déblocage |
| REC-SCE-14 (partiel) | Transverse : notifications, continuité, dossier, cloisonnement, audit — seule la portion socle (dossier, audit) relève de la Feature 001 |

## REC-SCE hors périmètre (Feature 001)

**REC-SCE-01 à REC-SCE-08** (8 scénarios) — conservés tels quels dans `docs/04-matrice-recette.md`
pour les features futures listées dans le tableau « Erreur détectée » ci-dessus. Aucune information
n'a été supprimée de ce document : la correction ne porte que sur les artefacts de la Feature 001.

## Nombre d'exigences Feature 001

**74/74** — inchangé. Vérifié mécaniquement : les 74 références de `spec.md` (FR-001 à FR-074)
correspondent toujours exactement aux groupes A (Socle, SOC/CYC/MSG) et B (Workflow/Supervision,
WKF/PEC/TRF/TPS/SLA/ALE/SUP/DIS/AFF/PRI/MAS) de `docs/01-matrice-exigences.md`. Cette correction ne
change **aucune** exigence, aucune priorité, aucune User Story — uniquement l'évaluation de leur
couverture par les scénarios de recette CDC.

### Répartition Must / Should / Could

| Priorité | Nombre |
|---|---|
| Must | 62 |
| Should | 11 |
| Could | 1 |
| **Total** | **74** |

## Couverture recette recalculée (périmètre Feature 001 uniquement)

> Méthode : extraction mécanique de `docs/04-matrice-recette.md` (Partie 3 pour les 62 Must ;
> colonne Should/Could de la Partie 1 pour les 12 Should/Could), puis intersection avec l'ensemble
> {REC-SCE-09 … REC-SCE-14}. Aucun comptage manuel.

| Couverture | Nombre (/74) | dont Must (/62) | dont Should/Could (/12) |
|---|---|---|---|
| **Directe** | 36 | 33 | 3 |
| **Partielle** | 5 | 5 | 0 |
| **Absente** | 33 | 24 | 9 |

## Must sans scénario CDC direct exécutable dans le périmètre Feature 001 (24)

| Référence CDC | User Story | Couverture actuelle | Motif |
|---|---|---|---|
| SOC-02 | US1, US6 | **Absente** | Seul scénario CDC couvrant (REC-SCE-01 à REC-SCE-08) est un parcours Billing Office, hors périmètre F001 |
| SOC-03 | US1 | **Absente** | Seul scénario CDC couvrant (REC-SCE-01 à REC-SCE-08) est un parcours Billing Office, hors périmètre F001 |
| SOC-05 | US5 | **Absente** | Seul scénario CDC couvrant (REC-SCE-01) est un parcours Billing Office, hors périmètre F001 |
| SOC-06 | US1 | **Absente** | Seuls scénarios CDC couvrant (REC-SCE-01, REC-SCE-07) sont des parcours Billing Office/Customer Complaint, hors périmètre F001 |
| SOC-07 | US6 | **Absente** | Seul scénario CDC couvrant (REC-SCE-01 à REC-SCE-08) est un parcours Billing Office, hors périmètre F001 |
| SOC-08 | US1 | **Absente** | Seul scénario CDC couvrant (REC-SCE-01) est un parcours Billing Office, hors périmètre F001 |
| SOC-09 | US4 | **Absente** | Seul scénario CDC couvrant (REC-SCE-01) est un parcours Billing Office, hors périmètre F001 |
| MSG-01 | US5 | **Absente** | Seul scénario CDC couvrant (REC-SCE-01) est un parcours Billing Office, hors périmètre F001 |
| WKF-01 | US1, US2 | **Absente** | Seul scénario CDC couvrant (REC-SCE-01 à REC-SCE-08) est un parcours Billing Office, hors périmètre F001 |
| CYC-01 | US4 | **Absente** | Aucun scénario CDC §22 ne couvre cette exigence, y compris hors périmètre |
| CYC-02 | US4 | **Absente** | Aucun scénario CDC §22 ne couvre cette exigence, y compris hors périmètre |
| CYC-03 | US4 | **Absente** | Aucun scénario CDC §22 ne couvre cette exigence, y compris hors périmètre |
| CYC-04 | US3 | **Absente** | Aucun scénario CDC §22 ne couvre cette exigence, y compris hors périmètre |
| CYC-05 | US3 | **Absente** | Aucun scénario CDC §22 ne couvre cette exigence, y compris hors périmètre |
| MSG-02 | US5 | **Absente** | Aucun scénario CDC §22 ne couvre cette exigence, y compris hors périmètre |
| MSG-03 | US5 | **Absente** | Aucun scénario CDC §22 ne couvre cette exigence, y compris hors périmètre |
| MSG-05 | US5 | **Absente** | Aucun scénario CDC §22 ne couvre cette exigence, y compris hors périmètre |
| DIS-03 | US9 | **Absente** | Aucun scénario CDC §22 ne couvre cette exigence, y compris hors périmètre |
| AFF-07 | US9 | **Absente** | Aucun scénario CDC §22 ne couvre cette exigence, y compris hors périmètre |
| PRI-01 | US10 | **Absente** | Aucun scénario CDC §22 ne couvre cette exigence, y compris hors périmètre |
| PRI-02 | US10 | **Absente** | Aucun scénario CDC §22 ne couvre cette exigence, y compris hors périmètre |
| PRI-03 | US10 | **Absente** | Aucun scénario CDC §22 ne couvre cette exigence, y compris hors périmètre |
| PRI-04 | US10 | **Absente** | Aucun scénario CDC §22 ne couvre cette exigence, y compris hors périmètre |
| MAS-02 | US10 | **Absente** | Aucun scénario CDC §22 ne couvre cette exigence ; de plus conditionnée à MAS-01 (Should) |

*(9 premières lignes : exigences reclassées par cette correction, auparavant comptées « Directe »/
« Partielle » à tort via un scénario Billing Office. 15 lignes suivantes : déjà sans scénario avant
la correction — inchangées.)*

Ces 24 exigences Must ne sont pas facultatives. Un Test Plan complémentaire devra être défini pour
chacune lors de `/speckit.tasks`, à partir de leurs critères d'acceptation déjà formulés dans
`spec.md`.

## Contrôle final

| Vérification | Résultat |
|---|---|
| Feature 001 reste à 74/74 exigences (FR-001 à FR-074, groupes A+B de `01-matrice-exigences.md`) | ✅ Confirmé mécaniquement — aucune exigence ajoutée, retirée ou renumérotée |
| Aucun scénario métier futur (REC-SCE-01→08) n'est présenté comme recette du socle | ✅ Confirmé — recherche exhaustive de `REC-SCE-0[1-8]` dans `spec.md`, `quickstart.md`, `plan.md`, `research.md` : ne subsiste que dans des phrases explicatives indiquant qu'ils sont hors périmètre, jamais comme preuve de couverture d'une ligne de matrice |
| Aucune exigence CDC n'a été supprimée | ✅ Confirmé — `docs/01-matrice-exigences.md` et `docs/04-matrice-recette.md` non modifiés ; les 74 FR de `spec.md` inchangées (seule leur colonne Couverture a été recalculée) |
| Aucun scénario n'a été inventé | ✅ Confirmé — les intitulés fabriqués précédemment présents dans `quickstart.md` ont été retirés et remplacés par les intitulés exacts de `docs/04-matrice-recette.md` ; aucun nouvel identifiant REC-SCE n'a été créé |
