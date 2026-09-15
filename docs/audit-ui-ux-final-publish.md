# Audit UI/UX — recalcul après recette authentifiée sur build publié

**Date** : 7 septembre 2026. Tests **1611/1611**, 0 échec, 0 ignoré. Build publié, env UAT,
base CIT_UAT_RECETTE. Aucun commit.

## 1. Score — 94/100

| Critère | Note | Justification (preuve) |
|---|---|---|
| Design visuel | **14**/15 | −1 : ~200 `style=` inline résiduels (chantier de fond). |
| Cohérence | **10**/10 | — |
| Navigation | **10**/10 | Sidebar filtrée live (6 profils, 0 lien interdit) + flux dossier→ticket **rétabli** (D-ROUTE). |
| Formulaires | **10**/10 | — |
| Feedback | **10**/10 | — |
| Responsive | **10**/10 | **+1** : 6 largeurs MESURÉES sur pages authentifiées (comptes-agents 320→1920 = 0) + zoom 200 % + le seul défaut trouvé (en-tête mobile) **corrigé**. Plus d'interpolation, plus de « non testé ». |
| Accessibilité | **9**/10 | −1 : recette clavier/contraste authentifiée FAITE (3 contrastes sombres corrigés, focus visible live, reflow 320) ; reste l'**audit WCAG formel** (NVDA/JAWS, certification) — non réalisé. |
| Gestion des erreurs | **8**/10 | −2 : D-04 (infra) + cumul CSV (faux positif). |
| Parcours utilisateur | **9**/10 | −1 : AM-01/02/03 (décisions métier). |
| Qualité frontend | **4**/5 | −1 : `!important` requis par Bootstrap (retrait = régression sombre, démontré). |
| **TOTAL** | **94/100** | |

## 2. Le point gagné — Responsive 9 → 10

Au tour précédent, Responsive restait à 9 car les pages authentifiées n'étaient pas mesurables
(CSS non servi). Le **build publié** l'a permis. Résultat, MESURÉ (aucune interpolation) :

- comptes-agents (la plus dense) : 320/390/768/1024/1366/1920 = **0** ; zoom 200 % (~683) = 0.
- Pages client à 320 = 0 ; ~35 pages authentifiées traversées à 1366 = 0 débordement.
- Le **seul** défaut responsive trouvé (en-tête mobile, +188 px à 320) est **corrigé et gardé**.

## 3. Ce qui empêche 100/100 (exactement)

| Manque | Catégorie | Détail |
|---|---|---|
| Sweep des ~200 `style=` inline | **A — TECHNIQUE (fond)** | Sans impact fonctionnel ; risque cosmétique. Design 14→15. |
| Audit WCAG formel (NVDA/JAWS, clavier certifié) | **D — AUDIT FORMEL** | Accessibilité 9→10. |
| D-04 (DataProtection/antiforgery, message session expirée) | **C — INFRASTRUCTURE** | Gestion erreurs. |
| Cumul CSV syntaxe+référentiel | **A/faux positif** | Gestion erreurs — une ligne mal formée ne peut être vérifiée sémantiquement. |
| AM-01 / AM-02 / AM-03 | **B — MÉTIER** | Parcours. |
| Retrait des `!important` Bootstrap | **E — RISQUE ACCEPTÉ** | Qualité ; retrait = régression sombre (démontré). |
| Boutons de marque 4,16:1 | **B — DÉCISION (token --action)** | Contraste ; impact identité. |

**94/100 est le maximum démontré.** Les 6 points restants relèvent d'un sweep de fond (1),
d'un audit formel (1), de décisions infra (1) et métier (2), et d'un risque accepté (1).

## 4. Régressions

| | |
|---|---|
| Sécurité | **0** (mécanisme UAT impossible en Production, aucune permission créée ; route corrigée sans toucher au contrôle d'accès central). |
| Fonctionnelles | **0** (1611/1611). Le correctif D-ROUTE **restaure** une fonctionnalité cassée (dossiers). |
| Introduites puis retirées | 2 tentatives de contournement CSS du tour précédent (déjà retirées). |
