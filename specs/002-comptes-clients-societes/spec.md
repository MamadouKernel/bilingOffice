# Feature Specification: Comptes clients, sociétés et utilisateurs rattachés

**Feature Branch**: `002-comptes-clients-societes`

**Created**: 2026-08-29

**Status**: Draft — spécification initiale, en attente de validation humaine avant `/speckit.clarify`/`/speckit.plan`

**Input**: User description : « Comptes clients, sociétés et utilisateurs rattachés — construire les
capacités liées aux comptes clients, sociétés et utilisateurs nécessaires aux futurs parcours
métier, indépendamment des parcours détaillés (Billing Office IMPORT/EXPORT, Simulation Proforma,
Avoir/Exonération, Customer Complaint, Enrôlement Transporteurs). »

## Sources de vérité

- `docs/CDC_Plateforme_Digitale_CIT_v3.docx` — cahier des charges, source de vérité métier unique
- `docs/00-analyse-cdc.md` (notamment §4.1 Acteurs côté client, §6.7 Authentification/autorisation,
  §6.8 Comptes clients et sociétés, §20 Modèle de données minimal), `docs/01-matrice-exigences.md`
  (groupe C — CPT/ENT), `docs/02-points-a-clarifier.md`, `docs/03-revue-coherence-analyse.md`,
  `docs/04-matrice-recette.md`
- `docs/cdc_text.txt` — texte brut du CDC (section 10 « Gestion des comptes et types de clients »,
  §10.1 « Compte entreprise et utilisateurs rattachés », §20 « Modèle de données minimal »)
- `.specify/memory/constitution.md` — principes non négociables du projet (v1.0.0)
- `specs/001-socle-tickets-workflows/spec.md`, `closure-review.md`, `decision-d6-numbering.md` —
  consultés uniquement pour identifier les capacités transverses déjà livrées et éviter leur
  duplication (Feature 001 est **CLOSED**, commit baseline `a9657a4` ; non modifiée par cette
  feature, sauf contradiction bloquante — aucune n'a été détectée)

## Étape 0 — Détermination mécanique du périmètre réel

### 0.1 Table de sélection des exigences (extraction depuis `docs/01-matrice-exigences.md`)

> Méthode : chaque exigence du CDC a été relue dans son texte intégral (`docs/cdc_text.txt` section
> 10, `docs/00-analyse-cdc.md` §6.8) — jamais sélectionnée sur la seule base de son préfixe
> (`CPT-`/`ENT-`). Deux exigences portant un préfixe différent (`CPT-04`) ont été incluses parce que
> leur contenu réel concerne le compte client ; aucune exigence `CPT-`/`ENT-` n'a été exclue.

| Exigence CDC | Priorité | Description (fidèle au CDC) | Pourquoi Feature 002 | Dépendance Feature 001 | Hors périmètre éventuel |
|---|---|---|---|---|---|
| CPT-01 | Must | Création de compte client soumise à validation par CIT avant activation ; informations et justificatifs paramétrables | Définit le cycle de vie du compte (société cliente) lui-même — cœur de cette feature | Aucune (nouveau référentiel) ; réutilisera le journal d'audit (SOC-13) pour tracer la validation | Aucun — pleinement dans le périmètre |
| CPT-02 | Must | Le profil du client (normal / en compte / deposit / IES) est déclaré à la création et validé par CIT ; détermine automatiquement le workflow Billing Office | Le **profil** est un attribut du compte (déclaré/validé ici) ; son **usage** pour orienter un workflow Billing Office appartient à la future feature Billing Office | Aucune | La logique de sélection du workflow Billing Office selon le profil (IMP-06, IMP-10…) est **hors périmètre** — Feature 002 ne fait qu'exposer la valeur du profil |
| CPT-03 | Must | Authentification sécurisée (identifiant/mot de passe, politique de robustesse) ; récupération de mot de passe en autonomie | Le compte client n'a de sens que si son titulaire peut s'y connecter ; capacité fonctionnelle observable (se connecter, récupérer son mot de passe) | Aucune (Feature 001 a explicitement exclu SEC-01/08-11 comme « générique, hors socle ») | Les mécanismes techniques (ASP.NET Core Identity, claims, policies) — décision de `/speckit.plan` |
| CPT-04 | Must | Un client n'accède qu'à ses propres tickets, documents et échanges | Règle de cloisonnement **au niveau du compte client** — nécessaire dès qu'un compte existe, avant même qu'un ticket ne soit créé | **Extension du socle** : Feature 001 a déjà un mécanisme de cloisonnement (`CloisonnementService`) vérifiant la propriété d'un ticket via un identifiant client unique — Feature 002 doit préciser à quoi cet identifiant correspond une fois un compte société/utilisateur réel introduit (voir §« Frontière avec Feature 001 ») | Le mécanisme technique de vérification (policies, requêtes filtrées) reste `/speckit.plan` |
| CPT-05 | Must | La gestion multi-utilisateurs pour une même société cliente est requise (détaillée en §10.1 CDC) | Renvoie explicitement à ENT-01..04 — c'est l'affirmation du principe, dont le détail est porté par le groupe ENT | Aucune | — |
| CPT-06 | Should | Modification du type de client par l'administrateur fonctionnel CIT : tracée, sans effet rétroactif sur les tickets en cours | Gestion administrative du référentiel de comptes — cœur de cette feature | Réutilise le journal d'audit (SOC-13) pour la traçabilité | La définition de ce que « sans effet rétroactif » signifie pour un ticket **déjà engagé** dans un workflow Billing Office appartient à la feature qui possède ce workflow |
| CPT-07 | Could | Authentification renforcée (second facteur) pour les comptes clients (indépendant de SEC-08, agents) | Capacité optionnelle du compte client | Aucune | Mécanisme technique du second facteur — `/speckit.plan` |
| ENT-01 | Must | Une société cliente peut rattacher plusieurs utilisateurs à son compte | Cœur du modèle multi-utilisateurs — cette feature | Aucune | — |
| ENT-02 | Must | Un administrateur de la société gère ses collaborateurs : activation/désactivation, droits de consultation et de soumission paramétrables | Cœur du modèle multi-utilisateurs — cette feature | Réutilise le moteur d'habilitations paramétrables de Feature 001 (rôles/permissions par attribution, jamais codés en dur) pour porter ces droits | Le contenu métier détaillé de « droit de soumission » pour un module particulier (ex. Billing IMPORT) appartient à ce module futur — Feature 002 ne fait que porter le droit générique |
| ENT-03 | Must | La visibilité des dossiers dépend des droits accordés : un utilisateur voit ses propres demandes ou l'ensemble de celles de sa société | Règle de cloisonnement **au niveau société**, extension du modèle CPT-04 | **Extension du socle** : le cloisonnement de Feature 001 ne connaît qu'un identifiant client unique par dossier ; ENT-03 exige de distinguer l'utilisateur individuel du compte société auquel il est rattaché (voir §« Frontière avec Feature 001 ») | Le mécanisme technique (requêtes, policies) — `/speckit.plan` |
| ENT-04 | Should | Journalisation des actions des administrateurs de société (ajout/retrait de collaborateurs, modification de droits) | Traçabilité des actions de gestion des utilisateurs rattachés | Réutilise intégralement le journal d'audit de Feature 001 (SOC-13, append-only) — **aucune réimplémentation** | — |

**Total sélectionné** : 11 exigences (8 Must, 2 Should, 1 Could) — identique au total du groupe
« Comptes clients / Sociétés (CPT, ENT) » de `docs/01-matrice-exigences.md` (récapitulatif par
module : 8 Must + 2 Should + 1 Could = 11).

### 0.2 Vérification inverse (Feature 002 → exigences sélectionnées → matrice CDC)

| Contrôle | Résultat |
|---|---|
| Chaque exigence sélectionnée existe-t-elle littéralement dans `docs/01-matrice-exigences.md` (groupe C) ? | ✅ Les 11 références (CPT-01→07, ENT-01→04) sont présentes mot pour mot, aucune référence inventée |
| Toutes les exigences du groupe C sont-elles couvertes (aucune omission) ? | ✅ 11/11 — aucune exigence CPT-/ENT- laissée de côté |
| Une exigence d'un autre groupe a-t-elle été absorbée à tort ? | ✅ Non — SEC-01/02/03/07/08/10/11/12 (citées uniquement comme critères transverses, à l'identique de la méthode Feature 001), COM-01 (citée uniquement pour justifier qu'un identifiant de contact existe), WKF-01/IMP-06 (citées uniquement pour justifier la frontière de périmètre) ne sont **pas** des FR de cette feature |
| Un doublon avec Feature 001 a-t-il été détecté ? | ✅ Non — CPT-04/SEC-03 (cloisonnement) sont déjà **appliqués génériquement** par Feature 001 sur un identifiant client abstrait ; Feature 002 ne réimplémente pas ce mécanisme, elle en précise le sujet (voir §« Frontière avec Feature 001 ») |
| Une exigence appartenant à une feature future a-t-elle été absorbée artificiellement ? | ✅ Non — IMP-06/IMP-10/IMP-20/IMP-30/IMP-40 (orientation du workflow Billing Office selon le profil client) sont explicitement **exclues** ; seule la **déclaration/validation** du profil (CPT-02) est retenue |

## Périmètre de cette feature

Cette feature spécifie le référentiel des **comptes clients (sociétés)**, des **utilisateurs qui y
sont rattachés**, et des règles de **cloisonnement** et d'**habilitation** directement liées à ces
comptes. Elle correspond exactement au groupe **C — Comptes clients et sociétés (CPT/ENT)** de
`docs/01-matrice-exigences.md`.

**Explicitement hors périmètre** (feront l'objet de features dédiées ultérieures, qui consommeront
ce référentiel) : voir la section « Hors périmètre Feature 002 » ci-dessous.

Le CDC ne mandate aucune technologie précise pour cette feature : les choix d'architecture détaillée
(modèle de données physique, projets .NET, mécanisme d'authentification technique) appartiennent
exclusivement à `/speckit.plan`, conformément au Principe I de la constitution.

## Identité technique vs notions métier

Conformément à la demande explicite de ne pas fusionner ces concepts sans justification, et en
s'appuyant strictement sur `docs/00-analyse-cdc.md` §4.1 et §6.8 et `docs/cdc_text.txt` §10/§10.1 :

- **Identité technique** (mécanisme d'authentification) : hors périmètre de cette spécification —
  appartient à `/speckit.plan` (CPT-03 décrit la **capacité observable** — s'authentifier, récupérer
  un mot de passe — jamais son mécanisme technique).
- **Utilisateur métier** : une personne physique qui utilise la plateforme. Le CDC distingue
  exactement deux rôles d'utilisateur côté société (`docs/00-analyse-cdc.md` §4.1) : **Utilisateur
  de société** (droits paramétrables) et **Administrateur de société** (DEFINED_ROLE, gère les
  utilisateurs de sa société). Le CDC ne définit aucune autre distinction (pas de « principal/
  secondaire » au-delà de ces deux rôles).
- **Société / Compte client** : d'après `docs/cdc_text.txt` §10.1, *« l'entité principale est la
  société cliente, à laquelle sont rattachés plusieurs utilisateurs »*, et d'après
  `docs/00-analyse-cdc.md` §4.1, les quatre profils de client (`normal`, `en compte`, `deposit`,
  `IES`) sont **tous** de type de compte « Société ». **Constat dérivé du CDC (non inventé)** : pour
  ce périmètre (Billing Office et au-delà), la notion de « client » du CDC est *systématiquement*
  portée par une société — le CDC ne décrit aucun compte client individuel distinct d'une société à
  un seul utilisateur. Une société à un seul utilisateur reste néanmoins possible : cet utilisateur
  unique est alors nécessairement son propre « Administrateur de société » (voir Assumptions).
- **Client** : notion métier du CDC désignant le compte société avec son profil (normal/en compte/
  deposit/IES) — c'est le sujet de CPT-01/02.
- **Habilitation** : permission/périmètre déjà modélisé de façon générique et paramétrable par
  Feature 001 (`HabilitationUtilisateur`, jamais un rôle codé en dur) — Feature 002 réutilise ce
  mécanisme pour porter les droits de consultation/soumission d'ENT-02, elle n'en crée pas un
  second.

## Frontière avec Feature 001

Feature 001 fournit déjà, de façon générique et paramétrable : le ticket, le dossier, les files, les
affectations, le moteur d'habilitations (`IAutorisationService`, `HabilitationUtilisateur`), le
cloisonnement (`CloisonnementService`), le journal d'audit (append-only), la gestion documentaire,
les SLA, la supervision. **Feature 002 réutilise ces capacités sans les réimplémenter.**

**Aucune réimplémentation** : ni second système d'autorisation, ni second système d'audit, ni second
moteur d'habilitations, ni second modèle de workflow, ni second mécanisme de documents.

**Besoins d'extension du socle identifiés** (documentés ici, non conçus techniquement — décision de
`/speckit.plan`) :

1. **Cloisonnement à deux niveaux (CPT-04 + ENT-03)** — *Besoin* : le mécanisme actuel de Feature 001
   (`CloisonnementService.ExigerProprieteClientAsync`) vérifie la propriété d'un dossier via un
   identifiant client unique et abstrait (`Dossier.ClientId`), sans distinguer l'utilisateur
   individuel qui a soumis la demande de la société à laquelle il est rattaché. *Déjà prévu par
   Feature 001 ?* Non — Feature 001 documente elle-même (spec.md, section Sécurité) que CPT-04 est
   « hors périmètre » et n'est respecté qu'en frontière, sur un `ClientId` volontairement non défini
   plus précisément. *Extension nécessaire, précisée par CLAR-01/CLAR-02* : `Dossier.ClientId`
   devient conceptuellement l'identifiant de la **société** propriétaire (le cloisonnement
   inter-société existant, « `ClientId` inconnu → toujours refusé », continue de s'appliquer sans
   modification) ; un champ **auteur individuel distinct** est ajouté pour permettre le filtre
   « ses propres demandes uniquement » (ENT-03) ; le droit de consultation de l'utilisateur (ENT-02 :
   « ses demandes » vs « toutes celles de la société ») détermine ensuite si le filtre auteur
   s'applique ou non, sauf pour l'administrateur de société qui voit toujours l'ensemble (CLAR-02).
   *Garde-fou* : le comportement déjà validé de Feature 001 (un `ClientId` inconnu de l'appelant est
   toujours refusé) ne doit pas être modifié silencieusement — l'extension s'ajoute au mécanisme
   existant, elle ne le remplace pas. La représentation technique exacte (nouveau champ sur
   `Dossier`, table de rattachement séparée, etc.) reste une décision de `/speckit.plan`.
