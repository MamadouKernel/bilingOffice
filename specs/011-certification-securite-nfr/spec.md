# Spécification Fonctionnelle — Feature 011 (Certification Sécurité & Socle Non-Fonctionnel)

> **Document** : `specs/011-certification-securite-nfr/spec.md`
> **Feature** : F011 — Certification Sécurité & Socle Non-Fonctionnel
> **Date** : 1er septembre 2026
> **Statut** : DRAFT (préparé pour `/speckit.clarify`, non lancé par ce document)
> **Périmètre** : **FIGÉ à 11 exigences** par le Final Scope Gate (`docs/18`, addendum)
> **Baseline CDC** : **146 / 188 (77,66 %)** — source de vérité `docs/17-baseline-nominative-188-exigences.md`

---

## 1. Objectif et nature de la Feature

F011 est une **Feature de certification**, pas une Feature de construction.

Elle vise **11 exigences CDC déjà servies par le socle F001→F010** mais **jamais comptabilisées comme CONFORMES**, parce qu'elles ont toujours été traitées comme des « contraintes transverses appliquées partout » plutôt que comme des livrables rattachés à une Feature. `docs/17` a établi ce constat par comptage nominatif.

**Conséquence directrice** : F011 **ne réimplémente aucun mécanisme existant**. Elle produit **la preuve** que le comportement exigé par le CDC est effectivement tenu, et rattache nominativement chaque exigence à cette preuve.

**Une seule correction de produit est anticipée** — ERR-02, §7.

---

## 1 bis. Clarifications

### Session 2026-09-01

- Q: SEC-10 — le CDC fixe-t-il une durée d'inactivité contractuelle ? → A: **NON.** Le CDC exige seulement que la période soit *« paramétrable »*. **Aucune durée n'est contractuelle.** Constat vérifié : le mécanisme lit bien une clé de configuration, mais **cette clé est absente des fichiers de configuration** ; le code retombe sur un **défaut technique de 30 minutes**, documenté en commentaire comme provisoire *« tant que le paramétrage métier n'a pas été administré »*. Ce défaut est **technique, non contractuel** ; il n'est ni durci ni promu en valeur métier par F011.
- Q: SEC-10 — comment certifier l'expiration sans attendre réellement la durée configurée ? → A: **Preuve en deux volets, sans horloge réelle.** (1) *Paramétrabilité* : une valeur de configuration fournie au démarrage est **effectivement appliquée** à la politique de session. (2) *Effet* : une session dont la validité est dépassée se voit **refuser l'accès** à une ressource protégée. La durée employée par les tests est une **valeur technique de démonstration, explicitement non contractuelle**. **Aucune durée métier n'est introduite dans le produit pour les besoins du test.**
- Q: ERR-02 — le fail-fast doit-il empêcher le démarrage de l'application entière, ou seulement du composant concerné ? → A: **Option B — le composant concerné.** Seul le mécanisme de reprise dépend de cette configuration ; le reste de la plateforme (tickets, workflow, centre de notifications) fonctionne sans elle. Faire échouer l'application entière serait un durcissement **non soutenu par le CDC**. L'exigence tenue est : *l'absence de configuration doit produire une erreur explicite et empêcher le composant de démarrer silencieusement inopérant* — jamais une dégradation muette.
- Q: ERR-02 — le fail-fast résout-il CLAR-NOT-02 ? → A: **NON, et il ne doit pas.** Le nombre de tentatives, la cadence et la condition d'arrêt restent des **décisions métier/DSI ouvertes**. F011 certifie uniquement que *l'absence ou l'invalidité de la configuration est signalée explicitement*. **Aucune valeur par défaut de production n'est ajoutée.**
- Q: ERR-03 — le volet « intégrité » du CDC dispose-t-il d'un mécanisme distinct ? → A: **NON.** Vérification faite : les volets **type** et **taille** sont implémentés avec messages explicites ; **aucun mécanisme d'intégrité dédié n'existe** (ni empreinte, ni somme de contrôle). Le seul contrôle de contenu existant est le point d'ancrage antivirus, qui relève de **SEC-07 — exclue**. **Réserve portée** : F011 certifie les volets *type* et *taille* ; si le métier exige un contrôle d'intégrité distinct, celui-ci constitue un **développement nouveau** et **ERR-03 sort du périmètre** (cf. §8).
- Q: NFR-02 — quel niveau de preuve est réellement atteignable pour le responsive ? → A: **Preuve structurelle (option A+B), pas visuelle.** Constat vérifié : le dépôt **ne dispose d'aucun navigateur automatisé** (ni Playwright, ni Selenium) ; l'outillage se limite à bUnit et au harnais d'hôte applicatif. **bUnit ne prouve pas une mise en page** — il rend du balisage sans moteur de rendu. **Aucune infrastructure lourde n'est introduite.** La preuve retenue est donc structurelle (§9) et sa **limite est déclarée**.
- Q: NFR-02 — des largeurs peuvent-elles être citées comme seuils ? → A: **NON.** Le CDC ne mentionne **aucune largeur de rupture**. Toute dimension employée dans un test est une **valeur technique de démonstration, explicitement non contractuelle**. Il est **interdit** de présenter 320, 375, 768 ou 1024 pixels comme un seuil CDC.
- Q: NFR-11 — le calendrier doit-il être étendu (jours fériés nationaux, source externe) ? → A: **NON.** Le calendrier existant porte déjà **heures d'ouverture, jours ouvrés et jours fériés**, tous **paramétrables**, avec un mode « toujours ouvert » par défaut tant qu'aucun paramétrage n'a été saisi. **Aucun calendrier national ni service externe de jours fériés n'est introduit** — le CDC n'en exige aucun. F011 **certifie** le mécanisme existant ; il ne l'étend pas.

---

## 2. Périmètre contractuel — 11 exigences

Textes repris **littéralement** de `docs/cdc_text.txt` (source contractuelle ultime). Lorsque `docs/01-matrice-exigences.md` diverge, le CDC prévaut.

