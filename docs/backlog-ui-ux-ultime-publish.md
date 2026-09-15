# Backlog UI/UX ultime — après recette build publié

**Date** : 7 septembre 2026. Tests **1611/1611**. Score **94/100**. Aucun commit.

## Fermés ce tour (build publié)
- **D-ROUTE** : `/dossiers/{guid}` renvoyait 500 (AmbiguousMatchException — FicheDossier vs
  DetailDossier). Corrigé (DetailDossier → `/dossiers/{DossierId:guid}/operationnel`). Garde
  `RoutesUniques` + mutation. Flux dossier→ticket rétabli.
- **D-ENTETE** : en-tête authentifié débordait la page au mobile (320/390). Corrigé + garde.
- **D-SOMBRE** : 3 contrastes sombres (btn-outline-primary, avatar, btn-light). Corrigés + gardes.
- Responsive authentifié mesuré aux 6 largeurs + zoom 200 % → **10/10**.

## Restant — ce qui empêche 100

| ID | Catégorie | Détail |
|---|---|---|
| Sweep ~200 `style=` inline | TECHNIQUE (fond) | Design 14→15. Cosmétique, sans impact. |
| Audit WCAG formel | AUDIT FORMEL | Accessibilité 9→10 (NVDA/JAWS, clavier certifié). |
| D-04 DataProtection/antiforgery | INFRASTRUCTURE | Gestion erreurs. |
| AM-01/02/03 | MÉTIER | Parcours (durée séjour, périmètre collab.limité, confirmation transitions). |
| Cumul CSV | FAUX POSITIF | Gestion erreurs. |
| `!important` Bootstrap | RISQUE ACCEPTÉ | Qualité ; retrait = régression sombre. |
| Boutons marque 4,16:1 | DÉCISION (token) | Contraste ; impact identité. |

## Acquis solides (ne plus rouvrir)
- Mécanisme d'auth de recette UAT (sans secret, 4 barrières, impossible en Production) + build publié
  qui sert les CSS correctement.
- Recette visuelle authentifiée complète (responsive 6 largeurs, zoom, sombre, clavier) sur build
  publié.
- Sidebar par permissions (0 lien interdit, 6 profils) + enforcement serveur.
