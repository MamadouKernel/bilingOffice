# Matrice CDC globale post-F017 — 188 exigences

> **Document** : `docs/33-post-f017-global-cdc-matrix.md`  
> **Établi le** : 2026-09-02, sur le commit `d80a126`
> **Révisé le** : 2026-09-03 — **huit exigences déclassées** après audit indépendant
> (`docs/88-audit-conformite-cdc.md`)
> **Révisé le** : 2026-09-04 — **sept de ces huit refermées**, corrigées et testées
> **Révisé le** : 2026-09-04 (soir) — **audit complet des 188** (`docs/89`) : treize exigences
> supplémentaires déclassées, dont deux de sécurité, refermées le même jour
> **Révisé le** : 2026-09-04 (nuit) — **les neuf dernières fonctions sans interface sont
> câblées** : `SUP-04`, `PEC-05`, `MAS-01`, `MAS-02`, `PRI-03`, `SOC-10`, `SOC-02`, `SOC-14`,
> `ENR-05`. Score : **172 / 188 (91,49 %)**  
> **Sources** : `docs/cdc_text.txt` (texte contractuel), `docs/01-matrice-exigences.md`,
> `specs/001` à `specs/017`, `docs/32-post-f017-feature-selection-gate.md`  
> **Méthode** : extraction automatique des 188 lignes de la matrice, rattachement aux
> features par recherche des identifiants dans les `spec.md`, priorités relues **dans le
> texte du CDC** et non dans les documents dérivés.

---

## 1. Résultat

| Statut | Nombre | Part |
|:---|---:|---:|
| `CERTIFIED` | 172 | 91,49 % |
| `PARTIAL` | 2 | 1,06 % |
| `IMPLEMENTED_NOT_ENVIRONMENT_PROVEN` | 3 | 1,60 % |
| `NOT_IMPLEMENTED` (dont bloquées) | 11 | 5,85 % |
| **Total** | **188** | **100 %** |

Le comptage reste exact : 172 + 2 + 3 + 11 = 188.

**La baseline de 174 / 188 (92,55 %) n'était pas tenable.** Elle était fondée sur un rattachement
des exigences aux `spec.md`, c'est-à-dire sur l'**intention** déclarée par une feature — non sur
une lecture du code livré. L'audit du 2026-09-03 a confronté huit de ces exigences à
l'implémentation : aucune n'était entièrement satisfaite. Sept l'ont été depuis, par du code et
des tests. L'audit complet du 2026-09-04 (`docs/89`) a ensuite confronté **les 188** au code, et
non plus un échantillon : il en a déclassé treize de plus, dont deux de sécurité — refermées
depuis, ainsi que le parcours client. Le **163** d'aujourd'hui est constaté exigence par
exigence, non hérité d'un rattachement aux specs.

### Correction d'audit du 2026-09-03 — huit exigences déclassées, sept refermées depuis

**État au 2026-09-04 : sept des huit sont corrigées et testées.** Seule `COM-01` reste `PARTIAL`,
et pour une raison qui n'est pas d'ingénierie — voir la fin de cette section.

| Réf. | Ce que le CDC exige | Ce que le code livre |
|:---|:---|:---|
| `SLA-01` | Paramétrage par module, type de demande, **étape, file, priorité** | `ParametreSlaAdministrable` porte `Module` et `TypeDemande`. **Deux axes sur cinq** |
| `SLA-02` | Quatre seuils : information, alerte, dépassement, **escalade** | `SeuilInformation` et `SeuilAlerte` seuls ; aucun seuil d'escalade |
| `SLA-05` | Seuils et durées cibles administrables **sans développement** | `GererParametresSlaCommand` existe ; **aucun écran ne l'expose** |
| `ALE-02` | Règles, seuils, **destinataires** et canaux paramétrables | Les destinataires sont **deux classes C#** ; en ajouter un exige d'écrire du code |
| `ALE-03` | Escalade : notifier, remonter la priorité, réaffecter | Notification seule. `IResolveurDestinataires` **déclare lui-même** : « ALE-03 reste hors périmètre de raccordement » |
| `SOC-07` | Types de pièces paramétrables **par étape** | Formats et taille : conformes. **Par étape : absent** |
| `COM-03` | Modèles paramétrables **par module, par étape**, par canal | `RegleDiffusion` porte `TypeEvenement` et `Canal` seuls |
| `COM-01` | Au moins **un canal de messagerie mobile** | `CanalNotification` déclare `Sms` et `WhatsApp` ; **aucun fournisseur implémenté** |

#### Ce qui a été livré le 2026-09-04

