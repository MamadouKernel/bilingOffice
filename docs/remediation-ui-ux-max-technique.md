# Remédiation UI/UX — maximum technique + recette

**Date** : 7 septembre 2026. Objectif : maximiser la qualité UI/UX **démontrée**, sans décision
métier ni infrastructure, sans forcer le score.

> **Résultat honnête : 92 → 93/100.** Un seul point gagné — mais adossé à **trois défauts de
> contraste RÉELS** (WCAG 1.4.3) trouvés en navigateur et corrigés, dont un qui touchait **tout
> visiteur en thème clair par défaut**. Le reste du travail est de la **vérification** (responsive,
> non-régression) qui confirme l'existant sans ouvrir de nouveau point.

---

## 1. Baseline (préconditions vérifiées)

| | |
|---|---|
| HEAD | `1c3cd38` (inchangé) — branche `main` |
| Modifications concurrentes | aucune |
| Tests baseline | **1593/1593**, 0 échec, 0 ignoré — **reproduits** avant toute modification |
| Score de départ | 92/100 |

---

## 2. Défauts trouvés et corrigés (thème sombre / contraste)

Tous constatés **en navigateur** (scanner de contraste sur l'app réelle), tous **vérifiés après
correction** dans les deux thèmes.

### D-C1 — Pastilles/panneaux « subtle » illisibles en sombre
- **Constat** : `badge bg-primary bg-opacity-10 text-primary` (« Onboarding Entreprise ») →
  fond clair `#eff6ff` figé + encre azur clair → **2,9:1**.
- **Cause** : le bloc `.bg-*.bg-opacity-10 / .bg-*-subtle` fige des **fonds clairs** en `!important`
  sans variante sombre ; il écrase même les tokens `.bg-*-subtle` themables définis plus haut.
- **Correction** : variante `:root[data-theme="dark"]` — fond **sombre teinté**
  (`rgba(var(--X-rgb), .16)`) + encre d'accent **claire**, pour les 6 familles (primary/success/
  warning/danger/info/dark).
- **Preuve après** : primary **5,16:1**, success **7,62:1**, warning **8,73:1** (mesuré). Clair
  **inchangé** (fond `#eff6ff` restauré).

### D-C2 — `.text-primary` / liens illisibles en clair par défaut (le plus grave)
- **Constat** : en thème **clair par défaut**, `.text-primary` rendait l'azur clair `#3899d1` →
  **3,16:1 sur blanc**. Trois éléments réels mesurés (« Onboarding Entreprise »,
  « Comprendre les 4 profils », « Pré-enrôlez-vous »).
- **Cause** : `theme-manager.js` ne pose `data-theme` **que** pour le sombre ; un visiteur en clair
  par défaut a `data-theme` **absent**. Or les règles sombres étaient écrites
  `:root:not([data-theme="light"])` — qui capture l'**absence** (≠ « light »). Les couleurs sombres
  s'appliquaient donc en clair.
- **Correction** : re-portée de ces règles vers `:root[data-theme="dark"]` (la façon dont **tous**
  les jetons sombres sont déjà déclarés).
- **Preuve après** : clair par défaut `.text-primary` = `--action` `#14547f` → **8,06:1** ;
  sombre = azur clair → **5,58:1**. Les deux corrects.

### D-C3 — Boutons `btn-warning`/`btn-light.text-dark` : texte clair sur ambre en sombre
- **Constat** : « Pré-enrôlement Transporteur » (`btn-warning text-dark`) → encre claire sur ambre →
  **1,84:1**.
- **Cause** : l'override global `.text-dark { color: var(--ink) !important }` éclaircit l'encre en
  sombre ; sur un bouton à fond clair (ambre), le texte devait rester sombre. Le correctif antérieur
  ne couvrait que les **badges**, pas les **boutons**.
- **Correction** : `.btn-warning.text-dark, .btn-light.text-dark { color: #2a1c05 !important; }`
  (fond clair dans les deux thèmes → encre sombre partout).
- **Preuve après** : **7,7:1** (mesuré).

**Après correction, re-scan sombre de l'accueil et de la connexion** : plus aucun échec de contenu ;
seuls subsistent des boutons de marque **blanc-sur-azur** (`#2183bd`) à **4,16:1** — voir §6.

---

## 3. Régression introduite PENDANT la mission, puis corrigée (règle 13)

- **Régression** : ma première version de D-C1 utilisait `:root:not([data-theme="light"])`. Elle a
  **débordé sur le thème clair par défaut** (fond teinté sombre sur pastille claire), constaté
  immédiatement en navigateur (`data-theme=null`, `bodyBg` clair, pastille `rgba(20,84,127,.16)`).
- **Cause** : exactement D-C2 — `:not([data-theme="light"])` capture l'état clair par défaut.
- **Correction** : re-portée vers `:root[data-theme="dark"]` uniquement, puis vérifiée dans les trois
  états (clair par défaut, clair basculé, sombre).
- **Garde ajoutée** : `ThemeSombreScopingTests` interdit tout `:root:not([data-theme="light"])` dans
  `app.css`. **Mutation vérifiée** : réintroduire le motif fait échouer la garde.

---

## 4. Responsive (vérification exhaustive raisonnée)

Détail complet : [`matrice-responsive-81-pages.md`](matrice-responsive-81-pages.md).

