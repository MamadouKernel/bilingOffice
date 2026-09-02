# Feature Specification: Socle transverse de gestion des tickets et workflows

**Feature Branch**: `001-socle-tickets-workflows`

**Created**: 2026-08-28

**Status**: Draft — clarifié le 2026-08-28 (`/speckit.clarify` : 5 décisions validées, l'utilisateur ayant délégué le choix final à l'analyste)

**Input**: User description : « Socle transverse de gestion des tickets et workflows — création et
identification des tickets, dossiers, cycle de vie, statuts, historique, prise en charge, files de
traitement, affectation, réaffectation, transfert, workflow nominal, dérogations, commentaires
client/agent, notes internes, documents, liaison entre tickets, mesure des temps, attente client,
SLA, alertes, escalades, disponibilité opérateur, priorités, supervision opérationnelle, audit,
capacités transverses nécessaires aux notifications. »

## Sources de vérité

- `docs/CDC_Plateforme_Digitale_CIT_v3.docx` — cahier des charges, source de vérité métier unique
- `docs/00-analyse-cdc.md`, `docs/01-matrice-exigences.md`, `docs/02-points-a-clarifier.md`,
  `docs/03-revue-coherence-analyse.md`, `docs/04-matrice-recette.md` — base d'analyse validée
- `.specify/memory/constitution.md` — principes non négociables du projet (v1.0.0)

## Périmètre de cette feature

Cette feature spécifie le **socle transverse** utilisé par tous les modules métier (Billing
Office, Customer Complaint, Enrôlement Transporteurs) : le ticket, le dossier, le moteur de
workflow, les files, l'affectation, le transfert, la mesure des temps et des SLA, les alertes,
la supervision et l'audit. Elle correspond exactement aux groupes **A — Socle de gestion des
tickets (SOC/CYC/MSG)** et **B — Moteur de workflow, files et supervision
(WKF/PEC/TRF/TPS/SLA/ALE/SUP/DIS/AFF/PRI/MAS)** de `docs/01-matrice-exigences.md`.

**Explicitement hors périmètre** (feront l'objet de features dédiées ultérieures, qui
consommeront ce socle) :

- les parcours métier Billing Office IMPORT/EXPORT, Simulation de Proforma et Avoir/Exonération
  (`IMP-*`, `UNI-*`, `EXP-*`, `SIM-*`, `AVE-*`) ;
- Customer Complaint (`REC-*`) ;
- Enrôlement Transporteurs (`ENR-*`) ;
- la gestion des comptes clients et sociétés (`CPT-*`, `ENT-*`) ;
- la continuité digital/présentiel et la prise de rendez-vous (`CON-*`) ;
- l'assistant IA (`IA-*`) ;
- le catalogue complet des canaux et modèles de notification (`COM-*`, `NOT-*`) — cette feature
  fournit uniquement les **événements déclencheurs** (changements de statut, transferts, alertes)
  que la feature Notifications consommera ; elle ne spécifie ni les canaux, ni les modèles, ni
  les préférences client ;
- l'écran « Mes actions requises » du tableau de bord client (`ACR-01`) — écran consommateur du
  socle, hors périmètre ici ;
- toute exigence de sécurité générique non directement nécessaire au fonctionnement du socle
  (authentification, session, 2FA — `SEC-01`, `SEC-08` à `SEC-11`) : seules les exigences
  d'autorisation directement engagées par le socle sont citées (voir « Sécurité »).

Le CDC ne mandate aucune technologie précise pour ce socle : les choix d'architecture détaillée
(modèle de données, projets .NET, patterns) appartiennent exclusivement à `/speckit.plan`,
conformément au Principe I de la constitution.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Création, mise en file et prise en charge d'un ticket (Priority: P1)

Un client (ou un agent, selon le module appelant) déclenche la création d'une demande. Le socle
DOIT immédiatement produire un ticket identifié de façon unique, l'orienter vers la file
correspondant à son rôle de traitement, et permettre à un opérateur habilité de le prendre en
charge. C'est le socle minimal sans lequel aucun autre module métier ne peut fonctionner.

**Why this priority** : sans création, identification et prise en charge fiables d'un ticket,
aucun module (Billing Office, Customer Complaint, Enrôlement) ne peut exister. C'est le P1
absolu — le reste du socle (transfert, SLA, supervision…) n'a de sens que si cette base existe.

**Independent Test** : peut être testé intégralement en créant un ticket via une file de test,
en vérifiant l'unicité et la persistance de son numéro, son apparition dans la bonne file à
l'état « en attente de prise en charge », puis son passage à l'état « en traitement » après
acceptation par un opérateur — sans dépendre d'aucun autre module métier.

**Acceptance Scenarios**:

1. **Given** un module appelant soumet une nouvelle demande, **When** le ticket est créé,
   **Then** le système génère un numéro de ticket unique, communiqué immédiatement au module
   appelant (SOC-01).
2. **Given** plusieurs tickets rattachés à une même demande multi-éléments (ex. plusieurs BL),
   **When** ils sont créés dans le même mouvement, **Then** ils sont regroupés sous un numéro de
   dossier unique permettant un suivi global (SOC-11).
3. **Given** un ticket nouvellement créé, **When** il est routé, **Then** il arrive dans la file
   de traitement correspondant au rôle en charge, à l'état « en attente de prise en charge », et
   n'apparaît que pour les agents habilités sur cette file (SOC-04, PEC-01).
4. **Given** un ticket en attente dans une file, **When** un opérateur habilité de cette file
   l'accepte, **Then** le ticket lui est affecté, passe à l'état « en traitement », et le système
   enregistre l'heure d'arrivée en file, l'heure de prise en charge, l'opérateur, le début de
   traitement et la dernière activité (SOC-12, PEC-02, PEC-03).
5. **Given** un ticket pris en charge, **When** n'importe quel utilisateur habilité consulte son
   détail, **Then** il retrouve le statut courant, l'historique horodaté des événements, les
   documents et les échanges centralisés sur ce ticket unique (SOC-02, SOC-03).
6. **Given** un client consultant son espace, **When** il affiche l'un de ses tickets,
   **Then** il voit le statut en temps réel, cohérent avec le workflow du module concerné
   (SOC-08).

---

### User Story 2 - Affectation, réaffectation et transfert d'un ticket (Priority: P2)

Un opérateur ou un superviseur doit pouvoir déplacer un ticket entre files ou entre opérateurs
lorsque la situation opérationnelle l'exige, tout en respectant les circuits autorisés et sans
jamais perdre l'historique du dossier.

**Why this priority** : c'est la mécanique qui permet au workflow non rigide (principe P2 du
CDC) de fonctionner ; elle est indispensable dès qu'un ticket doit changer de main, ce qui
arrive dans la quasi-totalité des parcours réels, mais suppose que US1 existe déjà.

**Independent Test** : peut être testé en manipulant des tickets déjà créés et pris en charge
(état atteint via US1) : affecter, réaffecter, transférer, tenter une transition non autorisée,
et vérifier la conservation de l'historique — sans dépendre des règles métier d'un module
spécifique.

**Acceptance Scenarios**:

1. **Given** un ticket non pris en charge dans une file, **When** un superviseur (ou une règle
   d'affectation paramétrée) l'affecte directement à un opérateur précis, **Then** le ticket lui
   est assigné sans passer par la prise libre (PEC-05, AFF-02).
2. **Given** un ticket déjà affecté à un opérateur A, **When** un opérateur habilité (sous
   réserve des droits configurés, pas nécessairement le superviseur) le réaffecte à un opérateur
   B de la même file, **Then** le ticket change d'opérateur et l'opération est tracée (TRF-04).
3. **Given** un ticket dans la file X, **When** un opérateur habilité le transfère vers la file Y
   figurant parmi les destinations autorisées pour X, **Then** le ticket change de file, un
   motif obligatoire est enregistré, et le client est notifié de l'impact sur son parcours
   (TRF-01, TRF-03, TRF-05).
4. **Given** la même situation, **When** le transfert est effectué, **Then** le ticket conserve
   son numéro, son dossier, son historique, ses documents et ses commentaires (TRF-02).
5. **Given** un opérateur tente de transférer un ticket vers une file qui ne figure PAS parmi les
   destinations autorisées pour la file source, **When** il soumet ce transfert, **Then** le
   système refuse l'opération (TRF-06, WKF-01).
6. **Given** un utilisateur habilité déroge exceptionnellement au circuit nominal (WKF-02),
   **When** cette dérogation est appliquée, **Then** elle est tracée dans l'historique du ticket
   et dans le journal d'audit (WKF-03).
7. **Given** un ticket transféré vers une autre file **sans destinataire explicitement désigné**,
   **When** le transfert est validé, **Then** le ticket arrive dans la file de destination à
   l'état « en attente de prise en charge » (non affecté — PEC-01) et un nouveau temps d'attente
   est mesuré pour cette nouvelle étape (TPS-01) ; le SLA applicable est celui de la nouvelle
   étape/file (SLA-01). Si un destinataire a été désigné lors du transfert (TRF-03 : «
   destinataire éventuel »), le ticket lui est directement affecté. *(Clarification 2026-08-28 —
   CLAR-03.)*

---

### User Story 3 - Échange avec le client : demande de complément et attente (Priority: P2)

Lorsqu'un agent a besoin d'une information ou d'un document supplémentaire, il doit pouvoir
placer le ticket en attente d'une action du client, puis reprendre le traitement dès que le
client répond, sans que ce temps d'attente ne pénalise sa performance.

**Why this priority** : c'est un point de passage très fréquent de tous les workflows métier
(quel que soit le module) ; sans lui, aucun parcours réaliste ne peut être démontré de bout en
bout.

**Independent Test** : peut être testé en plaçant un ticket pris en charge (US1) en état
« attente client », en vérifiant que le décompte de performance de l'opérateur/file est suspendu
pendant cet état, puis en simulant la réponse du client et la reprise du traitement.

**Acceptance Scenarios**:

1. **Given** un agent a besoin d'un complément, **When** il place le ticket en attente client
   avec un motif, **Then** le ticket passe à l'état « en attente client », le client est notifié,
   et le décompte du temps interne CIT est suspendu (TPS-02, TPS-04).
2. **Given** un ticket en attente client, **When** le client dépose les éléments demandés,
   **Then** le ticket revient automatiquement dans la file de traitement d'origine pour reprise
   (TPS-02, WKF-01).
3. **Given** un ticket resté longtemps en attente client sans réponse, **When** les relances
   automatiques paramétrées (nombre et cadence) ont toutes été envoyées sans réponse,
   **Then** le système génère une suggestion de clôture pour absence de réponse — jamais une
   clôture automatique (CYC-04).
4. **Given** un ticket ayant atteint un état engageant (paiement confirmé, document officiel
   produit), **When** l'absence de réponse se prolonge, **Then** aucune suggestion de clôture
   n'est proposée (CYC-05).

---

### User Story 4 - Cycle de vie : annulation, clôture, réouverture (Priority: P2)

Le client ou un agent habilité doit pouvoir mettre fin au traitement d'un ticket — par annulation
avant engagement, par demande d'annulation après engagement, ou par clôture normale — et un
utilisateur CIT habilité doit pouvoir rouvrir un ticket clôturé en cas de besoin, sans jamais
perdre son historique.

**Why this priority** : c'est la fin naturelle du cycle de vie d'un ticket, nécessaire à toute
démonstration complète d'un parcours, et indépendante du mécanisme de transfert/affectation
(US2).

**Independent Test** : peut être testé sur un ticket pris en charge (US1) en déclenchant chaque
issue (annulation libre, demande d'annulation, clôture, réouverture) et en vérifiant la
conservation du numéro, de l'historique et des documents à chaque étape.

**Acceptance Scenarios**:

1. **Given** un ticket n'ayant pas atteint une étape bloquante (paramétrable par workflow),
   **When** le client l'annule lui-même, **Then** l'annulation est acceptée immédiatement,
   motivée, horodatée et conservée dans l'historique (CYC-01, CYC-03).
2. **Given** un ticket ayant dépassé l'étape bloquante, **When** le client demande l'annulation,
   **Then** la demande est transmise à la file ou au responsable habilité pour décision — elle
   n'est pas appliquée automatiquement (CYC-02).
3. **Given** un ticket dont le traitement métier est terminé, **When** l'agent habilité le
   clôture, **Then** le ticket est automatiquement archivé et reste consultable en permanence par
   le client et les agents habilités (SOC-09).
4. **Given** un ticket clôturé (clôture normale ou pour absence de réponse), **When** un
   utilisateur CIT habilité le rouvre avec un motif obligatoire, **Then** le ticket redevient
   actif tout en conservant son numéro, son historique, ses documents et ses échanges (CYC-06).
   Un ticket **annulé** n'entre pas dans le périmètre de la réouverture — le client crée une
   nouvelle demande (CYC-01) ou sollicite une réouverture (CYC-07). Le client, lui, ne peut pas
   rouvrir directement un ticket clôturé — il peut seulement demander une réouverture ou créer
   une nouvelle demande (CYC-07). *(Clarification 2026-08-28 — CLAR-02.)*

---

### User Story 5 - Communication sur le ticket : chat client/agent et notes internes (Priority: P3)

Le client et les agents doivent pouvoir échanger sur un ticket via un fil de messages horodaté et
conservé, tandis que les agents doivent pouvoir se coordonner entre eux via des notes strictement
internes, jamais visibles du client.

**Why this priority** : nécessaire à la collaboration humaine autour d'un ticket, mais son
absence ne bloque pas la démonstration du cycle de vie de base (US1-US4) ; elle vient
l'enrichir.

**Independent Test** : peut être testé sur un ticket existant en publiant un message client, un
message agent et une note interne, puis en vérifiant leur visibilité respective et leur
distinction visuelle.

**Acceptance Scenarios**:

1. **Given** un ticket en cours, **When** un client ou un agent publie un message dans le fil de
   commentaires, **Then** le message est horodaté, visible par le client et les agents habilités,
   et conservé dans l'historique (SOC-05, MSG-01).
2. **Given** le même ticket, **When** un agent publie une note interne, **Then** cette note n'est
   JAMAIS visible du client, seulement des collaborateurs CIT habilités (MSG-02).
3. **Given** l'affichage du fil, **When** un utilisateur le consulte, **Then** le caractère
   interne ou externe de chaque élément est visuellement très distinct (MSG-03).
4. **Given** une note interne, **When** un agent y mentionne un collaborateur (@Nom),
   **Then** la personne mentionnée reçoit une notification, et le ticket **reste sous la
   responsabilité de son opérateur courant** — la mention ne réaffecte jamais le ticket
   (MSG-04, MSG-05).

---

### User Story 6 - Dépôt de documents et contrôle d'accès (Priority: P3)

Le client et les agents doivent pouvoir déposer des documents sur un ticket, avec des contrôles
de conformité systématiques, et le socle doit garantir qu'aucun utilisateur ne peut voir ou agir
au-delà de son périmètre d'habilitation.

**Why this priority** : nécessaire pour tout parcours réel avec pièces jointes, mais peut être
testé et démontré séparément du reste (une fois un ticket existant, via US1).

**Independent Test** : peut être testé en déposant un document sur un ticket existant en tant que
client puis en tant qu'agent, en tentant un dépôt avec un fichier de type/taille non autorisé, et
en vérifiant qu'un utilisateur hors périmètre ne peut ni voir ni agir sur le ticket ou ses
documents.

**Acceptance Scenarios**:

1. **Given** un ticket ouvert, **When** le client ou un agent dépose un document, **Then** le
   document est rattaché au ticket et centralisé avec les autres pièces (SOC-07, SOC-02).
2. **Given** un dépôt de document, **When** le fichier ne respecte pas le type ou la taille
   autorisés pour l'étape courante, **Then** le dépôt est refusé avec un message d'erreur
   explicite (SOC-07).
3. **Given** un utilisateur authentifié, **When** il tente d'accéder à un ticket qui n'appartient
   pas à son périmètre (client tiers, file hors de son rôle), **Then** l'accès est refusé — le
   cloisonnement est vérifié côté serveur, pas seulement masqué dans l'interface (voir «
   Sécurité »).

---

### User Story 7 - Regroupement en dossier et liaison entre tickets (Priority: P4)

Un utilisateur habilité doit pouvoir consulter, pour une même demande, l'ensemble des tickets
rattachés à un même dossier, et associer entre eux des tickets appartenant à un même dossier
métier même lorsqu'ils relèvent de modules différents.

**Why this priority** : enrichit la vue globale d'un dossier client mais n'est pas nécessaire à
la démonstration du cycle de vie unitaire d'un ticket (US1-US4).

**Independent Test** : peut être testé en créant plusieurs tickets rattachés au même dossier
(via US1), puis en vérifiant leur regroupement, et en créant une liaison manuelle entre deux
tickets de modules différents.

**Acceptance Scenarios**:

1. **Given** plusieurs tickets d'un même dossier, **When** un utilisateur habilité consulte le
   dossier, **Then** il voit l'ensemble des tickets qui le composent et leur état respectif,
   pour un suivi global (SOC-11).
2. **Given** deux tickets relevant potentiellement du même dossier métier (y compris de modules
   différents), **When** un agent les associe, **Then** la liaison est visible sur les deux
   tickets ; les agents peuvent créer cette liaison, et le client bénéficie de sa visibilité
   depuis son tableau de bord (SOC-10).
3. **Given** un dossier dont tous les tickets sont clôturés ou annulés, **When** son état est
   consulté, **Then** le dossier suit le sort de ses tickets — considéré comme terminé et
   archivé au même titre qu'eux (SOC-09), tout en restant consultable en permanence ; la
   réouverture de l'un de ses tickets réactive automatiquement le dossier ; le numéro de dossier
   n'est jamais réutilisé (SOC-11). Le dossier ne porte aucun état ni action métier propre.
   *(Clarification 2026-08-28 — CLAR-01, décision métier déléguée.)*

---

### User Story 8 - Mesure des temps, SLA, alertes et escalades (Priority: P4)

Le socle doit mesurer automatiquement les temps d'attente et de traitement à chaque étape,
appliquer des seuils SLA paramétrables, et déclencher des alertes puis des escalades lorsque ces
seuils sont approchés ou dépassés.

**Why this priority** : capacité de pilotage indispensable à l'exploitation réelle, mais elle
mesure des événements produits par US1-US4 — elle ne peut être démontrée sans eux.

**Independent Test** : peut être testé en faisant transiter un ticket (créé via US1) à travers
plusieurs états avec des délais contrôlés, puis en vérifiant les mesures produites, le
déclenchement d'une alerte de seuil et d'une escalade.

**Acceptance Scenarios**:

1. **Given** un ticket traversant une étape, **When** il passe de l'arrivée en file à la prise en
   charge puis à la sortie de l'étape, **Then** le système mesure le temps d'attente et le temps
   de traitement de cette étape (TPS-01).
2. **Given** un ticket ayant traversé plusieurs étapes, **When** son parcours est consulté,
   **Then** le système distingue le temps interne CIT, le temps d'attente client, et le temps
   total du parcours (TPS-03).
3. **Given** un SLA configuré pour une file/étape/priorité donnée, **When** le temps écoulé
   atteint le seuil d'information (ex. 50 %) puis d'alerte (ex. 80 %) puis de dépassement
   (100 %), **Then** le système déclenche successivement les événements correspondants,
   jusqu'à l'escalade paramétrée (notification superviseur, remontée de priorité, réaffectation)
   (SLA-01, SLA-02, ALE-01, ALE-03).
4. **Given** un ticket en état « attente client », **When** le temps s'écoule dans cet état,
   **Then** le SLA applicable peut être suspendu ou adapté pendant cette période, conformément au
   paramétrage (SLA-03, TPS-04).
5. **Given** une file dont le backlog dépasse un seuil, ou un opérateur indisponible avec des
   tickets encore affectés, **When** cette situation survient, **Then** une alerte est générée à
   destination du superviseur (ALE-01).

---

### User Story 9 - Disponibilité des opérateurs et modes d'affectation automatique (Priority: P5)

Le socle doit connaître l'état de disponibilité de chaque opérateur et l'utiliser pour piloter les
modes d'affectation automatique paramétrables (Round-Robin, charge, disponibilité, compétence),
en garantissant qu'un opérateur indisponible ne reçoit jamais de nouvelle affectation
automatique.

**Why this priority** : optimisation de la distribution du travail ; utile en exploitation
réelle mais non indispensable pour démontrer qu'un ticket peut être créé, traité et clôturé
(US1-US4 suffisent pour un MVP fonctionnel avec prise libre/affectation manuelle uniquement).

**Independent Test** : peut être testé en faisant varier la disponibilité déclarée d'un opérateur
et en vérifiant qu'aucune affectation automatique ne lui parvient tant qu'il est indisponible ou
absent.

**Acceptance Scenarios**:

1. **Given** un opérateur, **When** son statut de disponibilité est Disponible, Indisponible ou
   Absent, **Then** le système en tient compte : seul un opérateur Disponible peut recevoir une
   affectation automatique (DIS-01).
2. **Given** un opérateur devenant Absent, **When** ce changement est enregistré,
   **Then** ses tickets en cours restent identifiables et deviennent réaffectables en masse par
   le superviseur (DIS-02).
3. **Given** une file configurée en mode Round-Robin (ou charge, disponibilité, compétence),
   **When** un nouveau ticket doit être distribué automatiquement, **Then** il n'est jamais
   proposé à un opérateur Indisponible ou Absent (AFF-03 à AFF-06, dépendance déduite vers
   DIS-01).
4. **Given** un administrateur fonctionnel, **When** il change le mode d'affectation d'une file,
   **Then** ce changement s'applique sans nécessiter de développement (AFF-07).

---

### User Story 10 - Supervision opérationnelle des files, de la charge et priorisation (Priority: P5)

Un superviseur doit disposer d'une vue temps réel des tickets, des files et de la charge des
opérateurs de son périmètre, pouvoir intervenir directement (affecter, réaffecter, transférer,
débloquer, agir en masse), et le système doit permettre de piloter la priorité des tickets.

**Why this priority** : capacité de pilotage transverse qui s'appuie sur tout ce qui précède
(US1 à US9) ; sa valeur est réelle mais elle vient après la fiabilité du socle de base.

**Independent Test** : peut être testé en consultant les vues de supervision sur un jeu de
tickets déjà créés et traités (via les stories précédentes), en déclenchant une action
superviseur (affectation, déblocage) et une action de masse.

**Acceptance Scenarios**:

1. **Given** un superviseur, **When** il consulte sa vue temps réel, **Then** il voit les tickets
   par état (non affectés, en cours, attente client, proches SLA, en dépassement SLA, sans
   activité), les files (volume, backlog, ancienneté, délai moyen, respect SLA), et la charge par
   opérateur (SUP-01, SUP-02, SUP-03). Cette vue et ses actions sont limitées au périmètre
   (files et/ou services) auquel le superviseur est rattaché par configuration, sans
   développement ; un périmètre global reste possible comme configuration particulière.
   *(Clarification 2026-08-28 — CLAR-05 ; la composition exacte du périmètre reste à définir par
   le métier, PC-04.)*
2. **Given** un ticket non pris en charge ou bloqué, **When** le superviseur intervient (affecte,
   réaffecte, transfère, débloque), **Then** l'action est appliquée et tracée (SUP-04, SUP-05).
3. **Given** un ticket dispose d'un niveau de priorité issu d'un référentiel paramétrable,
   **When** cette priorité est attribuée automatiquement ou modifiée par un utilisateur habilité,
   **Then** elle peut influencer l'ordre dans la file, le SLA applicable, les alertes et les
   règles d'escalade ; toute modification manuelle enregistre l'ancienne valeur, la nouvelle
   valeur, l'auteur, la date/heure et le motif (PRI-01, PRI-02, PRI-03). Le client ne peut pas
   s'auto-déclarer « urgent/critique » (PRI-04).
4. **Given** une sélection de tickets, **When** le superviseur applique une action de masse
   (affecter, réaffecter, transférer, modifier la priorité), **Then** chaque ticket de la
   sélection est traité et **chacune de ces actions est tracée individuellement, ticket par
   ticket** (MAS-01, MAS-02).

---

### User Story 11 - Audit des actions (Priority: P6)

Toute action réalisée sur un ticket ou dans le cadre du socle (création, prise en charge,
changement de statut, transfert, réaffectation, dépôt de document, annulation, réouverture,
modification de disponibilité, action de masse) doit être tracée de façon inviolable dans un
journal d'audit distinct de l'historique fonctionnel du ticket.

**Why this priority** : l'audit est transversal — il enregistre les événements produits par
toutes les stories précédentes. Il est placé en dernier non parce qu'il est secondaire (c'est un
Must non négociable, principe P5 du CDC) mais parce qu'il ne peut être démontré et testé qu'une
fois qu'il existe des actions à auditer.