### SEC-01 (Must) — Authentification et robustesse des accès
> *« Authentification obligatoire pour tout accès ; politique de mots de passe robuste ; verrouillage après échecs répétés. »*

### SEC-03 (Must) — Cloisonnement strict des données
> *« Cloisonnement strict des données : un client n'accède qu'à ses tickets ; un agent n'accède qu'aux files et actions de son rôle. »*

### SEC-05 (Must) — Journal d'audit non modifiable
> *« Journal d'audit complet et non modifiable des actions utilisateurs et administrateurs. »*

### SEC-10 (Must) — Expiration de session
> *« Les sessions utilisateurs expirent après une période d'inactivité paramétrable. »*

### SEC-11 (Must) — Désactivation immédiate sans perte d'historique
> *« Les comptes agents peuvent être désactivés immédiatement, sans suppression de leur historique. »*

### SEC-12 (Must) — Lien de notification jamais un accès direct
> *« Aucun lien transmis par e-mail, SMS ou WhatsApp ne donne accès direct à un document protégé sans contrôle préalable de l'identité et des habilitations : le lien conduit au ticket après authentification. »*

### ERR-02 (Must) — Reprise d'envoi de notification
> *« Échec d'envoi d'une notification : nouvelle tentative automatique et traçage ; l'échec d'un canal n'empêche pas les autres. »*

### ERR-03 (Must) — Vérification des documents à l'upload
> *« Les documents déposés sont vérifiés à l'upload (intégrité, type, taille) avec message d'erreur explicite. »*

### NFR-01 (Must) — Interface française et ergonomie simple
> *« Interface en français ; ergonomie simple adaptée à des utilisateurs non techniciens. »*

### NFR-02 (Must) — Portail responsive
> *« Portail client responsive, utilisable sur mobile (une part importante des clients et chauffeurs utilisera un smartphone). »*

### NFR-11 (Must) — Horaires de traitement et SLA
> *« Horaires de traitement : les services CIT conservent leurs horaires de fonctionnement propres, utilisés notamment dans le calcul des SLA. Un client peut déposer un dossier hors heures ouvrées ; le SLA de traitement démarre selon les règles de la file concernée. »*

**TOTAL : 11 exigences — 11 Must, 0 Should, 0 Could.**

---

## 3. Hors périmètre — exclusions du Final Scope Gate

Ces trois exigences **n'apparaissent dans aucune User Story, aucun critère d'acceptation, aucune suite TEST-SPEC, et ne comptent pour aucun gain de couverture**. Elles figurent ici uniquement au titre des travaux futurs.

| Exigence | Motif d'exclusion | Suite attendue |
|:---|:---|:---|
| **SEC-02** | Le texte CDC intégral est *« RBAC […] y compris au niveau des champs **pour les traitements parallèles** »* — il désigne la branche IES, où Pilote Manifeste et Pilote Contrôle Documentaire interviennent chacun sur des champs réservés. F003 a livré la séparation **par branche** (testée) ; **PC-05** (🟡, *« Bloque : SPECIFICATION »*, source `IMP-40 · SEC-02`) constate que les règles champ par champ **ne sont pas détaillées** par le CDC. | Atelier métier Pilotes Manifeste / CD, puis Feature de remédiation. **Aucun champ réservé n'est inventé ici. Aucun moteur générique de Field-Level Security n'est créé.** |
| **SEC-07** | Le CDC exige trois volets : types autorisés ✅, taille maximale ✅, **analyse antivirale ❌**. L'unique implémentation enregistrée retourne `Sain` **sans lire le flux**. Une abstraction n'est pas une analyse antivirale. | Décision de production (moteur, fournisseur, coût). **Aucun antivirus n'est implémenté ici, aucun fournisseur choisi, aucune validation MIME requalifiée en antivirus.** |
| **SEC-08** | Chaque scénario 2FA passe **0 échec / 3 en isolation** ; la classe complète échoue **1 / 3 en exécution sérialisée**. Contamination d'état inter-tests démontrée ; mécanisme exact **non déterminé**. **Aucun défaut produit n'est démontré.** | Diagnostic du harnais de test. **L'authentification et le 2FA ne sont pas modifiés par F011.** |

**Également hors périmètre** : NFR-03, NFR-04, NFR-05, NFR-06, NFR-07, NFR-08, NFR-09, NFR-10, SEC-04, SEC-06, SEC-09, `IA-*`, ainsi que les dettes ponctuelles SOC-14, PRI-02, IMP-03, REC-02, REC-03, COM-06 (Feature de remédiation ultérieure).

---

## 4. Principe de non-duplication

F011 s'interdit formellement :

- de **réimplémenter** un mécanisme de sécurité déjà centralisé (`IAutorisationService`, `CloisonnementService`, `JournalAuditAppendOnlyGuard`, contrôles de dépôt documentaire) ;
- de **dupliquer** une logique métier livrée par F001→F010 ;
- de **créer** une table, une entité de domaine ou une migration ;
- d'**ajouter** une fonctionnalité métier ou un écran ;
- d'**absorber** du périmètre AVE (F007), CON (F010), Notifications (F009) ou Finance.

> **Règle d'arrêt** : si la conception conclut qu'une migration, une table ou une entité nouvelle est nécessaire, **l'implémentation s'arrête** et l'écart est rapporté — ce serait le signe que le périmètre a dérivé de la certification vers la construction.

---

## 5. Matrice de traçabilité — 11 exigences

