# Pourquoi 87/100 et non 100/100 — analyse traçable + feuille de route

**Date** : 7 septembre 2026. Aucune modification de code dans cette analyse.
Chaque perte de point est rattachée à une cause précise, une preuve, un correctif, un effort et un
jugement de faisabilité. Aucune décote « peut être amélioré ».

---

## 1. Décomposition par catégorie

### Design visuel — 14 / 15 (−1)

| # | Points perdus | Cause exacte | Preuve | Correctif | Effort | Max atteignable ? |
|---|---|---|---|---|---|---|
| 1 | −1 | ~200 attributs `style=` inline subsistent (couleurs/espacements en dur hors thème). | `grep -rn 'style=' src/…/Pages` : Home 30+, SoumettreDemande, InscriptionTransporteur, ComptesAgents. | Promouvoir les motifs récurrents en classes `cit-` / tokens. | **M** (1-3 j) | **OUI** — purement technique. |

### Cohérence — 9 / 10 (−1)

| # | Points perdus | Cause | Preuve | Correctif | Effort | Max ? |
|---|---|---|---|---|---|---|
| 1 | −1 | `ComptesAgents.razor` porte un bloc `<style>` local (vocabulaire visuel propre à une page) ; les autres pages n'en ont pas. | `ComptesAgents.razor` : bloc `<style>` scoped. | Extraire les classes utiles dans `app.css` (tokens déjà appliqués), retirer le bloc local. | **S** (½ j) | **OUI**. |

### Navigation — 9 / 10 (−1)

| # | Points perdus | Cause | Preuve | Correctif | Effort | Max ? |
|---|---|---|---|---|---|---|
| 1 | −1 | Pas de fil d'Ariane sur les fiches/détails : depuis une fiche ticket atteinte via un dossier, la hiérarchie n'est pas rappelée (UI-19). | `FicheTicket.razor`, `FicheDossier.razor`, `DetailSimulationPage.razor` : aucun breadcrumb. | Composant `FilAriane` (Accueil › Dossier › Ticket) sur les pages de détail. | **M** | **OUI** — feature, pas décision. |

### Formulaires — 9 / 10 (−1)

| # | Points perdus | Cause | Preuve | Correctif | Effort | Max ? |
|---|---|---|---|---|---|---|
| 1 | −1 | Quelques boutons désactivés n'expliquent pas leur condition hors des cas déjà traités (catégories, réclamation). Ex. soumission BL, soumission export. | `SoumettreDemande.razor` (import) : `disabled=@(string.IsNullOrWhiteSpace(...))` sans aide ; idem export. | Généraliser l'aide contextuelle sous chaque bouton désactivé (motif déjà en place ailleurs). | **S** | **OUI**. |

### Feedback — 9 / 10 (−1)

| # | Points perdus | Cause | Preuve | Correctif | Effort | Max ? |
|---|---|---|---|---|---|---|
| 1 | −1 | Le panneau de succès de soumission import s'affiche **sous** le formulaire, hors écran à petite hauteur ; pas de défilement automatique (D-19). | `SoumettreDemande.razor` : `_resultat` rendu après l'`EditForm`. | Après succès, remplacer le formulaire par le résultat, **ou** `scrollIntoView` (interop). | **S** | **OUI**. |

### Responsive — 9 / 10 (−1)

| # | Points perdus | Cause | Preuve | Correctif | Effort | Max ? |
|---|---|---|---|---|---|---|
| 1 | −1 | Matrice large vérifiée (390→1920, 0 débordement) mais **pas exhaustivement sur les 81 pages** ni en zoom 200 %. | Recette : 8 pages mesurées à 5 tailles ; zoom 200 % non testé. | Balayage des 81 pages + test zoom 200 % (reflow WCAG 1.4.10). | **M** | **OUI** (technique). |

### Accessibilité — 7 / 10 (−3)

Barème strict (jamais 10/10 sans vérification). Évaluation par exigence :

