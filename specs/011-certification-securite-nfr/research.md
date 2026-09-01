# Research — Feature 011 (Certification Sécurité & Socle NFR)

> **Phase 0.** Audit du **code réel** et des **suites de tests réelles** avant toute décision technique.
> **Aucun code produit, aucune migration, aucun commit.**
> Méthode : inspection du dépôt, pas lecture des rapports antérieurs.

---

## D0 — Constat structurant : la couverture existante est plus forte qu'estimée au preflight

Le preflight (`docs/18`) classait 11 exigences en « certifiable avec tests ». **L'audit du code contredit partiellement cette estimation** : trois exigences disposent déjà de suites qui démontrent **exactement** le comportement exigé par le CDC.

| Exigence | Estimation preflight | **Constat d'audit** |
|:---|:---|:---|
| **SEC-05** | tests à ajouter | **`JournalAuditAppendOnlyTests` couvre déjà les 3 scénarios** — modification impossible, suppression impossible, ajout possible — **sur SQL Server réel** |
| **ERR-03** | tests à ajouter | **`ControleDepotTests` couvre déjà type et taille**, **avec assertion du message d'erreur** (`Assert.Contains("exe", …)`, `Assert.Contains("taille", …)`) |
| **SEC-12** | certifiable sans code | **`AccesDocumentAutoriseTests` couvre 4 scénarios** dont le refus au non-propriétaire — mais **le scénario « lien ouvert sans authentification » est absent** |

**Conséquence de conception** : F011 doit **rattacher** avant d'**écrire**. Écrire un test qui duplique une preuve existante serait un gaspillage et une violation du principe de non-duplication.

---

## D1 — Audit exigence par exigence

| ID | Code existant (vérifié) | Tests existants (vérifiés) | Tests manquants | Code à modifier | Niveau de preuve retenu |
|:---|:---|:---|:---|:---|:---|
| **SEC-01** | Politique de mot de passe et verrouillage configurés au niveau du fournisseur d'identité (longueur minimale, plafond d'échecs, durée de verrouillage) | 4 fichiers **nomment** SEC-01 (`AutorisationServeurTests`, `CustomerComplaintSecurityTests`, `AuthorizationUxTests`, `NouvelEnrolementTests`) — ils couvrent l'**autorisation**, pas le **verrouillage** | **Verrouillage : AUCUN test.** Recherche `MaxFailedAccessAttempts` / `LockoutEnd` / `AccessFailedCount` / `IsLockedOut` dans `tests/` → **0 occurrence** | **AUCUN** | Intégration sur hôte réel |
| **SEC-03** | Service de cloisonnement + filtres serveur appliqués dans la requête ; habilitations par file | Cloisonnement **massivement** exercé (≥ 6 suites : Affectation, AvoirExoneration, BillingImport/Export…) mais **0 fichier ne nomme SEC-03** | Rattachement nominatif + scénario **agent hors de sa file** | **AUCUN** | Application + Intégration |
| **SEC-05** | Garde applicative rejetant `Modified`/`Deleted` sur les entrées d'audit avant enregistrement | **`JournalAuditAppendOnlyTests` — 3 scénarios, sur SQL Server réel** : modification impossible, suppression impossible, ajout possible | **AUCUN** — la preuve est complète | **AUCUN** | Infrastructure (déjà en place) |
| **SEC-10** | Lecture d'une clé de configuration pour la durée de session, avec glissement sur activité | **0 test** | Paramétrabilité **et** effet | **AUCUN produit** — configuration de test uniquement | Intégration |
| **SEC-11** | Indicateur d'activité du compte, consulté à l'authentification | `EstActif` exercé dans `DesactivationDernierAdministrateurTests` et `RattacherUtilisateurTests` — **sous l'angle de la gestion des collaborateurs, pas de l'accès** | Scénario **double** : accès refusé **ET** historique conservé | **AUCUN** | Application + Intégration |
| **SEC-12** | Lien de notification portant la référence du ticket ; lien relatif, sans jeton | `AccesDocumentAutoriseTests` (4 scénarios : antivirus, propriétaire, non-propriétaire, agent habilité) + `RaccordementMentionTests` | **Scénario « lien ouvert sans authentification » absent** | **AUCUN** | Application |
| **ERR-02** | Moteur de reprise complet : statut par canal, nouvelle tentative, traçage, isolation | 6 tests (TEST-ERR-02) | Démarrage sans configuration | **1 point unique** (D3) | Intégration (démarrage réel) |
| **ERR-03** | Contrôles de type et de taille paramétrables par étape, avec messages explicites | **`ControleDepotTests` — 3 scénarios avec assertion du message** | **AUCUN** pour type/taille — **gate sur « intégrité »** (D5) | **AUCUN** | Application (déjà en place) |
| **NFR-01** | Interface intégralement francophone | **0 test de langue** | Vérification reproductible des libellés | **AUCUN** | Interface |
| **NFR-02** | Directive d'affichage mobile déclarée ; points de rupture définis ; primitives adaptatives largement employées | **0 test** ; **aucun navigateur automatisé dans le dépôt** | Preuve structurelle en 5 points (D4) | **AUCUN** | Interface (structurel) |
| **NFR-11** | Calendrier ouvré paramétrable : heures d'ouverture/fermeture, jours ouvrés, **jours fériés**, mode « toujours ouvert » par défaut | 11 tests (`MoteurSlaTests`, `SurveillanceEcheancesCalendrierTests`, `ParametresSlaProviderTests`) | Scénario limite **« dépôt hors heures ouvrées »** | **AUCUN** | Domaine + Application |