| Réf. | Correction | Preuve |
|:---|:---|:---|
| `SLA-01` | `FileId` (l'étape) et `PrioriteId` sur `ParametreSlaAdministrable` ; résolution du plus spécifique, ordre total | `ParametresSlaAxesTests` (8) |
| `SLA-02` | `SeuilEscalade`, refusé s'il précède le dépassement ; `StatutSla.Escalade` | idem |
| `SLA-05` | Écran `/administration/parametres-sla` | — |
| `ALE-02` | Destinataires déduits des `PerimetreSupervision` administrés | `RegleEscaladeTests` (8) |
| `ALE-03` | `RegleEscalade` — notifier / remonter la priorité / remettre en distribution ; écran `/administration/regles-escalade` | idem |
| `SOC-07` | `PieceExigeeEtape` + écran `/administration/pieces-exigees-etape` | `PieceExigeeEtapeTests` (8) |
| `COM-03` | `Module` et `FileId` sur `RegleDiffusion` ; filtrage à la distribution | `DistribuerNotificationsUseCaseTests` (4) |

**Toutes ces corrections sont livrées VIDES** — référentiels sans contenu, axes optionnels nuls.
Le comportement d'une plateforme non paramétrée est strictement inchangé, et aucune valeur métier
n'a été inventée. Quatre migrations, toutes additives.

#### Audit complet du 2026-09-04 — treize exigences déclassées

`docs/89-audit-complet-188-exigences.md` a confronté **les 188 exigences** au code livré, et non
plus un échantillon. Sa découverte centrale :

> **29 des 150 commandes et requêtes applicatives ne sont appelées par aucun écran.** Cinq sont des
> traitements de fond, légitimement sans interface. Les autres portent des fonctions exigées par le
> CDC : implémentées, testées, tracées — et hors d'atteinte de tout utilisateur.

Déclassées : `SUP-04`, `PEC-05`, `MAS-01`, `MAS-02`, `PRI-03`, `SOC-10`, `SOC-02`, `ERR-01`,
`IMP-10`, `SOC-14`, `ENR-05`, `SEC-03`, `SEC-12`.

**`SEC-03` et `SEC-12` relèvent de la sécurité**, pas de l'ergonomie : quatre écrans prototypes
(`Tickets/Documents`, `FilMessages`, `MaFile`, `Soumettre`) ne vérifient ni authentification ni
habilitation, injectent un repository en contournant la couche applicative, et font **saisir
l'identité de l'auteur dans un champ de formulaire**. Le contrôle d'habilitation porte donc sur
une valeur choisie par l'appelant.

#### Correction du 2026-09-04 (soir) — `SEC-03` et `SEC-12` refermées

Les quatre écrans prototypes sont traités :

| Écran | Traitement |
|:---|:---|
| `Tickets/FilMessages` | **Retiré** — `FicheTicket` publie déjà les messages |
| `Tickets/MaFile` | **Retiré** — les files par module font le travail |
| `Tickets/Soumettre` | **Retiré** — les soumissions par module font le travail |
| `Tickets/Documents` | **Sécurisé** — seul à porter le dépôt de document, sans équivalent |

Aucun lien de l'interface ne menait à ces routes. `Documents.razor` lit désormais par
`ObtenirFicheTicketQuery`, qui applique le cloisonnement, et prend l'identité dans le contexte
authentifié.

**Deux autres fuites, trouvées par le test structurel** : `PrendreRendezVous` lisait un ticket
par `ITicketRepository` — un client pouvait afficher le numéro, le module et le BL de n'importe
quel ticket en devinant son identifiant. `AgendaGuichet` lisait l'agenda sans habilitation. Les
deux passent par un cas d'usage.

**21 pages reçoivent `[Authorize]`.** Elles vérifiaient déjà l'habilitation en page ; la garde de
route ajoute une redirection vers la connexion au lieu d'une page qui se rend puis refuse.

`SurfaceRoutableTests` ferme la porte : toute page routable qui injecterait un repository, ou qui
n'aurait ni `[Authorize]` ni inscription explicite parmi les pages publiques, échoue désormais au
build. Ce test a lui-même trouvé les deux fuites ci-dessus.

#### Correction du 2026-09-04 (soir) — parcours client : `IMP-10` et `ERR-01` refermées

| Réf. | Correction |
|:---|:---|
| `IMP-10` | Bouton « Demander ma facture » sur la fiche du ticket, pour le client, sur un ticket Billing actif. Le droit `PeutDemanderFacture` est calculé dans `ObtenirFicheTicketQuery`, comme tous les autres — jamais déduit dans la page |
| `ERR-01` | Le brouillon est écrit **avant** l'envoi et effacé **au succès seulement** : si la soumission échoue, la saisie est retrouvée au retour, avec un bandeau qui le dit |
| `IMP-12` / `EXP-03` | Lien « Joindre un document » depuis la fiche : l'écran existait, rien n'y menait. C'est par là que le client attache sa preuve de paiement |

`ServicesAtteignablesTests` empêche désormais tout nouveau service applicatif d'être livré sans
écran. Il porte une liste de **dette connue** — les fonctions encore hors d'atteinte, chacune avec
l'exigence qu'elle bloque. Cette liste ne doit que rétrécir, et un second test échoue si une
entrée y reste alors qu'elle a été câblée.

**Le test a immédiatement révélé un effet de bord de mes propres retraits** : `ConsulterFileQuery`,
`PriseLibreCommand` et `CreerTicketCommand` n'étaient appelés que par les écrans prototypes
supprimés. Ce sont désormais trois services morts — à supprimer, mais c'est une décision du
projet, pas de l'audit.

#### Correction du 2026-09-04 (nuit) — les neuf dernières fonctions sans interface

La dette laissée par l'audit était de **quinze fonctions écrites, testées et hors d'atteinte**.
Elle est résorbée. Ce qui a été câblé, et par où :

| Réf. | Ce qui manquait | Ce qui a été livré |
|:---|:---|:---|
| `SUP-04`, `PEC-05` | L'écran de supervision n'injectait que deux requêtes de **comptage** : le superviseur voyait combien de tickets attendaient sans pouvoir en désigner un | `ListerTicketsDeLaFileQuery` — nommer les tickets d'une file, sous le **même périmètre** que les vues de comptage (CLAR-05). Affectation et réaffectation par des cas d'usage distincts : l'historique doit les séparer |
| `MAS-01`, `MAS-02` | Aucune redistribution en masse | Sélection multiple sur la file ouverte, appliquée par `ActionDeMasseCommand` — **ticket par ticket**, avec le détail des succès et des échecs plutôt qu'un verdict global |
| `PRI-03` | Aucun écran ne modifiait une priorité | Changement de priorité en masse, **motif obligatoire** : sans motif, le changement serait inexplicable après coup |
| `SOC-02` | Les documents se déposaient et se listaient ; ils ne se **téléchargeaient** pas | Bouton de téléchargement par document, voie agent ou voie client selon la nature authentifiée (SEC-12 : contrôle à **chaque** accès). Un document non analysé n'offre pas de bouton, il dit pourquoi |
| `SOC-10` | Aucune liaison entre tickets, et la fiche n'en montrait aucune | Panneau « Tickets liés » sur la fiche, visible du client comme des agents ; l'agent lie par **numéro** de ticket, la seule référence qu'il ait sous les yeux |
| `SOC-14` | Aucun export | Export CSV de la file pour le superviseur, de ses demandes pour le client. Format partagé `FormatCsv`, qui porte la **neutralisation des formules tableur** |
| `ENR-05` | La commande de rappel n'était appelée **par rien** — ni écran, ni ordonnancement | `RappelExpirationBadgesService` : un traitement de fond, puisque sa nature est d'être périodique. Il ne fait rien tant que les paramètres métier ne sont pas administrés, et le dit sans lever d'erreur |
| `ENR` (renouvellement) | Le titulaire d'un badge délivré ne pouvait pas le renouveler | Bouton « Renouveler » sur un enrôlement clôturé |
| `MSG-04` | La mention d'un collaborateur était `CERTIFIED` sans écran | Champ d'adresse sur une note interne. Mentionner un **compte client** est refusé : MSG-02 interdit qu'il voie une note interne |
| `SIM-11` | Les contrôles de cohérence étaient `CERTIFIED` sans écran : on calculait un proforma sur des dates inversées sans rien dire | Contrôles exécutés **avant** le calcul et avant l'enregistrement ; les incohérences bloquent, les messages d'aide informent |
| § 17.3, `SUP-05`, `AFF-06` | Matrice de transitions, périmètres de supervision, compétences : trois commandes d'administration sans écran | Trois écrans, avec lecture, correction et retrait |

**Deux défauts trouvés en câblant, qui n'auraient pas été vus autrement.**

`ExporterIndicateursQuery` n'exigeait **ni habilitation ni périmètre**. Elle prenait un
identifiant de file et rendait ses tickets. Le défaut restait théorique tant qu'aucun écran ne
l'appelait ; la brancher telle quelle aurait ouvert l'export de n'importe quelle file à qui en
devine l'identifiant. Elle applique désormais le contrôle de `ListerTicketsDeLaFileQuery`.

`GererPerimetresSupervisionCommand` et `GererMatriceTransitionsCommand` ne savaient qu'**ajouter**.
Les deux dépôts lisent par `SingleOrDefaultAsync` : redéfinir un périmètre aurait créé une seconde
ligne, et la lecture aurait **levé à chaque ouverture de la supervision**. Corriger un périmètre
aurait donc interdit de superviser ; corriger une transition aurait bloqué tout transfert entre
les deux files. Les deux redéfinissent maintenant en place. `AdministrationRedefinitionTests`
couvre les deux cas.

**Ce qui reste dans `DetteConnue` n'est plus une fonction manquante** : huit entrées, dont six de
code mort — la fonction est livrée par un autre chemin — et deux services internes à qualifier.
`DeposerPreuvePaiementExportCommand` en fait partie : c'est un simple relais vers
`DeposerDocumentCommand`, que l'écran des documents appelle déjà. `EXP-03` est donc satisfaite,
et cette commande est du code mort, pas une fonction absente.

**Une réserve, à trancher par le projet.** `ValiderCoherenceSimulationQuery`, désormais visible du
client, porte deux valeurs métier écrites en dur : une franchise de séjour de **10 jours** et des
masses maximales de **32 t / 40 t**. Elles y étaient déjà ; les brancher les rend opposables au
client. Elles relèvent de PC-12 et devraient être administrées, non codées.

#### Correction du 2026-09-04 (soir) — `NFR-07` déclassée

L'utilisateur a signalé que le changement de langue ne produit aucun effet. Vérification faite :

| Constat | Mesure |
|:---|---:|
| Clés de traduction définies (`SharedResourcesLocalizer`) | 182 |
| Composants qui les utilisent | **1** sur 97 |
| Chaînes françaises codées en dur dans l'interface | **880** |

Le mécanisme est pourtant complet et correct : fournisseurs de culture, cookie, sélecteur de
langue, redirection. **Il n'est simplement branché sur presque rien** — seul
`ChampMotDePasse.razor` l'emploie. Basculer en anglais change la culture et n'affiche rien
d'autre.

`NFR-07` (« Interface disponible en anglais ») passe donc de `CERTIFIED` à `PARTIAL`. C'est une
`Should`, pas un bloquant contractuel, mais l'écart était réel et la matrice le masquait.

**Cet écart a échappé à l'audit du 2026-09-03**, qui avait contrôlé la traçabilité — `NFR-07` est
bien cité dans le code — sans vérifier la substance. C'est exactement le défaut de méthode que
cet audit reprochait à la matrice, reproduit par l'audit lui-même.

**`COM-01` ne peut pas être fermée par du code.** L'abstraction est complète et les canaux `Sms`
et `WhatsApp` sont déclarés ; écrire l'adaptateur suppose de **nommer un fournisseur**, ce qui est
une décision d'achat. Elle reste `PARTIAL` avec `ENR-04`, qui dépend du même arbitrage.

**Une incohérence interne est corrigée par la même occasion.** `COM-01` et `ENR-04` dépendent du
même fournisseur de messagerie absent. `ENR-04` était classée `NOT_IMPLEMENTED /
BLOCKED_PROVIDER`, `COM-01` était classée `CERTIFIED` : deux verdicts opposés pour une cause
unique.

**Le fil rouge — l'axe « étape ».** Trois de ces exigences (`SOC-07`, `COM-03`, `SLA-01`) échouent
sur le même point : la plateforme modélise le workflow mais n'expose pas l'étape comme axe de
paramétrage. Ce n'est pas trois oublis, c'est une dimension absente du modèle — ce qui explique
qu'aucun contrôle antérieur ne l'ait vue, chacun examinant les exigences une par une. L'entité
`PassageEnFile`, ajoutée le 2026-09-03, matérialise enfin cette notion et donne au correctif un
point d'ancrage qui n'existait pas.

---

## 2. Correction d'une erreur du document de référence

`docs/32-post-f017-feature-selection-gate.md` décrit les exigences restantes d'une manière
qui **ne correspond pas au texte du CDC**. Trois écarts, vérifiés ligne à ligne :

### 2.1 Les exigences IA sont décrites de façon inexacte

`docs/32` présente IA-01 à IA-07 comme sept cas d'usage distincts : prédiction d'affluence,
détection d'anomalies documentaires, routage intelligent, synthèse de réclamations, assistant
conversationnel, aide à la décision, analyse prédictive des contentieux.

**Aucune de ces formulations ne figure dans le CDC.** Le texte contractuel (lignes 794 à 817
de `docs/cdc_text.txt`) décrit sept exigences portant toutes sur **un seul et même assistant
conversationnel** :

| Réf. | Priorité CDC | Exigence (texte contractuel abrégé) |
|:---|:---|:---|
| `IA-01` | **Must** | L'assistant répond aux questions simples des clients (fonctionnement du portail, pièces à fournir, état d'avancement) en langage naturel, en français |
| `IA-02` | **Must** | L'assistant oriente le client dans ses démarches (choix du parcours, du module, de la catégorie de réclamation) et l'aide à compléter ses dossiers |
| `IA-03` | **Must** | L'assistant escalade vers un opérateur humain lorsque cela est nécessaire ; le passage se fait sans perte de contexte (le fil est conservé dans le tic |
| `IA-04` | **Should** | L'assistant s'appuie sur une base de connaissances administrable (FAQ, procédures) ; il cite la source de ses réponses lorsque pertinent |
| `IA-05` | **Should** | L'assistant est accessible depuis le portail et, si possible, depuis les canaux de messagerie configurés |
| `IA-06` | **Must** | L'assistant identifie les situations nécessitant une intervention humaine et permet le transfert vers un opérateur habilité sans perte de contexte |
| `IA-07` | **Must** | Le client peut demander une intervention humaine explicitement ; l'assistant réduit les sollicitations inutiles sans constituer une barrière |

