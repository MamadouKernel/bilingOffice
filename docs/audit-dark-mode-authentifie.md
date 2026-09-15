# Audit dark mode — pages authentifiées (build publié)

**Date** : 7 septembre 2026. Scan de contraste (luminance) sur le build publié, thème sombre,
pages authentifiées. Seuil texte normal 4,5:1.

## Défauts trouvés et corrigés

| Élément | Page | Avant | Après | Correctif (dark uniquement) |
|---|---|---|---|---|
| `.btn-outline-primary` (« Consulter ») | dashboard client | 4,23:1 | ~5,5:1 | texte + bordure `var(--azure-400)` |
| `.cit-user-avatar` (initiale) | en-tête (toutes pages) | 3,23:1 | lisible | encre `#e8eef4` |
| `.btn-light` (filtres « Actifs/Inactifs ») | comptes-agents | 2,96:1 | ✅ | fond `var(--surface-2)` + encre `var(--ink)` |

Re-scan après correction (dashboard client, file manifeste, admin comptes) : **plus aucun échec de
contenu**. Ne restent que les boutons de MARQUE blanc-sur-azur `#2183bd` à 4,16:1 — identiques en
clair, décision de token `--action`, non un défaut de thème.

## Non-régression clair

Vérifié : en clair (défaut, `data-theme` absent), `.btn-outline-primary` reste `#14547f` (--action,
8:1). Les correctifs sont scopés `:root[data-theme="dark"]` : le clair est inchangé.

## Gardes

`RecetteAuthentifieePublishTests` verrouille les trois correctifs sombres (présence des règles).
Aucune conformité WCAG globale revendiquée sans audit formel.