2. **Rattachement des habilitations à un compte société plutôt qu'à un identifiant isolé** — ENT-02
   attribue des droits de consultation/soumission à un utilisateur *dans le contexte de sa société*.
   Le modèle `HabilitationUtilisateur` de Feature 001 (permission + utilisateur + file optionnelle)
   ne porte aujourd'hui aucune notion de société. *Extension nécessaire* : documentée pour
   `/speckit.plan` ; ne modifie pas le comportement des habilitations déjà validées côté agents/
   superviseurs de Feature 001.
3. **Audit des actions de l'administrateur de société (ENT-04)** — *Déjà prévu par Feature 001 ?*
   Oui, intégralement : le journal d'audit append-only existant suffit à tracer auteur/action/
   horodatage/objet pour toute action de gestion de collaborateurs. **Aucune extension requise.**

## Cas d'usage recherchés dans le CDC (vérification explicite)

| # | Cas d'usage | Prévu par le CDC ? | Référence |
|---|---|---|---|
| 1 | Création d'un compte | ✅ Oui | CPT-01 |
| 2 | Création/enregistrement d'une société | ✅ Oui — la société **est** le compte (§10.1) | CPT-01, CPT-02 |
| 3 | Rattachement d'un utilisateur à une société | ✅ Oui | ENT-01 |
| 4 | Plusieurs utilisateurs pour une société | ✅ Oui | ENT-01, CPT-05 |
| 5 | Modification des informations | ✅ Oui, mais **seulement pour le type de client**, par CIT (CPT-06) — aucune exigence CDC sur la modification des autres informations de profil (nom, contact…) : non incluse (non inventée) | CPT-06 |
| 6 | Activation/désactivation | ✅ Oui, à deux niveaux distincts : activation du compte société lui-même (CPT-01, par CIT) et activation/désactivation d'un collaborateur (ENT-02, par l'administrateur de société) | CPT-01, ENT-02 |
| 7 | Consultation du profil | ⚠️ Non explicitement écrit comme exigence dédiée — déduit comme corollaire nécessaire de CPT-04/ENT-03 (un utilisateur doit pouvoir consulter ce qu'il est autorisé à consulter) ; traité comme critère d'acceptation, pas comme FR séparée | CPT-04, ENT-03 (déduit) |
| 8 | Gestion des droits de consultation | ✅ Oui | ENT-02, ENT-03 |
| 9 | Gestion des droits de soumission | ✅ Oui | ENT-02 |
| 10 | Visibilité limitée aux données autorisées | ✅ Oui | CPT-04, ENT-03 |
| 11 | Utilisateur principal/secondaire | ❌ Non prévu au-delà de la distinction Administrateur/Utilisateur de société | — (voir Assumptions) |
| 12 | Changement de rattachement (d'une société à une autre) | ❌ Non défini par le CDC | Voir « Clarifications » |
| 13 | Gestion administrative des comptes | ✅ Oui, côté CIT (validation, typage — CPT-01/02/06) | CPT-01, CPT-02, CPT-06 |
| 14 | Historique/audit des modifications | ✅ Oui (ENT-04 explicite pour les actions de l'admin de société ; SOC-13/SEC-05 pour le reste, réutilisé de Feature 001) | ENT-04 |
| 15 | Recherche/consultation des sociétés ou comptes | ⚠️ Déduit : CPT-01 exige une validation CIT, qui suppose nécessairement qu'un admin fonctionnel puisse consulter les comptes en attente ; aucune exigence de recherche avancée n'est écrite | CPT-01 (déduit) |
| 16 | Distinction des types de clients | ✅ Oui | CPT-02 |
| 17 | Données obligatoires selon type de client | ⚠️ Le CDC dit explicitement que « les informations et justificatifs exigés sont paramétrables » (CPT-01) — **aucune liste figée n'est donnée** ; non inventée ici | CPT-01 |
| 18 | Restrictions particulières selon profil/type | ❌ Aucune restriction spécifique par profil (normal/en compte/deposit/IES) n'est décrite au niveau du **compte** lui-même (les différences de workflow Billing Office sont hors périmètre) | — |

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Création et validation d'un compte société (Priority: P1)

Un représentant d'une société souhaitant utiliser la plateforme soumet une demande de création de
compte en déclarant son profil (normal, en compte, deposit ou IES) ; un administrateur fonctionnel
CIT valide cette demande avant que le compte ne devienne actif.

**Why this priority** : sans un compte société validé, aucune autre capacité de cette feature (ni
aucun futur parcours métier consommateur) ne peut exister. C'est le P1 absolu de cette feature.

**Independent Test** : peut être testé intégralement en soumettant une demande de compte avec un
profil déclaré, en vérifiant qu'elle reste inactive tant qu'un administrateur fonctionnel ne l'a pas
validée, puis en vérifiant l'activation effective après validation — sans dépendre d'aucun parcours
métier futur.

**Acceptance Scenarios**:

1. **Given** un visiteur non encore titulaire d'un compte, **When** il soumet une demande de compte
   société en déclarant son profil (normal/en compte/deposit/IES) et les informations/justificatifs
   demandés, **Then** une demande de compte est enregistrée à l'état « en attente de validation »,
   non utilisable pour se connecter (CPT-01, CPT-02).
2. **Given** une demande de compte en attente, **When** un administrateur fonctionnel CIT l'examine
   et la valide, **Then** le compte devient actif et son profil déclaré est confirmé (CPT-01, CPT-02).
3. **Given** une demande de compte en attente, **When** un administrateur fonctionnel CIT la rejette
   (motif obligatoire, par cohérence avec le principe de traçabilité de Feature 001), **Then** le
   compte reste inactif et la décision est tracée (CPT-01 ; critère de traçabilité déduit du
   Principe IX de la constitution — aucune exigence CDC dédiée au rejet n'existe explicitement pour
   ce cas, traité par analogie avec SOC-06/IMP-04).
4. **Given** un utilisateur non habilité (pas administrateur fonctionnel), **When** il tente de
   valider une demande de compte, **Then** l'opération est refusée côté serveur (scénario négatif —
   réutilise `IAutorisationService` de Feature 001).

---

### User Story 2 - Authentification et accès au compte (Priority: P1)

Le titulaire d'un compte validé doit pouvoir s'authentifier de façon sécurisée pour accéder à la
plateforme, et récupérer l'accès à son compte de façon autonome en cas de mot de passe oublié.

**Why this priority** : un compte créé mais inaccessible n'a aucune valeur — c'est un prérequis
direct à toute utilisation réelle du compte, au même niveau de priorité que sa création.

**Independent Test** : peut être testé en tentant de se connecter avec un compte validé (succès),
un compte non encore validé (échec), et en déclenchant une procédure de récupération de mot de
passe — sans dépendre d'aucun autre module.

**Acceptance Scenarios**:

1. **Given** un compte validé et actif, **When** son titulaire s'authentifie avec des identifiants
   corrects, **Then** l'accès est accordé (CPT-03).
2. **Given** un compte non encore validé ou désactivé, **When** une tentative d'authentification est
   effectuée, **Then** l'accès est refusé, avec un motif explicite ne révélant pas d'information
   sensible sur l'état exact du compte (scénario négatif — compte désactivé/non validé).
3. **Given** un titulaire de compte ayant oublié son mot de passe, **When** il déclenche la
   procédure de récupération, **Then** il peut redéfinir son mot de passe de façon autonome, sans
   intervention d'un agent CIT (CPT-03).
4. **Given** une politique de robustesse des mots de passe paramétrée, **When** un mot de passe ne
   la respecte pas, **Then** sa définition est refusée avec un message explicite (CPT-03).

---

### User Story 3 - Gestion des utilisateurs rattachés à une société (Priority: P2)

L'administrateur d'une société gère les collaborateurs rattachés au compte de sa société : il peut
en ajouter, les activer ou les désactiver, et paramétrer leurs droits de consultation et de
soumission.

**Why this priority** : c'est la capacité multi-utilisateurs explicitement requise par le CDC
(CPT-05) ; elle suppose qu'un compte société existe déjà (US1) mais peut être démontrée
indépendamment de tout parcours métier consommateur.

**Independent Test** : peut être testé en rattachant plusieurs utilisateurs à une société déjà
validée (via US1), en faisant varier leurs droits, et en vérifiant les actions d'activation/
désactivation par l'administrateur de société — sans dépendre d'aucun module métier futur.

**Acceptance Scenarios**:

1. **Given** un compte société actif, **When** son administrateur de société rattache un nouvel
   utilisateur, **Then** l'utilisateur est associé à la société avec un droit de consultation et un
   droit de soumission initiaux (ENT-01, ENT-02).
2. **Given** un utilisateur rattaché à une société, **When** l'administrateur de société modifie ses
   droits de consultation (ses demandes uniquement / toutes les demandes de la société) ou son
   droit de soumission, **Then** le changement s'applique immédiatement (ENT-02).
3. **Given** un utilisateur rattaché à une société, **When** l'administrateur de société le
   désactive, **Then** il ne peut plus s'authentifier, sans que son historique ne soit supprimé
   (ENT-02 ; principe de conservation de l'historique — cf. Principe VIII/IX de la constitution, par
   analogie avec SEC-11 appliqué aux agents).
4. **Given** un utilisateur qui n'est pas administrateur de sa société, **When** il tente d'ajouter,
   d'activer/désactiver ou de modifier les droits d'un autre utilisateur de la société, **Then**
   l'opération est refusée côté serveur (scénario négatif — moindre privilège).
5. **Given** toute action de gestion de collaborateur par un administrateur de société (ajout,
   retrait, modification de droits), **When** elle est exécutée, **Then** elle est journalisée dans
   le journal d'audit existant, avec auteur, action, date/heure et détail (ENT-04, réutilise le
   journal d'audit de Feature 001 sans le réimplémenter).
6. **Given** une société avec un seul administrateur actif, **When** une tentative de le désactiver
   ou de retirer son rôle d'administrateur est effectuée (par lui-même ou par un tiers habilité),
   **Then** l'opération est refusée avec un motif explicite, garantissant qu'il existe toujours au
   moins un administrateur actif pour la société (ENT-02 — **CLAR-03**).

---

### User Story 4 - Cloisonnement et visibilité des données selon le compte et les droits (Priority: P2)

Un utilisateur ne doit jamais consulter des données appartenant à un autre compte société ; au sein
de sa propre société, sa visibilité dépend précisément du droit qui lui a été accordé (ses demandes
uniquement, ou l'ensemble de celles de sa société).

**Why this priority** : sujet de sécurité critique explicitement signalé comme tel par la demande de
spécification ; sa valeur est réelle dès qu'un compte existe (US1) et prend tout son sens dès que
plusieurs utilisateurs coexistent (US3).

**Independent Test** : peut être testé en créant deux comptes sociétés distincts et en vérifiant
qu'aucun utilisateur de l'un n'accède aux données de l'autre, puis en faisant varier le droit de
visibilité d'un utilisateur au sein d'une même société — sans dépendre d'aucun ticket ni parcours
métier réel (des données de test suffisent).

**Acceptance Scenarios**:

1. **Given** deux comptes société distincts A et B, **When** un utilisateur du compte A tente de
   consulter une donnée (profil, liste d'utilisateurs rattachés) du compte B, **Then** l'accès est
   refusé côté serveur, indépendamment de toute restriction d'interface (CPT-04, SEC-03 — critère
   non négociable, cf. Principe VII de la constitution).
2. **Given** un utilisateur d'une société disposant du droit « ses demandes uniquement », **When**
   il consulte la liste des demandes de sa société, **Then** il ne voit que celles qu'il a lui-même
   soumises (ENT-03).
3. **Given** un utilisateur d'une société disposant du droit « toutes les demandes de la société »,
   **When** il consulte la liste des demandes de sa société, **Then** il voit l'ensemble des
   demandes soumises par tous les utilisateurs de sa société, y compris celles soumises par
   d'autres (ENT-03).
4. **Given** un compte désactivé (société ou utilisateur), **When** une tentative d'accès aux
   données qui lui sont normalement associées est effectuée, **Then** l'accès est refusé (scénario
   négatif — compte désactivé, ENT-02).
5. **Given** un administrateur de société dont le droit de consultation individuel est réglé sur
   « ses demandes uniquement », **When** il consulte les demandes de sa société, **Then** il voit
   l'ensemble des demandes soumises par tous les utilisateurs de sa société, malgré son propre droit
   individuel restreint — le rôle d'administrateur porte un droit de consultation total qui prévaut
   sur son paramétrage individuel (ENT-02, ENT-03 — **CLAR-02**).

---

### User Story 5 - Modification tracée du type de client (Priority: P3)

Un administrateur fonctionnel CIT peut faire évoluer le profil déclaré d'une société cliente
(normal, en compte, deposit, IES), cette modification étant tracée et sans effet rétroactif sur les
tickets déjà en cours.

**Why this priority** : capacité de gestion administrative utile en exploitation réelle, mais non
indispensable à la démonstration du cycle de vie de base du compte (US1-US4) ; priorité Should du
CDC.

**Independent Test** : peut être testé en modifiant le profil d'une société déjà validée (via US1)
et en vérifiant la traçabilité de la modification et l'absence d'effet rétroactif déclaré.

**Acceptance Scenarios**:

1. **Given** un compte société actif avec un profil déclaré, **When** un administrateur fonctionnel
   CIT modifie ce profil, **Then** le changement est appliqué, avec l'ancien profil, le nouveau
   profil, l'auteur et la date/heure tracés (CPT-06).
2. **Given** un compte dont le profil vient d'être modifié, **When** l'effet de ce changement sur
   les tickets déjà en cours est examiné, **Then** aucun de ces tickets n'est rétroactivement
   affecté par le nouveau profil (CPT-06 ; la définition précise de « ticket en cours » et de son
   traitement concret reste de la responsabilité de la future feature Billing Office qui porte ces
   tickets — Feature 002 ne fait que garantir que le changement de profil du compte, en lui-même,
   ne déclenche aucune action rétroactive).
3. **Given** un utilisateur non habilité (pas administrateur fonctionnel CIT), **When** il tente de
   modifier le profil d'une société, **Then** l'opération est refusée côté serveur (scénario
   négatif).

---

### User Story 6 - Authentification renforcée pour les comptes clients (Priority: P4)

Un titulaire de compte peut activer un second facteur d'authentification pour renforcer la sécurité
de son compte.

**Why this priority** : capacité optionnelle (Could) — apporte de la valeur en sécurité perçue mais
n'est indispensable à aucune démonstration fonctionnelle du socle de comptes.

**Independent Test** : peut être testé en activant un second facteur sur un compte déjà authentifiable
(US2) et en vérifiant qu'il est requis lors des connexions suivantes.

**Acceptance Scenarios**:

1. **Given** un compte actif, **When** son titulaire active un second facteur d'authentification,
   **Then** les connexions suivantes exigent ce second facteur en plus de l'identifiant/mot de passe
   (CPT-07).

---

### Edge Cases

- Que se passe-t-il si le dernier « Administrateur de société » d'une société est désactivé alors
  que d'autres utilisateurs restent rattachés ? **Résolu (CLAR-03)** : la désactivation du compte du
  dernier administrateur actif, ou le retrait de son rôle, est refusée tant qu'aucun autre
  administrateur actif n'existe pour la société — plusieurs administrateurs peuvent coexister, mais
  une société ne peut jamais se retrouver sans administrateur actif.
- Un utilisateur peut-il être rattaché à plusieurs sociétés simultanément ? Le CDC décrit un
  rattachement (« ses utilisateurs », « rattaché à un compte entreprise ») sans jamais évoquer une
  pluralité de rattachements pour une même personne — traité par défaut comme un rattachement
  unique par utilisateur (voir Assumptions), sans que cela soit une exigence explicite.
- Que devient une demande de compte jamais traitée (ni validée ni rejetée) par CIT ? Le CDC ne
  prévoit aucun délai ni expiration automatique — non résolu, voir « Clarifications ».
- Un compte société peut-il exister sans aucun utilisateur rattaché (tous désactivés) ? Le CDC ne
  l'interdit pas explicitement ; traité comme un état valide (le compte reste, ses données restent
  consultables par CIT), par analogie avec le principe de non-suppression de l'historique
  (Principe VIII/IX de la constitution).
- Comment le système réagit-il si deux administrateurs fonctionnels CIT valident/rejettent la même
  demande de compte simultanément ? Le CDC ne précise pas de règle explicite ; par analogie avec le
  principe déjà retenu par Feature 001 pour la prise en charge concurrente d'un ticket (un seul des
  deux doit réussir, l'autre étant informé que l'opération n'est plus possible), le même principe
  général s'applique ici — la conception détaillée du mécanisme reste `/speckit.plan`.

## Requirements *(mandatory)*

> Chaque exigence ci-dessous conserve sa référence CDC d'origine entre parenthèses et sa priorité
> Must/Should/Could telle que documentée dans `docs/01-matrice-exigences.md`. Aucune référence n'est
> inventée ; aucune n'est renumérotée.

### Compte client — création et cycle de vie (CPT)

- **FR-001** (CPT-01, Must) : Le système DOIT permettre à un client de soumettre une demande de
  compte société, et DOIT soumettre cette demande à une validation par un administrateur
  fonctionnel CIT avant toute activation. Les informations et justificatifs exigés à la soumission
  DOIVENT rester paramétrables (aucune liste figée dans cette feature — PC-08).
- **FR-002** (CPT-02, Must) : Le système DOIT permettre au client de déclarer, à la création de son
  compte, un profil parmi : normal, en compte, deposit, IES ; ce profil DOIT être confirmé par CIT
  lors de la validation. La feature expose ce profil comme un attribut du compte ; l'usage de ce
  profil pour déterminer un workflow Billing Office est hors périmètre (voir « Hors périmètre »).
- **FR-003** (CPT-03, Must) : Le système DOIT permettre au titulaire d'un compte validé de
  s'authentifier de façon sécurisée (politique de robustesse du mot de passe) et de récupérer
  l'accès à son compte de façon autonome en cas de mot de passe oublié.
- **FR-004** (CPT-04, Must) : Le système DOIT garantir qu'un client n'accède qu'à ses propres
  tickets, documents et échanges — vérifié côté serveur, indépendamment de toute restriction
  d'interface.
- **FR-005** (CPT-05, Must) : Le système DOIT permettre la gestion multi-utilisateurs pour une même
  société cliente (détail porté par les exigences ENT-01 à ENT-04 ci-dessous).
- **FR-006** (CPT-06, Should) : Le système DEVRAIT permettre à un administrateur fonctionnel CIT de
  modifier le profil déclaré d'un compte société ; cette modification DOIT être tracée (ancien
  profil, nouveau profil, auteur, date/heure) et NE DOIT PAS produire d'effet rétroactif sur les
  tickets déjà en cours.
