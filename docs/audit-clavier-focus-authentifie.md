# Audit clavier / focus — pages authentifiées (build publié)

**Date** : 7 septembre 2026. Observations RÉELLES (activeElement, styles calculés) sur le build
publié.

| Aspect | Constat |
|---|---|
| Focus visible | Tab sur le dashboard client → `.btn-outline-secondary` avec `outline: 2px solid rgb(33,131,189)`, `:focus-visible` actif. Repère visible sur les contrôles authentifiés. |
| Lien d'évitement | Premier élément tabbable (vérifié tours précédents ; markup partagé). |
| Ordre de tabulation | Logique observée sur le dashboard (skip-link → en-tête → contenu). |
| Retour de focus modale (2.4.3) | Couvert par `ModaleConfirmationFocusTests` (memoriser/restaurer) ; comportement identique aux pages anonymes déjà vérifiées. |
| Focus initial / Échap modale | Couvert par `ModaleConfirmationVisibiliteTests` + `…FocusTests`. |

**NON couvert (honnête)** : parcours clavier COMPLET (Tab/Shift+Tab/Enter/Space/Escape) sur CHAQUE
formulaire et modale authentifiés, et test au **lecteur d'écran** (NVDA/JAWS). Ces éléments relèvent
de l'**audit WCAG formel** — non réalisé. Accessibilité reste à 9/10 pour cette raison.
