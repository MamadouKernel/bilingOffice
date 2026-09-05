# Audit de conformité — application vs CDC_Plateforme_Digitale_CIT_v3

> **Document** : `docs/88-audit-conformite-cdc.md`
> **Établi le** : 2026-09-03
> **Source de vérité** : `docs/CDC_Plateforme_Digitale_CIT_v3.docx` — lu directement
> **Objet de l'audit** : l'application **et** la matrice de conformité `docs/33`
> **Auditeur** : revue indépendante, postérieure à la remédiation MERISE

---

> **Mise à jour du 2026-09-04.** Sept des huit exigences relevées par cet audit ont été
> **corrigées, testées et rendues administrables**. Le score est passé de 166 à **173 / 188
> (92,02 %)**. Seule `COM-01` reste ouverte, faute de fournisseur de messagerie arbitré — ce
> n'est pas une décision d'ingénierie. Le corps du document conserve le constat d'origine :
> c'est lui qui explique pourquoi chaque écart comptait.

## Verdict en une phrase

**L'application n'est pas conforme à 100 %, et l'écart principal n'est pas dans le code : il est
dans la matrice qui le mesure.** Huit exigences déclarées `CERTIFIED` dans `docs/33` ne sont pas
entièrement satisfaites — dont une que le code lui-même déclare hors périmètre.

---

## 1. Méthode

Le `.docx` porte **264 paragraphes et 54 tableaux**. Les exigences vivent dans les tableaux : une
extraction par paragraphes les manque entièrement. J'ai extrait les 336 lignes de tableau, puis
mesuré la traçabilité de chaque code sur **682 fichiers** `.cs` et `.razor`.

**Faille de mon propre instrument, corrigée** : mon extracteur exigeait un préfixe de 3 lettres
minimum et a donc manqué la famille `IA-01..07`. 181 + 7 = **188**, le compte de `docs/33` est
juste. Je le signale parce qu'un audit dont l'instrument n'est pas contrôlé ne vaut rien.

### Traçabilité constatée

| Situation | Nb |
|:---|---:|
| Exigence citée dans le code **et** dans un test | 150 |
| Citée dans le code, dans aucun test | 19 |
| Citée seulement dans un test | 7 |
| Citée nulle part | 5 |

Les 5 non citées sont `NFR-05`, `NFR-06`, `NFR-08`, `NFR-09`, `SEC-09` — volumétrie, sauvegardes,
hébergement, cloisonnement des environnements. Aucune ne se vérifie dans un dépôt de code.

---

## 2. Les huit sur-certifications

Chacune est marquée `CERTIFIED` dans `docs/33` et contredite par le code.

### 2.1 `SLA-01` *(Must)* — **NON CONFORME**

> « Les SLA sont paramétrables selon, au minimum : module, type de demande, **étape, file,
> priorité**, et éventuellement catégorie de client. »

`ParametreSlaAdministrable` porte `Module` et `TypeDemande`. **Deux axes sur cinq.** La fabrique
`Definir(module, typeDemande, dureeCible, seuilInformation, seuilAlerte)` ne laisse aucun doute, et
le commentaire de la classe l'écrit noir sur blanc : « paramètre SLA administrable **par
module/type de demande** ».

### 2.2 `SLA-02` *(Must)* — **PARTIELLEMENT CONFORME**

Quatre niveaux exigés : information, alerte, dépassement, **escalade au-delà**.
`SeuilInformation` et `SeuilAlerte` existent ; le dépassement est la durée cible ; **le seuil
d'escalade n'existe pas**.

### 2.3 `SLA-05` *(Must)* — **PARTIELLEMENT CONFORME**

> « Les valeurs de seuils et les durées cibles sont administrables **sans développement**. »

`GererParametresSlaCommand` existe dans la couche Application, mais **aucun écran ne l'expose**.
Un administrateur fonctionnel ne peut pas modifier un seuil sans développeur.

### 2.4 `ALE-02` *(Must)* — **PARTIELLEMENT CONFORME**

> « Les règles d'alerte, les seuils, **les destinataires** et les canaux sont paramétrables sans
> développement. »

Les destinataires sont résolus par **deux classes C#** (`ResolveurClientDuDossier`,
`ResolveurCollaborateurMentionne`). Ajouter un destinataire exige d'écrire une classe : c'est la
définition inverse de « sans développement ».

### 2.5 `ALE-03` *(Must)* — **NON CONFORME**

> « Le dépassement d'un SLA peut déclencher une escalade (notification d'un superviseur, remontée
> de priorité, réaffectation) selon des règles paramétrables. »

Seule la notification est automatisée. Aucune règle d'escalade n'est modélisée. **Le code le
déclare lui-même**, dans `IResolveurDestinataires` :