- **FR-007** (CPT-07, Could) : Le système DEVRAIT permettre au titulaire d'un compte client d'activer
  une authentification renforcée (second facteur), indépendamment du second facteur des comptes
  agents/administrateurs (SEC-08, hors périmètre de cette feature).

### Société et utilisateurs rattachés (ENT)

- **FR-008** (ENT-01, Must) : Le système DOIT permettre à une société cliente de rattacher
  plusieurs utilisateurs à son compte.
- **FR-009** (ENT-02, Must) : Le système DOIT permettre à un administrateur de société de gérer ses
  collaborateurs : activation, désactivation, et paramétrage de leurs droits de consultation et de
  soumission. Plusieurs administrateurs peuvent coexister pour une même société ; le système DOIT
  refuser la désactivation du compte du dernier administrateur actif ainsi que le retrait de son
  rôle, tant qu'aucun autre administrateur actif n'existe pour la société (**CLAR-03**). Le rôle
  Administrateur de société porte de facto un droit de consultation total sur les demandes de sa
  société, indépendamment de son propre droit de consultation individuel éventuel (**CLAR-02**).
- **FR-010** (ENT-03, Must) : Le système DOIT restreindre la visibilité des dossiers/demandes selon
  les droits accordés à chaque utilisateur rattaché : un utilisateur voit uniquement ses propres
  demandes, ou l'ensemble des demandes de sa société, selon le droit qui lui a été accordé — jamais
  par défaut la totalité sans droit explicite (sauf pour un administrateur de société, cf. FR-009 /
  **CLAR-02**). Une demande soumise par un utilisateur rattaché est associée à la fois à son auteur
  individuel et à la société propriétaire, permettant les deux filtres ci-dessus (**CLAR-01**).
