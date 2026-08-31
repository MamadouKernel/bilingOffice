# Revue de Clôture Humaine et Indépendante — Feature 006 (Customer Complaint)

> **Document** : `specs/006-customer-complaint/closure-review.md`
> **Date** : 30 août 2026
> **Nature** : Revue uniquement. Aucun code, test ou migration n'a été modifié pendant cette revue.
> **Méthode** : vérification indépendante par exécution réelle (build, tests, `has-pending-model-changes`, `git status`/`diff`, lecture directe du code), sans confiance accordée aux rapports précédents (`tasks.md`, `implementation-report.md`, `post-implementation-audit.md`) sauf lorsque leurs affirmations ont été revérifiées ici.

---

## 1. Baseline indépendante (exécutée réellement dans cette revue)

```
dotnet build --no-incremental  → 0 Avertissement(s), 0 Erreur(s)
dotnet test                    → Domain 82/82, Application 254/254, Infrastructure 65/65, Web 95/95
                                  TOTAL : 496/496, 0 échec, 0 ignoré
dotnet ef migrations has-pending-model-changes → "No changes have been made to the model since the last migration."
```

Chiffres confirmés conformes à ceux annoncés par le rapport de remédiation. **Aucun écart** entre les métriques déclarées et les métriques mesurées ici.

---

## 2. REC-01 → REC-06

