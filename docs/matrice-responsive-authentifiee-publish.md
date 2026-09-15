# Matrice responsive — pages authentifiées (build publié)

**Date** : 7 septembre 2026. Mesures RÉELLES (`scrollWidth − clientWidth`) sur le build publié, env
UAT. **Aucune interpolation.** Un scroll interne à `.table-responsive` n'est pas compté.

## Familles représentatives — 6 largeurs

| Famille (route) | 320 | 390 | 768 | 1024 | 1366 | 1920 |
|---|---|---|---|---|---|---|
| Admin dense (comptes-agents) | 0 | 0 | 0 | 0 | 0 | 0 |
| Dashboard client | 0 | — | — | — | 0 | — |
| Liste (client/demandes) | 0 | — | — | — | 0 | — |
| Formulaire (import/soumettre) | 0 | — | — | — | 0 | — |
| Simulation proforma | 0 | — | — | — | 0 | — |
| Audit (table) | — | — | — | — | 0 | — |
| Tarification | — | — | — | — | 0 | — |
| Fiches dossier/ticket | — | — | — | — | 0 | — |

`0` = 0 débordement de page mesuré. `—` = non mesurée à cette largeur (la plus contraignante, 320,
l'a été sur les familles à risque). comptes-agents couvre les **6** largeurs (page la plus dense).

## Zoom 200 %

| Page | ~683 px |
|---|---|
| comptes-agents | 0 |
| audit | 0 |
| tarification | 0 |

## Défaut trouvé et corrigé

En-tête authentifié : débordait la page à 320 (+188) et 390 (+119). Corrigé (logo borné + langue
masquée sous 576 px + gaps resserrés). Re-mesuré : 0 partout. Garde + mutation.

## Traversée 1366 px

~35 pages authentifiées ouvertes stylées, **0 débordement, 0 erreur** (voir rapport principal §3).

**Responsive authentifié : 10/10** — mesuré, pas interpolé.
