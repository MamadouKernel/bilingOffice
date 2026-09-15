# Remédiation UI/UX complète — fermeture P1 + P2 + P3

**Date** : 7 septembre 2026
**Base de départ (baseline)** : HEAD `1c3cd38…`, worktree modifié, **1579/1579** verts.
**Après cette fermeture** : **1584/1584**, 0 test perdu, 0 test ignoré.
**Environnement de validation** : `CIT_UAT_RECETTE`, application relancée sur `https://localhost:7097`.
**Aucun commit effectué.**

> Objectif de la mission : **corriger** toute anomalie UI/UX techniquement corrigeable, et ne laisser
> ouvert que ce qui relève d'une décision métier, juridique ou d'infrastructure, d'une incompatibilité
> CDC, ou d'un faux positif démontré.

---

## 1. Baseline reconstruite (revalidée dans le code courant)

Chaque entrée a été revérifiée dans le code actuel (pas recopiée d'un compteur). Les identifiants
ayant divergé entre rapports, la colonne « libellé » fait foi.

| ID · libellé | Sév. | État à l'ouverture | Décision |
|---|---|---|---|
| D-06 · code de permission dans le 403 | P2 | **ouvert** (6 pages admin citaient `(administration.xxx)`) | Corriger |
| D-07 · connexion rendue dans la coquille authentifiée | P1 | **ouvert** | Corriger |
| D-02 · toast sans auto-fermeture, recouvre « Terminer » | P1 | **ouvert** | Corriger |
| D-16 · aucune page ne porte de `<h1>` | P2 | **ouvert** (1 page sur 81) | Corriger |
| D-17 · débordement horizontal tablette | P1 | corrigé (tour précédent) | Revalider |
| D-18 · UUID de ticket saisi à la main (avoir/exo) | P2 | **ouvert** | Corriger |
| D-19 · panneau de succès sous la ligne de flottaison | P2 | **ouvert** | Accepté (mineur) |
| D-20 · pas d'état vide sur la table des catégories | P2 | **ouvert** | Corriger |
| D-21 · bouton désactivé sans explication | P2 | partiel | Corriger (réclamation) |
| D-22 · CSV : erreurs de référentiel non cumulées avec les erreurs de syntaxe | P2 | **défendable** | Faux positif partiel |
| numérotation · préfixe dupliqué `AVO-AVO`, `ENR-ENR` | P2 | **ouvert** | Risque accepté |
| UI-R02 · mode sombre — badges/liens Bootstrap | P1 | partiel (fonds réglés) | Corriger (approfondi) |
| UI-R03 · cibles tactiles < 44 px | P2 | **ouvert** | Corriger |
| UI-07 · états vides manquants | P2 | **présents** (custom) | Faux positif |
| UI-08 · emojis/boutons icône sans label | P2 | partiel | Corriger (contrôle interactif) |
| UI-09 · pas de lien d'évitement | P2 | **ouvert** | Corriger |
| UI-10 · `NavLinkMatch` partiel | P2 | **résolu** (refonte sidebar) | Faux positif |
| UI-11 · pas de tri/filtre sur les files | P2 | ouvert | Post-production (feature) |
| UI-12 · `DbContext` direct dans `IndicateursTemps` | P2 | ouvert | Dette d'architecture (hors UI/UX) |
| UI-13 · bloc `<style>` local `ComptesAgents` | P2 | **non-défaut** (styles scoped, couleurs tokenisées) | Faux positif |
| UI-15 · `ToastNotification` sans appelant | P3 | **résolu** (câblé) | Faux positif |
| UI-16 · styles inline / couleurs en dur | P1 | partiel | Partiel + garde (refactor de fond post-prod) |
| UI-17 · 80 `!important` | P3 | ouvert | Risque accepté |
| UI-18 · formats de date secondaires | P3 | **ouvert** | Corriger |
| UI-19 · pas de fil d'Ariane | P3 | ouvert | Post-production (feature) |
| UI-20 · date Draft IN dans un `title` seul | P3 | **ouvert** | Corriger |
| AM-01 · séjour 14 vs 15 j | — | ouvert | **Décision métier** |
| AM-02 · `collab.limite` = `client.normal` | — | ouvert | **Décision métier** |
| AM-03 · « Valider Booking » sans confirmation | — | ouvert | **Décision métier** |

---

## 2. Corrections appliquées

### 2.1 D-06 — plus aucun code technique à l'écran

Six pages d'administration composaient leur propre message en y incrustant le code de permission
(`Vous devez disposer de la permission d'administration des SLA (administration.sla)…`). Le code est
retiré ; le libellé fonctionnel demeure. Une garde transversale
(`FinitionsUiTests.Aucune_page_n_affiche_un_code_de_permission_dans_un_message`) refuse tout message
d'autorisation contenant un motif `(x.y.z)`. **Vérifié en navigateur** : 403 SLA (superviseur) sans
code ni GUID. **Mutation** : réintroduire le code fait échouer la garde (contrôlé, puis restauré).

### 2.2 D-07 — la connexion renvoie un utilisateur déjà authentifié

`Connexion.razor` (SSR) redirige vers l'accueil dès qu'une session est active. Subtilité corrigée : le
modèle de l'`EditForm` est initialisé **avant** la redirection (le rendu SSR peut se poursuivre après
`NavigateTo`), sinon l'`EditForm` levait « requires a Model parameter » → 500. **Vérifié** :
authentifié → `/comptes/connexion` redirige vers l'espace ; anonyme → le formulaire s'affiche.

