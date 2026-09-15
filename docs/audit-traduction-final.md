# Audit i18n FR/EN — état réel (mesuré)

**Date** : 12 septembre 2026. Chiffres **mesurés** sur le worktree, pas repris d'un audit antérieur.
Aucun commit.

## 1. Mécanisme (correct)

| Élément | État |
|---|---|
| Cultures | `fr-FR` (défaut) + `en-US` |
| Localizer | `SharedResourcesLocalizer : IStringLocalizer<SharedResources>` — 2 dictionnaires en mémoire, clés nommées (`Common.*`, `Nav.*`, `Statut.Ticket.*`…) |
| Parité clés | **97 FR / 97 EN** — parité OK, gardée par `TraductionParityTests` |
| Changement de culture | POST `/culture/set`, cookie `.AspNetCore.Culture` (1 an), CSRF |
| `<html lang>` | dynamique (`CultureInfo.CurrentUICulture`) |
| StatutBadge | clés `Statut.Ticket.*` présentes FR+EN |

**Réserve** : le localizer applique un **repli silencieux FR en mode EN** (clé absente en EN →
valeur FR). Aujourd'hui inoffensif (parité 97/97), mais il masquerait une clé EN manquante à
l'exécution. La garde de parité l'empêche au build ; le repli reste une dette à surveiller.

## 2. Couverture (le vrai trou)

| Mesure | Valeur |
|---|---|
| Pages routables | 81 |
| Composants `.razor` (total) | 101 |
| **Composants utilisant le localizer** | **7 / 101** |
| Composants **sans** localisation | **94 / 101** |
| `<PageTitle>` codés en dur (FR) | **71 / 81** |
| `placeholder="…"` codés en dur | **72** |
| `aria-label="…"` codés en dur | **15** |
| Enums rendus bruts (`@x.ToString()`) | **1** (le reste passe par des composants/badges) |
| Messages `DataAnnotations` (`ErrorMessage=`) | 0 (validation par messages inline) |

Composants déjà localisés (7) : `SoumettreDemande` (import), `SoumettreDemandeExport`, `Connexion`,
`VerifierCode`, `NouvelleReclamation`, `SimulationProformaPage`, `ChampMotDePasse`.

## 3. Conséquence honnête

En anglais, **la quasi-totalité des surfaces reste en français** : sidebar (labels dans
`CatalogueNavigation`, couche Application), titres de page, corps de page, placeholders,
validations, la plupart des messages. Le **mécanisme** fonctionne ; la **couverture** est ~7 %.

Atteindre « 100 % des surfaces utilisateur en EN » suppose : extraire ~1 500–3 000 chaînes en clés,
câbler 94 composants, localiser 71 `PageTitle` + 72 placeholders + 15 `aria-label`, et rejouer une
recette FR **et** EN par profil/largeur/thème. C'est un chantier de plusieurs jours, non réalisable
en une session avec le niveau de preuve exigé.

## 4. Ce qui a été fait / non fait ce tour

- **Fait** : baseline mesurée (1640→1644), inventaire exact, AM-03 (voir doc dédiée), un
  enregistrement partagé `ConfirmationEnAttente`.
- **NON fait (par intégrité — pour ne pas laisser 94 composants à moitié migrés)** : la traduction
  massive des pages. Aucune page n'a été partiellement migrée : l'état reste **cohérent**.

## 5. Plan d'attaque proposé (lots)

| Lot | Périmètre | Effort | Pré-requis |
|---|---|---|---|
| 2 — Navigation partagée | `CatalogueNavigation` (labels→clés) + `NavMenu`, `HeaderBar`, `FilAriane`, `EtatVide`, `ToastNotification`, `PermissionDenied` | M | adapter 2 assertions de titres de groupe (routes intactes) |
| 1 — Public | Accueil, auth, création/validation compte, clôture, erreurs | M | vérifiable **anonymement** FR+EN |
| 3 — Client | dashboard, tickets, réclamations, enrôlement, avoir/exo | L | recette authentifiée |
| 4 — Agent/Superviseur | files, instruction, SLA, import/export | L | recette authentifiée |
| 5 — Administration | comptes, habilitations, sociétés, config, audit | L | recette authentifiée |
| Transversal | `PageTitle`, placeholders, `aria-label`, enums, validations | M | garde anti-FR-en-EN **après** migration |

> La **garde anti-texte-français-en-EN** ne peut être ajoutée en vert **qu'après** la migration :
> aujourd'hui elle échouerait sur ~94 composants (ou exigerait une allowlist qui la viderait de son
> sens). Elle est donc planifiée en fin de lot, pas maintenant.

## Score

Traduction : **2/10 → 2/10** (inchangé). Le mécanisme et la parité sont solides, mais la couverture
réelle (~7 %) n'a pas bougé ce tour. Annoncer plus serait faux.
