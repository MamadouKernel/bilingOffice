# Feature Specification: Billing Office IMPORT & Unicité BL

**Feature Branch**: `003-billing-office-import`

**Created**: 2026-08-30

**Status**: Draft — spécification initiale, en attente de validation humaine avant `/speckit.clarify`/`/speckit.plan`

**Input**: User description : « Billing Office IMPORT & Unicité BL — spécifier le parcours de
soumission et de traitement des demandes d'importation (multi-BL), le contrôle d'unicité des
numéros de BL, l'instruction au Pilote Manifeste, l'aiguillage automatique selon le type de client,
et les quatre circuits de traitement (Normal, En Compte, Deposit, IES), en réutilisant intégralement
les socles Feature 001 (tickets/dossiers/workflows) et Feature 002 (comptes/sociétés/TypeClient),
tous deux CLOSED. »

## Sources de vérité

- `docs/CDC_Plateforme_Digitale_CIT_v3.docx` — cahier des charges, source de vérité métier unique
- `docs/cdc_text.txt` — texte brut du CDC (section 11.2 « Billing Office IMPORT », lignes 480-660 ;
  section CON, lignes 777-789)
- `docs/00-analyse-cdc.md` §6.9 « Billing Office — IMPORT » (tronc commun, contrôle d'unicité,
  variantes par type de client, traitement parallèle IES, statuts paramétrables)
- `docs/01-matrice-exigences.md` — groupe **D — Billing Office IMPORT (IMP / UNI)**, lignes 128-156
  et 335 (récapitulatif)
- `docs/02-points-a-clarifier.md` — PC-03, PC-05, PC-08, PC-09, PC-10, PC-19
- `docs/03-revue-coherence-analyse.md` — correction C-05 (IMP-15 / dépendance CON-01)
- `docs/04-matrice-recette.md` — REC-SCE-01 à REC-SCE-04 (texte exact), tableau de couverture Must
  (lignes 153-175)
- `docs/05-roadmap-features.md` — analyse de séquençage (utilisée comme point de départ, **vérifiée
  et corrigée** ci-dessous, jamais recopiée telle quelle — voir Étape 0)
- `.specify/memory/constitution.md` — principes non négociables du projet (v1.0.0)
- `specs/001-socle-tickets-workflows/spec.md`, `closure-review.md` — Feature 001, **CLOSED**
  (commit baseline `a9657a4`), consultée pour identifier les capacités transverses déjà livrées
- `specs/002-comptes-clients-societes/spec.md`, `closure-review.md` — Feature 002, **CLOSED**
  (commit de clôture `46b2605`), consultée pour le modèle de compte/société/TypeClient déjà livré

Feature 001 et Feature 002 sont **CLOSED** et ne sont **pas modifiées** par cette spécification.

## Étape 0 — Garde de périmètre obligatoire et détermination mécanique du périmètre réel

> Méthode identique à celle appliquée en Feature 002 : chaque exigence a été relue dans son texte
> intégral (`docs/cdc_text.txt`, `docs/00-analyse-cdc.md` §6.9), jamais sélectionnée sur la seule
> base de son préfixe (`IMP-`/`UNI-`) ni sur la seule base du regroupement proposé par
> `docs/05-roadmap-features.md`.

### 0.1 Contradiction IMP-15 — analyse et décision

`docs/05-roadmap-features.md` compte IMP-15 dans les « 25 exigences » de Feature 003 (§3, §6) **mais**
le classe simultanément « OUT OF SCOPE » dans une future Feature 009 « Continuité Digital/Présentiel »
(§8.2, §10). Résolution, à partir des sources primaires (jamais de la roadmap seule) :

| Source | Ce qu'elle dit d'IMP-15 |
|---|---|
| `docs/cdc_text.txt` lignes 548-550 | IMP-15 est numérotée et positionnée **structurellement à l'intérieur** de la section Billing Office IMPORT du CDC (juste après IMP-14, juste avant la section « Client en compte » IMP-20) |
| `docs/01-matrice-exigences.md` ligne 143 | Colonne **Module = « Billing IMPORT »** (pas « Continuité ») ; priorité **Should** ; note explicite : *« IMP-15 est une instanciation de CON-01 dans le module Billing Office — CON-01 (Must) rend IMP-15 possible »* |
| `docs/02-points-a-clarifier.md` PC-10 | Point classé **« Workflow Billing Office »** (pas module Continuité), sources croisées IMP-15 (Should) · CON-01-03 |
| `docs/03-revue-coherence-analyse.md` C-05 | Correction explicite : *« la User Story IMP-15 ne doit pas bloquer CON-01 »* — CON-01 (Must, capacité générale) **active** IMP-15 (Should, instanciation Billing Office spécifique) ; ce n'est pas IMP-15 qui appartient au module CON-01 |

**Décision (Option A)** : IMP-15 appartient fonctionnellement à Billing Office IMPORT et **reste dans
le périmètre de Feature 003**, en tant qu'exigence **Should**. Toutefois, sa réalisation dépend
structurellement de CON-01 (recherche/prise de rendez-vous, capacité générale non liée à un module),
qui n'est **pas construite par Feature 001, ni par Feature 002, ni par Feature 003** (elle appartient
à la future feature « Continuité Digital/Présentiel »). Feature 003 spécifie donc **uniquement** la
part Billing-Office d'IMP-15 : permettre au client de déclarer son intention de finaliser
physiquement à l'issue du contrôle BL, et faire porter cette trace par le ticket — **sans** construire
le mécanisme général de prise de rendez-vous/reprise au guichet (CON-01/02/03), décrit comme une
dépendance externe non implémentée (voir « Hors périmètre »).

### 0.2 Contradiction UNI-04 — analyse et décision (découverte additionnelle, non signalée par la roadmap)

En vérifiant chaque exigence individuellement (consigne du garde de périmètre), une **seconde**
contradiction est apparue, non signalée par `docs/05-roadmap-features.md` ni explicitement demandée
par cette tâche, mais découverte par la même méthode mécanique que celle exigée pour IMP-15 :

| Source | Ce qu'elle dit d'UNI-04 |
|---|---|
| `docs/01-matrice-exigences.md` ligne 156 (ligne individuelle) | Colonne **Module = « Billing EXPORT »** (pas IMPORT) ; description : *« Pour l'EXPORT, une règle équivalente sur le numéro de booking pourra être mise en œuvre après validation métier »* ; dépendance `UNI-01 [D]` (déduite, pas explicite) |
| `docs/01-matrice-exigences.md` ligne 335 (en-tête de groupe D) | Compte UNI-04 dans le total du groupe « Billing Office IMPORT (IMP, UNI) = 25 » — **en tension directe** avec la ligne individuelle ci-dessus |
| `docs/04-matrice-recette.md` ligne 36 | UNI-04 est couverte par **REC-SCE-05 « EXPORT »**, jamais par REC-SCE-01 à 04 (IMPORT) ; REC-SCE-01 (IMPORT client normal) cite explicitement UNI-01 · UNI-02 · UNI-03 — **jamais UNI-04** |
| `docs/02-points-a-clarifier.md` PC-09 | Classée *« Module EXPORT »* ; question ouverte : *« Faut-il implémenter UNI-04 dès le premier incrément ou dans un incrément ultérieur ? »* — le point lui-même envisage un report |
| `docs/00-analyse-cdc.md` §6.9, tableau « Contrôle d'unicité BL » | UNI-04 y est listée comme *« Règle équivalente sur numéro de booking pour l'EXPORT »* — donc explicitement une règle **EXPORT**, mentionnée dans le même tableau qu'UNI-01/02/03 uniquement par proximité de numérotation, pas par appartenance fonctionnelle |

**Analyse** : la même situation que IMP-15/CON-01 se reproduit — un regroupement par préfixe
numérique (`UNI-01` à `UNI-04` forment une suite contiguë dans le CDC) est confondu avec une
appartenance fonctionnelle. Le contenu réel de chaque ligne individuelle (module, dépendance,
couverture REC-SCE, classification PC) désigne UNI-04 sans ambiguïté comme une exigence **Billing
Office EXPORT**, jamais IMPORT.

**Décision** : UNI-04 est **retirée du périmètre de Feature 003** et référencée uniquement comme
dépendance externe pour la future Feature « Billing Office EXPORT » (voir « Hors périmètre »). Cette
exclusion n'est pas un choix arbitraire : elle applique la même règle de résolution que celle
explicitement demandée pour IMP-15 (contenu de la ligne individuelle > regroupement par préfixe), et
respecte la consigne de ne pas absorber une exigence appartenant à une feature future (§14 de la
demande, §22 point 7).

### 0.3 Table de sélection des exigences retenues