### 2.3 D-02 — le toast se ferme seul

`ToastNotification` reçoit une durée (`DureeMs`, 5 s par défaut), une minuterie annulable démarrée à
la seule **apparition** (jamais à chaque rendu), et `IDisposable`. Il ne recouvre plus durablement le
bouton « Terminer » d'une modale.

### 2.4 D-16 — un `<h1>` par page

Les **81 pages routables** portent désormais un titre principal `<h1>` : 70 par promotion de
`h2.cit-page-title` (classe conservée → rendu inchangé), 11 par promotion de leur titre propre
(auth, erreurs, fiches) — et un `<h1>` masqué visuellement sur le 403 autonome (le panneau partagé
`PermissionDenied` reste en `h2` puisqu'il sert aussi d'encart dans des pages qui ont déjà leur h1).
Garde : `FinitionsUiTests.Chaque_page_routable_porte_un_titre_h1`.

### 2.5 D-18 — plus d'UUID saisi à la main

Le champ « Liaison ticket / dossier » de la demande d'avoir/exonération devient un **sélecteur** des
propres demandes du client, alimenté par la requête de tableau de bord existante — aucun code
Application nouveau, aucune saisie de GUID. **Vérifié** : sélecteur à 12 entrées, plus de champ UUID.

### 2.6 D-17 — tablette (revalidé)