**Independent Test** : peut être testé en exerçant une action socle quelconque (créée par
n'importe laquelle des stories précédentes) et en vérifiant qu'une entrée d'audit correspondante
existe, avec auteur, action, date/heure et détail, et qu'elle n'est pas modifiable par un
utilisateur métier.

**Acceptance Scenarios**:

1. **Given** une action sensible sur un ticket (création, prise en charge, changement de statut,
   transfert, réaffectation, annulation, réouverture, dépôt/téléchargement de document, décision,
   modification de disponibilité, action administrateur), **When** elle est exécutée,
   **Then** une entrée est créée dans le journal d'audit avec l'auteur, la date/heure et le
   détail de l'action (SOC-13).
2. **Given** le journal d'audit, **When** un utilisateur métier ordinaire tente d'en modifier une
   entrée, **Then** l'opération est impossible : le journal est inviolable (SOC-13).
3. **Given** l'historique fonctionnel du ticket (visible des agents habilités dans le cadre du
   dossier) et le journal d'audit (réservé à l'administrateur technique), **When** on les
   consulte, **Then** ils restent deux vues distinctes d'un même ensemble d'événements, sans être
   confondues.

---

### Edge Cases

- Que se passe-t-il si tous les tickets d'un dossier sont clôturés ou annulés ? **Résolu en
  séance de clarification du 28/08/2026** (CLAR-01, décision métier déléguée) : l'état du dossier
  est dérivé de ses tickets (voir Key Entities « Dossier » et US7) — le CDC, lui, ne définit
  toujours pas d'action propre au dossier.
- Que se passe-t-il si un ticket est transféré vers une file qui vient d'être désactivée par un
  administrateur pendant que le transfert est en cours ? Comportement non défini par le CDC ;
  à traiter comme un cas limite lors de la conception détaillée (`/speckit.plan`), sans préjuger
  d'une règle métier non fournie par le CDC.
- Comment le système réagit-il si deux opérateurs tentent de prendre en charge le même ticket au
  même instant (concurrence sur la prise en charge) ? Le CDC ne précise pas de règle explicite ;
  le principe général (un ticket a un seul opérateur assigné à la fois — PEC-02) implique qu'un
  seul des deux doit réussir, le second devant être informé que le ticket n'est plus disponible.
- Un ticket peut-il être transféré vers une file alors qu'il est en état « attente client » ?
  Le CDC ne l'interdit pas explicitement ; à traiter comme hypothèse ouverte, non résolue ici
  (voir Assumptions).
- Que se passe-t-il si un opérateur devient Indisponible/Absent alors qu'un ticket vient de lui
  être affecté automatiquement l'instant d'avant ? Le CDC ne précise pas de rétroaction ; DIS-02
  permet au superviseur de réaffecter en masse les tickets d'un opérateur Absent, ce qui couvre
  le cas a posteriori mais pas la prévention en temps réel.
- Quel rôle a l'habilitation pour clôturer un ticket pour absence de réponse (CYC-04) ? **Non
  défini par le CDC** — voir clarification PC-20.
- MAS-02 (traçabilité des actions de masse) suppose que MAS-01 (actions de masse elles-mêmes,
  Should) soit implémenté : si MAS-01 est différé à un incrément ultérieur, MAS-02 doit être
  considéré comme non applicable pour cet incrément, sans que cela ne constitue une anomalie.

## Requirements *(mandatory)*

> Chaque exigence ci-dessous conserve sa référence CDC d'origine entre parenthèses et sa priorité
> Must/Should/Could telle que documentée dans `docs/01-matrice-exigences.md`. Aucune référence
> n'est inventée ; aucune n'est renumérotée. Les exigences Should/Could sont marquées comme
> telles et restent optionnelles pour le périmètre minimal viable (US1).

### Socle du ticket (SOC)

- **FR-001** (SOC-01, Must) : Le système DOIT générer, pour toute demande créée par un module
  appelant, un ticket portant un numéro unique, communiqué immédiatement au client (portail et
  e-mail).
- **FR-002** (SOC-02, Must) : Le système DOIT centraliser sur le ticket : documents, statut,
  historique horodaté, échanges client/agent, décisions et motifs de rejet, documents finaux.