- **Anonyme (11/13 pages)** : mesure en direct à 320/390/768/1920 + zoom 200 % → **0 débordement de
  page** partout. Reflow WCAG 1.4.10 (320 px) satisfait.
- **Authentifié (68 pages)** : **analyse statique** — 0 `<table>` hors `table-responsive`, largeurs
  px toutes en `max-width` (rétrécissables) ou en `min-width` **dans** un `table-responsive` (scroll
  interne légitime). **Aucun motif de débordement de page.**
- **Réserve déclarée** : les 68 pages authentifiées n'ont **pas** été re-mesurées en direct (le login
  exigerait d'exposer le secret UAT en console). Ce n'est pas une validation en direct.

---

## 5. Sidebar, clavier, formulaires, feedback — non-régression

| Sujet | État | Preuve |
|---|---|---|
| **Sidebar par permissions** | Logique **non touchée** cette session ; 20 tests `NavigationParPermissions` + garde `CablageNavigation` (0 lien interdit) ; recette authentifiée antérieure. | Tests verts ; `git` : NavMenu/Catalogue non modifiés cette session. |
| **Focus visible (2.4.7)** | Corrigé au tour précédent (outline sur `.btn:focus-visible`), gardé (`FocusVisibleTests`), re-vérifié en direct. | Mesure : outline 2 px sur bouton ombré. |
| **Retour de focus modale (2.4.3)** | Corrigé au tour précédent, gardé (`ModaleConfirmationFocusTests`). | 2 tests. |
| **Lien d'évitement / ordre de tabulation** | Vérifié en direct : le lien d'évitement est le **premier** tabbable. | Mesure DOM. |
| **Formulaires / feedback** | Aides de boutons désactivés + focus du panneau de résultat (import) posés au tour précédent. | Build + tests. |

---

## 6. Points restants (classés)

| Point | Catégorie | Détail |
|---|---|---|
| Boutons de marque blanc-sur-azur `#2183bd` à **4,16:1** | **DÉCISION (design token)** | Sous 4,5:1 de peu. Corriger = assombrir `--action` → impact large (toute l'identité d'action). Décision de design, pas un correctif mécanique. |
| Emojis décoratifs sans `aria-hidden` | **RISQUE ACCEPTÉ** | Balayage de masse, faible valeur ; les contrôles portent déjà un nom accessible (texte visible). |
| Sweep des ~200 `style=` inline restants | **TECHNIQUE (fond)** | Sans impact fonctionnel ; chantier de fond. |
| Recette **live** des 68 pages authentifiées (responsive + parcours) | **TECHNIQUE (env)** | Nécessite l'environnement authentifié sans exposer le secret UAT. |
| Audit WCAG **formel** (NVDA/JAWS, clavier complet sur pages authentifiées, zoom 200 % partout) | **AUDIT FORMEL** | 10ᵉ point d'accessibilité. |
| Cumul CSV syntaxe+référentiel | **FAUX POSITIF (D-22)** | Une ligne mal formée ne peut être vérifiée sémantiquement. |
| `!important` du thème Bootstrap | **RISQUE ACCEPTÉ** | Battent les utilitaires `!important` natifs ; les retirer régresserait le sombre (démontré). |
| **D-04** session/antiforgery | **INFRASTRUCTURE** | Inchangé. Non arbitré. |
| **AM-01 / AM-02 / AM-03** | **MÉTIER** | Inchangés. Non arbitrés. Voir [`backlog-ui-ux-final.md`](backlog-ui-ux-final.md). |

---

## 7. Tests & mutations

- Suite complète : **1594/1594** (0 échec, 0 ignoré). Base 1593 + **1** nouvelle garde
  (`ThemeSombreScopingTests`).
- Aucun test existant retiré ni affaibli.
- **Mutations vérifiées** : `ThemeSombreScopingTests` (réintroduction du motif → échec) ;
  `FocusVisibleTests` et `ModaleConfirmationFocusTests` (tours précédents).
- Aucune règle métier / tarification / cloisonnement / workflow modifiée. **CSS et composants
  d'affichage uniquement.**

---

## 8. Score recalculé — depuis les preuves

Voir [`audit-ui-ux-final-recalcule.md`](audit-ui-ux-final-recalcule.md).

| Critère | Avant | Après | Δ |
|---|---|---|---|
| Accessibilité | 8 | **9** | +1 (trois corrections de contraste 1.4.3 vérifiées, dont l'état clair par défaut) |
| Design, Cohérence, Navigation, Formulaires, Feedback, Responsive, Gestion erreurs, Parcours, Qualité | — | inchangés | 0 |
| **TOTAL** | **92** | **93** | **+1** |

Le 10ᵉ point d'accessibilité exige un **audit formel** ; Responsive reste à 9 tant que les 68 pages
authentifiées ne sont pas mesurées **en direct**.

---

## 9. Verdicts

| Question | Réponse | Motif |
|---|---|---|
| **UI prête pilote** | **OUI** | Aucun défaut technique UI ouvert ; trois contrastes réels corrigés et vérifiés ; responsive sans débordement. |
| **UI prête production** | **OUI SOUS CONDITIONS** | Conditions **non-UI** : D-04 (infra), AM-01/02/03 (métier), + recette live authentifiée et audit WCAG formel recommandés. |
| **Application prête production** | **OUI SOUS CONDITIONS** | L'UI n'est pas le facteur limitant. Conditions système : décisions métier/infra + dettes documentées des audits antérieurs. |

**Aucun commit effectué.**