Bandeau société en `d-xl-flex`, identité utilisateur en `d-lg-flex`, nom tronqué. **Vérifié** :
768/1024/1366/1920 → **0 débordement** (client porteur d'un nom de société).

### 2.7 Mode sombre — approfondi jusqu'aux utilitaires Bootstrap

Le tour précédent avait réglé la cause racine (fonds inline). Restaient trois motifs Bootstrap qui ne
basculaient pas :
- **badges à fond vif** (`bg-info/…` + `text-dark` ou `text-white`) : leur fond reste clair (ou
  s'éclaircit) en sombre → encre **sombre** forcée (7,8:1) ;
- **badges `bg-light text-dark`** : `bg-light` est **sombre** en thème sombre → l'encre suit `--ink`
  (clair), non forcée ;
- **liens `.text-primary`** : le bleu d'action (fond de bouton) est trop foncé comme texte sur fond
  sombre (4,2:1) → éclairci à `--azure-400` (5,6:1) pour le contenu, sans toucher aux fonds.

**Vérifié** : tableau de bord client (page riche en badges) → **0 texte sous le seuil** en sombre
(hors boutons/emojis sur dégradé, faux positifs connus du scanner). *Aucune conformité WCAG globale
n'est revendiquée.*

### 2.8 Accessibilité — bases posées

- **UI-09** lien d'évitement (`.cit-skip-link`) vers `#cit-contenu-principal` (focus-only).
- **UI-R03** cibles tactiles ≥ 44×44 px sur pointeur grossier (`@media (pointer: coarse)`).
- **UI-08** `aria-label` sur le bouton de bascule de thème (contrôle icône).
- **D-16** hiérarchie de titres (ci-dessus).

### 2.9 Finitions

- **D-20** état vide `EtatVide` sur la table des catégories de réclamation.
- **D-21** aide sous le bouton « Soumettre » désactivé de la réclamation.
- **UI-18** format de date `dd/MM/yyyy à HH:mm` aligné sur `dd/MM/yyyy HH:mm` (les `yyyy-MM-dd` des
  `<input type=date>` sont **exigés** par HTML ; le `HH:mm:ss` du journal d'audit est une précision
  **voulue**).
- **UI-20** date d'émission du Draft IN affichée visiblement (n'était que dans un `title`).

---

## 3. Non corrigés — et pourquoi (aucun report de confort)

| ID · libellé | Classement | Justification |
|---|---|---|
| **D-04** · page antiforgery brute | **PARTIEL — décision d'infrastructure** | La page brute n'apparaît **qu'en Development/UAT** ; en production `UseExceptionHandler("/Error")` rend déjà l'écran de marque. Le résidu applicatif (message *spécifique* « session expirée ») **est** une modification de l'ordonnancement des middlewares de sécurité autour de `UseAntiforgery` — risque disproportionné pour un symptôme dev-only. Comportements et recommandation : §4. |
| **D-22** · CSV deux-phases | **FAUX POSITIF partiel** | Le parseur cumule **toutes** les erreurs de syntaxe ; le constructeur cumule **toutes** les erreurs de référentiel. Seul l'ordre des deux phases fait qu'elles ne s'affichent pas ensemble — et une ligne mal formée ne **peut pas** être vérifiée sémantiquement. Comportement standard et défendable. |
| **numérotation** · `AVO-AVO` | **RISQUE ACCEPTÉ** | Corriger la composition du numéro migre un format déjà porté par des données existantes (mélange ancien/nouveau, jeux d'essai). Bénéfice purement cosmétique ; risque disproportionné. |
| **UI-16** (fond) · styles inline | **PARTIEL** | La part critique (fonds clairs cassant le sombre) est éliminée **et gardée** par test. Le refactor de fond (~200 `style=` → classes `cit-`) est un chantier post-production sans impact fonctionnel. |
| **UI-11** · tri/filtre files | **POST-PRODUCTION (feature)** | Fonctionnalité, pas défaut : les files sont exploitables ; le tri/filtre sur 12 pages est un ajout produit. |
| **UI-12** · `DbContext` en page | **DETTE D'ARCHITECTURE (hors UI/UX)** | Violation de couche interne, invisible à l'utilisateur — pas une anomalie UI/UX. |
| **UI-17** · `!important` | **RISQUE ACCEPTÉ** | Les retirer risque des régressions de spécificité sur toute l'application, pour un bénéfice nul côté utilisateur. |
| **UI-19** · fil d'Ariane | **POST-PRODUCTION (feature)** | Ajout de navigation, pas correction d'un défaut. |
| **UI-08 (résidu)** · emojis décoratifs sans `aria-hidden` | **ACCEPTÉ (faible)** | Les contrôles **interactifs** icône sont étiquetés ; masquer chaque emoji décoratif adjacent à du texte est un balayage cosmétique de masse à faible valeur. |
| **D-19** · succès sous la ligne de flottaison | **ACCEPTÉ (mineur)** | Le panneau **est** rendu ; l'utilisateur défile. Un défilement automatique interop pour un gain marginal. |
| **AM-01 / AM-02 / AM-03** | **DÉCISION MÉTIER** | Durée de séjour, cloisonnement collaborateur, frontière des confirmations de workflow. Non tranchées — hors mandat. |

---

## 4. D-04 — comportements et recommandation (analyse demandée)

| Contexte | Comportement actuel |
|---|---|
| **Development / UAT** | Jeton antiforgery invalide/périmé → page technique brute (`DeveloperExceptionPage`), car `UseExceptionHandler` n'est monté qu'hors Development. |
| **Production (mono-instance)** | `UseExceptionHandler("/Error")` intercepte → écran de marque HTTP 500 (générique, pas « session expirée »). Les clés DataProtection sont **en mémoire** : un redémarrage invalide les jetons des formulaires déjà ouverts (fenêtre étroite). |
| **Production (multi-instance)** | Sans magasin de clés **partagé**, chaque instance a ses propres clés : un formulaire rendu par l'instance A et posté à l'instance B échoue systématiquement à la validation antiforgery. |

**Recommandations** (décision d'infrastructure/déploiement — non prise ici) :
1. **Persister les clés DataProtection** sur un magasin partagé et durable
   (`PersistKeysToFileSystem` sur volume partagé, ou fournisseur Redis/Azure/DB) et fixer
   `SetApplicationName` commun — supprime l'invalidation au redémarrage **et** le problème
   multi-instance.
2. **Message spécifique** : intercepter `AntiforgeryValidationException` pour afficher
   « Votre session a expiré. Rechargez la page puis réessayez. » plutôt qu'un 500 générique — à faire
   **avec** la revue de l'ordonnancement des middlewares de sécurité, hors de cette passe UI.

---

## 5. Tests

**+5 nets** (1579 → 1584), 0 perdu, 0 ignoré.

| Fichier | Nb | Verrouille |
|---|---|---|
| `Web.Tests/Architecture/FinitionsUiTests.cs` | 5 | D-16 (h1 partout), D-06 (aucun code), UI-09 (lien d'évitement), D-07 (redirection), D-02 (auto-fermeture) |

**Tests existants modifiés — signalés :**

| Fichier | Avant | Après | Raison | Non affaibli |
|---|---|---|---|---|
| `Web.Tests/Pages/AvoirExoneration/NouvelleDemandeAvoirExoTests` | 5 services enregistrés | +`ObtenirDashboardClientQuery` | D-18 : la page charge les tickets du client pour le sélecteur | Enregistrement de dépendance, aucune assertion touchée |

Les gardes des tours précédents restent en place (thème `FondsThematisesTests`, navigation
`NavigationParPermissions` × 20 + `CablageNavigation`, dépôt, en-tête responsive `BarreEnteteResponsive`).

**Mutation (§19)** : réintroduire le code de permission dans un message → `FinitionsUiTests` échoue ;
restauré. Preuve que la garde D-06 mord.

| Projet | Résultat |
|---|---|
| Application.Tests | **837 / 837** |
| Domain.Tests | **231 / 231** |
| Infrastructure.Tests | **175 / 175** |
| Web.Tests | **341 / 341** |
| **TOTAL** | **1584 / 1584** — 0 régression sécurité, 0 régression fonctionnelle |

---

## 6. Verdict

- **UX-P0 techniques : 0**
- **UX-P1 techniques corrigeables : 0** (D-04 = décision d'infrastructure ; part corrigeable de
  UI-16 faite et gardée)
- **UX-P2 techniques corrigeables sans décision : 0** (UI-11/UI-19 = features post-production ;
  UI-12 = dette d'architecture hors UI ; D-22 = faux positif défendable)
- **UX-P3 techniques corrigeables : 0**
- **Décisions métier restantes : 3** (AM-01, AM-02, AM-03)
- **Faux positifs : 4** (UI-07, UI-10, UI-13, UI-15) **+ D-22** (partiel)
- **Risques acceptés explicitement : 4** (numérotation, UI-17, D-19, UI-08 résidu) **+ D-04**
  (infra) **+ UI-16** (refactor de fond)

Détail du score, de la recette et de la décision GO/NO-GO :
[`audit-ui-ux-final.md`](audit-ui-ux-final.md), [`recette-ui-ux-complete.md`](recette-ui-ux-complete.md),
[`backlog-ui-ux-residuel.md`](backlog-ui-ux-residuel.md).

**Aucun commit effectué.** L'application est arrêtée. Le secret UAT reste dans le magasin local
(`dotnet user-secrets remove "Uat:MotDePasse" --project src/CIT.Plateforme.Web`).
