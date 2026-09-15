# Sweep Design — final (après recette build publié)

**Date** : 7 septembre 2026.

La recette visuelle authentifiée n'a **pas** révélé d'incohérence visuelle répétée nouvelle sur les
~35 pages traversées (cartes, badges, boutons, tables cohérents — système de tokens appliqué). Les
extractions à valeur réelle (dégradés/pastilles de l'accueil, bloc `<style>` ComptesAgents, badges
« subtle » sombres) sont déjà faites et gardées.

| Classe | Motif | Décision |
|---|---|---|
| C — duplication | motifs accueil, styles ComptesAgents | **Extrait** (tours précédents) + gardes. |
| E — risque thème | fonds figés, scoping, `text-dark`/`text-white`, **btn-outline-primary/btn-light/avatar (ce tour)** | **Corrigés** + gardes. |
| F — dette pure | ~200 `style=` inline one-off | **Non fait** : cosmétique, sans impact ; Design reste 14/15. |

Aucune refonte mécanique. Le reliquat F est la seule dette Design, assumée.
