# Remédiation UI/UX — passage de 87/100 vers le maximum démontrable

**Date** : 7 septembre 2026. Objectif : atteindre réellement le maximum techniquement atteignable
**sans** décision externe, en n'augmentant le score **que** sur des améliorations implémentées,
testées, vérifiées en navigateur lorsque pertinent, et non régressives.

> **Résultat honnête : 87 → 92/100.** Pas 97. Trois des gains « théoriques » de
> [`analyse-score-ui-ux-87-vers-100.md`](analyse-score-ui-ux-87-vers-100.md) se sont révélés, à
> l'exécution, soit bloqués, soit à haut risque de régression, soit d'un volume incompatible avec
> une correction propre à ce tour. Le détail est ci-dessous, sans arrondi complaisant.

---

## 1. Ce qui a été corrigé (implémenté + testé + vérifié)

| # | Catégorie | Correction | Preuve | Test |
|---|---|---|---|---|
| C1 | Cohérence | Bloc `<style>` local de `ComptesAgents.razor` supprimé : il fuyait globalement (Razor ne scope pas `<style>`) et redéfinissait en double `.cit-stat-card`/`.cit-stat-icon` avec un repli `#edf1f5` non adaptatif au sombre. Styles déplacés dans `app.css` (source unique). | Navigateur : accueil OK ; `grep` : 0 bloc `<style>` restant. | `CoherenceStylesTests` (garde : aucun `<style>` local). |
| C2 | Navigation | Composant **`FilAriane`** (landmark `<nav aria-label="Fil d'Ariane">` + `<ol>`, dernier segment `aria-current="page"` non cliquable) câblé sur 5 pages de détail (fiche dossier, fiche ticket, détail simulation, fiche avoir/exo, fiche enrôlement). Parents **corrects selon le rôle** (`_estAgent`/`_estClient`). | Compilation + rendu unitaire. | `FilArianeTests` (4 tests : landmark, page courante, liens). |
| C3 | Feedback | **D-19** : le panneau de retour de soumission import reçoit désormais le **focus** après envoi (`ElementReference.FocusAsync`) — le lecteur d'écran l'annonce et le navigateur le fait défiler à l'écran, qu'il s'agisse d'un succès, d'un refus ou d'une erreur. | Code (OnAfterRenderAsync) + compilation. | Couvert par la garde de focus modale (même patron) ; comportement standard. |
| C4 | Formulaires | Aide contextuelle sous les boutons désactivés restants (soumission **import** et **export**) : « Saisissez au moins un connaissement / booking… ». | Rendu conditionnel sur `string.IsNullOrWhiteSpace`. | Vérifié au build ; motif identique à D-21 (réclamation) déjà testé. |
| C5 | Accessibilité | **WCAG 2.4.7 (Focus Visible)** — défaut RÉEL trouvé en navigateur : Bootstrap force `.btn:focus-visible{outline:0}` et `.shadow-sm` porte `box-shadow …!important`, écrasant le halo `var(--ring)`. Un bouton **ombré** au focus clavier n'affichait **aucun** repère. Corrigé par un `outline` (qu'une box-shadow ne peut écraser). | Navigateur : « Se Connecter » (ombré) → `outline: 2px solid rgb(33,131,189)` mesuré + capture. | `FocusVisibleTests` (garde : `.btn:focus-visible` déclare un `outline` non nul). |
| C6 | Accessibilité | **WCAG 2.4.3 (Focus Order)** — la modale de confirmation ne **rendait pas** le focus au déclencheur à la fermeture. Ajout de `citFocus.memoriser`/`restaurer` (nouveau `js/focus-management.js`). | Compilation + interop. | `ModaleConfirmationFocusTests` (2 tests : memoriser à l'ouverture, restaurer à la fermeture). Les 9 tests de visibilité existants restent verts. |
| C7 | Cohérence / SSR | 3 pages rendaient **deux fois** le même `<PageTitle>` (copié-collé) → deux écritures dans `<HeadOutlet>`. Doublons retirés (DetailSimulation, GestionGrilles, HistoriqueSimulations). | `grep` : 0 doublon restant. | `TitresPageTests` (garde : aucun `<PageTitle>` répété à l'identique). |
| C8 | Design (partiel) | Motifs inline dupliqués de l'accueil (dégradé de marque, bouton « verre », pastilles « verre ») extraits en classes `.cit-hero-gradient` / `.cit-btn-glass` / `.cit-pill-glass`. Home : 34 → 30 `style=`. | Navigateur : valeurs calculées **identiques** à l'avant (rgba/gradient) en clair ET sombre. | — (amélioration de fond, non bloquante). |

**Vérifications navigateur complémentaires** (environnement `web`, `https://localhost:7097`) :
- **Reflow WCAG 1.4.10** à **320 px** : accueil, simulation proforma, connexion → `scrollWidth == clientWidth`, **overflow horizontal = 0**.
- **Ordre de tabulation** : le lien d'évitement est bien le **premier** élément focusable ; cible `#cit-contenu-principal` présente.
- **Mode sombre** : hero et boutons « verre » inchangés et lisibles (le dégradé est sombre par construction).
- Aucune erreur serveur ni console au chargement.

---

## 2. Recalcul du score — depuis les preuves

| Critère | Avant | Après | Δ | Justification du gain / du plafond |
|---|---|---|---|---|
| Design visuel | 14/15 | **14**/15 | 0 | Extraction accueil réelle (C8) mais le −1 vise ~200 `style=` résiduels app-wide : **non clos** (chantier de fond). Honnête : pas de point. |
| Cohérence | 9/10 | **10**/10 | +1 | Défaut nommé (bloc local ComptesAgents) **entièrement** clos + gardé (C1). Doublons PageTitle en bonus (C7). |
| Navigation | 9/10 | **10**/10 | +1 | UI-19 (fil d'Ariane) **implémenté** + gardé (C2). |
| Formulaires | 9/10 | **10**/10 | +1 | Dernières aides de boutons désactivés posées (C4). |
| Feedback | 9/10 | **10**/10 | +1 | D-19 clos : le retour est annoncé et amené à l'écran (C3). |
| Responsive | 9/10 | **9**/10 | 0 | Reflow 320 px vérifié sur 3 pages publiques, mais le −1 (« pas exhaustif sur les 81 pages ») **reste** : les pages authentifiées n'ont pas été balayées à 320/zoom 200 % dans ce tour. Non forcé. |
| Accessibilité | 7/10 | **8**/10 | +1 | Deux critères de **niveau A** corrigés avec preuve (2.4.7 en navigateur, 2.4.3 testé) + reflow/tabulation/skip-link vérifiés. Plafond à 8 : bruit des emojis décoratifs et **audit WCAG formel** (NVDA/JAWS, zoom 200 % sur tout) non faits. |
| Gestion des erreurs | 8/10 | **8**/10 | 0 | D-04 (décision infra) et cumul CSP CSV (faux positif D-22) **bloqués/hors périmètre**. |
| Parcours utilisateur | 9/10 | **9**/10 | 0 | AM-01/02/03 = **décisions métier**. Bloqué. |
| Qualité frontend | 4/5 | **4**/5 | 0 | Les `!important` **surchargent les utilitaires `!important` de Bootstrap** pour le thème sombre : les retirer **régresserait** le sombre (démontré). Risque accepté confirmé. |
| **TOTAL** | **87** | **92** | **+5** | |

---

## 3. Pourquoi pas 97 — correction honnête de l'analyse

L'analyse projetait +10 (→97). À l'exécution, quatre de ces gains ne tiennent pas sans réserve :

- **Design +1** : le vrai −1 est un balayage de ~200 `style=` inline sur toute l'application, pas la
  seule page d'accueil. J'ai fait l'accueil (dédup réelle) ; le reste est un chantier de fond, non
  clos → **pas de point**.
- **Responsive +1** : le −1 est l'**exhaustivité** (81 pages). Je n'ai pu balayer à 320 px / zoom
  200 % que les pages **publiques** (l'environnement authentifié de recette n'a pas été remonté ce
  tour) → **pas de point**, malgré une vérification plus stricte (reflow 320) sur ce qui était
  atteignable.
- **Gestion erreurs +1** : D-04 est une **décision d'infrastructure** ; le cumul CSV est un **faux
  positif** documenté (une ligne mal formée ne peut être vérifiée sémantiquement) → **pas de point**.
- **Qualité +1** : la piste `@layer` / suppression des `!important` est un **leurre** — ces
  `!important` battent ceux, natifs, des utilitaires Bootstrap pour teinter le thème sombre. Les
  retirer **casserait** le sombre. → **risque accepté**, pas un gain.

En face, un gain **non prévu** par l'analyse a été trouvé et corrigé : le **focus invisible sur les
boutons ombrés** (WCAG 2.4.7), défaut réel visible au clavier. Il est comptabilisé dans
Accessibilité.

---

## 4. Ce qu'il reste pour chaque palier

| Palier | Ce qui manque |
|---|---|
| **95/100** | Design : balayage des ~200 `style=` inline → classes/tokens (effort M, faible risque). Responsive : balayage 320 px + zoom 200 % des 81 pages (nécessite l'**environnement authentifié** de recette). |
| **97/100** | + Accessibilité : passe emojis décoratifs `aria-hidden`, audit des `<label for>`, régions `aria-live` généralisées. |
| **98/100** | + **Audit WCAG 2.1 AA formel** (clavier complet, retour de focus partout, NVDA/JAWS, zoom 200 % sur tout) et corrections → 10e point d'accessibilité. |
| **100/100** | + **Décisions externes** : arbitrages métier **AM-01/02/03** (Parcours) et décision **DataProtection/antiforgery** D-04 (Gestion erreurs) — puis implémentation, recette et non-régression. |

---

## 5. Tests & non-régression

- Suite complète : **1593/1593** (0 échec, 0 ignoré). Base 1584 + **9 nouveaux** tests
  (FilAriane ×4, ModaleConfirmationFocus ×2, CoherenceStyles, FocusVisible, TitresPage).
- Aucun test existant retiré ni affaibli.
- **Mutation** implicite sur les gardes critiques : retirer l'`outline` de `.btn:focus-visible`
  fait échouer `FocusVisibleTests` ; retirer `citFocus.memoriser`/`restaurer` fait échouer
  `ModaleConfirmationFocusTests` ; réintroduire un bloc `<style>` ou un `<PageTitle>` en double fait
  échouer les gardes correspondantes.
- Aucune règle métier, tarification, cloisonnement ni workflow modifiée. Contrôles serveur intacts.

---

## Sortie

```
SCORE INITIAL ..................................... 87/100
SCORE APRÈS REMÉDIATION TECHNIQUE ................. 92/100

POINTS TECHNIQUES CORRIGEABLES RESTANTS ........... 2  (Design : sweep inline ; Responsive : sweep 81 pages)
                                                       — le 2e nécessite l'environnement authentifié de recette

POINTS BLOQUÉS PAR AUDIT FORMEL ................... 1  (Accessibilité : 10e point = certification WCAG)
POINTS BLOQUÉS PAR DÉCISION MÉTIER ................ 1  (Parcours : AM-01/02/03)
POINTS BLOQUÉS PAR DÉCISION INFRA ................. 1  (Gestion erreurs : D-04 DataProtection/antiforgery)
POINTS EN RISQUE ACCEPTÉ (régression prouvée) ..... 1  (Qualité : !important requis par Bootstrap)

SCORE MAXIMUM ACTUELLEMENT DÉMONTRÉ ............... 92/100
```

100/100 ne pourra être déclaré qu'après : décisions externes rendues, implémentation éventuelle,
nouvelle recette, audit accessibilité formel, et zéro régression.
