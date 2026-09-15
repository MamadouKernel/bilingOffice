# Recette authentifiée exhaustive — build publié

**Date** : 7 septembre 2026. HEAD `1c3cd38` (branche `main`, aucun commit). Tests **1611/1611**,
0 échec, 0 ignoré. Build de recette : **`dotnet publish -c Release`** (env **UAT**, base
**CIT_UAT_RECETTE**).

> **Le blocage est levé.** Le build publié sert correctement les CSS compressés au navigateur ; la
> recette VISUELLE des pages authentifiées, impossible au tour précédent, a été menée en direct — et
> a fait surface un **bug de routage majeur** (toute consultation de dossier renvoyait 500) et un
> **défaut responsive** de l'en-tête au mobile, tous deux **corrigés et vérifiés**.

---

## 1. Levée du blocage CSS (preuve)

| Requête | Debug/dotnet run (tour précédent) | **Publish Release (ce tour)** |
|---|---|---|
| `app.css` sans compression | 64 339 o, `text/css` | 64 339 o |
| `app.css` avec `Accept-Encoding: gzip, br` | **0 octet** (variante vide) | **13 656 o, `Content-Encoding: br`** ✅ |
| Application des feuilles au navigateur | ❌ (page HTML brute) | ✅ bootstrap 1297 règles, app.css 315, scoped 18 |
| `--surface` (token) | absent | `#0e1a24` ✅ |

Cause du blocage précédent : `MapStaticAssets` servait des variantes précompressées **vides** en
build Debug hors Development. Le **publish** génère des variantes valides (`app.css.br` = 13,6 Ko).
Le lancement du **dll publié** (`--environment UAT --contentRoot <publish>`) sert donc des pages
stylées. Les user-secrets (chaîne CIT_UAT_RECETTE + secret) sont chargés pour l'env UAT ; aucun
secret versionné.

Sécurité confirmée : env **UAT**, base **CIT_UAT_RECETTE** (barrière 4 de `/uat/session` = base
courante), 19 comptes `@local.test`, canaux journalisés. Mécanisme d'auth : valide → 302 `/` ;
désactivé → `/comptes/acces-refuse` sans cookie ; inexistant/hors-domaine → 404.

---

## 2. Défauts trouvés et CORRIGÉS (recette visuelle)

### D-ROUTE — /dossiers/{guid} : 500 sur TOUTE consultation de dossier (majeur)
- **Reproduit** : ouverture d'un dossier depuis le tableau de bord → page blanche ; `curl` → **500**
  (`AmbiguousMatchException`). Deux pages revendiquaient le même gabarit :
  `FicheDossier` (`/dossiers/{Id:guid}`) **et** `DetailDossier` (`/dossiers/{DossierId:guid}`).
- **Portée** : les deux pages étaient **inatteignables** (500) — invisible en tests unitaires, qui
  rendent les composants sans passer par le routeur.
- **Corrigé** : `FicheDossier` est la page canonique (contrôle d'accès central
  `ObtenirFicheDossierQuery` + `ExigerConsultationDossierAsync`, parcours client et tests de
  cloisonnement) ; `DetailDossier` (vue opérationnelle) reçoit la route distincte
  `/dossiers/{DossierId:guid}/operationnel`.
- **Vérifié** : `/dossiers/{guid}` → **302** (redirection auth), puis rendu authentifié complet —
  h1 « 📁 Dossier DOS-000008 », fil d'Ariane « Accueil › Dossier DOS-000008 », 0 débordement ;
  ticket lié « Accueil › Dossier › Ticket BIL-IMP-000008 ». Le flux dossier→ticket est rétabli.
- **Garde** : `RoutesUniquesTests` refuse tout gabarit de route dupliqué. **Mutation vérifiée**
  (réintroduire le doublon fait échouer la garde).

### D-ENTETE — en-tête authentifié : débordement de PAGE au mobile
- **Reproduit** : à 320 px, l'en-tête (logo 163 px + langue + thème + cloche + menu compte) débordait
  la page de **+188 px** ; +119 px à 390. Partagé par toutes les pages authentifiées.
- **Corrigé** : sous 576 px, logo borné (84 px), sélecteur de langue masqué (réapparaît en sm+),
  paddings/gaps resserrés.
