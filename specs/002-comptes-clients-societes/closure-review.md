# Revue de fermeture — Feature 002 : Comptes clients, sociétés et utilisateurs rattachés

**Date** : 2026-08-30
**Sources** : `.specify/memory/constitution.md`, `spec.md`, `plan.md`, `research.md`, `data-model.md`,
`contracts/use-cases.md`, `quickstart.md`, `tasks.md`, `post-implementation-audit.md`,
`docs/01-matrice-exigences.md`, `docs/04-matrice-recette.md`, `specs/001-socle-tickets-workflows/closure-review.md`,
inspection directe du code source, des migrations, de la base SQL Server réelle (`CitPlateformeTests`,
LocalDB) et de l'état Git.

> Cette revue reconstitue chaque score et chaque affirmation **indépendamment** du rapport
> `post-implementation-audit.md` — ce dernier est utilisé comme référence à vérifier, jamais comme
> preuve suffisante en lui-même. Aucune tâche cochée, aucun commentaire de code, aucune recommandation
> antérieure n'est acceptée sans re-vérification directe (build, tests réexécutés, requêtes SQL
> directes, lecture de code).

---

## 1. Résumé exécutif

Feature 002 a traversé : `/speckit.clarify` (3 clarifications validées) → `/speckit.plan` (12
décisions D1-D12) → `/speckit.tasks` (76 tâches) → `/speckit.implement` → un audit
post-implémentation ayant détecté 5 écarts réels → une remédiation ciblée ayant fermé les 5 écarts
(dont un bug critique de liaison de formulaire Blazor SSR découvert et corrigé au passage) → cette
revue de fermeture finale.

**Reconstitution indépendante confirmée** :
- **11/11 exigences CPT/ENT CONFORME**, 0 PARTIEL, 0 ÉCART, 0 NON DÉMONTRÉ.
- **8/8 Must implémentés et testés**, aucun bloqué.
- **CLAR-01/02/03** démontrées par du code et des tests réels, cohérentes entre `spec.md`,
  `research.md`, `data-model.md` et l'implémentation.
- **180/180 tests verts** (31 Domain + 106 Application + 22 Infrastructure/SQL Server réel + 21 Web),
  réexécutés dans cette session, 0 échec.
