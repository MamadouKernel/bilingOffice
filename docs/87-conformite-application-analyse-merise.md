# Conformité de l'application à l'analyse MERISE

> **Document** : `docs/87-conformite-application-analyse-merise.md`
> **Établi le** : 2026-09-03, sur le commit `1208a0c`
> **Mis à jour le** : 2026-09-03, après remédiation
> **Référence** : `docs/85-analyse-merise.md` — modèle dérivé du CDC
> **Grille** : `docs/86-grille-comparaison-cdc-implementation.md`

---

## État après remédiation

Les quatre écarts qui portaient une exigence contractuelle ont été refermés. Le corps du document
conserve le constat d'origine — c'est lui qui explique **pourquoi** chaque écart comptait ; chaque
section porte désormais son état courant.

| Écart | Exigence | État |
|:---|:---|:---|
| Aucune mesure du temps par étape | TPS-01, TPS-05 | **Refermé** — entité `PassageEnFile`, synchronisée au point d'enregistrement unique |
| Temps total du parcours mal borné | TPS-03 | **Refermé** — `MesureTemps` distingue l'étape du parcours |
| Statuts non paramétrables | §9.10, §11.4 | **Refermé** — référentiel `LibelleStatutTicket`, approvisionnement additif |
| Motifs de transfert non paramétrables | §9.10, §17.3 | **Refermé** — référentiel `MotifTransfert` + écran d'administration |

**Ce qui reste ouvert n'est pas corrigible par le code** : l'escalade IA et `CONVERSATION_IA`
attendent l'arbitrage d'un fournisseur (PC-18), et la conformité du MOT s'observe en recette.

---

## Réponse en une phrase

**Constat d'origine — désormais remédié.** L'application respectait déjà l'essentiel des règles de
gestion, du MCD, du MLD et du MPD. **Trois écarts portaient une exigence contractuelle non
satisfaite**, tous trois sur la **mesure du temps par étape** (§9.5), auxquels s'ajoutait un
quatrième référentiel non paramétrable (§9.10). Les quatre sont refermés.

---

## Verdict par livrable

| Livrable | Verdict à l'audit | Verdict après remédiation |
|:---|:---|:---|
| **Règles de gestion** | Conforme à 93 % — 3 écarts sur 45 | **45 / 45 conformes** |
| **MCD** | Conforme avec écarts — 4 écarts structurels | **Conforme** ; ne restent que les 2 écarts assumés (`TRANSFERT`/`RÉAFFECTATION` en historique) et `CONVERSATION_IA`, bloqué |
| **MLD** | Conforme | **Conforme** — associations porteuses présentes, cardinalités respectées |
| **MPD** | Conforme, et supérieur | **Conforme, et supérieur** — index filtrés, garde append-only, contraintes d'unicité |
| **MCT** | Conforme avec écarts — 11 sur 13 | **12 sur 13** ; l'escalade IA reste bloquée par PC-18 |
| **MOT** | Non vérifiable | **Moitié vérifiable — et vérifiée conforme** : les 10 traitements annoncés automatisés ont tous une implémentation. La moitié organisationnelle (postes, lieux, horaires) relève de la recette |

---

## 1. Les trois écarts qui portent une exigence

### ÉCART-01 — Aucune mesure du temps par étape

| | |
|:---|:---|
| **Attendu** | `PASSAGE_EN_FILE` : une occurrence **par passage en file**, portant arrivée, prise en charge, début de traitement et sortie |
| **Constaté** | **Aucune table de passage.** Les horodatages sont des **colonnes uniques** du ticket : `DateArriveeEnFileUtc`, `DatePriseEnChargeUtc` |
| **Preuve** | `Ticket.Transferer()` — `src/CIT.Plateforme.Domain/Tickets/Ticket.cs:236` — exécute `DateArriveeEnFileUtc = maintenantUtc` |
| **Nature** | **`ÉCART-MANQUE`** |

**Ce que cela produit.** Au premier transfert, les horodatages du passage précédent sont
**écrasés**. Un ticket passé par Contrôle BL, Facturation puis Contrôle Documentaire ne conserve que
les temps de la **dernière** file.

**Exigences non satisfaites** :