- **FR-003** (SOC-03, Must) : Chaque ticket DOIT suivre un workflow propre à son module et, pour
  le Billing Office, à son type de client (le déclenchement du bon workflow est une donnée
  d'entrée fournie par le module appelant, hors périmètre ici).
- **FR-004** (SOC-04, Must) : Le système DOIT présenter à chaque agent uniquement les tickets en
  attente de son rôle, avec tri et filtres (date, type client, statut, priorité).
- **FR-005** (SOC-05, Must) : Le système DOIT fournir un fil de commentaires (chat) par ticket
  entre le client et les agents, horodaté et conservé dans l'historique.
- **FR-006** (SOC-06, Must) : Le système DOIT permettre les actions de workflow standard :
  valider, rejeter avec motif obligatoire, demander un complément au client.
- **FR-007** (SOC-07, Must) : Le système DOIT permettre le dépôt de documents par le client et
  par les agents, avec types de pièces paramétrables par étape et formats/taille configurables.
- **FR-008** (SOC-08, Must) : Le système DOIT afficher au client le statut de chaque ticket en
  temps réel.
- **FR-009** (SOC-09, Must) : Le système DOIT archiver automatiquement tout ticket clôturé, tout
  en le laissant consultable en permanence par le client et les agents habilités.
- **FR-010** (SOC-10, Must) : Le système DOIT permettre aux agents de créer une liaison entre
  tickets (y compris entre modules), avec visibilité de cette liaison également accordée au
  client concerné, pour conserver la continuité d'un même dossier métier.
- **FR-011** (SOC-11, Must) : Le système DOIT regrouper sous un numéro de dossier unique les
  tickets d'une même demande multi-éléments, permettant un suivi global.
- **FR-012** (SOC-12, Must) : Le système DOIT distinguer l'arrivée d'un ticket en file de sa
  prise en charge effective, et enregistrer heure d'arrivée, heure de prise en charge, opérateur,
  début de traitement, dernière activité.
- **FR-013** (SOC-13, Must) : Le système DOIT tracer, dans un journal d'audit inviolable, toute
  action avec auteur, date/heure et détail.
- **FR-014** (SOC-14, Could) : Le système DEVRAIT permettre l'export des indicateurs et listes de
  tickets aux formats usuels (Excel, PDF).

### Cycle de vie du ticket (CYC)

- **FR-015** (CYC-01, Must) : Le système DOIT permettre au client d'annuler lui-même un ticket
  tant qu'aucun traitement engageant ni document officiel n'a été produit ; les étapes bloquantes
  DOIVENT être paramétrables par workflow, non codées en dur.
- **FR-016** (CYC-02, Must) : Au-delà de l'étape bloquante, le système DOIT permettre au client de
  soumettre une demande d'annulation transmise à la file ou au responsable habilité pour
  décision.
- **FR-017** (CYC-03, Must) : Le système DOIT motiver, horodater et conserver dans l'historique
  toute annulation ou demande d'annulation.
- **FR-018** (CYC-04, Must) : Lorsqu'un ticket est en attente d'une action du client, le système
  DOIT émettre des relances automatiques selon des délais paramétrables (nombre et cadence) ; au
  terme des relances sans réponse, il DOIT générer une suggestion de clôture — la clôture reste
  une décision manuelle d'un agent habilité, jamais automatique.
- **FR-019** (CYC-05, Must) : Le système NE DOIT JAMAIS proposer de suggestion de clôture pour
  absence de réponse lorsque le ticket a atteint un état engageant (paiement confirmé ou document
  officiel produit).
- **FR-020** (CYC-06, Should) : Le système DEVRAIT permettre à un utilisateur CIT habilité de
  rouvrir un ticket clôturé, avec motif obligatoire et traçabilité, en conservant numéro,
  historique, documents et échanges. *(Clarification 2026-08-28 — CLAR-02 : « ticket clôturé »
  couvre la clôture normale et la clôture pour absence de réponse ; un ticket annulé ne peut pas
  être rouvert.)*
- **FR-021** (CYC-07, Should) : Le client NE DEVRAIT PAS pouvoir rouvrir directement un ticket
  clôturé ; il peut seulement demander une réouverture ou créer une nouvelle demande.

### Messages et notes internes (MSG)

- **FR-022** (MSG-01, Must) : Un message client/agent DOIT être visible par le client et les
  agents habilités.
- **FR-023** (MSG-02, Must) : Une note interne CIT DOIT être visible uniquement par les
  collaborateurs CIT habilités, jamais par le client.
- **FR-024** (MSG-03, Must) : Le caractère interne ou externe d'un message DOIT être visuellement
  très distinct dans l'interface ; les deux types DOIVENT être horodatés et historisés.
- **FR-025** (MSG-04, Should) : Le système DEVRAIT permettre de mentionner un collaborateur
  (@Nom) dans une note interne ; la personne mentionnée reçoit une notification (portail et,
  selon paramétrage, e-mail).
- **FR-026** (MSG-05, Must) : Une mention NE DOIT JAMAIS entraîner de réaffectation : le ticket
  reste sous la responsabilité de son opérateur courant.

### Moteur de workflow (WKF)

- **FR-027** (WKF-01, Must) : Le système DOIT appliquer automatiquement le workflow correspondant
  au module, au type de demande et au profil du client (le profil/type est une donnée fournie par
  le module appelant).
- **FR-028** (WKF-02, Must) : Le système DOIT permettre à un utilisateur habilité de transférer ou
  réaffecter un ticket lorsque la situation opérationnelle le nécessite, dans le respect des
  règles d'habilitation paramétrées — le circuit n'est jamais rigide.
- **FR-029** (WKF-03, Must) : Toute dérogation au circuit nominal DOIT être tracée dans
  l'historique du ticket et le journal d'audit.

### Prise en charge (PEC)

- **FR-030** (PEC-01, Must) : Un ticket entrant dans une file DOIT rester à l'état « en attente de
  prise en charge » tant qu'aucun opérateur ne l'a accepté.
- **FR-031** (PEC-02, Must) : Un opérateur habilité DOIT pouvoir prendre en charge (accepter) un
  ticket de sa file ; le ticket lui est alors affecté et passe en traitement.
- **FR-032** (PEC-03, Must) : Le système DOIT enregistrer, pour chaque ticket : heure d'arrivée en
  file, heure de prise en charge, opérateur ayant accepté, début de traitement, dernière
  activité.
- **FR-033** (PEC-04, Must) : Le système DOIT calculer le délai de prise en charge (arrivée en
  file → acceptation) et le rendre exploitable dans les indicateurs.
- **FR-034** (PEC-05, Should) : Le système DEVRAIT permettre à un superviseur (ou à une règle
  d'affectation paramétrée) d'affecter directement un ticket non pris en charge à un opérateur
  précis.

### Transfert et réaffectation (TRF)

- **FR-035** (TRF-01, Must) : Un opérateur habilité DOIT pouvoir transférer un ticket vers une
  autre file de traitement autorisée, sans que le client recommence sa demande.
- **FR-036** (TRF-02, Must) : Lors d'un transfert, le ticket DOIT conserver son numéro, son
  dossier, son historique, ses documents, ses commentaires et les informations déjà collectées.
- **FR-037** (TRF-03, Must) : Un transfert DOIT tracer au minimum : opérateur à l'origine,
  date/heure, file source, file destination, motif (obligatoire), destinataire éventuel.
- **FR-038** (TRF-04, Must) : Un opérateur habilité DOIT pouvoir réaffecter un ticket à un autre
  opérateur habilité, sous réserve des droits configurés — ce droit n'est pas réservé au
  superviseur.
- **FR-039** (TRF-05, Must) : Le système DOIT notifier automatiquement le client d'une
  réorientation ayant un impact sur son parcours.
- **FR-040** (TRF-06, Must) : Les transitions autorisées entre files DOIVENT être définies par une
  matrice paramétrable ; aucun circuit de transfert n'est codé en dur. *(Clarification
  2026-08-28 — CLAR-04 : la matrice porte, pour chaque transition file source → file cible, les
  destinations autorisées, les rôles habilités à l'initier et les motifs autorisés ; son contenu
  reste à valider avec le métier — PC-07.)*

### Mesure des temps (TPS)

- **FR-041** (TPS-01, Must) : Pour chaque étape/file traversée, le système DOIT mesurer le temps
  d'attente (arrivée → prise en charge) et le temps de traitement (prise en charge → sortie).
- **FR-042** (TPS-02, Must) : Le système DOIT mesurer le temps d'attente client (durée en état
  « en attente client »).
- **FR-043** (TPS-03, Must) : Le système DOIT distinguer trois durées : temps interne CIT, temps
  d'attente client, temps total du parcours (création → clôture).
- **FR-044** (TPS-04, Must) : Le temps d'attente client NE DOIT PAS être imputé à la performance
  de l'opérateur ou de la file ; les SLA internes PEUVENT être suspendus pendant cet état.
- **FR-045** (TPS-05, Should) : L'historique du ticket DEVRAIT restituer la chronologie détaillée
  par étape (attente + traitement pour chaque file traversée).

### SLA

- **FR-046** (SLA-01, Must) : Le système DOIT permettre de paramétrer des SLA selon, au minimum :
  module, type de demande, étape, file, priorité, et éventuellement catégorie de client.
- **FR-047** (SLA-02, Must) : Le système DOIT permettre plusieurs seuils par SLA : information
  (ex. 50 %), alerte (ex. 80 %), dépassement (100 %) et escalade au-delà.
- **FR-048** (SLA-03, Must) : Les SLA DOIVENT pouvoir être suspendus ou adaptés pendant certains
  états, notamment « en attente client ».
- **FR-049** (SLA-04, Should) : Le calcul des SLA DEVRAIT pouvoir prendre en compte les horaires
  de fonctionnement (heures ouvrées) du service concerné.
- **FR-050** (SLA-05, Must) : Les valeurs de seuils et durées cibles des SLA DOIVENT être
  administrables sans développement (les valeurs exactes elles-mêmes restent à déterminer avec
  le métier — voir PC-12).

### Alertes et escalades (ALE)

- **FR-051** (ALE-01, Must) : Le système DOIT générer des alertes pour : ticket non pris en charge
  depuis X minutes, ticket sans activité depuis X minutes/heures, SLA bientôt dépassé, SLA
  dépassé, backlog de file supérieur à un seuil, volume anormal de tickets, opérateur
  indisponible avec des tickets encore affectés.