| Exigence | État | Preuve |
|---|---|---|
| Structure des titres (h1) | ✅ | 81/81 pages, garde `FinitionsUiTests`. |
| Lien d'évitement | ✅ | `.cit-skip-link` → `#cit-contenu-principal`. |
| Focus initial des modales | ✅ | `ModaleConfirmation` `FocusAsync` sur Annuler. |
| Échap sur modales | ✅ | testé (bUnit + recette). |
| Contrastes (texte normal) | ✅ AA | mesurés : accueil 15:1, dashboard 0 sous seuil. |
| Cibles tactiles | ✅ | `@media (pointer: coarse)` ≥ 44 px. |
| Reflow / mobile | ✅ | 390→1920, 0 débordement. |
| Messages dynamiques (aria-live) | ⚠️ partiel | toast `role=status aria-live=polite` ✅ ; alertes inline non systématiquement annoncées. |
| Labels de formulaire | ⚠️ partiel | la plupart des champs ont `<label for>` ; non audité exhaustivement. |
| Boutons icône (aria-label) | ⚠️ partiel | thème ✅ ; emojis décoratifs non `aria-hidden`. |
| Navigation clavier **complète** | ❌ non vérifié | pas de parcours TAB/SHIFT+TAB/ENTER/SPACE exhaustif. |
| Retour du focus après fermeture | ❌ non vérifié | non instrumenté. |
| Lecteur d'écran (NVDA/JAWS/VoiceOver) | ❌ non testé | aucun test lecteur d'écran réalisé. |
| Zoom 200 % | ❌ non testé | non instrumenté. |

| # | Points perdus | Cause | Correctif | Effort | Max ? |
|---|---|---|---|---|---|
| 1 | −1 | Emojis décoratifs sans `aria-hidden` ; quelques boutons icône sans `aria-label`. | Balayage `aria-hidden="true"` sur emojis décoratifs ; `aria-label` sur les rares boutons icône. | **M** | **OUI** (technique). |
| 2 | −1 | Messages dynamiques inline (succès/erreur) pas tous dans une région live ; labels non audités. | Envelopper les zones de retour dans `aria-live` ; audit des `<label for>`. | **M** | **OUI** (technique). |
| 3 | −1 | **Aucun audit formel** : clavier complet, retour de focus, lecteur d'écran, zoom 200 %. | Passe d'audit WCAG 2.1 AA (clavier + NVDA + zoom) et corrections. | **L** (> 3 j) | **OUI mais nécessite un audit formel** — le 10ᵉ point ne s'obtient qu'après cette passe. |

### Gestion des erreurs — 8 / 10 (−2)

| # | Points perdus | Cause | Preuve | Correctif | Effort | Max ? |
|---|---|---|---|---|---|---|
| 1 | −1 | Page antiforgery brute en **Development/UAT** (D-04). En prod, écran `/Error` générique — pas un message *spécifique* « session expirée ». | Recette (tour P0) : page framework brute. | Message spécifique + persistance clés DataProtection. | **M** | **DÉCISION INFRA** — la part applicative est une modification du pipeline de sécurité. |
| 2 | −1 | Parseur CSV : erreurs de syntaxe et de référentiel en deux phases (non cumulées dans le même rapport) (D-22). | `ImportateurGrilleTarifaireCsv` + `Construire`. | Cumuler les deux quand les lignes fautives sont syntaxiquement valides. | **S** | **PARTIEL** — une ligne mal formée ne **peut** être vérifiée sémantiquement (limite intrinsèque). |

### Parcours utilisateur — 9 / 10 (−1)

| # | Points perdus | Cause | Preuve | Correctif | Effort | Max ? |
|---|---|---|---|---|---|---|
| 1 | −1 | Trois incohérences de parcours non tranchées : durée de séjour 14 vs 15 j (AM-01), `collab.limite` = `client.normal` (AM-02), « Valider Booking » sans confirmation (AM-03). | Recette multi-rôles. | Appliquer les règles **après décision métier**. | **S** (une fois décidé) | **NON sans décision métier**. |

### Qualité frontend — 4 / 5 (−1)

| # | Points perdus | Cause | Preuve | Correctif | Effort | Max ? |
|---|---|---|---|---|---|---|
| 1 | −1 | 80 `!important` dans `app.css` (lutte de spécificité contre Bootstrap) (UI-17). | `grep -c '!important' app.css` = 80. | Refonte de la cascade (couches CSS `@layer`) pour retirer les `!important`. | **L** | **OUI mais risque de régression élevé** — à faire avec une campagne de non-régression visuelle. |

---

## 2. Synthèse des points récupérables