| Exigence CDC | Priorité | Description (fidèle au CDC) | Retenue en Feature 003 ? |
|---|---|---|---|
| IMP-01 | Must | Formulaire dynamique multi-BL (jusqu'à 5 BL), dépôt de documents par BL | ✅ |
| IMP-02 | Must | Génération automatique d'un ticket indépendant par BL, regroupés dans un dossier | ✅ |
| IMP-03 | Must | Client reçoit ses numéros de ticket sur son profil et par e-mail | ✅ |
| IMP-04 | Must | Instruction Pilote Manifeste : Validé / Rejeté (motif) / En attente de complément | ✅ |
| IMP-05 | Must | Complément : client notifié, dépose éléments, retour file Pilote Manifeste | ✅ |
| IMP-06 | Must | Aiguillage automatique du ticket validé selon le type de client | ✅ |
| IMP-10 | Must | Client normal : demande de facture depuis le ticket | ✅ |
| IMP-11 | Must | Pilote Facturation Terre édite la facture, joint au ticket | ✅ |
| IMP-12 | Must | Client paie (hors portail), joint sa preuve de paiement | ✅ |
| IMP-13 | Must | Validation interne de la preuve de paiement avant Contrôle Documentaire | ✅ |
| IMP-14 | Must | Pilote CD vérifie, dépose BS/BADT, renvoie et clôture le ticket | ✅ |
| IMP-15 | Should | Alternative présentielle après contrôle BL (dépend de CON-01, externe) | ✅ (voir §0.1) |
| IMP-20 | Must | Client en compte : parcours identique jusqu'à l'édition de la facture | ✅ |
| IMP-21 | Must | Client en compte : facture → Contrôle Documentaire direct (pas de paiement) | ✅ |
| IMP-22 | Must | Client en compte : récupère son ticket clôturé avec tous les documents | ✅ |
| IMP-30 | Must | Client deposit : parcours identique jusqu'à l'édition de la facture | ✅ |
| IMP-31 | Must | Client deposit : facture → lettrage Comptabilité Client (IRIS, hors portail) | ✅ |
| IMP-32 | Must | Client deposit : après lettrage → Contrôle Documentaire → retour au client | ✅ |
| IMP-40 | Must | Client IES : traitement parallèle Manifeste/CD, champs réservés par rôle | ✅ |
| IMP-41 | Must | Client IES : jointure obligatoire avant retour au client | ✅ |
| IMP-42 | Must | Client IES : finalisation sur plateforme IES (externe), clôture portail | ✅ |
| UNI-01 | Must | Blocage création si ticket actif existe pour le même BL | ✅ |
| UNI-02 | Must | BL libéré si son seul ticket est clôturé ou annulé | ✅ |
| UNI-03 | Must | Contrôle porté BL par BL, n'affecte pas les autres modules | ✅ |
| UNI-04 | Should | Règle équivalente pour bookings EXPORT | ❌ — voir §0.2 (Feature EXPORT) |

**Total retenu : 24 exigences (23 Must, 1 Should, 0 Could)** — et non 25/23/2/0 comme l'annonçait
`docs/05-roadmap-features.md` (qui comptait à tort UNI-04 dans le groupe IMPORT). Ce nombre est
strictement égal au décompte de `docs/01-matrice-exigences.md` ligne 335 (25) **moins** UNI-04 (1),
avec IMP-15 confirmée conservée comme démontré en §0.1.

### 0.4 Vérification inverse (Feature 003 → exigences sélectionnées → matrice CDC)

| Contrôle | Résultat |
|---|---|
| Chaque exigence sélectionnée existe-t-elle littéralement dans `docs/01-matrice-exigences.md` (groupe D) ? | ✅ Les 24 références sont présentes mot pour mot, aucune référence inventée |
| Toutes les exigences du groupe D sont-elles couvertes (aucune omission non justifiée) ? | ✅ 24/25 retenues, la seule omise (UNI-04) est explicitement justifiée en §0.2, pas silencieuse |
| Une exigence d'un autre groupe a-t-elle été absorbée à tort ? | ✅ Non — CPT-02, WKF-01, SOC-01/07/09/11, COM-01, SEC-02, CON-01 sont citées **uniquement** comme dépendances déclarées par la matrice elle-même, jamais comme FR propres à cette feature |
| Un doublon avec Feature 001 ou Feature 002 a-t-il été détecté ? | ✅ Non — voir « Réutilisation Feature 001 et Feature 002 » |
| Une exigence appartenant à une feature future a-t-elle été absorbée artificiellement ? | ✅ Non — UNI-04 (EXPORT) explicitement exclue ; CON-01/02/03 (Continuité) explicitement non implémentées ; EXP/SIM/AVE/REC/ENR/COM/NOT/IA/SEC/NFR ne sont ni citées comme FR ni absorbées |

## Périmètre de cette feature

Cette feature spécifie le parcours **Billing Office IMPORT**, depuis la soumission d'une demande
multi-BL par un client jusqu'à la clôture du ou des tickets qui en résultent, pour les quatre profils
de client déjà modélisés par Feature 002 (Normal, En Compte, Deposit, IES), ainsi que le contrôle
d'unicité des numéros de BL propre à ce module (UNI-01 à UNI-03). Elle correspond au groupe **D —
Billing Office IMPORT (IMP/UNI)** de `docs/01-matrice-exigences.md`, **moins UNI-04** (voir §0.2).

Le CDC ne mandate aucune technologie précise pour cette feature : les choix d'architecture détaillée
(modèle de données physique, projets .NET, mécanisme précis d'orientation du workflow) appartiennent
exclusivement à `/speckit.plan`, conformément au Principe I de la constitution.

## Réutilisation Feature 001 et Feature 002 *(ne pas recréer ces mécanismes)*

Feature 001 et Feature 002 sont **CLOSED**. Cette feature les consomme **sans les réimplémenter** :

| Capacité déjà livrée | Feature | Usage direct par Feature 003 |
|---|---|---|
| Ticket, Dossier, regroupement multi-ticket par dossier (SOC-11) | 001 | IMP-02 réutilise **exactement** ce mécanisme pour regrouper les tickets générés par BL sous un même dossier — aucune nouvelle structure de groupement |
| Files de traitement paramétrables, matrice de transitions (WKF-01/02/03) | 001 | Files « Pilote Manifeste », « Facturation », « Contrôle Documentaire », « Comptabilité Client » (IMP-04, IMP-10, IMP-14, IMP-31) sont des **instances paramétrées** du mécanisme de files existant, pas de nouveau moteur |
| Documents déposés, validation de type/taille (SOC-07) | 001 | Dépôt des pièces par BL (IMP-01), preuve de paiement (IMP-12), BS/BADT (IMP-14) réutilisent le mécanisme documentaire existant |
| Moteur d'habilitations paramétrable (`HabilitationUtilisateur`, jamais de rôle codé en dur) | 001 | Porte les habilitations Pilote Manifeste, Pilote Facturation, Comptable Client, Pilote CD, et l'habilitation de validation de paiement (IMP-13, PC-03) |
| Cloisonnement (le client ne voit que ses propres tickets/dossiers) | 001 | Aucune redéfinition — les tickets IMPORT restent des `Dossier`/`Ticket` ordinaires au sens du cloisonnement déjà démontré (CPT-04) |
| Journal d'audit append-only | 001 | Trace les décisions du Pilote Manifeste, la validation de paiement, les dépôts de documents, la clôture — aucun second journal |
| SLA, alertes, supervision | 001 | Applicables sans changement aux files utilisées par Feature 003 (paramétrage, pas de nouveau mécanisme) |
| Société cliente, profil (**TypeClient** : Normal/En Compte/Deposit/IES) | 002 | IMP-06 lit ce profil pour l'aiguillage (voir précision ci-dessous) |
| Instantané non-rétroactif du TypeClient sur le dossier/ticket (CPT-06, remédiation post-audit) | 002 | **Pivot central d'IMP-06** — voir « Aiguillage automatique » |
| Utilisateur rattaché, visibilité propre/société (ENT-03, CLAR-01/02) | 002 | Le client (utilisateur rattaché à une société) voit ses tickets IMPORT selon les mêmes règles de visibilité déjà démontrées, sans redéfinition |
| Authentification, compte société actif | 002 | Précondition d'accès au formulaire de soumission (aucune nouvelle règle) |

**Aucune réimplémentation** : ni second moteur de files/transitions, ni second système documentaire,
ni second moteur d'habilitations, ni second journal d'audit, ni second modèle de compte/société.

## Frontière avec le socle — besoins d'extension identifiés (fonctionnels uniquement)

Conformément à la Constitution (aucune conception technique dans cette étape), les besoins suivants
sont **documentés fonctionnellement**, sans préjuger de leur traduction technique (décision de
`/speckit.plan`, à l'image de l'extension `AuteurId`/`TypeClient` de `Dossier` en Feature 002) :

1. **Identifiant de BL par ticket** : chaque ticket IMPORT doit porter un numéro de BL identifiable,
   nécessaire au contrôle d'unicité (UNI-01/02/03) et à la correspondance ticket↔BL (IMP-02).
2. **Données de facturation/paiement par ticket** : référence de facture, preuve de paiement déposée,
   statut de paiement — nécessaires à IMP-11/12/13.
3. **Indicateur de type de client applicable au dossier** : Feature 003 **réutilise sans modification**
   le champ instantané déjà ajouté à `Dossier` par la remédiation post-audit de Feature 002
   (`TypeClient`, non rétroactif) — aucune nouvelle extension requise sur ce point précis.
4. **Trace de bascule présentielle** (IMP-15) : le ticket doit pouvoir porter l'indication que le
   client a déclaré vouloir finaliser physiquement, sans pour autant modéliser le rendez-vous
   lui-même (hors périmètre, voir §0.1).
5. **Séparation de champs par rôle pour le traitement parallèle IES** (IMP-40) : un mécanisme
   permettant que deux rôles interviennent simultanément sur un même ticket avec des permissions
   distinctes par champ/action — la portée exacte des champs réservés reste un point ouvert (PC-05,
   voir « Clarifications »).

Aucun de ces besoins ne remet en cause le comportement existant de `CloisonnementService`, du moteur
d'habilitations ou du journal d'audit — ce sont des extensions additives, dans l'esprit de D11/D5 de
Feature 002.

## User Scenarios & Testing *(mandatory)*

> L'organisation en 9 User Stories (et non les 8 suggérées) sépare explicitement le **contrôle
> d'unicité** (US2) de la soumission (US1), et l'**aiguillage automatique** (US4) de l'instruction
> Pilote Manifeste (US3), parce que le CDC leur attribue des exigences Must distinctes (UNI-01/02/03
> ; IMP-06) avec des critères d'acceptation propres et indépendamment testables — les regrouper
> masquerait des scénarios de test que le CDC distingue.

### User Story 1 - Soumettre une demande IMPORT multi-BL (Priority: P1)

Un utilisateur rattaché à une société cliente disposant du droit de soumission (Feature 002) dépose
une demande IMPORT comportant un ou plusieurs BL (jusqu'à 5), avec les documents associés à chacun.

**Why this priority** : sans soumission, aucun autre parcours n'existe — c'est le point d'entrée
unique de tout le module (IMP-01/02/03).

**Independent Test** : soumettre un formulaire avec 3 BL distincts et vérifier qu'exactement 3
tickets indépendants sont créés, regroupés sous un même dossier, chacun portant son propre BL et ses
propres documents.

**Acceptance Scenarios**:

1. **Given** un utilisateur habilité à soumettre pour sa société, **When** il soumet une demande avec
   2 BL valides (non actifs ailleurs) et les documents requis pour chacun, **Then** 2 tickets
   indépendants sont créés, regroupés dans un même dossier, chacun visible séparément par son numéro
   de BL (IMP-01, IMP-02).
2. **Given** une soumission validée, **When** les tickets sont créés, **Then** le client retrouve
   leurs numéros sur son profil (IMP-03) ; la notification par e-mail dépend d'une capacité future
   (COM-01, Feature Notifications) et n'est **pas** démontrée par cette feature (voir « Hors
   périmètre »).
3. **Given** un utilisateur non habilité à soumettre pour sa société (droit de soumission non
   accordé, Feature 002 ENT-02), **When** il tente de soumettre, **Then** le serveur refuse
   l'opération, quel que soit l'état de l'interface.

### User Story 2 - Contrôler l'unicité du BL à la soumission (Priority: P1)

Le système empêche la création d'un nouveau ticket pour un BL qui possède déjà un ticket actif, et
autorise une nouvelle demande dès que le seul ticket existant pour ce BL est clôturé ou annulé.

**Why this priority** : Must, condition d'intégrité absolue avant même la génération du ticket — sans
ce contrôle, IMP-02 pourrait créer des doublons.

**Independent Test** : soumettre deux fois le même numéro de BL sans clôturer le premier ticket ;
vérifier le blocage et la proposition d'accès au ticket existant ; clôturer ce ticket puis resoumettre
le même BL et vérifier que la création est acceptée.

**Acceptance Scenarios**:

1. **Given** un BL déjà associé à un ticket actif (ni clôturé ni annulé), **When** un client (même
   société ou une autre) soumet une nouvelle demande pour ce même BL, **Then** la création est
   bloquée et un accès au ticket existant est proposé (UNI-01).
2. **Given** un BL dont l'unique ticket est clôturé ou annulé, **When** une nouvelle demande est
   soumise pour ce BL, **Then** la création est acceptée (UNI-02).
3. **Given** un numéro de BL déjà référencé par un ticket Customer Complaint (module distinct, hors
   périmètre), **When** ce même numéro est soumis en IMPORT, **Then** le contrôle d'unicité IMPORT ne
   tient pas compte de cette référence externe (UNI-03).
4. **Given** un BL dont l'unique ticket a été **rejeté** par le Pilote Manifeste, **When** une
   nouvelle demande est soumise pour ce même BL, **Then** la création est acceptée immédiatement,
   sans action préalable d'un agent (UNI-02, clarifié le 2026-08-30 — Rejeté équivaut à Annulé pour ce
   contrôle).

### User Story 3 - Instruire au Pilote Manifeste (Priority: P1)

Le Pilote Manifeste (rôle défini par le CDC) instruit chaque ticket individuellement à l'issue du
contrôle BL : validation, rejet motivé, ou demande de complément.

**Why this priority** : Must, verrou obligatoire avant tout traitement ultérieur (facturation,
contrôle documentaire) — aucun ticket ne progresse sans cette décision.

**Independent Test** : présenter 3 tickets distincts au Pilote Manifeste et leur appliquer chacun une
décision différente (validé / rejeté / complément), vérifier que chaque ticket suit sa propre
trajectoire indépendamment des deux autres (conforme au scénario contractuel REC-SCE-01).

**Acceptance Scenarios**:

1. **Given** un ticket en attente d'instruction, **When** le Pilote Manifeste le valide, **Then** le
   ticket est marqué Validé et devient éligible à l'aiguillage automatique (US4).
2. **Given** un ticket en attente d'instruction, **When** le Pilote Manifeste le rejette, **Then** un
   motif est obligatoirement saisi, le client est notifié, et le ticket n'est pas éligible à
   l'aiguillage (IMP-04).
3. **Given** un ticket en attente d'instruction, **When** le Pilote Manifeste demande un complément,
   **Then** le client est notifié, peut déposer les éléments demandés dans le ticket, et celui-ci
   revient dans la file du Pilote Manifeste pour une nouvelle décision (IMP-05).
4. **Given** un dossier de 3 tickets (BL distincts), **When** le Pilote Manifeste valide le premier,
   rejette le second (motif) et demande un complément sur le troisième, **Then** les trois tickets
   suivent chacun leur propre trajectoire sans influence mutuelle (REC-SCE-01, IMP-02 indépendance).

### User Story 4 - Aiguiller automatiquement selon le type de client (Priority: P1)

À l'issue d'une validation par le Pilote Manifeste, le ticket s'oriente automatiquement vers le
circuit correspondant au profil de la société (Normal, En Compte, Deposit, IES).

**Why this priority** : Must, pivot structurel — sans lui, les 4 circuits (US5-US8) ne peuvent jamais
démarrer.

**Independent Test** : valider un ticket pour chacun des 4 profils de société et vérifier qu'il
rejoint la première étape du circuit correspondant (facturation pour Normal/En Compte/Deposit,
traitement parallèle pour IES).

**Acceptance Scenarios**:

1. **Given** un ticket validé dont le dossier porte l'instantané de type « Normal » (Feature 002,
   figé à la création du dossier), **When** l'aiguillage s'exécute, **Then** le ticket rejoint le
   circuit Client Normal (IMP-06 → US5).
