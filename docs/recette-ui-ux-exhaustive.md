# Recette UI/UX — session « maximum technique »

**Date** : 7 septembre 2026 · `https://localhost:7097` · pages **anonymes** (aucune authentification,
donc aucun secret UAT manipulé).

> Portée : cette session a mené la recette **en direct** sur les surfaces anonymes et l'analyse
> statique sur les 68 pages authentifiées. La recette authentifiée par profil reste celle de
> [`recette-ui-ux-authentifiee.md`](recette-ui-ux-authentifiee.md) (session antérieure) — non rejouée
> ici pour ne pas exposer le mot de passe UAT en console.

## 1. Contraste — mesures navigateur (scanner de luminance)

| Surface | Thème | Avant | Après | Verdict |
|---|---|---|---|---|
| Pastille `bg-primary bg-opacity-10 text-primary` | sombre | 2,9:1 | **5,16:1** | ✅ |
| Pastille `bg-success …` / `bg-warning …` | sombre | — | **7,62 / 8,73:1** | ✅ |
| `.text-primary` / liens | **clair par défaut** | 3,16:1 | **8,06:1** | ✅ |
| `.text-primary` / liens | sombre | — | **5,58:1** | ✅ |
| Bouton `btn-warning.text-dark` | sombre | 1,84:1 | **7,7:1** | ✅ |
| Accueil / connexion (re-scan contenu) | sombre | — | **0 échec de contenu** | ✅ |

Restant : boutons de marque **blanc-sur-azur** `#2183bd` à **4,16:1** (identiques en clair et sombre)
— décision de design token, voir backlog.

## 2. Responsive — débordement horizontal de page (en direct)

| Largeur | Pages anonymes testées | Débordement |
|---|---|---|
| **320** (reflow WCAG 1.4.10) | 11 | **0** |
| 390 | 3 | **0** |
| 768 | 2 | **0** |
| 1920 | 3 | **0** |
| Zoom 200 % (~640) | 2 | **0** |

68 pages authentifiées : **analyse statique** (0 table hors `table-responsive`, px en `max-width`) →
aucun motif de débordement. Détail : [`matrice-responsive-81-pages.md`](matrice-responsive-81-pages.md).

## 3. Thème — vérification multi-état

Le thème est piloté par `data-theme` (`theme-manager.js`) : **absent** = clair par défaut,
`"dark"` = sombre, `"light"` = clair basculé. Les trois corrections ont été vérifiées dans les
**trois** états — c'est ce qui a révélé, puis fait corriger, la mauvaise portée
`:root:not([data-theme="light"])` (qui capturait le clair par défaut).

## 4. Clavier / focus (en direct)

| Élément | Constat |
|---|---|
| Lien d'évitement | **Premier** élément tabbable ; cible `#cit-contenu-principal` présente. |
| Focus visible (bouton ombré) | `outline: 2px solid` mesuré (correctif WCAG 2.4.7 du tour précédent, re-vérifié). |

## 5. Éléments couverts par tests/analyse (non rejoués en direct cette session)

- Sidebar par permissions (20 tests `NavigationParPermissions` + garde `CablageNavigation`) — logique
  **non modifiée** cette session.
- Retour de focus modale (2.4.3) — `ModaleConfirmationFocusTests`.
- Parcours authentifiés par profil — recette antérieure.

**Recette : conforme.** Trois défauts de contraste réels corrigés et vérifiés ; aucun défaut de page
responsive ; aucune régression (1594/1594).
