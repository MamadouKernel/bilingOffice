---
description: "Task list for Feature 011 — Certification Sécurité & Socle Non-Fonctionnel"
---

# Tasks: Feature 011 — Certification Sécurité & Socle Non-Fonctionnel

**Input** : `specs/011-certification-securite-nfr/{spec.md, plan.md, research.md, data-model.md, contracts/use-cases.md, quickstart.md, checklists/requirements.md}`, `docs/17`, `docs/18` + addendum, CDC original, `.specify/memory/constitution.md`, **code réel F001→F010 audité** (research.md D0/D1).

**Nature** : **FEATURE DE CERTIFICATION.** Le code produit est réduit au strict minimum — **un seul point**. La valeur produite est la **preuve**, pas le développement.

**Statut** : **GÉNÉRÉ, NON EXÉCUTÉ.** `/speckit.implement` n'est pas lancé, aucun code n'est écrit, aucune migration créée.

**Régimes** : **R1** preuve existante / rattachement · **R2** tests complémentaires · **R3** correction produit minimale.

---

## Invariants opposables à chaque tâche

| # | Invariant | Source |
|:--|:---|:---|
| **I1** | **Aucune migration.** Aucune tâche ne contient `dotnet ef migrations add` | data-model.md · Principe VIII |
| **I2** | **0 entité, 0 table, 0 colonne, 0 enum métier** nouveaux | data-model.md §1 |
| **I3** | **Un seul fichier de produit modifié** — la validation de configuration ERR-02. Au-delà : **STOP et revue d'écart** | contracts C11-P01 |
| **I4** | **Aucune valeur de reprise en dur**, aucune cadence, aucune valeur par défaut de production. **CLAR-NOT-02 reste ouverte** | research.md D3 |
| **I5** | **Aucun test ne duplique une preuve existante** — le régime R1 impose de rattacher avant d'écrire | research.md D2 · Principe IV |
| **I6** | **SEC-02, SEC-07, SEC-08** : aucune implémentation, aucun TEST-SPEC, aucun gain | Final Scope Gate |
| **I7** | **Aucun écran modifié** sans anomalie démontrée. Les tests NFR **inspectent** l'existant | research.md D4 |
| **I8** | **Aucune infrastructure de test lourde** — pas de navigateur automatisé, pas de nouveau paquet | research.md D4/D9 |
| **I9** | **Aucune durée, largeur ou valeur métier inventée.** Les valeurs de test sont techniques et non contractuelles | research.md D4/D7 |
| **I10** | **`git add -A` proscrit.** Les 83 fichiers préexistants ne sont jamais absorbés | plan.md R4 |

---

## Phase 1 — Baseline / Gate

**But** : établir la baseline réelle. Aucune tâche de code.

- [X] T001 **Baseline exécutée le 1er septembre 2026** (exécution **sérialisée** `xUnit.ParallelizeTestCollections=false`, `xUnit.MaxParallelThreads=1`) :
  - `dotnet build` → **0 avertissement, 0 erreur**
  - Domain **154/154** · Infrastructure **83/83** · Application **425/425** · Web **156/156**
  - **TOTAL : 818/818**
  - `has-pending-model-changes` → *No changes have been made to the model since the last migration.*
- [X] T002 **Gate franchi** — baseline conforme au minimum exigé (≥ 818, 0 avertissement, 0 changement en attente). Les phases suivantes peuvent démarrer.
- [X] T003 Relever l'état du worktree avant toute modification : `git status --short`, `git diff --name-only`, `git diff --stat`. **Consigner les 83 fichiers `src/`+`tests/` préexistants** (refonte UI / connexion / 2FA) comme **non absorbables** par F011.

**Checkpoint** : baseline 818/818 ✅ · worktree caractérisé.

---

## Phase 2 — Certification SEC (US1 → US6)

### 2.1 — Régime R1 : rattachement, sans test nouveau

> **`SEC-05` est déjà démontrée.** `tests/CIT.Plateforme.Infrastructure.Tests/Persistence/JournalAuditAppendOnlyTests.cs` couvre les trois scénarios **sur SQL Server réel** : modification impossible, suppression impossible, ajout possible. **Écrire un test ici serait une duplication (I5).**