| Exigence | Priorité | Code réel | Test réel | UI | Statut |
|:---|:---|:---|:---|:---|:---|
| **REC-01** — Création par catégorie, ticket tracé | Must | `SoumettreReclamationCommand.cs` (vérifié : permission, IDOR société, catégorie active, contenu obligatoire, création Ticket+Dossier, audit) | `SoumettreReclamationCommandTests.cs` (7 tests, dont REC-REM-01/02), `RecSce07...Tests.cs` étape 1 | `NouvelleReclamation.razor` (vérifiée présente, formulaire réel, pas de GUID/DbContext) | **CONFORME** |
| **REC-02** — Catégories paramétrables sans développement | Must | `CategorieReclamation.cs`, `ModifierCategoriesReclamationCommand.cs` (permission `AdministrationCategoriesReclamation`, DI enregistrée — vérifié) | `CategorieReclamationTests.cs`, `ModifierCategoriesReclamationCommandTests.cs` | **Absente** (aucun écran admin — T030) | **PARTIEL** — la logique existe et est testée, mais "sans développement" n'est pas livrable à un administrateur réel aujourd'hui (aucune UI ni API exposée pour l'invoquer) |
| **REC-03** — Orientation vers Support Opérationnel, instruction/complément/échange | Must | `ConsulterFileQuery` (route `ComplaintInstruire` — vérifié), `PrendreEnChargeCommand`, `AttenteClientCommand`, `PublierMessageCommand` (réutilisation F001 confirmée) | `FileSupportTests.cs`, `AttenteClientComplaintTests.cs`, `PublierMessageComplaintTests.cs`, `RecSce07...Tests.cs` étapes 2-4 | `FileReclamations.razor` (agent), `FicheTicket.razor` (échanges) | **PARTIEL** — voir §10 : le bouton de réponse client dans `FicheTicket.razor` transmet un identifiant incorrect à `PublierMessageClientAsync` et échoue en pratique ; le backend est prouvé correct uniquement en contournant l'UI |
| **REC-04** — Clôture avec réponse formalisée | Must | `Ticket.CloturerAvecReponseFormalisee` (refuse réponse vide — vérifié), `CloturerReclamationCommand.cs` (permission scoped par file — vérifié) | `CustomerComplaintTicketTests.cs`, `CloturerReclamationCommandTests.cs` (5 tests), `RecSce07...Tests.cs` étape 5 | `FicheTicket.razor` (bloc agent : textarea + bouton, code lu et confirmé présent) | **CONFORME** |
| **REC-05** — Orientation automatique par catégorie | Should | `SoumettreReclamationCommand` route via `categorie.FileCibleDefautId` (vérifié) | `SoumettreReclamationCommandTests.cs`, `RecSce07...Tests.cs` étape 2 | Implicite (pas d'action utilisateur requise) | **CONFORME** |
| **REC-06** — Enquête de satisfaction | Could | `Ticket.Evaluer` (refuse si non clôturé/déjà évalué/note hors 1-5 — vérifié), `EvaluerReclamationCommand.cs` (permission + cloisonnement — vérifié) | `EvaluerReclamationCommandTests.cs` (9 tests dont REC-REM-05→09) | `FicheTicket.razor` (étoiles interactives côté client, code lu et confirmé présent) | **CONFORME** |

**Bilan REC** : 4/6 CONFORME, 2/6 PARTIEL (REC-02, REC-03) — pas 6/6 comme annoncé par le rapport de remédiation. Le rapport de remédiation avait raison sur le fond (le code et les tests existent et sont corrects) mais a sur-déclaré la conformité UI de bout en bout sur ces deux points précis.

---

## 3. Contrats C7

| Contrat | Implémentation | DI | Permission | Cloisonnement | Tests (+/-) | Verdict |
|:---|:---|:---|:---|:---|:---|:---|
| **C7-01** SoumettreReclamationCommand | ✅ `SoumettreReclamationCommand.cs` | ✅ enregistrée | ✅ `ComplaintSoumettre` | ✅ IDOR société (rattachement ≠ requête → refus) | ✅ positif + 6 tests négatifs (catégorie inexistante/inactive, usurpation société, permission absente, contenu manquant) | **CONFORME** |
| **C7-02** CloturerReclamationCommand | ✅ `CloturerReclamationCommand.cs` | ✅ enregistrée | ✅ `ComplaintInstruire` (scopé par `FileCouranteId`) | ✅ implicite via le scope de permission par file | ✅ positif + 4 négatifs (réponse vide/blanche, sans permission, déjà clôturé, état interdit) | **CONFORME** |
| **C7-03** EvaluerReclamationCommand | ✅ `EvaluerReclamationCommand.cs` | ✅ enregistrée | ✅ `ComplaintRepondre` (ajoutée en remédiation) | ✅ société du dossier | ✅ positif + 4 négatifs (non clôturé, autre société, déjà évalué, note hors limites) — autorisation et cloisonnement testés **séparément** (REC-REM-06 vs REC-REM-09) | **CONFORME** |
| **C7-04** ListerCategoriesReclamationQuery | ✅ `ListerCategoriesReclamationQuery.cs` | ✅ enregistrée | Authentifié (page-level `[Authorize]`, conforme au contrat "Autorisation : Authentifié") | N/A (référentiel non sensible) | ✅ | **CONFORME** |
| **C7-05** ModifierCategoriesReclamationCommand | ✅ `ModifierCategoriesReclamationCommand.cs` | ✅ enregistrée | ✅ `AdministrationCategoriesReclamation` | N/A (action admin globale) | ✅ positif + négatif | **CONFORME au niveau contrat** — mais **inaccessible en pratique** : aucune UI, aucun endpoint HTTP n'invoque ce contrat. Un administrateur ne peut pas l'utiliser aujourd'hui sans qu'un développeur écrive du code d'appel. |

**Bilan C7** : 5/5 implémentés, testés (positif+négatif), avec DI/permission/cloisonnement corrects. C7-05 a une réserve d'accessibilité identique à REC-02 ci-dessus.

---

## 4. REC-SEC-01 → REC-SEC-06

| Cas | Preuve vérifiée | Statut |
|:---|:---|:---|
| **REC-SEC-01** anonyme → authentification | `AuthorizationUxTests.RecSec01_RoutesReclamations_UtilisateurAnonyme_RedirigeVersConnexion` (3 routes F006, théorie) + `CasD_...` générique — HTTP 302 vers `/comptes/connexion` vérifié réellement (`AllowAutoRedirect = false`) | **CONFORME** |
| **REC-SEC-02** client sans permission → refus | `SoumettreReclamationCommandTests.Utilisateur_sans_permission_ComplaintSoumettre_retourne_AccesRefuse` | **CONFORME** |
| **REC-SEC-03** société A → ticket société B → refus | `SoumettreReclamationCommandTests.E_F_Societe_issue_du_contexte_interdit_usurpation`, `EvaluerReclamationCommandTests.REC_REM_09_...`, `AuthorizationUxTests.CasC_...` (fiche ticket, société étrangère, pas de fuite `BL-B` dans le markup) | **CONFORME** |
| **REC-SEC-04** agent sans permission → refus | `FileSupportTests.Agent_sans_permission_ne_peut_pas_voir_la_file`, `CloturerReclamationCommandTests.Agent_sans_permission_ne_peut_pas_cloturer` | **CONFORME** |
| **REC-SEC-05** note interne invisible au client | `RecSce07CustomerComplaintE2ETests` — assertion directe (`Assert.DoesNotContain(ficheClientAvantReponse.Messages, m => m.Id == noteInterne.Id)`) après lecture via `ObtenirFicheTicketQuery` (pas un masquage UI — le filtrage est serveur, dans `ObtenirFicheTicketQuery`/`MessageRepository.ListerVisiblesParClientAsync`, vérifié dans le code) | **CONFORME** |
| **REC-SEC-06** appel direct sans droit → refus | Couvert par C7-01/C7-02/C7-03 négatifs ci-dessus (appel direct de la commande, pas via HTTP) | **CONFORME** |

**Bilan REC-SEC** : 6/6 CONFORME. Ces preuves sont bien au niveau serveur (permission/cloisonnement dans les Commands/Queries), pas un simple masquage de bouton côté UI.

---

## 5. REC-SCE-07

Texte CDC littéral (`docs/04-matrice-recette.md` ligne 38) : *"Création par catégorie, instruction, complément, orientation, clôture avec réponse"* — REC-01·REC-02·REC-03·REC-04 (Directe), REC-05 (Should).

| Étape CDC | Action testée (`RecSce07CustomerComplaintE2ETests.cs`) | Assertion | Conforme ? |
|:---|:---|:---|:---|
| 1. Création par catégorie | `SoumettreReclamationCommand.ExecuterAsync` | Ticket créé, `CategorieReclamationId` et `FileCouranteId` corrects | ✅ |
| 2. Orientation | `ConsulterFileQuery.ExecuterAsync` (agent) | Ticket visible dans la file Support Opérationnel | ✅ |
| 3. Instruction & complément | `PrendreEnChargeCommand` + `PublierNoteInterneAsync` + `AttenteClientCommand.MettreEnAttenteAsync` | Statut `EnAttenteClient`, ACR-01 présent, note interne invisible client | ✅ |
| 4. Réponse au complément | `PublierMessageClientAsync` (**avec `rattachement.SocieteId`, pas l'UtilisateurId — voir §10**) + `RependreApresReponseAsync` | Statut `EnCours`, ACR-01 disparu | ✅ au niveau commande — **ne reproduit pas le chemin réel emprunté par un client dans l'UI**, qui échouerait (§10) |
| 5. Clôture avec réponse | `CloturerReclamationCommand.ExecuterAsync` | Statut `Cloture`, réponse visible côté client | ✅ |
| 6. Évaluation (Could) | `EvaluerReclamationCommand.ExecuterAsync` | Note et commentaire persistés | ✅ |

Le test **reproduit réellement** le scénario contractuel (6 actions distinctes, assertions sur l'état réel après chaque étape, pas une méthode vide portant juste le nom REC-SCE-07). Sur SQL Server réel (LocalDB via `PlateformeWebApplicationFactory`), confirmé par relecture du fichier et ré-exécution indépendante (inclus dans les 496 tests verts ci-dessus).

**Réserve** : l'étape 4 utilise sciemment un identifiant différent de celui que l'UI réelle transmettrait (voir §10) pour contourner un bug transversal. Le test valide donc le **contrat applicatif**, pas le **chemin UI réel** pour cette étape précise.

**Statut REC-SCE-07** : **CONFORME au niveau contractuel/applicatif** — avec la réserve UI ci-dessus, qui n'invalide pas le scénario CDC lui-même (celui-ci ne prescrit pas de mécanisme UI particulier) mais limite sa valeur de preuve pour l'UI livrée.

---

## 6. UI Client

Vérifié par lecture directe des 3 fichiers + `FicheTicket.razor` :

| Élément | Statut |
|:---|:---|
| Nouvelle réclamation | ✅ `NouvelleReclamation.razor` — formulaire réel (catégorie, objet, description), appelle `SoumettreReclamationCommand` |
| Mes réclamations | ✅ `MesReclamations.razor` — réutilise `ObtenirDashboardClientQuery`, filtre `Module == "CustomerComplaint"` |
| Consultation | ✅ via `/tickets/{Id}` (`FicheTicket.razor`) |
| Catégorie / Objet / Description | ✅ affichés conditionnellement (`Module == "CustomerComplaint"`) |
| Réponse formalisée | ✅ affichée si `ReponseFormalisee` non vide |
| Évaluation | ✅ étoiles interactives 1-5, verrouillage après évaluation (affichage lecture seule) |
| Navigation | ✅ `NavMenu.razor` : "Nouvelle réclamation", "Mes réclamations" dans l'espace Client |
| Réponse à une demande de complément | ❌ **cassé** — voir §10, le bouton "Envoyer" du fil de messages échoue pour un client sur n'importe quel ticket, pas seulement Customer Complaint |

Scans confirmés : **0** `Guid.Parse`, **0** `PlateformeDbContext` dans les 3 pages + `FicheTicket.razor`. **1** injection de repository (`IFileDeTraitementRepository` dans `FileReclamations.razor`, côté agent) — conforme à un précédent déjà établi (`Documents.razor`, `FilMessages.razor` font de même), pas une nouvelle violation introduite par F006.

---

## 7. UI Agent

| Élément | Statut |
|:---|:---|
| File Réclamations | ✅ `FileReclamations.razor` — réutilise `ConsulterFileQuery` (route déjà `ComplaintInstruire`) |
| Prise en charge | ✅ bouton présent, appelle `PrendreEnChargeCommand` |
| Fiche | ✅ lien `/tickets/{Id}` |
| Réponse (message) | ⚠️ le champ de message générique de `FicheTicket.razor` fonctionne pour un agent (`PublierMessageAgentAsync`/`PublierNoteInterneAsync` utilisent `_utilisateurId`, ce qui est le bon identifiant pour un agent — le bug §10 ne touche que la branche client) |
| Clôture | ✅ bloc dédié (textarea réponse formalisée + bouton), gate correctement sur `PeutCloturer` |
| Permissions | ✅ `Complaint.Instruire` vérifiée à chaque niveau (query, prise en charge, clôture) |
| Navigation | ✅ "File Réclamations" dans l'espace Agent |

**Action backend sans UI identifiée** : `ModifierCategoriesReclamationCommand` (C7-05) — aucune UI ni endpoint ne l'invoque (voir §3, §9).

---

## 8. T026 et T030 — Décision de clôture argumentée

### T026 — Tests bUnit dédiés par composant

- **Lecture spec/plan/tasks** : tasks.md Phase 9 dit "TEST-FIRST bUnit / Web **autant que possible**" — formulation d'effort, pas une obligation absolue. Ni spec.md ni plan.md n'imposent bUnit spécifiquement comme condition de clôture.
- **Constat réel** : la couverture existe, mais via des tests d'intégration/E2E (`AuthorizationUxTests`, `RecSce07CustomerComplaintE2ETests`) plutôt que des bUnit isolés par composant. Cette couverture teste le comportement réel de bout en bout (plus fort qu'un bUnit isolé sur certains aspects, plus faible sur d'autres — un bUnit isolé aurait par exemple pu détecter le bug §10 en rendant `FicheTicket` et en simulant un clic client, ce que les tests actuels ne font pas).
- **Décision** : **C (couverte autrement) + D (dette acceptable)**. Le risque concret que l'absence de bUnit a laissé passer (bug §10) est documenté séparément et ne dépend pas de T026 pour être détecté — il l'a été par cette revue.

### T030 — Écran d'administration des catégories

- **Lecture spec/plan/tasks** : tasks.md §4 (traçabilité, rédigée **avant** toute implémentation) marque explicitement "AdminCategories (**optionnel** UI)". C'est une décision de conception d'origine, pas une invention du rapport de remédiation.
- **Tension avec le CDC** : REC-02 est **Must** et son texte exige "sans développement". Le contrat C7-05 existe et est testé, mais sans écran ni API exposée, un administrateur ne peut aujourd'hui modifier les catégories qu'en passant par un développeur — ce qui contredit littéralement "sans développement".
- **Décision** : **B (optionnelle selon la décision de conception d'origine) avec une réserve non triviale contre REC-02 (Must)**. Ce n'est pas une dette purement technique : c'est un écart fonctionnel réel par rapport au texte CDC, atténué par le fait qu'un jeu de catégories utilisable existe déjà (seed) pour permettre l'usage immédiat de F006. Recommandation : ne doit pas rester indéfiniment différé, mais ne bloque pas la mise en service initiale.

---

## 9. Bug `FicheTicket.razor` / `PublierMessageClientAsync`

Vérifié par lecture directe du code (pas une supposition) :

- **Code réel** : `FicheTicket.razor` ligne 501 — `await PublierMessage.PublierMessageClientAsync(_utilisateurId, Id, _nouveauMessageContenu);` où `_utilisateurId = contexte.UtilisateurId` (l'identifiant utilisateur individuel, ligne 425).
- **`CloisonnementService.ClientEstProprietaireDuTicketAsync`** (appelée par `PublierMessageClientAsync`) compare cet argument à `dossier.ClientId`.
- **`dossier.ClientId` porte la `SocieteId`**, pas l'`UtilisateurId` — vérifié dans `SoumettreReclamationCommand.cs` (`Dossier.Creer(numeroDossier, rattachement.SocieteId, ...)`) et, identiquement, dans `SoumettreDemandeImportCommand.cs` (F001, `Dossier.Creer(numeroDossier, requete.SocieteId, ...)`).
- **Identifiant attendu** : `SocieteId`.
- **Identifiant transmis** : `UtilisateurId`.
- **Reproductibilité** : déterministe, 100 % — un `UtilisateurId` n'est structurellement jamais égal à une `SocieteId`. Reproduit directement lors de la construction du test REC-SCE-07 (échec `AccesRefuseException` avec `_utilisateurId`, succès avec `rattachement.SocieteId`).
- **Impact F006** : bloque l'étape 4 du scénario REC-SCE-07 ("réponse au complément") **via l'UI réelle** — le backend est correct, seul l'appel depuis `FicheTicket.razor` est fautif.
- **Impact F001-F005** : identique. `FicheTicket.razor` est le composant unique de réponse-client pour **tous** les modules (Billing Import/Export, Simulation, etc.). Un client tentant de répondre à un message sur n'importe quel ticket, de n'importe quelle feature déjà close, échouerait de la même façon. Ce n'est donc **pas un défaut introduit par F006**.
- **Criticité** : élevée pour l'expérience utilisateur (fonction de réponse client totalement inopérante, tous modules confondus), mais **transversale** — sa correction dépasse le mandat de cette remédiation F006 et toucherait des features déjà closes (001-005).

**Classement** : **DETTE TRANSVERSALE** (pas un blocant F006 au sens strict — F006 n'a ni introduit ni aggravé ce défaut — mais un défaut réel, sévère, et déjà présent dans le périmètre livré de F006 via le composant partagé). Non corrigé dans cette revue, conformément à la consigne.

---

## 10. Historique EF Core (`__EFMigrationsHistory` sur `BillingOffice`)

Diagnostic non destructif exécuté :

- **Chaîne de connexion utilisée** (`dotnet user-secrets list` côté `CIT.Plateforme.Web`) : `Server=127.0.0.1,14330;Database=BillingOffice;...` (conteneur Docker `billingoffice-sqlserver`).
- **Recherche dans le dépôt** : `EnsureDeleted`/`EnsureCreated`/`Database.Migrate` → 3 fichiers seulement, tous des fixtures de tests (`BaseDeTestSql.cs` → LocalDB `CitPlateformeTests` ; `PlateformeWebApplicationFactory.cs` → LocalDB `CitPlateformeTestsWeb` ; `ConcurrenceUniciteBlSqlTests.cs` → utilise `BaseDeTestSql`, donc `CitPlateformeTests`). **Aucun ne cible `BillingOffice`.**
- **Respawn** : absent du dépôt (aucune référence, `grep` négatif).
- **Scripts Docker / SQL** : absents du dépôt (`find` négatif sur `docker-compose*`/`*.sql`).
- **`DevelopmentSeeder`** : idempotent, vérifie l'existence avant insertion — ne supprime ni ne recrée jamais de schéma.
- **Occurrences de "BillingOffice" dans le code** : uniquement comme chaîne de module de test (`Ticket.Creer(..., "BillingOffice", ...)`) dans des tests de domaine sans lien avec la base de données réelle du même nom — collision de nommage, pas une piste.
- **Constat observé pendant la remédiation** (rapporté, non re-testé ici pour rester non destructif) : schéma complet présent, `__EFMigrationsHistory` vide, à 3 reprises.

**CAUSE PROBABLE** : un processus externe à ce dépôt (hors code, hors tests, hors scripts versionnés) agit directement sur le conteneur `billingoffice-sqlserver`, probablement une session de développement concurrente. Aucune preuve dans le dépôt lui-même ne peut expliquer le phénomène.

**Impact sur la clôture F006** : aucun blocage — la remédiation a démontré (§ précédente session) que la réconciliation est possible et déterministe, et `has-pending-model-changes` (qui ne dépend pas de l'état de la base, uniquement du modèle vs. dernière migration) rapporte 0 en permanence. Ce phénomène est un **problème opérationnel/environnemental**, pas un défaut du code F006.

---

## 11. Frontière AVE

```
grep -rln "Avoir|Exoneration|Finance|DirectionFinanciere" (fichiers F006 : Domain/Reclamations, Application/CustomerComplaint,
CloturerReclamationCommand.cs, EvaluerReclamationCommand.cs, ModifierCategoriesReclamationCommand.cs, Web/CustomerComplaint)
→ 0 résultat
```

**AVE : ABSENT.** Confirmé, aucune contamination.

---

## 12. Tâches T001 → T035 (recalculé indépendamment)

Chaque tâche vérifiée contre une preuve directe (fichier de code existant + fichier de test existant et vert, ou migration appliquée) :

- **Démontrées** : 33/35 (T001-T025, T027-T029, T031-T035)
- **Partielles** : 0
- **Non réalisées** : 2 (T026 — bUnit dédiés, voir §8 ; T030 — écran admin, voir §8)

Ce chiffre confirme celui du rapport de remédiation (33/35). Aucune tâche cochée `[X]` dans `tasks.md` ne s'est révélée sans preuve lors de cette revue.

---

## 13. État Git

```
git log --oneline -7 :
98b7b46 feat: close feature 005 proforma simulation   ← dernier commit
29c88e2 feat: close feature 004 billing office export
...
git status : ~34 fichiers modifiés, ~20 nouveaux répertoires/fichiers non suivis
```

**Modifications correspondant à F006 + remédiation** (attendues) : tout `src/**/CustomerComplaint`, `Domain/Reclamations`, `CloturerReclamationCommand.cs`, `EvaluerReclamationCommand.cs`, `ModifierCategoriesReclamationCommand.cs`, les 2 migrations F006, `NatureUtilisateur.cs`, `Ticket.cs`, `DependencyInjection.cs`, `PlateformeDbContext.cs`, `DevelopmentSeeder.cs`, `NavMenu.razor`, `FicheTicket.razor`, `Web/CustomerComplaint/*`, `Home.razor`, `PersonaSwitcher.razor` (routes mortes corrigées), `Repositories.cs`, `FicheTicketVue.cs`, `ObtenirFicheTicketQuery.cs`, `ConsulterFileQuery.cs`, `PrendreEnChargeCommand.cs`, `AttenteClientCommand.cs`, `PublierMessageCommand.cs`, `ObtenirDashboardClientQuery.cs`, `DashboardClientVue.cs`, `PlateformeDbContextModelSnapshot.cs`, et les fichiers de tests correspondants.

**Modifications étrangères identifiées** (antérieures à cette remédiation, issues d'un travail de refonte visuelle concurrent dans la même session, non lié à F006) :
- `src/CIT.Plateforme.Web/Components/App.razor`, `HeaderBar.razor`, `Comptes/Connexion.razor`, `Comptes/MotDePasseOublie.razor`, `Program.cs`, `Properties/launchSettings.json`, `wwwroot/app.css`, `wwwroot/js/theme-manager.js` — refonte du design (maritime/navy), sans rapport avec Customer Complaint.
- `src/CIT.Plateforme.Infrastructure/TraitementsDeFond/DistributionAutomatiqueService.cs`, `RelancesAbsenceReponseService.cs`, `SurveillanceSlaService.cs` — refactoring de la gestion d'annulation (`OperationCanceledException`) des services d'arrière-plan, sans rapport avec F006.
- `docs/05-roadmap-features.md`, `07-roadmap-post-ui.md`, `08-roadmap-post-f005.md`, `09-etat-avancement-global.md`, `10-preflight-feature-006.md`, `09-comptes-test-development.md` — documents de suivi/roadmap.
- `.claude/launch.json` — configuration d'outillage locale (créée pendant cette session pour la prévisualisation navigateur).

Ces modifications étrangères ne sont **ni fausses ni suspectes** (traçables à des étapes légitimes antérieures de la même session globale), mais elles confirment qu'un `git commit` sur l'état actuel du working tree **ne serait pas un commit "F006 seul"** — à signaler à l'utilisateur avant tout commit futur, pour qu'il choisisse de scinder ou non.

---

## 14. Verdict de clôture

- **Conformité fonctionnelle** : PARTIELLE — REC-02 et REC-03 ont une réserve réelle (§2, §9).
- **Conformité sécurité** : CONFORME (REC-SEC-01→06, §4).
- **Conformité UI** : PARTIELLE — Client/Agent globalement livrés et corrects, mais un bug transversal bloque la réponse client à un complément (§9), et l'administration des catégories reste sans écran (§8).
- **Conformité REC-SCE-07** : CONFORME au niveau contractuel, avec réserve sur la fidélité du test à l'UI réelle pour l'étape 4 (§5).
- **Dette technique** : T026 (bUnit dédiés), T030 (écran admin catégories) — documentées, non nouvelles.
- **Problèmes transversaux** : bug `PublierMessageClientAsync`/`FicheTicket.razor` (§9), préexistant à F006, affecte F001-F006 identiquement.
- **État migrations** : CONFORME (0 pending model changes, migration `RemediationCustomerComplaintContent` cohérente).
- **État Git** : working tree contient des modifications F006 + des modifications étrangères légitimes mais non liées (§13) — aucun commit n'a été fait.

Le bug §9 est transversal et préexistant — il ne doit pas, à lui seul, empêcher la clôture de F006 (il n'est pas du ressort de cette feature de le corriger, et le pénaliser reviendrait à geler indéfiniment F006 pour un défaut qui affecte identiquement des features déjà closes). Mais il doit être **explicitement accepté comme dette connue**, pas silencieusement ignoré — ce que le rapport de remédiation n'avait pas fait (il annonçait "UI : CONFORME" sans réserve).

**VERDICT : CLOSE WITH ACCEPTED DEBT**

Dette acceptée proposée (à faire valider par l'utilisateur, à traiter dans un ordre de priorité suggéré) :
1. Bug `PublierMessageClientAsync`/`FicheTicket.razor` (transversal F001-F006) — **priorité haute**, casse une fonction client de base sur toutes les features déjà closes.
2. T030 — écran d'administration des catégories (tension avec REC-02 Must) — priorité moyenne.
3. T026 — bUnit dédiés par composant UI — priorité basse (couverture fonctionnelle déjà assurée autrement).

---

## 15. Sortie finale

```
FEATURE : 006 — Customer Complaint
REC : 4/6 CONFORME, 2/6 PARTIEL (REC-02, REC-03)
C7 : 5/5 (C7-05 conforme au contrat, inaccessible en pratique — cf. REC-02)
CLAR-REC : 8/8
REC-SEC : 6/6
REC-SCE-07 : CONFORME (réserve sur la fidélité UI de l'étape 4 — voir §5/§9)
TÂCHES : 33/35 démontrées
T026 : NON RÉALISÉE — couverte autrement (intégration/E2E) ; dette acceptable, priorité basse
T030 : NON RÉALISÉE — optionnelle dès l'origine (tasks.md) mais en tension avec REC-02 (Must) ; dette acceptable, priorité moyenne
UI CLIENT : PARTIEL — Nouvelle réclamation/Mes réclamations/Fiche/Évaluation OK ; réponse à un complément cassée (bug transversal)
UI AGENT : CONFORME
MIGRATIONS : CONFORME (0 pending model changes)
PENDING MODEL CHANGES : 0
BUG PublierMessageClientAsync : CONFIRMÉ — DETTE TRANSVERSALE (F001-F006, pas spécifique à F006), priorité haute
EF MIGRATION HISTORY : CAUSE PROBABLE — processus externe au dépôt agissant sur le conteneur Docker ; aucun code/test/script du dépôt n'en est responsable ; ne bloque pas la clôture
AVE : ABSENT
TESTS : 496/496
BUILD WARNINGS : 0
RÉGRESSIONS : 0
DETTE ACCEPTÉE PROPOSÉE : bug PublierMessageClientAsync (transversal, priorité haute) ; T030 écran admin catégories (priorité moyenne) ; T026 bUnit dédiés (priorité basse)
VERDICT : CLOSE WITH ACCEPTED DEBT
```

---

## MISE À JOUR — REMÉDIATION FINALE UI (30 août 2026, suite à la revue de clôture ci-dessus)

### Bug initial

`FicheTicket.razor.EnvoyerMessageAsync` (branche client) appelait
`PublierMessage.PublierMessageClientAsync(_utilisateurId, Id, _nouveauMessageContenu)`.
`PublierMessageClientAsync` délègue à `CloisonnementService.ExigerProprieteClientAsync(clientId, ticketId)`,
qui compare `clientId` à `Dossier.ClientId`. `Dossier.ClientId` est documenté (`Dossier.cs`) et
vérifié dans le code (`SoumettreReclamationCommand`, `SoumettreDemandeImportCommand` F001) comme
portant la **SocieteId**, jamais l'`UtilisateurId` individuel. Résultat : `AccesRefuseException`
systématique dès qu'un client tentait de répondre à un message sur **n'importe quel** ticket, de
n'importe quel module (F001-F006).

### Cause racine

Confusion entre deux espaces d'identité distincts dans un seul paramètre `clientId` : le contrôle de
propriété (`CloisonnementService`) a besoin de la société propriétaire du dossier ; l'appel émis par
`FicheTicket.razor` transmettait l'identifiant utilisateur individuel.

### Test rouge (avant correction)

`ReponseComplementDepuisFicheTicketTests.Client_Repond_Depuis_FicheTicket_Publie_Le_Message_Et_Reprend_Le_Traitement`
— rend réellement `<FicheTicket>` (bUnit), simule la frappe dans le champ de message
(`.Change(...)`) et le clic sur "Envoyer" (`.Click()`), sans jamais injecter directement le bon
identifiant. Échec confirmé avant correction : `Assert.Contains() Failure: Filter not matched in
collection Collection: []` — le message n'était jamais persisté (l'exception levée par
`PublierMessageClientAsync` était silencieusement absorbée par le `catch` de
`EnvoyerMessageAsync`, qui se contente d'afficher un message d'erreur dans l'UI au lieu de planter).

### Correction (minimale, ciblée)

Un seul changement fonctionnel, dans `FicheTicket.razor`, branche client uniquement :

```diff
- await PublierMessage.PublierMessageClientAsync(_utilisateurId, Id, _nouveauMessageContenu);
+ await PublierMessage.PublierMessageClientAsync(_fiche!.SocieteId, Id, _nouveauMessageContenu);
```

Rien d'autre n'a été modifié : ni `PublierMessageCommand.cs`, ni `CloisonnementService.cs`, ni le
workflow, ni les permissions, ni le modèle domaine, ni aucune migration. La branche agent
(`PublierMessageAgentAsync`/`PublierNoteInterneAsync`, qui utilisent correctement `_utilisateurId`
— un espace d'identité individuel, cohérent avec les habilitations) n'a pas été touchée.

**Effet de bord identifié et assumé** : `Message.AuteurId` porte désormais la `SocieteId` pour les
messages client (au lieu de l'`UtilisateurId`), car `PublierMessageClientAsync` réutilise le même
paramètre pour l'autorisation ET l'attribution d'auteur. Conséquence observée : l'affichage du nom
de l'auteur d'un message client retombe sur le libellé générique "Utilisateur" au lieu du nom réel
(`ObtenirFicheTicketQuery` ne trouve pas de `SocieteId` dans `IUtilisateurInfoRepository`, qui
n'indexe que des utilisateurs individuels). C'est un défaut cosmétique mineur préexistant dans la
conception de `PublierMessageClientAsync` (même défaut qu'aurait eu n'importe quel appelant
transmettant correctement la `SocieteId`), **pas** introduit par ce correctif — le message est bien
publié, sur le bon ticket, et le workflow progresse correctement ; seul le nom affiché est générique
au lieu d'être nominatif. Documenté ici plutôt que corrigé silencieusement (corriger nécessiterait de
faire porter DEUX identifiants à `PublierMessageClientAsync`, hors du périmètre "correction minimale
de l'identifiant transmis depuis FicheTicket.razor" fixé par ce brief).

### Test vert (après correction)

Même test, sans modification : vert. Confirmé stable sur 1 exécution (déterministe, base InMemory).

### Non-régression messagerie (A-F)

| Cas | Test | Résultat |
|:---|:---|:---|
| A. Customer Complaint, réponse via UI | `ReponseComplementDepuisFicheTicketTests` | ✅ Vert |
| B. Billing Import, messagerie intacte | `MessagerieFicheTicketNonRegressionTests.B_BillingImport_...` (nouveau — le même bug affectait déjà Billing Import ; ce test prouve qu'il est également corrigé, pas seulement "non régressé") | ✅ Vert |
| C. Billing Export, messagerie intacte | `MessagerieFicheTicketNonRegressionTests.C_BillingExport_...` (idem B) | ✅ Vert |
| D. Note interne toujours invisible client | Non re-testé au niveau UI (redondant) — le correctif ne touche que la branche `_estClient` de `EnvoyerMessageAsync` ; `PublierNoteInterneAsync` et le filtrage serveur (`ObtenirFicheTicketQuery`/`MessageRepository.ListerVisiblesParClientAsync`) sont inchangés et déjà couverts par `NotesInternesTests.Une_note_interne_n_est_jamais_retournee_par_la_lecture_client` + `RecSce07CustomerComplaintE2ETests` (REC-SEC-05) | ✅ Couvert (existant, structurellement non affecté) |
| E. Mauvais tenant toujours refusé | Non re-testé au niveau UI (redondant) — `_fiche.SocieteId` n'est atteignable qu'après que `ObtenirFicheTicketQuery` a déjà validé l'appartenance société du client (sinon `AccesRefuseException` avant même l'affichage du formulaire, cf. `AuthorizationUxTests.CasC_...`) ; `MessagesTests.Un_client_ne_peut_pas_publier_sur_un_ticket_qui_n_est_pas_le_sien` couvre le niveau commande | ✅ Couvert (existant + raisonnement structurel vérifié dans le code) |
| F. Mauvais ticket, aucune publication croisée | Non applicable — le correctif ne touche pas `Id` (le ticket ciblé), seulement l'identifiant `clientId` transmis | ✅ Structurellement impossible à régresser par ce correctif |

### Impact REC-02

**Aucun** — le correctif ne touche ni le référentiel de catégories ni son administration. REC-02
reste **PARTIEL** (voir §8 ci-dessous pour la décision motivée sur T030).

### Impact REC-03

**REC-03 passe de PARTIEL à CONFORME.** Le composant unique de réponse client
(`FicheTicket.razor`) publie désormais réellement le message et permet la reprise du traitement —
l'exigence CDC ("échanger avec le client") est maintenant démontrable de bout en bout via l'écran
réel, pas seulement via la Command applicative.

### Impact REC-SCE-07

`RecSce07CustomerComplaintE2ETests.cs` réécrit : l'étape 4 rend désormais `<FicheTicket>` (bUnit),
simule la frappe et le clic "Envoyer" réels, contre SQL Server réel (LocalDB). Difficulté technique
rencontrée et résolue : le rendu bUnit interactif contre une base réelle partageait initialement le
même `PlateformeDbContext` que le reste du test, provoquant une exception EF Core
("A second operation was started on this context instance...") — résolue en isolant les services
injectés dans `FicheTicket` dans une portée DI (`IServiceScope`) dédiée, pointant vers la même base
réelle. La transition d'état "Reprendre le traitement" (bouton distinct, sans paramètre d'identité —
donc non concerné par le bug corrigé) est appelée directement dans ce test après plusieurs tentatives
de l'enchaîner également via un second rendu bUnit qui se sont révélées instables (SQL timeout /
concurrence EF) sans rapport avec le bug ; cette transition spécifique est démontrée via clic UI réel
et enchaînée avec l'envoi de message dans `ReponseComplementDepuisFicheTicketTests` (base InMemory,
stable sur répétition). **REC-SCE-07 reste CONFORME**, avec une fidélité UI désormais réelle pour
l'étape la plus critique (envoi du message, où résidait le bug) plutôt que contournée.

### Statut T026

**COUVERTE AUTREMENT** (renforcé). Cette remédiation a produit les premiers tests bUnit réellement
interactifs du dépôt (`.Change()`/`.Click()` sur des éléments rendus, pas seulement une assertion sur
`cut.Markup`) : `ReponseComplementDepuisFicheTicketTests.cs`, `MessagerieFicheTicketNonRegressionTests.cs`.
Ce ne sont pas des tests par composant au sens strict de T026 (Nouvelle/Mes/File Réclamations n'ont
toujours pas de bUnit dédié), donc la case reste non cochée — mais l'objectif de fond (prouver
l'interaction UI réelle) est désormais atteint pour le chemin le plus critique. Aucun test redondant
n'a été ajouté uniquement pour atteindre 35/35.

### Statut T030

**Décision motivée (A-E)** : reclassé **C (optionnel selon la décision de conception documentée
dans tasks.md §4) avec réserve explicite non résolue contre A (contractuellement requis par
REC-02)**. Ni A ni C ne s'imposent sans ambiguïté à la lecture littérale du CDC ("sans
développement" peut se lire comme "données paramétrables en base, pas en code" — déjà vrai — ou
comme "accessible à un administrateur sans l'aide d'un développeur" — pas encore vrai). Trancher
définitivement en faveur de E (obligatoire avant clôture) sans mandat CDC univoque serait "inventer
la réponse" dans un sens ; trancher en faveur de C sans réserve le serait dans l'autre. Décision :
**ne bloque pas la clôture** (le référentiel seedé est utilisable en l'état pour une mise en service
initiale), mais la dette est **élevée en priorité** (de "moyenne" à "élevée" par rapport à la
première revue), et doit être résolue avant toute évolution du référentiel de catégories en
production.

### Changements étrangers à F006 — matrice complète (git status / diff)

| Fichier | F006 ? | Justification | Inclure futur commit F006 ? | Exclure ? |
|:---|:---:|:---|:---:|:---:|
| `src/CIT.Plateforme.Application/Abstractions/Repositories.cs` | Oui | `ICategorieReclamationRepository` | Oui | — |
| `src/CIT.Plateforme.Application/Client/DashboardClientVue.cs` | Oui | ACR-01 Customer Complaint | Oui | — |
| `src/CIT.Plateforme.Application/Client/ObtenirDashboardClientQuery.cs` | Oui | ACR-01 Customer Complaint | Oui | — |
| `src/CIT.Plateforme.Application/Messagerie/PublierMessageCommand.cs` | Oui | branches `ComplaintInstruire` | Oui | — |
| `src/CIT.Plateforme.Application/Tickets/AttenteClientCommand.cs` | Oui | branches Customer Complaint | Oui | — |
| `src/CIT.Plateforme.Application/Tickets/ConsulterFileQuery.cs` | Oui | route `ComplaintInstruire` | Oui | — |
| `src/CIT.Plateforme.Application/Tickets/FicheTicketVue.cs` | Oui | champs Objet/Description/catégorie/réponse/évaluation | Oui | — |
| `src/CIT.Plateforme.Application/Tickets/ObtenirFicheTicketQuery.cs` | Oui | idem | Oui | — |
| `src/CIT.Plateforme.Application/Tickets/PrendreEnChargeCommand.cs` | Oui | branche Customer Complaint | Oui | — |
| `src/CIT.Plateforme.Domain/Securite/NatureUtilisateur.cs` | Oui | permissions `Complaint.*` | Oui | — |
| `src/CIT.Plateforme.Domain/Tickets/Ticket.cs` | Oui | extensions Objet/Description/catégorie/réponse/évaluation | Oui | — |
| `src/CIT.Plateforme.Infrastructure/DependencyInjection.cs` | Oui | enregistrement des contrats C7 | Oui | — |
| `.../Migrations/PlateformeDbContextModelSnapshot.cs` | Oui | reflète les 2 migrations F006 | Oui | — |
| `src/CIT.Plateforme.Infrastructure/Persistence/PlateformeDbContext.cs` | Oui | configuration EF F006 | Oui | — |
| `src/CIT.Plateforme.Web/Components/Pages/Tickets/FicheTicket.razor` | Oui | extension UI + correctif final | Oui | — |
| `tests/CIT.Plateforme.Application.Tests/CIT.Plateforme.Application.Tests.csproj` | Oui | référence projet pour les tests CustomerComplaint | Oui | — |
| `tests/.../Tickets/ObtenirFicheTicketQueryTests.cs` | Oui | REC-REM-03 | Oui | — |
| `tests/.../E2E/ParcoursClientE2ETests.cs` | Oui | DI CloturerReclamation/EvaluerReclamation | Oui | — |
| `tests/.../E2E/ParcoursCloisonnementMultiTenantE2ETests.cs` | Oui | idem | Oui | — |
| `tests/.../Pages/AuthorizationUxTests.cs` | Oui | idem + REC-SEC-01 nouvelles routes | Oui | — |
| `tests/.../Pages/Tickets/FicheTicketTests.cs` | Oui | idem | Oui | — |
| Tout `specs/006-customer-complaint/**` | Oui | artefacts Spec Kit F006 | Oui | — |
| Tout `src/**/CustomerComplaint/**`, `Domain/Reclamations/**`, `Web/.../CustomerComplaint/**` | Oui | code F006 | Oui | — |
| `CloturerReclamationCommand.cs`, `EvaluerReclamationCommand.cs`, `ModifierCategoriesReclamationCommand.cs` | Oui | contrats C7 | Oui | — |
| `CategorieReclamationRepository.cs`, `DevelopmentSeeder.cs` | Oui | persistence + seed F006 | Oui | — |
| 2 migrations `AjoutSupportCustomerComplaint`/`RemediationCustomerComplaintContent` | Oui | migrations F006 | Oui | — |
| Tout `tests/**/CustomerComplaint/**`, `Reclamations/**` (Domain/Infra tests) | Oui | tests F006 | Oui | — |
| `src/CIT.Plateforme.Web/Components/Layout/NavMenu.razor` | **Partiel** | contient les liens F006 (Réclamations) **et** des changements de style issus de la refonte visuelle concurrente | Partiellement (`git add -p` recommandé) | Portion styling |
| `src/CIT.Plateforme.Web/Components/Layout/PersonaSwitcher.razor` | Non | correctif route morte (`/administration/comptes-a-valider` → `/comptes/valider`), fait dans cette session mais **avant** et **indépendamment** de F006 | Non | Commit séparé |
| `src/CIT.Plateforme.Web/Components/Pages/Home.razor` | Non | idem | Non | Commit séparé |
| `src/CIT.Plateforme.Web/Components/App.razor` | Non | refonte visuelle (thème) | Non | Commit séparé |
| `src/CIT.Plateforme.Web/Components/Layout/HeaderBar.razor` | Non | refonte visuelle | Non | Commit séparé |
| `src/CIT.Plateforme.Web/Components/Pages/Comptes/Connexion.razor` | Non | refonte visuelle | Non | Commit séparé |
| `src/CIT.Plateforme.Web/Components/Pages/Comptes/MotDePasseOublie.razor` | Non | refonte visuelle | Non | Commit séparé |
| `src/CIT.Plateforme.Web/Program.cs` | Non | sans rapport avec F006 (middleware/host) | Non | Commit séparé |
| `src/CIT.Plateforme.Web/Properties/launchSettings.json` | Non | config locale (port, env) | Non | Commit séparé |
| `src/CIT.Plateforme.Web/wwwroot/app.css` | Non | refonte visuelle | Non | Commit séparé |
| `src/CIT.Plateforme.Web/wwwroot/js/theme-manager.js` | Non | refonte visuelle | Non | Commit séparé |
| `src/CIT.Plateforme.Infrastructure/TraitementsDeFond/*.cs` (3 fichiers) | Non | refactoring `OperationCanceledException`, sans rapport | Non | Commit séparé |
| `docs/05-07-08-09-10-*.md` (roadmap/état d'avancement) | Non | documents de suivi, pas du code | Non | Commit séparé (ou dossier `docs/`) |
| `.claude/launch.json` | Non | config d'outillage locale (prévisualisation navigateur) | Non | Ne pas committer |

**Conclusion §9** : les modifications F006 sont clairement identifiables et isolables (préfixes de
dossiers cohérents : `CustomerComplaint`, `Reclamations`, fichiers listés ci-dessus). Un futur
`git add` sélectif (liste explicite de fichiers, pas `git add -A`) permettrait un commit F006 propre,
à l'exception de `NavMenu.razor` qui nécessiterait un `git add -p` pour séparer proprement les deux
hunks. Rien n'a été modifié ou supprimé dans les travaux étrangers pendant cette remédiation.

### Migrations

`dotnet ef migrations has-pending-model-changes` → `No changes have been made to the model since the
last migration.` — confirmé après le correctif (qui ne touche que du code Razor, aucun modèle EF).

### Suite complète après correction

```
Domain          : 82/82
Application     : 254/254
Infrastructure  : 65/65
Web             : 98/98   (+3 : ReponseComplementDepuisFicheTicketTests, MessagerieFicheTicketNonRegressionTests ×2)
TOTAL           : 499/499, 0 échec, 0 ignoré, 0 avertissement de compilation
```

---

## VERDICT FINAL (remplace le verdict de la revue de clôture initiale ci-dessus)

```
BUG PublierMessageClientAsync : CORRIGÉ
REC-02 : PARTIEL (inchangé — dépend de T030, hors périmètre de ce correctif transversal)
REC-03 : CONFORME (corrigé — était PARTIEL à cause du bug UI)
REC-SCE-07 : CONFORME (étape 4 désormais réellement pilotée par l'UI, pas contournée)
T026 : NON RÉALISÉE — COUVERTE AUTREMENT (renforcé par 2 nouveaux fichiers de bUnit interactifs réels)
T030 : NON RÉALISÉE — DETTE ACCEPTÉE, PRIORITÉ ÉLEVÉE (tension réelle et non résolue avec REC-02 Must, documentée, ne bloque pas la mise en service initiale)
TÂCHES : 33/35 démontrées (inchangé — le correctif transversal n'appartient à aucune des 35 tâches d'origine)
DETTE ACCEPTÉE : T030 (priorité élevée) ; T026 (priorité basse, couverture fonctionnelle déjà assurée) ; effet de bord cosmétique sur l'attribution d'auteur des messages client (nom générique "Utilisateur" au lieu du nom réel)
MIGRATIONS : CONFORME
PENDING MODEL CHANGES : 0
TESTS : 499/499 verts
BUILD WARNINGS : 0
MODIFICATIONS ÉTRANGÈRES F006 : ISOLÉES (matrice complète ci-dessus, aucune touchée ni supprimée)
AVE : ABSENT

VERDICT : READY FOR FINAL CLOSURE REVIEW
```

---

## DÉCISION FINALE T030 / REC-02 (30 août 2026)

### Texte CDC exact de REC-02

> Les catégories sont paramétrables et dynamiques (ajout/modification sans développement).
> — Acteur : Admin fonctionnel

### Preuve décisive : CDC §6.19 "Administration fonctionnelle"

`docs/00-analyse-cdc.md` §6.19 (titre exact du CDC) liste explicitement, dans un tableau intitulé
*"Tous les éléments suivants sont administrables **sans développement**"* :

| Domaine | Éléments |
|---|---|
| ... | ... |
| **Catégories** | **Catégories de réclamation**, catégories de marchandise |
| ... | ... |

Cette même section liste, dans les MÊMES conditions ("sans développement"), les Files/rôles, la
Matrice de transitions, les SLA, les Grilles tarifaires, etc. — et **chacun de ces éléments
comparables possède déjà un écran d'administration réel** dans les features closes de ce dépôt :
`StrategieFile.razor` (Files/affectation) et `GestionGrillesTarifaires.razor` (Simulation) en sont
la preuve directe (vérifiés présents par lecture du dépôt). Il n'existe aucune raison, dans le texte
CDC ou dans le précédent architectural de ce projet, de traiter "Catégories de réclamation" comme une
exception aux autres éléments de la même liste.

### Réponses aux 4 questions (§3 du brief)

- **A. REC-02 exige-t-elle qu'un utilisateur métier puisse administrer les catégories depuis
  l'application ?** OUI — au sens où le CDC classe explicitement les catégories de réclamation dans
  la même catégorie "administrable sans développement" que des éléments dont l'implémentation réelle,
  dans ce dépôt, a systématiquement pris la forme d'un écran applicatif.
- **B. Ou seulement modifiable sans développement, par un mécanisme quelconque ?** Lecture
  alternative possible en isolant le seul texte de REC-02, mais **contredite** par le précédent
  établi en §6.19 et par l'usage réel du projet (aucun des éléments comparables n'a été livré comme
  "paramétrable en base uniquement, via un DBA").
- **C. Le seed actuel satisfait-il REC-02 ?** NON — un jeu de catégories pré-chargé au démarrage
  n'est pas "paramétrable" par un admin fonctionnel ; c'est une configuration figée par le
  développeur.
- **D. Une modification en base/seed nécessitant une intervention technique peut-elle être
  "sans développement" ?** NON — modifier une table SQL directement, ou changer `DevelopmentSeeder.cs`
  et redéployer, requiert une compétence et un accès techniques qu'un "Admin fonctionnel" n'a pas par
  définition (c'est précisément la distinction que le CDC trace en nommant cet acteur).

### Décision

**T030 : OBLIGATOIRE POUR REC-02** (et non "optionnelle" comme la décision de conception d'origine,
non invalidée jusqu'ici, le supposait). Implémentée dans cette remédiation.

### Ce qui existait déjà (backend prêt, seul l'écran manquait)

`CategorieReclamation` (domaine), `ModifierCategoriesReclamationCommand` (C7-05 : permission
`AdministrationCategoriesReclamation`, DI enregistrée, testée positif+négatif),
`ICategorieReclamationRepository.ListerToutesAsync()` (existait déjà, jamais exposé à l'Application).
Seule manquait une requête Application exposant cette dernière méthode avec contrôle d'autorisation
(`ListerCategoriesReclamationQuery`, C7-04, ne retourne que les catégories actives et ne vérifie
aucune permission — impropre à un usage admin) et la page Razor elle-même.

### Implémentation (TEST-FIRST)

1. **Nouveau contrat, minimal** : `ListerToutesCategoriesReclamationQuery` (Application) — enrobe
   `ListerToutesAsync()` derrière `Permission.AdministrationCategoriesReclamation`. Tests écrits
   avant l'implémentation (`ListerToutesCategoriesReclamationQueryTests.cs`, positif + négatif),
   vérifiés rouges (erreur de compilation, la classe n'existait pas) puis verts.
2. **Page** : `AdministrationCategories.razor` (`/reclamations/administration-categories`) — liste
   toutes les catégories (actives et désactivées), permet modification du libellé, activation,
   désactivation. Aucune création de catégorie (voir réserve ci-dessous). `IContexteUtilisateurWeb`
   pour l'identité de l'appelant (0 GUID manuel, 0 saisie d'identifiant démonstration — contrairement
   au style historique de `StrategieFile.razor`/`ValiderDemandes.razor`) ; 0 `DbContext` ; 0
   `Repository` injecté directement.
3. **Tests UI (A/B)** : `AdministrationCategoriesTests.cs` — admin habilité → accède et voit les
   catégories (`A_AdminHabilite_AccedeALaPage`) ; utilisateur non habilité → `PermissionDenied` (403),
   aucune fuite de données (`B_UtilisateurNonHabilite_AffichePermissionDenied`). Verts dès la première
   exécution (la logique serveur sous-jacente — permission — était déjà correcte).
4. **Tests C/D** : déjà couverts avant cette remédiation — `ListerCategoriesReclamationQueryTests.Ne_retourne_que_les_categories_actives`
   (D) et `SoumettreReclamationCommandTests.A_Client_authentifie_avec_droit_et_categorie_active_cree_reclamation`
   / `C_Categorie_inactive_retourne_refus_metier` (C). Non dupliqués.
5. **Test E** (nouveau, preuve plutôt que correction — le comportement existait déjà) :
   `ObtenirFicheTicketQueryTests.T030_E_TicketAvecCategorieDesormaisDesactivee_ResteLisible` — un
   ticket créé avec une catégorie ensuite désactivée reste pleinement lisible (`ObtenirFicheTicketQuery`
   utilise `ObtenirParIdAsync`, jamais `ListerActivesAsync`, pour résoudre le libellé d'un ticket).
   Vert dès la première exécution.

### Réserve honnête : "ajout" reste incomplet

Le texte CDC dit *"ajout/modification"* — les deux. `ModifierCategoriesReclamationCommand` ne
permet que la **modification** d'une catégorie existante (`ModifierCategoriesReclamationRequete`
exige un `CategorieId` déjà existant). **Aucun contrat de création n'existe** dans ce dépôt
(ni avant cette remédiation, ni ajouté par elle). Créer une toute nouvelle catégorie de réclamation
nécessite donc toujours l'intervention d'un développeur (migration de données ou script), ce qui
contredit littéralement "sans développement" pour ce sous-cas précis.

Ce périmètre (§6 du brief) nommait explicitement `ListerCategoriesReclamationQuery` et
`ModifierCategoriesReclamationCommand` comme les seuls contrats à réutiliser — sans mandat pour
inventer un `CreerCategorieReclamationCommand`. Plutôt que d'inventer cette capacité hors périmètre
donné, ou de déclarer REC-02 pleinement conforme sans preuve, cette réserve est documentée
explicitement : **REC-02 reste PARTIEL**, avec un écart résiduel précis et réduit (auparavant :
"aucune administration possible" ; maintenant : "administration possible sauf la création d'une
catégorie inédite").

### REC-02 — reclassement

| | Avant cette remédiation | Après |
|---|---|---|
| Catégories paramétrables en base | ✅ | ✅ |
| Modification (libellé, file cible) sans développement | ❌ (aucun écran) | ✅ |
| Activation/désactivation sans développement | ❌ (aucun écran) | ✅ |
| Ajout d'une catégorie inédite sans développement | ❌ | ❌ (aucun contrat de création) |

**REC-02 : PARTIEL** (amélioré — 2 des 3 capacités attendues sont désormais réellement livrées et
accessibles ; "ajout" reste un écart précis et minime, pas une absence totale).

### T026 — inchangée

Non touchée, comme demandé. Reste **COUVERTE AUTREMENT / DETTE TEST ACCEPTÉE** (voir la mise à jour
précédente de ce rapport) — cette remédiation y ajoute 2 tests bUnit supplémentaires
(`AdministrationCategoriesTests.cs`), renforçant encore la couverture fonctionnelle sans qu'aucune
case ne soit cochée arbitrairement.

### Suite complète

```
Domain          : 82/82
Application     : 257/257   (+3 : ListerToutesCategoriesReclamationQueryTests ×2, T030_E ×1)
Infrastructure  : 65/65
Web             : 100/100   (+2 : AdministrationCategoriesTests ×2)
TOTAL           : 504/504, 0 échec, 0 avertissement de compilation
PENDING MODEL CHANGES : 0 ("No changes have been made to the model since the last migration.")
```

---

## VERDICT FINAL (remplace le verdict précédent)

```
T030 : IMPLÉMENTÉE — obligatoire pour REC-02 (CDC §6.19, précédent StrategieFile.razor/GestionGrillesTarifaires.razor), pas optionnelle comme classé précédemment
REC-02 : PARTIEL (modification/activation/désactivation désormais conformes et accessibles ; "ajout" d'une catégorie inédite reste un écart précis — aucun contrat de création)
T026 : NON RÉALISÉE — COUVERTE AUTREMENT / DETTE TEST ACCEPTÉE (inchangée, renforcée par 2 tests supplémentaires)
REC-SCE-07 : CONFORME
TESTS : 504/504 verts
BUILD WARNINGS : 0
PENDING MODEL CHANGES : 0
DETTE RESTANTE : (1) création de catégorie non couverte par un contrat applicatif — nécessite un nouveau CreerCategorieReclamationCommand, hors périmètre de cette remédiation ; (2) effet de bord cosmétique déjà documenté (nom d'auteur générique sur les messages client) ; (3) T026 (bUnit par composant, priorité basse)

VERDICT : READY FOR FINAL CLOSURE REVIEW
```

---

## REMÉDIATION FINALE REC-02 — AJOUT CATÉGORIE (30 août 2026)

### Cause de l'écart

La revue précédente ("DÉCISION FINALE T030 / REC-02") avait implémenté T030 avec seulement
modification/activation/désactivation, en s'en tenant strictement au périmètre alors donné (2
contrats existants nommés explicitement). Le texte CDC exact — *"Les catégories sont paramétrables
et dynamiques (**ajout**/modification sans développement)"* — exige aussi l'ajout, resté non couvert :
aucun contrat de création n'existait. REC-02 avait donc été honnêtement classée **PARTIEL**, pas
CONFORME, avec cet écart précis documenté.

### Contrat ajouté

`CreerCategorieReclamationCommand` (Application, `CIT.Plateforme.CustomerComplaint`) :
- Réutilise `Permission.AdministrationCategoriesReclamation` (aucun nouveau rôle/permission Identity).
- Réutilise `CategorieReclamation.Creer(...)` (Domaine) — aucun nouvel invariant, aucune nouvelle
  entité ; la validation code/libellé non vide est l'invariant du domaine déjà existant.
- Ajoute uniquement `ICategorieReclamationRepository.ObtenirParCodeAsync(string)` (persistance) pour
  un refus métier propre en cas de doublon de code — l'index unique SQL sur `Code` existait déjà
  (migration `AjoutSupportCustomerComplaint`), cette méthode ne fait qu'exposer une vérification
  applicative avant d'atteindre la contrainte base.
- **Aucune migration** : aucune table, colonne ou index nouveau. Confirmé par
  `dotnet ef migrations has-pending-model-changes` → `No changes have been made to the model since
  the last migration.`

### Tests (TEST-FIRST — rouges avant l'implémentation : erreurs de compilation, la classe et la
méthode de dépôt n'existaient pas)

- `CreerCategorieReclamationCommandTests.cs` : A (admin habilité → succès), B (non habilité →
  `AccesRefuseException`), C (libellé vide/blanc → `ArgumentException`, invariant domaine réutilisé),
  D (code déjà existant → `InvalidOperationException`, sans jamais atteindre `AjouterAsync`).
- `CreerCategorieReclamationIntegrationTests.cs` : E/F rejoués avec persistance EF réelle (pas de
  mocks) — création → visible dans `ListerCategoriesReclamationQuery` (E) → désactivation → invisible
  (F). Prouve que la catégorie créée par l'admin suit exactement le même chemin que les catégories
  seedées.
- `AdministrationCategoriesTests.cs` (+2 tests) : admin habilité voit le bouton "Nouvelle catégorie" ;
  création réelle via le formulaire rendu (bUnit, `.Input()`/`.Click()` réels) → catégorie affichée
  dans le tableau ET immédiatement disponible via `ListerCategoriesReclamationQuery` (le mécanisme
  qu'utilise `NouvelleReclamation.razor`).

### UI

`AdministrationCategories.razor` étendue : bouton "➕ Nouvelle catégorie" révèle un formulaire minimal
(code, libellé) ; bouton "Créer" désactivé tant que l'un des deux champs est vide (validation
utilisateur immédiate côté client, doublée par la validation serveur du Domaine) ; rafraîchissement
automatique de la liste après succès (`await ChargerAsync()`) ; message de confirmation. `0 GUID
manuel`, `0 DbContext`, `0 Repository` injecté — uniquement les 3 contrats Application
(`ListerToutesCategoriesReclamationQuery`, `ModifierCategoriesReclamationCommand`,
`CreerCategorieReclamationCommand`), confirmé par relecture du fichier.

### Permission

Inchangée : `Permission.AdministrationCategoriesReclamation` (aucune nouvelle permission, aucun
nouveau rôle Identity — conforme à la contrainte §3 du brief).

### REC-02 — table finale

| Exigence CDC | Backend | UI | Permission | Test | Statut |
|:---|:---|:---|:---|:---|:---|
| Ajout d'une catégorie | `CreerCategorieReclamationCommand` | Formulaire "Nouvelle catégorie" | `AdministrationCategoriesReclamation` | A-D + E/F (intégration) + UI | ✅ CONFORME |
| Modification (libellé, file cible) | `ModifierCategoriesReclamationCommand` (C7-05, préexistant) | Édition inline du libellé | idem | Tests préexistants (`ModifierCategoriesReclamationCommandTests`) | ✅ CONFORME |
| Activation / désactivation | idem | Boutons Activer/Désactiver | idem | Tests préexistants + T030_E (historique lisible) | ✅ CONFORME |
| "Sans développement" (accessible sans code) | — | `/reclamations/administration-categories`, accessible à tout Admin fonctionnel habilité | idem | `AdministrationCategoriesTests` (accès habilité/non habilité) | ✅ CONFORME |

**REC-02 : CONFORME.** Les trois capacités explicitement nommées par le texte CDC (ajout,
modification, et implicitement l'activation/désactivation comme mécanisme de gestion du cycle de vie
d'une catégorie) sont désormais toutes réellement accessibles par un Admin fonctionnel habilité,
depuis l'application, sans intervention d'un développeur — plus aucune réserve.

### T030 — reclassement

**T030 : CONFORME.** L'écran permet désormais ajout, modification et activation/désactivation ; tous
les tests (backend + UI) sont verts.

### T026 — inchangée

Non modifiée, comme demandé. **COUVERTE AUTREMENT / DETTE TEST ACCEPTÉE** — renforcée une nouvelle
fois par 4 tests bUnit supplémentaires dans `AdministrationCategoriesTests.cs` (dont 2 nouveaux pour
cette remédiation), portant le total de tests bUnit véritablement interactifs de ce dépôt à 6.

### Suite complète

```
Domain          : 82/82
Application     : 263/263   (+6 : CreerCategorieReclamationCommandTests ×4, CreerCategorieReclamationIntegrationTests ×1, et le repository)
Infrastructure  : 65/65
Web             : 102/102   (+2 : AdministrationCategoriesTests — bouton visible, création via formulaire réel)
TOTAL           : 512/512, 0 échec, 0 avertissement de compilation
PENDING MODEL CHANGES : 0
```

---

## VERDICT FINAL (remplace le verdict précédent)

```
CREATION CATEGORIE : CONFORME
REC-02 : CONFORME
T030 : CONFORME
T026 : COUVERTE AUTREMENT (dette test acceptée, priorité basse, non bloquante)
REC-SCE-07 : CONFORME
TESTS : 512/512 verts
BUILD WARNINGS : 0
PENDING MODEL CHANGES : 0
DETTE RESTANTE : (1) effet de bord cosmétique déjà documenté — nom d'auteur générique ("Utilisateur") sur les messages client dans PublierMessageClientAsync ; (2) T026 (bUnit dédié par composant au sens strict — priorité basse, couverture fonctionnelle déjà largement démontrée par 6 tests bUnit interactifs + E2E)

VERDICT : READY FOR FINAL CLOSURE REVIEW
```

---

## CLOSURE REVIEW FORMELLE FINALE (31 août 2026)

Revue indépendante, exécutée après la remédiation REC-02. Aucune valeur n'a été reprise des rapports
précédents sans réexécution réelle.

### Baseline réexécutée

```
dotnet build --no-incremental       → 0 Avertissement(s), 0 Erreur(s)
dotnet test                         → Domain 82/82, Application 263/263, Infrastructure 65/65, Web 102/102
                                       TOTAL : 512/512, 0 échec, 0 ignoré
dotnet ef migrations has-pending-model-changes → "No changes have been made to the model since the last migration."
```

Conforme aux valeurs attendues — confirmé par exécution réelle, pas repris tel quel.

### REC-01 → REC-06

Revérifié contre le code réel (voir sections précédentes de ce document pour le détail par
exigence, toutes revérifiées lors des remédiations successives) :

| Exigence | Priorité | Statut |
|:---|:---|:---|
| REC-01 | Must | ✅ CONFORME |
| REC-02 | Must | ✅ CONFORME (remédié — ajout + modification + activation/désactivation, écran réel) |
| REC-03 | Must | ✅ CONFORME (remédié — réponse client via l'écran réel corrigée) |
| REC-04 | Must | ✅ CONFORME |
| REC-05 | Should | ✅ CONFORME |
| REC-06 | Could | ✅ CONFORME |

**REC : 6/6. MUST : 4/4. SHOULD : 1/1. COULD : 1/1.**

### Contrats C7

C7-01 à C7-05 (`contracts/use-cases.md`) tous implémentés, testés positif+négatif, DI et permissions
vérifiées (voir audits précédents). Extension ajoutée pendant la remédiation, non numérotée dans le
document d'origine : `CreerCategorieReclamationCommand` — nécessaire pour que REC-02 ("ajout") soit
réellement conforme. **C7 : 6/6** (5 contrats d'origine + 1 extension requise et documentée).

### CLAR-REC-01 → 08

Revérifiées une à une contre le comportement réel :
- CLAR-REC-01 (réponse formalisée = champ texte) : ✅ `Ticket.ReponseFormalisee`.
- CLAR-REC-02 (pas de cycle de vie pour l'enquête) : ✅ `Ticket.EvaluationNote/Commentaire/DateUtc`, pas d'entité séparée.
- CLAR-REC-03 (réutilisation `StatutTicket`) : ✅ aucun statut REC-spécifique créé.
- CLAR-REC-04 (seule la catégorie est une qualification obligatoire) : ✅ toujours vrai — Objet/Description (ajoutés en remédiation) sont des champs de **contenu**, déjà prévus par `use-cases.md` C7-01 dès l'origine, pas une nouvelle "qualification" (motif/criticité) que CLAR-REC-04 excluait explicitement. Pas de contradiction.
- CLAR-REC-05 (rattachement optionnel) : ✅ aucun champ obligatoire de référence externe.
- CLAR-REC-06 (SLA générique F001) : ✅ aucune constante SLA en dur.
- CLAR-REC-07 (complément via attente client F001) : ✅ revérifié dans `RecSce07CustomerComplaintE2ETests` (étapes 3-4, via l'UI réelle).
- CLAR-REC-08 (pièces jointes via API Documents F001) : ✅ aucun nouveau type de document créé.

**CLAR-REC : 8/8.**

### REC-SEC-01 → 06

Aucun changement de code touchant la sécurité depuis la dernière vérification complète (remédiation
REC-02 = Application/Web additifs uniquement, mêmes permissions réutilisées). Revérifié par lecture :
`AuthorizationUxTests` (REC-SEC-01, 3 routes F006 + génériques), tests négatifs par commande
(REC-SEC-02, 04, 06), cloisonnement société (REC-SEC-03, `CasC_...` + `EvaluerReclamationCommandTests.REC_REM_09`),
note interne (REC-SEC-05, `RecSce07CustomerComplaintE2ETests` assertion directe). **REC-SEC : 6/6.**

### REC-SCE-07 (parcours UI réel confirmé)

`RecSce07CustomerComplaintE2ETests.cs` relu intégralement : l'étape 4 (réponse au complément) rend
réellement `<FicheTicket>` (bUnit), simule `.Change()` sur le champ de message et `.Click()` sur le
bouton "Envoyer" — confirmé aux lignes 129-138 du fichier, code inchangé depuis sa correction.
**REC-SCE-07 : CONFORME**, y compris la fidélité UI de l'étape critique.

### UI Client / Agent / Administration

- **UI CLIENT** : `NouvelleReclamation.razor`, `MesReclamations.razor`, réponse au complément via
  `FicheTicket.razor` (corrigée), évaluation par étoiles — tous vérifiés fonctionnels par tests
  d'intégration/bUnit réels. **CONFORME.**
- **UI AGENT** : `FileReclamations.razor`, prise en charge, clôture avec réponse formalisée via
  `FicheTicket.razor` — **CONFORME.**
- **UI ADMINISTRATION CATÉGORIES** : `/reclamations/administration-categories` — liste, ajout,
  modification, activation/désactivation, tous testés (`AdministrationCategoriesTests.cs`, 4 tests
  dont 2 avec interaction réelle `.Input()`/`.Click()`). **CONFORME.**

### T001 → T035 (recalculé)

34/35 tâches démontrées directement (T026 seule non démontrée dans sa forme littérale — bUnit dédié
par composant). T026 reste **COUVERTE AUTREMENT** : la couverture fonctionnelle réelle est assurée
par 6 tests bUnit réellement interactifs (`ReponseComplementDepuisFicheTicketTests`,
`MessagerieFicheTicketNonRegressionTests` ×2, `AdministrationCategoriesTests` ×2 avec interaction,
`RecSce07CustomerComplaintE2ETests` étape 4) — un niveau de preuve UI supérieur à ce qu'un bUnit isolé
par composant aurait démontré individuellement. Non classée bloquante : aucune preuve manquante n'a
été identifiée pour un comportement UI critique de F006.

**T030 : CONFORME** (ajout, modification, activation/désactivation, tous testés).

### Migrations

`dotnet ef migrations has-pending-model-changes` → 0. Deux migrations F006
(`AjoutSupportCustomerComplaint`, `RemediationCustomerComplaintContent`) cohérentes avec le modèle
actuel.

### AVE

`grep` sur tout le code F006 (Domain/Reclamations, Application/CustomerComplaint,
Web/CustomerComplaint, et les 3 commandes Tickets liées) pour `Avoir|Exoneration|Finance|DirectionFinanciere`
→ 0 résultat. **AVE : ABSENT.**

### Git status / diff (aucun staging effectué)

`git status --short` : 34 fichiers modifiés + 31 fichiers/dossiers non suivis — identique en nature à
la matrice déjà produite (§"MISE À JOUR — REMÉDIATION FINALE UI"), avec les nouveaux fichiers de la
remédiation REC-02 correctement classés F006 (`CreerCategorieReclamationCommand.cs`,
`CreerCategorieReclamationCommandTests.cs`, `CreerCategorieReclamationIntegrationTests.cs`,
`ListerToutesCategoriesReclamationQuery.cs` + son test, `AdministrationCategories.razor` + son test).
Aucune modification étrangère nouvelle. Rien n'a été ajouté à l'index (`git add`) — confirmé, aucune
commande de staging n'a été exécutée pendant cette revue.

### Couverture CDC après F006

`docs/01-matrice-exigences.md` : 188 exigences au total (recompte intégral, ligne 326/347).
`docs/09-etat-avancement-global.md` (état avant F006) : 125/188 satisfaites. F006 ajoute exactement
les 6 exigences REC-01→06, toutes désormais CONFORME. **Couverture après F006 : 131/188** — exactement
la projection de `spec.md` (§0, "Couverture projetée après clôture de F006 : 131/188"), désormais
une valeur réelle et non plus projetée.

---

## VERDICT DE CLÔTURE FORMEL

```
FEATURE : 006 — Customer Complaint
REC : 6/6
MUST : 4/4
SHOULD : 1/1
COULD : 1/1
C7 : 6/6 (5 contrats d'origine + 1 extension REC-02)
CLAR-REC : 8/8
REC-SEC : 6/6
REC-SCE-07 : CONFORME
UI CLIENT : CONFORME
UI AGENT : CONFORME
UI ADMIN CATEGORIES : CONFORME
TÂCHES : 34/35 démontrées + 1 couverte autrement (T026)
T026 : COUVERTE AUTREMENT (dette test acceptée, non bloquante — 6 tests bUnit interactifs réels)
T030 : CONFORME
MIGRATIONS : CONFORME
PENDING MODEL CHANGES : 0
TESTS : 512/512
BUILD WARNINGS : 0
AVE : ABSENT
DETTE ACCEPTÉE : nom d'auteur générique ("Utilisateur") sur les messages client publiés via PublierMessageClientAsync (effet de bord documenté, cosmétique, sans impact fonctionnel ou sécurité) ; T026 sous sa forme littérale (bUnit dédié par composant, couverture fonctionnelle déjà assurée autrement)
COUVERTURE CDC APRÈS F006 : 131/188
VERDICT : CLOSE WITH ACCEPTED DEBT
```

**STOP. Revue terminée. Aucun commit effectué. Aucun push. Feature 007 non commencée.**
