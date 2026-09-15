# Audit accessibilité — état final

**Date** : 7 septembre 2026. Distinction stricte, comme dans les tours précédents :
**accessibilité technique corrigée et vérifiée** vs **audit lecteur d'écran formel restant**.
Aucune conformité WCAG globale n'est revendiquée sans la passe lecteur d'écran.

## 1. WCAG 1.4.11 — Contraste non textuel (champs de saisie) — PASS technique

**Corrigé et mesuré** ce cycle. La bordure des champs (input/select/textarea, cases à cocher,
radios) atteint désormais ≥ 3:1 dans les deux thèmes, contre chaque fond possible.

| État | Clair (avant → après) | Sombre (avant → après) |
|---|---|---|
| repos | 1,39 → **3,70** | 1,37 → **4,14** |
| sur panneau `--surface-2` | 1,30 → **3,48** | 1,26 → **3,81** |
| survol | 1,39 → **4,50** | 1,37 → **6,03** |
| focus | 4,16 → 4,16 | 4,23 → 4,23 |
| lecture seule | 1,39 → **3,70** | 1,37 → **4,14** |
| désactivé (exempté, conforme quand même) | 1,17 → **3,11** | 1,44 → **4,37** |
| erreur | 6,68 → 6,68 | 6,03 → 6,03 |
| succès (→ repos) | 1,39 → **3,70** | 1,37 → **4,14** |
| case à cocher / radio | 1,85 → **3,70** | conforme | 

**Preuves.**
- Valeurs **calculées par le moteur de rendu** (`getComputedStyle` sur `app.css` réel, thèmes clair
  et sombre chargés au démarrage) — pas seulement théoriques.
- Basculement de thème à chaud vérifié : la bordure se ré-résout correctement.
- Jeton dédié `--field-border` (+ `--field-border-hover`, `--field-border-focus`), distinct du filet
  décoratif `--line` (cartes, séparateurs, tableaux), lui exempté de 1.4.11.
- Garde `ContrasteChampsWcagTests` (6 tests) : vérifie la **valeur** (≥ 3:1, deux thèmes, tous fonds)
  ET le **câblage** (les règles de champ lisent bien le jeton, pas `--line`). Mutation-vérifiée.

## 2. Déjà corrigé et vérifié (tours précédents)

| Critère | État | Preuve |
|---|---|---|
| 1.3.1 titres / h1 | ✅ | garde `FinitionsUiTests`, 81/81 pages |
| 2.4.1 lien d'évitement | ✅ | premier tabbable, mesuré |
| 2.4.3 retour de focus modale | ✅ | `ModaleConfirmationFocusTests` |
| 2.4.7 focus visible | ✅ | garde `FocusVisibleTests` |
| 2.4.11 focus non masqué (scroll-padding) | ✅ | `scroll-padding-top` sur `html` |
| 1.4.3 contrastes texte | ✅ | mesures deux thèmes |
| 1.4.10 reflow 320 px | ✅ | pages anonymes, 0 débordement |
| 1.4.12 espacement du texte | ✅ | dashboard client |
| 4.1.3 messages d'état | ✅ | garde `AccessibiliteFormelleTests` (rôles `alert`/`status`) |

## 3. Confirmations accessibles (AM-03, ce cycle)

La `ModaleConfirmation` désormais câblée sur les transitions irréversibles porte : `role="dialog"`,
`aria-modal`, `aria-labelledby`/`aria-describedby`, focus initial sur le geste sûr (Annuler/Retour),
fermeture Échap, restitution du focus au déclencheur (2.4.3), neutralisation pendant traitement
(anti double-validation). Couvert par `ModaleConfirmationFocusTests`, `ModaleConfirmationVisibiliteTests`
et `DetailTicketConfirmationTests`.

## 4. Restant pour le 10ᵉ point — NON EXÉCUTABLE ici

Le 10ᵉ point exigeait **deux** conditions : (a) décision 1.4.11 et (b) passe lecteur d'écran formelle.

- (a) **1.4.11 : levée** (PASS technique, ci-dessus).
- (b) **Lecteur d'écran formel : NON EXÉCUTABLE DANS CET ENVIRONNEMENT.** NVDA/JAWS/VoiceOver
  indisponibles. Un scan DOM automatique n'est pas un audit lecteur d'écran.

Le point exige (a) **ET** (b). Une seule des deux étant satisfaite, le point **n'est pas attribué**.

**Accessibilité : 9/10** (inchangé). Le verrou passe de « décision + audit » à « audit lecteur
d'écran seul ».