| Exigence | Mécanisme actuel (F001→F010) | Preuve actuelle | Preuve manquante | TEST-SPEC | Statut attendu |
|:---|:---|:---|:---|:---|:---|
| **SEC-01** | Politique de mot de passe et verrouillage configurés au niveau du fournisseur d'identité (longueur minimale, plafond d'échecs, durée de verrouillage) | 4 tests nomment SEC-01 | Scénario **négatif** : échecs répétés → compte verrouillé ; puis accès refusé même avec le bon mot de passe | `SEC-CERT-01` | CONFORME |
| **SEC-03** | `CloisonnementService` + filtres serveur appliqués dans la requête ; habilitations par file | Massivement testé **sous l'angle CPT-04** ; **0 test ne nomme SEC-03** | Rattachement **nominatif** ; scénario « agent hors de sa file » ; tentative d'accès direct par identifiant connu | `SEC-CERT-02` | CONFORME |
| **SEC-05** | Journal append-only : garde applicative rejetant toute modification ou suppression avant enregistrement | 2 tests nomment SEC-05 | Scénario **négatif** : tentative de modification **et** de suppression d'une entrée → rejet, journal intact | `SEC-CERT-03` | CONFORME |
| **SEC-10** | Expiration de session pilotée par un paramètre de configuration, avec glissement sur activité | **0 test** | Preuve que la période est **effectivement paramétrable** et **appliquée** : session expirée → accès refusé | `SEC-CERT-04` | CONFORME |
| **SEC-11** | Indicateur d'activité sur le compte, consulté à l'authentification | 1 test nomme SEC-11 | Scénario **double** : compte désactivé → accès refusé **ET** historique/audit de ce compte **intégralement conservé** | `SEC-CERT-05` | CONFORME |
| **SEC-12** | Lien de notification portant la référence du ticket ; lien **relatif**, **aucun jeton** ; accès arbitré par les contrôles existants | 2 tests | Certification **nominative** ; scénario « lien ouvert sans authentification » ; scénario « lien vers ticket inaccessible » | `SEC-CERT-06` | CONFORME |
| **ERR-02** | Moteur de reprise F009 : statut par canal, nouvelle tentative, traçage, isolation inter-canaux | 6 tests (TEST-ERR-02) | **Fail-fast de configuration au démarrage** (§7) + scénario de démarrage sans configuration | `ERR-CERT-01` | CONFORME **après correction** |
| **ERR-03** | Contrôles de type, de taille et d'intégrité à l'upload, appliqués par la commande de dépôt | Indirect (F003/F004/F006/F008) ; **0 test ne nomme ERR-03** | Scénarios **négatifs** par volet (type refusé, taille dépassée) avec **message d'erreur explicite** vérifié | `ERR-CERT-02` | CONFORME |
| **NFR-01** | Interface intégralement francophone | Indirect (`docs/06-audit-ui-ux`) | Preuve **reproductible** : absence de libellé non francophone dans les écrans exposés à l'utilisateur | `NFR-CERT-01` | CONFORME |
| **NFR-02** | Portail responsive : viewport déclaré, points de rupture définis, classes responsives largement employées | Indirect | Preuve **reproductible** : rendu utilisable en largeur mobile, navigation et actions principales accessibles | `NFR-CERT-02` | CONFORME |
| **NFR-11** | Calendrier ouvré du service, consommé par le calcul des délais et des seuils SLA | Indirect | Scénario **limite** : dépôt hors heures ouvrées → le SLA démarre selon les règles de la file, pas à l'instant du dépôt | `NFR-CERT-03` | CONFORME |

**Total : 11 exigences, aucun double comptage, aucune exigence hors périmètre.**

---

## 6. Fiches de certification — SEC

Chaque fiche définit **acteur**, **règle protégée**, **comportement attendu**, **comportement interdit**, **contrôle serveur**, **scénario positif**, **scénario négatif**.

> **Principe transverse opposable aux six fiches** : une protection présente **uniquement dans l'interface est insuffisante**. Toute règle doit être tenue **côté serveur**, indépendamment de ce que l'interface affiche ou masque.

### SEC-01 — Authentification et robustesse des accès

| Dimension | Contenu |
|:---|:---|
| **Acteur** | Tout utilisateur (client, agent, administrateur) |
| **Règle protégée** | Aucun accès sans authentification ; mots de passe robustes ; résistance au forçage par essais répétés |
| **Comportement attendu** | Toute ressource protégée exige une authentification préalable. Un mot de passe ne respectant pas la politique est refusé à la création. Après un nombre paramétré d'échecs consécutifs, le compte est verrouillé pour une durée paramétrée |
| **Comportement interdit** | Accéder à une ressource protégée sans session valide ; enchaîner un nombre illimité de tentatives de mot de passe |
| **Contrôle serveur** | Refus émis par le serveur, jamais par un masquage d'interface |
| **Scénario positif** | Identifiants valides → accès accordé |
| **Scénario négatif** | Échecs répétés au-delà du plafond → compte verrouillé ; une tentative ultérieure **avec le bon mot de passe** est **refusée** tant que le verrouillage court |

### SEC-03 — Cloisonnement strict des données

| Dimension | Contenu |
|:---|:---|
| **Acteur** | Client · Agent |
| **Règle protégée** | Un client n'accède qu'à **ses** tickets ; un agent n'accède qu'aux **files et actions de son rôle** |
| **Comportement attendu** | Le périmètre de données est déterminé par le contexte authentifié et appliqué **dans la requête**, jamais après chargement |
| **Comportement interdit** | Accéder au ticket d'un autre client, y compris en connaissant son identifiant ; agir sur une file pour laquelle l'agent n'est pas habilité |
| **Contrôle serveur** | Le refus ne divulgue **ni l'existence, ni le contenu, ni l'appartenance** de la ressource |
| **Scénario positif** | Un client consulte son ticket · un agent agit dans sa file |
| **Scénario négatif** | Client A demande le ticket de client B **par identifiant direct** → refusé · Agent hors file tente une action → refusé côté serveur |

### SEC-05 — Journal d'audit non modifiable

| Dimension | Contenu |
|:---|:---|
| **Acteur** | Utilisateurs et administrateurs (tracés) ; tout composant tentant une écriture |
| **Règle protégée** | Le journal est **complet** et **non modifiable** |
| **Comportement attendu** | Les actions des utilisateurs **et des administrateurs** produisent une entrée. Le journal n'accepte que l'ajout |
| **Comportement interdit** | Modifier une entrée existante ; supprimer une entrée ; enregistrer une action d'administration sans trace |
| **Contrôle serveur** | Toute tentative de modification ou de suppression est **rejetée avant enregistrement** |
| **Scénario positif** | Une action d'administration produit une entrée horodatée avec son auteur |
| **Scénario négatif** | Tentative de **modification** → rejetée · tentative de **suppression** → rejetée · le journal reste **intact** après les deux tentatives |