| Réf. | Exigence | État |
|:---|:---|:---|
| **TPS-01** | « Pour **chaque étape/file traversée**, la plateforme mesure le temps d'attente et le temps de traitement » | **Non satisfaite** — seule l'étape courante est mesurable |
| **TPS-05** | « L'historique restitue la **chronologie détaillée par étape** » | **Non satisfaite** |
| **SLA-01** | SLA paramétrable **par étape** | Paramétrable, mais non **mesurable** rétrospectivement par étape |

**Atténuation partielle** : `HistoriqueTickets` conserve les événements avec horodatage. La
chronologie est donc **reconstructible par analyse de texte**, mais elle n'est ni structurée ni
exploitable pour les indicateurs de §9.9 — « délai moyen par étape », « ancienneté du ticket le plus
ancien par file ».

---

### ÉCART-02 — Le « temps total du parcours » ne part pas de la création

| | |
|:---|:---|
| **Attendu** | TPS-03 : « le **temps total du parcours** (création → clôture) » |
| **Constaté** | `MesureTemps.Calculer` reçoit `dateArriveeEnFileUtc` et calcule `tempsTotal = instantMesure − dateArriveeEnFile` |
| **Preuve** | `IndicateursTemps.razor:88` passe `ticket.DateArriveeEnFileUtc`, réinitialisé à chaque transfert |
| **Nature** | **`ÉCART-MANQUE`** |

**Ce que cela produit.** Le « temps total » affiché est le temps écoulé depuis l'arrivée dans la
**file courante**, pas depuis la création du ticket. Sur un ticket transféré, il est **sous-évalué**.

C'est une conséquence directe d'ÉCART-01, mais elle mérite d'être citée séparément : l'indicateur
existe, il est affiché, et il **ne mesure pas ce que son nom annonce**. C'est plus trompeur qu'une
absence.

**Le champ nécessaire existe pourtant** : `Ticket.DateCreationUtc`. La correction consiste à le
passer au lieu de `DateArriveeEnFileUtc`.

---

### ÉCART-03 — Les statuts ne sont pas paramétrables

| | |
|:---|:---|
| **Attendu** | §11.4 : « libellés paramétrables ». §9.10 : « l'administration permet de gérer sans développement […] les statuts » |
| **Constaté** | `StatutTicket` est une **énumération C#** — `src/CIT.Plateforme.Domain/Tickets/StatutTicket.cs:6` |
| **Nature** | **`ÉCART-MANQUE`** |

**Ce que cela produit.** Ajouter un statut — le CDC en évoque un « spécifique de lettrage possible
pour les clients deposit » — exige une modification du code, une recompilation et un déploiement.

**Nuance importante** : l'énumération apporte une **sûreté de typage** que le CDC ne demande pas
mais qui a de la valeur — un statut inexistant est une erreur de compilation, non une erreur
d'exécution. C'est un arbitrage défendable, mais il **contredit une exigence explicite**.

---

## 2. Les écarts qui n'en sont pas

### ÉCART-CHOIX-01 — Le type de client figé est porté par le Dossier

| | |
|:---|:---|
| **Modèle** | `type_client_applique` sur **TICKET** |
| **Implémentation** | `TypeClient` sur **DOSSIER** — `Dossier.cs:34` — et `Ticket.DossierId` **non nullable** |
| **Nature** | **`ÉCART-CHOIX`** |

**L'implémentation est meilleure que le modèle.** RG-07 (CPT-06) exige qu'un changement de type
n'ait pas d'effet rétroactif. Les deux approches y parviennent, mais :

- mon modèle dénormalisait sur **N tickets** ;
- l'implémentation dénormalise sur **1 dossier**.

Puisque tous les tickets d'un dossier partagent le type, et que le rattachement à un dossier est
obligatoire, la propriété est garantie avec **moins de redondance**. C'est le modèle qui est à
corriger, pas le code.

### ÉCART-CHOIX-02 — Dossier obligatoire pour tout ticket

Mon modèle prévoyait `0,1`. L'implémentation impose `1,1`. C'est ce qui rend ÉCART-CHOIX-01
possible, et cela ne contredit pas SOC-11 : un dossier d'un seul ticket reste un dossier.

### ÉCART-CHOIX-03 — Transferts tracés en historique, non en tables dédiées

