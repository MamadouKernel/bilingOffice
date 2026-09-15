# Sweep Design — fin (classification exhaustive + corrections)

**Date** : 7 septembre 2026. Tests **1612/1612**, 0 échec, 0 ignoré. Aucun commit.

> **Constat honnête** : les ~217 styles inline ne sont PAS ~200 dettes. Classés un par un, la quasi-
> totalité est **légitime** (A/B). La seule dette à **risque de thème** — 6 replis hexadécimaux morts
> derrière un token — a été corrigée et **gardée**.

## 1. Classification des 217 styles inline

| Classe | Nb | Traitement |
|---|---|---|
| **A — dynamique/légitime** : couleurs **tokenisées** `var(--action/surface/ink…)` (adaptent au thème) | **67** | Conservé (theme-safe). |
| **B — ponctuel acceptable** : dimensions/typographie one-off (`width`,`height`,`max-width`,`font-size`,`line-height`,`min-width`…) | **105** | Conservé (choix de mise en page par élément, sans couleur). |
| **B — décoratif theme-safe** : 5 teintes sur le dégradé sombre de l'accueil (blancs/ambre sur navy) + 2 voiles `rgba` (backdrop sombre, halo translucide) | **7** | Conservé (lisible dans les deux thèmes ; le dégradé est sombre par construction). |
| **B/combinés** : dimensions + `border-radius`/token combinés (cartes, modales) | **~32** | Conservé (tokenisés ou dimensionnels). |
| **E/F — repli hexadécimal mort derrière un token** (`var(--surface, #ffffff)`) | **6** | **CORRIGÉ → `var(--surface)`** (piège de thème + code mort). |

**Dette à risque de thème après sweep : 0.**

## 2. Correction appliquée (E/F)

`var(--surface, #ffffff)` et `var(--azure-600, #196b9f)` (×6, ComptesAgents) → repli hexadécimal
**retiré**. Le token existe toujours : le repli était **mort** ET un **piège** — si le token venait à
manquer, le fond redeviendrait CLAIR, y compris en sombre. La garde `FondsThematisesTests` ne le
voyait pas (elle ignore tout ce qui contient `var(--`). Nouveau garde-fou : `ReplisTokenInlineTests`
refuse tout `var(--jeton, #hex)` inline (**mutation vérifiée**).

Recette (ComptesAgents, la page modifiée) : panneaux = `--surface` — **clair** `#ffffff`, **sombre**
`#0e1a24` — 0 débordement à 1366 et 320, dans les deux thèmes. Le retrait est un **no-op visuel**
(le token résolvait déjà), sans régression.

## 3. Résidu assumé (NON un défaut)

- **Répétition de valeurs `border-radius` en rem** (0,75 / 1 / 1,25 / 1,5 rem). Theme-safe. Sa
  résolution propre est une **décision de design-system** (les tokens `--r-*` sont en px : 5/8/12 —
  échelle distincte). La convertir mécaniquement changerait le rendu OU ajouterait des tokens : hors
  périmètre d'un « corriger cette occurrence », et explicitement déconseillé par la consigne
  (« ne pas transformer mécaniquement »).
- **5 teintes codées en dur sur le dégradé accueil** : décoratives, lisibles dans les deux thèmes.

Aucun de ces éléments n'est un **défaut technique ouvert** (ni risque de thème, ni contraste, ni
débordement). Ce sont des raffinements de design-system, différés délibérément.

## 4. Bilan

Les styles inline **dynamiques/légitimes restent possibles** (on ne bannit pas l'inline). Seule la
classe réellement dangereuse (repli hex derrière token) est interdite et gardée. Le point Design est
fermé : **plus aucune dette technique à risque n'est ouverte.**