---

## D2 — Décision : trois régimes de certification, pas un seul

**Décision.** Les 11 exigences sont réparties en **trois régimes**, selon ce que l'audit démontre — et non selon une hypothèse uniforme.

| Régime | Exigences | Travail |
|:---|:---|:---|
| **R1 — Rattachement nominatif seul** | **SEC-05**, **ERR-03** (volets type/taille) | La preuve existe et démontre le texte CDC. Le travail consiste à **rattacher** la suite existante à l'exigence, sans écrire de test redondant |
| **R2 — Complément de preuve** | SEC-01, SEC-03, SEC-10, SEC-11, SEC-12, NFR-01, NFR-02, NFR-11 | Le mécanisme existe ; **un ou plusieurs scénarios manquent**, principalement les cas négatifs |
| **R3 — Correction produit minimale** | **ERR-02** | Un point unique de validation au démarrage |

**Rationale.** Le principe de non-duplication (Constitution, Principe IV) interdit d'écrire un test qui reproduit une preuve existante. Trois exigences sur onze n'appellent **aucun test nouveau** ou presque.

**Alternatives écartées.**
- *Réécrire une suite de certification complète et autonome pour les 11* — rejetée : duplication massive, et le rattachement nominatif suffit à la Closure Review.
- *Se contenter du rattachement pour les 11* — rejetée : SEC-01 n'a **aucun** test de verrouillage, SEC-10 **aucun** test, NFR-01/02 **aucun** test. Le rattachement serait une certification par présomption.

---

## D3 — ERR-02 : validation au démarrage du composant

**Décision.** Valider la configuration de reprise **au démarrage du composant qui en dépend**, via le mécanisme d'options du framework déjà employé par la plateforme, avec validation déclenchée au démarrage.

**Portée : le composant, pas l'application** (décision de clarification, option B). Le reste de la plateforme ne dépend pas de cette configuration ; faire échouer l'application entière serait un durcissement non soutenu par le CDC.

**Rationale.** L'écart constaté n'est pas fonctionnel — les trois obligations d'ERR-02 sont tenues et testées. L'écart est que **l'absence de configuration produit une dégradation silencieuse** : le composant démarre, journalise une erreur périodique, et ne retente jamais. La correction consiste à rendre cet échec **explicite et immédiat**.

**Contraintes absolues, opposables à l'implémentation.**
- Aucun nombre de tentatives, aucune cadence, aucune condition d'arrêt **en dur**.
- Aucune valeur par défaut de production.
- **CLAR-NOT-02 reste ouverte** — la validation signale l'absence, elle ne la comble pas.
- Le moteur de notification F009 **n'est pas modifié**, hormis le point de validation.
- Aucun fournisseur tiers.

**Ordre de mise en œuvre — contraignant** (D6).

**Alternatives écartées.**
- *Faire échouer l'application entière* — rejetée : non soutenue par le CDC, et disproportionnée.
- *Journaliser un avertissement plus visible* — rejetée : ne satisfait pas l'exigence, la dégradation resterait silencieuse pour l'exploitant.
- *Introduire une valeur par défaut documentée* — rejetée : résoudrait CLAR-NOT-02 implicitement.

---

## D4 — NFR-02 : preuve structurelle, limite déclarée

**Décision.** Certifier NFR-02 par une **preuve structurelle en 5 points**, et **déclarer explicitement sa limite**.