- **FR-052** (ALE-02, Must) : Les règles d'alerte, seuils, destinataires et canaux DOIVENT être
  paramétrables sans développement.
- **FR-053** (ALE-03, Must) : Le dépassement d'un SLA DOIT pouvoir déclencher une escalade
  (notification superviseur, remontée de priorité, réaffectation) selon des règles paramétrables.

### Supervision (SUP)

- **FR-054** (SUP-01, Must) : Le système DOIT fournir au superviseur une vue temps réel des
  tickets par état : en attente, non affectés, en cours, attente client, proches SLA, en
  dépassement SLA, sans activité depuis une durée définie.
- **FR-055** (SUP-02, Must) : Le système DOIT fournir une vue temps réel par file : nombre de
  tickets, backlog, ancienneté du ticket le plus ancien, délai moyen, respect du SLA.
- **FR-056** (SUP-03, Must) : Le système DOIT fournir une vue de la charge et de la
  disponibilité/connexion des opérateurs du périmètre du superviseur.
- **FR-057** (SUP-04, Must) : Le superviseur DOIT pouvoir : affecter un ticket non pris en charge,
  réaffecter, transférer, équilibrer la charge, intervenir sur un ticket bloqué, rechercher et
  consulter l'historique complet.
- **FR-058** (SUP-05, Must) : Le système DOIT fournir un suivi de l'activité opérationnelle des
  agents du périmètre : disponibilité, dernière activité, tickets affectés/en cours, volumes,
  indicateurs de délai.

### Disponibilité opérateur (DIS)

- **FR-059** (DIS-01, Must) : Le système DOIT permettre de connaître la disponibilité d'un agent
  (Disponible / Indisponible / Absent) ; un agent Indisponible ou Absent NE DOIT PAS recevoir de
  nouvelle affectation automatique.
- **FR-060** (DIS-02, Must) : Le superviseur DOIT pouvoir modifier la disponibilité d'un
  opérateur, identifier ses tickets affectés, les réaffecter en masse, et consulter sa charge
  restante.
- **FR-061** (DIS-03, Must) : Tout changement de disponibilité DOIT être tracé.

### Affectation (AFF)

- **FR-062** (AFF-01, Must) : Un opérateur habilité DOIT pouvoir prendre librement un ticket dans
  sa file (prise libre).
- **FR-063** (AFF-02, Must) : Un superviseur DOIT pouvoir affecter manuellement un ticket à un
  opérateur (exigence Must autonome, indépendante de PEC-05).
- **FR-064** (AFF-03, Should) : Le système DEVRAIT proposer une distribution automatique
  Round-Robin, qui NE DOIT PAS proposer de ticket à un opérateur indisponible (dépendance déduite
  vers DIS-01).
- **FR-065** (AFF-04, Should) : Le système DEVRAIT proposer une distribution selon la charge
  actuelle des agents.
- **FR-066** (AFF-05, Should) : Le système DEVRAIT proposer une distribution selon la
  disponibilité des agents.
- **FR-067** (AFF-06, Should) : Le système DEVRAIT proposer une distribution selon la
  compétence/l'habilitation.
- **FR-068** (AFF-07, Must) : Le changement de stratégie d'affectation d'une file DOIT être
  réalisable sans développement.

### Priorités (PRI)

- **FR-069** (PRI-01, Must) : Chaque ticket DOIT disposer d'un niveau de priorité issu d'un
  référentiel paramétrable ; la priorité PEUT être attribuée automatiquement ou modifiée par un
  utilisateur habilité.
- **FR-070** (PRI-02, Must) : La priorité PEUT influencer l'ordre du ticket dans la file, le SLA
  applicable, les alertes et les règles d'escalade.
- **FR-071** (PRI-03, Must) : Toute modification manuelle de priorité DOIT enregistrer : ancienne
  priorité, nouvelle priorité, auteur, date/heure, motif.
- **FR-072** (PRI-04, Must) : Le client NE DOIT PAS pouvoir déclarer librement sa demande urgente
  ou critique, sauf si un processus métier particulier le prévoit explicitement.

### Actions de masse (MAS)

- **FR-073** (MAS-01, Should) : Le superviseur DEVRAIT pouvoir, en masse sur une sélection de
  tickets : affecter, réaffecter, transférer de file, modifier la priorité, et réaliser d'autres
  actions administratives autorisées.
- **FR-074** (MAS-02, Must) : Toute action de masse DOIT être tracée **ticket par ticket** dans
  l'historique et le journal d'audit. *(Cette exigence Must est testable uniquement si MAS-01 est
  implémenté ; si MAS-01 est différé, MAS-02 doit être marquée non applicable pour l'incrément
  concerné — voir Edge Cases.)*

### Key Entities *(le socle manipule des données ; aucune structure de table ni classe n'est
définie ici — ce choix appartient à `/speckit.plan`)*

- **Ticket** : unité de traitement centrale (principe P1 du CDC). Porte un numéro unique, un
  module d'origine, un type de demande, un statut courant (propre au workflow de son module), une
  priorité, une file courante, un opérateur assigné le cas échéant, un dossier de rattachement
  éventuel, des liens vers d'autres tickets, et des horodatages (arrivée en file, prise en
  charge, début de traitement, dernière activité, clôture).
- **Dossier** : regroupement d'un ou plusieurs tickets issus d'une même demande, permettant un
  suivi global (SOC-11). Distinct de la liaison inter-tickets (SOC-10), qui associe des tickets
  sans nécessairement les regrouper sous un même dossier. Son état est **dérivé** de ses tickets :
  lorsque tous ses tickets sont clôturés ou annulés, il suit leur sort (archivé au sens de
  SOC-09, consultable en permanence) et la réouverture d'un ticket le réactive ; son numéro
  n'est jamais réutilisé (CLAR-01).
- **File de traitement** : ensemble de tickets en attente pour un rôle donné ; porte des règles
  de paramétrage (mode d'affectation, SLA applicables) et des transitions autorisées vers
  d'autres files.
- **Historique du ticket** : chronologie fonctionnelle des événements d'un ticket, consultable
  par le client et les agents habilités — distincte du journal d'audit (voir ci-dessous).
- **Journal d'audit** : registre inviolable des actions du système, consultable par
  l'administrateur technique, répondant à une finalité de sécurité/conformité distincte de
  l'historique fonctionnel (SOC-13, cf. Principe IX de la constitution).
- **Message / Commentaire** : élément du fil d'échange entre client et agents sur un ticket,
  horodaté (SOC-05, MSG-01).
- **Note interne** : élément de communication réservé aux collaborateurs CIT habilités, jamais
  visible du client (MSG-02).
- **Document** : pièce déposée sur un ticket par le client ou un agent, avec type, taille et
  contrôles de conformité associés (SOC-07).
- **SLA (définition et instance mesurée)** : règle de délai cible paramétrable applicable à un
  périmètre (module/étape/file/priorité), avec ses seuils ; une instance de SLA suit l'état d'un
  ticket donné vis-à-vis de cette règle.
- **Alerte** : notification déclenchée par le franchissement d'une condition opérationnelle
  (seuil SLA, backlog, inactivité), destinée en priorité au superviseur.
- **Disponibilité opérateur** : état courant d'un agent (Disponible / Indisponible / Absent)
  influençant les affectations automatiques.
- **Priorité** : valeur issue d'un référentiel paramétrable, attribuée à un ticket, influençant
  son traitement.
- **Action de masse** : opération appliquée par un superviseur à une sélection de tickets,
  décomposée et tracée individuellement pour chaque ticket concerné.
- **Matrice des transitions autorisées** : règle paramétrable définissant, pour chaque transition
  file source → file cible, les destinations autorisées, les rôles habilités à initier le
  transfert et les motifs autorisés (TRF-06, CLAR-04) — son contenu exact reste à valider avec
  le métier (PC-07).

## Sécurité *(critères fonctionnels d'autorisation — l'implémentation technique de ces contrôles
appartient à `/speckit.plan`, conformément au Principe VII de la constitution)*

Le socle DOIT structurellement permettre l'application des critères suivants (portés par
d'autres modules/exigences transverses, mais dont l'application concrète repose sur le socle
défini ici) :

| Critère | Exigence CDC référencée | Application dans ce socle |
|---|---|---|
| Cloisonnement client | CPT-04 (module Comptes, hors périmètre), SEC-03 | Un client ne DOIT accéder qu'à ses propres tickets, documents et échanges (US1, US6) |
| Accès aux files selon habilitation | SOC-04 (dans le périmètre) | Un agent ne DOIT voir que les tickets de sa/ses file(s) de rôle (US1) |
| Accès aux actions selon rôle | SEC-02 (RBAC) | Les actions de transfert, réaffectation, action de masse DOIVENT être réservées aux rôles habilités (US2, US10) |
| Confidentialité des notes internes | MSG-02 (dans le périmètre) | Une note interne ne DOIT jamais être visible du client (US5) |
| Contrôle des documents déposés | SOC-07 (dans le périmètre), SEC-07, ERR-03 (hors périmètre) | Type, taille, intégrité contrôlés à chaque dépôt, avec message d'erreur explicite (US6) |
| Traçabilité des actions | SOC-13 (dans le périmètre), SEC-05 (hors périmètre — même journal, portée sécurité) | Toute action socle génère une entrée d'audit inviolable (US11) |

Une restriction affichée uniquement dans l'interface NE constitue JAMAIS une autorisation
suffisante : tout contrôle listé ci-dessus DOIT pouvoir être vérifié indépendamment de
l'interface utilisée (conformité au Principe VII de la constitution).

## Paramétrabilité *(comportements devant rester administrables sans développement, conformément
au CDC section 11 et au Principe VI de la constitution — les valeurs elles-mêmes ne sont ni
connues ni fixées ici)*