### SEC-10 — Expiration de session

| Dimension | Contenu |
|:---|:---|
| **Acteur** | Tout utilisateur authentifié |
| **Règle protégée** | Une session inactive **expire** ; la période est **paramétrable** |
| **Comportement attendu** | Après la période d'inactivité configurée, la session cesse d'être valide et l'accès aux ressources protégées est refusé |
| **Comportement interdit** | Session perpétuelle ; période figée en dur, non paramétrable |
| **Contrôle serveur** | L'expiration est arbitrée côté serveur, jamais par une minuterie d'interface |
| **Scénario positif** | Une session active dans la période reste valide |
| **Scénario négatif** | Session inactive au-delà de la période → accès refusé, ré-authentification exigée |

> **Réserve de spécification (clarifiée)** : le CDC exige que la période soit *« paramétrable »* mais **n'en fixe aucune valeur**. **Aucune durée n'est imposée par cette spécification** ; la valeur d'exploitation reste une décision métier/DSI.
>
> **Constat d'état vérifié** : le mécanisme lit bien une clé de configuration, mais **cette clé est absente des fichiers de configuration** ; le code retombe sur un **défaut technique de 30 minutes**, documenté comme provisoire dans le code lui-même. Ce défaut est **technique et non contractuel**. F011 ne le durcit pas, ne le promeut pas en valeur métier, et ne le supprime pas.
>
> **Stratégie de preuve (clarifiée)** — l'expiration se certifie **sans attendre la durée réelle**, en deux volets :
> 1. **Paramétrabilité** : une valeur fournie au démarrage est **effectivement appliquée** à la politique de session — ce qui démontre le mot *« paramétrable »* du CDC.
> 2. **Effet** : une session dont la validité est dépassée se voit **refuser l'accès** à une ressource protégée.
>
> La durée employée par les tests est une **valeur technique de démonstration, explicitement non contractuelle**. **Aucune durée métier n'est introduite dans le produit pour satisfaire un test.**

### SEC-11 — Désactivation immédiate sans perte d'historique

| Dimension | Contenu |
|:---|:---|
| **Acteur** | Administrateur (agit) · Agent (subit) |
| **Règle protégée** | Désactivation **immédiate** d'un compte agent, **sans suppression de son historique** |
| **Comportement attendu** | Dès la désactivation, le compte ne peut plus accéder à la plateforme. **Toutes ses traces antérieures demeurent** : historique de tickets, entrées d'audit, messages, attributions |
| **Comportement interdit** | Conserver un accès après désactivation ; supprimer, anonymiser ou détacher les traces du compte désactivé |
| **Contrôle serveur** | L'état d'activité est consulté à l'authentification, côté serveur |
| **Scénario positif** | Un compte actif accède normalement |
| **Scénario négatif** | Compte désactivé → accès refusé **immédiatement** ; **et** l'historique produit par ce compte reste **intégralement consultable** par les acteurs habilités |

### SEC-12 — Lien de notification jamais un accès direct

| Dimension | Contenu |
|:---|:---|
| **Acteur** | Destinataire d'une notification · tiers non authentifié |
| **Règle protégée** | Aucun lien transmis par e-mail, SMS ou WhatsApp ne donne un accès direct à un document protégé |
| **Comportement attendu** | Le lien **conduit au ticket**, après contrôle de l'identité **et** des habilitations |
| **Comportement interdit** | Lien pointant directement une pièce jointe protégée ; lien porteur d'un jeton contournant les habilitations ; exposition d'un détail du dossier avant authentification |
| **Contrôle serveur** | L'arbitrage repose sur les contrôles d'accès existants ; **aucun chemin d'accès parallèle n'est introduit** |
| **Scénario positif** | Le destinataire habilité active le lien → accède au ticket |
| **Scénario négatif** | Un tiers **non authentifié** ouvre le lien → conduit au contrôle d'identité, **aucun détail exposé** · destinataire dont l'habilitation a été retirée → refus **sans divulguer** l'existence du ticket |

---

## 7. ERR-02 — seule correction de produit anticipée

### Écart constaté

Le CDC exige *« nouvelle tentative automatique et traçage ; l'échec d'un canal n'empêche pas les autres »*. Ces trois obligations **sont tenues** et démontrées par 6 tests.

L'écart porte sur un point distinct : **la garantie que le mécanisme fonctionne effectivement en exploitation**. La validation des paramètres de reprise est aujourd'hui **paresseuse** — elle ne s'exécute qu'au premier passage du traitement de fond, où l'exception est capturée et journalisée. En l'absence de configuration :

- l'application **démarre normalement** ;
- une erreur est journalisée périodiquement ;
- **aucune reprise n'a jamais lieu** ;
- les notifications internes continuent d'être créées, de sorte que **le système paraît fonctionner alors qu'une exigence Must est silencieusement inopérante**.

### Comportement cible — frontière clarifiée

**Décision : option B — le composant concerné, pas l'application entière.**

Seul le mécanisme de reprise dépend de cette configuration. Le reste de la plateforme — tickets, workflow, centre de notifications in-app — fonctionne sans elle. **Faire échouer l'application entière serait un durcissement non soutenu par le CDC** ; l'exigence tenue est que l'absence de configuration produise une **erreur explicite** et empêche le composant de démarrer **silencieusement inopérant**.

| Dimension | Contenu |
|:---|:---|
| **Acteur** | Exploitant déployant la plateforme |
| **Portée du refus** | **Le composant de reprise** — pas la plateforme entière |
| **Comportement attendu** | Au démarrage du composant, si les paramètres de reprise sont absents ou invalides, le démarrage de **ce composant** échoue avec un message **désignant le paramètre manquant** et **la raison** de son caractère obligatoire |
| **Comportement interdit** | Démarrer puis échouer silencieusement à chaque passage ; substituer une valeur par défaut ; dégrader le service sans le signaler ; **faire échouer l'application entière** |
| **Scénario positif** | Configuration valide fournie → composant démarré, reprise opérationnelle |
| **Scénario négatif** | Configuration absente → **échec explicite et immédiat du composant**, signalé à l'exploitant |