- **12 migrations** appliquées et rejouables depuis zéro, vérifiées par requête SQL directe (pas par
  `dotnet ef migrations list`, dont une anomalie d'affichage a déjà été documentée comme non fiable).
- **2 décisions de dette technique** (absence de FK sur `UtilisateurSociete.UtilisateurId` et sur
  `Dossier.ClientId`) — vérifiées par requête SQL directe sur la base réelle, toutes deux classées
  **ACCEPTÉE**, aucune ne remplit un critère de blocage de clôture.
- **Documentation alignée** : 3 divergences documentaires mineures détectées et corrigées pendant
  cette revue (`research.md`, `data-model.md`, `quickstart.md`) — purement rédactionnelles, aucun
  changement de comportement.
- **État Git propre** : 7 fichiers Feature 001 modifiés (tous conformes à l'extension additive prévue
  par D11), aucun secret, aucun artefact `bin`/`obj` suivi, un artefact temporaire (`__pycache__`)
  détecté et supprimé pendant cette revue.

**Décision finale : `CLOSE`** (détail section 16).

---

## 2. Score final — reconstitution indépendante des 11 exigences

Reconstitué ligne par ligne à partir de `docs/01-matrice-exigences.md`, du code source
(`src/CIT.Plateforme.{Domain,Application,Infrastructure,Web}/**/Comptes*`, `Dossier.cs`), et d'une
réexécution fraîche de la suite de tests dans cette session (`dotnet test` → 180/180, 0 échec, exit
code 0). Le tableau ci-dessous n'est **pas** une copie de `post-implementation-audit.md` §3 : chaque
cellule a été revérifiée indépendamment (voir sections 4, 6-9 pour le détail des vérifications).

| Exigence | Priorité (source : `docs/01-matrice-exigences.md` L114-124) | Mécanisme implémenté (vérifié) | Test(s) réel(s) exécuté(s) | Statut final |
|---|---|---|---|---|
| CPT-01 | Must | `SocieteCliente.Soumettre/Valider/Rejeter` ; société inutilisable avant `Active` | `SoumettreDemandeCompteTests`, `ValiderDemandeCompteTests`, `ConcurrenceValidationCompteSqlTests` | **CONFORME** |
| CPT-02 | Must | Profil (`TypeClient`) déclaré à la soumission, confirmé non modifié à la validation | Idem CPT-01 | **CONFORME** |
| CPT-03 | Must | `Connexion.razor` (`PasswordSignInAsync` réel via Identity) + récupération mdp via jetons Identity natifs | `VerifierAccesCompteTests`, `RecuperationMotDePasseTests` (dont le nouveau test HTTP réel de non-régression du bug de binding) | **CONFORME** |
| CPT-04 | Must | Cloisonnement dossier via `CloisonnementService` (Feature 001, inchangé) + `ConsulterProfilSocieteQuery`/`ListerDemandesEnAttenteQuery` (remédiation IDOR) | `RecSce14CloisonnementCompteTests`, `ConsulterProfilSocieteTests`, `ListerDemandesEnAttenteTests` — recherche `@inject I*Repository` dans `Components/Pages/Comptes/` : **aucune occurrence** (voir §6) | **CONFORME** |
| CPT-05 | Must | Renvoi structurel à ENT-01→04, aucun comportement propre (confirmé par lecture du CDC) | Couvert par les tests ENT-01→04 | **CONFORME** |
| CPT-06 | **Should** | `ModifierTypeClientCommand` (tracé en audit) + `Dossier.TypeClient` (snapshot non réécrit) | `ModifierTypeClientTests`, `NonRetroactiviteTypeClientSqlTests` (SQL Server réel, scénario 5 étapes) | **CONFORME** |
| CPT-07 | **Could** | `PasswordSignInAsync` → `RequiresTwoFactor` → `VerifierCode.razor` → `TwoFactorAuthenticatorSignInAsync` | `ParcoursConnexion2FaTests` (5 tests HTTP réels A-E via `WebApplicationFactory`) | **CONFORME** |
| ENT-01 | Must | `RattacherUtilisateurCommand`, cloisonné à la société de l'appelant | `RattacherUtilisateurTests` | **CONFORME** |
| ENT-02 | Must | Invariant `NombreAdministrateursActifs == COUNT(EstAdmin && EstActif)` maintenu sur les 8 transitions, gardé par `EstActif`/`EstAdministrateur` avant mutation | `InvariantCompteurAdministrateurTests` (6 cas), CAS1-5 SQL Server réel | **CONFORME** |
| ENT-03 | Must | `DeterminerVisibiliteDemandesQuery` — 5 cas A-E | `DeterminerVisibiliteDemandesTests` | **CONFORME** |
| ENT-04 | **Should** | Audit via `IJournalisationService` (Feature 001, inchangé) pour toutes les opérations de gestion | `AuditGestionCollaborateursTests`, `ModifierTypeClientTests` | **CONFORME** |

**Total reconstitué : 11 CONFORME / 0 PARTIEL / 0 ÉCART / 0 NON DÉMONTRÉ.** Ce total est identique à
celui annoncé par `post-implementation-audit.md`, mais a été obtenu par revérification indépendante
de chaque ligne (relecture de code, exécution réelle des tests cités, et non par confiance dans le
rapport).

---

## 3. Matrice Must — reconstitution indépendante depuis `docs/01-matrice-exigences.md`

Lu directement (lignes 114-124) : **CPT-01, CPT-02, CPT-03, CPT-04, CPT-05 = Must** ; **CPT-06 =
Should** ; **CPT-07 = Could** ; **ENT-01, ENT-02, ENT-03 = Must** ; **ENT-04 = Should**.

**8 Must** = CPT-01, CPT-02, CPT-03, CPT-04, CPT-05, ENT-01, ENT-02, ENT-03 — **2 Should** = CPT-06,
ENT-04 — **1 Could** = CPT-07. Total 11. Confirme indépendamment le décompte de `spec.md`.

| Must | Implémenté | Testé | PARTIEL/NON DÉMONTRÉ/bloqué ? |
|---|---|---|---|
| CPT-01 | ✅ | ✅ | Non |
| CPT-02 | ✅ | ✅ | Non |
| CPT-03 | ✅ | ✅ | Non |
| CPT-04 | ✅ | ✅ | Non |
| CPT-05 | ✅ (renvoi structurel) | ✅ (via ENT) | Non |
| ENT-01 | ✅ | ✅ | Non |
| ENT-02 | ✅ | ✅ | Non |
| ENT-03 | ✅ | ✅ | Non |

**8/8 Must CONFORME, aucun PARTIEL, aucun NON DÉMONTRÉ, aucun bloqué.**

---

## 4. CLAR-01 / CLAR-02 / CLAR-03 — cohérence cross-documents

Vérifié en relisant le texte exact de `spec.md` § Clarifications (lignes 649-705) et en le comparant
au code/tests.

| Clarification | `spec.md` (texte exact) | `research.md`/`data-model.md` | Code | Tests |
|---|---|---|---|---|
| **CLAR-01** — modèle hybride société+auteur | « La demande... est rattachée à la société... et porte en plus un champ auteur individuel distinct » | `data-model.md` §2 : `AuteurId : Guid?` distinct de `ClientId` (société) ; `research.md` D11 précise cette extension additive | `Dossier.ClientId` (société, non nullable) + `Dossier.AuteurId` (`Guid?`) — deux propriétés EF Core mappées distinctement, confirmé par requête SQL directe (`uniqueidentifier NOT NULL` / `NULL`) | `DossierAuteurTests`, `DeterminerVisibiliteDemandesTests` |
| **CLAR-02** — admin voit tout malgré droit individuel restreint | « Le rôle Administrateur de société porte de facto le droit... indépendamment de tout paramétrage individuel » | `data-model.md` invariant #5 | `DeterminerVisibiliteDemandesQuery` : cas D (administrateur) ignore `DroitConsultation` individuel | `DeterminerVisibiliteDemandesTests` cas D |
| **CLAR-03** — plusieurs admins possibles, dernier admin protégé | « plusieurs administrateurs peuvent coexister... Le système DOIT empêcher la désactivation... du dernier administrateur actif » | `data-model.md` invariant #2 ; `research.md` D6/D7 (concurrence optimiste) | `SocieteCliente.DecrementerAdministrateursActifs()` lève `DernierAdministrateurActifException` si compteur ≤ 1 ; gardes `EstActif`/`EstAdministrateur` dans `GererRoleAdministrateurCommand` | CAS1 (1 admin, désactivation refusée), CAS2 (1 admin, retrait rôle refusé), CAS3/4/5 (2 admins, concurrence réelle SQL Server — une seule opération réussit) |

**Les trois clarifications sont cohérentes de bout en bout, spec → recherche → modèle → code → test,
sans divergence.**

---

## 5. TypeClient — CPT-06 (voir aussi §8 pour le détail)

Confirmé : le champ `SocieteCliente.TypeClient` (profil, mutable via `ModifierTypeClientCommand`) est
bien distinct du champ `Dossier.TypeClient` (instantané figé à la création, jamais réécrit). Le
scénario NORMAL→dossier1 NORMAL→société DEPOSIT→dossier1 reste NORMAL→dossier2 nouveau = DEPOSIT est
démontré par `NonRetroactiviteTypeClientSqlTests.cs` sur SQL Server réel (5 étapes, relu en détail
dans `post-implementation-audit.md` §8, comportement confirmé cohérent avec le code source de
`Dossier.Creer(...)` et `ModifierTypeClientCommand`). **Aucune logique de Billing Office** (routage,
codification export, contrôle BL) n'a été introduite — `Dossier.TypeClient` est un champ de données
brut, sans branchement conditionnel de workflow métier dans le code Feature 002.

---

## 6. Sécurité — fermeture des 2 écarts IDOR

Recherche ciblée exécutée dans cette session : aucune page de `src/CIT.Plateforme.Web/Components/Pages/Comptes/`
n'injecte de repository directement (`@inject I*Repository` → 0 résultat). Les deux pages
initialement en écart (`GestionCollaborateurs.razor`, `ValiderDemandes.razor`) injectent désormais
respectivement `ConsulterProfilSocieteQuery` et `ListerDemandesEnAttenteQuery`, deux requêtes
applicatives qui vérifient l'autorisation et le périmètre société côté serveur avant tout retour de
données. Confirmé par lecture directe des deux fichiers `.razor` et de leurs commandes/requêtes
associées dans `src/CIT.Plateforme.Application/Comptes/`.

**Portée de cette vérification** : ciblée sur les composants Feature 002, conformément à la consigne
de ne pas refaire un audit de sécurité global de Feature 001. Aucun nouvel accès direct non autorisé
n'a été détecté dans le périmètre Feature 002.

**Statut : CONFORME, les 2 écarts IDOR sont fermés.**

---

## 7. 2FA — CPT-07

Flux vérifié par lecture directe de `Connexion.razor` et `VerifierCode.razor` : mot de passe soumis à
`SignInManager.PasswordSignInAsync` → si `resultat.RequiresTwoFactor`, redirection vers
`/comptes/verifier-code` (après vérification d'accès société via `VerifierAccesCompteQuery`) → saisie
du code TOTP → `SignInManager.TwoFactorAuthenticatorSignInAsync` → cookie d'authentification émis
uniquement en cas de succès.

Les 5 tests HTTP réels de `ParcoursConnexion2FaTests.cs` (exécutés dans cette session, verts) :
- **A** — utilisateur sans 2FA : mot de passe seul suffit, connexion directe.
- **B** — utilisateur avec 2FA : mot de passe seul ne connecte pas, redirection vers le défi.
- **C** — code TOTP valide : connexion réussie, accès à une page `[Authorize]` confirmé (200).
- **D** — code TOTP invalide : refus, pas de connexion.
- **E** — utilisateur désactivé : refus même avec un code TOTP valide.

**Statut : CONFORME.** Le mécanisme est un vrai défi HTTP de bout en bout, pas une simple
vérification des préconditions Identity.

---

## 8. TypeClient — non-rétroactivité (détail technique)

`Dossier` porte deux champs nullables additifs : `AuteurId : Guid?` (Feature 002 initial) et
`TypeClient : TypeClient? ` (remédiation post-audit). `Dossier.Creer(...)` accepte ces deux paramètres
en position optionnelle, valeur par défaut `null` — **aucun appel existant de Feature 001 n'est
cassé** (vérifié : `CreerTicketCommand.cs` continue d'appeler `Dossier.Creer(numeroDossier,
requete.ClientId, maintenant)` à 3 arguments, sans régression).

**Précision documentée (déjà signalée lors de la remédiation, reconfirmée ici)** : le câblage
automatique de `AuteurId`/`TypeClient` depuis un flux métier réel de création de ticket (ex. via
Billing Office ou un futur point d'entrée Feature 002) reste **hors périmètre** de cette feature — le
mécanisme de snapshot est prouvé par construction directe dans les tests, pas par un flux de
production automatique. Ceci est cohérent avec le périmètre défini par `spec.md` (« Hors périmètre
Feature 002 » — Workflow Billing Office) et ne constitue pas un écart, seulement une limite de portée
déjà documentée dans `data-model.md` §2.

---

## 9. Concurrence — ENT-02 / invariant compteur administrateurs

Invariant formel vérifié : `SocieteCliente.NombreAdministrateursActifs == COUNT(UtilisateurSociete
WHERE EstAdministrateur = true AND EstActif = true)`.

Transitions gardées (confirmé par lecture de `GererRoleAdministrateurCommand.cs` — les gardes
`if (cible.EstActif)` sont présentes aux 4 emplacements attendus) et de `UtilisateurSociete.cs`
(`Activer`/`Desactiver` ne mutent le compteur que si `EstAdministrateur == true`).

| Cas | Scénario | Résultat attendu | Résultat vérifié |
|---|---|---|---|
| CAS1 | 1 admin actif, tentative de désactivation | Refus (`DernierAdministrateurActifException`) | ✅ |
| CAS2 | 1 admin actif, tentative de retrait du rôle | Refus | ✅ |
| CAS3 | 2 admins actifs, deux désactivations concurrentes | Une seule réussit, 1 admin actif final | ✅ (SQL Server réel) |
| CAS4 | 2 admins actifs, désactivation de A + retrait de rôle de B concurrents | Une seule réussit, 1 admin actif final | ✅ (SQL Server réel) |
| CAS5 | 2 admins actifs, deux retraits de rôle concurrents | Une seule réussit, 1 admin actif final | ✅ (SQL Server réel) |

**Aucune dérive résiduelle du compteur** n'a été détectée par `InvariantCompteurAdministrateurTests`
(6 cas couvrant les 8 transitions), qui interroge directement la base pour comparer le compteur
dénormalisé au `COUNT` réel après chaque transition.

**Statut : CONFORME.**

---

## 10. REC-SCE — portion applicable

Seule la portion de **REC-SCE-14** exerçant CPT-04 (« un client ne voit pas les tickets d'un autre »)
est applicable à Feature 002, conformément à `quickstart.md` §5.1 et à la méthode déjà appliquée par
`specs/001-socle-tickets-workflows/recipe-scope-audit.md`. Test associé :
`RecSce14CloisonnementCompteTests`, inclus dans les 21 tests Web réexécutés dans cette session (vert).

Les 7 Must sans scénario CDC applicable (CPT-01, CPT-02, CPT-03, CPT-05, ENT-01, ENT-02, ENT-03) sont
couverts par des TEST-SPEC dérivés des critères d'acceptation de `spec.md`, jamais présentés comme des
scénarios CDC officiels — confirmé, aucune nouvelle entrée REC-SCE n'a été inventée dans le code ou
les tests.

**Statut : conforme au périmètre défini, aucune extension non autorisée de REC-SCE.**

---

## 11. Migrations

Vérifié par requête SQL directe sur `CitPlateformeTests` (LocalDB), pas par `dotnet ef migrations
list` (dont l'anomalie d'affichage ponctuelle a déjà été documentée comme non fiable dans les sessions
précédentes) :

- **12 migrations** au total (8 Feature 001 + 4 Feature 002), toutes présentes dans
  `sys.foreign_keys`/le schéma réel de la base recréée depuis zéro pendant la session de remédiation.
- Les 4 migrations Feature 002 : `AjoutAuteurIdDossier`, `AjoutComptesSocietesUtilisateursRattaches`,
  `AjoutDemandeurIdSocieteCliente`, `RemediationPostAuditFeature002`.
- **Foreign keys confirmées par requête directe** (`sys.foreign_keys`, cette session) : la base réelle
  contient exactement 7 FK — les 6 FK Identity standard (`AspNetRoleClaims`, `AspNetUserClaims`,
  `AspNetUserLogins`, `AspNetUserRoles` ×2, `AspNetUserTokens`, toutes `CASCADE`) plus
  `FK_UtilisateursSociete_Societes_SocieteId` (`NO_ACTION` = `RESTRICT`). Confirme exactement
  l'affirmation de `post-implementation-audit.md` §10 — vérifié indépendamment, pas copié.
- **Aucune migration destructive** : aucune colonne/table Feature 001 supprimée ou renommée (confirmé
  par lecture des 4 fichiers de migration Feature 002 — toutes `AddColumn`/`CreateTable`/`AddForeignKey`,
  aucun `DropColumn`/`DropTable`/`RenameColumn`).
- **Base recréable depuis zéro** : démontré pendant la session de remédiation (suppression + `dotnet ef
  database update` complet, base de test `CitPlateformeTests` actuellement interrogée dans cette
  section est le résultat de cette recréation).

**Statut : CONFORME.**

---

## 12. Tests — totaux reconstitués

Suite complète réexécutée fraîchement dans cette session de revue :

| Projet | Total | Résultat |
|---|---|---|
| `CIT.Plateforme.Domain.Tests` | 31 | 31/31 verts |
| `CIT.Plateforme.Application.Tests` | 106 | 106/106 verts |
| `CIT.Plateforme.Infrastructure.Tests` | 22 | 22/22 verts (SQL Server réel, LocalDB) |
| `CIT.Plateforme.Web.Tests` | 21 | 21/21 verts |
| **Total** | **180** | **180/180, 0 échec** |

Confirme exactement le total annoncé par `post-implementation-audit.md` — **vérifié par réexécution
réelle dans cette session**, pas par confiance dans le chiffre du rapport.

Composantes spécifiquement revérifiées :
- **REC-SCE-14 (portion CPT-04)** : verte (`RecSce14CloisonnementCompteTests`, inclus dans les 21 Web).
- **Concurrence** (CAS1-5) : verte, SQL Server réel (`CIT.Plateforme.Infrastructure.Tests`).
- **Non-rétroactivité TypeClient SQL Server** : verte (`NonRetroactiviteTypeClientSqlTests`).
- **2FA Web** (5 tests A-E) : verts, HTTP réels via `WebApplicationFactory`.

**Statut : CONFORME.**

---

## 13. Dette technique — 2 décisions à classifier

### A. `UtilisateurSociete.UtilisateurId` sans FK vers `AspNetUsers.Id`

**Vérification directe** (cette session) : confirmé par requête SQL sur `sys.foreign_keys` — aucune
contrainte `FK_UtilisateursSociete_AspNetUsers_UtilisateurId` n'existe dans la base réelle. Une
contrainte d'unicité existe en revanche (`HasIndex(u => u.UtilisateurId).IsUnique()`, confirmée dans
`PlateformeDbContext.cs` L270), garantissant qu'un utilisateur Identity n'est jamais rattaché à plus
d'une société à la fois — l'invariant fonctionnel #3 de `data-model.md` est donc protégé même sans FK.

- **Risque** : un `ApplicationUser` supprimé physiquement laisserait un `UtilisateurSociete` orphelin
  (référence à un `Id` inexistant). Recherche exhaustive dans `src/` (cette session) : **aucun appel**
  à `UserManager.DeleteAsync` ou équivalent n'existe dans tout le code source — Identity ne supprime
  jamais physiquement un utilisateur, seule la désactivation (`EstActif = false`) est utilisée,
  conformément au principe VIII (rien n'est supprimé physiquement) déjà appliqué à l'identique en
  Feature 001. Le risque réel est donc **théorique tant qu'aucune fonctionnalité de suppression
  physique n'est introduite**.
- **Justification** : ce découplage Identity/domaine est une décision intentionnelle documentée
  (`research.md` D1) et cohérente avec le principe déjà établi par Feature 001 de ne jamais coupler
  d'entité métier par FK physique à `AspNetUsers` (aucune table métier de Feature 001 ne porte de FK
  vers `AspNetUsers` non plus, à titre de comparaison).
- **Classification : ACCEPTÉE.**
- **Échéance recommandée** : à réévaluer uniquement si une future feature introduit une suppression
  physique réelle de compte Identity (ex. droit à l'effacement RGPD) — dans ce cas, préférer un
  contrôle applicatif explicite (refus de suppression si des rattachements existent) plutôt qu'une FK,
  pour rester cohérent avec le principe VIII. Pas d'échéance fixe tant que ce scénario n'est pas
  planifié.

### B. `Dossier.ClientId` sans FK vers `Societes.Id`

**Vérification directe** (cette session) : confirmé par requête SQL — aucune FK
`Dossiers.ClientId → Societes.Id` n'existe. `PlateformeDbContext.cs` ne définit aucun `HasOne`/
`HasForeignKey` pour cette relation (recherche confirmée : aucune correspondance).

- **Risque** : très faible. `Dossier` est une entité du socle Feature 001, dont `ClientId` a toujours
  été un identifiant abstrait de client, indépendant de toute structure de société (Feature 001 a été
  conçu, implémenté et testé avant l'existence de la table `Societes`). Le cloisonnement effectif
  (garantie que CPT-04 n'est jamais violé) est assuré **applicativement** par `CloisonnementService`,
  inchangé depuis Feature 001 et déjà couvert par les scénarios REC-SCE-09→14 de Feature 001 et
  REC-SCE-14/CPT-04 de Feature 002 — la protection ne dépend pas de la présence d'une FK physique.
- **Justification** : ajouter cette FK coupleraient structurellement une entité du socle générique
  (`Dossier`, conçu pour rester agnostique du type exact de client) à un modèle spécifique introduit
  par une feature ultérieure (`Societes`), ce qui contredirait le principe d'extension additive de
  D11 (« ne pas modifier le comportement de `CloisonnementService` ») et le principe d'architecture de
  Feature 001 de ne pas présumer la structure exacte du client. Cette absence de FK est donc un choix
  d'architecture délibéré, pas un oubli.
- **Classification : ACCEPTÉE.**
- **Échéance recommandée** : aucune — cette absence de FK est structurellement correcte tant que
  `Dossier` reste une entité générique du socle partagée par plusieurs types de client potentiels
  (au-delà des seules sociétés clientes). À ne reconsidérer que si une décision d'architecture future
  restreint définitivement `Dossier.ClientId` à ne jamais référencer autre chose qu'une `Societe`.

**Aucune des deux décisions ne remplit un critère de blocage de clôture** (n'empêche pas la
satisfaction d'un Must, ne compromet ni sécurité ni intégrité fonctionnelle démontrée, n'invalide
aucun critère d'acceptation, ne bloque aucun scénario REC-SCE-14 applicable). **Aucune correction de
code n'est requise avant la clôture.**

---

## 14. Points ouverts (non résolus, à dessein)

| Point | Nature | Statut |
|---|---|---|
| PC-08 — informations/justificatifs exigés par type de client | Métier | Ouvert, non fixé par ce guide (`quickstart.md` §3) — n'affecte pas la conformité fonctionnelle démontrée |
| PC-14 — politique de conservation d'un compte désactivé/refusé | Métier | Ouvert, non fixé (`quickstart.md` §3) — architecture compatible, valeur non inventée |
| NOUVEAU-03 — changement de société de rattachement | Métier, hors périmètre | Non implémenté, conformément à la consigne explicite de `/speckit.implement` |
| Câblage automatique `AuteurId`/`TypeClient` depuis un flux métier réel (Billing Office) | Technique, hors périmètre | Non implémenté, mécanisme prouvé par construction directe en test uniquement (voir §8) |
| Dette technique A (FK `UtilisateurId`) | Technique | Acceptée (§13) |
| Dette technique B (FK `Dossier.ClientId`) | Technique | Acceptée (§13) |
| Dettes techniques héritées de Feature 001 (concurrence admin sur entités de config, audit append-only applicatif seul, environnement SQL CI) | Technique | Déjà closes/acceptées par `specs/001-socle-tickets-workflows/closure-review.md` §6 — sans lien avec Feature 002, non rouvertes ici |

**Aucun de ces points n'a été tranché par cette revue** — conformément à la consigne, ils restent des
décisions métier, d'outillage ou de portée future, sans impact sur la conformité fonctionnelle
démontrée de Feature 002.

---

## 15. État Git

Vérifié par `git status --short` (cette session, après nettoyage) :

**7 fichiers Feature 001 modifiés** (extension additive prévue par D11, tous cohérents avec le
périmètre annoncé) :
- `src/CIT.Plateforme.Application/Abstractions/Repositories.cs` (+26 lignes) — nouvelles interfaces de
  repository Feature 002.
- `src/CIT.Plateforme.Domain/Securite/NatureUtilisateur.cs` (+12 lignes) — extension additive.
- `src/CIT.Plateforme.Domain/Tickets/Dossier.cs` (+21/-2 lignes) — ajout `AuteurId`/`TypeClient`
  nullable, paramètres optionnels additifs sur `Creer(...)`, compatibilité des appels existants
  préservée (confirmé §8).
- `src/CIT.Plateforme.Infrastructure/DependencyInjection.cs` (+33 lignes) — `AddDefaultTokenProviders`
  + enregistrements Feature 002.
- `src/CIT.Plateforme.Infrastructure/Persistence/PlateformeDbContext.cs` (+40 lignes) — mappings
  Feature 002 + FK remédiée.
- `src/CIT.Plateforme.Infrastructure/Persistence/Repositories.cs` (+30 lignes) — implémentations
  Feature 002.
- `src/CIT.Plateforme.Infrastructure/Persistence/Migrations/PlateformeDbContextModelSnapshot.cs` —
  snapshot EF Core mis à jour (généré automatiquement par les migrations).

**Aucune ligne de Feature 001 supprimée ou modifiée en comportement** — chaque diff constaté est un
ajout net, cohérent avec le mandat « extension additive uniquement » de D11.

**Nouveaux répertoires/fichiers Feature 002** (non suivis, prêts à être ajoutés) :
- `specs/002-comptes-clients-societes/` (spec, plan, research, data-model, contracts, quickstart,
  tasks, post-implementation-audit, ce fichier de clôture).
- `src/CIT.Plateforme.Domain/Comptes/`, `src/CIT.Plateforme.Application/Comptes/`,
  `src/CIT.Plateforme.Web/Components/Pages/Comptes/`.
- 4 migrations Feature 002 (fichiers `.cs` + `.Designer.cs`).
- `tests/CIT.Plateforme.{Domain,Application,Infrastructure,Web}.Tests/Comptes/`,
  `tests/CIT.Plateforme.Domain.Tests/Tickets/DossierAuteurTests.cs`.
- `.claude/launch.json` — outil de session (prévisualisation navigateur), pas un artefact de la
  feature elle-même.

**Vérifications de propreté** (cette session) :
- Aucun secret détecté (pas de `.env`, clé, mot de passe en clair dans les fichiers modifiés/nouveaux).
- Aucun artefact `bin`/`obj` suivi par Git.
- Un artefact temporaire `.specify/extensions/companion/scripts/__pycache__/` détecté puis **supprimé**
  pendant cette revue — confirmé absent par un `git status` de suivi.

**Aucun commit n'a été effectué** pendant cette revue, conformément à la consigne.

---

## 16. Décision finale

### Vérification des critères de clôture (section 14 de la demande)

| Critère | Statut |
|---|---|
| 11/11 exigences tracées CDC→spec→plan→tasks→code→test | ✅ |
| Tous les Must (8/8) sont CONFORME | ✅ |
| 0 écart fonctionnel | ✅ |
| 0 écart de sécurité (IDOR fermés, contrôle serveur confirmé) | ✅ |
| 0 NON DÉMONTRÉ bloquant | ✅ |
| CLAR-01/02/03 démontrées et cohérentes cross-documents | ✅ |
| Portion applicable de REC-SCE-14 verte | ✅ |
| Migrations rejouables depuis zéro, FK vérifiées par SQL direct | ✅ |
| Tests intégralement verts (180/180) | ✅ |
| Feature 001 sans régression (7 fichiers modifiés, tous additifs, 0 comportement changé) | ✅ |
| Aucune décision métier ouverte requise pour le fonctionnement de Feature 002 (PC-08/PC-14 sont des paramétrages futurs, pas des blocages fonctionnels) | ✅ |
| Dette technique restante explicitement acceptée et justifiée (2/2 décisions classées ACCEPTÉE) | ✅ |

**12/12 critères satisfaits.**

## **CLOSE**

**Feature 002 (Comptes clients, sociétés et utilisateurs rattachés) est déclarée CLÔTURÉE.**

**Justification** :
- Le score de 11/11 CONFORME n'est pas un report du rapport d'audit : il a été reconstitué exigence
  par exigence à partir du code, des tests réexécutés et de `docs/01-matrice-exigences.md`.
- Les 5 écarts de l'audit initial (2FA inerte, 2 IDOR, dérive de compteur ENT-02, absence de snapshot
  CPT-06) sont fermés et redémontrés indépendamment dans cette revue (sections 5-9), au-delà de la
  confiance dans le rapport de remédiation.
- Un bug critique non lié à la liste initiale (`[SupplyParameterFromForm]` mal lié sur tous les
  formulaires SSR statiques) a été découvert et corrigé pendant la remédiation — sa correction est
  également revérifiée ici via des tests HTTP réels, pas de simples assertions Identity.
- Les 2 seules dettes techniques encore ouvertes (absence de FK sur 2 relations) sont classées
  ACCEPTÉE avec risque, justification et échéance documentés — aucune ne remplit un critère de
  blocage.
- La documentation de conception (`research.md`, `data-model.md`, `quickstart.md`) a été mise à jour
  pendant cette revue pour refléter fidèlement l'état final implémenté — ces corrections sont
  purement rédactionnelles (aucun changement de comportement).
- L'état Git est propre : les seules modifications de Feature 001 sont additives et documentées, aucun
  secret ni artefact indésirable ne subsiste.

### Éléments à inclure dans le futur commit (présentés pour validation humaine — aucun `git add`/`git commit` exécuté)

**Fichiers Feature 001 modifiés (extension additive D11)** :
- `src/CIT.Plateforme.Application/Abstractions/Repositories.cs`
- `src/CIT.Plateforme.Domain/Securite/NatureUtilisateur.cs`
- `src/CIT.Plateforme.Domain/Tickets/Dossier.cs`
- `src/CIT.Plateforme.Infrastructure/DependencyInjection.cs`
- `src/CIT.Plateforme.Infrastructure/Persistence/PlateformeDbContext.cs`
- `src/CIT.Plateforme.Infrastructure/Persistence/Repositories.cs`
- `src/CIT.Plateforme.Infrastructure/Persistence/Migrations/PlateformeDbContextModelSnapshot.cs`

**Nouveaux répertoires/fichiers Feature 002** :
- `specs/002-comptes-clients-societes/` (intégralement : spec, plan, research, data-model, contracts,
  quickstart, tasks, post-implementation-audit, closure-review)
- `src/CIT.Plateforme.Domain/Comptes/`
- `src/CIT.Plateforme.Application/Comptes/`
- `src/CIT.Plateforme.Web/Components/Pages/Comptes/`
- 4 migrations : `20260829200147_AjoutAuteurIdDossier*`,
  `20260829200531_AjoutComptesSocietesUtilisateursRattaches*`,
  `20260829202640_AjoutDemandeurIdSocieteCliente*`, `20260829213603_RemediationPostAuditFeature002*`
- `tests/CIT.Plateforme.Domain.Tests/Comptes/`, `tests/CIT.Plateforme.Domain.Tests/Tickets/DossierAuteurTests.cs`
- `tests/CIT.Plateforme.Application.Tests/Comptes/`
- `tests/CIT.Plateforme.Infrastructure.Tests/Comptes/`
- `tests/CIT.Plateforme.Web.Tests/Comptes/`

**À exclure ou traiter séparément (à la discrétion de l'utilisateur, hors périmètre de cette revue)** :
- `.claude/launch.json` — outil de session (prévisualisation navigateur), sans rapport fonctionnel
  avec Feature 002 ; à évaluer si l'utilisateur souhaite le versionner comme configuration d'équipe ou
  le laisser local.

**Feature 003 n'a pas été commencée par cette revue et ne le sera pas tant qu'une validation humaine
explicite n'aura pas été donnée sur ce résultat.**