**Constat d'outillage vérifié.** Le dépôt ne contient **aucun navigateur automatisé** : les dépendances de test se limitent au rendu de composants (bUnit), au harnais d'hôte applicatif (`Mvc.Testing`), au fournisseur en mémoire et au runner. Ni Playwright, ni Selenium, ni pilote équivalent.

**Conséquence assumée.** Le rendu de composants produit du balisage **sans moteur de rendu ni calcul de dimensions**. Il ne peut pas prouver une mise en page. **Prétendre le contraire serait une certification par présomption.**

**Preuve retenue (structurelle)** :
1. directive de largeur d'affichage destinée aux terminaux mobiles déclarée ;
2. points de rupture définis dans la feuille de styles ;
3. écrans du parcours client employant des primitives adaptatives plutôt que des largeurs fixes ;
4. contenus larges par nature placés dans un conteneur défilant propre ;
5. navigation et actions principales atteignables dans la variante réduite.

**Limite déclarée** : cette preuve démontre que **le contrat responsive est appliqué**, non que le rendu est visuellement exempt de défaut. La validation visuelle sur navigateur réel reste une **dette de recette**, distincte de la certification CDC.

**Alternatives écartées.**
- *Introduire un navigateur automatisé* — rejetée : infrastructure lourde pour une exigence que le CDC formule **sans aucun seuil**. Disproportionné (Principe IV, YAGNI).
- *Certifier sur la seule inspection du CSS* — rejetée : ne dit rien des écrans réellement rendus.
- *Déclarer NFR-02 non certifiable* — rejetée : le contrat responsive **est** appliqué et démontrable ; seule la validation visuelle manque.

> **Interdiction opposable** : toute largeur employée dans un test est une **valeur technique de démonstration, non contractuelle**. Aucune dimension ne doit apparaître comme un seuil CDC, ni dans le domaine, ni dans la documentation métier.

---

## D5 — GATE-ERR03 : le volet « intégrité »

**Décision.** Poser un **gate métier** avant de certifier ERR-03, et **ne rien inventer** en attendant.

**Constat vérifié.** Le CDC énumère trois volets — *intégrité, type, taille*. Audit :

| Volet | Mécanisme | Test | Message explicite |
|:---|:---|:---|:---|
| Type | liste d'extensions paramétrable par étape | ✅ `Depot_avec_type_non_autorise_est_refuse_avec_message_explicite` | ✅ |
| Taille | taille maximale paramétrable par étape | ✅ `Depot_depassant_la_taille_maximale_est_refuse_avec_message_explicite` | ✅ |
| **Intégrité** | **AUCUN mécanisme dédié** — ni empreinte, ni somme de contrôle (recherche `checksum`/`hash`/`sha256`/`md5` → 0 occurrence) | — | — |

Le seul contrôle portant sur le **contenu** est le point d'ancrage antivirus, qui relève de **SEC-07 — exclue**.

**GATE-ERR03** :

| Lecture du métier | Conséquence |
|:---|:---|
| *« Intégrité » = le fichier est exploitable et cohérent selon les mécanismes existants* | **ERR-03 reste dans F011** — certifiée par rattachement. Gain **+11**, projection **157/188** |
| *« Intégrité » exige un mécanisme distinct* (empreinte, somme de contrôle, détection de corruption) | **ERR-03 SORT de F011** — c'est un développement nouveau, hors nature de certification. Total **10**, projection **156/188** |

**Interdiction absolue** : **ne pas inventer une somme de contrôle pour conserver +11.**

**Statut** : gate **conditionnel**, à trancher **avant** l'exécution de `ERR-CERT-02`. Il ne bloque **pas** `/speckit.tasks` : les 10 autres exigences sont indépendantes.

---

## D6 — Ordre de mise en œuvre d'ERR-02 (contraignant)

**Décision.** Imposer l'ordre suivant. **L'inverser ferait tomber simultanément l'ensemble des tests qui démarrent l'hôte applicatif réel** — soit la totalité de la suite Web.

```
1. Adapter le HARNAIS de tests pour fournir explicitement une configuration valide
2. Vérifier la baseline — suite complète verte
3. Activer le fail-fast dans le PRODUIT
4. Tester la configuration absente ou invalide  → échec explicite du composant
5. Tester la configuration valide              → composant démarré
6. Non-régression F009 + suite Web complète
```

**Règle non négociable** : **le comportement du produit n'est jamais modifié pour accommoder un harnais mal configuré.** Si un test échoue après l'étape 3, c'est le harnais qui est corrigé, jamais le fail-fast qui est affaibli.