2. **Given** un ticket validé pour un dossier « En Compte » / « Deposit » / « IES », **When**
   l'aiguillage s'exécute, **Then** le ticket rejoint respectivement US6 / US7 / US8.
3. **Given** un dossier créé quand la société était de type « Normal », **When** la société change de
   type vers « Deposit » (Feature 002, CPT-06) **après** la création du dossier mais **avant**
   l'instruction du Pilote Manifeste, **Then** l'aiguillage utilise **l'instantané figé à la création
   du dossier** (« Normal »), jamais le type courant de la société — aucune lecture rétroactive
   (cohérent avec l'invariant de non-rétroactivité déjà démontré par Feature 002, CPT-06).

### User Story 5 - Traiter un dossier Client Normal jusqu'à clôture (Priority: P2)

Facturation, paiement avec preuve, validation de la preuve, dépôt des documents de sortie (BS, BADT),
clôture.

**Why this priority** : circuit le plus fréquent et le plus complet fonctionnellement (5 exigences,
seul circuit avec étape de paiement en ligne « hors portail »).

**Independent Test** : dérouler un ticket Client Normal validé jusqu'à sa clôture avec dépôt des
documents finaux, conformément au scénario contractuel REC-SCE-01.

**Acceptance Scenarios**:

1. **Given** un ticket Client Normal aiguillé, **When** le client demande sa facture, **Then** le
   ticket rejoint la file Facturation (IMP-10).
2. **Given** un ticket en file Facturation, **When** le Pilote Facturation Terre édite la facture,
   **Then** elle est jointe au ticket et le client est notifié (IMP-11).
3. **Given** une facture éditée, **When** le client règle (hors portail) et dépose sa preuve de
   paiement, **Then** le ticket passe en attente de validation de paiement (IMP-12).
4. **Given** une preuve de paiement déposée, **When** l'utilisateur habilité à valider le paiement
   (PC-03) l'examine et la confirme, **Then** le ticket passe « Payé confirmé » et rejoint le Contrôle
   Documentaire (IMP-13).
5. **Given** un ticket au Contrôle Documentaire, **When** le Pilote CD vérifie le dossier et dépose
   BS et BADT, **Then** le ticket est renvoyé au client et clôturé (IMP-14).
6. **Given** une preuve de paiement déposée, **When** l'utilisateur habilité à valider le paiement la
   juge invalide, **Then** le ticket revient à un état « en attente de nouvelle preuve », le client
   est notifié avec un motif tracé, et peut déposer une nouvelle preuve (IMP-13, clarifié le
   2026-08-30, par analogie avec IMP-05).

### User Story 6 - Traiter un dossier Client En Compte jusqu'à clôture (Priority: P2)

Parcours identique au Client Normal jusqu'à l'édition de la facture, puis transmission directe au
Contrôle Documentaire, sans étape de paiement.

**Why this priority** : Must, second circuit par fréquence attendue ; démontre la variabilité du
tronc commun de facturation.

**Independent Test** : dérouler un ticket En Compte jusqu'à clôture, en vérifiant explicitement
l'absence de toute étape de paiement (conforme à REC-SCE-02).

**Acceptance Scenarios**:

1. **Given** un ticket En Compte facturé, **When** la facture est éditée, **Then** le ticket est
   transmis **directement** au Contrôle Documentaire, sans passage par une file de paiement (IMP-20,
   IMP-21).
2. **Given** un ticket En Compte clôturé, **When** le client consulte son ticket, **Then** il récupère
   l'ensemble des documents (facture, BS, BADT) (IMP-22).

### User Story 7 - Traiter un dossier Client Deposit jusqu'à clôture (Priority: P2)

Parcours identique au Client Normal jusqu'à l'édition de la facture, puis lettrage par la
Comptabilité Client (dans IRIS, hors portail) avant Contrôle Documentaire.

**Why this priority** : Must, introduit une étape de confirmation comptable distincte des deux
circuits précédents (conforme à REC-SCE-03).

**Independent Test** : dérouler un ticket Deposit jusqu'à clôture en vérifiant le passage obligatoire
par la confirmation du Comptable Client avant le Contrôle Documentaire.

**Acceptance Scenarios**:

1. **Given** un ticket Deposit facturé, **When** la facture est éditée, **Then** le ticket est
   transmis à la file Comptabilité Client (IMP-30, IMP-31).
2. **Given** un ticket en Comptabilité Client, **When** le lettrage est effectué dans IRIS (hors
   portail, déclaratif) et confirmé dans le ticket, **Then** le ticket rejoint le Contrôle Documentaire
   (IMP-31).
3. **Given** un ticket au Contrôle Documentaire après lettrage, **When** le Pilote CD dépose les
   documents, **Then** le ticket est retourné au client et clôturé (IMP-32).
4. **Given** un ticket en file Comptabilité Client, **When** le lettrage se révèle impossible ou doit
   être repris, **Then** le ticket reste dans cette file jusqu'à résolution manuelle, sans clôture
   automatique ni retour vers la Facturation (IMP-31, clarifié le 2026-08-30).

### User Story 8 - Traiter un dossier Client IES en parcours parallèle (Priority: P3)

Dès sa création, le ticket est instruit **simultanément** par le Pilote Manifeste et le Pilote
Contrôle Documentaire, chacun agissant uniquement sur les champs/actions de son rôle ; le ticket
n'est retourné au client qu'après jointure des deux traitements.

**Why this priority** : Must mais circuit le plus complexe (traitement parallèle, cloisonnement de
champs) — dépend d'un point encore ouvert (PC-05, portée exacte des champs réservés).

**Independent Test** : instruire les deux branches (Manifeste, Contrôle Documentaire) dans un ordre
arbitraire (Manifeste avant CD, ou l'inverse) et vérifier que le ticket n'est jamais retourné au
client tant que les deux traitements ne sont pas terminés, quel que soit l'ordre (conforme à
REC-SCE-04).

**Acceptance Scenarios**:

1. **Given** un ticket IES aiguillé, **When** il est créé, **Then** il est instruit en parallèle par
   le Pilote Manifeste et le Pilote CD, chacun limité aux champs/actions de son rôle (IMP-40).
2. **Given** les deux traitements parallèles en cours, **When** un seul des deux est terminé, **Then**
   le ticket n'est **pas** retourné au client (jointure requise) (IMP-41).
3. **Given** les deux traitements terminés (dans un ordre quelconque), **When** la jointure est
   constatée, **Then** le client peut finaliser (facturation, paiement, documents) sur la plateforme
   IES externe, et le ticket portail est clôturé à la remise des documents (IMP-42).

### User Story 9 - Basculer vers une finalisation présentielle après contrôle BL (Priority: P4, Should)

À l'issue du contrôle BL, le client peut déclarer son intention de finaliser physiquement au Billing
Office ; le ticket porte la trace de cette bascule.

**Why this priority** : Should — dépend d'une capacité générale (CON-01) non construite par cette
feature ; seule la déclaration/trace côté Billing Office est spécifiée ici.

**Independent Test** : après validation d'un ticket par le Pilote Manifeste, déclarer l'intention de
finalisation présentielle et vérifier que le ticket porte cette trace, sans qu'aucun mécanisme de
prise de rendez-vous ne soit requis pour que ce test passe.

**Acceptance Scenarios**:

1. **Given** un ticket validé par le Pilote Manifeste, **When** le client déclare vouloir finaliser
   physiquement, **Then** le ticket porte la trace de cette bascule (IMP-15).
2. **Given** cette trace enregistrée, **When** une future feature « Continuité Digital/Présentiel »
   (CON-01/02/03) sera livrée, **Then** elle pourra s'appuyer sur cette trace pour permettre à un
   agent de guichet de reprendre le dossier — **ce mécanisme de reprise n'est pas démontré par cette
   feature** (dépendance externe, non implémentée ici).

### Edge Cases

| Cas limite | Traitement retenu | Source |
|---|---|---|
| BL déjà actif au moment de la soumission | Blocage, accès au ticket existant proposé (UNI-01) | US2 |
| Deux soumissions concurrentes du même numéro de BL | Cas limite fonctionnel identifié : un seul aboutissement doit réussir, l'autre doit être refusé proprement — la stratégie technique (verrouillage, contrainte d'unicité, détection de conflit) est **hors périmètre de cette spécification** et appartient à `/speckit.plan` (à l'image de la stratégie déjà retenue pour l'invariant du dernier administrateur, Feature 002 D6/D7) | Constaté, non résolu ici |
| 1 BL valide + 1 BL invalide (rejeté) dans un dossier multi-BL | Chaque ticket suit sa propre trajectoire indépendamment des autres — comportement **directement prescrit** par IMP-02 (« totalement indépendant ») et illustré littéralement par le scénario contractuel REC-SCE-01 (1 validé, 1 rejeté, 1 complément) | IMP-02, REC-SCE-01, US3 scénario 4 |
| Complément demandé sur un seul ticket d'un dossier multi-BL | Seul le ticket concerné retourne en file Manifeste ; les autres tickets du même dossier poursuivent leur propre trajectoire sans attendre | IMP-02, IMP-05, US3 |
| Rejet d'un ticket parmi plusieurs | Le ticket rejeté s'arrête à cet état (notification, motif tracé) ; les autres tickets du dossier ne sont pas affectés | IMP-02, IMP-04, US3 |
| Changement de TypeClient de la société après création du dossier, avant aiguillage | L'aiguillage utilise l'instantané figé à la création du dossier (Feature 002, CPT-06), jamais le type courant — aucune lecture rétroactive | US4 scénario 3 |
| Preuve de paiement absente | Le ticket reste bloqué avant le Contrôle Documentaire tant qu'aucune preuve n'est déposée (IMP-12 est un préalable explicite à IMP-13) | IMP-12/13 |
| Preuve de paiement jugée invalide par l'utilisateur habilité à la valider | Retour à un état « en attente de nouvelle preuve », client notifié avec motif tracé, nouvelle preuve possible — **clarifié le 2026-08-30**, par analogie avec IMP-05 | US5 scénario 6, FR-016 |
| Document requis absent lors d'une soumission ou d'un dépôt | Comportement générique déjà couvert par le socle documentaire de Feature 001 (validation de présence/type paramétrable, SOC-07) — aucune règle nouvelle nécessaire | Réutilisation F001 |
| Lettrage (client Deposit) impossible ou à reprendre | Le ticket reste dans la file Comptabilité Client jusqu'à résolution manuelle — **clarifié le 2026-08-30** | US7 scénario 4, FR-022 |
| Branches IES terminées dans un ordre différent (Manifeste avant CD, ou l'inverse) | Comportement **symétrique et indépendant de l'ordre** — la jointure exige seulement que les deux soient terminées, quel que soit l'ordre d'achèvement | IMP-41, US8 scénario 3 |
| Utilisateur non habilité tente une action réservée (validation Manifeste, validation paiement, dépôt CD, etc.) | Refus serveur systématique, quel que soit l'état de l'interface (Principe VII de la constitution, cohérent avec Feature 001/002) | Sécurité (transversal) |
| Accès à une information IES réservée au rôle opposé | Refus serveur — portée exacte des champs réservés non définie par le CDC, clarification créée (PC-05) | Clarification ouverte |

## Requirements *(mandatory)*

> Les rôles explicitement nommés par le CDC (Pilote Manifeste, Pilote Facturation Terre, Pilote
> Contrôle Documentaire, Comptable Client) sont des **DEFINED_ROLE** au sens de
> `docs/00-analyse-cdc.md` §4 — utilisés directement. Le rôle de validation du paiement (IMP-13)
> reste **ROLE_TO_CONFIRM** (PC-03) : formulé ci-dessous de façon neutre (« utilisateur habilité à
> valider le paiement »), sans inventer de nom de rôle officiel.

### Soumission multi-BL et unicité (IMP-01, IMP-02, IMP-03, UNI-01, UNI-02, UNI-03)

- **FR-001**: Le système DOIT permettre à un utilisateur rattaché à une société cliente, disposant du
  droit de soumission (Feature 002, ENT-02), de soumettre une demande IMPORT comportant un ou
  plusieurs numéros de BL (jusqu'à 5 par défaut — valeur paramétrable, PC-19 ouvert sur son caractère
  figé ou non), avec les documents associés à chaque BL.
- **FR-002**: Pour chaque numéro de BL soumis, le système DOIT d'abord vérifier l'absence de ticket
  **actif** pour ce même BL (UNI-01) avant toute création. Ce contrôle porte sur une **portée
  globale** (toute la plateforme, tous clients/sociétés confondus) — **clarifié le 2026-08-30** : un
  numéro de BL est un identifiant délivré par la compagnie maritime, jamais alloué par société. La
  comparaison DOIT être insensible à la casse et ignorer les espaces en début/fin de saisie, sans
  jamais supprimer les tirets ni les espaces internes (CLARIFICATION PROJET du 2026-08-30, voir
  « Clarifications »).
- **FR-003**: Si un ticket actif existe déjà pour un BL soumis (y compris s'il appartient à une autre
  société que celle du client soumetteur), le système DOIT bloquer la création pour ce BL et ne DOIT
  proposer un accès au ticket existant que si le client dispose des droits de le consulter
  (cloisonnement inter-société, CPT-04, absolu — un simple message générique « une demande est déjà
  en cours » DOIT être affiché sans accès direct dans le cas contraire) (UNI-01).
- **FR-004**: Un BL dont l'unique ticket est à l'état clôturé, annulé, **ou rejeté** DOIT pouvoir
  faire l'objet d'une nouvelle demande, sans action supplémentaire d'un agent (UNI-02, précisé le
  2026-08-30 — un ticket Rejeté est fonctionnellement équivalent à Annulé pour ce contrôle).
