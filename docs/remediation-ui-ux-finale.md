# Remédiation UI/UX finale — clôture P1/P2

**Date** : 6 septembre 2026
**Base de départ** : 1559/1559 (après la remédiation P0 navigateur).
**Après cette clôture** : **1579/1579**, 0 test perdu, 0 test ignoré.
**Environnement de validation** : `CIT_UAT_RECETTE`, application relancée sur `https://localhost:7097`.

> **Décision métier reçue et appliquée** : la simulation de proforma est **publique** (UI-R01). Ce
> défaut, auparavant `DECISION REQUIRED`, est désormais un **bug fonctionnel corrigé**.

---

## 1. Backlog réel reconstruit

Reconstruit en croisant `recette-ui-ux-authentifiee.md`, `backlog-ui-ux-final.md`,
`remediation-ui-ux-p0-p1.md` et `remediation-uat-p0.md`, en reprenant le **libellé** de chaque
défaut (les identifiants `D-xx` ont connu des collisions — voir la note ci-dessous).

> **Collision d'identifiants signalée.** Dans le rapport de recette, **`D-15`** désigne à la fois
> (a) la sidebar non filtrée par permission et (b) le préfixe de numéro dupliqué (`AVO-AVO-`). Le
> premier sens a été corrigé au tour P0 sous l'étiquette **D-05/D-15 (sidebar)** ; le second reste
> ouvert et est désigné ici **« numérotation (préfixe dupliqué) »**, sans numéro, pour lever
> l'ambiguïté.

### 1.1 Traités dans cette clôture

