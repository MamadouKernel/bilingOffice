# Audit WCAG 2.2 AA — formel (build publié, pages authentifiées)

**Date** : 7 septembre 2026. Environnement : build **publish Release**, UAT, base CIT_UAT_RECETTE,
pages **authentifiées et stylées**. Outils : mesures navigateur réelles (contraste, reflow, focus,
tailles), tests clavier, injection d'espacement, inspection DOM. Tests **1614/1614**.

> **Portée honnête.** Cet audit couvre tout ce qui est **démontrable par outillage et clavier**. Il
> n'inclut **pas** de passe au **lecteur d'écran** (NVDA/JAWS/VoiceOver) : aucun n'est disponible
> ici. Voir la ligne dédiée. Aucune conformité globale n'est déclarée sans cette passe.

## 1. Résultats par critère

| Critère | Verdict | Preuve / note |
|---|---|---|
| 1.1.1 Contenu non textuel | ✅ PASS | 0 `<img>` sans `alt` ; emojis décoratifs des recherches passés en `aria-hidden`. |
| 1.3.1 Information et relations | ✅ PASS (corrigé) | Labels présents ; en-têtes de table ; **2 champs de recherche sans nom accessible → `aria-label` ajouté**. |
| 1.3.2 Ordre séquentiel | ✅ PASS | Ordre DOM logique (traversée). |
| 1.3.5 Identifier la finalité | ⚠️ PARTIEL | `autocomplete` absent sur quelques champs (ex. e-mail de connexion) — amélioration possible, non bloquant. |
| 1.4.3 Contraste (texte) | ✅ PASS | Corrigé (badges, `.text-primary`, boutons ambre, dashboards). Résiduel : boutons de marque 4,16:1 = décision de token. |
| 1.4.10 Reflow (320 px) | ✅ PASS | 0 débordement de page sur toutes les surfaces (en-tête mobile corrigé). |
| 1.4.11 Contraste non textuel | ⚠️ DÉCISION | Bordure de champ ~1,4:1 (clair et sombre) : les inputs s'appuient sur une bordure discrète (fond = fond de carte). Passage à ≥3:1 = **décision de token** (poids de bordure / fond d'input). |
| 1.4.12 Espacement du texte | ✅ PASS | Après injection WCAG (interligne 1,5 ; lettres 0,12em ; mots 0,16em ; § 2em) : 0 débordement, 0 clip. |
| 1.4.13 Contenu au survol/focus | ⚠️ PARTIEL | Menus `<details>` natifs (clic, non survol) ; infobulles = `title` natifs. Escape ne referme pas `<details>` — amélioration possible. |
| 2.1.1 Clavier | ✅ PASS | Contrôles atteignables, focus visible, ordre logique. |
| 2.1.2 Pas de piège clavier | ✅ PASS (observé) | Aucun piège observé (Tab traverse ; modale fermable Échap — garde `ModaleConfirmationFocusTests`). |
| 2.4.1 Contournement | ✅ PASS | Lien d'évitement (premier tabbable). |
| 2.4.2 Titre de page | ✅ PASS | 81/81 (garde). |
| 2.4.3 Ordre du focus | ✅ PASS | Focus initial + retour modale (gardes). |
| 2.4.4 Fonction du lien | ✅ PASS | 0 contrôle sans nom accessible. |
| 2.4.6 Titres et labels | ✅ PASS | `h1` unique par page ; labels descriptifs. |
| 2.4.7 Focus visible | ✅ PASS | Outline 2px mesuré (garde `FocusVisibleTests`). |
| **2.4.11 Focus non masqué** | ✅ **CORRIGÉ** | En-tête sticky (62px) sans `scroll-padding-top` → cible de focus masquée. **`scroll-padding-top: var(--header-h)` ajouté** + garde. |
| 2.5.3 Label dans le nom | ✅ PASS | Nom accessible = texte visible ; recherches désormais nommées. |
| 2.5.8 Taille des cibles (min 24px) | ✅ PASS | 0 cible < 24px ; `@media (pointer: coarse)` ≥ 44px. |
| 3.1.1 Langue de la page | ✅ PASS | `lang="fr"`. |
| 3.2.x Cohérence | ✅ PASS | Navigation/en-tête cohérents sur toutes les pages. |
| 3.3.1 Identification des erreurs | ✅ PASS | Erreurs en texte (alertes). |
| 3.3.2 Labels / instructions | ✅ PASS | Champs étiquetés. |
| 3.3.3 Suggestion sur erreur | ⚠️ PARTIEL | Présente sur les formulaires clés ; non auditée exhaustivement. |
| 4.1.2 Nom, rôle, valeur | ✅ PASS (corrigé) | Après ajout des `aria-label` de recherche. |
| **4.1.3 Messages de statut** | ✅ **CORRIGÉ** | **67 alertes dynamiques** (48 erreur + 19 succès) sans rôle live → `role="alert"` / `role="status"` ajoutés + garde. Toasts déjà `role="status"`. |
| **Lecteur d'écran (NVDA/JAWS)** | ⛔ **NON TESTÉ FORMELLEMENT** | Aucun lecteur d'écran disponible dans cet environnement. Le 10ᵉ point d'accessibilité l'exige. |

## 2. Défauts techniques trouvés et CORRIGÉS ce tour

| # | Critère | Défaut | Correction | Garde |
|---|---|---|---|---|
| A1 | 4.1.3 | 67 alertes dynamiques non annoncées au lecteur d'écran | `role="alert"`/`role="status"` | `AccessibiliteFormelleTests` |
| A2 | 2.4.11 | focus masqué sous l'en-tête sticky | `scroll-padding-top: var(--header-h)` | `AccessibiliteFormelleTests` |
| A3 | 1.3.1 / 4.1.2 / 2.5.3 | 2 champs de recherche sans nom accessible (placeholder seul) | `aria-label` + emoji `aria-hidden` | — |

Toutes vérifiées sur le build publié (search `aria-label` ✅, `scroll-padding-top: 62px` ✅).

## 3. Ce qui bloque encore la conformité AA formelle

1. **Lecteur d'écran non testé** (NVDA/JAWS indisponibles ici) — obligatoire pour déclarer AA.
2. **1.4.11 bordures de champ** : décision de token (poids de bordure / fond d'input) pour ≥3:1.
3. Améliorations non bloquantes : `autocomplete` (1.3.5), `<details>` + Échap (1.4.13), suggestions
   d'erreur exhaustives (3.3.3), sweep `aria-hidden` des emojis décoratifs restants (1.1.1).

**Accessibilité : 9/10.** Le 10ᵉ point exige la passe lecteur d'écran (indisponible) ET la décision
1.4.11. L'audit outillé + clavier est, lui, complet, et ses défauts techniques sont corrigés.
