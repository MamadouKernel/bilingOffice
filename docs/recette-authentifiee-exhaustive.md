# Recette navigateur authentifiée — mécanisme UAT + résultats

**Date** : 7 septembre 2026. HEAD `1c3cd38` (branche `main`, aucun commit). Baseline d'entrée :
**1594/1594**, 0 échec, 0 ignoré — reproduite avant toute modification.

> **Ce qui a été résolu ce tour :** l'authentification UAT sans exposer de secret. Un mécanisme
> d'ouverture de session de recette a été construit, testé et **prouvé en direct**. Les surfaces
> authentifiées ont été traversées au niveau **DOM/sécurité** (sidebar, cloisonnement, comptes
> négatifs).
>
> **Ce qui reste bloqué :** la recette **visuelle** des pages authentifiées (responsive, sombre,
> zoom, focus) — non par l'authentification, mais par un défaut de service des CSS **propre au
> harnais de prévisualisation** en environnement non-Development (détaillé au §6). Ce point est
> marqué **NON TESTÉ**, jamais remplacé par une supposition.

---

## 1. Mécanisme d'authentification UAT (sans secret)

### Endpoint `GET /uat/session/{email}`
Fichier : [`EndpointsRecetteUat.cs`](../src/CIT.Plateforme.Web/Recette/EndpointsRecetteUat.cs).
Ouvre une **vraie** session pour un compte de recette **déjà amorcé**, via
`SignInManager.SignInAsync` — **sans mot de passe**. Il pose le cookie d'authentification, rien de
plus : **aucune permission créée**, les habilitations restant lues en base à chaque requête.

### Barrières (cumulatives)
| # | Barrière | Mise en œuvre |
|---|---|---|
| 1 | Jamais monté en **Production** | `if (app.Environment.IsProduction()) return;` au montage |
| 2 | Politique UAT complète, **ré-évaluée à chaque requête** | `PolitiqueRecetteUat.Decider` : env ≠ Production, `Uat:ActiverSeed=true`, secret présent, `Uat:NomBase` = base courante = `CIT_UAT_RECETTE` |
| 3 | Compte cible sous le domaine réservé | doit finir par `@local.test` |
| 4 | Politique d'accès **normale** | `RegleAccesConnexion.AccesAutoriseAsync` — un compte désactivé est refusé, comme à la connexion par mot de passe |

Hors de ce cadre exact : **404**. Aucun contournement réutilisable en production.

### Non-exposition du secret
- Le mot de passe UAT vit dans `dotnet user-secrets` (hors dépôt). L'endpoint **ne le lit ni ne le
  transmet** — il signe la session sans lui.
- Chargement des user-secrets **étendu à l'environnement « UAT » uniquement** (Program.cs,
  `IsEnvironment("UAT")`) : jamais en Production.
- `appsettings.UAT.json` ne contient **que** de la configuration non secrète (retry notifications).
  Le profil de lancement `uat` ne porte **que** `ASPNETCORE_ENVIRONMENT=UAT`. **Aucun secret** dans
  un fichier versionné.

