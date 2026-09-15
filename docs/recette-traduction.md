# Recette traduction — preuves

**Date** : 8 septembre 2026. Distinction stricte entre vérification navigateur, rendu composant et
tests. Rien n'est déclaré traduit sans preuve.

## 1. Reproduction du défaut (navigateur)

| Étape | Attendu (culture EN) | Observé | Verdict |
|---|---|---|---|
| Accueil, cookie EN posé + reload | interface anglaise | `<html lang="en">` ✅ mais titre + hero + CTAs **en français** | **KO couverture** |
| Cookie de culture | posé, persistant | `.AspNetCore.Culture=c=en-US|uic=en-US` présent | OK |
| `lang` HTML | `en` | `en` | OK |

**Cause** : ~92 % des pages et la quasi-totalité des chaînes (titres, enums, validation, dynamique)
sont codées en dur en français. Le mécanisme bascule ; le contenu non.

## 2. Correctif StatutBadge (rendu composant + tests)

| Scénario | Vérif | Résultat |
|---|---|---|
| Culture FR → libellés | `StatutBadgeTests` (5 cas) : « En attente », « En cours »… | ✅ |
| Culture EN → libellés | `StatutBadgeTests` (5 cas) : « Pending », « In progress », « Awaiting client », « Closed », « Cancelled » | ✅ |
| Rejet manifeste FR/EN | « Rejeté » / « Rejected » | ✅ |
| Parité dictionnaires | `TraductionParityTests` : mêmes clés FR/EN | ✅ |
| Statuts réellement bilingues | FR ≠ EN pour chaque `Statut.Ticket.*` | ✅ |
| Bascule localizer | FR « En cours » → EN « In progress » | ✅ |
| Mutation (énum brut) | 5 tests tombent, puis restauré | ✅ |

## 3. Parcours public rejoué (navigateur, sans authentification)

| Parcours | Vérif | Résultat |
|---|---|---|
| Simulation proforma publique `/simulation/proforma` | rendu, formulaire + bouton « Calculer la proforma » | ✅ |
| `<html lang>` / titre | `fr` / « Simulateur Tarifaire Proforma… » (page localisée) | ✅ |
| Responsive 320 px | débordement horizontal = **0 px** | ✅ |
| Thème sombre | `data-theme=dark`, fond `#070f16`, bordure de champ `#6b7d8c` (WCAG 1.4.11 conforme) | ✅ |

## 4. Non rejoué ce cycle (honnêteté de portée)

- **Recette traduction authentifiée** (client / agent / admin) : le commutateur n'existe que dans
  l'en-tête authentifié ; la recette multi-persona n'a pas été rejouée (coût d'authentification).
- **Traduction EN des 93 pages en dur** : non réalisée (chantier — voir feuille de route).

Ces éléments **ne sont pas comptés comme PASS**. Aucune régression introduite : le seul changement
UI (StatutBadge) rend en FR par défaut exactement comme avant.
