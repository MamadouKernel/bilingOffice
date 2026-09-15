# Matrice responsive — pages authentifiées

**Date** : 7 septembre 2026.

> **Statut : NON TESTÉ (bloqué).** Les pages authentifiées **n'ont pas** pu être mesurées en
> responsive ce tour. Cause détaillée : [`recette-authentifiee-exhaustive.md`](recette-authentifiee-exhaustive.md) §6
> — le navigateur de prévisualisation reçoit des CSS **vides** du serveur UAT (variante précompressée
> servie à 0 octet par `MapStaticAssets` en Debug hors Development), alors que le serveur sert bien
> la feuille complète à `curl` (64 KB, `text/css`). Une page sans CSS ne peut pas être mesurée en
> responsive : sa mise en page n'est pas celle de production.

## Ce qui EST mesuré (rappel, tours précédents — pages anonymes)

| Largeur | Pages anonymes | Débordement de page |
|---|---|---|
| 320 / 390 / 768 / 1920 / zoom~640 | 11 pages | **0** |

Détail : [`matrice-responsive-81-pages.md`](matrice-responsive-81-pages.md).

## Pages authentifiées — matrice

| Groupe (profil) | 320 | 390 | 768 | 1024 | 1366 | 1920 | Zoom 200 % |
|---|---|---|---|---|---|---|---|
| Dashboard client (`client.normal`) | NON TESTÉ | NON TESTÉ | NON TESTÉ | NON TESTÉ | NON TESTÉ | NON TESTÉ | NON TESTÉ |
| Dashboard agent (`pilote.manifeste`) | NON TESTÉ | NON TESTÉ | NON TESTÉ | NON TESTÉ | NON TESTÉ | NON TESTÉ | NON TESTÉ |
| Supervision (`superviseur`) | NON TESTÉ | NON TESTÉ | NON TESTÉ | NON TESTÉ | NON TESTÉ | NON TESTÉ | NON TESTÉ |
| Administration comptes (`admin.fonctionnel`) | NON TESTÉ | NON TESTÉ | NON TESTÉ | NON TESTÉ | NON TESTÉ | NON TESTÉ | NON TESTÉ |
| Files agent (tables) | NON TESTÉ | NON TESTÉ | NON TESTÉ | NON TESTÉ | NON TESTÉ | NON TESTÉ | NON TESTÉ |
| Formulaires authentifiés (import/export/avoir-exo) | NON TESTÉ | NON TESTÉ | NON TESTÉ | NON TESTÉ | NON TESTÉ | NON TESTÉ | NON TESTÉ |
| Fiches (ticket, dossier, avoir-exo) | NON TESTÉ | NON TESTÉ | NON TESTÉ | NON TESTÉ | NON TESTÉ | NON TESTÉ | NON TESTÉ |

> Aucune de ces cases n'est marquée « OK par interpolation » ni « OK car même CSS ». Elles sont
> **NON TESTÉES**, avec la cause exacte. Pour les lever : lancer la recette contre un `dotnet publish`
> (assets compressés valides) ou un navigateur qui décompresse correctement la variante servie.

## Analyse statique (rappel, non substituable à une mesure)

Indice — sans valeur de test : sur les 68 pages authentifiées, 0 `<table>` hors `table-responsive`,
largeurs px en `max-width`. C'est un signal favorable, **pas** une mesure. Le responsive authentifié
reste à **9/10** (le −1 n'est pas levé).