### Interdictions opposables

- **Aucune valeur de reprise en dur** — ni nombre de tentatives, ni cadence, ni condition d'arrêt.
- **Aucune valeur par défaut de production** destinée à contourner l'échec.
- **CLAR-NOT-02 n'est pas résolue implicitement** : les valeurs d'exploitation restent une décision métier/DSI ouverte.
- **Le moteur de notification F009 n'est pas modifié**, sauf nécessité démontrée.
- **Aucun fournisseur tiers** n'est introduit.

### Ordre d'exécution obligatoire (clarifié)

L'ordre suivant est **contraignant** ; l'inverser ferait tomber simultanément l'ensemble des tests qui démarrent l'hôte applicatif réel.

1. **Adapter le harnais** pour qu'il fournisse explicitement une configuration de reprise valide.
2. **Vérifier la baseline** — la suite complète doit rester verte avant toute modification de produit.
3. **Activer le fail-fast** dans le produit.
4. **Tester la configuration invalide ou absente** → échec explicite du composant.
5. **Tester la configuration valide** → composant démarré, reprise opérationnelle.

> **Règle non négociable** : **le comportement du produit ne doit jamais être modifié pour accommoder un harnais mal configuré.** Si un test échoue après l'étape 3, c'est le harnais qui est corrigé, pas le fail-fast qui est affaibli.

> **La spécification décrit le comportement attendu ; elle n'impose aucun mécanisme technique particulier.** Le choix du dispositif de validation au démarrage relève de `/speckit.plan`.

---

## 8. ERR-03 — vérification des documents à l'upload

| Dimension | Contenu |
|:---|:---|
| **Acteur** | Client · Agent déposant un document |
| **Règle protégée** | Trois volets exigés : **intégrité**, **type**, **taille** — avec **message d'erreur explicite** |
| **Comportement attendu** | Un document non conforme est refusé **avant** enregistrement, avec un message indiquant **quel critère** n'est pas satisfait |
| **Comportement interdit** | Accepter un fichier hors des types autorisés ; accepter un fichier au-delà de la taille maximale ; refuser sans indiquer le motif |
| **Contrôle serveur** | Les contrôles sont appliqués côté serveur, indépendamment de tout filtrage d'interface |
| **Scénario positif** | Document conforme → accepté et rattaché |
| **Scénario négatif** | Type non autorisé → refusé avec motif · taille dépassée → refusé avec motif |

> **Frontière avec SEC-07** : ERR-03 porte sur la **vérification à l'upload** et son **message d'erreur**. Le volet **analyse antivirale** relève de SEC-07, **exclue du périmètre** (§3). **Aucune validation de type ou de taille ne doit être présentée comme une analyse antivirale.**

### Réserve sur le volet « intégrité » (clarifiée)

Le CDC énumère **trois** volets : *intégrité, type, taille*. État vérifié :

| Volet | Mécanisme existant | Message explicite | Certifiable par F011 |
|:---|:---|:---|:---:|
| **Type** | Liste d'extensions autorisées, paramétrable par étape | ✅ *« Le type de fichier '.x' n'est pas autorisé… »* | ✅ |
| **Taille** | Taille maximale paramétrable par étape | ✅ *« Le fichier dépasse la taille maximale autorisée… »* | ✅ |
| **Intégrité** | **AUCUN mécanisme dédié** — ni empreinte, ni somme de contrôle | — | ⚠️ **réserve** |

Le seul contrôle portant sur le **contenu** du fichier est le point d'ancrage antivirus, qui relève de **SEC-07 — exclue du périmètre**.

**Position retenue** : F011 certifie les volets **type** et **taille**, qui sont implémentés, paramétrables et dotés de messages explicites conformes au CDC.

> **Condition de sortie** : si le métier établit que « intégrité » exige un contrôle distinct (empreinte, somme de contrôle, validation de format), celui-ci constitue un **développement nouveau** — donc hors nature de certification — et **ERR-03 sort du périmètre de F011**, ramenant le gain à **+10** et la projection à **156/188**. **Aucun mécanisme d'intégrité n'est inventé par cette spécification.**

---

## 9. NFR — certification sans seuil inventé

> **Constat de spécification** : NFR-01 et NFR-02 **ne portent aucun seuil chiffré** dans le CDC. Ce sont des exigences **qualitatives**. **Aucun seuil ergonomique, aucune métrique de performance et aucune largeur de rupture contractuelle ne sont inventés par cette spécification.** Les exigences comportant des seuils (NFR-03 disponibilité, NFR-04 temps de réponse, NFR-05 volumétrie) sont **hors périmètre**.

### NFR-01 — Interface en français, ergonomie simple

| Dimension | Contenu |
|:---|:---|
| **Exigence CDC** | *« Interface en français ; ergonomie simple adaptée à des utilisateurs non techniciens »* |
| **Comportement attendu** | Les écrans exposés à l'utilisateur présentent des libellés, messages et intitulés **en français** |
| **Critère démontrable** | Absence de libellé non francophone dans les écrans exposés à l'utilisateur, vérifiée de façon **reproductible** |
| **Non exigé** | Aucun score d'ergonomie, aucun référentiel externe, aucune métrique d'utilisabilité — le CDC n'en fixe aucun |

### NFR-02 — Portail responsive

| Dimension | Contenu |
|:---|:---|
| **Exigence CDC** | *« Portail client responsive, utilisable sur mobile »* |
| **Comportement attendu** | En largeur mobile, le portail reste **utilisable** : contenu lisible sans défilement horizontal du corps de page, navigation atteignable, actions principales accessibles |
| **Critère démontrable** | Application vérifiable et reproductible du contrat responsive sur les écrans du parcours client (voir « niveau de preuve » ci-dessous) |
| **Non exigé** | Aucune largeur de rupture contractuelle, aucun score de performance mobile — le CDC n'en fixe aucun |