| | |
|:---|:---|
| **Modèle** | Tables `TRANSFERT` et `REAFFECTATION` distinctes |
| **Implémentation** | Événements dans `HistoriqueTickets` et `JournalAudit` |
| **Nature** | **`ÉCART-CHOIX` avec réserve** |

TRF-03 et WKF-03 exigent la **traçabilité** — elle est assurée. Mais le détail (file d'origine, file
de destination, motif) est stocké en **texte libre** dans le champ `Detail`.

**Réserve** : les indicateurs de §9.9 — « taux de transfert/réorientation » — supposent de compter et
d'agréger les transferts. Sur du texte libre, c'est fragile. L'exigence de traçabilité est
satisfaite ; celle d'indicateur ne l'est pas confortablement.

### ÉCART-CHOIX-04 — Compteur de relances, non historique des relances

`Ticket.NombreRelancesEnvoyees` est un compteur. Mon modèle prévoyait une table `RELANCE` avec la
date de chaque envoi. CYC-04 n'exige qu'un nombre et une cadence : **le compteur suffit**.

---

## 3. Ce que l'application fait au-delà du CDC

### SURPLUS-01 — Unicité du booking export implémentée

UNI-04 classe cette règle **Should**, « après validation métier ». Elle est implémentée :
`HasFilter("[NumeroBooking] IS NOT NULL AND [Statut] IN (0, 1, 2)")`.

**À vérifier** : la validation métier a-t-elle eu lieu ? Sinon, une règle non validée est appliquée
en production.

### SURPLUS-02 — Rétention et gel légal

`LotsPurgeRetention`, `EstSousGelLegal`, `EstAnonymise` : le CDC (SEC-06) demande le respect de
l'ARTCI sans détailler de mécanisme. L'implémentation va plus loin — c'est un surplus **utile**.

### SURPLUS-03 — Évaluation de satisfaction

`EvaluationNote`, `EvaluationCommentaire` sur le ticket. REC-06 est **Could**. Implémenté.

---

## 4. Conformité détaillée par livrable

### 4.1 Règles de gestion — 42 / 45 à l'audit, **45 / 45 après remédiation**

| Domaine | Vérifié | Conforme | Écarts |
|:---|---:|---:|:---|
| Cycle de vie du ticket | 6 | 6 | — |
| Workflow et orientation | 9 | 9 | — |
| **Temps et SLA** | **6** | **6** | ÉCART-01, 02, 03 — **refermés** |
| Affectation et supervision | 6 | 6 | — |
| Notifications | 6 | 6 | — |
| Simulation | 10 | 10 | — |
| Cloisonnement et sécurité | 7 | 7 | — |

**Points notables vérifiés conformes** :

| Règle | Constat |
|:---|:---|
| **RG-69** — aucune clôture automatique | `EstEligibleASuggestionDeCloture` — le système **suggère** |
| **RG-70** — pas de suggestion sur état engageant | `EstEnEtatEngageant` présent et testé |
| **RG-81** — jointure IES | `SuivisBranchesIes` avec les deux branches |
| **RG-74** — unicité du BL actif | **Index unique filtré** sur les statuts actifs |
| **RG-109** — audit non modifiable | `JournalAuditAppendOnlyGuard` |
| **RG-111** — échec de canal isolé | `EnvoisNotification` séparée, `ResultatEnvoi` par canal |
| **RG-52** — indisponible non affecté | Vérifié dans la logique d'affectation |
| **RG-38** — attente client soustraite | `DureeAttenteClienteCumulee` |

### 4.2 MCD — 61 entités implémentées vs 62 modélisées

| Écart structurel | Nature à l'audit | État |
|:---|:---|:---|
| `PASSAGE_EN_FILE` | **ABSENT** — ÉCART-01 | **Présent** — `PassageEnFile` |
| `STATUT` en table | **ABSENT** — ÉCART-03 (enum) | **Présent** — `LibelleStatutTicket` |
| `MOTIF_TRANSFERT` en référentiel | **ABSENT** — §9.10 le liste comme paramétrable | **Présent** — `MotifTransfert` |
| `TRANSFERT` / `REAFFECTATION` | Absents en tables — ÉCART-CHOIX-03 | Inchangé — **choix assumé**, tracés en historique |
| `CONVERSATION_IA` | **ABSENT** — écart **attendu**, fournisseur non arbitré | Inchangé — **bloqué** (PC-18) |