### 2.2 Les priorités IA sont erronées

`docs/32` classe les sept exigences IA en **Could**. Le CDC en fait **cinq Must et deux
Should**. L'écart n'est pas cosmétique : il transforme une obligation contractuelle en
option, et minore d'autant le reste à faire.

### 2.3 Deux priorités NFR sont inversées

| Réf. | Priorité CDC | Priorité annoncée par `docs/32` | Effet |
|:---|:---|:---|:---|
| `NFR-05` | **Must** | Should | Obligation minorée |
| `NFR-09` | **Should** | Must | Obligation majorée |

### 2.4 Conséquence

Les 14 exigences restantes se répartissent en **10 Must et 4 Should** — et non en une
majorité de Could. Le pourcentage de conformité est inchangé ; la **nature de la dette**
l'est profondément.

---

## 3. Les 14 exigences non certifiées

| Réf. | Prio. CDC | Statut | Blocage | PC | Exigence |
|:---|:---|:---|:---|:---|:---|
| `ENR-04` | **Should** | `NOT_IMPLEMENTED` | `BLOCKED_PROVIDER` | PC-16 | Notification au chauffeur (SMS/WhatsApp prioritaires) à chaque étape clé : dossier validé, convocation, badge  |
| `IA-01` | **Must** | `NOT_IMPLEMENTED` | `BLOCKED_DSI` | PC-18 | L'assistant répond aux questions simples des clients (fonctionnement du portail, pièces à fournir, état d'avan |
| `IA-02` | **Must** | `NOT_IMPLEMENTED` | `BLOCKED_DSI` | PC-18 | L'assistant oriente le client dans ses démarches (choix du parcours, du module, de la catégorie de réclamation |
| `IA-03` | **Must** | `NOT_IMPLEMENTED` | `BLOCKED_DSI` | PC-18 | L'assistant escalade vers un opérateur humain lorsque cela est nécessaire ; le passage se fait sans perte de c |
| `IA-04` | **Should** | `NOT_IMPLEMENTED` | `BLOCKED_DSI` | PC-18 | L'assistant s'appuie sur une base de connaissances administrable (FAQ, procédures) ; il cite la source de ses  |
| `IA-05` | **Should** | `NOT_IMPLEMENTED` | `BLOCKED_DSI` | PC-18 | L'assistant est accessible depuis le portail et, si possible, depuis les canaux de messagerie configurés |
| `IA-06` | **Must** | `NOT_IMPLEMENTED` | `BLOCKED_DSI` | PC-18 | L'assistant identifie les situations nécessitant une intervention humaine et permet le transfert vers un opéra |
| `IA-07` | **Must** | `NOT_IMPLEMENTED` | `BLOCKED_DSI` | PC-18 | Le client peut demander une intervention humaine explicitement ; l'assistant réduit les sollicitations inutile |
| `NFR-03` | **Must** | `IMPLEMENTED_NOT_ENVIRONMENT_PROVEN` | `BLOCKED_ENVIRONMENT` | — | Disponibilité cible en heures ouvrées ≥ 99 % ; maintenance planifiée hors heures de pointe |
| `NFR-04` | **Must** | `IMPLEMENTED_NOT_ENVIRONMENT_PROVEN` | `BLOCKED_ENVIRONMENT` | — | Temps de réponse < 3 secondes pour les écrans courants dans des conditions normales de charge |
| `NFR-05` | **Must** | `NOT_IMPLEMENTED` | `BLOCKED_DSI` | PC-01 | Volumétrie dimensionnante à confirmer en phase de cadrage : tickets/jour, taille des pièces jointes, utilisate |
| `NFR-06` | **Must** | `NOT_IMPLEMENTED` | `BLOCKED_ENVIRONMENT` | — | Sauvegardes régulières (données et documents) et procédure de restauration testée |
| `NFR-09` | **Should** | `NOT_IMPLEMENTED` | `BLOCKED_DSI` | PC-13 | Hébergement conforme aux standards du groupe ; le choix (infrastructure CIT/AGL ou cloud) sera arrêté selon le |
| `NFR-10` | **Must** | `IMPLEMENTED_NOT_ENVIRONMENT_PROVEN` | `BLOCKED_ENVIRONMENT` | — | La plateforme est accessible 24h/24 et 7j/7, hors périodes de maintenance planifiées |