#### Niveau de preuve retenu (clarifié) — preuve **structurelle**, limite déclarée

**Constat d'outillage vérifié** : le dépôt **ne dispose d'aucun navigateur automatisé** — ni Playwright, ni Selenium, ni pilote équivalent. L'outillage de test se limite au rendu de composants et au harnais d'hôte applicatif.

**Conséquence assumée** : le rendu de composants **ne prouve pas une mise en page** — il produit du balisage sans moteur de rendu ni calcul de dimensions. **Aucune infrastructure lourde n'est introduite** pour combler cet écart : ce serait disproportionné au regard d'une exigence que le CDC formule sans aucun seuil.

**Preuve retenue — structurelle, sur les écrans du parcours client** :
1. la **directive de largeur d'affichage** destinée aux terminaux mobiles est déclarée ;
2. la feuille de styles définit des **points de rupture** distinguant l'affichage réduit de l'affichage large ;
3. les écrans du parcours client emploient des **primitives de mise en page adaptative** plutôt que des largeurs fixes ;
4. les contenus larges par nature (tableaux) sont placés dans un **conteneur défilant propre**, afin que le corps de page ne déborde pas horizontalement ;
5. la **navigation** et les **actions principales** restent atteignables dans la variante réduite.

> **Limite déclarée, à porter en Closure Review** : cette preuve démontre que **le contrat responsive est appliqué**, non que le rendu visuel est exempt de défaut sur un terminal réel. Une validation visuelle sur navigateur reste une **dette de recette**, indépendante de la certification CDC.
>
> **Interdiction** : toute dimension employée dans un test est une **valeur technique de démonstration, explicitement non contractuelle**. Il est **interdit** de présenter 320, 375, 768 ou 1024 pixels — ou toute autre largeur — comme un seuil issu du CDC.

### NFR-11 — Horaires de traitement et SLA

| Dimension | Contenu |
|:---|:---|
| **Exigence CDC** | *« Les services CIT conservent leurs horaires de fonctionnement propres, utilisés notamment dans le calcul des SLA. Un client peut déposer un dossier hors heures ouvrées ; le SLA de traitement démarre selon les règles de la file concernée. »* |
| **Comportement attendu** | Deux propriétés distinctes : (1) le **dépôt hors heures ouvrées est possible** et n'est jamais refusé ; (2) le **décompte du SLA** suit les règles de la file concernée et le calendrier ouvré du service, et non l'horloge calendaire brute |
| **Comportement interdit** | Refuser un dépôt hors heures ouvrées ; décompter le SLA en heures calendaires lorsque la file est paramétrée en heures ouvrées |
| **Scénario positif** | Dépôt en heures ouvrées → décompte conforme aux règles de la file |
| **Scénario négatif / limite** | Dépôt **hors heures ouvrées** → dépôt accepté, et le décompte démarre selon les règles de la file, pas à l'instant du dépôt |
| **Non exigé** | Aucune nouvelle règle calendaire n'est introduite — le calendrier ouvré existant est **certifié**, pas étendu |

#### État vérifié du calendrier (clarifié)

Le calendrier existant est un objet de domaine pur, **entièrement paramétrable**, portant :

| Dimension | État |
|:---|:---|
| Heure d'ouverture / de fermeture | ✅ paramétrable |
| Jours ouvrés de la semaine | ✅ paramétrable |
| **Jours fériés** | ✅ **déjà modélisés et paramétrables** |
| Franchissement de journée | ✅ calcul jour par jour, bornes d'ouverture et de fermeture respectées |
| Défaut sans paramétrage | ✅ mode « toujours ouvert » — **aucun horaire métier inventé** |
| Tests existants | 11 tests portant sur le calendrier et le moteur SLA |

**Aucun calendrier national ni service externe de jours fériés n'est introduit** : le CDC n'en exige aucun, et le mécanisme de jours fériés existe déjà sous forme paramétrable. F011 **certifie** ce mécanisme ; il ne l'étend pas.

**Tests limites manquants** (objet de `NFR-CERT-03`) : le scénario *« dépôt hors heures ouvrées »* — dépôt accepté, décompte du SLA démarrant selon les règles de la file — n'est couvert par aucun test existant.

---

## 10. User Stories

- **US1 — Garantir qu'aucun accès n'échappe à l'authentification** (SEC-01) : en tant que responsable sécurité, je veux la preuve qu'aucune ressource protégée n'est atteignable sans authentification et que les essais répétés sont bloqués, afin de démontrer la conformité SEC-01.
- **US2 — Garantir le cloisonnement des données** (SEC-03) : en tant que responsable sécurité, je veux la preuve nominative qu'un client n'atteint jamais les données d'un autre et qu'un agent n'agit jamais hors de son rôle, y compris par accès direct.
- **US3 — Garantir l'inviolabilité du journal d'audit** (SEC-05) : en tant qu'auditeur, je veux la preuve que toute tentative de modification ou de suppression d'une entrée est rejetée.
- **US4 — Garantir l'expiration des sessions** (SEC-10) : en tant que responsable sécurité, je veux la preuve qu'une session inactive expire et que la période est paramétrable.
- **US5 — Garantir la désactivation sans perte d'historique** (SEC-11) : en tant qu'administrateur, je veux la preuve qu'un compte désactivé perd immédiatement son accès **et** que son historique demeure intact.
- **US6 — Garantir la sécurité des liens de notification** (SEC-12) : en tant que responsable sécurité, je veux la preuve qu'un lien reçu hors du portail ne donne jamais un accès direct à un document protégé.
- **US7 — Garantir que la reprise d'envoi est opérationnelle en exploitation** (ERR-02) : en tant qu'exploitant, je veux qu'un déploiement dépourvu de la configuration requise échoue explicitement au démarrage, plutôt que de laisser la reprise silencieusement inopérante.
- **US8 — Garantir le contrôle des documents déposés** (ERR-03) : en tant que client ou agent, je veux qu'un document non conforme soit refusé avec un message m'indiquant le motif.
- **US9 — Garantir une interface en français** (NFR-01) : en tant qu'utilisateur non technicien, je veux une interface intégralement en français.
- **US10 — Garantir l'utilisabilité mobile** (NFR-02) : en tant que client ou chauffeur équipé d'un smartphone, je veux pouvoir utiliser le portail depuis mon téléphone.
- **US11 — Garantir le respect des horaires de traitement dans le SLA** (NFR-11) : en tant que client, je veux pouvoir déposer un dossier hors heures ouvrées, et en tant que superviseur, je veux que le SLA se décompte selon les règles de la file.