- **FR-005**: Le contrôle d'unicité DOIT porter sur chaque BL individuellement et ne DOIT produire
  aucun effet sur l'utilisation du même numéro de BL dans un autre module (ex. Customer Complaint)
  (UNI-03).
- **FR-006**: À la validation du formulaire, le système DOIT générer automatiquement un ticket par BL
  accepté, chacun totalement indépendant des autres, avec une correspondance univoque ticket ↔ BL
  (IMP-02), regroupés sous un même dossier (réutilisation de SOC-11, Feature 001). Si une soumission
  multi-BL contient un ou plusieurs BL invalides (déjà actifs ailleurs, UNI-01), le système DOIT créer
  les tickets pour les BL valides et refuser uniquement les BL invalides — **jamais** rejeter
  l'intégralité de la soumission (résolu directement par IMP-02 « totalement indépendant » et illustré
  littéralement par le scénario contractuel REC-SCE-01 : 3 BL soumis ensemble aboutissent à 3
  décisions distinctes — validé, rejeté, complément — sans qu'aucune ne bloque les autres).
- **FR-007**: Le client DOIT retrouver les numéros de ses tickets sur son profil dès leur création
  (IMP-03). La notification par e-mail dépend d'une capacité de notification non construite par cette
  feature (COM-01) — décrite comme dépendance, non implémentée ici (voir « Hors périmètre »).