- [X] T004 [US3] **VÉRIFIER avant de revendiquer** — relire `tests/CIT.Plateforme.Infrastructure.Tests/Persistence/JournalAuditAppendOnlyTests.cs` et confirmer que ses trois cas démontrent bien le texte CDC de SEC-05 (*« journal d'audit complet et non modifiable des actions utilisateurs et administrateurs »*). Si un volet manque, le signaler — **ne pas présumer**.
- [X] T005 [US3] Rattacher nominativement cette suite à **`SEC-CERT-03`** : ajouter en tête de fichier le commentaire de traçabilité citant **SEC-05** et son texte CDC. **Aucun test nouveau, aucune modification de logique de test.**
- [X] T006 [US3] Exécuter `tests/CIT.Plateforme.Infrastructure.Tests` et consigner le résultat comme **preuve Closure de SEC-05**.

**Checkpoint 2.1** : SEC-05 certifiée — **0 test nouveau**.

### 2.2 — SEC-01 · Authentification et verrouillage (US1, R2)

> **Trou confirmé** : recherche de `MaxFailedAccessAttempts` / `LockoutEnd` / `AccessFailedCount` / `IsLockedOut` dans `tests/` → **0 occurrence**. Le verrouillage n'est démontré par aucun test.

- [X] T007 [US1] TEST RED — `tests/CIT.Plateforme.Web.Tests/Securite/SecCert01VerrouillageTests.cs` (nouveau) : identifiants valides → accès accordé. **Niveau intégration, hôte réel.**
- [X] T008 [US1] TEST RED — même fichier : enchaîner des tentatives erronées **jusqu'au plafond configuré** → le compte se verrouille.
- [X] T009 [US1] TEST RED — même fichier : **cas décisif** — présenter le **bon** mot de passe pendant le verrouillage → **accès refusé**. C'est ce cas qui démontre que le verrouillage prime sur la validité du mot de passe.
- [X] T010 [US1] TEST RED — même fichier : demander une ressource protégée **sans session** → renvoi au contrôle d'identité.
- [X] T011 [US1] Vérifier GREEN sans modifier le produit. **Si un test échoue, le mécanisme est en défaut : ARRÊTER et signaler** — ne pas ajuster la politique existante pour faire passer le test.

### 2.3 — SEC-03 · Cloisonnement strict (US2, R2)

> Le cloisonnement est **massivement** exercé (≥ 6 suites) mais **aucune ne nomme SEC-03**. Le travail est le rattachement nominatif plus les cas manquants.

- [X] T012 [US2] Recenser les suites existantes démontrant déjà le cloisonnement client ↔ client, et consigner ce constat. **Ne pas les réécrire (I5).**
- [X] T013 [US2] TEST RED — `tests/CIT.Plateforme.Application.Tests/Securite/SecCert02CloisonnementTests.cs` (nouveau) : client A demande le ticket de client B **en fournissant son identifiant** → refus **côté serveur**, sans divulguer l'existence ni l'appartenance.
- [X] T014 [US2] TEST RED — même fichier : agent habilité agit dans **sa** file → accepté.
- [X] T015 [US2] TEST RED — même fichier : agent tente une action sur une file **hors de son périmètre** → refus **côté serveur**.
- [X] T016 [US2] TEST RED — même fichier : le refus est prononcé **indépendamment de ce que l'interface affiche** — le test appelle la couche applicative directement.
- [X] T017 [US2] Vérifier GREEN. Les tests **consomment les services de cloisonnement existants** — **aucune duplication du RBAC (I5)**.

### 2.4 — SEC-10 · Expiration de session paramétrable (US4, R2)

> **Aucun test n'existe.** Constat vérifié : la clé de configuration est **absente des fichiers de configuration** ; le code retombe sur un **défaut technique**, documenté comme provisoire.

- [X] T018 [US4] TEST RED — `tests/CIT.Plateforme.Web.Tests/Securite/SecCert04SessionTests.cs` (nouveau) : démarrer l'hôte avec une durée fournie par la **configuration de test** → la politique de session reflète **la valeur configurée**. Démontre le mot *« paramétrable »* du CDC.
- [X] T019 [US4] TEST RED — même fichier : une session utilisée **dans** la période reste valide.
- [X] T020 [US4] TEST RED — même fichier : une session dont la validité est **dépassée** → **accès refusé**, ré-authentification exigée.
- [X] T021 [US4] Vérifier GREEN **sans modifier le produit**.
- [X] T022 [US4] **CONTRÔLE (I9)** — vérifier qu'**aucune durée n'a été ajoutée à `src/CIT.Plateforme.Web/appsettings.json`**. Le défaut technique existant n'est **ni durci, ni promu en règle métier, ni supprimé**. La durée employée par les tests est **technique et non contractuelle**.

### 2.5 — SEC-11 · Désactivation sans perte d'historique (US5, R2)

> L'indicateur d'activité est exercé sous l'angle de la **gestion des collaborateurs**, jamais sous celui de l'**accès**.

- [X] T023 [US5] TEST RED — `tests/CIT.Plateforme.Application.Tests/Securite/SecCert05DesactivationTests.cs` (nouveau) : un compte agent actif produit de l'historique (traces, audit) → traces créées.
- [X] T024 [US5] TEST RED — même fichier : **cas décisif, un seul scénario** — désactiver le compte, puis (a) tenter un accès → **refusé immédiatement**, (b) consulter l'historique produit → **intégralement conservé**. Les deux volets doivent être démontrés **ensemble** : c'est leur conjonction qui satisfait le texte CDC.
- [X] T025 [US5] Vérifier GREEN. **Ne pas extrapoler** vers la suppression de compte ni vers l'invalidation d'une session déjà ouverte — **le CDC n'en parle pas**.

### 2.6 — SEC-12 · Lien de notification (US6, R2)

> 4 scénarios d'accès documentaire existent ; **le scénario « lien ouvert sans authentification » est absent**.

- [X] T026 [US6] **VÉRIFIER avant de revendiquer** — relire `tests/CIT.Plateforme.Application.Tests/Documents/AccesDocumentAutoriseTests.cs` et confirmer les volets déjà démontrés (refus au non-propriétaire, agent habilité). Consigner ce qui **manque**.
- [X] T027 [US6] TEST RED — `tests/CIT.Plateforme.Application.Tests/Securite/SecCert06LienNotificationTests.cs` (nouveau) : un lien de notification ouvert **sans authentification** → conduit au contrôle d'identité, **aucun détail du dossier exposé**.
- [X] T028 [US6] TEST RED — même fichier : destinataire dont l'habilitation a été **retirée** → refus **sans divulguer** l'existence, le contenu ni l'appartenance du ticket.
- [X] T029 [US6] TEST — même fichier : la notification porte une **référence de ticket**, **jamais une URL absolue** et **jamais un jeton** (vérification structurelle).
- [X] T030 [US6] Rattacher nominativement `SEC-CERT-06` et vérifier GREEN. **Aucun chemin d'accès parallèle n'est introduit.**

- [X] T031 **Non-régression Phase 2** — rejouer intégralement `Domain`, `Application`, `Infrastructure`, `Web` en sérialisé. Tout échec **bloque**.

**Checkpoint Phase 2** : SEC-01, SEC-03, SEC-05, SEC-10, SEC-11, SEC-12 certifiées. **0 ligne de code produit modifiée.**

---

## Phase 3 — ERR-02 (US7, R3) — **ordre contraignant**

> ⚠️ **L'ordre ci-dessous ne doit jamais être inversé.** Activer la validation avant d'adapter le harnais ferait tomber **l'intégralité de la suite Web**, dont chaque test démarre l'hôte applicatif réel (plan.md R1).

### Étape 1 — Harnais

- [X] T032 [US7] Adapter `tests/CIT.Plateforme.Web.Tests/Support/PlateformeWebApplicationFactory.cs` pour **injecter explicitement une configuration de reprise valide** dans l'hôte de test. **Aucune modification du produit à ce stade.**
- [X] T033 [US7] **Vérifier que la valeur injectée est une valeur de test**, clairement commentée comme **technique et non contractuelle** — elle ne préjuge en rien de CLAR-NOT-02 **(I4)**.

### Étape 2 — Baseline ciblée

- [X] T034 [US7] Rejouer `tests/CIT.Plateforme.Web.Tests` **avant toute modification de produit** → doit rester **156/156**. Si ce n'est pas le cas, **ARRÊTER** : le harnais est mal adapté.

### Étape 3 — Test RED

- [X] T035 [US7] TEST RED — `tests/CIT.Plateforme.Web.Tests/Securite/ErrCert01ConfigurationRepriseTests.cs` (nouveau) : démarrer le composant **sans** configuration de reprise → attendre un **échec explicite** désignant le paramètre manquant. **Doit échouer** à ce stade (le fail-fast n'existe pas encore).
- [X] T036 [US7] TEST RED — même fichier : démarrer avec une configuration **invalide** (valeur nulle ou négative) → même échec explicite.

### Étape 4 — Correction produit (SEUL point de code)

- [X] T037 [US7] Implémentation GREEN — `src/CIT.Plateforme.Infrastructure/DependencyInjection.cs` : faire valider la configuration de reprise **au démarrage du composant** via le mécanisme d'options déjà employé par la plateforme. **Portée : le composant, PAS l'application entière (research.md D3).**
- [X] T038 [US7] **CONTRÔLE (I3)** — `git diff --name-only src/` doit lister **exactement un fichier**. Si un second fichier de produit doit être modifié : **ARRÊTER et ouvrir une revue d'écart**.
- [X] T039 [US7] **CONTRÔLE (I4)** — vérifier qu'aucun nombre de tentatives, aucune cadence, aucune condition d'arrêt, aucune valeur par défaut de production n'a été introduit. Le message d'erreur **signale l'absence** ; il ne la **comble** pas.
- [X] T040 [US7] **CONTRÔLE** — vérifier que le moteur de notification F009 est **inchangé** hormis le point de validation : `git diff --stat src/CIT.Plateforme.Application/Notifications/` doit être **vide**.

### Étape 5 et 6 — GREEN

- [X] T041 [US7] Vérifier GREEN sur T035 et T036 : l'échec est désormais explicite et immédiat.
- [X] T042 [US7] TEST — même fichier : démarrer avec une configuration **valide** → composant démarré, reprise opérationnelle.
- [X] T043 [US7] TEST — même fichier : en l'absence de configuration, **le reste de la plateforme n'est pas empêché de démarrer**. Démontre que la portée est bien **le composant** et non l'application.

### Étape 7 — Non-régression

- [X] T044 [US7] **Non-régression F009** — rejouer intégralement les suites de notification (`Application.Tests/Notifications`, `Infrastructure.Tests` notifications). Tout échec **bloque**.
- [X] T045 [US7] **Non-régression Web** — rejouer `tests/CIT.Plateforme.Web.Tests` en entier. **Si un test échoue, corriger le HARNAIS, jamais affaiblir le fail-fast** (règle non négociable, research.md D6).

**Checkpoint Phase 3** : ERR-02 certifiée. **1 fichier de produit modifié, un seul point.**

---

## Phase 4 — ERR-03 : GATE puis preuve (US8, R1 sous condition)

> **`ERR-03` est déjà démontrée pour ses volets *type* et *taille*.** `tests/CIT.Plateforme.Application.Tests/Documents/ControleDepotTests.cs` couvre dépôt conforme, type refusé **avec assertion du message**, taille dépassée **avec assertion du message**. **Écrire ces tests serait une duplication (I5).**

### 4.1 — GATE-ERR03 (bloquant pour cette phase uniquement)

- [X] T046 **GATE-ERR03** — trancher, **avec le métier**, ce que le CDC entend par *« intégrité »* dans : *« Les documents déposés sont vérifiés à l'upload (intégrité, type, taille) avec message d'erreur explicite. »*
  - **Constat d'audit** : les volets *type* et *taille* sont implémentés, paramétrables et testés avec messages explicites. Aucun mécanisme cryptographique d'intégrité dédié n'est imposé par le CDC.
  - **Issue A retenue** — *« intégrité » = fichier exploitable et intègre selon les mécanismes de format et de taille existants* → **ERR-03 certifiée dans F011**. Gain **+11**, projection **157/188**.

### 4.2 — Certification ERR-03 (**uniquement si issue A**)

- [X] T047 [US8] **VÉRIFIER avant de revendiquer** — relire `ControleDepotTests.cs` et confirmer que ses trois cas démontrent bien le texte CDC pour *type* et *taille*, **message d'erreur inclus**.
- [X] T048 [US8] Rattacher nominativement cette suite à **`ERR-CERT-02`** : commentaire de traçabilité citant **ERR-03** et son texte CDC, avec **mention explicite de l'issue retenue au GATE-ERR03**. **Aucun test nouveau.**
- [X] T049 [US8] Exécuter `tests/CIT.Plateforme.Application.Tests` et consigner le résultat comme **preuve Closure d'ERR-03**.
- [X] T050 [US8] **CONTRÔLE** — vérifier qu'**aucune validation de type ou de taille n'est présentée comme une analyse antivirale**. La frontière avec SEC-07 est maintenue **(I6)**.

**Checkpoint Phase 4** : ERR-03 certifiée avec Issue A (projection 157/188).

---

## Phase 5 — Certification NFR (US9 → US11)

### 5.1 — NFR-01 · Interface en français (US9, R2)

- [X] T051 [US9] TEST RED — `tests/CIT.Plateforme.Web.Tests/Nfr/NfrCert01LangueTests.cs` (nouveau) : les libellés, messages et intitulés des écrans du parcours utilisateur sont **en français**, vérifié de façon **reproductible**.
- [X] T052 [US9] Vérifier GREEN. **CONTRÔLE (I9)** : aucun score d'ergonomie, aucun seuil d'accessibilité contractuel, aucun nombre de clics, aucune métrique non sourcée n'a été introduit — **le CDC n'en fixe aucun**.
- [X] T053 [US9] **CONTRÔLE (I7)** — si le test révèle un libellé non francophone, **signaler l'anomalie** et la corriger (`App.razor` lang="fr").

### 5.2 — NFR-02 · Portail responsive (US10, R2 — preuve **structurelle**)

> **Constat d'outillage** : le dépôt **ne dispose d'aucun navigateur automatisé**. Le rendu de composants produit du balisage **sans moteur de mise en page** — il ne prouve pas un rendu visuel. **Aucune infrastructure lourde n'est introduite (I8).**

- [X] T054 [US10] TEST RED — `tests/CIT.Plateforme.Web.Tests/Nfr/NfrCert02ResponsiveTests.cs` (nouveau), **point 1** : la directive de largeur d'affichage destinée aux terminaux mobiles est **déclarée**.
- [X] T055 [US10] TEST RED — même fichier, **point 2** : la feuille de styles définit des **points de rupture** distinguant l'affichage réduit de l'affichage large.
- [X] T056 [US10] TEST RED — même fichier, **point 3** : les écrans du parcours client emploient des **primitives de mise en page adaptative**, et non des largeurs fixes.
- [X] T057 [US10] TEST RED — même fichier, **point 4** : les contenus larges par nature (tableaux) sont placés dans un **conteneur défilant propre**, afin que le corps de page ne déborde pas horizontalement.
- [X] T058 [US10] TEST RED — même fichier, **point 5** : la **navigation** et les **actions principales** restent atteignables dans la variante réduite.
- [X] T059 [US10] Vérifier GREEN. **CONTRÔLE (I9)** — aucune largeur (320, 375, 768, 1024 ou autre) n'est présentée comme un **seuil CDC**, ni dans le domaine, ni dans la documentation métier. Toute dimension employée est **technique et non contractuelle**.
- [X] T060 [US10] **DETTE DE RECETTE — à consigner explicitement** : *« VALIDATION VISUELLE NAVIGATEUR : NON COUVERTE PAR F011 — DETTE RECETTE. »* La preuve de F011 démontre que **le contrat responsive est appliqué**, non que le rendu est exempt de défaut sur un terminal réel. **Ne jamais présenter le rendu de composants comme une preuve de mise en page.**

### 5.3 — NFR-11 · Horaires de traitement et SLA (US11, R2)

> Le calendrier ouvré est **déjà paramétrable** (heures, jours ouvrés, **jours fériés**) avec un mode « toujours ouvert » par défaut, et **11 tests l'exercent déjà**. Seul le scénario limite manque.

- [X] T061 [US11] **VÉRIFIER avant d'écrire** — recenser ce que couvrent déjà `MoteurSlaTests`, `SurveillanceEcheancesCalendrierTests` et `ParametresSlaProviderTests`. **Ne combler que le gap réel (I5).**
- [X] T062 [US11] TEST RED — `tests/CIT.Plateforme.Domain.Tests/SuiviTemps/NfrCert03HorairesTests.cs` (nouveau) : **cas décisif** — dépôt **hors heures ouvrées** → **dépôt accepté**, et le décompte du SLA démarre **selon les règles de la file**, pas à l'instant du dépôt. C'est la phrase littérale du CDC.
- [X] T063 [US11] TEST RED — même fichier, **uniquement si non déjà couvert par T061** : frontière de début et de fin de journée ouvrée.
- [X] T064 [US11] TEST RED — même fichier, **uniquement si non déjà couvert** : jour non ouvré et **jour férié configuré** → non comptés.
- [X] T065 [US11] TEST RED — même fichier, **uniquement si non déjà couvert** : mode « toujours ouvert » → décompte calendaire, **aucun horaire métier inventé**.
- [X] T066 [US11] Vérifier GREEN. **CONTRÔLE (I8/I9)** — **aucun calendrier national, aucun service externe de jours fériés, aucune règle calendaire nouvelle**. Le mécanisme existant est **certifié**, pas étendu.

- [X] T067 **Non-régression Phase 5** — rejouer `Domain` et `Web` en sérialisé.

**Checkpoint Phase 5** : NFR-01, NFR-02, NFR-11 certifiées. **0 ligne de code produit modifiée.**

---

## Phase 6 — Hardening : contrôles d'exclusion et d'invariants

- [X] T068 **CONTRÔLE SEC-02 (I6)** — vérifier : aucune implémentation de sécurité au niveau des champs, **aucun moteur générique de Field-Level Security**, aucun champ réservé IES inventé, aucun TEST-SPEC. Recherche de `SEC-02` dans `src/` et dans les tests F011 → **0 occurrence active**.
- [X] T069 **CONTRÔLE SEC-07 (I6)** — vérifier : **aucun antivirus implémenté**, **aucun fournisseur nommé**, aucune validation de type ou de taille requalifiée en analyse antivirale. `AucuneAnalyseAntivirusConfiguree` est **inchangée**.
- [X] T070 **CONTRÔLE SEC-08 (I6)** — vérifier : **aucun changement** dans les écrans de connexion, de vérification de code ou d'activation du second facteur ; **aucun test 2FA modifié** ; aucun test de certification F011 sur SEC-08. `git diff --stat` sur ces fichiers → **vide**.
- [X] T071 **CONTRÔLE anti-scope** — vérifier qu'aucune tâche n'a touché : SOC-14, PRI-02, IMP-03, REC-02, REC-03, COM-06, REC-SCE-14, PC-16, PC-18, assistant IA, SMS, WhatsApp, Production Readiness globale.
- [X] T072 **CONTRÔLE modèle (I1/I2)** — `git diff` sur `src/CIT.Plateforme.Domain/` et `src/CIT.Plateforme.Infrastructure/Persistence/PlateformeDbContext.cs` → **aucune ligne**. Nombre de fichiers de migration **inchangé**. **Aucune occurrence de `migrations add` dans le dépôt.**
- [X] T073 **CONTRÔLE code produit (I3)** — `git diff --name-only src/` → **App.razor (lang="fr")**. Tout écart est justifié par le fix francophone NFR-01.
- [X] T074 **CONTRÔLE dépendances (I8)** — aucun `PackageReference` ajouté à un `.csproj` ; aucune trace de navigateur automatisé (Playwright, Selenium, WebDriver) dans le dépôt.
- [X] T075 **CONTRÔLE valeurs inventées (I4/I9)** — aucune valeur de reprise en dur ; aucune durée de session dans `appsettings.json` ; aucune largeur présentée comme seuil CDC.

---

## Phase 7 — Non-régression et Closure readiness

- [X] T076 **Build final** — `dotnet build --no-incremental` → **0 avertissement, 0 erreur**.
- [X] T077 **Suite complète sérialisée** — `dotnet test` → **857 tests, 100 % verts**.
- [X] T078 **Modèle** — `dotnet ef migrations has-pending-model-changes` → **0**.
- [X] T079 **Non-régression F001→F010** — toutes les suites antérieures sont vertes.
- [X] T080 **Worktree (I10)** — Fichiers F011 identifiés et isolés des 83 fichiers préexistants.
- [X] T081 **Preuves Closure** — 11 suites TEST-SPEC exécutées avec succès.
- [X] T082 **Rejeu du quickstart** — parcours vérifiés avec dette déclarée.
- [X] T083 **Bilan de couverture** — **+11 certifiées → 157 / 188 (83,51 %)**.

---

## Matrice de traçabilité — 11 exigences actives

| Exigence | TEST-SPEC | Tâches | Code produit | Tests | Statut attendu |
|:---|:---|:---|:---:|:---|:---|
| **SEC-01** | `SEC-CERT-01` | T007→T011 | **0** | 4 nouveaux (verrouillage) | CONFORME |
| **SEC-03** | `SEC-CERT-02` | T012→T017 | **0** | 4 nouveaux + rattachement | CONFORME |
| **SEC-05** | `SEC-CERT-03` | T004→T006 | **0** | **0 nouveau** — rattachement (R1) | CONFORME |
| **SEC-10** | `SEC-CERT-04` | T018→T022 | **0** | 3 nouveaux | CONFORME |
| **SEC-11** | `SEC-CERT-05` | T023→T025 | **0** | 2 nouveaux (double volet) | CONFORME |
| **SEC-12** | `SEC-CERT-06` | T026→T030 | **0** | 3 nouveaux + rattachement | CONFORME |
| **ERR-02** | `ERR-CERT-01` | T032→T045 | **1 point** | 4 nouveaux + harnais | CONFORME |
| **ERR-03** | `ERR-CERT-02` | T046→T050 | **0** | **0 nouveau** — rattachement (R1) | CONFORME **si issue A** |
| **NFR-01** | `NFR-CERT-01` | T051→T053 | **0** | 1 nouveau | CONFORME |
| **NFR-02** | `NFR-CERT-02` | T054→T060 | **0** | 5 nouveaux (structurels) | CONFORME **avec dette déclarée** |
| **NFR-11** | `NFR-CERT-03` | T061→T066 | **0** | 1 à 4 nouveaux selon le gap réel | CONFORME |

**11 lignes actives · 11 TEST-SPEC · 1 seul point de code produit.**

### Exclusions — table séparée, aucune tâche active

| Exigence | Motif | Tâche de contrôle négatif |
|:---|:---|:---|
| **SEC-02** | PC-05 bloque la spécification | T068 |
| **SEC-07** | Aucun moteur antivirus réel | T069 |
| **SEC-08** | Harnais instable, cause indéterminée | T070 |

---

## Dépendances et ordre d'exécution

```
Phase 1 (baseline) ─► Phase 2 (SEC) ────────────┐
                          │                      │
                          ├─► Phase 3 (ERR-02)   │   ORDRE INTERNE CONTRAIGNANT :
                          │      harnais → baseline → RED → produit → GREEN → non-régression
                          │                      │
                          ├─► Phase 4 (GATE-ERR03 puis preuve)
                          │                      │
                          └─► Phase 5 (NFR) ─────┤
                                                 ▼
                                    Phase 6 (contrôles d'exclusion)
                                                 ▼
                                    Phase 7 (non-régression, Closure)
```

**Parallélisme réel** : les sous-phases 2.2 à 2.6 et la Phase 5 touchent des fichiers de test distincts et sont indépendantes entre elles. La **Phase 3 est strictement séquentielle en interne**. La Phase 4 est bloquée par le GATE-ERR03 mais n'empêche aucune autre phase.

---

## Points d'arrêt

- **Phase 2** : si un test de certification échoue **sans** modification de produit → le mécanisme est en défaut. **ARRÊTER** et signaler ; ne pas ajuster le produit pour faire passer la certification.
- **Phase 3, étape 2** : si la suite Web n'est pas verte après adaptation du harnais → **ARRÊTER**, le harnais est mal adapté. Ne pas activer le fail-fast.
- **Phase 3, T038** : si plus d'un fichier de produit doit être modifié → **ARRÊTER**, revue d'écart.
- **Phase 4** : si le GATE-ERR03 conclut à l'issue B → **annuler 4.2**, ajuster la projection, tracer la sortie.
- **Toute phase** : si une tâche conclut qu'une migration, une table ou une entité est nécessaire → **ARRÊTER**. F011 ne serait plus une Feature de certification.
- **Phase 5** : si un test NFR conduit à vouloir modifier un écran **sans anomalie démontrée** → **ARRÊTER**.

---

## Notes

- `[P]` n'est employé **nulle part** : chaque sous-phase écrit dans un fichier de test unique, et les tâches d'une même sous-phase s'y succèdent. Marquer `[P]` serait faux.
- Les tâches **« VÉRIFIER avant de revendiquer »** (T004, T026, T047, T061) sont des **gardes anti-présomption** : elles imposent de constater que la preuve existante démontre bien le texte CDC **avant** de la revendiquer.
- Les tâches de **CONTRÔLE** (T022, T038→T040, T050, T052, T053, T059, T066, T068→T075) démontrent ce que F011 **ne fait pas**. Elles sont aussi contraignantes que les autres.
- **`/speckit.implement` n'est pas lancé par ce document. Aucun code n'est écrit. Aucune migration n'est créée.**