**Entités présentes non modélisées par moi** : `InvitationCollaborateur`, `LotPurgeRetention`,
`EvenementSocle`, `BrouillonSaisie`. Les trois premières sont des surplus utiles ; la quatrième
figurait dans mon modèle sous le même rôle.

### 4.3 MLD — conforme

Les associations porteuses attendues sont présentes : `UtilisateursSociete` (rattachement),
`LiaisonsTicket`, `TransitionsAutorisees`, `Habilitations`, `PerimetresSupervision`,
`PreferencesCanalUtilisateur`, `CompetencesOperateur`.

Toutes portent des propriétés propres — aucune n'est une simple table de jonction.

### 4.4 MPD — conforme, et par endroits supérieur

| Attendu | Constaté |
|:---|:---|
| Index unique **filtré** sur BL actif | **Présent**, et étendu au booking |
| Journal d'audit non modifiable | **Présent** — garde applicative |
| Horodatages UTC | **Présent** — suffixe `Utc` systématique |
| Durée SLA non plafonnée à 24 h | **Présent** — stockage en ticks |
| Index filtrés | **4 index filtrés** constatés |

**Réserve sur l'audit** : la garde est **applicative**. Mon modèle proposait aussi un déclencheur
base. Un accès direct à la base contourne la garde. Ce n'est pas un manque au CDC — SEC-05 dit
« non modifiable » sans préciser le niveau — mais c'est une défense en profondeur absente.

### 4.5 MCT — 11 traitements sur 13 à l'audit, **12 sur 13 après remédiation**

| Traitement | État |
|:---|:---|
| Création de compte, soumission, traitement en file, orientation, transfert, relance, annulation, SLA, notification, simulation, réclamation, enrôlement | **Conformes** |
| **Mesure du temps par étape** | **Conforme** — était ÉCART-01, refermé |
| **Escalade IA** | **Absent** — écart attendu, PC-18 |

### 4.6 MOT — la moitié vérifiable est conforme

Le MOT décrit **qui fait quoi, où et quand**. Il a d'abord été porté « non vérifiable », ce qui
était excessif : deux de ses quatre colonnes se lisent dans le code.

**Ce que le code confirme — colonne « nature ».** Chaque traitement que le MOT §8.6 annonce
automatisé a été recherché dans les sources. Les dix ont une implémentation :

| Traitement annoncé automatisé | Règle | Implémentation constatée |
|:---|:---|:---|
| Évaluer les SLA et les seuils | RG-41 | `SurveillanceSlaService`, `SurveillerEcheancesUseCase` |
| Générer les alertes | RG-63 | `TypeAlerte.SlaSeuilInformation` / `SlaSeuilAlerte` |
| Escalader les dépassements | RG-65 | **Notification uniquement** — voir la réserve ci-dessous |
| Relancer l'absence de réponse | RG-69 | `RelancesAbsenceReponseService` |
| **Suggérer** la clôture | RG-69 | `EstEligibleASuggestionDeCloture` |
| **Décider** la clôture — annoncé *manuel* | RG-69 | Aucun automate ne clôture : **conforme par absence** |
| Reprendre les envois en échec | RG-111 | `DistributionNotificationsService` |
| Rappeler l'expiration de badge | ENR-05 | `RappelerExpirationBadgesCommand` |
| Expirer les sessions inactives | RG-113 | `ConfigureApplicationCookie` — `ExpireTimeSpan` (SEC-10) |
| Purger selon les durées de conservation | RG-115 | `GestionnaireRetentionService` |

**La ligne la plus importante est la sixième** : le MOT annonce la décision de clôture comme
manuelle. La vérifier consiste à s'assurer qu'**aucun** automate ne la prend — une conformité qui se
prouve par l'absence, non par la présence. `EstEligibleASuggestionDeCloture` ne fait que suggérer.

**Réserve sur l'escalade (RG-65).** La règle énonce que le dépassement « **peut** déclencher :
notification, remontée de priorité, réaffectation ». Seule la notification est automatisée. Les deux
autres formes supposent de décider quel seuil déclenche quelle action — un arbitrage métier que le
CDC ne donne pas. L'implémenter reviendrait à inventer une politique d'escalade.