| Catégorie | Perte | Récupérable sans décision externe | Bloqué par décision externe |
|---|---|---|---|
| Design visuel | −1 | **+1** (refactor inline) | — |
| Cohérence | −1 | **+1** (styles locaux) | — |
| Navigation | −1 | **+1** (fil d'Ariane) | — |
| Formulaires | −1 | **+1** (aides boutons) | — |
| Feedback | −1 | **+1** (scroll résultat) | — |
| Responsive | −1 | **+1** (balayage + zoom) | — |
| Accessibilité | −3 | **+2** (aria, live, labels) | **+1** (audit WCAG formel) |
| Gestion erreurs | −2 | **+1** (CSV, limité) | **+1** (D-04 : décision infra) |
| Parcours | −1 | — | **+1** (AM-01/02/03 métier) |
| Qualité frontend | −1 | **+1** (`@layer`, risque élevé) | — |
| **TOTAL** | **−13** | **+10** | **+3** |

- **Score actuel : 87/100.**
- **Score techniquement atteignable sans décision externe : 97/100** (+10). Reste bloqué : audit
  a11y formel (+1), D-04 infra (+1), arbitrages métier (+1).
- **Score maximum après audit accessibilité formel : 98/100** (le +1 a11y débloqué ; restent D-04
  infra et AM métier).
- **100/100 : uniquement après les 3 décisions externes** (arbitrages métier AM-01/02/03 **et**
  décision DataProtection/antiforgery).

---

## 3. Feuille de route

### → OBJECTIF 90 (87 → 90, +3)
- **Changements** : refactor inline Home/Comptes (Design +1) ; extraction du bloc `<style>`
  `ComptesAgents` (Cohérence +1) ; aides sous boutons désactivés restants (Formulaires +1).
- **Tests** : garde « aucun `style=` de couleur en dur hors thème » (extension de `FondsThematises`) ;
  garde bUnit sur l'aide des boutons désactivés.
- **Recette** : Home + Comptes en clair/sombre ; formulaires import/export.
- **Accessibilité** : inchangée.
- **Effort** : ~2 j.

### → OBJECTIF 95 (90 → 95, +5)
- **Changements** : fil d'Ariane (Navigation +1) ; scroll/replace du résultat de soumission
  (Feedback +1) ; balayage responsive des 81 pages + zoom 200 % (Responsive +1) ; `aria-hidden` sur
  emojis + `aria-label` boutons icône (Accessibilité +1) ; régions `aria-live` + audit labels
  (Accessibilité +1).
- **Tests** : garde « toute page routable a un fil d'Ariane hors racine » ; garde « emoji décoratif =
  `aria-hidden` » ; snapshot reflow.
- **Recette** : navigation dossier→ticket ; NVDA sur les formulaires clés ; zoom 200 %.
- **Effort** : ~4-6 j.

### → OBJECTIF 98 (95 → 98, +3)
- **Changements** : CSV cumul syntaxe+référentiel quand possible (Gestion erreurs +1, partiel) ;
  refonte `@layer` pour retirer les `!important` (Qualité +1) ; **audit WCAG 2.1 AA formel**
  (clavier complet, retour de focus, lecteur d'écran, zoom) et corrections (Accessibilité +1).
- **Tests** : non-régression visuelle (captures avant/après) pour la refonte CSS ; scénarios clavier
  automatisés (bUnit `KeyDown`/focus).
- **Recette** : passe lecteur d'écran complète (NVDA + VoiceOver) sur les 9 parcours.
- **Effort** : ~8-10 j.

### → OBJECTIF 100 (98 → 100, +2) — **nécessite décisions externes**
- **AM-01/02/03** rendus → règles appliquées (Parcours +1).
- **DataProtection / antiforgery** décidé (magasin de clés partagé) + message « session expirée »
  (Gestion erreurs +1).
- **Tests** : cloisonnement `collab.limite` ; confirmation « Valider Booking » ; test antiforgery
  périmé → écran de marque ; test durée de séjour selon la borne retenue.
- **Recette** : rejeu des 3 parcours arbitrés ; simulation de session expirée.
- **Effort** : ~3-4 j **après** obtention des décisions.

---

## 4. Sortie

```
SCORE ACTUEL ......................................... 87/100
SCORE TECHNIQUEMENT ATTEIGNABLE SANS DÉCISION EXTERNE . 97/100
SCORE MAXIMUM APRÈS AUDIT ACCESSIBILITÉ FORMEL ....... 98/100
SCORE 100/100 ........................................ uniquement après les 3 décisions externes
                                                        (AM-01/02/03 métier + DataProtection/antiforgery infra)
```
