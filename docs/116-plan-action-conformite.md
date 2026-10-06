# Plan d'action vers la conformité 100 % — établi le 06/10/2026

> Objectif : passer de « pilote déployé, ~90 % en code » (voir `docs/113`) à **recette signée /
> conformité CDC prononcée**. Le chemin critique n'est **pas** le développement : c'est la décision
> métier, l'exploitation et la recette. Dates indicatives à ajuster avec les équipes.

## Vérification documentaire (fait le 06/10)

Les deux documents sources (`description du parcours client billing office - digitalisation.docx`
et `Parcours client Billing Office.docx`) ont été lus et comparés au CDC :
- **Aucun besoin n'échappe au CDC.** Ce sont les sources narratives que le CDC a consolidées.
- Cas particuliers import (marchandises dangereuses / fiche d'escorte, enlèvement sous palan,
  véhicules usagés / réquisition, entreposage exceptionnel) : **déjà implémentés** (migration
  `CasParticuliersImport`).
- Seul élément « en plus » dans le doc : la variante **portail interfacé avec IPAKI/IRIS** →
  **hors périmètre** du CDC (§5.2), explicitement prévue en **phase 2 future**.

## Jalons

| # | Action | Propriétaire | Début | Fin visée | Preuve de clôture |
|---|---|---|---|---|---|
| J1 | **Acter par écrit les exclusions de périmètre** (SMS/WhatsApp ; IA générative → approche base de connaissances) via avenant/PV | DSI | 06/10 | **08/10** | Note signée |
| J2 | **Ateliers valeurs métier** (`docs/115`) : SLA + seuils, matrice transitions, pièces exigées par compte/étape, relances, horaires ouvrés, rétention | Métier (Billing, Gate, Finance, Support) | 07/10 | **10/10** | Valeurs arrêtées |
| J3 | **Saisie des valeurs** dans l'administration (sans développement) | Admin fonctionnel | 10/10 | **13/10** | Captures / export paramétrage |
| J4 | **Sécurité d'exploitation** : certificat TLS reconnu, remplacement du compte `sa` (`deploy/sql/creer-compte-applicatif.sql`), antivirus confirmé sur `/ready` | DSI / Infra | 07/10 | **13/10** | `/ready` 5× Healthy + certificat valide |
| J5 | **Preuves non fonctionnelles** : perf < 3 s (p95), dispo ≥ 99 %, **test de restauration** (scripts `deploy/`) | DSI / Infra | 13/10 | **16/10** | Rapports archivés |
| J6 | **Branch protection GitHub** sur `main` (CI bloquante) | DSI | 06/10 | **07/10** | Règle active (Settings → Branches) |
| J7 | **Campagne de recette §22** : rejouer les 14 scénarios (base `RecSce*`) en environnement de recette | Recetteur métier + CIT | 14/10 | **20/10** | `docs/114` rempli |
| J8 | **Traitement des anomalies** éventuelles (bloquantes/majeures) | Dev | 17/10 | **22/10** | Anomalies closes |
| J9 | **Prononcé de recette** (sans / avec réserves) et signatures | Métier + CIT + DSI | 23/10 | **24/10** | `docs/114` signé |

## Chemin critique

J2 → J3 → J7 (les valeurs métier doivent être saisies avant de recetter). J4/J5/J6 sont parallèles.

## Définition de « 100 % » (à valider)

Conformité prononcée = **100 % des scénarios Must §22 en succès et signés**, 0 anomalie
bloquante/majeure ouverte, sécurité vérifiée, documentation d'exploitation livrée —
**hors SMS/WhatsApp et IA générative actés en J1**.

## Rappel honnête

Le développement n'est plus le goulot : le code est à ~90 %+ et testé (2162 tests, 32 scénarios
`RecSce*`). Ce qui reste est de la **décision**, de l'**exploitation** et de la **validation
signée**. Sans la recette signée (J7–J9), la plateforme reste un **pilote**, pas une solution
**conforme**.