- **Vérifié** : comptes-agents **320/390/768/1024/1366/1920 = 0** débordement ; pages client à
  320 = 0. **Garde** `RecetteAuthentifieePublishTests` + mutation.

### D-SOMBRE — trois contrastes sous le seuil (thème sombre)
| Élément | Avant | Après |
|---|---|---|
| `.btn-outline-primary` (boutons « Consulter ») | 4,23:1 | azure-400 → ~5,5:1 |
| `.cit-user-avatar` (initiale) | 3,23:1 | encre claire → lisible |
| `.btn-light` (filtres « Actifs/Inactifs ») : fond clair figé | 2,96:1 | fond sombre re-teinté |

Corrigés en dark uniquement (clair inchangé, vérifié : `.text-primary` reste `#14547f` 8:1).
**Gardes** + re-scan : plus aucun échec de contenu ; ne restent que les boutons de marque
blanc-sur-azur à 4,16:1 (identiques en clair, décision de token).

---

## 3. Traversée authentifiée (visuelle, 1366 px, clair)

~35 pages ouvertes en session réelle, **stylées**, **0 débordement**, **0 erreur** :
- Client (13) : dashboard, demandes, RDV, import, export, simulation, historique, avoir-exo (×2),
  réclamations (×2), enrôlement, profil, préférences.
- Agent (10) : dashboards + files manifeste, IES, facturation import/export, contrôle documentaire
  import/export, réclamations, agent-gate.
- Superviseur (2) : supervision, disponibilité.
- Admin (10) : valider, comptes-agents, audit, tarification, notifications, pièces-exigées,
  catégories-réclamation, paramètres-SLA, matrice-transitions, règles-escalade.
- Fiches (2) : dossier, ticket (après correction D-ROUTE).

---

## 4. Responsive réel — mesuré (aucune interpolation)

`comptes-agents` (page la plus dense, table large + switcher de persona) :

| 320 | 390 | 768 | 1024 | 1366 | 1920 | Zoom 200 % (~683) |
|---|---|---|---|---|---|---|
| **0** | **0** | **0** | **0** | **0** | **0** | **0** |

Pages client à 320 = 0 ; zoom 200 % sur audit/tarification = 0. Détail :
[`matrice-responsive-authentifiee-publish.md`](matrice-responsive-authentifiee-publish.md).

---

## 5. Thème sombre / Clavier — authentifié

- **Sombre** : scan de contraste sur dashboard client, file manifeste, admin comptes → 3 défauts
  corrigés (§2). Détail : [`audit-dark-mode-authentifie.md`](audit-dark-mode-authentifie.md).
- **Clavier** : Tab sur le dashboard → focus **visible** (`outline: 2px solid`, `:focus-visible`
  actif) sur les contrôles authentifiés ; lien d'évitement premier ; retour de focus modale couvert
  par `ModaleConfirmationFocusTests`. Détail :
  [`audit-clavier-focus-authentifie.md`](audit-clavier-focus-authentifie.md).

---

## 6. Sidebar — non-régression (0 lien interdit)

Confirmée en direct (tour précédent + ce tour) sur admin (8), superviseur (2), client (15),
transporteur (7), agent (4), agent-sans-droits (2). Enforcement serveur : accès direct refusé pour
l'agent sans droits. Aucune régression.

---

## 7. Tests, gardes, mutations

- Suite : **1611/1611** (base 1606 + 5 : `RoutesUniques`, `RecetteAuthentifieePublish` ×4).
- **Mutations vérifiées** : `RoutesUniques` (doublon réintroduit → échec) ; en-tête mobile (logo
  décapé → échec).
- Aucun test retiré/affaibli. `git diff --check` propre.
- Aucune règle métier/tarification/cloisonnement/workflow modifiée. Correctifs CSS + une route.

---

## 8. Score

Voir [`audit-ui-ux-final-publish.md`](audit-ui-ux-final-publish.md). **93 → 94/100** : Responsive
passe à 10/10 (6 largeurs mesurées + zoom + défaut en-tête corrigé). Les autres pertes tiennent à des
décisions externes / audit formel — détaillées dans le fichier.