- **FR-011** (ENT-04, Should) : Le système DEVRAIT journaliser toute action d'un administrateur de
  société sur ses collaborateurs (ajout, retrait, modification de droits), en réutilisant le journal
  d'audit existant de Feature 001.

### Key Entities *(le référentiel manipule des données ; aucune structure de table ni classe n'est
définie ici — ce choix appartient à `/speckit.plan`)*

- **Compte client (Société)** : entité principale de ce référentiel (`docs/cdc_text.txt` §10.1).
  Porte un profil (normal, en compte, deposit, IES — CPT-02), un état de validation (en attente,
  actif, refusé), et les informations/justificatifs demandés à la création (liste paramétrable, non
  fixée ici). Regroupe un ou plusieurs utilisateurs rattachés.
- **Utilisateur rattaché** : une personne physique associée à exactement un compte société (voir
  Assumptions), avec un rôle (Administrateur de société ou Utilisateur de société — DEFINED_ROLE
  d'après `docs/00-analyse-cdc.md` §4.1), un état (actif/désactivé), et des droits paramétrables de
  consultation (ses demandes uniquement / toutes celles de la société) et de soumission.
- **Demande de création de compte** : l'objet transitoire entre la soumission par le client et la
  décision (validation ou rejet) de l'administrateur fonctionnel CIT — distincte du compte lui-même,
  qui n'existe à l'état actif qu'après validation.
- **Décision de validation/modification de compte** : trace de la décision de CIT (validation,
  rejet, ou modification ultérieure du profil), avec auteur, date/heure et motif le cas échéant —
  portée par le journal d'audit existant de Feature 001, pas par une entité propre à cette feature.
- **Droit de consultation / droit de soumission** : habilitations paramétrables portées par le
  mécanisme générique de Feature 001 (`HabilitationUtilisateur`), appliquées dans le contexte d'un
  compte société.

## Cloisonnement *(section critique — critères vérifiables côté serveur)*

Conformément à la demande explicite de traiter ce sujet avec rigueur, et en s'appuyant strictement
sur CPT-04, ENT-03, `docs/00-analyse-cdc.md` §4.1 (« Ne voit que ses propres tickets ») et §6.8 :

| Question | Réponse dérivée du CDC | Référence |
|---|---|---|
| Ce qu'un client (société) peut consulter | Uniquement les données rattachées à son propre compte société — jamais celles d'un autre compte société, quel que soit son profil | CPT-04, SEC-03 |
| Ce qu'un utilisateur d'une société peut consulter | Dépend du droit qui lui a été accordé par l'administrateur de sa société : « ses demandes uniquement » (par défaut, valeur la plus restrictive tant qu'aucun droit explicite n'est accordé — Principe VII, moindre privilège) ou « l'ensemble des demandes de sa société » ; un **administrateur de société** voit toujours l'ensemble des demandes de sa société, quel que soit son propre droit individuel (**CLAR-02**) | ENT-02, ENT-03 |
| Ce que plusieurs utilisateurs de la même société partagent | Le compte société lui-même (profil, informations déclarées) est visible par tous les utilisateurs rattachés à titre informatif ; le partage des **demandes/dossiers** dépend strictement du droit de consultation individuel (ENT-03) — **pas de partage automatique et inconditionnel des dossiers entre collaborateurs** | ENT-02, ENT-03 |
| Ce qui reste individuel | L'identité propre de l'utilisateur (ses identifiants d'authentification, CPT-03) ; ses demandes propres si son droit de consultation est restreint | CPT-03, ENT-03 |
| Ce que les agents CIT peuvent consulter | Le CDC ne restreint pas la consultation du référentiel de comptes pour les rôles CIT habilités (administrateur fonctionnel — validation/gestion des comptes, `docs/00-analyse-cdc.md` §6.8/§17.3) ; cette consultation reste soumise au moindre privilège général déjà porté par Feature 001 (habilitation requise, jamais un accès non contrôlé) | CPT-01, CPT-06, Principe VII |
| Périmètres administratifs éventuels | Aucun périmètre administratif spécifique aux comptes n'est décrit par le CDC au-delà du rôle générique « Administrateur fonctionnel » (DEFINED_ROLE unique, sans sous-périmètre par société) | `docs/00-analyse-cdc.md` §4.2 |

**Non-négociable** : une restriction affichée uniquement dans l'interface ne constitue jamais une
autorisation suffisante (Principe VII de la constitution). Chaque critère du tableau ci-dessus DOIT
pouvoir être vérifié indépendamment de l'interface utilisée — voir les scénarios négatifs des User
Stories 1, 3 et 4 ci-dessus, qui serviront de base aux tests d'autorisation lors de
`/speckit.tasks`.

## Multi-utilisateurs par société *(analyse des conséquences fonctionnelles)*

| Question posée | Le CDC tranche-t-il ? | Réponse retenue | Référence |
|---|---|---|---|
| Partage des tickets/dossiers de la société | ✅ Oui | Conditionné au droit de consultation individuel (ENT-03) — jamais un partage automatique et inconditionnel | ENT-02, ENT-03 |
| Partage des documents | ⚠️ Non explicite, mais déduit sans ambiguïté | Un document est rattaché à un ticket/dossier (SOC-02, Feature 001) ; s'il voit le dossier, l'utilisateur voit ses documents — corollaire direct d'ENT-03, pas une règle distincte | ENT-03 (déduit) |
| Partage de l'historique | ⚠️ Non explicite, mais déduit sans ambiguïté | Même raisonnement que les documents — l'historique fonctionnel d'un ticket suit la visibilité du ticket lui-même | ENT-03 (déduit) |
| Droits individuels | ✅ Oui | Chaque utilisateur rattaché a ses propres droits de consultation et de soumission, paramétrés individuellement par l'administrateur de société — pas un droit unique appliqué à toute la société | ENT-02 |
| Soumission au nom de la société | ✅ Résolu par clarification | Modèle hybride : la demande est rattachée à la société (identifiant de cloisonnement) et porte un champ auteur individuel distinct — voir **CLAR-01** | ENT-02, ENT-03, **CLAR-01** |
| Visibilité de l'administrateur de société | ✅ Résolu par clarification | L'administrateur voit automatiquement toutes les demandes de sa société, indépendamment de son propre droit de consultation individuel — voir **CLAR-02** | ENT-02, ENT-03, **CLAR-02** |
| Consultation des demandes créées par un autre utilisateur de la même société | ✅ Oui | Explicitement le sens même d'ENT-03 (« l'ensemble des demandes de sa société ») lorsque ce droit est accordé | ENT-03 |

**Le point « soumission au nom de la société » a été résolu par la session `/speckit.clarify` du
2026-08-29** (voir section « Clarifications », CLAR-01 et CLAR-02) plutôt que par un choix
arbitraire.

## Rôles et habilitations

Conformément à la consigne de ne pas inventer de nouveaux rôles, cette feature réutilise
exclusivement la classification déjà validée dans `docs/00-analyse-cdc.md` :

| Rôle | Classification | Source |
|---|---|---|
| Utilisateur de société | DEFINED_ROLE (§4.1) | `docs/00-analyse-cdc.md` |
| Administrateur de société | DEFINED_ROLE (§4.1) | `docs/00-analyse-cdc.md` |
| Administrateur fonctionnel (CIT) | DEFINED_ROLE (§4.2) | `docs/00-analyse-cdc.md` (déjà utilisé par Feature 001) |

**PC-03 reste une source de vigilance** : aucun des rôles CIT non définis en section 6 du CDC
(Caisse/Finance, Agent de guichet, Intervenant Avoir/Exo) n'intervient dans cette feature — ils
n'apparaissent dans aucune des 11 exigences sélectionnées.

**« Droit de soumission » et « droit de consultation » (ENT-02) sont modélisés comme des
habilitations paramétrables, pas comme de nouveaux rôles RBAC** : conformément à l'exemple donné
dans la demande de spécification, un droit fonctionnel ne devient pas automatiquement un rôle. La
décision technique de représentation (attribut sur l'utilisateur, permission dédiée, ou autre)
appartient à `/speckit.plan`.

## Sécurité *(critères fonctionnels — l'implémentation technique appartient à `/speckit.plan`)*

| Critère | Exigence CDC référencée | Application dans cette feature |
|---|---|---|
| Authentification | CPT-03 (dans le périmètre), SEC-01 (cité, hors périmètre — générique à toute la plateforme) | Le titulaire d'un compte client DOIT pouvoir s'authentifier et récupérer l'accès à son compte de façon autonome |
| Autorisation | SEC-02 (cité, réutilisé de Feature 001) | Toute action de gestion de compte/utilisateur (valider, modifier, activer/désactiver, consulter) DOIT être vérifiée côté serveur selon l'habilitation de l'appelant |
| Cloisonnement | CPT-04, ENT-03 (dans le périmètre), SEC-03 (cité) | Voir section « Cloisonnement » ci-dessus |
| Moindre privilège | Principe VII de la constitution | Un droit de consultation non explicitement accordé DOIT être traité comme restrictif par défaut (« ses demandes uniquement »), jamais permissif par défaut |
| Confidentialité | SEC-04, SEC-06 (cités, hors périmètre technique — chiffrement, ARTCI) | Les informations de compte (identification, contact) sont traitées comme sensibles ; leur protection technique (chiffrement, conservation) reste `/speckit.plan`/PC-14 |
| Audit des opérations sensibles | SOC-13/SEC-05 (réutilisé intégralement de Feature 001), ENT-04 (dans le périmètre) | Validation de compte, modification de profil, gestion de collaborateurs DOIVENT produire une entrée d'audit |
| Désactivation | ENT-02 (dans le périmètre), SEC-11 (cité par analogie — désactivation de compte agent, non identique mais même principe de non-suppression de l'historique) | Un utilisateur désactivé NE DOIT PAS pouvoir s'authentifier ; son historique n'est jamais supprimé |
| Accès aux données de société | ENT-03, CPT-04 | Voir section « Cloisonnement » |

Une restriction affichée uniquement dans l'interface NE constitue JAMAIS une autorisation
suffisante (Principe VII, non négociable).

## Données personnelles *(principe de minimisation)*

Le CDC (CPT-01) dit explicitement que « les informations et justificatifs exigés sont
paramétrables » — **aucune liste figée d'informations personnelles n'est imposée par le CDC** à ce
niveau de détail. Conformément au principe de minimisation demandé :

- **Aucune donnée personnelle n'est inventée par cette spécification** : ni date de naissance, ni
  sexe, ni adresse personnelle, ni numéro national d'identité, ni autre information sensible non
  explicitement exigée par le CDC.
- Les seules informations dont la nécessité peut être déduite sans ambiguïté du CDC lui-même sont :
  une **identité déclarative minimale** permettant à CIT d'instruire la demande (CPT-01), et un
  **identifiant de connexion** nécessaire à l'authentification (CPT-03). Le contenu exact et la
  liste complète des justificatifs restent un paramètre (PC-08), non fixé ici.
- SEC-06 (protection des données personnelles, conformité ARTCI) est cité comme critère transverse
  applicable à ce référentiel, sans que cette feature ne définisse elle-même la politique de
  conservation (PC-14, point déjà ouvert et non résolu ici).

## Testabilité et recette CDC

### Analyse de `docs/04-matrice-recette.md`

> Conformément à la consigne de ne jamais déduire l'applicabilité d'un REC-SCE de son seul
> identifiant, chaque scénario a été relu dans son contenu réel avant d'être retenu ou écarté.

| REC-SCE | Contenu réel (CDC §22) | Applicable à Feature 002 ? | Décision |
|---|---|---|---|
| REC-SCE-01 à REC-SCE-05 | Parcours Billing Office IMPORT/EXPORT complets (dépôt BL, contrôle, facturation, paiement, codification…) | ❌ Non — ces scénarios exercent CPT-02 **accessoirement**, au sein d'un parcours métier qui n'existe pas encore dans le périmètre livré par cette feature (même méthode que `recipe-scope-audit.md` de Feature 001) | Écarté |
| REC-SCE-06 à REC-SCE-08 | Simulation Proforma, Customer Complaint, Enrôlement | ❌ Non — aucun rapport avec les comptes/sociétés | Écarté |
| REC-SCE-09 à REC-SCE-13 | Prise en charge, transfert, réaffectation, temps/SLA, supervision | ❌ Non — périmètre du socle transverse (Feature 001), déjà couvert et clos, sans rapport avec les comptes | Écarté |
| REC-SCE-14 | « Transverse : notifications... continuité digital/présentiel... regroupement en dossier... **contrôle des habilitations (un client ne voit pas les tickets d'un autre ; un agent ne voit pas les files des autres rôles)**... journal d'audit » | ⚠️ **Partiellement** — seule la portion « un client ne voit pas les tickets d'un autre » exerce directement CPT-04 ; le reste du scénario (canaux de notification, continuité digital/présentiel, dossier, files par rôle) est hors périmètre de cette feature, exactement comme Feature 001 l'a déjà traité pour sa propre portion | **Retenu, portion CPT-04 uniquement** — TEST-CDC limité à cette portion |

**Aucun REC-SCE n'est réellement applicable à ENT-01/02/03/04, ni à CPT-01/02/03/05/06/07** — ce sont
des exigences nouvelles (comptes/sociétés) qui n'existaient pas encore comme concept exploitable
lors de la rédaction des scénarios de recette du CDC §22 (lesquels supposent des comptes déjà
existants pour dérouler un parcours Billing Office). Chacune de ces 10 exigences (sur 11) devra
recevoir un **TEST-SPEC** (scénario complémentaire dérivé des critères d'acceptation ci-dessus,
jamais présenté comme un scénario officiel du CDC) lors de `/speckit.tasks`.

**Nombre de Must sans scénario CDC applicable** : **7 sur 8** (CPT-01, CPT-02, CPT-03, CPT-05,
ENT-01, ENT-02, ENT-03 — cette dernière seulement partiellement couverte par la portion CPT-04 de
REC-SCE-14, donc comptée comme non couverte pour elle-même ; seule CPT-04 dispose d'une couverture
directe, quoique partielle, via REC-SCE-14).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001** : 100 % des tentatives d'accès à un compte société autre que le sien (par un client ou
  un utilisateur rattaché) sont refusées côté serveur, quel que soit le point d'entrée applicatif
  utilisé (zéro contournement constaté en recette).
- **SC-002** : Un compte société créé n'est jamais utilisable pour s'authentifier tant qu'un
  administrateur fonctionnel CIT ne l'a pas validé (100 % des cas vérifiés).
- **SC-003** : Le droit de consultation d'un utilisateur rattaché (ses demandes uniquement / toutes
  celles de la société) est systématiquement respecté dans 100 % des consultations, sans exception
  ni contournement.
- **SC-004** : 100 % des actions de gestion de collaborateurs par un administrateur de société
  (ajout, désactivation, modification de droits) produisent une entrée d'audit vérifiable.
- **SC-005** : Un utilisateur désactivé ne peut plus s'authentifier immédiatement après sa
  désactivation, tout en conservant l'intégralité de son historique.
- **SC-006** : La modification du profil d'un compte société par CIT n'entraîne, dans 0 % des cas
  vérifiés, un effet rétroactif observable sur les tickets déjà en cours.

## Assumptions

- Un utilisateur est rattaché à **exactement un** compte société à la fois (aucune exigence CDC ne
  décrit un rattachement multiple) — hypothèse de travail documentée, révisable si le métier
  l'infirme.
- Le premier utilisateur à l'origine d'une demande de compte société devient, par défaut, le premier
  « Administrateur de société » de ce compte une fois celui-ci validé — valeur par défaut
  raisonnable non contredite par le CDC, qui ne précise pas explicitement ce point.
- Une société peut compter un seul utilisateur, qui est alors nécessairement son propre
  administrateur de société — cas limite valide, non exclu par le CDC (§10.1 décrit une capacité
  multi-utilisateurs, jamais une obligation d'en avoir plusieurs).
- Le rôle « Administrateur fonctionnel » (CIT) qui valide les comptes et modifie les profils est le
  même rôle générique déjà utilisé par Feature 001, sans périmètre restreint par société (le CDC ne
  décrit aucun périmètre administratif par société pour ce rôle).
- Les informations et justificatifs exigés à la création d'un compte (contenu exact de PC-08) ne
  sont pas fixés par cette spécification — ils resteront un paramètre configurable, à l'image de la
  matrice des transitions ou des seuils SLA de Feature 001.
- Le mécanisme concret de rejet d'une demande de compte (motif obligatoire, notification) est traité
  par analogie avec les mécanismes déjà validés de Feature 001 (rejet avec motif obligatoire,
  SOC-06) — le CDC ne détaille pas ce cas précis pour les comptes, mais le principe général de
  traçabilité des décisions (Principe IX) s'applique.

## Clarifications nécessaires avant planification

> Chaque point est classé, et les PC-xx existants sont réutilisés lorsqu'ils couvrent exactement le
> sujet. Aucune ambiguïté métier n'est résolue à la place du métier.

| # | Point | Source CDC | Exigences concernées | Question | Impact | Classification |
|---|---|---|---|---|---|---|
| PC-08 | Liste exacte des pièces/informations exigées à la création d'un compte (par type de client) | §25 du CDC, déjà référencé par `docs/02-points-a-clarifier.md` | CPT-01 | Quelles informations et quels justificatifs sont réellement exigés, par profil (normal/en compte/deposit/IES) ? | Détermine le formulaire de demande de compte et les règles de validation | **NON BLOQUANTE POUR PLAN** *(le mécanisme reste paramétrable sans développement — CPT-01)* / **À CONFIRMER AVANT RECETTE/PRODUCTION** |
| PC-03 | Rôles non définis en section 6 du CDC | `docs/02-points-a-clarifier.md` | Aucune des 11 exigences de cette feature (rappel de vigilance uniquement — Administrateur fonctionnel et Administrateur de société sont, eux, des DEFINED_ROLE confirmés) | Sans objet pour cette feature — cité par prudence | Aucun | **NON BLOQUANTE POUR PLAN** *(sans objet direct — vigilance uniquement)* |
| PC-14 | Politique de conservation des données personnelles/comptes | `docs/02-points-a-clarifier.md` | CPT-01, ENT-02 (désactivation sans suppression) | Quelle est la durée de conservation d'un compte désactivé ou refusé ? | Détermine si/quand une donnée de compte peut être purgée | **NON BLOQUANTE POUR PLAN** / **À CONFIRMER AVANT PRODUCTION** |
| **NOUVEAU-01** *(aucun PC-xx existant ne couvre exactement ce sujet — voir note ci-dessous)* | Soumission d'une demande « au nom de la société » | `docs/cdc_text.txt` §10.1, ENT-02 | ENT-02, ENT-03 | Lorsqu'un utilisateur rattaché soumet une demande, celle-ci porte-t-elle uniquement son identité individuelle, ou explicitement celle de la société (ou les deux) ? Cette réponse conditionne directement l'implémentation d'ENT-03 (visibilité « toutes les demandes de la société ») | Détermine le modèle d'attribution d'une demande à un compte société vs à un utilisateur individuel — impact direct sur `/speckit.plan` | ✅ **RÉSOLU (CLAR-01, CLAR-02 — session 2026-08-29)** *(anciennement BLOQUANTE POUR PLAN)* |
| **NOUVEAU-02** | Devenir du dernier administrateur de société désactivé | `docs/cdc_text.txt` §10.1 (Edge Case) | ENT-02 | Une société peut-elle se retrouver sans aucun administrateur actif ? Un transfert automatique du rôle est-il prévu, ou la désactivation du dernier administrateur doit-elle être empêchée ? | Détermine une règle de garde sur la désactivation | ✅ **RÉSOLU (CLAR-03 — session 2026-08-29)** *(anciennement NON BLOQUANTE POUR PLAN / À CONFIRMER AVANT IMPLEMENTATION)* |
| **NOUVEAU-03** | Changement de rattachement d'un utilisateur à une autre société | Absence de mention CDC | ENT-01 (cas d'usage #12 de la demande de spécification) | Un utilisateur peut-il changer de société de rattachement, et selon quel processus (décision de qui) ? | Détermine si une fonctionnalité de « transfert » doit être conçue | **NON BLOQUANTE POUR PLAN** *(absence totale de traitement CDC — peut être différé)* / **À CONFIRMER AVANT IMPLEMENTATION** |

> **Note sur NOUVEAU-01/02/03** : aucun point existant de `docs/02-points-a-clarifier.md` (PC-01 à
> PC-26) ne couvre exactement ces trois sujets — vérifié par relecture intégrale de ce document.
> Conformément à la consigne de ne pas modifier le CDC ni les documents d'analyse partagés sans
> demande explicite, ces trois points sont documentés **ici, dans le périmètre de cette feature**,
> plutôt que d'être ajoutés unilatéralement à `docs/02-points-a-clarifier.md`. Il est recommandé à
> l'équipe/au maître d'ouvrage de les faire formellement intégrer à ce document partagé (à la suite
> de PC-26) s'ils sont confirmés comme des points de clarification durables, à l'image de la
> révision qui avait ajouté PC-24 à PC-26.

## Clarifications

> Session `/speckit.clarify` du **2026-08-29**. Chaque décision suit la traçabilité :
> Question → Réponse → Exigences concernées → User Story → Critère(s) d'acceptation.
>
> **Mode de validation** : les trois décisions (CLAR-01 à CLAR-03) ont été présentées avec une
> recommandation ancrée dans le CDC et la cohérence avec Feature 001 ; l'utilisateur a validé
> chaque recommandation proposée. **Aucune règle métier nouvelle n'est inventée ; aucune exigence
> du CDC n'est modifiée** ; les décisions techniques de représentation (schéma, claims, policies)
> restent réservées à `/speckit.plan`.

### CLAR-01 — Propriété d'une demande soumise « au nom de la société » (NOUVEAU-01)

- **Question** : lorsqu'un utilisateur rattaché soumet une demande, celle-ci appartient-elle à la
  société (avec l'auteur individuel tracé séparément), à l'utilisateur individuellement, ou à la
  société sans auteur distinct ?
- **Réponse** : modèle **hybride**. La demande (dossier/ticket, porté par Feature 001) est
  rattachée à la société — ce rattachement société est l'identifiant utilisé pour le cloisonnement
  (extension du `ClientId` actuel de `Dossier`, aujourd'hui un identifiant abstrait unique sans
  distinction individu/société — voir `src/CIT.Plateforme.Domain/Tickets/Dossier.cs`) — et porte en
  plus un champ auteur individuel distinct, permettant de satisfaire simultanément : le
  cloisonnement inter-société (CPT-04, le `ClientId` reste toujours vérifié comme aujourd'hui), le
  filtre « ses propres demandes uniquement » (ENT-03, comparaison auteur = utilisateur courant), et
  le filtre « toutes les demandes de la société » (ENT-03, comparaison société = société de
  l'utilisateur courant). Aucune des deux autres options n'aurait permis de satisfaire les deux
  filtres Must d'ENT-03 simultanément. Justification : conséquence directe et nécessaire de la
  combinaison CPT-04 + ENT-03 telle qu'écrite dans le CDC (`docs/cdc_text.txt` §10.1) ; ne crée
  aucune règle métier nouvelle, précise uniquement l'attribution d'un identifiant déjà requis.
- **Exigences** : ENT-02 (FR-009), ENT-03 (FR-010), CPT-04 (FR-004)
- **User Story** : US3 (scénario 1 précisé), US4 (scénarios 2-3 précisés)
- **Critères d'acceptation** : US4 scénarios 2 et 3 ; section « Cloisonnement » ; section
  « Frontière avec Feature 001 » point 1 (extension précisée) ; ligne « Soumission au nom de la
  société » de la section « Multi-utilisateurs par société » marquée résolue.

### CLAR-02 — Visibilité automatique de l'administrateur de société (dérivé de NOUVEAU-01)

- **Question** : l'administrateur d'une société voit-il automatiquement toutes les demandes de sa
  société, même si son propre droit de consultation individuel est réglé sur « ses demandes
  uniquement » ?
- **Réponse** : **oui**. Le rôle Administrateur de société porte de facto le droit de consultation
  « toutes les demandes de la société », indépendamment de tout paramétrage individuel de droit de
  consultation qu'il pourrait par ailleurs détenir comme n'importe quel utilisateur rattaché. Ce
  privilège est lié au rôle lui-même (non retirable indépendamment du rôle), cohérent avec sa
  responsabilité de gestion des collaborateurs et de leurs droits (ENT-02) : un administrateur ne
  pourrait pas exercer cette responsabilité s'il ne voyait pas l'activité de ses collaborateurs.
- **Exigences** : ENT-02 (FR-009), ENT-03 (FR-010)
- **User Story** : US4 (nouveau scénario d'acceptation 5)
- **Critères d'acceptation** : nouveau scénario US4 : *Given un administrateur de société dont le
  droit de consultation individuel est « ses demandes uniquement », When il consulte les demandes
  de sa société, Then il voit l'ensemble des demandes de sa société malgré son propre droit
  individuel restreint (ENT-02, ENT-03)*.

### CLAR-03 — Gouvernance du rôle Administrateur de société (NOUVEAU-02)

- **Question** : une société peut-elle se retrouver sans aucun administrateur actif, et plusieurs
  administrateurs peuvent-ils coexister pour une même société ?
- **Réponse** : **plusieurs administrateurs peuvent coexister** pour une même société (le CDC ne
  l'exclut pas — « un administrateur... gère » ne signifie pas « un seul »). Le système DOIT
  empêcher la désactivation du compte du dernier administrateur actif d'une société, ainsi que le
  retrait de son rôle d'administrateur (par lui-même ou par un tiers), tant qu'aucun autre
  administrateur actif n'existe pour cette société — garde-fou préventif, le CDC ne décrivant aucun
  mécanisme de transfert automatique du rôle ni d'intervention CIT pour ce cas.
- **Exigences** : ENT-02 (FR-009)
- **User Story** : US3 (nouveau scénario d'acceptation 6)
- **Critères d'acceptation** : nouveau scénario US3 : *Given une société avec un seul administrateur
  actif, When une tentative de le désactiver ou de retirer son rôle d'administrateur est effectuée
  (par lui-même ou par un tiers habilité), Then l'opération est refusée avec un motif explicite
  (ENT-02)* ; Edge Case « dernier Administrateur de société désactivé » marqué résolu.

## Hors périmètre Feature 002

- Workflow Billing Office (orientation selon le profil client, contrôle BL, codification export) —
  IMP-*, EXP-*, UNI-* : Feature 002 expose le profil du compte (CPT-02), elle ne décide d'aucune
  règle de routage de ticket.
- Facturation, paiement, lettrage — portés par les futures features Billing Office IMPORT/EXPORT.
- Simulation de Proforma (SIM-*) — future feature dédiée.
- Avoir et Exonération (AVE-*) — future feature dédiée.
- Customer Complaint (REC-*) — future feature dédiée.
- Enrôlement Transporteurs (ENR-*) — future feature dédiée ; le « compte transporteur/chauffeur »
  mentionné par `docs/cdc_text.txt` §20 est un objet distinct du compte client/société de cette
  feature, non traité ici.
- Rendez-vous (CON-*) — future feature dédiée.
- Assistant IA (IA-*) — future feature dédiée.
- Notifications multicanales (COM-*, NOT-*) — future feature dédiée ; cette feature ne fait
  qu'exposer les données de contact nécessaires à l'identification du compte, sans définir de canal
  ni de modèle de notification.
- Toute règle spécifique à une future demande métier (ex. quels justificatifs pour une demande
  IMPORT particulière) — hors périmètre, seul le référentiel générique de comptes est ici concerné.
- Gestion des comptes **agents CIT** (SEC-11, rôles internes DEFINED_ROLE de la section 4.2 du CDC)
  — hors périmètre : le titre de cette feature (« comptes clients, sociétés et utilisateurs
  rattachés ») désigne exclusivement le côté client ; la gestion des comptes agents relève d'un
  concept CDC distinct (§4.2), non repris dans le groupe CPT/ENT et non traité ici.
- Décisions techniques d'implémentation (ASP.NET Core Identity, claims, policies, schéma SQL
  détaillé) — `/speckit.plan`.

## Matrice de couverture — Exigence CDC → User Story → Critère d'acceptation → REC-SCE → Couverture

| Exigence CDC | Priorité | User Story | Critère(s) d'acceptation (FR) | REC-SCE éventuel | Couverture |
|---|---|---|---|---|---|
| CPT-01 | Must | US1 | FR-001, scénarios 1-4 | Aucun | **Absente (TEST-SPEC requis)** |
| CPT-02 | Must | US1 | FR-002, scénarios 1-2 | Aucun *(REC-SCE-01→05 hors périmètre F002, voir analyse recette)* | **Absente (TEST-SPEC requis)** |
| CPT-03 | Must | US2 | FR-003, scénarios 1-4 | Aucun | **Absente (TEST-SPEC requis)** |
| CPT-04 | Must | US4 | FR-004, scénario 1 | REC-SCE-14 (portion cloisonnement client uniquement) | **Partielle** |
| CPT-05 | Must | US3 | FR-005 (renvoi à ENT-01→04) | Aucun | **Absente (TEST-SPEC requis)** |
| CPT-06 | Should | US5 | FR-006, scénarios 1-3 | Aucun | **Absente (TEST-SPEC requis)** |
| CPT-07 | Could | US6 | FR-007, scénario 1 | Aucun | **Absente (TEST-SPEC requis)** |
| ENT-01 | Must | US3 | FR-008, scénario 1 | Aucun | **Absente (TEST-SPEC requis)** |
| ENT-02 | Must | US3 | FR-009, scénarios 1-3, 6 (**CLAR-03**) | Aucun | **Absente (TEST-SPEC requis)** |
| ENT-03 | Must | US4 | FR-010, scénarios 2-3, 5 (**CLAR-01, CLAR-02**) | Aucun *(distinct de la portion CPT-04 de REC-SCE-14)* | **Absente (TEST-SPEC requis)** |
| ENT-04 | Should | US3 | FR-011, scénario 4 | Aucun | **Absente (TEST-SPEC requis)** |

**Aucune exigence du groupe C (CPT/ENT) de `docs/01-matrice-exigences.md` n'est absente de cette
matrice** : les 11 lignes ci-dessus couvrent exhaustivement CPT-01→07, ENT-01→04.

## Contrôle de couverture *(vérification mécanique finale)*

| # | Contrôle | Résultat |
|---|---|---|
| 1 | Toutes les exigences sélectionnées sont présentes dans le CDC | ✅ 11/11 vérifiées mot pour mot dans `docs/01-matrice-exigences.md` |
| 2 | Aucune référence inventée | ✅ Aucune référence `CPT-`/`ENT-` au-delà de celles listées dans le CDC ; aucune référence d'un autre groupe (SEC-, COM-, WKF-…) n'est présentée comme FR de cette feature — seulement citée comme critère transverse |
| 3 | Priorités identiques à `docs/01-matrice-exigences.md` | ✅ CPT-01/02/03/04/05 = Must, CPT-06 = Should, CPT-07 = Could, ENT-01/02/03 = Must, ENT-04 = Should — vérifié ligne à ligne |
| 4 | Aucune exigence Feature 001 réimplémentée inutilement | ✅ Audit/habilitations/cloisonnement générique explicitement réutilisés (§« Frontière avec Feature 001 ») — aucune FR de cette feature ne recrée un système déjà livré |
| 5 | Aucune exigence d'une future feature absorbée artificiellement | ✅ IMP-*/EXP-*/SIM-*/AVE-*/REC-*/ENR-*/CON-*/IA-*/COM-*/NOT-* explicitement exclues (§« Hors périmètre ») |
| 6 | Chaque Must possède un critère d'acceptation | ✅ Les 8 Must (CPT-01,02,03,04,05, ENT-01,02,03) ont chacun au moins un scénario Given/When/Then dans les User Stories 1 à 4 |
| 7 | Chaque REC-SCE cité est réellement applicable | ✅ Seul REC-SCE-14 est cité, et uniquement pour sa portion CPT-04 réellement exercée — jamais son intégralité, jamais un autre REC-SCE dont l'identifiant seul aurait pu suggérer une pertinence trompeuse (REC-SCE-01→05 explicitement examinés et écartés) |

## Conformité à la constitution (`.specify/memory/constitution.md`)

| # | Principe | Statut | Justification |
|---|---|---|---|
| I | Stack Technique Imposée | N/A à ce stade | Aucune décision technique prise dans cette spécification (réservé à `/speckit.plan`) |
| II | Source de Vérité et Traçabilité | ✅ PASS | Les 11 exigences conservent leurs références CDC d'origine ; chaîne CDC→FR→US→critère tracée intégralement |
| III | Priorisation Must/Should/Could | ✅ PASS | Priorités reprises à l'identique de `docs/01-matrice-exigences.md`, aucune reprioritisation |
| IV | Principes d'Architecture | N/A à ce stade | Réservé à `/speckit.plan` — cette spec ne définit aucune structure technique |
| V | Domaine Métier : Ticket et Capacités Transverses | ✅ PASS | Cette feature ne redéfinit ni ticket, ni dossier, ni workflow — elle ajoute un référentiel de comptes consommé en amont |
| VI | Workflows Paramétrables par Conception | ✅ PASS | Informations/justificatifs de création de compte (CPT-01), droits de consultation/soumission (ENT-02) explicitement gardés paramétrables, non codés en dur |
| VII | Sécurité (NON-NÉGOCIABLE) | ✅ PASS | Cloisonnement et autorisation systématiquement définis comme vérifiables côté serveur (§Cloisonnement, §Sécurité) ; aucune restriction UI-seule acceptée |
| VIII | Données et Persistance | N/A à ce stade | Modèle physique réservé à `/speckit.plan` ; principe de non-suppression de l'historique déjà rappelé (US3, US4) |
| IX | Audit et Traçabilité Opérationnelle | ✅ PASS | ENT-04 explicitement rattaché au journal d'audit existant de Feature 001 ; aucune réimplémentation |
| X | Gestion des Documents | N/A | Cette feature ne traite aucun document (réutilise Feature 001 lorsque des documents seront rattachés à un compte via un module futur) |
| XI | Intégrations et Périmètre Autonome | ✅ PASS | Aucune intégration externe introduite |
| XII | Tests et Stratégie de Validation | ✅ PASS | Chaque Must dispose d'un critère d'acceptation ; les 7 Must sans scénario CDC applicable sont explicitement identifiés pour recevoir un TEST-SPEC lors de `/speckit.tasks`, jamais présentés comme facultatifs |
| XIII | Performance et Exploitation | ✅ PASS | Aucune valeur métier non définie n'est inventée (PC-08 non tranché, informations de compte non fixées) |
| XIV | Observabilité | N/A à ce stade | Réservé à `/speckit.plan` |
| XV | Gestion des Points Non Résolus (NON-NÉGOCIABLE) | ✅ PASS | PC-08, PC-14 explicitement rappelés comme non résolus (paramètres, non bloquants) ; NOUVEAU-01 et NOUVEAU-02 résolus par la session `/speckit.clarify` du 2026-08-29 (CLAR-01/02/03), chaque décision ancrée dans le CDC, jamais tranchée arbitrairement ; NOUVEAU-03 reste ouvert, non bloquant |
| XVI | Definition of Done | N/A à ce stade | S'applique à `/speckit.implement`, pas à cette spécification |
| XVII | Gouvernance des Écarts d'Implémentation | ✅ PASS | Aucun écart à la constitution ni au CDC introduit ; les besoins d'extension du socle Feature 001 sont documentés, jamais appliqués silencieusement |
