# Remédiation P2-04 — Tests de câblage de l'état engageant

## Exigence

**RG-TCK-07** — « Le client peut annuler librement un ticket tant qu'aucun traitement métier
engageant ni document officiel n'a été produit ; au-delà, l'annulation directe est fermée. » (CYC-01)

**RG-TCK-11** — « La suggestion de clôture pour absence de réponse est interdite dès lors que le
ticket a atteint un état engageant (paiement confirmé, document officiel produit). » (CYC-05)

---

## Défaut

Le **comportement de production était correct** depuis la remédiation P1-01 : six commandes posent
le drapeau, trois points le consomment, et la colonne est persistée.

**Mais le câblage n'était protégé par aucun test.** Aucun test de la solution n'assertait
`EstEnEtatEngageant` après exécution d'une commande. Les deux seules suites qui manipulaient le
drapeau — `AnnulationSeuilBloquantTests` et `RelancesAbsenceReponseTests` — l'appelaient **à la
main**, exactement comme celles qui n'avaient pas vu, à l'origine, que `MarquerEtatEngageant()`
n'avait aucun appelant.

Un test qui pose lui-même l'état qu'il vérifie ne dit rien du code qui devrait le poser.

---

## Reproduction

La reproduction de ce lot n'est pas un test qui échoue sur le code actuel — le comportement est
correct — mais une **mutation qui ne fait rien tomber**. Avant l'ajout de cette suite, supprimer les
six appels `ticket.MarquerEtatEngageant()` laissait la solution **entièrement verte**.

C'est cette insensibilité qui constituait le défaut, et c'est elle que la mutation du § *Mutations*
mesure désormais dans l'autre sens.

---

## Cause racine

Les tests éprouvaient la **règle** (« un ticket engageant n'est pas suggérable à la clôture ») là où
la condition était posée à la main, jamais le **câblage** (« telle commande rend le ticket
engageant »). C'est le même angle mort que celui qui a produit P1-01, puis P0-bis, puis P0-ter : une
règle vérifiée sur une donnée fabriquée par le test ne dit rien du chemin de production.

---

## Correction

Aucun code de production n'a été modifié. `tests/CIT.Plateforme.Application.Tests/Tickets/CablageEtatEngageantTests.cs`
— **9 tests**, couvrant les six commandes engageantes et les deux effets attendus.

Chaque test :

1. prépare un ticket dans la file d'où part la commande, et un agent habilité sur cette file ;
2. **vérifie que le drapeau est faux au départ** ;
3. exécute la **commande réelle** ;
4. **recharge le ticket depuis la base** (`ChangeTracker.Clear()` puis relecture par le dépôt) ;
5. assert `EstEnEtatEngageant == true`.

Le rechargement n'est pas une précaution de style : vérifier l'instance en mémoire prouverait
seulement qu'une méthode a été appelée, pas que l'état a été **persisté**.

| Commande | Événement CDC |
|---|---|
| `EditerFactureCommand` (IMPORT) | document officiel produit |
| `EditerFactureExportCommand` (EXPORT) | document officiel produit |
| `ValiderPreuvePaiementCommand` (IMPORT) | paiement confirmé |
| `ValiderPreuvePaiementExportCommand` (EXPORT) | paiement confirmé |
| `ConfirmerLettrageCommand` (IMPORT) | paiement confirmé |
| `ConfirmerLettrageExportCommand` (EXPORT) | paiement confirmé |

**Les deux effets**, sans lesquels le drapeau ne serait qu'une donnée :

- `Un_ticket_devenu_engageant_ne_peut_plus_etre_annule_directement` — RG-TCK-07 ;
- `Un_ticket_engageant_n_est_jamais_suggere_a_la_cloture_pour_absence_de_reponse` — RG-TCK-11,
  vérifié **après épuisement des relances**, c'est-à-dire dans la seule situation où la suggestion
  serait autrement recevable.

**Un témoin** : `Un_ticket_non_engageant_reste_suggerable_a_la_cloture`. Sans lui, une
implémentation qui refuserait toujours la suggestion passerait le test précédent.

---

## Tests

| Suite | Résultat |
|---|---|
| `CablageEtatEngageantTests` *(créée)* | **9 / 9** |
| **Suite globale** | **1 363 / 1 363** |

---

## Mutations

| Mutation | Régression simulée | Tests tombés |
|---|---|---|
| **Retrait des 6 appels `ticket.MarquerEtatEngageant()`** | Le défaut P1-01 à l'identique | **8** |

Les huit sont les six tests de commande et les deux tests d'effet. Le témoin reste vert, comme
attendu — il décrit le cas non engageant.

Avant ce lot, la même mutation faisait tomber **zéro** test.

Restauration vérifiée : 6 fichiers contiennent à nouveau l'appel, suite globale verte.

---

## Fichiers modifiés

**Production : aucun.** Le comportement était correct ; seul le filet manquait.

**Tests — 1 créé** : `tests/CIT.Plateforme.Application.Tests/Tickets/CablageEtatEngageantTests.cs`.

---

## Risques résiduels

1. **La liste des six commandes est figée dans la suite.** Une septième commande engageante ajoutée
   demain ne serait couverte par rien. Un test d'inventaire — comparer les appelants de
   `MarquerEtatEngageant()` à une liste attendue — fermerait ce trou ; il n'a pas été écrit, ce lot
   portant sur le câblage existant.
2. **Le circuit IES n'a pas de commande engageante propre** : sa facturation est produite hors
   plateforme (RG-EXP-04). Un ticket IES ne devient donc jamais engageant côté CIT. Conforme au CDC,
   mais à revoir si la finalisation IES était rapatriée.
3. **L'effet RG-TCK-07 est vérifié via `EstEligibleASuggestionDeCloture`**, qui partage le drapeau
   avec la garde d'annulation d'`AnnulerTicketCommand`. Un test traversant réellement la commande
   d'annulation serait plus direct ; il existe déjà dans `AnnulationSeuilBloquantTests`, mais avec un
   drapeau posé à la main.