### Instruction Pilote Manifeste (IMP-04, IMP-05)

- **FR-008**: Chaque ticket validé au formulaire DOIT arriver dans la file du Pilote Manifeste, qui
  DOIT pouvoir prononcer, pour ce ticket individuellement, l'une des trois décisions : Validé, Rejeté
  (motif obligatoire), ou En attente de complément (IMP-04).
- **FR-009**: En cas de demande de complément, le client DOIT être notifié, DOIT pouvoir déposer les
  éléments demandés dans le ticket, et le ticket DOIT alors revenir dans la file du Pilote Manifeste
  pour une nouvelle décision (IMP-05).
- **FR-010**: La décision prise sur un ticket d'un dossier multi-BL ne DOIT produire aucun effet sur
  les autres tickets du même dossier (IMP-02, REC-SCE-01).

### Aiguillage automatique par type de client (IMP-06)

- **FR-011**: À l'issue d'une décision « Validé » du Pilote Manifeste, le système DOIT orienter
  automatiquement le ticket vers le circuit correspondant au type de client (Normal, En Compte,
  Deposit, IES) (IMP-06).
- **FR-012**: L'aiguillage DOIT s'appuyer exclusivement sur l'instantané du type de client figé à la
  création du dossier (Feature 002, `Dossier.TypeClient`, invariant de non-rétroactivité démontré par
  CPT-06) — **jamais** sur le type de client courant de la société au moment de la décision du Pilote
  Manifeste, même si celui-ci a changé entre-temps.

### Client Normal (IMP-10 à IMP-14)

- **FR-013**: Un ticket Client Normal aiguillé DOIT permettre au client de demander sa facture,
  faisant transiter le ticket vers la file Facturation (IMP-10).
- **FR-014**: Le Pilote Facturation Terre DOIT pouvoir éditer la facture (contrôles réalisés hors
  portail) et la joindre au ticket ; le client DOIT être notifié (IMP-11).
- **FR-015**: Le client DOIT pouvoir déposer sa preuve de paiement (règlement effectué hors portail)
  dans le ticket (IMP-12).
- **FR-016**: La preuve de paiement DOIT faire l'objet d'une validation par un utilisateur habilité à
  valider le paiement (rôle non officiellement nommé par le CDC — PC-03) avant tout passage au
  Contrôle Documentaire, avec les statuts intermédiaires En attente paiement → En attente validation →
  Payé confirmé (IMP-13). Si la preuve est jugée invalide, le ticket DOIT revenir à un état « en
  attente de nouvelle preuve », le client DOIT être notifié avec un motif tracé, et DOIT pouvoir
  déposer une nouvelle preuve (clarifié le 2026-08-30, par analogie avec IMP-05).
- **FR-017**: Le Pilote Contrôle Documentaire DOIT vérifier le dossier, déposer les documents de
  sortie (Bon de Sortie, BADT) dans le ticket, renvoyer le ticket au client et le clôturer (IMP-14).

### Client En Compte (IMP-20 à IMP-22)

- **FR-018**: Le parcours Client En Compte DOIT être identique au Client Normal jusqu'à l'édition de
  la facture (IMP-20).
- **FR-019**: Après édition de la facture, le ticket En Compte DOIT être transmis directement au
  Contrôle Documentaire, sans étape de paiement (IMP-21).
- **FR-020**: Le client en compte DOIT pouvoir récupérer son ticket clôturé avec l'ensemble des
  documents produits (facture, BS, BADT) (IMP-22).

### Client Deposit (IMP-30 à IMP-32)

- **FR-021**: Le parcours Client Deposit DOIT être identique au Client Normal jusqu'à l'édition de la
  facture (IMP-30).
- **FR-022**: Après édition de la facture, le ticket Deposit DOIT être transmis à la file Comptabilité
  Client pour lettrage (effectué dans IRIS, hors portail) ; le Comptable Client DOIT confirmer ce
  lettrage dans le ticket (IMP-31). Si le lettrage est impossible ou doit être repris, le ticket DOIT
  rester dans la file Comptabilité Client jusqu'à résolution manuelle — aucune clôture automatique, ni
  retour vers la file Facturation (clarifié le 2026-08-30).
- **FR-023**: Après confirmation du lettrage, le ticket DOIT rejoindre le Contrôle Documentaire, puis
  être retourné au client avec ses documents (IMP-32).

### Client IES (IMP-40 à IMP-42)

- **FR-024**: Dès sa création, un ticket IES DOIT être instruit **simultanément** par le Pilote
  Manifeste et le Pilote Contrôle Documentaire, chacun intervenant exclusivement sur les champs et
  actions relevant de son propre rôle (IMP-40). La liste exacte des champs réservés à chaque rôle
  reste un point ouvert (PC-05, voir « Clarifications »).
- **FR-025**: Le ticket IES ne DOIT être retourné au client que lorsque les deux traitements
  parallèles sont terminés (jointure), quel que soit l'ordre dans lequel ils s'achèvent (IMP-41).
- **FR-026**: Le client IES DOIT finaliser son parcours (facturation, paiement, documents) sur la
  plateforme IES externe (hors portail, hors périmètre de cette feature) ; le ticket portail DOIT être
  clôturé à la remise des documents (IMP-42).

### Alternative présentielle (IMP-15, Should)

- **FR-027**: À l'issue du contrôle BL (décision du Pilote Manifeste), le système DEVRAIT permettre au
  client de déclarer son intention de finaliser physiquement au Billing Office ; cette déclaration
  DOIT alors être tracée sur le ticket (IMP-15). Le mécanisme général de prise de rendez-vous et de
  reprise au guichet (CON-01/02/03) n'est **pas** implémenté par cette exigence — dépendance externe
  décrite en « Hors périmètre ».

### Key Entities *(le référentiel manipule des données ; aucune structure de table ni classe n'est définie ici — décision de `/speckit.plan`)*