**Les cadences restent `À VALIDER`.** Le CDC n'en fixe aucune (§8.6 de l'analyse). Les mécanismes
existent et lisent leur paramétrage ; leur déclenchement périodique attend une décision métier.

**Ce que le code ne peut pas confirmer** : les postes de travail, les lieux et les horaires réels —
que le CDC lui-même laisse `À VALIDER` pour 10 postes sur 15. **Cette moitié du MOT relève de la
recette**, avec des utilisateurs réels.

---

## 5. Synthèse

| Nature | À l'audit | Après remédiation |
|:---|---:|---:|
| **`ÉCART-MANQUE`** — exigence CDC non satisfaite | **3** | **0** |
| `ÉCART-CHOIX` — autre implémentation, exigence satisfaite | 4 | 4 |
| `ÉCART-SURPLUS` — au-delà du CDC | 3 | 3 |
| Écarts **attendus** — bloqués par une décision externe | 1 | 1 |
| Points vérifiés **conformes** | ~120 | ~124 |

### Les manques constatés, et ce qui les a refermés

| # | Écart | Exigence | Correction apportée |
|---:|:---|:---|:---|
| **1** | Pas de mesure par étape | **TPS-01, TPS-05** | `PassageEnFile` — un enregistrement par passage, ses quatre bornes ; synchronisation branchée au point d'enregistrement unique plutôt qu'aux 34 appelants |
| **2** | Temps total mal calculé | **TPS-03** | `MesureTemps` borne le parcours à la création, l'étape à l'arrivée en file |
| **3** | Statuts non paramétrables | **§9.10, §11.4** | `LibelleStatutTicket` — approvisionnement strictement additif |
| **4** | Motifs de transfert non paramétrables | **§9.10, §17.3** | `MotifTransfert` + écran `/administration/motifs-transfert` |

**Sur les écarts 3 et 4, la propriété qui compte n'est pas la table mais son comportement** : un
approvisionnement qui réécrirait les valeurs par défaut à chaque démarrage annulerait le paramétrage
de l'administrateur, et la fonctionnalité paraîtrait présente sans l'être. Un test verrouille ce
point dans les deux cas.

**Aucune valeur métier n'a été inventée.** Le CDC ne nomme aucun motif de transfert : le référentiel
est livré **vide**, et tant qu'il l'est un motif libre reste recevable — le motif demeurant
obligatoire (TRF-03). Dès qu'un motif y est saisi, le référentiel fait autorité. Même convention que
les pièces exigées à l'ouverture d'un compte.

---

## 6. Ce que ce constat ne dit pas

**Il ne modifie pas le score CDC de 174/188.** Les manques concernaient des exigences Must
(TPS-01, TPS-03, TPS-05) déjà comptabilisées dans la matrice. Elles sont désormais satisfaites, mais
le score reste **inchangé** faute d'avoir instruit comment `docs/33` les comptait.

**C'est le point ouvert le plus important de ce document.** Deux lectures, et une seule est vraie :
si la matrice les déclarait déjà conformes, elle était fausse à la date de l'audit et le score
n'avait pas lieu d'être relevé ; si elle les déclarait non conformes, le score doit monter. Trancher
demande de lire `docs/33`, pas de raisonner — et personne ne devrait relever un score sans cette
lecture.

**Il ne juge pas la qualité du code.** Un écart au modèle n'est pas un défaut de qualité : deux des
quatre `ÉCART-CHOIX` constatés sont **meilleurs** que ce que j'avais modélisé.

**Il reste soumis à la réserve de méthode** de `docs/86 § G` : le modèle a été rédigé depuis le CDC,
mais par un auteur ayant lu l'implémentation plus tôt. Les convergences sont donc plausibles sans
être des preuves d'indépendance. **Les trois divergences, elles, sont solides** — et ce sont elles
qui portent l'information.

---

**Statut : `CONFRONTATION EFFECTUÉE, REMÉDIATION APPLIQUÉE`.** 133 points de la grille instruits.
Les quatre écarts qui portaient une exigence sont refermés et couverts par des tests. Ce qui reste
ouvert relève d'un arbitrage externe (PC-18) ou de la recette (MOT).
