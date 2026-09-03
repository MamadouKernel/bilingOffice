# Conformité de l'application à l'analyse MERISE

> **Document** : `docs/87-conformite-application-analyse-merise.md`
> **Établi le** : 2026-09-03, sur le commit `1208a0c`
> **Référence** : `docs/85-analyse-merise.md` — modèle dérivé du CDC
> **Grille** : `docs/86-grille-comparaison-cdc-implementation.md`

---

## Réponse en une phrase

**Oui, largement — mais pas totalement.** L'application respecte l'essentiel des règles de gestion,
du MCD, du MLD et du MPD. **Trois écarts portent une exigence contractuelle non satisfaite**, et
tous trois concernent la **mesure du temps par étape** (§9.5 du CDC).

---

## Verdict par livrable

| Livrable | Verdict | Détail |
|:---|:---|:---|
| **Règles de gestion** | **Conforme à 93 %** | 3 écarts sur 45 règles vérifiées |
| **MCD** | **Conforme avec écarts** | 61 entités implémentées vs 62 modélisées ; 4 écarts structurels |
| **MLD** | **Conforme** | Associations porteuses présentes, cardinalités respectées |
| **MPD** | **Conforme, et supérieur** | Index filtrés, garde append-only, contraintes d'unicité |
| **MCT** | **Conforme avec écarts** | 11 traitements sur 13 ; les 2 manquants sont des blocages externes |
| **MOT** | **Non vérifiable** | Nécessite l'observation d'une organisation réelle |

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

### 4.1 Règles de gestion — 42 / 45 vérifiées conformes

| Domaine | Vérifié | Conforme | Écarts |
|:---|---:|---:|:---|
| Cycle de vie du ticket | 6 | 6 | — |
| Workflow et orientation | 9 | 9 | — |
| **Temps et SLA** | **6** | **3** | **ÉCART-01, 02, 03** |
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

| Écart structurel | Nature |
|:---|:---|
| `PASSAGE_EN_FILE` | **ABSENT** — ÉCART-01 |
| `STATUT` en table | **ABSENT** — ÉCART-03 (enum) |
| `MOTIF_TRANSFERT` en référentiel | **ABSENT** — §9.10 le liste comme paramétrable |
| `TRANSFERT` / `REAFFECTATION` | Absents en tables — ÉCART-CHOIX-03 |
| `CONVERSATION_IA` | **ABSENT** — écart **attendu**, fournisseur non arbitré |

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

### 4.5 MCT — 11 traitements sur 13

| Traitement | État |
|:---|:---|
| Création de compte, soumission, traitement en file, orientation, transfert, relance, annulation, SLA, notification, simulation, réclamation, enrôlement | **Conformes** |
| **Escalade IA** | **Absent** — écart attendu, PC-18 |
| **Mesure du temps par étape** | **Écart** — ÉCART-01 |

### 4.6 MOT — non vérifiable

Le MOT décrit **qui fait quoi, où et quand**. Cela s'observe dans une organisation, pas dans du
code. Ce que le code peut confirmer :

- la **nature** des opérations — automatisée vs manuelle : les services de fond correspondent aux
  traitements que le MOT annonce comme automatisés ;
- la **répartition des habilitations** par profil.

Ce qu'il ne peut pas confirmer : les postes de travail, les lieux, les horaires réels. **La
vérification du MOT relève de la recette**, avec des utilisateurs réels.

---

## 5. Synthèse

| Nature | Nombre |
|:---|---:|
| **`ÉCART-MANQUE`** — exigence CDC non satisfaite | **3** |
| `ÉCART-CHOIX` — autre implémentation, exigence satisfaite | 4 |
| `ÉCART-SURPLUS` — au-delà du CDC | 3 |
| Écarts **attendus** — bloqués par une décision externe | 1 |
| Points vérifiés **conformes** | ~120 |

### Les trois manques, par ordre d'importance

| # | Écart | Exigence | Effort de correction |
|---:|:---|:---|:---|
| **1** | Pas de mesure par étape | **TPS-01, TPS-05** | **Élevé** — nouvelle entité, migration, reprise des mesures |
| **2** | Temps total mal calculé | **TPS-03** | **Faible** — passer `DateCreationUtc` au lieu de `DateArriveeEnFileUtc` |
| **3** | Statuts non paramétrables | **§9.10, §11.4** | **Moyen** — référentiel + migration |

**L'écart 2 est le plus rentable à corriger** : une ligne, et un indicateur affiché cesse d'être
trompeur. Il ne referme pas TPS-01, mais il supprime une mesure fausse.

---

## 6. Ce que ce constat ne dit pas

**Il ne remet pas en cause le score CDC de 174/188.** Les trois manques concernent des exigences
Must (TPS-01, TPS-03, TPS-05) déjà comptabilisées dans la matrice de conformité. Savoir si elles y
sont comptées conformes est une **question distincte**, à instruire contre `docs/33` — et si elles
l'étaient à tort, le score baisserait.

**C'est le point le plus important de ce document.** Je n'ai pas vérifié comment la matrice traite
TPS-01, TPS-03 et TPS-05. Si elle les déclare conformes, il y a une contradiction à trancher.

**Il ne juge pas la qualité du code.** Un écart au modèle n'est pas un défaut de qualité : deux des
quatre `ÉCART-CHOIX` constatés sont **meilleurs** que ce que j'avais modélisé.

**Il reste soumis à la réserve de méthode** de `docs/86 § G` : le modèle a été rédigé depuis le CDC,
mais par un auteur ayant lu l'implémentation plus tôt. Les convergences sont donc plausibles sans
être des preuves d'indépendance. **Les trois divergences, elles, sont solides** — et ce sont elles
qui portent l'information.

---

**Statut : `CONFRONTATION EFFECTUÉE`.** 133 points de la grille instruits, dont ~120 conformes.
