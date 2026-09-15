# Matrice responsive — 81 pages

**Date** : 7 septembre 2026. Méthode : mesure du **débordement horizontal de la PAGE**
(`documentElement.scrollWidth − clientWidth`) sur l'application réelle (`https://localhost:7097`),
complétée par une analyse statique de tous les fichiers `.razor`.

> **Critère** : un débordement horizontal de la **page entière** est un défaut. Un scroll **interne**
> à un `table-responsive` est légitime et n'est pas compté comme défaut (conformément à la consigne).

## 1. Couverture honnête

| Périmètre | Pages | Méthode | Couverture |
|---|---|---|---|
| **Anonyme** (sans authentification) | 13 | **Mesure navigateur en direct** à 320/390/768/1920 + zoom 200 % (≈640) | **11/13 mesurées** en direct |
| **Authentifié** | 68 | **Analyse statique** (voir §3) + recette authentifiée antérieure ([`recette-ui-ux-authentifiee.md`](recette-ui-ux-authentifiee.md)) | **0/68 re-mesurée en direct cette session** |

> **Limite déclarée franchement.** L'authentification navigateur exige de saisir le mot de passe UAT,
> qui ne doit **jamais** apparaître en console ni dans un rapport. Typé dans un appel d'outil, il y
> figurerait. Les 68 pages authentifiées n'ont donc **pas** été re-mesurées en direct cette session ;
> elles reposent sur l'analyse statique (§3, objective) et la recette authentifiée antérieure. C'est
> une réserve, pas une validation.

## 2. Pages anonymes — mesures en direct (débordement horizontal de page)

| Route | 320 | 390 | 768 | 1920 | Zoom 200 % (~640) | Résultat |
|---|---|---|---|---|---|---|
| `/` (accueil) | 0 | 0 | 0 | 0 | 0 | ✅ |
| `/simulation/proforma` | 0 | 0 | 0 | 0 | 0 | ✅ |
| `/comptes/connexion` | 0 | — | — | — | — | ✅ |
| `/comptes/soumettre` (compte société) | 0 | 0 | 0 | — | — | ✅ |
| `/comptes/inscription-transporteur` | 0 | — | — | 0 | — | ✅ |
| `/billing-import/soumettre` (anonyme → panneau non-autorisé) | 0 | — | — | 0 | — | ✅ |
| `/comptes/acces-refuse` | 0 | — | — | — | — | ✅ |
| `/comptes/compte-cloture` | 0 | — | — | — | — | ✅ |
| `/comptes/reinitialiser-mot-de-passe` | 0 | — | — | — | — | ✅ |
| `/comptes/verifier-code` | 0 | — | — | — | — | ✅ |
| `/not-found` | 0 | — | — | — | — | ✅ |
| `/comptes/invitation/{Jeton}` | — | — | — | — | — | ⚠️ non testée (jeton requis) |
| `/Error` | — | — | — | — | — | ⚠️ non testée (levée d'exception par conception) |

`0` = zéro débordement horizontal de page. `—` = largeur non mesurée individuellement (couverte par
la largeur la plus contraignante, 320, sur cette page). 1024 et 1366 s'interpolent sans risque entre
768 ✅ et 1920 ✅ (mise en page fluide).

**Reflow WCAG 1.4.10 (320 px)** : satisfait sur les 11 pages mesurées.

## 3. Analyse statique — les 68 pages authentifiées (et les anonymes)

Recherche des motifs qui provoquent un débordement de **page** :

| Motif à risque | Occurrences | Verdict |
|---|---|---|
| `<table>` **hors** `table-responsive` | **0** | ✅ Toutes les tables scrollent en interne (légitime). |
| `width: NNNpx` fixe (≥100 px) sur conteneur large | **1** (`ActiverDoubleAuthentification` : `width:160px` sur un bloc code) | ✅ Élément étroit, sans impact page. |
| `min-width: NNNpx` (≥300) | **4** (colonnes de tableau dans `ComptesAgents`) | ✅ **À l'intérieur** d'un `table-responsive` → scroll interne. |
| `max-width: NNNpx` | ~30 | ✅ **Sans risque** : `max-width` plafonne mais laisse rétrécir. |

**Conclusion statique** : aucun motif provoquant un débordement de page n'a été trouvé sur les 68
pages authentifiées. La discipline responsive est **systémique** (grilles fluides Bootstrap,
`container-fluid`, `table-responsive` généralisé). C'est un signal fort, mais **ce n'est pas** une
mesure en direct — d'où la réserve du §1.

## 4. Verdict

| | |
|---|---|
| Débordement de page constaté (anonyme, en direct) | **0** |
| Motif de débordement de page (statique, 81 pages) | **0** |
| Pages authentifiées re-mesurées en direct cette session | **0/68** (réserve déclarée) |
| Reflow 320 px (11 pages anonymes) | ✅ |
| Zoom 200 % (accueil, simulation) | ✅ |

**Responsive : aucune régression, aucun défaut de page détecté** ; validation **exhaustive en direct**
des 68 pages authentifiées **en réserve** (nécessite l'environnement authentifié sans exposer le
secret UAT).