| ID (+ libellé) | Sévérité | Page / couche | Cause racine | Preuve navigateur (après) | Décision métier |
|---|---|---|---|---|---|
| **UI-R01** — simulation publique | P1 | `SimulationProformaPage` | La page était dans l'allowlist publique, mais son propre `OnInitializedAsync` refusait l'anonyme (`!EstAuthentifie \|\| !SocieteId`) | Anonyme : page ouverte, calcul Dry **79 400 XOF**, Reefer+dangereux+OOG **484 250 XOF**, résultat visible, CTA « Connectez-vous pour enregistrer » ; ticket/historique/admin toujours refusés | **Rendue** (publique) |
| **UI-R02** — mode sombre illisible | P1 | `Home`, `ComptesAgents` + garde transversale | Fonds clairs codés en dur (`#ffffff`, `#f8fafc`) en `style=` inline écrasant `var(--surface)`, tandis que le texte suivait `--ink` → 1,17:1 en sombre | Cartes d'accueil **15,07:1** (titres) / **5,64:1** (paragraphes) en sombre ; garde `FondsThematisesTests` : 0 fond clair codé en dur restant | Non |
| **D-03** — journal d'audit sans auteur | P1 | `JournalAudit` + lecteur | `@(AuteurId is null ? … : "Utilisateur")` — jamais résolu | **0** ligne « Utilisateur » (sur 111) ; auteurs résolus (nom + nature + e-mail + GUID stable) | Non |
| **D-06** — GUID dans le 403 | P2 | `AutorisationService` | Le message d'exception (visible) portait `L'utilisateur {GUID} … permission '{code}'` | Page 403 : **aucun GUID** ; le détail technique part dans `DetailTechnique` (journaux) | Non |
| **D-09** — `ToString` brut | P2 | `HistoriqueSimulationsPage` | `@s.Sens` affichait le record entier | Colonne Sens : **« Import local »** ; aucun `ReferenceTarifaireVue { … }` | Non |
| **D-10** — dossier vide + faux succès | P1 | `SoumettreDemandeImportCommand` | Dossier persisté **avant** la boucle BL → soumission tout-doublon = dossier vide + « succès » | Tout-doublon : **« Aucun connaissement retenu — aucun dossier créé »** ; en base : 0 dossier ajouté | Non |
| **D-13** — pages sans `<PageTitle>` | P2 | `FicheTicket`, `FicheDossier` | Les 2 seules pages sans titre | Toutes les pages ont désormais un `<PageTitle>` | Non |
| **D-17** — débordement tablette | P1 | `HeaderBar` | Le bandeau société + l'identité s'affichaient dès 768 px sans réduction | 768 px et 1024 px : **0 débordement** (était +160 px) | Non |

### 1.2 Confirmés en non-régression (corrigés au tour P0, re-validés ici)

| ID | Preuve |
|---|---|
| **D-05/D-15 (sidebar)** | superviseur 2 liens / **0 interdit** · agent.sansdroits 2 liens / **0 interdit** |
| D-14, D-11a, D-11b, D-01, D-12 | suite verte, aucune régression |

### 1.3 Restés ouverts — classés, non corrigés

| ID (+ libellé) | Sévérité | Raison du report / statut |
|---|---|---|
| **D-04** — page antiforgery brute | P1 → **PARTIEL** | La page brute n'apparaît **qu'en environnement Development/UAT** : en production, `UseExceptionHandler("/Error")` rend déjà l'écran de marque. Le résidu (message *spécifique* « session expirée » au lieu d'un 500 générique, et persistance des clés DataProtection sur un magasin partagé) touche le pipeline de sécurité et relève d'une **décision de déploiement** — non traité pour ne pas fragiliser une mécanique de sécurité au bénéfice marginal. |
| **D-02** — toast recouvre « Terminer » | P1 | Le toast (z-index 1080) chevauche le bouton d'une modale ouverte et ne se ferme pas seul (12 s). Fix nécessitant une temporisation + un décalage conditionnel à l'ouverture d'une modale. Non traité (contenu mais hors priorités reçues). |
| **D-07** — connexion dans la coquille authentifiée | P1 | `/comptes/connexion` reste accessible en session ouverte et se rend dans le layout authentifié. Fix : rediriger l'utilisateur authentifié vers son espace. Non traité. |
| **D-18** — UUID saisi à la main (avoir/exo) | P2 | Le client saisit un UUID de ticket parent. Fix : sélecteur parmi ses propres tickets (nouvelle requête). Non traité. |
| **D-22** — CSV s'arrête à la 1ʳᵉ erreur de référentiel | P2 | Le parseur cesse d'accumuler dès la première erreur ; une ligne à référentiel inconnu n'est pas cumulée. Non traité. |
| **numérotation (préfixe dupliqué)** | P2 | `AVO-AVO-`, `ENR-ENR-`. Changement de composition de numéro, à fort rayon (tous les numéros futurs, jeux d'essai). Reporté par prudence. |
| **D-19** (panneau succès sous la ligne de flottaison), **D-20** (état vide table catégories), **D-21** (bouton désactivé sans explication) | P2/P3 | Cosmétiques ; D-21 partiellement traité en P0 (formulaire catégories). |
| **D-06 (résidu)** — code de permission dans un conseil | P2 | Certaines pages d'admin composent leur propre message d'aide citant le **code** de permission (`administration.sla`) — jamais le GUID. Résidu mineur, sur écran réservé à un administrateur. |
| **UI-16** — styles en ligne / couleurs en dur | P1 | Partiellement traité (les fonds clairs de UI-R02 sont éliminés ; garde en place). Le reste (≈200 `style=`, promotion en classes `cit-`) reste un chantier de fond. |
| **UI-02/03/04/06** | P1 | Fermés sur leurs surfaces les plus exposées au tour P0 ; restent ouverts hors de ces surfaces. |
| **UI-R03, UI-07…UI-15, UI-17…UI-20** | P2/P3 | Cibles tactiles, états vides résiduels, fil d'Ariane, formats de date, `!important`, `aria-hidden` sur emojis, lien d'évitement. Non traités. |
| **AM-01** (séjour 14 vs 15 j), **AM-02** (collab.limite = client.normal), **AM-03** (« Valider Booking » sans confirmation) | DECISION REQUIRED | Arbitrages métier — non tranchés (hors mandat). |

---

## 2. UI-R02 — la cause dans le système de thème, pas trois cartes

**Le système de thème était correct.** `.cit-card` peint `var(--surface)`, `.text-dark` suit
`var(--ink)`, et chaque token a sa valeur claire **et** sombre. Ce qui le mettait en défaut, ce sont
des attributs `style=` **en ligne** qui figeaient le fond à `#ffffff` / `#f8fafc` : l'inline gagne
sur la classe pour le fond, mais le texte, lui, continuait de suivre le token — d'où, en sombre,
une encre claire sur une carte restée blanche (1,17:1).