> « AUCUN résolveur « superviseur » n'est fourni : **ALE-03 reste hors périmètre de raccordement**
> pour cette feature. »

Une exigence que le code déclare non raccordée est certifiée conforme dans la matrice. C'est le
constat le plus grave de cet audit — non parce que le manque est grand, mais parce qu'il était
**écrit et disponible**.

### 2.6 `SOC-07` *(Must)* — **PARTIELLEMENT CONFORME**

> « types de pièces paramétrables **par étape** ; formats acceptés et taille maximale
> configurables. »

Formats et taille : `ValidateurFichierDocument` (10 Mo par défaut, extensions injectables) —
conforme. **Par étape : absent.** `PiecesExigeesCompte` ne couvre que l'ouverture de compte.

### 2.7 `COM-03` *(Must)* — **PARTIELLEMENT CONFORME**

> « paramétrables par l'administrateur fonctionnel (**par module, par étape**, par canal). »

`RegleDiffusion` porte `TypeEvenement`, `Canal`, `EstObligatoire`, `EstActif`. **Ni module, ni
étape.** L'écran `AdministrationNotifications.razor` existe et administre ce qui est modélisé —
le manque est dans le modèle, pas dans l'interface.

### 2.8 `COM-01` *(Must)* — **PARTIELLEMENT CONFORME**, et **incohérence de la matrice**

> « notifie le client via : e-mail, notifications dans le portail, et **au moins un canal de
> messagerie mobile** (WhatsApp ou SMS). »

`CanalNotification` déclare `InApp, Email, Sms, WhatsApp` — la structure est prête, **aucun
fournisseur n'est implémenté**.

`ENR-04`, qui dépend du **même** fournisseur absent, est classé `NOT_IMPLEMENTED /
BLOCKED_PROVIDER`. `COM-01` est classé `CERTIFIED`. **Deux exigences, une seule cause, deux
verdicts opposés** : la matrice se contredit.

---

## 3. Le fil rouge : l'axe « par étape »

Trois exigences **Must** échouent sur le même point — `SOC-07`, `COM-03`, `SLA-01`.

**La plateforme modélise le workflow mais n'expose pas l'étape comme axe de paramétrage.** Ce n'est
pas trois oublis indépendants : c'est une dimension absente du modèle. Cela explique qu'aucun audit
antérieur ne l'ait vu — chacun regardait les exigences une par une.

Point favorable : l'entité `PassageEnFile`, ajoutée le 2026-09-03, matérialise enfin la notion
d'étape. Le correctif dispose désormais d'un point d'ancrage qui n'existait pas.

---

## 4. Score corrigé

| | `docs/33` avant audit | Après audit (03/09) | Après correction (04/09) |
|:---|---:|---:|---:|
| `CERTIFIED` | **174 / 188** (92,55 %) | **166 / 188** (88,3 %) | **173 / 188** (92,02 %) |
| Non conformes | — | 2 (`SLA-01`, `ALE-03`) | 0 |
| Partiellement conformes | — | 6 | 1 (`COM-01`) |

### Ce qui a fermé les sept

