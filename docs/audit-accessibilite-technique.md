# Audit accessibilité technique

**Date** : 7 septembre 2026. Distinction stricte : **accessibilité technique corrigée** vs
**certification / audit WCAG formel restant**. Aucune conformité WCAG n'est revendiquée sans audit.

## 1. Corrigé et vérifié (tours précédents, pages anonymes)

| Critère WCAG | État | Preuve |
|---|---|---|
| 1.3.1 titres / h1 | ✅ | 81/81 pages, garde `FinitionsUiTests` |
| 2.4.1 lien d'évitement | ✅ | premier tabbable, mesuré |
| 2.4.3 retour de focus modale | ✅ | `citFocus.memoriser/restaurer`, `ModaleConfirmationFocusTests` |
| 2.4.7 focus visible | ✅ | outline sur `.btn:focus-visible`, mesuré (bouton ombré) + garde `FocusVisibleTests` |
| 1.4.3 contrastes (clair + sombre) | ✅ | 3 défauts réels corrigés (badges subtle, `.text-primary` clair par défaut, boutons ambre) — mesures dans les deux thèmes |
| 1.4.10 reflow 320 px | ✅ | 11 pages anonymes, 0 débordement |
| 2.5.3 nom accessible | ✅ (partiel) | boutons porteurs de texte visible (audit du tour « max technique ») |

## 2. NON TESTÉ ce tour (pages authentifiées — blocage harnais)

Le navigateur de prévisualisation ne reçoit pas les CSS du serveur UAT
([`recette-authentifiee-exhaustive.md`](recette-authentifiee-exhaustive.md) §6). Sans styles, ni le
focus visible, ni les contrastes, ni le reflow des pages authentifiées ne sont observables de façon
représentative.

| Aspect | Pages authentifiées |
|---|---|
| Navigation clavier (Tab/Shift+Tab/Enter/Escape) sur parcours agent/admin | **NON TESTÉ** |
| Focus visible sur composants authentifiés | **NON TESTÉ** |
| Contrastes thème sombre (dashboards, tables, badges de statut) | **NON TESTÉ** |
| Reflow 320 px / zoom 200 % des pages authentifiées | **NON TESTÉ** |

Ce qui a **quand même** été observé au niveau DOM (indépendant du CSS) : `aria-current`, landmarks
`<nav>` (fil d'Ariane), structure — présents. Mais l'observation **visuelle** manque.

## 3. Restant pour le 10ᵉ point d'accessibilité

- Audit **WCAG 2.1 AA formel** : lecteur d'écran (NVDA/JAWS/VoiceOver), clavier complet sur pages
  authentifiées, zoom 200 % sur toutes les pages.
- `aria-hidden` sur les emojis purement décoratifs (risque accepté, faible valeur — non fait).
- Recette visuelle authentifiée (déblocage harnais requis).

**Accessibilité : 9/10** — le 10ᵉ point exige l'audit formel ET la recette visuelle authentifiée.
