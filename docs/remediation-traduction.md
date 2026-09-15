# Remédiation traduction — correctif borné + feuille de route

**Date** : 8 septembre 2026. La traduction complète des ~93 pages codées en dur est un chantier de
grande ampleur, non réalisable **ni vérifiable** en une passe. Plutôt que de prétendre le contraire,
ce cycle : (1) applique **un** correctif borné, correct et testé qui démontre le bon patron ; (2)
pose des gardes de non-régression ; (3) trace la feuille de route honnête du reste.

## 1. Correctif apporté ce cycle — StatutBadge bilingue

**Constat.** `StatutBadge` (composant partagé, utilisé sur ~15 pages) affichait des libellés de
statut **codés en dur en français**. Les clés `Status.*` du dictionnaire étaient mortes.

**Correctif.**
- Ajout de 6 clés `Statut.Ticket.*` (FR + EN) alignées 1:1 sur l'énum `StatutTicket`.
- `StatutBadge.Libelle` lit désormais le libellé **localisé** (`SharedResourcesLocalizer.Get`).
- La valeur interne de l'énum n'est plus jamais affichée brute ; le libellé suit la culture active.
- **Aucune régression FR** : en `fr-FR` (défaut), les chaînes rendues sont identiques à avant
  (« En attente », « En cours », « En attente client », « Clôturé », « Annulé », « Rejeté »).

**Preuves.**
- `StatutBadgeTests` : 12 tests — 5 libellés FR, 5 libellés EN (« Pending », « In progress »,
  « Awaiting client », « Closed », « Cancelled »), 2 cas rejet manifeste (FR/EN). Culture épinglée.
- `TraductionParityTests` : 8 tests — parité stricte des clés FR/EN, statuts réellement bilingues
  (FR ≠ EN), bascule effective du localizer.
- **Mutation** : remplacer le libellé localisé par `Statut.ToString()` (énum brut) → 5 tests tombent.
  Restauré.

## 2. Gardes de non-régression posées

| Garde | Rôle |
|---|---|
| `TraductionParityTests.Les_dictionnaires_FR_et_EN_ont_exactement_les_memes_cles` | toute clé ajoutée d'un seul côté fait échouer la garde (pas de demi-traduction) |
| `TraductionParityTests.Les_libelles_de_statut_de_ticket_sont_reellement_bilingues` | interdit un libellé EN copié du FR |
| `TraductionParityTests.Le_localizer_bascule_reellement_de_culture` | verrouille le comportement FR→EN du localizer |
| `StatutBadgeTests` (EN) | verrouille l'affichage bilingue du composant à forte fréquence |

Ces gardes ne sont **pas** naïves : elles ne prétendent pas interdire tout littéral (ce qui serait
ingérable vu l'état du code) ; elles protègent la cohérence du dictionnaire et l'élément corrigé.

## 3. Feuille de route (NON réalisée ce cycle — honnêteté de portée)

Pour amener la traduction à « complète », par ordre de valeur/visibilité :

1. **Shell partagé** : `NavMenu` + `HeaderBar` + libellés de `ConstruireNavigationQuery` (sidebar).
2. **PageTitle** : passer tous les `<PageTitle>` par le localizer.
3. **Commutateur anonyme** : exposer `LanguageSwitcher` sur l'accueil public.
4. **Dashboards** (client, agent, société, supervision) : ~toutes chaînes en dur.
5. **Formulaires + validation** : messages `required`/format/métier via le localizer.
6. **Modales / toasts / états vides / pagination / breadcrumbs**.
7. **Texte dynamique backend** : messages métier, erreurs, refus — source de libellés localisée
   (réutiliser le patron `LibelleStatutTicket` FR/EN déjà présent au domaine pour les statuts).
8. **Pages admin**.

Chaque lot doit s'accompagner de sa recette navigateur FR/EN et d'un test de non-régression, comme
le correctif StatutBadge ci-dessus.

## 4. Ce qui n'a délibérément PAS été fait

- Aucune traduction de masse non vérifiable (aucune page marquée « traduite » sans preuve).
- Aucune langue inventée : seules `fr-FR` et `en-US`, réellement prévues, sont traitées.
- Aucun changement du mécanisme (il est correct).