| Entité (conceptuelle) | Description | Attributs métier significatifs (fonctionnels) | Exigences |
|---|---|---|---|
| **Demande/Dossier IMPORT** | Regroupement des tickets issus d'une même soumission multi-BL — réutilise le `Dossier` de Feature 001 (SOC-11) et son instantané de type client (Feature 002) | Société propriétaire, auteur (utilisateur ayant soumis), instantané du type de client à la création | IMP-01, IMP-02, IMP-06 |
| **Ticket IMPORT (par BL)** | Un `Ticket` de Feature 001, étendu fonctionnellement par les attributs ci-contre | Numéro de BL, statut du circuit IMPORT (paramétrable), référence facture, preuve de paiement déposée, statut de paiement, trace de bascule présentielle (IMP-15) | IMP-02 à IMP-42, UNI-01 à UNI-03 |
| **Décision Pilote Manifeste** | Événement d'instruction — n'est pas une entité technique distincte, portée par le journal d'audit existant (D8, Feature 001/002) | Décision (Validé/Rejeté/Complément), motif (si rejet), auteur, date/heure | IMP-04, IMP-05, ENT-04 (Feature 002, réutilisé) |
| **Traitement parallèle IES** | Représentation fonctionnelle des deux sous-instructions (Manifeste, Contrôle Documentaire) d'un même ticket IES | État de chaque sous-traitement, champs/actions réservés par rôle, indicateur de jointure | IMP-40, IMP-41 |

**Non-entité, volontairement** : il n'existe aucune entité « DemandeImport » technique distincte du
`Dossier`/`Ticket` de Feature 001 — la distinction demande/ticket est **conceptuelle** (regroupement
multi-BL vs unité de traitement), pas une nouvelle paire d'entités techniques, à l'identique de la
méthode déjà appliquée par Feature 002 pour « Société cliente ».

## Rôles et habilitations

Le CDC nomme explicitement (DEFINED_ROLE, `docs/00-analyse-cdc.md` §4) : **Pilote Manifeste**,
**Pilote Facturation (Terre)**, **Pilote Contrôle Documentaire**, **Comptable Client**. Ces rôles sont
utilisés directement dans les User Stories et Functional Requirements ci-dessus.

Le rôle responsable de la validation de la preuve de paiement (IMP-13) **n'est pas** officiellement
nommé par la section 6 du CDC (« Caisse/Finance » n'apparaît que dans le texte de l'exigence, jamais
dans la liste officielle des rôles — PC-03, BLOQUANT pour la section 6 du CDC, mais **non bloquant**
pour cette spécification). Conformément à la consigne, cette spécification :
- conserve la **responsabilité fonctionnelle** (« valider la preuve de paiement ») ;
- référence explicitement **PC-03** comme point ouvert ;
- utilise la formulation neutre « utilisateur habilité à valider le paiement » partout où ce rôle est
  concerné (FR-016, US5 scénario 4) ;
- ne résout **pas** PC-03 dans ce document — la traduction technique de cette habilitation (permission
  paramétrable réutilisant le moteur de Feature 001, à l'image de la solution déjà retenue pour
  `Permission.CompteValider` en Feature 002) appartient à `/speckit.plan`.

Le rôle « Agent de guichet » (CON-01/02, FUNCTIONAL_RESPONSIBILITY) n'est **pas** sollicité par cette
feature : IMP-15 ne requiert, côté Billing Office, aucune action d'un tel rôle (seule la déclaration
du client et la trace sur le ticket sont spécifiées ici).

## Documents

PC-08 (liste exacte des pièces exigées par étape) reste un point ouvert **global** au projet, non
résolu ici. Cette spécification :
- conserve les documents **explicitement nommés** par le CDC pour ce module : Bon de Sortie (BS),
  BADT (IMP-14, IMP-22, IMP-32), la facture (IMP-11), la preuve de paiement (IMP-12) ;
- n'invente **aucune** liste de pièces au-delà de ces noms explicites (par exemple, aucune pièce
  précise n'est imposée pour la soumission initiale IMP-01 au-delà de « documents associés à chaque
  BL », paramétrable par profil comme le prévoit déjà SOC-07 de Feature 001) ;
- réutilise conceptuellement le socle documentaire de Feature 001 (contrôle d'accès, validation de
  type/taille, traçabilité) sans le redéfinir.

## Sécurité *(critères fonctionnels — l'implémentation technique appartient à `/speckit.plan`)*

- Toute action réservée à un rôle (décision du Pilote Manifeste, validation de la preuve de paiement,
  édition de facture, lettrage, dépôt de documents au Contrôle Documentaire) DOIT être vérifiée
  **côté serveur**, quel que soit l'état de l'interface — une restriction affichée uniquement dans
  l'UI ne constitue jamais une autorisation suffisante (Principe VII de la constitution, cohérent avec
  Feature 001/002).
- Le cloisonnement inter-société déjà démontré par Feature 002 (CPT-04) s'applique sans exception aux
  tickets IMPORT : un utilisateur d'une société ne peut jamais accéder aux tickets IMPORT d'une autre
  société, quel que soit son rôle.
- Le traitement parallèle IES (IMP-40) introduit un besoin de cloisonnement **au niveau du champ**
  (RBAC champ par champ, SEC-02) entre le Pilote Manifeste et le Pilote Contrôle Documentaire — décrit
  ici uniquement comme une **règle fonctionnelle de visibilité** (chacun n'agit que sur les
  champs/actions de son rôle), sans transformation en mécanisme technique à ce stade (PC-05 reste
  ouvert sur la liste exacte des champs).
- Toute décision sensible (validation/rejet/complément Manifeste, validation de paiement, dépôt de
  documents de sortie, lettrage confirmé, bascule présentielle déclarée) DOIT produire au minimum une
  entrée du journal d'audit existant (Feature 001, D8) — aucun second journal.

## Testabilité et recette CDC

### Analyse littérale de `docs/04-matrice-recette.md`

| REC-SCE | Intitulé exact | Exigences Must Feature 003 couvertes | Dépendance vers une feature future | Couverture |
|---|---|---|---|---|
| **REC-SCE-01** | IMPORT client normal | SOC-01 · SOC-02 · SOC-06 · SOC-09 (F001, déjà démontrées) · **IMP-01 à IMP-06 · IMP-10 à IMP-14 · UNI-01 à UNI-03** ; Should/Could couverts : COM-01 · COM-02 | COM-01/COM-02 (notifications, Feature future « Notifications ») — non démontrées ici | **Directe** pour la portion IMP/UNI ; **hors périmètre** pour la portion COM |
| **REC-SCE-02** | IMPORT client en compte | **IMP-20 · IMP-21 · IMP-22** | Aucune | **Directe**, intégralement couverte par Feature 003 |
| **REC-SCE-03** | IMPORT client deposit | **IMP-30 · IMP-31 · IMP-32** | Aucune | **Directe**, intégralement couverte par Feature 003 |
| **REC-SCE-04** | IMPORT client IES | **IMP-40 · IMP-41 · IMP-42** · SEC-02 (RBAC champ) | Aucune structurelle, mais PC-05 (portée exacte des champs) reste ouvert | **Directe**, sous réserve de PC-05 |

**Important** : REC-SCE-01, tel qu'écrit littéralement dans le CDC, traverse plusieurs modules (SOC-*
de Feature 001, déjà démontrées et closes ; COM-* d'une future feature Notifications). Feature 003 ne
peut donc démontrer que sa **portion propre** (IMP/UNI) de ce scénario — jamais son intégralité — à
l'image de la méthode déjà appliquée par Feature 002 pour REC-SCE-14. Les intitulés REC-SCE ne sont
**jamais renommés** ; ils sont reproduits mot pour mot ci-dessus.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un dossier soumis avec plusieurs BL génère exactement un ticket indépendant par BL, sans
  exception, dans 100 % des soumissions valides.