Cette feature NE DOIT PAS coder en dur les éléments suivants ; leurs valeurs concrètes restent
des paramètres de configuration, à renseigner lors du cadrage (voir « Clarifications ») :

- les étapes bloquant l'annulation libre d'un ticket (CYC-01) ;
- le nombre et la cadence des relances automatiques en attente client (CYC-04) ;
- la matrice des transitions autorisées entre files, y compris les rôles initiateurs et les
  motifs par transition (TRF-06 — PC-07) ;
- le périmètre (files et/ou services) rattaché à chaque superviseur, dont la composition exacte
  reste à définir par le métier (PC-04 — CLAR-05) ;
- la destination des demandes d'annulation au-delà de l'étape bloquante — file d'origine ou
  responsable habilité (CYC-02) ;
- les modes et règles d'affectation par file (AFF-01 à AFF-07) ;
- les niveaux de priorité et les règles d'attribution automatique (PRI-01, PRI-02) ;
- le périmètre, les seuils et les durées cibles des SLA (SLA-01, SLA-02, SLA-05 — PC-12) ;
- la prise en compte des heures ouvrées dans le calcul des SLA (SLA-04) ;
- les règles d'alerte, seuils, destinataires et canaux (ALE-02) ;
- les types de pièces exigées par étape et les formats/tailles acceptés (SOC-07 — PC-08) ;
- les motifs de transfert, d'annulation, de rejet.

## Testabilité

> **Correction de périmètre (28 août 2026, voir `recipe-scope-audit.md`)** : le CDC §22 définit 14
> scénarios de recette contractuels. **REC-SCE-01 à REC-SCE-08** portent sur des parcours métier
> complets (Billing Office IMPORT/EXPORT, Simulation de Proforma, Customer Complaint, Enrôlement
> Transporteurs) qui appartiennent à des features futures consommant ce socle — ce ne sont **pas**
> des scénarios de recette de la Feature 001, même lorsqu'ils exercent accessoirement une exigence
> du socle. Seuls **REC-SCE-09 à REC-SCE-14** sont exécutables dans le seul périmètre de ce socle.

Conformément à `docs/04-matrice-recette.md`, les scénarios de recette contractuels du CDC §22
applicables à ce socle sont :

| Scénario CDC | Couvre (dans ce périmètre) |
|---|---|
| REC-SCE-09 | Prise en charge, horodatages (US1 — FR-012, FR-030 à FR-033) |
| REC-SCE-10 | Transfert de file, conservation de l'historique, refus de destination non autorisée (US2 — FR-035 à FR-040) |
| REC-SCE-11 | Réaffectation entre opérateurs selon habilitations (US2 — FR-038) |
| REC-SCE-12 | Temps et SLA : mesure, distinction interne/client, seuils, escalade (US8 — FR-041 à FR-053) |
| REC-SCE-13 | Supervision : vue temps réel, alertes, affectation, déblocage (US10 — FR-054 à FR-058) |
| REC-SCE-14 (partiel) | Regroupement en dossier, contrôle des habilitations, journal d'audit (US1, US6, US7, US11) |

**Exigences Must de ce périmètre actuellement sans scénario CDC exécutable dans le seul périmètre
de la Feature 001** (24 au total — recalculées mécaniquement, voir `recipe-scope-audit.md` ; elles
ne sont pas facultatives pour autant — un Test Plan complémentaire DOIT être défini lors de
`/speckit.tasks`) :

- Socle du ticket, sans scénario exécutable hors Billing Office (SOC-02, SOC-03, SOC-05, SOC-06,
  SOC-07, SOC-08, SOC-09) — US1, US4, US5, US6
- Messages et notes internes (MSG-01, MSG-02, MSG-03, MSG-05) — US5
- Cycle de vie (CYC-01, CYC-02, CYC-03, CYC-04, CYC-05) — US3, US4
- Moteur de workflow (WKF-01, dont le seul scénario CDC est un parcours Billing Office) — US1, US2
- Disponibilité (DIS-03) — US9
- Affectation sans développement (AFF-07) — US9
- Priorités (PRI-01, PRI-02, PRI-03, PRI-04) — US10
- Traçabilité des actions de masse (MAS-02, conditionnée à MAS-01) — US10

