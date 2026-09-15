# Audit UI/UX — score recalculé depuis les preuves

**Date** : 7 septembre 2026. Recalcul **après** corrections, tests (1594/1594) et recette navigateur.
Progression : 87 → 92 (tour précédent) → **93/100** (ce tour).

## 1. Score global — 93/100

| Critère | Note | Perte expliquée (preuve) |
|---|---|---|
| Design visuel | **14**/15 | −1 : ~200 `style=` inline résiduels (chantier de fond, sans impact fonctionnel). |
| Cohérence | **10**/10 | — |
| Navigation | **10**/10 | — (fil d'Ariane livré et gardé au tour précédent). |
| Formulaires | **10**/10 | — |
| Feedback | **10**/10 | — |
| Responsive | **9**/10 | −1 : 11 pages anonymes mesurées **en direct** (0 débordement, reflow 320) + 68 pages authentifiées **analysées statiquement** (0 motif de débordement), mais **non re-mesurées en direct** (secret UAT). Exhaustivité live non close. |
| Accessibilité | **9**/10 | −1 : trois contrastes 1.4.3 corrigés et vérifiés (badges/panneaux sombres, `.text-primary` clair par défaut, boutons ambre) ; focus visible/ordre OK ; mais **pas d'audit WCAG formel** (NVDA/JAWS, clavier complet sur pages authentifiées, zoom 200 % partout) ni `aria-hidden` emojis. |
| Gestion des erreurs | **8**/10 | −2 : D-04 (décision infra) + cumul CSV (faux positif D-22). |
| Parcours utilisateur | **9**/10 | −1 : AM-01/02/03 (décisions métier). |
| Qualité frontend | **4**/5 | −1 : `!important` requis par les utilitaires Bootstrap (retrait = régression sombre, démontré). |
| **TOTAL** | **93/100** | |

## 2. Le point gagné ce tour — justification

**Accessibilité 8 → 9.** Trois défauts de contraste **réels** (WCAG 1.4.3, niveau AA), constatés en
navigateur et **corrigés avec preuve mesurée dans les deux thèmes** :

| Défaut | Avant | Après |
|---|---|---|
| Pastilles/panneaux « subtle » en sombre | 2,9:1 | 5,16–8,73:1 |
| `.text-primary` / liens en **clair par défaut** (tout visiteur clair) | 3,16:1 | 8,06:1 |
| Boutons `btn-warning`/`btn-light.text-dark` en sombre | 1,84:1 | 7,7:1 |

Le 10ᵉ point exige un **audit WCAG formel** — non revendiqué sans lui.

## 3. Classement des points non obtenus

| Point | Catégorie |
|---|---|
| Responsive live des 68 pages authentifiées | **TECHNIQUE (nécessite env authentifié sans exposer le secret)** |
| Audit accessibilité formel | **AUDIT FORMEL** |
| Boutons de marque à 4,16:1 | **DÉCISION (design token `--action`)** |
| Sweep inline Design | **TECHNIQUE (fond, non bloquant)** |
| D-04 | **INFRASTRUCTURE** |
| AM-01 / AM-02 / AM-03 | **MÉTIER** |
| `!important` Bootstrap ; emojis décoratifs ; CSV | **RISQUE ACCEPTÉ / FAUX POSITIF** |

## 4. Régressions

| | |
|---|---|
| Régressions sécurité | **0** (aucune logique serveur touchée ; CSS + composants d'affichage). |
| Régressions fonctionnelles | **0** (1594/1594). |
| Régression introduite puis corrigée dans la session | **1** (débordement clair du 1ᵉʳ correctif D-C1) — signalée, corrigée, **gardée** (`ThemeSombreScopingTests`). |