### Preuves de refus (curl live contre le serveur UAT)
| Requête | Attendu | Obtenu |
|---|---|---|
| `/uat/session/uat.superviseur@local.test` (valide) | 302 → `/`, session ouverte | **302 → /** ✅ |
| `/uat/session/uat.compte.desactive@local.test` (désactivé) | refusé, pas de session | **302 → /comptes/acces-refuse, aucun Set-Cookie** ✅ |
| `/uat/session/uat.inexistant@local.test` | 404 | **404** ✅ |
| `/uat/session/attaquant@gmail.com` (hors @local.test) | 404 | **404** ✅ |

### Tests (12) + mutation
[`EndpointsRecetteUatTests.cs`](../tests/CIT.Plateforme.Web.Tests/Recette/EndpointsRecetteUatTests.cs) :
- `Verifier` (pur) : périmètre UAT + domaine `@local.test` ; refus Production / base ≠ / seed off / sans secret / adresse hors domaine.
- Intégration (`WebApplicationFactory`) : l'endpoint est **inerte (404, sans cookie)** dans l'environnement de test (base ≠ `CIT_UAT_RECETTE`).
- **Mutation vérifiée** : retirer la barrière de politique fait échouer 5 tests (dont le refus Production).

---

## 2. Environnement de recette (confirmé)
| | |
|---|---|
| Base | **`CIT_UAT_RECETTE`** sur `(localdb)\MSSQLLocalDB` — confirmée par les logs de migration au démarrage |
| Comptes | **19** adresses `@local.test` en base (18 profils + amorçage) — aucune donnée réelle |
| Canaux externes | `CanalJournalise` (journalisation) dans **tous** les environnements — aucun envoi réel possible |
| Secret versionné | **aucun** |

---

## 3. Sidebar par profil — mesure LIVE (0 lien interdit)

Sessions réelles ouvertes via l'endpoint ; liens de navigation lus dans le DOM.

| Profil | Atterrissage | Liens sidebar | Portée | Liens interdits |
|---|---|---|---|---|
| `admin.fonctionnel` | /comptes/valider | 8 (comptes/valider, admin/tarification, modifier-type, admin-catégories, notifications, comptes-agents, pièces-exigées, audit) | Administration | **0** |
| `superviseur` | /supervision | 2 (supervision, disponibilité) | Supervision | **0** |
| `client.normal` | /client/dashboard | 15 (dashboard, profil, demandes, RDV, import/export soumettre, avoir-exo, simulation, historique, réclamations, enrôlement, préférences, 2FA) | Client | **0** |
| `transporteur` | /client/dashboard | 7 (dashboard, demandes, RDV, enrôlement, préférences, 2FA) | Transporteur | **0** |
| `pilote.manifeste` | /agent/dashboard | 4 (dashboard, import/manifeste, import/ies, 2FA) | Agent (ses files) | **0** |
| `agent.sansdroits` | /agent/dashboard | 2 (dashboard, 2FA) | Minimal | **0** |

Complété par 20 tests `NavigationParPermissions` + garde `CablageNavigation`. **Aucun profil ne voit
un lien vers une destination non autorisée.**

---

## 4. Enforcement serveur (défense en profondeur)

`agent.sansdroits`, accès **direct** par URL à des pages hors périmètre :

| URL | Résultat |
|---|---|
| `/administration/comptes-agents` | **Refusé** (accès refusé affiché) ✅ |
| `/billing-import/manifeste` | **Refusé** ✅ |

La sidebar ne propose pas ces liens **et** le serveur refuse l'accès direct. L'absence de lien n'est
jamais la seule protection.

---

## 5. Traversée de pages authentifiées (sans erreur)

Pages ouvertes en session réelle, sans écran blanc ni exception (titre correct, pas de 500) :
- Admin : comptes/valider, admin/tarification, comptes-agents, audit, notifications (5) — toutes OK.
- Dashboards : client, opérateur (agent), supervision, société — tous OK.

*(La couverture des 81 pages n'est pas exhaustive au niveau visuel — voir §6.)*

---

## 6. Recette VISUELLE authentifiée — NON TESTÉE (blocage documenté)

**Constat.** Sous le serveur UAT lancé via le harnais (`dotnet run`, environnement non-Development),
les pages authentifiées s'affichent **sans CSS** dans le navigateur de prévisualisation.

**Cause, isolée par preuve :**
- `curl` (sans négociation de compression) obtient la feuille **complète** : `app.css` = 64 339
  octets, `text/css`, 200. **Le serveur sert donc les CSS correctement.**
- `curl -H "Accept-Encoding: gzip, br"` obtient **0 octet**. Le navigateur de prévisualisation
  négocie la compression → il reçoit la variante précompressée que `MapStaticAssets` sert **vide**
  en Debug hors Development.

**Ce n'est pas un défaut produit** : en Development (tours précédents), les CSS s'affichent ; un
déploiement publié (Release/publish) génère des variantes compressées valides. C'est un artefact du
couple *build Debug + environnement non-Development + navigateur négociant gzip*. Tentatives de
contournement (rebuild propre, `StaticWebAssetsEnableCompression=false`, retrait de l'en-tête
`Accept-Encoding` en middleware UAT) **infructueuses** : `MapStaticAssets` ne rebascule pas sur
l'identité. Elles ont été **retirées** (pas de code mort).

**Conséquence honnête :** les vérifications qui exigent des pages **stylées** sur les surfaces
**authentifiées** sont **NON TESTÉES ce tour** :

| Aspect | Pages anonymes | Pages authentifiées |
|---|---|---|
| Responsive 320→1920 | ✅ (tours précédents) | **NON TESTÉ** |
| Zoom 200 % | ✅ | **NON TESTÉ** |
| Thème sombre / contrastes | ✅ (3 défauts corrigés) | **NON TESTÉ** |
| Clavier / focus visible | ✅ | **NON TESTÉ** |

La feuille `app.css` est **la même** pour toutes les pages ; son comportement responsive/sombre a
été validé sur les pages anonymes. Mais conformément à la consigne, cela **ne vaut pas** validation
des pages authentifiées : « le CSS est le même » n'est pas une mesure.

**Pour lever le blocage :** lancer la recette contre un `dotnet publish` (assets compressés valides)
ou un navigateur qui n'échoue pas sur la variante compressée.

---

## 7. AM-01 / AM-02 / AM-03 et D-04

Non tranchés (décisions métier/infra). `collab.limite` atterrit sur le même dashboard que
`client.normal` — la comparaison fine de leurs périmètres (AM-02) reste une **décision métier**.
Voir [`backlog-ui-ux-ultime.md`](backlog-ui-ux-ultime.md).

---

## 8. Score

Voir [`audit-ui-ux-final-apres-recette-auth.md`](audit-ui-ux-final-apres-recette-auth.md). Le
mécanisme d'authentification et la recette DOM/sécurité authentifiée sont **acquis et prouvés** ;
la recette **visuelle** authentifiée reste **NON TESTÉE** (blocage harnais). Le score n'augmente pas
sur des aspects non mesurés.