**Rationale.** Précédent mesuré : lors de F009, l'ajout d'une dépendance dans l'en-tête de page a fait échouer un test de disposition faute de service enregistré dans le harnais. Le risque est ici d'un ordre de grandeur supérieur, l'hôte applicatif étant démarré par toute la suite Web.

---

## D7 — SEC-10 : ne pas promouvoir le défaut technique

**Décision.** Certifier la **paramétrabilité** et l'**effet**, sans toucher au défaut technique existant et **sans l'inscrire dans la configuration de production**.

**Constat vérifié.** Le produit lit une clé de configuration de durée de session. **Cette clé est absente des fichiers de configuration** ; le code retombe sur un **défaut technique de 30 minutes**, documenté en commentaire comme provisoire *« tant que le paramétrage métier n'a pas été administré »*.

**Analyse.** Ce n'est **pas** un défaut au sens d'ERR-02 : la session **expire bien**, et le mécanisme **est** paramétrable. Le CDC exige *« une période d'inactivité paramétrable »* — les deux propriétés sont tenues. **Le CDC ne fixe aucune valeur.**

**Ce que F011 fait** : prouver que (1) une valeur configurée est **effectivement appliquée**, (2) une session dépassée se voit **refuser l'accès**.

**Ce que F011 ne fait pas** :
- inscrire 30 minutes dans la configuration de production comme s'il s'agissait d'une règle métier ;
- durcir, supprimer ou modifier le défaut technique ;
- trancher la valeur d'exploitation — **décision métier/DSI, hors périmètre**.

La durée employée par les tests est une **valeur technique de démonstration, explicitement non contractuelle**.

---

## D8 — NFR-11 : certifier sans étendre

**Décision.** Certifier le calendrier ouvré **existant**, sans l'étendre.

**Constat vérifié.** Le calendrier est un objet de domaine pur, **entièrement paramétrable**, portant heures d'ouverture et de fermeture, jours ouvrés de la semaine, **et jours fériés**. Il expose un mode « toujours ouvert » par défaut, explicitement conçu pour **ne jamais inventer d'horaires métier** tant qu'aucun paramétrage n'a été saisi. 11 tests l'exercent déjà.

**Ce qui manque** : le scénario limite **« dépôt hors heures ouvrées »** — dépôt accepté, décompte du SLA démarrant selon les règles de la file — n'est couvert par aucun test.

**Interdictions.** Aucun calendrier national, aucun service externe de jours fériés, aucune règle calendaire nouvelle. Le mécanisme de jours fériés **existe déjà** sous forme paramétrable ; le CDC n'exige rien de plus.

---

## D9 — Niveaux de test : au plus juste

**Décision.** Choisir le niveau **le plus bas** qui démontre l'exigence.

| Niveau | Employé pour | Justification |
|:---|:---|:---|
| **Domaine** | NFR-11 (calcul ouvré) | Objet pur, aucune dépendance d'infrastructure |
| **Application** | SEC-03, SEC-11, SEC-12, ERR-03 | Règles arbitrées dans la couche applicative |
| **Infrastructure (SQL réel)** | SEC-05 uniquement | **Seule** exigence dont la garantie repose sur la persistance ; suite déjà en place |
| **Intégration (hôte réel)** | SEC-01, SEC-10, ERR-02 | Politique d'identité, cookie de session et démarrage ne sont observables qu'à ce niveau |
| **Interface** | NFR-01, NFR-02 | Rendu des écrans |

**Aucune infrastructure de test nouvelle n'est introduite.** SQL Server réel n'est employé que là où la persistance est en jeu.

---

## Synthèse des décisions

| # | Décision | Code produit | Écart au socle |
|:--|:---|:---:|:---|
| D0 | La couverture existante dépasse l'estimation du preflight | 0 | — |
| D1 | Audit exigence par exigence | 0 | — |
| D2 | Trois régimes de certification (R1/R2/R3) | 0 | — |
| D3 | ERR-02 : validation au démarrage du composant | **1 point** | Oui, minimal et justifié |
| D4 | NFR-02 : preuve structurelle, limite déclarée | 0 | — |
| D5 | GATE-ERR03 : gate métier sur « intégrité » | 0 | — |
| D6 | Ordre contraignant harnais → produit | 0 | — |
| D7 | SEC-10 : ne pas promouvoir le défaut technique | 0 | — |
| D8 | NFR-11 : certifier sans étendre | 0 | — |
| D9 | Niveaux de test au plus juste | 0 | — |

**Total code produit : un seul point de modification (ERR-02).**
