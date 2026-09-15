# Audit UI/UX — recalcul après recette authentifiée

**Date** : 7 septembre 2026. Tests : **1606/1606** (baseline 1594 + 12 tests du mécanisme UAT),
0 échec, 0 ignoré. HEAD `1c3cd38`, aucun commit.

## 1. Score — 93/100 (inchangé, et pourquoi)

Le score **ne bouge pas** ce tour. Ce n'est pas un échec : le tour a livré une **infrastructure**
(l'authentification de recette) et une **preuve de sécurité live**, mais aucun **aspect visuel
mesurable** nouveau — et la recette visuelle authentifiée, qui seule aurait pu débloquer Responsive
ou Accessibilité, est restée **bloquée** (harnais, §6 du rapport principal). Conformément à la
consigne, on n'attribue pas de point sur du non-mesuré.

| Critère | Note | Justification |
|---|---|---|
| Design visuel | **14**/15 | −1 : styles inline résiduels (chantier de fond). Voir [`sweep-design-final.md`](sweep-design-final.md). |
| Cohérence | **10**/10 | — |
| Navigation | **10**/10 | Confirmée **en direct** ce tour : sidebar filtrée sur 6 profils, 0 lien interdit + enforcement serveur. Déjà à 10 ; l'évidence passe de « tests » à « navigateur ». |
| Formulaires | **10**/10 | — |
| Feedback | **10**/10 | — |
| Responsive | **9**/10 | −1 : **inchangé**. Les 68 pages authentifiées **n'ont pas pu** être mesurées visuellement (CSS non servi au navigateur de prévisualisation, §6). Tentée, **NON aboutie** — donc NON TESTÉ, pas de point. |
| Accessibilité | **9**/10 | −1 : audit WCAG formel ; recette clavier/contraste authentifiée **NON TESTÉE** (même blocage). |
| Gestion des erreurs | **8**/10 | −2 : D-04 (infra) + CSV (faux positif). |
| Parcours utilisateur | **9**/10 | −1 : AM-01/02/03 (métier). |
| Qualité frontend | **4**/5 | −1 : `!important` requis par Bootstrap. |
| **TOTAL** | **93/100** | |

## 2. Ce que le tour a réellement apporté (hors barème)

- **Mécanisme d'authentification de recette UAT** — sans secret exposé, 4 barrières cumulatives,
  impossible en Production. Testé (12) + mutation + **prouvé en direct** (valide → session ;
  désactivé → refusé sans cookie ; inexistant/hors-domaine → 404).
- **Recette sécurité authentifiée live** : sidebar par profil (0 lien interdit), enforcement serveur
  (accès direct refusé pour l'agent sans droits), comptes négatifs conformes.
- **Fermeture du « principal trou de preuve »** côté **sécurité/navigation** (le cloisonnement est
  désormais observé en navigateur, plus seulement en tests).

## 3. Points non obtenus — classement

| Point | Catégorie |
|---|---|
| Responsive/dark/zoom/clavier **des pages authentifiées** | **TECHNIQUE (bloqué par le harnais)** — nécessite un `dotnet publish` ou un navigateur gérant la variante compressée |
| Audit WCAG formel | **AUDIT FORMEL** |
| Sweep inline Design | **TECHNIQUE (fond)** |
| Boutons de marque 4,16:1 | **DÉCISION (token `--action`)** |
| D-04 | **INFRASTRUCTURE** |
| AM-01/02/03 | **MÉTIER** |
| `!important` Bootstrap, emojis, CSV | **RISQUE ACCEPTÉ / FAUX POSITIF** |

## 4. Régressions
| | |
|---|---|
| Sécurité | **0** — le mécanisme UAT ne crée aucune permission, est impossible en Production, respecte la politique d'accès (compte désactivé refusé). Contrôles serveur intacts. |
| Fonctionnelles | **0** (1606/1606). |
| Introduites puis retirées ce tour | 2 tentatives de contournement CSS (middleware Accept-Encoding UAT ; flag de build) — **infructueuses, retirées** (pas de code mort). |