---

## 11. Suites TEST-SPEC — 11 suites nominatives

> **Règle opposable** : un test existant **ne vaut preuve que s'il démontre effectivement le texte CDC**. Un test qui exerce un mécanisme voisin sans couvrir l'exigence ne peut pas être invoqué en Closure Review.

### SEC-CERT — 6 suites

| Suite | Exigence | Précondition | Action | Résultat attendu | Résultat interdit | Niveau recommandé | Preuve pour la Closure Review |
|:---|:---|:---|:---|:---|:---|:---|:---|
| **SEC-CERT-01** | SEC-01 | Un compte actif ; politique et plafond d'échecs configurés | Enchaîner des tentatives erronées au-delà du plafond, puis présenter le **bon** mot de passe | Compte verrouillé ; l'accès reste **refusé** pendant la durée de verrouillage | Accès accordé après verrouillage ; nombre d'essais illimité | Intégration (hôte réel) | Verrouillage effectif démontré, refus postérieur au bon mot de passe |
| **SEC-CERT-02** | SEC-03 | Deux clients de sociétés distinctes ; deux agents de files distinctes | Client A demande la ressource de client B **par identifiant direct** ; agent hors file tente une action | Refus **côté serveur**, sans divulgation d'existence ni d'appartenance | Ressource retournée ; message révélant l'existence | Application + Intégration | Refus serveur démontré sur les deux axes (client et agent) |
| **SEC-CERT-03** | SEC-05 | Une entrée d'audit existante | Tenter une **modification**, puis une **suppression** | Les deux tentatives sont rejetées ; le journal reste **intact** | Entrée modifiée ou supprimée | Infrastructure (base réelle) | Rejet démontré sur les deux opérations |
| **SEC-CERT-04** | SEC-10 | Période d'inactivité **configurée** | Laisser la session inactive au-delà de la période, puis demander une ressource protégée | Accès refusé ; ré-authentification exigée | Session encore valide ; période non paramétrable | Intégration | Paramétrabilité **et** application effective démontrées |
| **SEC-CERT-05** | SEC-11 | Un compte agent actif ayant produit de l'historique | Désactiver le compte, puis (a) tenter un accès, (b) consulter son historique | (a) accès refusé **immédiatement** ; (b) historique **intégralement conservé** | Accès maintenu ; trace supprimée, anonymisée ou détachée | Application + Intégration | Les **deux** volets démontrés dans le même scénario |
| **SEC-CERT-06** | SEC-12 | Une notification portant un lien vers un ticket | (a) Ouvrir le lien **sans authentification** ; (b) l'ouvrir avec une habilitation retirée | (a) conduit au contrôle d'identité, **aucun détail exposé** ; (b) refus **sans divulgation** | Document protégé servi directement ; jeton contournant les habilitations | Application + Intégration | Absence de jeton et absence d'accès direct démontrées |

### ERR-CERT — 2 suites

| Suite | Exigence | Précondition | Action | Résultat attendu | Résultat interdit | Niveau recommandé | Preuve pour la Closure Review |
|:---|:---|:---|:---|:---|:---|:---|:---|
| **ERR-CERT-01** | ERR-02 | Environnement **dépourvu** de la configuration de reprise | Démarrer le composant | **Échec explicite au démarrage**, message désignant le paramètre manquant | Démarrage réussi puis reprise silencieusement inopérante ; valeur par défaut substituée | Intégration (démarrage réel) | Fail-fast démontré ; **aucune valeur de reprise en dur** dans le code |
| **ERR-CERT-02** | ERR-03 | Règles de dépôt configurées (types, taille) | Déposer (a) un type non autorisé, (b) un fichier au-delà de la taille maximale | Refus **avant enregistrement**, avec **message explicite** désignant le critère | Fichier accepté ; refus sans motif | Application | Les deux volets démontrés avec leur message |

### NFR-CERT — 3 suites

| Suite | Exigence | Précondition | Action | Résultat attendu | Résultat interdit | Niveau recommandé | Preuve pour la Closure Review |
|:---|:---|:---|:---|:---|:---|:---|:---|
| **NFR-CERT-01** | NFR-01 | Écrans du parcours utilisateur | Inspecter les libellés exposés | Libellés **en français** | Libellé non francophone exposé à l'utilisateur | Interface | Vérification **reproductible**, sans seuil inventé |
| **NFR-CERT-02** | NFR-02 | Écrans du parcours client | Rendre en largeur mobile | Contenu lisible sans défilement horizontal du corps de page ; navigation et actions principales atteignables | Contenu tronqué ou inatteignable en largeur mobile | Interface | Rendu mobile **reproductible**, sans largeur contractuelle inventée |
| **NFR-CERT-03** | NFR-11 | Une file paramétrée avec son calendrier ouvré | Déposer un dossier **hors heures ouvrées** | Dépôt **accepté** ; décompte du SLA selon les règles de la file, non à l'instant du dépôt | Dépôt refusé ; décompte en heures calendaires | Domaine + Application | Les deux propriétés démontrées dans le même scénario |

**11 suites pour 11 exigences — correspondance exacte, aucun double comptage.**

### Niveau de test et code impacté par suite