La correction supprime ces fonds figés et les remplace par les tokens :
- `Home.razor` : 3 cartes d'orientation + le bloc « 3 étapes » ;
- `ComptesAgents.razor` : badge super-admin, cartes de permission, en-têtes de catégorie — vers
  `var(--surface)`, `var(--surface-2)`, `var(--signal-100)`, `var(--info-100)`, `var(--ok-600)` ;
- un token `--accent-ink` **qui bascule** (l'ambre soutenu du clair retombe sous le seuil en sombre,
  il passe donc à `--signal-400`).

**La garde qui ferme la porte.** `FondsThematisesTests` calcule la luminance de tout fond codé en
dur dans les composants Razor et **refuse tout fond clair** (les dégradés sombres de bandeau, les
accents de bordure et les replis `var(--x, #fff)` restent permis). Un futur `background:#fff` inline
fera échouer le test.

**Vérification navigateur (sombre, styles calculés)** : cartes d'accueil titres **15,07:1**,
paragraphes **5,64:1** — au-dessus de AA 4,5:1. Le scanner signale encore une quinzaine d'éléments,
tous des **boutons colorés / textes sur le bandeau dégradé** : ce sont les faux positifs déjà
identifiés en recette (le scanner ne résout ni les dégradés ni les fonds translucides), confirmés
lisibles par capture. **Aucune conformité WCAG globale n'est revendiquée** — seuls les contrastes
réellement mesurés sur fonds solides le sont.

---

## 3. UI-R01 — la simulation est publique

Décision métier : **publique, sans authentification**. La correction est entièrement dans la page —
la route était déjà publique (allowlist, pas de `[Authorize]`), et le handler de calcul ne demande
**ni identité ni autorisation**. Seul l'`OnInitializedAsync` de la page bloquait l'anonyme.

- **Ouverture** : la garde `!EstAuthentifie || !SocieteId` est retirée. L'anonyme charge, saisit,
  calcule, voit son total.