- **SC-002**: Aucun BL possédant un ticket actif ne peut jamais faire l'objet d'un second ticket actif
  simultané (0 doublon constaté sur l'ensemble des scénarios testés).
- **SC-003**: Chaque ticket d'un même dossier peut recevoir une décision Pilote Manifeste différente
  (validé/rejeté/complément) sans qu'aucune décision n'affecte le sort d'un autre ticket du dossier.
- **SC-004**: 100 % des tickets validés sont aiguillés vers le circuit correspondant au type de client
  **figé à la création du dossier**, y compris lorsque le type courant de la société a changé entre
  temps.
- **SC-005**: Un client en compte ne rencontre, à aucun moment de son parcours, une étape de paiement.
- **SC-006**: Un ticket IES n'est jamais restitué au client tant que les deux traitements parallèles
  (Manifeste, Contrôle Documentaire) ne sont pas tous deux achevés, quel que soit l'ordre
  d'achèvement.
- **SC-007**: Toute action réservée à un rôle est refusée côté serveur pour un utilisateur non habilité,
  y compris lorsque l'interface ne présente pas l'option correspondante.
- **SC-008**: Un utilisateur d'une société ne peut jamais consulter ou modifier un ticket IMPORT d'une
  autre société.

## Assumptions

- **A-001** : Le rôle de validation de la preuve de paiement (IMP-13) sera porté par une habilitation
  paramétrable réutilisant le moteur de Feature 001, sans qu'un nom de rôle officiel ne soit inventé
  ici — l'hypothèse de travail est neutre (« utilisateur habilité à valider le paiement »), révisable
  dès que PC-03 sera tranché par le métier.
- **A-002** : Le regroupement multi-ticket d'un dossier IMPORT réutilise directement le mécanisme déjà
  livré par Feature 001 (SOC-11) — aucune nouvelle structure de groupement n'est nécessaire.
- **A-003** : L'instantané de type client déjà porté par `Dossier` (Feature 002, CPT-06) est
  suffisant pour piloter l'aiguillage IMP-06 sans extension supplémentaire du modèle.
- **A-004** : La limite « jusqu'à 5 BL » (IMP-01) est traitée comme une valeur par défaut paramétrable
  (Principe VI de la constitution), révisable sans redéveloppement — PC-19 reste ouvert sur son
  caractère définitivement figé ou non.
- **A-005** : Les intégrations externes mentionnées (IRIS pour le lettrage Deposit, la plateforme IES
  pour la finalisation) restent strictement déclaratives/hors portail, conformément au Principe XI
  (aucune intégration API automatique n'est supposée).

## Clarifications nécessaires avant planification

> Classification demandée : BLOQUANTE POUR PLAN / À CONFIRMER AVANT IMPLÉMENTATION / À CONFIRMER
> AVANT RECETTE / À CONFIRMER AVANT PRODUCTION / NON BLOQUANTE. **Mise à jour post-`/speckit.clarify`
> (2026-08-30)** : les points CL-01, CL-02 et CL-05 ont été résolus lors de la session de
> clarification (voir « Clarifications » ci-dessous) — ils restent listés ici, barrés, pour
> traçabilité. Les points restants ne sont **pas** résolus par cette spécification — chacun reste
> documenté avec sa source et son impact, jamais tranché arbitrairement (Principe XV de la
> constitution).

| # | Point | Source CDC | Classification | Impact si non résolu |
|---|---|---|---|---|
| CL-01 | ~~Portée du contrôle d'unicité BL (UNI-01)~~ | UNI-01, UNI-03 | ✅ **RÉSOLUE** (session 2026-08-30) | Portée **globale** retenue — voir « Clarifications » |
| CL-02 | ~~Stratégie de résolution en cas de soumission concurrente du même BL~~ | Analogue à D6/D7 de Feature 002 | ✅ **Dimension métier résolue** (portée connue) — reste une **décision technique** (mécanisme de concurrence), non bloquante pour `/speckit.plan` | `/speckit.plan` conçoit la stratégie de concurrence sur une contrainte d'unicité **globale**, à l'image de D6/D7 |
| CL-03 | PC-03 — rôle officiel habilité à valider la preuve de paiement (IMP-13) | PC-03 (BLOQUANT au sens du CDC §6, mais non bloquant pour cette spécification grâce à la formulation neutre retenue) | **À CONFIRMER AVANT IMPLÉMENTATION** | La spécification et le plan peuvent avancer avec une habilitation générique ; le nom exact du rôle devra être confirmé avant de coder l'écran de validation du paiement |
| CL-04 | PC-05 — liste exacte des champs réservés à chaque rôle dans le traitement parallèle IES (IMP-40) | PC-05 | **À CONFIRMER AVANT IMPLÉMENTATION** | Le plan peut concevoir un mécanisme générique de permission par champ ; la liste réelle des champs devra être confirmée avant de coder IMP-40 |
| CL-05 | ~~Comportement attendu si la preuve de paiement est jugée invalide (IMP-13)~~ | Nouvelle ambiguïté identifiée par cette spécification | ✅ **RÉSOLUE** (session 2026-08-30) | Retour au client par analogie avec IMP-05 — voir « Clarifications » |
| CL-06 | Comportement du ticket IMPORT après la déclaration présentielle (IMP-15) : le circuit digital est-il suspendu, ou continue-t-il en parallèle jusqu'à ce que la future feature Continuité prenne le relais ? | IMP-15 (nouvelle ambiguïté, dérivée de §0.1) | **À CONFIRMER AVANT IMPLÉMENTATION** | N'empêche pas la spécification/planification du reste du module (IMP-15 reste isolée, Should) ; à trancher avant de coder la trace de bascule |
| CL-07 | PC-08 — liste exacte des pièces exigées par étape et par profil client pour l'IMPORT | PC-08 (point global au projet) | **NON BLOQUANTE POUR PLAN** (à confirmer avant production) | `/speckit.plan` peut concevoir un mécanisme documentaire configurable (réutilisation SOC-07) sans connaître la liste exacte — cohérent avec la méthode déjà retenue en Feature 002 (quickstart §3) : seule l'exploitation réelle exige une liste validée |
| CL-08 | PC-19 — la limite « jusqu'à 5 BL » (IMP-01) est-elle une contrainte métier figée ou une valeur par défaut paramétrable ? | PC-19 | **À CONFIRMER AVANT PRODUCTION** | La spécification retient déjà l'hypothèse « valeur par défaut paramétrable » (A-004) ; seule la valeur numérique définitive doit être confirmée avant l'exploitation réelle |
| CL-09 | Exclusion d'UNI-04 du périmètre de Feature 003 (voir §0.2) | Découverte par cette spécification | **NON BLOQUANTE** pour Feature 003 — à confirmer humainement une seule fois, puis devient un point d'entrée pour la future feature EXPORT |

> **CL-01 et CL-02 résolues** lors de la session de clarification du 2026-08-30 (voir « Clarifications »
> ci-dessous) — elles ne bloquent plus `/speckit.plan`.

## Clarifications

### Session 2026-08-30

- Q: Sur quel périmètre porte le contrôle d'unicité du numéro de BL — toute la plateforme, ou
  seulement à l'intérieur d'une même société cliente ? → A: Portée **globale** — un même numéro de
  BL est unique sur toute la plateforme, tous clients confondus (Option A). *Impact* : CL-01
  résolue ; CL-02 (stratégie de concurrence) n'a plus de dimension métier bloquante — seule reste une
  décision technique de conception (mécanisme de contrainte d'unicité globale et sa sûreté sous
  concurrence), à traiter par `/speckit.plan`, à l'image de la stratégie déjà retenue par Feature 002
  (D6/D7) pour l'invariant du dernier administrateur. *Exigences impactées* : UNI-01, UNI-02, UNI-03.
  *User Stories impactées* : US2. *Critères d'acceptation impactés* : US2 scénario 1 (déjà rédigé en
  anticipant ce cas — « un client (même société ou une autre) » — confirmé correct sans modification).
- Q: Un ticket rejeté par le Pilote Manifeste libère-t-il son numéro de BL pour une nouvelle demande,
  au même titre qu'un ticket clôturé ou annulé ? → A: **Oui** — un ticket **Rejeté** est
  fonctionnellement équivalent à **Annulé** pour le contrôle d'unicité : le BL redevient
  immédiatement disponible pour une nouvelle demande, sans action supplémentaire d'un agent.
  *Exigences impactées* : UNI-02. *User Stories impactées* : US2. *Critères d'acceptation impactés* :
  nouveau scénario ajouté à US2 (voir Requirements/FR-004 et Edge Cases).
- Q: Que se passe-t-il si l'utilisateur habilité à valider le paiement juge la preuve déposée
  invalide (IMP-13) ? → A: Le ticket **revient à un état « en attente de nouvelle preuve »** ; le
  client est notifié avec un motif tracé et doit déposer une nouvelle preuve — par **analogie
  directe** avec le mécanisme de complément déjà défini pour le Pilote Manifeste (IMP-05), seul
  patron de « retour client » que le CDC décrit dans ce module. *Exigences impactées* : IMP-13.
  *User Stories impactées* : US5. *Critères d'acceptation impactés* : nouveau scénario ajouté à US5
  (voir Requirements/FR-016 et Edge Cases).
- Q: Que se passe-t-il si le lettrage (client Deposit, IMP-31) est impossible ou doit être repris ?
  → A: Le ticket **reste dans la file Comptabilité Client** jusqu'à résolution manuelle par le
  Comptable Client — aucune clôture automatique, aucun retour arrière vers la Facturation. Le CDC ne
  décrit aucun mécanisme de rejet pour cette étape ; retenir le ticket dans sa file ne perd aucune
  donnée et n'invente aucune règle absente du texte. *Exigences impactées* : IMP-31. *User Stories
  impactées* : US7. *Critères d'acceptation impactés* : nouveau scénario ajouté à US7 (voir
  Requirements/FR-022 et Edge Cases).
- Q: Pour le contrôle d'unicité du BL (UNI-01/02/03), deux numéros de BL doivent-ils être comparés à
  l'identique caractère pour caractère, ou une normalisation s'applique-t-elle (casse, espaces,
  tirets) ? → A (**CLARIFICATION PROJET, pas une exigence CDC** — le CDC est muet sur ce point,
  décision validée lors de la revue de plan technique du 2026-08-30) : la comparaison d'unicité est
  **insensible à la casse** et ignore les **espaces en début/fin de saisie** (trim externe), mais ne
  supprime **jamais** les tirets ni les espaces internes. Exemples : `ABC123` ≡ `abc123` ;
  `" ABC123 "` ≡ `ABC123` ; `ABC-123` ≠ `ABC123` ; `"ABC 123"` ≠ `ABC123`. La valeur affichée au
  client/à l'audit conserve la casse et les caractères saisis (hors espaces de bord). *Exigences
  impactées* : UNI-01, UNI-02, UNI-03 (FR-002 à FR-005). *User Stories impactées* : US1, US2.
  *Critères d'acceptation impactés* : US2 scénarios 1-4 (portée désormais précisée par cette règle de
  comparaison, sans modification des scénarios eux-mêmes).

## Hors périmètre Feature 003