| Réf. | Correction livrée |
|:---|:---|
| `SLA-01` | Axes `FileId` (l'étape) et `PrioriteId` ; résolution du plus spécifique, ordre total garanti par des poids en puissances de deux |
| `SLA-02` | `SeuilEscalade` + `StatutSla.Escalade` ; un seuil antérieur au dépassement est refusé |
| `SLA-05` | Écran `/administration/parametres-sla` |
| `ALE-02` | Destinataires déduits des `PerimetreSupervision` **administrés** — ajouter un superviseur est une donnée, plus un développement |
| `ALE-03` | `RegleEscalade` : notifier / remonter la priorité / remettre en distribution, + écran |
| `SOC-07` | `PieceExigeeEtape` + écran, distinct des pièces d'ouverture de compte |
| `COM-03` | `Module` et `FileId` sur `RegleDiffusion`, filtrage appliqué à la distribution |

**Les sept sont livrées vides.** Référentiels sans contenu, axes optionnels nuls : le comportement
d'une plateforme non paramétrée est strictement inchangé, et aucune valeur métier n'a été
inventée — le CDC renvoie lui-même ces valeurs au métier. Quatre migrations, toutes additives.
28 tests ajoutés, dont le premier de chaque famille vérifie précisément la rétrocompatibilité.

**Correspondance avec `docs/33`** : la matrice ne dispose que d'un statut pour ces huit cas et les
porte toutes en `PARTIAL`. Le présent audit les sépare en **2 non conformes** (`SLA-01`, `ALE-03`
— l'élément central de l'exigence manque) et **6 partiellement conformes** (le comportement
principal est livré, un élément nommé par le CDC manque). 2 + 6 = 8 : les deux documents décrivent
le même ensemble, à granularité différente.

Ce n'est pas un effondrement : la plateforme satisfait 166 exigences sur 188, et les parcours
métier centraux — import, export, comptes, cycle de vie, transferts, unicité des BL, mesure des
temps, cloisonnement, simulation — sont conformes et testés. **Le problème est que 174 était
annoncé comme un fait vérifié.**

**Réserve sur le 166 lui-même.** Une trentaine d'exigences ont été lues au fond, en visant
délibérément les zones à risque ; huit s'y sont révélées fausses. Ce taux n'autorise pas à tenir
les 158 non examinées pour acquises. **166 est un plafond constaté, pas un acquis démontré.**

---

## 5. Ce qui est conforme, et solidement

Pour ne pas déséquilibrer le jugement :

| Famille | Constat |
|:---|:---|
| `IMP` (21), `EXP` (4) | Parcours complets, variantes en compte / deposit / IES couvertes par des tests E2E |
| `UNI` (4) | Index unique **filtré** sur les statuts actifs ; unicité du booking implémentée **au-delà** de `UNI-04` qui la disait facultative |
| `TPS` (5) | Refermées le 2026-09-03 — `PassageEnFile` mesure attente et traitement par étape |
| `CYC` (7), `WKF` (3), `TRF` (6) | Cycle de vie, dérogations tracées, matrice de transitions consultée et jamais codée en dur |
| `SIM` (11) | Moteur tarifaire contrôlé **au franc près** contre l'annexe A8 du CDC |
| `SEC` (12) | Cloisonnement, RBAC au niveau des champs, 2FA, verrouillage, expiration de session |

---

## 6. Défaut de traçabilité — sans risque fonctionnel

19 exigences sont citées dans le code mais dans aucun test : toute la famille `SIM`, `AFF-03/04/05`,
`ERR-01`, `IMP-12/13/14`. **Vérification faite, ces comportements sont bien testés** —
`CalculerSimulationProformaQueryTests`, `StrategieAffectationParametrableTests`,
`ErrCert01BrouillonsTests`. Seul le code d'exigence manque en en-tête.

Aucun risque fonctionnel, mais la chaîne exigence → preuve est rompue : un auditeur externe
conclura à une absence de couverture. Correction estimée : une heure.

---

## 7. Référentiels sans écran d'administration

Le CDC §17.3 exige que ces référentiels soient administrables sans développement.

| Référentiel | Entité | Écran |
|:---|:---|:---|
| Motifs de transfert | `MotifTransfert` | ✅ `/administration/motifs-transfert` |
| Pièces exigées (compte) | `PieceExigeeCompte` | ✅ `/administration/pieces-exigees-compte` |
| Stratégies d'affectation | `StrategiesAffectation` | ✅ `/administration/strategie-file` |
| Catégories de réclamation | `CategorieReclamation` | ✅ `AdministrationCategories` |
| Modèles / règles de diffusion | `ModeleNotification`, `RegleDiffusion` | ✅ `AdministrationNotifications` |
| Grilles tarifaires | `VersionGrilleTarifaire` | ✅ `GestionGrillesTarifaires` |
| **Paramètres SLA et seuils** | `ParametreSlaAdministrable` | ❌ **aucun** — `SLA-05` |
| **Libellés de statut** | `LibelleStatutTicket` | ❌ **aucun** — §11.4 |
| **Niveaux de priorité** | `NiveauPriorite` | ❌ **aucun** — §17.3 |

---

## 8. Recommandations, par ordre

1. **Corriger `docs/33` avant toute autre chose.** Une matrice fausse est plus dangereuse qu'un
   défaut connu : elle empêche de voir. Les huit lignes doivent passer de `CERTIFIED` à
   `PARTIAL` ou `NOT_IMPLEMENTED`, et le score annoncé à 166/188.
2. **Ouvrir l'axe « étape »** sur `ParametreSlaAdministrable`, `RegleDiffusion` et les pièces
   exigées — désormais possible via `PassageEnFile`.
3. **Modéliser `RegleEscalade`**, livrée vide, et raccorder le résolveur « superviseur » que
   `IResolveurDestinataires` annonce comme manquant.
4. **Trois écrans d'administration** : SLA, libellés de statut, priorités.
5. **Étiqueter les 19 tests** avec leur code d'exigence.

Les points 2 à 4 suivent le patron déjà validé deux fois dans ce dépôt — `MotifTransfert` et
`LibelleStatutTicket` : un référentiel livré **vide**, que le métier remplit. Aucune valeur métier
n'a besoin d'être inventée.

**Hors périmètre du code** et non corrigeables ici : `COM-01` et `ENR-04` (fournisseur de
messagerie non arbitré), `IA-01..07` (fournisseur de modèle non arbitré), `NFR-05/06/09`,
`NFR-03/10`, `SEC-09` (infrastructure et organisation).