- **Enregistrement réservé** : `_peutEnregistrer = EstAuthentifie && SocieteId.HasValue`. L'anonyme
  ne voit pas le bouton « Enregistrer » — il reçoit une invitation
  « 🔒 Connectez-vous pour enregistrer » vers `/comptes/connexion?returnUrl=/simulation/proforma`
  (le retour est déjà géré, avec contrôle d'URL locale). **Aucune migration, aucun faux compte** :
  l'anonyme ne persiste rien ; le modèle `SimulationProforma` garde ses `SocieteId`/`AuteurId`
  non-nuls, employés seulement à l'enregistrement.
- **Sécurité inchangée** : anonyme → `/tickets/…`, `/simulation/historique`, `/admin/tarification`,
  `/billing-import/facturation`, `/administration/audit` = **redirigé vers la connexion**. RG-SIM-01
  n'est pas assoupli : le calcul n'a jamais exigé d'autorisation ; c'est la page qui en inventait une.

**Statut : BUG FONCTIONNEL CORRIGÉ.**
Simulation anonyme : **AUTORISÉE**. Recette navigateur : **OK** (Dry, Reefer, dangereux/OOG, résultat
visible, CTA connexion, boundaries refusées).

---

## 4. D-03 — le journal impute désormais ses entrées

La donnée existait (`AuteurId`), jamais résolue. Le lecteur d'audit résout maintenant les auteurs en
**une requête groupée** (`IUtilisateurInfoRepository.ObtenirInfosAsync`) :

- auteur nul → **« Système CIT »** ;
- auteur résolu → **nom** (à défaut e-mail) + **nature** + e-mail + **GUID stable** en dessous ;
- auteur dont le compte n'existe plus → **« Compte supprimé »**, mais l'entrée reste imputée à son
  identifiant — l'imputabilité ne s'efface pas.

**La clé stable reste l'identifiant** : le nom peut changer, jamais le GUID. Les deux sont affichés,
jamais l'un à la place de l'autre. Couverture : client / agent / administrateur / opération en masse
(54 entrées, même auteur) / compte désactivé / auteur introuvable — plus la lecture paginée.

Vérifié en navigateur : **0 ligne « Utilisateur »** ; auteurs nommés, natures et GUID visibles.

---

## 5. Les autres corrections, en bref

- **D-06** — `AccesRefuseException` porte désormais un message **générique** visible et un
  `DetailTechnique` (GUID, code, file) réservé aux journaux. Le 403 vérifié en navigateur ne
  contient plus de GUID. *(Résidu documenté : quelques pages d'admin citent le code de permission
  dans un conseil d'orientation — jamais le GUID.)*
- **D-09** — `@s.Sens.Libelle` : « Import local » au lieu du record.
- **D-10** — le dossier n'est créé **que si un BL est retenu** ; une soumission tout-doublon est un
  refus, plus un faux succès. Preuve base : aucun dossier ajouté ; 3 tests (tout-doublon, mixte,
  succès).
- **D-13** — `<PageTitle>` ajouté aux fiches ticket et dossier (numéro dans le titre d'onglet).
- **D-17** — le bandeau société ne réapparaît qu'en **XL**, l'identité utilisateur qu'en **LG**, le
  nom de société est **tronqué** (220 px). Débordement 768/1024 : **0** (était +160 px).

---

## 6. Sidebar — propriété conservée

La correction du tour P0 (le menu filtré sur les **permissions réelles**, même règle
`RegleHabilitation` que le refus serveur) est devenue une propriété verrouillée : 20 tests
`NavigationParPermissions` + la garde de câblage `CablageNavigationTests`. Re-validée en navigateur :
**0 lien de sidebar menant à un 403** pour absence de permission, pour tous les profils re-testés.
Rappel : **les contrôles serveur restent intacts** — l'absence de lien n'est jamais une sécurité.

---

## 7. Tests

**+20 tests** (1559 → 1579), 0 perdu, 0 ignoré.

| Fichier | Nb | Verrouille |
|---|---|---|
| `Web.Tests/Architecture/FondsThematisesTests.cs` | 1 | UI-R02 : aucun fond clair codé en dur dans les composants |
| `Web.Tests/Tarification/SimulationProformaWebTests.cs` (+3) | 3 | UI-R01 : page ouverte à l'anonyme, enregistrement réservé, CTA accueil direct |
| `Application.Tests/Securite/ResolutionAuteurJournalAuditTests.cs` | 11 | D-03 : client/agent/admin/masse/désactivé/introuvable/système/pagination + libellé |
| `Application.Tests/BillingImport/SoumettreDemandeImportTests.cs` (+2) | 2 | D-10 : tout-doublon → aucun dossier ; mixte → un seul dossier |
| `Web.Tests/Architecture/BarreEnteteResponsiveTests.cs` | 2 | D-17 : points de bascule XL/LG + troncature |
| `Application.Tests/Securite/AutorisationServeurTests.cs` (adapté) | — | D-06 : message sans GUID ni code, détail technique renseigné |
| `Application.Tests/Securite/PaginationJournalAuditTests.cs` (adapté) | — | signature du lecteur d'audit |

**Tests existants modifiés — signalés** : `AutorisationServeurTests` (l'assertion « le message
contient le code » devient « le code est dans `DetailTechnique`, pas dans `Message` » — c'est le
contrat D-06 lui-même) ; `PaginationJournalAuditTests` (constructeur du lecteur) ;
`SoumettreDemandeImportTests` (accès `Dossier` rendu non-nul). Aucune assertion affaiblie pour faire
passer une correction.

| Projet | Résultat |
|---|---|
| Application.Tests | **837 / 837** |
| Domain.Tests | **231 / 231** |
| Infrastructure.Tests | **175 / 175** |
| Web.Tests | **336 / 336** |
| **TOTAL** | **1579 / 1579** — 0 régression sécurité, 0 régression fonctionnelle |

---

## 8. Recette navigateur (extraits vérifiés)

| Parcours | Résultat |
|---|---|
| Simulation anonyme (Dry / Reefer+dangereux+OOG) | 79 400 / 484 250 XOF, résultat visible, CTA connexion |
| Boundaries anonyme (ticket, historique, admin) | redirigés vers la connexion |
| Journal d'audit (admin) | 0 « Utilisateur » ; auteurs nommés |
| Historique simulations (client) | Sens = « Import local » |
| Import tout-doublon (client) | « Aucun connaissement retenu », pas de dossier |
| 403 SLA (superviseur) | aucun GUID |
| Sidebar (superviseur, agent.sansdroits) | 0 lien interdit |
| Mode sombre (accueil) | cartes 15:1 |
| Responsive (login/simulation 375, dashboard 768/1024) | 0 débordement horizontal |

---

## 9. Score UI/UX final — 76 / 100

Recalculé **après** recette navigateur.

| Critère | Note | Justification de la perte |
|---|---|---|
| Design visuel | **13** / 15 | Système de tokens cohérent, mode sombre réparé. −2 : ≈200 `style=` inline subsistent (UI-16). |
| Cohérence | **8** / 10 | Composants partagés, tokens. −2 : `ComptesAgents` garde un bloc `<style>` local ; formats de date secondaires (UI-18). |
| Navigation | **8** / 10 | Sidebar filtrée sur permissions réelles (0 lien mort). −2 : pas de fil d'Ariane (UI-19) ; `NavLinkMatch` partiel (UI-10). |
| Formulaires | **7** / 10 | Validation solide, référentiels servis. −3 : UUID saisi à la main pour avoir/exo (D-18) ; certains boutons désactivés sans explication (D-21). |
| Feedback | **7** / 10 | Toasts, modales, confirmations câblées. −3 : le toast recouvre « Terminer » et ne se ferme pas seul (D-02). |
| Responsive | **8** / 10 | Mobile OK, débordement tablette corrigé (D-17). −2 : non éprouvé sur toutes les surfaces ; cibles tactiles < 44 px (UI-R03). |
| Accessibilité | **6** / 10 | Contrastes sombres réparés, ARIA sur modale/toast, focus géré. −4 : pas de `<h1>` (D-16), emojis sans `aria-hidden` (UI-08), pas de lien d'évitement (UI-09). |
| Gestion des erreurs | **7** / 10 | Liste blanche `MessagesUtilisateur`, messages génériques, 403 sans GUID (D-06). −3 : page antiforgery brute en dev (D-04) ; refus de dépôt métier restitué mais parseur CSV non cumulatif (D-22). |
| Parcours utilisateur | **8** / 10 | Simulation publique (UI-R01), import/export/réclamation/avoir/enrôlement fonctionnels, dossier vide corrigé (D-10). −2 : connexion rendue dans la coquille authentifiée (D-07) ; arbitrages métier ouverts (AM-01/02/03). |
| Qualité frontend | **4** / 5 | Gardes de non-régression (thème, navigation, dépôt), tokens. −1 : `!important` (UI-17) et styles inline résiduels. |
| **TOTAL** | **76 / 100** | |

Passage de **65/100 (PROVISOIRE — analyse statique)** à **76/100 (après recette navigateur réelle)**.

---

## 10. Verdict

- **UI prête pilote : OUI.** Aucun UX-P0 ; les parcours majeurs fonctionnent de bout en bout ; les
  défauts bloquants du pilote (dark mode, sidebar, simulation, dossier vide, imputabilité) sont
  fermés et gardés par des tests.
- **UI prête production : OUI SOUS CONDITIONS.** Restent, avant une mise en production sereine : la
  page antiforgery spécifique + persistance des clés (D-04), le chevauchement toast/modale (D-02),
  la connexion hors coquille (D-07), le sélecteur de ticket parent (D-18), le refactor des styles
  inline (UI-16), et **trois arbitrages métier** non tranchés (durée de séjour AM-01, cloisonnement
  collaborateur AM-02, confirmation « Valider Booking » AM-03).

---

## 11. Environnement

L'application est arrêtée. Le secret de recette reste dans le magasin local — à retirer une fois les
vérifications terminées :

```
dotnet user-secrets remove "Uat:MotDePasse" --project src/CIT.Plateforme.Web
```