- **UNI-04** (règle d'unicité sur les bookings EXPORT) — appartient fonctionnellement à **Billing
  Office EXPORT** (voir §0.2), pas à l'IMPORT ; référencée uniquement comme point d'entrée pour cette
  future feature (dépend elle-même de PC-09).
- **Alternative physique / Continuité Digital-Présentiel (CON-01, CON-02, CON-03, CON-04)** — capacité
  générale de bascule digital/présentiel et de prise de rendez-vous, dont IMP-15 n'est qu'une
  instanciation Billing-Office (voir §0.1) ; Feature 003 se limite à enregistrer la déclaration du
  client, sans construire la recherche de dossier au guichet ni la prise de rendez-vous elle-même.
- **Billing Office EXPORT** (`EXP-01` à `EXP-04`) — codification Back Office, Draft IN, BEE — future
  feature dédiée, qui pourra réutiliser les structures de facturation posées par l'IMPORT.
- **Simulation de Proforma** (`SIM-*`) — moteur de calcul tarifaire autonome, non consommé ni requis
  par le parcours IMPORT réel.
- **Avoir et Exonération** (`AVE-*`) — ajustement comptable a posteriori sur des tickets de
  facturation déjà clôturés ; dépend de l'existence de tickets IMPORT/EXPORT clôturés, mais n'est pas
  implémenté ici.
- **Customer Complaint** (`REC-*`) — module de réclamation distinct ; peut référencer un numéro de BL
  déjà utilisé en IMPORT (UNI-03 le permet explicitement) sans qu'aucune règle de ce module ne soit
  absorbée ici.
- **Notifications multicanales** (`COM-*`, `NOT-*`) — l'envoi effectif d'e-mails (IMP-03, IMP-05) et
  de tout autre canal dépend de cette future feature ; Feature 003 ne fait qu'identifier les
  événements déclencheurs (décision Manifeste, complément demandé), sans implémenter l'envoi lui-même.
- **Intégrations automatiques externes** (IRIS, IES, passerelles bancaires) — toute interaction avec
  ces systèmes reste déclarative/hors portail (Principe XI de la constitution) ; aucune intégration
  API n'est supposée ou implémentée par cette feature.

## Matrice de couverture — Exigence CDC → User Story → Critère d'acceptation → REC-SCE → Couverture

| Exigence | Priorité | User Story | Critère d'acceptation | REC-SCE | Couverture recette | Clarification | Dépendance |
|---|---|---|---|---|---|---|---|
| IMP-01 | Must | US1 | US1 scénario 1 | REC-SCE-01 | Directe (portion IMP) | CL-08 (PC-19) | F002 (droit de soumission) |
| IMP-02 | Must | US1 | US1 scénario 1 ; Edge Cases (indépendance) | REC-SCE-01 | Directe (portion IMP) | — | F001 (SOC-11) |
| IMP-03 | Must | US1 | US1 scénario 2 | REC-SCE-01 (Should COM-01 non couvert) | Partielle (portion IMP démontrée, portion e-mail hors périmètre) | — | Feature Notifications (COM-01) |
| IMP-04 | Must | US3 | US3 scénarios 1-2, 4 | REC-SCE-01 | Directe (portion IMP) | — | F001 (files) |
| IMP-05 | Must | US3 | US3 scénario 3 | REC-SCE-01 | Directe (portion IMP) | — | F001 (documents) |
| IMP-06 | Must | US4 | US4 scénarios 1-3 | REC-SCE-01 | Directe (portion IMP) | — | F002 (TypeClient, CPT-06) |
| IMP-10 | Must | US5 | US5 scénario 1 | REC-SCE-01 | Directe | — | — |
| IMP-11 | Must | US5 | US5 scénario 2 | REC-SCE-01 | Directe | — | — |
| IMP-12 | Must | US5 | US5 scénario 3 | REC-SCE-01 | Directe | — | — |
| IMP-13 | Must | US5 | US5 scénario 4 | REC-SCE-01 | Directe | CL-03 (PC-03), CL-05 | — |
| IMP-14 | Must | US5 | US5 scénario 5 | REC-SCE-01 | Directe | — | — |
| IMP-15 | Should | US9 | US9 scénarios 1-2 | Aucun (absente de la matrice recette) | Absente — TEST-SPEC à préparer en `/speckit.tasks` | CL-06 | Feature Continuité (CON-01) |
| IMP-20 | Must | US6 | US6 scénario 1 | REC-SCE-02 | Directe | — | — |
| IMP-21 | Must | US6 | US6 scénario 1 | REC-SCE-02 | Directe | — | — |
| IMP-22 | Must | US6 | US6 scénario 2 | REC-SCE-02 | Directe | — | — |
| IMP-30 | Must | US7 | US7 scénario 1 | REC-SCE-03 | Directe | — | — |
| IMP-31 | Must | US7 | US7 scénario 2 | REC-SCE-03 | Directe | — | — |
| IMP-32 | Must | US7 | US7 scénario 3 | REC-SCE-03 | Directe | — | — |
| IMP-40 | Must | US8 | US8 scénario 1 | REC-SCE-04 | Directe (sous réserve PC-05) | CL-04 (PC-05) | — |
| IMP-41 | Must | US8 | US8 scénario 2 | REC-SCE-04 | Directe | — | — |
| IMP-42 | Must | US8 | US8 scénario 3 | REC-SCE-04 | Directe | — | Plateforme IES (externe) |
| UNI-01 | Must | US2 | US2 scénario 1 | REC-SCE-01 | Directe | CL-01, CL-02 | — |
| UNI-02 | Must | US2 | US2 scénario 2 | REC-SCE-01 | Directe | — | — |
| UNI-03 | Must | US2 | US2 scénario 3 | REC-SCE-01 | Directe | — | — |

**24/24 exigences retenues sont tracées** — aucune sans User Story, aucune sans critère d'acceptation
vérifiable. Les 2 Must sans REC-SCE explicitement applicable (aucun cas dans cette feature — les 23
Must retenues sont **toutes** couvertes par REC-SCE-01 à 04) ; **1 Should (IMP-15) n'a aucun REC-SCE
applicable** et devra recevoir un TEST-SPEC dédié lors de `/speckit.tasks`, dérivé de US9, jamais
présenté comme un scénario CDC officiel.

## Contrôle de couverture *(vérification mécanique finale)*

1. **Références CDC extraites de ce document** : IMP-01 à IMP-06, IMP-10 à IMP-15, IMP-20 à IMP-22,
   IMP-30 à IMP-32, IMP-40 à IMP-42, UNI-01 à UNI-03 = **24 références**, toutes présentes dans
   `docs/01-matrice-exigences.md` groupe D.
2. **Comparaison avec le périmètre retenu** : identique à la table §0.3 — aucune référence
   supplémentaire, aucune omise sans justification.
3. **Omission** : UNI-04 (justifiée §0.2). **Doublon** : aucun détecté. **Exigence hors périmètre
   absorbée** : aucune détectée.
4. **Must/Should/Could** : 23 Must + 1 Should (IMP-15) + 0 Could = **24**, conforme à la
   reconstitution indépendante de §0.3 (et non 23/2/0 comme l'annonçait la roadmap).
5. **REC-SCE vérifiés contre `docs/04-matrice-recette.md`** : REC-SCE-01, 02, 03, 04 reproduits mot
   pour mot (lignes 32-35 du document source), aucun renommage, aucune fusion.
6. **Aucune exigence F001/F002 réabsorbée** : SOC-*, WKF-*, CPT-*, ENT-* sont citées uniquement comme
   dépendances déclarées par la matrice CDC elle-même (colonne « Dépendances »), jamais comme
   Functional Requirements propres à Feature 003.
7. **Aucune exigence EXP/SIM/AVE/REC/ENR/COM/NOT/CON/IA/SEC/NFR absorbée** : toutes citées uniquement
   en tant que dépendances externes explicitement décrites en « Hors périmètre », jamais implémentées
   ni transformées en FR de cette feature.

## Conformité à la constitution (`.specify/memory/constitution.md`)

| Principe | Vérification |
|---|---|
| I — Stack technique imposée | Aucune technologie mentionnée dans ce document — reporté à `/speckit.plan` |
| II — Source de vérité et traçabilité | Toutes les 24 exigences retenues portent leur référence CDC d'origine (IMP-xx/UNI-xx) ; aucune Must silencieusement ignorée |
| III — Priorisation Must/Should/Could | Priorités reprises telles quelles de `docs/01-matrice-exigences.md`, aucune reprioritisation |
| IV/V/VI — Architecture, domaine, paramétrabilité | Aucune classe/table définie ; files, transitions, permissions décrites comme réutilisation du socle paramétrable existant |
| VII — Sécurité | Contrôles serveur exigés explicitement (section Sécurité, FR transversaux) |
| XI — Intégrations et périmètre autonome | IRIS et IES explicitement traités comme hors portail/déclaratifs (A-005) ; aucune intégration API supposée |
| XII — Tests | REC-SCE-01 à 04 repris littéralement ; IMP-15 (Must sans REC-SCE... en réalité Should) signalée pour TEST-SPEC futur |
| XV — Points non résolus | PC-03, PC-05, PC-08, PC-09, PC-19 tous référencés sans résolution arbitraire ; 2 nouvelles ambiguïtés (CL-01/02/05/06) documentées sans invention de règle |
| XVI — Definition of Done | Non applicable à ce stade (spécification, pas implémentation) |
| XVII — Gouvernance des écarts | Les deux écarts par rapport à `docs/05-roadmap-features.md` (IMP-15 conservée, UNI-04 exclue) sont explicites, justifiés par les sources primaires, documentés en §0.1/§0.2 |