| ID | Exigence | Niveau de test | Code existant | Code à modifier | Preuve pour la Closure Review |
|:---|:---|:---|:---|:---|:---|
| `SEC-CERT-01` | SEC-01 | Intégration (hôte réel) | Politique de mot de passe et verrouillage configurés ; 4 fichiers de test nomment SEC-01 | **AUCUN** | Verrouillage effectif + refus postérieur au bon mot de passe |
| `SEC-CERT-02` | SEC-03 | Application + Intégration | Service de cloisonnement et filtres serveur ; **0 fichier de test ne nomme SEC-03** | **AUCUN** | Refus serveur sur les deux axes (client ↔ client, agent ↔ file) |
| `SEC-CERT-03` | SEC-05 | Infrastructure (base réelle) | Garde applicative append-only ; 2 fichiers nomment SEC-05 | **AUCUN** | Rejet démontré sur modification **et** suppression |
| `SEC-CERT-04` | SEC-10 | Intégration | Lecture d'une clé de configuration ; **0 test** ; clé absente des fichiers de configuration | **AUCUN produit** — configuration de test uniquement | Paramétrabilité **et** effet démontrés, sans durée métier introduite |
| `SEC-CERT-05` | SEC-11 | Application + Intégration | Indicateur d'activité du compte ; 1 fichier nomme SEC-11 | **AUCUN** | Accès refusé **et** historique conservé, dans le même scénario |
| `SEC-CERT-06` | SEC-12 | Application + Intégration | Lien relatif sans jeton ; 2 fichiers nomment SEC-12 | **AUCUN** | Absence de jeton et absence d'accès direct |
| `ERR-CERT-01` | ERR-02 | Intégration (démarrage réel) | Moteur de reprise complet ; 6 tests | **1 point unique** — validation au démarrage du composant | Échec explicite du composant ; **aucune valeur de reprise en dur** |
| `ERR-CERT-02` | ERR-03 | Application | Contrôles type et taille avec messages explicites ; **0 test ne nomme ERR-03** | **AUCUN** | Les deux volets refusés avec leur message |
| `NFR-CERT-01` | NFR-01 | Interface | Interface francophone | **AUCUN** | Vérification reproductible, sans seuil inventé |
| `NFR-CERT-02` | NFR-02 | Interface (**structurel**) | Directive d'affichage mobile, points de rupture, primitives adaptatives | **AUCUN** | Contrat responsive appliqué — **limite déclarée** : pas de validation visuelle navigateur |
| `NFR-CERT-03` | NFR-11 | Domaine + Application | Calendrier ouvré paramétrable (heures, jours, fériés) ; 11 tests existants | **AUCUN** | Dépôt hors heures ouvrées accepté + décompte selon les règles de la file |

### Classification du code par exigence

| Exigence | SANS CODE | TESTS UNIQUEMENT | CORRECTION PRODUIT MINIMALE |
|:---|:---:|:---:|:---:|
| SEC-01 | | ✅ | |
| SEC-03 | | ✅ | |
| SEC-05 | | ✅ | |
| SEC-10 | | ✅ | |
| SEC-11 | | ✅ | |
| SEC-12 | ✅ | | |
| ERR-02 | | | ✅ |
| ERR-03 | | ✅ | |
| NFR-01 | | ✅ | |
| NFR-02 | | ✅ | |
| NFR-11 | | ✅ | |
| **Total** | **1** | **9** | **1** |

> **SEC-12** est la seule exigence certifiable **sans production nouvelle** : les deux fichiers de test existants couvrent déjà le comportement exigé ; la certification consiste à les rattacher nominativement et à compléter les cas négatifs si l'examen en révèle l'absence.
>
> **ERR-02** est la **seule correction de produit** de toute la Feature, portant sur **un point unique** : la validation de configuration au démarrage du composant.

---

## 12. Critères d'acceptation transverses

- **CA-01** — Chacune des 11 exigences dispose d'au moins un **scénario négatif** démontrant le comportement interdit.
- **CA-02** — Aucune règle de sécurité n'est démontrée **uniquement** par l'interface : chaque refus est prononcé côté serveur.
- **CA-03** — Aucun mécanisme existant n'est réimplémenté ; les suites s'appuient sur les services centralisés en place.
- **CA-04** — **Aucune migration, aucune table, aucune entité de domaine** n'est créée.
- **CA-05** — **Aucune fonctionnalité métier ni écran** n'est ajouté.
- **CA-06** — ERR-02 : **aucune valeur de reprise** n'apparaît dans le code ; **CLAR-NOT-02 reste ouverte**.
- **CA-07** — NFR : **aucun seuil** absent du CDC n'est introduit.
- **CA-08** — SEC-02, SEC-07 et SEC-08 **n'apparaissent dans aucune User Story, aucun critère, aucune suite TEST-SPEC**, et ne comptent dans aucun gain.
- **CA-09** — La baseline reste **146/188** jusqu'à la Closure Review ; **aucune couverture n'est déclarée acquise** avant elle.

---

## 13. Hypothèses

1. Les mécanismes de sécurité livrés par F001→F010 sont **fonctionnellement corrects** ; F011 les **certifie** sans les auditer à nouveau au-delà de ce que les suites démontrent.
2. Le CDC prévaut sur `docs/01-matrice-exigences.md` en cas de divergence — précédent établi pour SEC-02, dont le texte était tronqué dans la matrice.
3. Les 83 fichiers modifiés préexistants dans l'arbre de travail (refonte UI / connexion / 2FA) sont **étrangers à F011** et ne doivent être ni absorbés, ni corrigés par cette Feature.
4. La correction ERR-02 est **isolable** de la logique métier du moteur de notification.

---

## 14. Impact sur la couverture CDC

| | |
|:---|:---|
| **Baseline acquise (source `docs/17`)** | **146 / 188 (77,66 %)** |
| Exigences visées par F011 | **11** (11 Must) |
| **Projection maximale** | **157 / 188 (83,51 %)** |
| **Couverture certifiée après F011** | **NON ACQUISE — établie uniquement en Closure Review** |

> Aucune exigence n'est comptée avant sa certification effective. Si une suite TEST-SPEC ne démontre pas son exigence, celle-ci reste **PARTIELLE** et sort du gain.

---

*Document préparé pour `/speckit.clarify`. Aucun code n'a été modifié, aucune migration créée. Les mécanismes existants sont certifiés, jamais réimplémentés. SEC-02, SEC-07 et SEC-08 restent hors périmètre conformément au Final Scope Gate.*