### Lecture

- **`IMPLEMENTED_NOT_ENVIRONMENT_PROVEN` (3)** — le code et l'instrumentation sont livrés en
  F016. Il manque une **mesure réelle** sur un environnement représentatif. Rien ne peut être
  certifié depuis un poste de développement.
- **`BLOCKED_DSI` (9)** — l'information manquante est une décision, pas un développement.
- **`BLOCKED_ENVIRONMENT` (1)** — NFR-06 dépend d'une infrastructure de sauvegarde.
- **`BLOCKED_PROVIDER` (1)** — ENR-04 dépend d'un contrat fournisseur.
- **`PARTIAL` (8)** — l'exigence est implémentée **en partie** : le comportement principal est
  livré, mais un élément explicitement nommé par le CDC manque. Le détail figure au § 1. Six de
  ces huit relèvent du code seul ; `COM-01` attend un fournisseur de messagerie, et `ALE-03`
  attend un arbitrage métier **en plus** du raccordement manquant.

---

## 4. Répartition des exigences certifiées par module

> Ce décompte par module n'a pas été recalculé après l'audit complet du 2026-09-04. Le total
> officiel est **159 / 188** ; les chiffres ci-dessous datent de la révision précédente.

Les huit exigences déclassées au § 1 avaient été retirées de ce décompte le 2026-09-03 ; sept y
sont réintégrées au 2026-09-04. Seule `COM-01` (Notifications) en reste exclue.

| Module | Certifiées |
|:---|---:|
| Socle | 67 |
| Billing IMPORT | 24 |
| Transverse | 12 |
| Sécurité | 12 |
| Comptes | 11 |
| Proforma | 11 |
| Notifications | 8 |
| Socle / Supervision | 7 |
| Customer Complaint | 6 |
| Billing EXPORT | 5 |
| Avoir/Exo | 4 |
| Enrôlement | 4 |
| Espace client | 1 |
| **Total** | **172** |

---

## 5. Matrice complète des 188 exigences

Rattachement établi par recherche de l'identifiant dans les `spec.md` de chaque feature.
Une exigence peut apparaître dans plusieurs specs lorsqu'elle a été traitée par étapes.

