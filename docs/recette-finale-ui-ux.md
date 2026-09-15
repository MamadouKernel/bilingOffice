# Recette finale UI/UX

**Date** : 7 septembre 2026. Ce document distingue honnêtement **ce qui a été réellement vérifié
ce cycle et comment**, de ce qui n'a pas pu l'être. Rien n'est déclaré conforme sans preuve.

## Méthodes de vérification employées

1. **Navigateur (rendu réel)** — serveur de développement, feuille `app.css` servie réellement,
   valeurs `getComputedStyle`.
2. **Rendu composant (bUnit)** — le composant Razor réel est monté avec les **vraies** requêtes et
   commandes applicatives sur base EF en mémoire ; on inspecte le markup produit et l'état persisté.
   C'est une vérification d'intégration au niveau du rendu, plus forte qu'un test de vue isolé.
3. **Tests unitaires/applicatifs** — logique métier et permissions.

## WCAG 1.4.11 — bordures de champ (navigateur, clair + sombre)

| Vérif | Résultat |
|---|---|
| Bordure repos clair (`#73879c` / blanc) | 3,70:1 ✅ |
| Bordure repos sombre (`#6b7d8c` / `--surface`) | 4,14:1 ✅ |
| Désactivé, lecture seule, erreur, succès, survol, focus — deux thèmes | tous ≥ 3:1 ✅ |
| Basculement de thème à chaud | bordure ré-résolue correctement ✅ |

Captures light + dark prises ; frontières de champ nettement visibles dans les deux thèmes.

## AM-02 — Collaborateur limité (rendu composant + applicatif)

| Scénario | Vérif | Résultat |
|---|---|---|
| Collaborateur **avec** droit de soumission | `ObtenirDashboardClientQuery` → `PeutSoumettre=vrai` | ✅ |
| Collaborateur **limité** (droit retiré) | `PeutSoumettre=faux` | ✅ |
| Rendu — collaborateur limité | raccourci « Nouvelle demande IMPORT » **absent** + CTA état vide absent | ✅ |
| Rendu — collaborateur normal | raccourci présent (non-régression) | ✅ |
| URL forcée sans droit | refus serveur inchangé (`SoumettreDemande*Command`) | ✅ (invariant serveur) |
| Menu latéral | dérivé des habilitations (`ConstruireNavigationQuery`) — inchangé | ✅ |

## AM-03 — Confirmation des transitions engageantes (rendu composant)

| Scénario | Vérif | Résultat |
|---|---|---|
| Clic « Clôturer » | ouvre la modale `role="dialog"`, ticket **non** clôturé | ✅ |
| Confirmation | clôture réelle (statut `Cloture` en base) | ✅ |
| Retour | modale fermée, aucun effet | ✅ |
| Double-validation | boutons désactivés pendant traitement (`Desactive`) | ✅ (composant) |
| Contrôle serveur | identité/habilitation/état revérifiés par la commande | ✅ (invariant serveur) |

## Recette navigateur authentifiée multi-persona — NON REJOUÉE ce cycle

La recette visuelle **authentifiée** sur les 8 profils (client, collaborateur limité, agent,
superviseur, admin, transporteur, agent sans droits, anonyme), le balayage responsive 320→1920 et
le thème sombre **par page authentifiée** n'ont **pas** été rejoués intégralement ce cycle : le coût
d'authentification réelle de chaque persona dans le harnais dépasse le périmètre de cette passe, et
les tours précédents avaient documenté une limite de livraison CSS sur le profil UAT publié.

Ce qui remplace partiellement cette recette pour les changements de ce cycle : les vérifications de
**rendu composant** ci-dessus montent les pages réelles (`DashboardClient`, `DetailTicket`) avec
leurs requêtes/commandes réelles et inspectent le markup effectivement produit — la permission-gating
et la confirmation sont donc prouvées au niveau du rendu, pas seulement au niveau de la logique.

**Honnêteté de portée** : les postes « recette navigateur multi-persona » et « responsive/dark par
page authentifiée » restent donc **NON REVÉRIFIÉS ce cycle** et ne sont pas comptés comme PASS
nouveaux. Ils ne régressent pas non plus : aucune modification de ce cycle ne touche la structure
responsive ni la palette hors du jeton `--field-border`.