*(9 de ces 24 — SOC-02, SOC-03, SOC-05, SOC-06, SOC-07, SOC-08, SOC-09, MSG-01, WKF-01 — sont
notées « Directe » ou « Partielle » dans `docs/04-matrice-recette.md` au niveau du CDC global, car
un scénario Billing Office comme REC-SCE-01 les exerce accessoirement ; mais ce scénario n'est pas
exécutable dans le seul périmètre de la Feature 001, d'où leur reclassement ici.)*

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001** : Un ticket créé obtient un numéro unique et apparaît dans la file de traitement
  correcte en moins de 3 secondes après soumission, dans des conditions de charge normales
  (aligné sur NFR-04 du CDC, cité sans en faire une exigence propre à cette feature).
- **SC-002** : 100 % des transferts de ticket conservent intégralement le numéro, le dossier,
  l'historique, les documents et les commentaires du ticket (zéro perte constatée en recette).
- **SC-003** : 100 % des tentatives de transfert vers une destination non autorisée par la
  matrice de transitions sont refusées par le système (zéro contournement constaté).
- **SC-004** : 100 % des actions sensibles définies dans le socle (création, prise en charge,
  transfert, réaffectation, annulation, réouverture, action de masse) produisent une entrée
  d'audit vérifiable, y compris pour les actions de masse (traçage ticket par ticket).
- **SC-005** : Un superviseur peut identifier, depuis sa vue temps réel, un ticket non pris en
  charge dépassant le seuil d'alerte configuré, sans recherche manuelle.
- **SC-006** : Le temps passé par un ticket en état « attente client » n'est jamais comptabilisé
  dans les indicateurs de performance d'un opérateur ou d'une file (0 % d'imputation constatée).
- **SC-007** : Une note interne n'est jamais visible par un compte client, dans 100 % des
  vérifications de recette (SC associé à MSG-02, exigence de confidentialité non négociable).
- **SC-008** : Les paramètres listés dans la section « Paramétrabilité » (transitions, seuils
  SLA, règles d'affectation, priorités) sont modifiables par un administrateur fonctionnel sans
  intervention d'un développeur.

## Assumptions

- La notion de « module appelant » (Billing Office, Customer Complaint, Enrôlement) est traitée
  comme une donnée d'entrée du socle : ce socle ne spécifie pas comment chaque module détermine
  le type de demande ou le profil client, seulement comment le ticket qui en résulte est géré une
  fois créé.
- Le concept de « rôle habilité » ou « agent habilité » est utilisé tel que nommé par le CDC,
  sans préjuger du modèle RBAC détaillé (rôles concrets, permissions), qui appartient à
  `/speckit.plan` ; les rôles concrets (Pilote Manifeste, Superviseur, etc.) sont définis dans
  `docs/00-analyse-cdc.md` section 4 et ne sont pas redéfinis ici.
- Le socle fournit les événements déclencheurs nécessaires aux notifications (ex. TRF-05 : «
  le client est notifié d'une réorientation »), mais la manière dont ces événements sont
  effectivement propagés vers les canaux (e-mail, portail, WhatsApp/SMS) relève d'une feature
  Notifications distincte, non spécifiée ici.
- La disponibilité opérateur (DIS-01) est traitée comme une donnée à trois états
  (Disponible/Indisponible/Absent) sans préjuger du mécanisme de déclaration (manuel,
  automatique par inactivité, etc.), non précisé par le CDC pour ce périmètre.
- Un ticket appartient à une seule file à la fois et à un seul opérateur assigné à la fois — c'est
  une lecture directe de PEC-02 et TRF-01, qui ne contredit aucune exigence du CDC.
- Aucune valeur concrète de SLA, de seuil, ni de matrice de transitions n'est fixée par cette
  spécification : toutes restent des paramètres de configuration (voir « Paramétrabilité »),
  conformément au Principe XIII de la constitution (« Ne pas inventer les valeurs métier
  actuellement non définies »).

## Clarifications nécessaires avant planification

> Ces points sont cités avec leur référence dans `docs/02-points-a-clarifier.md` et classés
> selon leur impact sur la suite du projet. Certains ont été **résolus** lors de la session de
> clarification du 28/08/2026 (voir la colonne Classification et la section « Clarifications ») ;
> les autres demeurent ouverts.

| # | Point | Exigences concernées | Classification | Décision attendue |
|---|---|---|---|---|
| PC-07 | Matrice des transitions autorisées entre files — le CDC ne fournit que des valeurs illustratives, « à définir avec le métier » | TRF-06, WKF-01 (US2) | **STRUCTURE RÉSOLUE le 28/08/2026 (CLAR-04 : matrice riche — destinations + rôles initiateurs + motifs par transition)** / **CONTENU (paramètre) À CONFIRMER AVANT RECETTE/PRODUCTION** | Atelier métier pour valider la liste exhaustive des files et des transitions autorisées |
| PC-12 | Valeurs cibles des SLA (délais, seuils par module/étape/file) non définies par le CDC | SLA-01, SLA-02, SLA-05 (US8) | **NON BLOQUANTE POUR PLAN** *(paramètre administrable sans développement — SLA-05)* / **À CONFIRMER AVANT RECETTE/PRODUCTION** | Atelier avec les chefs de service pour fixer les SLA cibles |
| PC-20 | Rôle habilité à clôturer un ticket pour absence de réponse (CYC-04/CYC-05) non précisé | CYC-04, CYC-05 (US3) | **NON BLOQUANTE POUR PLAN** *(représentable comme habilitation configurable par module)* / **À CONFIRMER AVANT IMPLEMENTATION** | Décision métier par module sur les rôles habilités |
| PC-24 | Devenir du dossier lorsque tous ses tickets sont clôturés ou annulés | SOC-11 (US7) | **RÉSOLUE le 28/08/2026 (CLAR-01, décision métier déléguée) — état du dossier dérivé de ses tickets** | — (voir « Clarifications ») |
| PC-04 | Périmètre exact d'un superviseur (limité à son service, ou transverse) non précisé par le CDC | SUP-04, DIS-02, MAS-01 (US9, US10) | **STRUCTURE RÉSOLUE le 28/08/2026 (CLAR-05 : périmètre borné et configurable)** / **COMPOSITION EXACTE (paramètre) À CONFIRMER AVANT IMPLEMENTATION** | Définition de la composition du périmètre par la direction métier |
| PC-02 | Latence et fréquence de rafraîchissement acceptables pour la supervision temps réel non précisées | SUP-01, SUP-02, SUP-03, ALE-01 (US8, US10) | **NON BLOQUANTE POUR PLAN** / **À CONFIRMER AVANT IMPLEMENTATION** | Le métier définit une latence maximale acceptable |
| PC-01 | Volumétrie opérationnelle cible (tickets/jour, pièces jointes, utilisateurs simultanés) non confirmée | Dimensionnement transversal du socle (toutes les US) | **NON BLOQUANTE POUR PLAN** / **À CONFIRMER AVANT PRODUCTION** | Atelier de cadrage avec Billing Office, Gate et DSI |
| PC-25 | Règles de notification interne CIT (agents) : événements déclencheurs et canaux non précisés | MSG-04, ALE-01, SUP-01 (US5, US8, US10 — limité aux événements déclencheurs, hors canaux) | **NON BLOQUANTE POUR PLAN** / **À CONFIRMER AVANT IMPLEMENTATION** | Décision métier/DSI, en coordination avec la future feature Notifications |
| PC-23 | Critères de recherche de tickets exhaustifs par profil (client, agent, superviseur, admin) non entièrement définis | SOC-04, SUP-04 (US1, US10) | **NON BLOQUANTE POUR PLAN** | Validation de la matrice de recherche par profil en conception détaillée |
| PC-03 | Rôles non définis en section 6 du CDC (Caisse/Finance, Agent de guichet, Intervenant Avoir/Exo) | Indirect : ces rôles agiraient comme « agent habilité » générique dans ce socle, sans que leur identité précise soit nécessaire à la spécification du socle lui-même | **NON BLOQUANTE POUR PLAN** *(le socle ne nomme aucun rôle métier concret)* | Atelier avec direction métier — pertinent surtout pour les modules consommateurs (Billing Office, Avoir/Exo) |

> **Mise à jour du 28/08/2026 (`/speckit.clarify`)** : PC-24 est résolu ; PC-07 et PC-04 sont
> résolus sur leur **structure** (leur *contenu*/*composition* reste un paramètre à valider avec
> le métier) — voir la section « Clarifications » ci-dessous. Les autres points demeurent
> ouverts. Les hypothèses de travail nécessaires à la formulation des User Stories (ex. le
> mécanisme abstrait de disponibilité à trois états) restent documentées dans « Assumptions »
> et révisables.

## Clarifications

> Session `/speckit.clarify` du **2026-08-28**. Chaque décision suit la traçabilité :
> Question → Réponse → Exigences concernées → User Story → Critère(s) d'acceptation.
>
> **Mode de validation** : les cinq décisions (CLAR-01 à CLAR-05) ont été validées par
> l'utilisateur au fil de la session ; le choix final entre les options proposées lui a été
> expressément délégué (« applique la meilleure recommandation selon ton expertise ») et a
> systématiquement suivi la recommandation de l'analyste, chaque fois ancrée dans les
> principes du CDC. **Aucune règle métier nouvelle n'est inventée ; aucune exigence du CDC
> n'est modifiée** ; les valeurs métier concrètes (contenu de la matrice, SLA, périmètres,
> habilitations) restent des paramètres non fixés ici.

### CLAR-01 — Devenir du dossier lorsque tous ses tickets sont clôturés ou annulés (PC-24)

- **Question** : le dossier porte-t-il un cycle de vie propre (clôture manuelle du dossier) ou
  dérivé de ses tickets ?
- **Réponse** : cycle de vie **dérivé**. Le dossier ne porte aucun état ni action métier
  propre : lorsque tous ses tickets sont clôturés ou annulés, il suit leur sort (considéré
  comme terminé et archivé au même titre qu'eux, au sens de SOC-09), il reste consultable en
  permanence, la réouverture de l'un de ses tickets le réactive automatiquement, et son numéro
  n'est jamais réutilisé. Justification : SOC-11 ne définit le dossier que comme regroupement
  de suivi ; créer une action de « clôture de dossier » inventerait une règle absente du CDC.
- **Exigences** : SOC-11 (FR-011), SOC-09 (FR-009) — point PC-24
- **User Story** : US7 (nouveau scénario d'acceptation 3)
- **Critères d'acceptation** : scénario 3 de US7 ; Key Entities « Dossier » ; Edge Case « tous
  les tickets du dossier clôturés/annulés » marqué résolu.

### CLAR-02 — Périmètre de la réouverture (CYC-06)

- **Question** : la réouverture s'applique-t-elle aussi aux tickets annulés, ou aux seuls
  tickets clôturés ?
- **Réponse** : aux **seuls tickets clôturés** (clôture normale ou pour absence de réponse).
  Un ticket annulé ne peut pas être rouvert : le client crée une nouvelle demande (CYC-01) ou
  sollicite une réouverture (CYC-07). Justification : le CDC distingue strictement l'annulation
  (CYC-01/02) de la clôture (SOC-09, CYC-04/05) ; CYC-06 vise « un ticket clôturé ».
- **Exigences** : CYC-06, CYC-07 (FR-020, FR-021)
- **User Story** : US4 (scénario d'acceptation 4 précisé)
- **Critères d'acceptation** : scénario 4 de US4 ; FR-020 annoté.

### CLAR-03 — Sort de l'opérateur après un transfert de file

- **Question** : après un transfert vers une autre file, le ticket conserve-t-il son opérateur
  assigné ou repart-il en attente de prise en charge ?
- **Réponse** : il repart en attente, **sauf destinataire explicitement désigné**. Le ticket
  arrive dans la file de destination à l'état « en attente de prise en charge » (non affecté —
  PEC-01) et un nouveau temps d'attente est mesuré pour la nouvelle étape (TPS-01) ; le SLA
  applicable après transfert est celui de la nouvelle étape/file (SLA-01). Si un destinataire
  est désigné lors du transfert (TRF-03 : « destinataire éventuel »), le ticket lui est
  directement affecté. Justification : PEC-01 veut que tout ticket en file non accepté soit «
  en attente de prise en charge » ; TRF-03 prévoit un destinataire *éventuel*.
- **Exigences** : TRF-01, TRF-03 (FR-035, FR-037), PEC-01 (FR-030), TPS-01 (FR-041), SLA-01
- **User Story** : US2 (nouveau scénario d'acceptation 7)
- **Critères d'acceptation** : scénario 7 de US2.

### CLAR-04 — Structure de la matrice des transitions (PC-07 — structure uniquement)

- **Question** : la matrice des transitions porte-t-elle uniquement les destinations, ou aussi
  les rôles habilités à initier chaque transfert et les motifs autorisés par transition ?
- **Réponse** : **matrice riche** — pour chaque transition file source → file cible, elle porte
  les destinations autorisées, les rôles habilités à initier le transfert et les motifs
  autorisés. Justification : PC-07 liste ces trois dimensions parmi les éléments à valider en
  atelier métier ; TRF-03 impose motif obligatoire et destinataire éventuel ; TRF-01 exige un
  opérateur habilité ; Principe VI (paramétrabilité). **Le contenu de la matrice (files,
  transitions, rôles, motifs) reste un paramètre à valider avec le métier avant
  recette/production — aucune valeur n'est fixée ici.**
- **Exigences** : TRF-06 (FR-040), TRF-01, TRF-03 (FR-035, FR-037)
- **User Story** : US2 (scénario 5 inchangé : refus des transitions hors matrice)
- **Critères d'acceptation** : FR-040 annoté ; Key Entities « Matrice des transitions
  autorisées » ; bullet matrice de la section « Paramétrabilité » étendu.

### CLAR-05 — Périmètre du superviseur (PC-04 — structure uniquement)

- **Question** : le périmètre du superviseur est-il structurellement borné par affectation, ou
  global par défaut ?
- **Réponse** : **borné et configurable**. Chaque superviseur est rattaché, par configuration
  sans développement, à un ensemble de files et/ou services ; ses vues (SUP-01 à SUP-03, SUP-05)
  et ses actions (SUP-04, DIS-02, MAS-01) sont limitées à ce périmètre. Un périmètre global
  reste possible comme configuration particulière. Justification : le CDC parle systématiquement
  de « son périmètre » (§9.8, SUP-03, SUP-05), ce qui implique un périmètre défini par
  affectation et non un accès global implicite. **La composition exacte du périmètre (quelles
  files/services pour quel superviseur) reste un paramètre à définir par la direction métier
  (PC-04 — non tranché ici).**
- **Exigences** : SUP-01 à SUP-05 (FR-054 à FR-058), DIS-02, MAS-01
- **User Story** : US10 (scénario d'acceptation 1 précisé)
- **Critères d'acceptation** : scénario 1 de US10 ; bullet « périmètre superviseur » ajouté à
  la section « Paramétrabilité ».

### Points volontairement laissés ouverts ou paramétrables (non tranchés le 28/08/2026)

| Point | Classification retenue | Moment de la décision |
|---|---|---|
| PC-07 — contenu de la matrice (files, transitions, rôles initiateurs, motifs) | Paramètre administrable (TRF-06) | Atelier métier, avant recette/production |
| PC-12 — valeurs cibles des SLA, seuils, horaires ouvrés, jours fériés | Paramètre administrable (SLA-05) | Atelier métier, avant recette/production |
| PC-20 — rôle(s) habilité(s) à clôturer pour absence de réponse | Habilitation configurable par module | Avant implémentation/recette |
| PC-04 — composition exacte du périmètre de chaque superviseur | Paramètre administrable (CLAR-05) | Avant implémentation/recette |
| PC-02 — latence/fréquence de rafraîchissement de la supervision temps réel | Paramètre de rafraîchissement | Avant implémentation de US10 |
| PC-01 — volumétrie opérationnelle (tickets/jour, pièces jointes, utilisateurs simultanés) | Donnée de dimensionnement infrastructure (non fonctionnelle) | Avant production (cadrage DSI) |
| PC-25 — règles de notification interne CIT (canaux, préférences agents) | Hors périmètre du socle (celui-ci ne fournit que les événements déclencheurs) | Future feature Notifications |
| PC-23 — critères de recherche exhaustifs par profil | Conception détaillée (matrice proposée dans `docs/02`) | Avant implémentation |
| PC-03 — rôles CDC manquants (Caisse/Finance, guichet, Avoir/Exo) | Non nécessaire au socle (il ne nomme aucun rôle concret) | Features consommatrices (modules métier) |
| Destination des demandes d'annulation au-delà de l'étape bloquante (CYC-02 : « file ou responsable habilité ») | Paramètre de routage (voir « Paramétrabilité ») | Conception détaillée |

## Matrice de couverture — Exigence CDC → User Story → Critère d'acceptation → Couverture

> **Correction de périmètre (28 août 2026, voir `recipe-scope-audit.md`)** : cette matrice a été
> recalculée mécaniquement pour ne citer que les scénarios **exécutables dans le seul périmètre de
> la Feature 001** (REC-SCE-09 à REC-SCE-14). Un scénario Billing Office/Simulation/Customer
> Complaint/Enrôlement (REC-SCE-01 à REC-SCE-08) n'est **jamais** cité comme preuve de couverture
> ici, même lorsque `docs/04-matrice-recette.md` le mentionne au niveau global du CDC (car il
> exerce accessoirement une exigence du socle au sein d'un parcours métier qui n'existe pas
> encore) : une exigence qui n'est couverte **que** par un tel scénario est reclassée
> « **Absente** » pour cette feature.

> Couverture : **Directe** = au moins un scénario REC-SCE-09→14 vérifie explicitement l'exigence ;
> **Partielle** = vérifiée en partie seulement au sein d'un tel scénario ; **Absente** = aucun
> scénario CDC §22 exécutable dans ce périmètre, un Test Plan complémentaire est requis en aval
> (ceci ne rend pas l'exigence facultative si Must).

> Couverture dérivée **mécaniquement** de `docs/04-matrice-recette.md` (Partie 3 pour les Must ;
> colonne « Exigences Should/Could couvertes » de la Partie 1 pour les Should/Could), filtrée sur
> l'intersection avec {REC-SCE-09, …, REC-SCE-14} — aucune réévaluation manuelle divergente,
> aucun scénario inventé, aucune exigence CDC supprimée. Détail de la méthode et des reclassements
> dans `recipe-scope-audit.md`.

| Exigence CDC | Priorité | User Story | Critère(s) d'acceptation (FR) | Couverture |
|---|---|---|---|---|
| SOC-01 | Must | US1 | FR-001 | Directe (REC-SCE-09 · REC-SCE-10 · REC-SCE-11 · REC-SCE-12 · REC-SCE-13 · REC-SCE-14) |
| SOC-02 | Must | US1, US6 | FR-002 | **Absente** |
| SOC-03 | Must | US1 | FR-003 | **Absente** |
| SOC-04 | Must | US1 | FR-004 | Directe (REC-SCE-09 · REC-SCE-13) |
| SOC-05 | Must | US5 | FR-005 | **Absente** |
| SOC-06 | Must | US1 | FR-006 | **Absente** |
| SOC-07 | Must | US6 | FR-007 | **Absente** |
| SOC-08 | Must | US1 | FR-008 | **Absente** |
| SOC-09 | Must | US4 | FR-009 | **Absente** |
| SOC-10 | Must | US7 | FR-010 | Partielle (REC-SCE-14) |
| SOC-11 | Must | US1, US7 | FR-011 | Directe (REC-SCE-14) |
| SOC-12 | Must | US1 | FR-012 | Directe (REC-SCE-09) |
| SOC-13 | Must | US11 | FR-013 | Directe (REC-SCE-14) |
| SOC-14 | Could | US10 | FR-014 | **Absente** |
| CYC-01 | Must | US4 | FR-015 | **Absente** |
| CYC-02 | Must | US4 | FR-016 | **Absente** |
| CYC-03 | Must | US4 | FR-017 | **Absente** |
| CYC-04 | Must | US3 | FR-018 | **Absente** |
| CYC-05 | Must | US3 | FR-019 | **Absente** |
| CYC-06 | Should | US4 | FR-020 | **Absente** |
| CYC-07 | Should | US4 | FR-021 | **Absente** |
| MSG-01 | Must | US5 | FR-022 | **Absente** |
| MSG-02 | Must | US5 | FR-023 | **Absente** |
| MSG-03 | Must | US5 | FR-024 | **Absente** |
| MSG-04 | Should | US5 | FR-025 | **Absente** |
| MSG-05 | Must | US5 | FR-026 | **Absente** |
| WKF-01 | Must | US1, US2 | FR-027 | **Absente** |
| WKF-02 | Must | US2 | FR-028 | Directe (REC-SCE-10 · REC-SCE-11) |
| WKF-03 | Must | US2 | FR-029 | Directe (REC-SCE-10 · REC-SCE-11) |
| PEC-01 | Must | US1 | FR-030 | Directe (REC-SCE-09) |
| PEC-02 | Must | US1 | FR-031 | Directe (REC-SCE-09) |
| PEC-03 | Must | US1 | FR-032 | Directe (REC-SCE-09) |
| PEC-04 | Must | US1 | FR-033 | Directe (REC-SCE-09) |
| PEC-05 | Should | US2 | FR-034 | **Absente** |
| TRF-01 | Must | US2 | FR-035 | Directe (REC-SCE-10) |
| TRF-02 | Must | US2 | FR-036 | Directe (REC-SCE-10) |
| TRF-03 | Must | US2 | FR-037 | Directe (REC-SCE-10) |
| TRF-04 | Must | US2 | FR-038 | Directe (REC-SCE-11) |
| TRF-05 | Must | US2 | FR-039 | Directe (REC-SCE-10) |
| TRF-06 | Must | US2 | FR-040 | Directe (REC-SCE-10) |
| TPS-01 | Must | US8 | FR-041 | Directe (REC-SCE-09 · REC-SCE-12) |
| TPS-02 | Must | US3, US8 | FR-042 | Directe (REC-SCE-12) |
| TPS-03 | Must | US8 | FR-043 | Directe (REC-SCE-12) |
| TPS-04 | Must | US3, US8 | FR-044 | Directe (REC-SCE-12) |
| TPS-05 | Should | US8 | FR-045 | Directe (REC-SCE-09) |
| SLA-01 | Must | US8 | FR-046 | Directe (REC-SCE-12) |
| SLA-02 | Must | US8 | FR-047 | Directe (REC-SCE-12) |
| SLA-03 | Must | US8 | FR-048 | Directe (REC-SCE-12) |
| SLA-04 | Should | US8 | FR-049 | Directe (REC-SCE-12) |
| SLA-05 | Must | US8 | FR-050 | Directe (REC-SCE-12) |
| ALE-01 | Must | US8, US10 | FR-051 | Directe (REC-SCE-12 · REC-SCE-13) |
| ALE-02 | Must | US8 | FR-052 | Directe (REC-SCE-12) |
| ALE-03 | Must | US8 | FR-053 | Directe (REC-SCE-12) |
| SUP-01 | Must | US10 | FR-054 | Directe (REC-SCE-13) |
| SUP-02 | Must | US10 | FR-055 | Directe (REC-SCE-13) |
| SUP-03 | Must | US10 | FR-056 | Directe (REC-SCE-13) |
| SUP-04 | Must | US10 | FR-057 | Directe (REC-SCE-13) |
| SUP-05 | Must | US10 | FR-058 | Directe (REC-SCE-13) |
| DIS-01 | Must | US9 | FR-059 | Partielle (REC-SCE-13) |
| DIS-02 | Must | US9 | FR-060 | Partielle (REC-SCE-13) |
| DIS-03 | Must | US9 | FR-061 | **Absente** |
| AFF-01 | Must | US1 | FR-062 | Partielle (REC-SCE-09) |
| AFF-02 | Must | US2 | FR-063 | Partielle (REC-SCE-13) |
| AFF-03 | Should | US9 | FR-064 | **Absente** |
| AFF-04 | Should | US9 | FR-065 | **Absente** |
| AFF-05 | Should | US9 | FR-066 | **Absente** |
| AFF-06 | Should | US9 | FR-067 | **Absente** |
| AFF-07 | Must | US9 | FR-068 | **Absente** |
| PRI-01 | Must | US10 | FR-069 | **Absente** |
| PRI-02 | Must | US10 | FR-070 | **Absente** |
| PRI-03 | Must | US10 | FR-071 | **Absente** |
| PRI-04 | Must | US10 | FR-072 | **Absente** |
| MAS-01 | Should | US10 | FR-073 | Directe (REC-SCE-13) |
| MAS-02 | Must | US10 | FR-074 | **Absente** (conditionnée à MAS-01) |

**Aucune exigence du périmètre Socle (groupe A) et Workflow/Supervision (groupe B) de
`docs/01-matrice-exigences.md` n'est absente de cette matrice** : les 74 lignes ci-dessus
couvrent exhaustivement SOC-01→14, CYC-01→07, MSG-01→05, WKF-01→03, PEC-01→05, TRF-01→06,
TPS-01→05, SLA-01→05, ALE-01→03, SUP-01→05, DIS-01→03, AFF-01→07, PRI-01→04, MAS-01→02.