| Réf. | Prio. | Module | Statut | Feature(s) | Exigence |
|:---|:---|:---|:---|:---|:---|
| `SOC-01` | Must | Socle | `CERTIFIED` | 001, 003, 006, 007, 008, 009, 010 | Toute demande génère un ticket avec numéro unique communiqué au client (portail + e-mail) |
| `SOC-02` | Must | Socle | `CERTIFIED` | 001, 002, 003, 008, 009 | Le ticket centralise : documents, statut, historique horodaté, échanges client/agent, décisions |
| `SOC-03` | Must | Socle | `CERTIFIED` | 001 | Chaque ticket suit un workflow propre à son module et, pour le Billing Office, au type de clien |
| `SOC-04` | Must | Socle | `CERTIFIED` | 001, 006, 008, 009 | Files de traitement par rôle : chaque agent voit les tickets en attente de son rôle, avec tri e |
| `SOC-05` | Must | Socle | `CERTIFIED` | 001, 008, 009 | Fil de commentaires (chat) par ticket entre le client et les agents, horodaté, conservé dans l' |
| `SOC-06` | Must | Socle | `CERTIFIED` | 001, 002, 003, 006, 008, 009 | Actions de workflow standard : valider, rejeter avec motif obligatoire, demander un complément  |
| `SOC-07` | Must | Socle | `CERTIFIED` | 001, 003, 006, 007, 008 | Dépôt de documents par le client et par les agents ; types de pièces paramétrables par étape ;  |
| `SOC-08` | Must | Socle | `CERTIFIED` | 001, 009 | Statuts visibles par le client en temps réel sur son profil, sur chaque ticket |
| `SOC-09` | Must | Socle | `CERTIFIED` | 001, 003, 006, 009, 010 | Clôture d'un ticket suivie d'un archivage automatique ; les archives restent consultables en pe |
| `SOC-10` | Must | Socle | `CERTIFIED` | 001, 006, 007, 008 | Liaison entre tickets (y compris entre modules) pour conserver la continuité d'un même dossier  |
| `SOC-11` | Must | Socle | `CERTIFIED` | 001, 003, 009 | Dossier : les tickets d'une même demande multi-BL/booking regroupés sous un numéro de dossier u |
| `SOC-12` | Must | Socle | `CERTIFIED` | 001 | Le système distingue l'arrivée d'un ticket en file de sa prise en charge effective ; il enregis |
| `SOC-13` | Must | Socle | `CERTIFIED` | 001, 002, 003, 008, 009, 010 | Journal d'audit inviolable : toute action tracée avec auteur, date/heure et détail. *(Note : SE |
| `SOC-14` | Could | Socle | `CERTIFIED` | 001, 011, 012 | Export des indicateurs et listes de tickets aux formats usuels (Excel, PDF) |
| `CYC-01` | Must | Socle | `CERTIFIED` | 001 | Le client peut annuler lui-même un ticket tant qu'aucun traitement engageant ni document offici |
| `CYC-02` | Must | Socle | `CERTIFIED` | 001 | Au-delà de l'étape bloquante, le client soumet une demande d'annulation transmise à la file ou  |
| `CYC-03` | Must | Socle | `CERTIFIED` | 001 | Toute annulation ou demande d'annulation est motivée, horodatée et conservée dans l'historique |
| `CYC-04` | Must | Socle | `CERTIFIED` | 001, 009 | Relances automatiques paramétrables (nombre et cadence) ; au terme des relances, la plateforme  |
| `CYC-05` | Must | Socle | `CERTIFIED` | 001 | La suggestion de clôture pour absence de réponse n'est jamais proposée lorsque le ticket a atte |
| `CYC-06` | Should | Socle | `CERTIFIED` | 001 | Un ticket clôturé peut être rouvert par un utilisateur CIT habilité ; la réouverture est motivé |
| `CYC-07` | Should | Socle | `CERTIFIED` | 001 | Le client ne peut pas rouvrir directement un ticket clôturé ; il peut demander une réouverture  |
| `MSG-01` | Must | Socle | `CERTIFIED` | 001 | Message client/agent : visible par le client et les agents habilités |
| `MSG-02` | Must | Socle | `CERTIFIED` | 001, 003, 009 | Note interne CIT : visible uniquement par les collaborateurs CIT habilités ; jamais visible du  |
| `MSG-03` | Must | Socle | `CERTIFIED` | 001 | Le caractère interne ou externe d'un message est visuellement très distinct dans l'interface ;  |
| `MSG-04` | Should | Socle | `CERTIFIED` | 001, 009 | Un collaborateur peut être mentionné (@Nom) dans une note interne ; la personne mentionnée reço |
| `MSG-05` | Must | Socle | `CERTIFIED` | 001, 009 | Une mention n'entraîne pas de réaffectation : le ticket reste sous la responsabilité de son opé |
| `WKF-01` | Must | Socle | `CERTIFIED` | 001, 002, 003, 006 | La plateforme applique automatiquement le workflow correspondant au module, au type de demande  |
| `WKF-02` | Must | Socle | `CERTIFIED` | 001 | Circuit non rigide : les utilisateurs habilités peuvent transférer ou réaffecter un ticket lors |
| `WKF-03` | Must | Socle | `CERTIFIED` | 001 | Toute dérogation au circuit nominal est tracée dans l'historique du ticket et le journal d'audi |
| `PEC-01` | Must | Socle | `CERTIFIED` | 001 | Un ticket entrant dans une file est à l'état « en attente de prise en charge » tant qu'aucun op |
| `PEC-02` | Must | Socle | `CERTIFIED` | 001 | Un opérateur habilité peut prendre en charge (accepter) un ticket de sa file ; le ticket lui es |
| `PEC-03` | Must | Socle | `CERTIFIED` | 001 | Enregistrement pour chaque ticket : heure d'arrivée en file, heure de PEC, opérateur ayant acce |
| `PEC-04` | Must | Socle | `CERTIFIED` | 001 | Le délai de PEC (arrivée en file → acceptation) est calculé et exploitable dans les indicateurs |
| `PEC-05` | Should | Socle | `CERTIFIED` | 001 | Affectation directe : un superviseur ou une règle d'affectation paramétrée peut affecter un tic |
| `TRF-01` | Must | Socle | `CERTIFIED` | 001 | Un opérateur habilité peut transférer un ticket vers une autre file de traitement autorisée, sa |
| `TRF-02` | Must | Socle | `CERTIFIED` | 001 | Lors d'un transfert, le ticket conserve son numéro, son dossier, son historique, ses documents, |
| `TRF-03` | Must | Socle | `CERTIFIED` | 001 | Un transfert trace au minimum : opérateur à l'origine, date/heure, file source, file destinatio |
| `TRF-04` | Must | Socle | `CERTIFIED` | 001 | Un opérateur habilité peut réaffecter un ticket à un autre opérateur habilité, **sous réserve d |
| `TRF-05` | Must | Socle | `CERTIFIED` | 001, 009 | Le client est automatiquement notifié d'une réorientation ayant un impact sur son parcours |
| `TRF-06` | Must | Socle | `CERTIFIED` | 001 | Les transitions autorisées entre files sont définies par une matrice paramétrable ; aucun circu |
| `TPS-01` | Must | Socle | `CERTIFIED` | 001 | Pour chaque étape/file traversée : mesure du temps d'attente (arrivée → PEC) et du temps de tra |
| `TPS-02` | Must | Socle | `CERTIFIED` | 001, 008 | Mesure du temps d'attente client (durée en état « En attente client ») |
| `TPS-03` | Must | Socle | `CERTIFIED` | 001 | La plateforme distingue trois durées : temps interne CIT, temps d'attente client, temps total d |
| `TPS-04` | Must | Socle | `CERTIFIED` | 001, 008 | Le temps d'attente client n'est pas imputé à la performance opérateur/file ; les SLA internes p |
| `TPS-05` | Should | Socle | `CERTIFIED` | 001 | L'historique du ticket restitue la chronologie détaillée par étape (attente + traitement pour c |
| `SLA-01` | Must | Socle | `CERTIFIED` | 001, 006 | SLA paramétrables selon, au minimum : module, type de demande, étape, file, priorité, et éventu |
| `SLA-02` | Must | Socle | `CERTIFIED` | 001 | Plusieurs seuils par SLA : seuil d'information (ex. 50%), seuil d'alerte (ex. 80%), dépassement |
| `SLA-03` | Must | Socle | `CERTIFIED` | 001 | Les SLA peuvent être suspendus ou adaptés pendant certains états, notamment « En attente client |
| `SLA-04` | Should | Socle | `CERTIFIED` | 001 | Les horaires de fonctionnement (heures ouvrées du service) peuvent être pris en compte dans le  |
| `SLA-05` | Must | Socle | `CERTIFIED` | 001 | Les valeurs de seuils et durées cibles des SLA sont administrables sans développement (les vale |
| `ALE-01` | Must | Socle | `CERTIFIED` | 001, 009 | Alertes générées pour : ticket non PEC depuis X min, ticket sans activité depuis X min/h, SLA b |
| `ALE-02` | Must | Socle | `CERTIFIED` | 001, 009 | Les règles d'alerte, seuils, destinataires et canaux sont paramétrables sans développement |
| `ALE-03` | Must | Socle | `CERTIFIED` | 001, 009 | Le dépassement d'un SLA peut déclencher une escalade (notification superviseur, remontée priori |
| `SUP-01` | Must | Socle / Supervision | `CERTIFIED` | 001, 009 | Vue temps réel des tickets par état : en attente, non affectés, en cours, attente client, proch |
| `SUP-02` | Must | Socle / Supervision | `CERTIFIED` | 001, 009 | Vue temps réel par file : nombre tickets, backlog, ancienneté ticket le plus ancien, délai moye |
| `SUP-03` | Must | Socle / Supervision | `CERTIFIED` | 001 | Vue de la charge par opérateur et de la disponibilité/connexion des opérateurs du périmètre |
| `SUP-04` | Must | Socle / Supervision | `CERTIFIED` | 001 | Le superviseur peut : affecter un ticket non PEC, réaffecter, transférer, équilibrer la charge, |
| `SUP-05` | Must | Socle / Supervision | `CERTIFIED` | 001 | Suivi activité opérationnelle des agents du périmètre : statut disponibilité, dernière activité |
| `DIS-01` | Must | Socle | `CERTIFIED` | 001, 003 | La plateforme permet de connaître la disponibilité opérationnelle d'un agent (Disponible / Indi |
| `DIS-02` | Must | Socle | `CERTIFIED` | 001 | Le superviseur peut : modifier la disponibilité d'un opérateur, identifier ses tickets affectés |
| `DIS-03` | Must | Socle | `CERTIFIED` | 001 | Les changements de disponibilité sont tracés |
| `AFF-01` | Must | Socle | `CERTIFIED` | 001 | Prise libre : un opérateur habilité prend un ticket dans sa file |
| `AFF-02` | Must | Socle | `CERTIFIED` | 001 | Affectation manuelle : un superviseur affecte un ticket à un opérateur *(Must autonome — indépe |
| `AFF-03` | Should | Socle | `CERTIFIED` | 001 | Distribution automatique Round-Robin |
| `AFF-04` | Should | Socle | `CERTIFIED` | 001 | Distribution selon la charge actuelle des agents |
| `AFF-05` | Should | Socle | `CERTIFIED` | 001 | Distribution selon la disponibilité des agents |
| `AFF-06` | Should | Socle | `CERTIFIED` | 001 | Distribution selon la compétence / l'habilitation |
| `AFF-07` | Must | Socle | `CERTIFIED` | 001 | Le changement de stratégie d'affectation d'une file est réalisable sans développement |
| `PRI-01` | Must | Socle | `CERTIFIED` | 001 | Chaque ticket dispose d'un niveau de priorité issu d'un référentiel paramétrable ; la priorité  |
| `PRI-02` | Must | Socle | `CERTIFIED` | 001, 011, 012 | La priorité peut influencer l'ordre du ticket dans la file, le SLA applicable, les alertes et l |
| `PRI-03` | Must | Socle | `CERTIFIED` | 001, 012 | Toute modification manuelle de priorité enregistre : ancienne priorité, nouvelle priorité, aute |
| `PRI-04` | Must | Socle | `CERTIFIED` | 001 | Le client ne peut pas déclarer librement sa demande urgente ou critique, sauf si un processus m |
| `MAS-01` | Should | Socle / Supervision | `CERTIFIED` | 001 | Le superviseur peut, en masse : affecter, réaffecter, transférer de file, modifier la priorité, |
| `MAS-02` | Must | Socle / Supervision | `CERTIFIED` | 001 | Toute action de masse est tracée ticket par ticket dans l'historique et le journal d'audit. *Te |
| `CPT-01` | Must | Comptes | `CERTIFIED` | 002 | Création de compte client soumise à validation par CIT avant activation ; les informations et j |
| `CPT-02` | Must | Comptes | `CERTIFIED` | 002, 003 | Le profil du client est déclaré par le client à la création et validé par CIT : normal, en comp |
| `CPT-03` | Must | Comptes | `CERTIFIED` | 002 | Authentification sécurisée (identifiant/mot de passe avec politique de robustesse) ; récupérati |
| `CPT-04` | Must | Comptes | `CERTIFIED` | 001, 002, 003, 009, 011, 012, 013 | Un client n'accède qu'à ses propres tickets, documents et échanges |
| `CPT-05` | Must | Comptes | `CERTIFIED` | 002 | La gestion multi-utilisateurs pour une même société cliente est requise (compte entreprise — vo |
| `CPT-06` | Should | Comptes | `CERTIFIED` | 002, 003 | Modification du type de client par l'administrateur fonctionnel CIT : tracée, sans effet rétroa |
| `CPT-07` | Could | Comptes | `CERTIFIED` | 002 | Authentification renforcée (second facteur) pour les comptes clients. *(Note : exigence indépen |
| `ENT-01` | Must | Comptes | `CERTIFIED` | 002 | Une société cliente peut rattacher plusieurs utilisateurs à son compte |
| `ENT-02` | Must | Comptes | `CERTIFIED` | 002, 003 | Un administrateur de la société gère ses collaborateurs : activation/désactivation, droits de c |
| `ENT-03` | Must | Comptes | `CERTIFIED` | 002, 003, 003 | La visibilité des dossiers est fonction des droits accordés : l'utilisateur voit uniquement ses |
| `ENT-04` | Should | Comptes | `CERTIFIED` | 002, 003 | Journalisation des actions des administrateurs de société (ajout/retrait de collaborateurs, mod |
| `IMP-01` | Must | Billing IMPORT | `CERTIFIED` | 003 | Formulaire dynamique de soumission permettant de saisir les informations nécessaires, d'ajouter |
| `IMP-02` | Must | Billing IMPORT | `CERTIFIED` | 003 | À la validation du formulaire, génération automatique d'un ticket par BL, totalement indépendan |
| `IMP-03` | Must | Billing IMPORT | `CERTIFIED` | 003, 009, 011, 012 | Le client reçoit ses numéros de ticket sur son profil et par e-mail |
| `IMP-04` | Must | Billing IMPORT | `CERTIFIED` | 002, 003, 008 | Les tickets arrivent dans la file du Pilote Manifeste, qui prononce pour chaque ticket : Validé |
| `IMP-05` | Must | Billing IMPORT | `CERTIFIED` | 003, 009 | En cas de complément, le client est notifié, dépose les éléments dans le ticket, et le dossier  |
| `IMP-06` | Must | Billing IMPORT | `CERTIFIED` | 002, 003 | À l'issue du contrôle BL, le ticket validé s'oriente automatiquement selon le type de client |
| `IMP-10` | Must | Billing IMPORT | `CERTIFIED` | 002, 003 | Client normal : après validation, le client demande la facture depuis son ticket ; le ticket pa |
| `IMP-11` | Must | Billing IMPORT | `CERTIFIED` | 003, 009 | Pilote Facturation Terre effectue ses contrôles (hors portail), édite la facture et la joint au |
| `IMP-12` | Must | Billing IMPORT | `CERTIFIED` | 003, 009 | Le client procède au paiement (hors portail) puis joint sa preuve de paiement au ticket |
| `IMP-13` | Must | Billing IMPORT | `CERTIFIED` | 003, 009 | La preuve de paiement fait l'objet d'une validation interne par le rôle habilité (Caisse/Financ |
| `IMP-14` | Must | Billing IMPORT | `CERTIFIED` | 003, 009 | Le Pilote Contrôle Documentaire vérifie le dossier, dépose les documents de sortie (BS, BADT) d |
| `IMP-15` | Should | Billing IMPORT | `CERTIFIED` | 003, 010 | Alternative présentielle : à l'issue du contrôle BL, le client peut choisir de prendre rendez-v |
| `IMP-20` | Must | Billing IMPORT | `CERTIFIED` | 002, 003 | Client en compte : parcours identique au client normal jusqu'à l'édition de la facture |
| `IMP-21` | Must | Billing IMPORT | `CERTIFIED` | 003 | Client en compte : après édition de la facture, le ticket est transmis directement au Contrôle  |
| `IMP-22` | Must | Billing IMPORT | `CERTIFIED` | 003 | Client en compte : récupère son ticket clôturé avec l'ensemble des documents (facture, BS, BADT |
| `IMP-30` | Must | Billing IMPORT | `CERTIFIED` | 002, 003 | Client deposit : parcours identique au client normal jusqu'à l'édition de la facture |
| `IMP-31` | Must | Billing IMPORT | `CERTIFIED` | 003 | Client deposit : après édition de la facture, le ticket est transmis à la file Comptabilité Cli |
| `IMP-32` | Must | Billing IMPORT | `CERTIFIED` | 003 | Client deposit : après lettrage, le ticket passe au Contrôle Documentaire puis est retourné au  |
| `IMP-40` | Must | Billing IMPORT | `CERTIFIED` | 002, 003, 011, 017 | Client IES : dès sa création, le ticket est traité en parallèle par le Pilote Manifeste et le P |
| `IMP-41` | Must | Billing IMPORT | `CERTIFIED` | 003, 017 | Client IES : le ticket n'est retourné au client que lorsque les deux traitements parallèles son |
| `IMP-42` | Must | Billing IMPORT | `CERTIFIED` | 003 | Client IES : le client finalise son parcours sur la plateforme IES (facturation, paiement, docu |
| `UNI-01` | Must | Billing IMPORT | `CERTIFIED` | 003 | À la création d'un ticket IMPORT, la plateforme vérifie s'il existe un ticket actif pour le mêm |
| `UNI-02` | Must | Billing IMPORT | `CERTIFIED` | 003 | Un BL associé uniquement à un ticket clôturé ou annulé peut faire l'objet d'une nouvelle demand |
| `UNI-03` | Must | Billing IMPORT | `CERTIFIED` | 003 | Le contrôle d'unicité porte sur chaque BL individuellement et n'empêche pas l'utilisation du mê |
| `UNI-04` | Should | Billing EXPORT | `CERTIFIED` | 003, 004 | Pour l'EXPORT, une règle équivalente sur le numéro de booking pourra être mise en œuvre après v |
| `EXP-01` | Must | Billing EXPORT | `CERTIFIED` | 003, 004 | Soumission client d'un ou plusieurs bookings avec documents (confirmation de booking, déclarati |
| `EXP-02` | Must | Billing EXPORT | `CERTIFIED` | 004 | Les tickets arrivent dans la file du Back Office, qui codifie le booking dans IPAKI (hors porta |
| `EXP-03` | Must | Billing EXPORT | `CERTIFIED` | 004 | Après codification, orientation selon les mêmes variantes par type de client que l'import ; le  |
| `EXP-04` | Must | Billing EXPORT | `CERTIFIED` | 003, 004 | Client IES export : la facture et le BEE sont générés côté plateforme IES après traitement para |
| `SIM-01` | Must | Proforma | `CERTIFIED` | 004, 005 | Le client peut lancer une simulation de proforma **avant, si nécessaire**, de démarrer son parc |
| `SIM-02` | Must | Proforma | `CERTIFIED` | 005 | Le moteur calcule, par conteneur : acconage, relevage, branchement frigorifique (Reefer), major |
| `SIM-03` | Must | Proforma | `CERTIFIED` | 005 | Le tarif applicable est déterminé par la clé Sens + Taille + Catégorie de marchandise, à partir |
| `SIM-04` | Must | Proforma | `CERTIFIED` | 005 | La durée de séjour est déduite des dates d'arrivée et d'enlèvement prévisionnel ; elle alimente |
| `SIM-05` | Must | Proforma | `CERTIFIED` | 005 | Le résultat présente le détail des postes par conteneur, un total général et la mention légale  |
| `SIM-06` | Must | Proforma | `CERTIFIED` | 005 | Tous les paramètres de calcul sont administrables sans développement : grilles, taux de majorat |
| `SIM-07` | Must | Proforma | `CERTIFIED` | 005 | Les grilles tarifaires sont versionnées avec une date d'effet ; une simulation référence la ver |
| `SIM-08` | Should | Proforma | `CERTIFIED` | 005 | Le client peut enregistrer une simulation, la retrouver sur son profil et la rattacher ultérieu |
| `SIM-09` | Should | Proforma | `CERTIFIED` | 005 | Le client peut exporter/imprimer le résultat de simulation en PDF |
| `SIM-10` | Should | Proforma | `CERTIFIED` | 005 | L'administrateur peut charger une nouvelle grille tarifaire par import (ex. tableur) en plus de |
| `SIM-11` | Could | Proforma | `CERTIFIED` | 005, 013 | Contrôles de cohérence à la saisie (taille et type reconnus, catégorie compatible avec le sens) |
| `AVE-01` | Must | Avoir/Exo | `CERTIFIED` | 004, 005, 007, 009 | Le client peut soumettre une demande d'avoir ou d'exonération avec les justificatifs exigés ; c |
| `AVE-02` | Must | Avoir/Exo | `CERTIFIED` | 007 | Chaque type de demande suit son propre workflow de validation, avec étapes et intervenants para |
| `AVE-03` | Must | Avoir/Exo | `CERTIFIED` | 007, 009 | La décision (accord, rejet motivé) et les documents associés sont déposés dans le ticket et not |
| `AVE-04` | Should | Avoir/Exo | `CERTIFIED` | 005, 007 | Une demande d'avoir/exonération peut être liée au ticket de parcours client concerné |
| `REC-01` | Must | Customer Complaint | `CERTIFIED` | 004, 005, 006 | Le client crée une demande en sélectionnant une catégorie de réclamation ; chaque demande génèr |
| `REC-02` | Must | Customer Complaint | `CERTIFIED` | 006, 008, 011, 012 | Les catégories sont paramétrables et dynamiques (ajout/modification sans développement) |
| `REC-03` | Must | Customer Complaint | `CERTIFIED` | 006, 008, 011, 012 | Les tickets sont orientés vers le Support Opérationnel, qui peut instruire, demander des complé |
| `REC-04` | Must | Customer Complaint | `CERTIFIED` | 006 | La clôture d'une réclamation comporte une réponse formalisée au client, conservée dans le ticke |
| `REC-05` | Should | Customer Complaint | `CERTIFIED` | 006 | Règles d'orientation automatique par catégorie (file ou service destinataire paramétrable) |
| `REC-06` | Could | Customer Complaint | `CERTIFIED` | 005, 006 | Enquête de satisfaction à la clôture du ticket |
| `ENR-01` | Must | Enrôlement | `CERTIFIED` | 004, 005, 008 | Le chauffeur ou le transporteur renseigne à distance les informations nécessaires et dépose les |
| `ENR-02` | Must | Enrôlement | `CERTIFIED` | 008 | La soumission génère un ticket transmis aux agents Gate, qui peuvent vérifier, demander des com |
| `ENR-03` | Must | Enrôlement | `CERTIFIED` | 008, 009 | Le déplacement physique du chauffeur n'est requis que pour les étapes qui l'exigent (photo, bad |
| `ENR-04` | Should | Enrôlement | `NOT_IMPLEMENTED` | — | Notification au chauffeur (SMS/WhatsApp prioritaires) à chaque étape clé : dossier validé, conv |
| `ENR-05` | Could | Enrôlement | `CERTIFIED` | 005, 008, 009, 013 | Gestion du renouvellement des badges avec rappel automatique avant expiration |
| `COM-01` | Must | Notifications | `PARTIAL` | 002, 003, 004, 007, 008, 009 | La plateforme notifie le client via : e-mail, notifications portail, et au moins un canal de me |
| `COM-02` | Must | Notifications | `CERTIFIED` | 003, 009 | Chaque notification comporte un lien invitant le client à se connecter directement à son ticket |
| `COM-03` | Must | Notifications | `CERTIFIED` | 009 | Les événements déclencheurs et les modèles de messages sont paramétrables par l'administrateur  |
| `COM-04` | Must | Notifications | `CERTIFIED` | 009 | Le chat/commentaires dans le ticket constitue le canal d'échange de référence entre le client e |
| `COM-05` | Should | Notifications | `CERTIFIED` | 009, 013 | Préférences de canaux par client (activation/désactivation par canal, dans le respect des notif |
| `COM-06` | Could | Notifications | `CERTIFIED` | 009, 011, 012 | Relances automatiques paramétrables lorsque le ticket est en attente d'une action client au-del |
| `NOT-01` | Must | Notifications | `CERTIFIED` | 009 | Chaque notification dispose d'un état Lu / Non lu et permet d'accéder directement au ticket con |
| `NOT-02` | Must | Notifications | `CERTIFIED` | 009, 011 | Les règles de diffusion sont paramétrables par événement et par canal |
| `NOT-03` | Should | Notifications | `CERTIFIED` | 009 | Un indicateur du nombre de notifications non lues est visible en permanence dans le portail |
| `CON-01` | Must | Transverse | `CERTIFIED` | 003, 009, 010 | Un client peut commencer son parcours en ligne et le poursuivre physiquement (ou inversement) s |
| `CON-02` | Must | Transverse | `CERTIFIED` | 003, 009, 010 | Les agents de guichet (FUNCTIONAL_RESPONSIBILITY) disposent d'une recherche rapide de ticket (n |
| `CON-03` | Must | Transverse | `CERTIFIED` | 003, 009, 010 | La plateforme permet au client de prendre un rendez-vous lorsque la présence physique est néces |
| `CON-04` | Should | Transverse | `CERTIFIED` | 003, 009, 010 | La plateforme et le système de gestion de file d'attente existant restent deux systèmes distinc |
| `IA-01` | Must | Assistant IA | `NOT_IMPLEMENTED` | — | L'assistant répond aux questions simples des clients (fonctionnement du portail, pièces à fourn |
| `IA-02` | Must | Assistant IA | `NOT_IMPLEMENTED` | — | L'assistant oriente le client dans ses démarches (choix du parcours, du module, de la catégorie |
| `IA-03` | Must | Assistant IA | `NOT_IMPLEMENTED` | — | L'assistant escalade vers un opérateur humain lorsque cela est nécessaire ; le passage se fait  |
| `IA-04` | Should | Assistant IA | `NOT_IMPLEMENTED` | — | L'assistant s'appuie sur une base de connaissances administrable (FAQ, procédures) ; il cite la |
| `IA-05` | Should | Assistant IA | `NOT_IMPLEMENTED` | — | L'assistant est accessible depuis le portail et, si possible, depuis les canaux de messagerie c |
| `IA-06` | Must | Assistant IA | `NOT_IMPLEMENTED` | — | L'assistant identifie les situations nécessitant une intervention humaine et permet le transfer |
| `IA-07` | Must | Assistant IA | `NOT_IMPLEMENTED` | — | Le client peut demander une intervention humaine explicitement ; l'assistant réduit les sollici |
| `SEC-01` | Must | Sécurité | `CERTIFIED` | 001, 002, 007, 008, 009, 011 | Authentification obligatoire pour tout accès ; politique de mots de passe robuste ; verrouillag |
| `SEC-02` | Must | Sécurité | `CERTIFIED` | 001, 002, 003, 009, 011, 012, 013, 015, 016, 017 | Contrôle d'accès par rôles (RBAC) appliquant le moindre privilège, y compris au niveau des cham |
| `SEC-03` | Must | Sécurité | `CERTIFIED` | 001, 002, 009, 011, 012, 013 | Cloisonnement strict : un client n'accède qu'à ses tickets ; un agent n'accède qu'aux files et  |
| `SEC-04` | Must | Sécurité | `CERTIFIED` | 002, 009, 011, 012, 015 | Chiffrement des échanges (HTTPS/TLS) et protection des documents stockés |
| `SEC-05` | Must | Sécurité | `CERTIFIED` | 001, 002, 009, 011, 017 | Journal d'audit complet et non modifiable des actions utilisateurs et administrateurs. *(Note : |
| `SEC-06` | Must | Sécurité | `CERTIFIED` | 002, 011, 012, 015, 016, 017 | Protection des données personnelles conformément à la réglementation ivoirienne (ARTCI) : minim |
| `SEC-07` | Must | Sécurité | `CERTIFIED` | 001, 009, 011, 012, 013, 015 | Contrôle des fichiers déposés : types autorisés, taille maximale, analyse antivirale |
| `SEC-08` | Should | Sécurité | `CERTIFIED` | 001, 002, 011, 012 | Second facteur d'authentification pour les profils agents et administrateurs. *(Note : exigence |
| `SEC-09` | Should | Sécurité | `CERTIFIED` | 011, 012, 015 | Cloisonnement des environnements (production, recette) et gestion des habilitations par environ |
| `SEC-10` | Must | Sécurité | `CERTIFIED` | 009, 011 | Les sessions utilisateurs expirent après une période d'inactivité paramétrable |
| `SEC-11` | Must | Sécurité | `CERTIFIED` | 001, 002, 009, 011 | Les comptes agents peuvent être désactivés immédiatement, sans suppression de leur historique |
| `SEC-12` | Must | Sécurité | `CERTIFIED` | 009, 011, 015 | Aucun lien transmis par notification ne donne accès direct à un document protégé sans contrôle  |
| `NFR-01` | Must | Transverse | `CERTIFIED` | 003, 011, 016 | Interface en français ; ergonomie simple adaptée à des utilisateurs non techniciens |
| `NFR-02` | Must | Transverse | `CERTIFIED` | 003, 008, 011, 016 | Portail client responsive, utilisable sur mobile (une part importante des clients et chauffeurs |
| `NFR-03` | Must | Transverse | `IMPLEMENTED_NOT_ENVIRONMENT_PROVEN` | F016 | Disponibilité cible en heures ouvrées ≥ 99 % ; maintenance planifiée hors heures de pointe |
| `NFR-04` | Must | Transverse | `IMPLEMENTED_NOT_ENVIRONMENT_PROVEN` | F016 | Temps de réponse < 3 secondes pour les écrans courants dans des conditions normales de charge |
| `NFR-05` | Must | Transverse | `NOT_IMPLEMENTED` | — | Volumétrie dimensionnante à confirmer en phase de cadrage : tickets/jour, taille des pièces joi |
| `NFR-06` | Must | Transverse | `NOT_IMPLEMENTED` | — | Sauvegardes régulières (données et documents) et procédure de restauration testée |
| `NFR-07` | Should | Transverse | `PARTIAL` | 011, 013, 014 | Interface disponible en anglais (clients internationaux) |
| `NFR-08` | Should | Transverse | `CERTIFIED` | 011, 013, 014, 015, 016, 017 | Architecture ouverte exposant des API pour faciliter d'éventuelles intégrations futures |
| `NFR-09` | Should | Transverse | `NOT_IMPLEMENTED` | — | Hébergement conforme aux standards du groupe ; le choix (infrastructure CIT/AGL ou cloud) sera  |
| `NFR-10` | Must | Transverse | `IMPLEMENTED_NOT_ENVIRONMENT_PROVEN` | F016 | La plateforme est accessible 24h/24 et 7j/7, hors périodes de maintenance planifiées |
| `NFR-11` | Must | Transverse | `CERTIFIED` | 011, 016 | Les services CIT conservent leurs horaires de fonctionnement propres pour le calcul des SLA ; u |
| `ERR-01` | Must | Transverse | `CERTIFIED` | 012, 013, 014, 015, 017 | Aucune perte de données saisies en cas d'incident lors d'une soumission (brouillon ou reprise d |
| `ERR-02` | Must | Transverse | `CERTIFIED` | 009, 011 | Échec d'envoi d'une notification : nouvelle tentative automatique et traçage ; l'échec d'un can |
| `ERR-03` | Must | Transverse | `CERTIFIED` | 001, 011 | Les documents déposés sont vérifiés à l'upload (intégrité, type, taille) avec message d'erreur  |
| `ERR-04` | Should | Transverse | `CERTIFIED` | 009, 010 | Mode dégradé documenté : en cas d'indisponibilité du portail, le parcours physique reste possib |
| `ACR-01` | Must | Espace client | `CERTIFIED` | 001, 003, 004, 006, 007, 009, 010 | Le tableau de bord client met clairement en évidence les actions attendues de sa part (« Mes ac |

---

## 6. Réserve de méthode

Le statut `CERTIFIED` était **repris des rapports de clôture des features** et n'avait pas été
recontrôlé exigence par exigence : le faire supposait de confronter chaque exigence au code
livré, ce que le périmètre de la passe du 2026-09-02 ne couvrait pas.

Ce qui **avait** été vérifié de façon indépendante : le comptage (188 lignes extraites), les
priorités (relues dans le texte contractuel), et le rattachement aux features.

### Ce que l'audit du 2026-09-03 change à cette réserve

L'audit `docs/88-audit-conformite-cdc.md` a effectué, **sur un échantillon**, le contrôle que
cette section différait : confronter l'exigence au code. Environ **trente exigences** ont été
lues au fond, en priorisant les familles porteuses de paramétrabilité (`SLA`, `ALE`, `COM`,
`SOC`, `SEC`, `UNI`, `TPS`, `WKF`). **Huit se sont révélées sur-certifiées.**

**Ce taux interdit de tenir les 166 restantes pour acquises.** Huit défauts sur une trentaine
d'exigences examinées ne se projette pas mécaniquement sur les 158 non examinées — l'échantillon
visait délibérément les zones à risque — mais il établit que le rattachement aux `spec.md` ne
vaut **pas** preuve de conformité. Le chiffre de 166 est un plafond constaté, non un acquis
démontré.

Ce qui soutient malgré tout les 166 : **150 exigences disposent à la fois d'un code applicatif et
d'un test qui les cite**, et les parcours métier centraux sont couverts par des tests de bout en
bout. C'est une présomption sérieuse — ce n'est pas la vérification exigence par exigence, qui
reste à faire.
