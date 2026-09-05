# Remédiation R-15 — Reprise de données pour le lot 3FN

## Exigence

Permettre l'application de la migration `20260905033506_NormalisationReferentielsTarifaires` sur
une base **contenant des données**.

Le script doit : créer/résoudre les référentiels, mapper les anciennes valeurs, remplir les
nouvelles clés étrangères, vérifier les données, et **seulement ensuite** créer les contraintes
bloquantes — avec transaction, idempotence, rollback, journalisation, contrôles avant/après et test
sur copie de données.

---

## Défaut

La migration procède dans cet ordre :

1. elle **supprime** les colonnes d'origine (`Sens`, `Taille`, `Type`, `CategorieMarchandise`,
   `PartCit/Oic/Paa`, et les treize scalaires `Parametres_*`) ;
2. elle **ajoute** les clés étrangères en `NOT NULL`, avec pour valeur par défaut
   `00000000-0000-0000-0000-000000000000` ;
3. elle **crée** les contraintes de clé étrangère.

Sur une base peuplée, l'étape 3 échoue : aucune ligne de référentiel ne porte l'identifiant nul.

**Le risque est un ÉCHEC DE MIGRATION, pas une perte de données.** La transaction est annulée, rien
n'est corrompu ; le déploiement est simplement impossible. Cette qualification, corrigée lors du
contre-audit, oriente toute la remédiation : il n'y a rien à réparer, il y a un ordre à rétablir.

L'ordre correct est celui-ci — et c'est l'inversion qui constitue la correction :

| Migration d'origine | Script de reprise |
|---|---|
| supprime les colonnes **puis** remplit les clés | remplit les clés **puis** supprime les colonnes |
| pose les contraintes sans vérifier | **vérifie**, et ne pose les contraintes que si la reprise est complète |

---

## Reproduction

Une base LocalDB a été montée au schéma **exact** d'avant la migration
(`20260904053850_AjoutPiecesExigeesEtape`) et peuplée d'un jeu couvrant les quatre sens, les deux
tailles, les deux types et plusieurs catégories — `scripts/reprise-r15/jeu-de-repetition.sql`.

C'est cette répétition qui a fait le travail : **trois erreurs du script ont été trouvées par
l'exécution, aucune par la relecture.**

---

## Cause racine

La migration a été générée par EF sur un modèle déjà normalisé, dans un environnement où la base
était vide. EF ne sait pas produire une reprise de données : il compare deux schémas et écrit la
transformation la plus directe. **La perte d'information — les colonnes supprimées avant d'être
lues — n'apparaît que s'il y a des lignes.** Tous les environnements du projet étant vierges,
aucune exécution ne l'a jamais révélée.

---

## Correction

`scripts/reprise-r15/reprise-r15.sql` — 823 lignes, neuf sections :

| Section | Objet |
|---|---|
| **A** | Contrôles préalables et comptages avant — refuse un schéma inattendu |
| **B** | Tables de référentiel (Sens, Tailles, Types, Catégories) |
| **B2** | Tables de paramètres (majorations, quotes-parts, tranches, seuils, franchises, parts simulées) |
| **C** | Approvisionnement Annexe A.6, **par code**, strictement additif |
| **C2** | Reprise des paramètres depuis les treize colonnes scalaires |
| **D** | Colonnes de clé étrangère en **NULLABLE** |
| **E** | Report des anciennes valeurs vers les clés étrangères |
| **F** | **Vérification** — arrêt si une seule ligne reste non résolue |
| **G** | `NOT NULL`, clés étrangères et index |
| **H** | Suppression des colonnes d'origine |
| **I** | Inscription dans `__EFMigrationsHistory` et comptages après |

### Les trois erreurs révélées par la répétition

**1. Les colonnes n'étaient pas du texte.** La première version comparait `Sens`, `Taille` et
`Type` à des codes texte. Le schéma réel les stocke en **entier** — ce sont des ordinaux
d'énumération sérialisés par EF. Seule `CategorieMarchandise` est une chaîne.

Les correspondances, retrouvées dans l'historique du dépôt et écrites en clair dans le script :

```
SensTrafic       : ImportLocal = 1, ImportTransit = 2, ExportLocal = 3, ExportTransit = 4
TailleConteneur  : VingtPieds = 20, QuarantePieds = 40
TypeConteneur    : Dry = 1, Reefer = 2
```

Elles ne sont pas déduites : un ordinal mal deviné rattacherait une ligne tarifaire au mauvais sens
et produirait un prix faux, sans rien signaler.

**2. Les colonnes nouvelles n'étaient pas résolues à la compilation.** T-SQL compile un lot entier
avant de l'exécuter : les colonnes ajoutées en section D n'existent pas encore pour l'analyseur
quand il lit les sections E, F et G. Toutes les instructions qui les touchent passent désormais en
SQL dynamique, et les tables de correspondance sont **temporaires** (`#`) plutôt que variables
(`@`) — seules les premières sont visibles dans un lot dynamique.

**3. Un index d'unicité dépendait des anciennes colonnes.**
`IX_LignesTarifaires_VersionGrilleTarifaireId_Sens_Taille_CategorieMarchandise` empêchait leur
suppression. Il porte une **règle métier** — un seul tarif par (grille, sens, taille, catégorie) —
et non une simple optimisation : son équivalent est donc créé sur les nouvelles clés en section G,
**avant** que l'ancien soit supprimé en section H. La règle d'unicité n'est à aucun moment absente.

### Ce que le script ne fait jamais

Il **n'invente aucune valeur de repli**. Si une valeur d'origine ne correspond à aucun code, la
section F interrompt tout, rend le journal et la liste des lignes fautives, et annule. Une ligne
tarifaire rattachée à la mauvaise catégorie produirait un prix faux en silence — mieux vaut un
déploiement qui échoue qu'une facturation qui dérive.

---

## Tests

**Quatre scénarios, exécutés sur SQL Server (LocalDB), pas en simulation.**

| # | Scénario | Résultat |
|---|---|---|
| 1 | **Reprise nominale** — base pré-3FN peuplée (6 lignes tarifaires, 1 simulation, 2 lignes de résultat) | ✅ 4 sens, 2 tailles, 2 types, 16 catégories approvisionnés ; 3 majorations, 3 quotes-parts, 3 tranches, 2 seuils, 2 franchises, 6 parts repris ; **0 ligne non résolue** ; comptages **inchangés** (6 / 2 / 1) |
| 2 | **Idempotence** — seconde exécution consécutive | ✅ aucune écriture, aucun doublon, comptages identiques, `__EFMigrationsHistory` non réécrit |
| 3 | **Données incohérentes** — un `Sens` porté à l'ordinal 9, inconnu | ✅ **refus explicite** (`Msg 50015`), journal rendu, liste des lignes fautives ; colonnes d'origine **toujours présentes**, tables de référentiel **non créées**, 6 lignes intactes |
| 4 | **Continuité EF** — `dotnet ef database update` après reprise | ✅ la chaîne de migrations se poursuit jusqu'à la dernière sans erreur |

Le scénario 3 est le plus important : il démontre que le script **échoue proprement**. La propriété
recherchée n'est pas qu'il réussisse toujours, mais qu'il ne laisse jamais la base à moitié
transformée.

**Suite applicative** : 1 370 / 1 370 — aucun code de production n'a été modifié par ce lot.

---

## Mutations

Sans objet au sens habituel : ce lot ne modifie aucun code de production et n'ajoute aucun test
unitaire. Le rôle qu'y jouerait la mutation est tenu par le **scénario 3**, qui introduit
délibérément une donnée non résoluble et vérifie que le script s'arrête au lieu de la contourner.

Les trois erreurs corrigées valent elles-mêmes démonstration : dans les trois cas, le script
échouait, et il échouait **avant** toute modification irréversible.

---

## Fichiers modifiés

| Fichier | Nature |
|---|---|
| `scripts/reprise-r15/reprise-r15.sql` | **créé** — 823 lignes, sections A→I |
| `scripts/reprise-r15/jeu-de-repetition.sql` | **créé** — jeu de données couvrant toutes les branches de correspondance |
| `docs/remediation-R15.md` | ce rapport |

**Aucun code de production, aucune migration EF, aucun test applicatif modifié.**

---

## Protocole d'exécution

Le script **ne doit pas être exécuté directement en production**. Séquence imposée :

1. **Restaurer une copie** de la base de production sur un serveur hors production.
2. **Confronter les valeurs réelles aux correspondances de la section E** :
   ```sql
   SELECT DISTINCT Sens FROM dbo.LignesTarifaires;              -- attendu : 1, 2, 3, 4
   SELECT DISTINCT Taille FROM dbo.LignesTarifaires;            -- attendu : 20, 40
   SELECT DISTINCT Type FROM dbo.LignesResultatsSimulation;     -- attendu : 1, 2
   SELECT DISTINCT CategorieMarchandise FROM dbo.LignesTarifaires;
   ```
   Toute valeur hors de ces ensembles doit être tranchée **avant** l'exécution, jamais pendant.
3. **Exécuter le script sur la copie** et lire le journal de bout en bout : les comptages avant et
   après doivent être identiques, et « Lignes non résolues » doit valoir 0.
4. **Contrôler un échantillon métier** : recalculer deux ou trois simulations historiques et
   vérifier que les montants sont inchangés (RG-SIM-12).
5. **Sauvegarde immédiatement antérieure**, puis exécution réelle en fenêtre de maintenance.
6. **Après exécution** : démarrer l'application et vérifier que `ProvisionnementReferentielsTarifaires`
   ne crée aucun doublon — il est additif par code, donc il ne devrait rien insérer.

---

## Risques résiduels

1. **Le script n'a pas été exécuté sur les données de PRODUCTION.** Il l'a été sur une base au
   schéma identique, peuplée d'un jeu représentatif mais **construit pour le test**. L'étape 2 du
   protocole ci-dessus est ce qui reste à faire, et elle n'est pas optionnelle : une valeur de sens
   ou de catégorie inattendue en production arrêterait le script — proprement, mais elle
   l'arrêterait.
2. **Les catégories de marchandise sont une chaîne libre.** Le script exige une correspondance
   exacte, à la casse et aux espaces près. Une valeur historique mal saisie (« c1 », « C 1 ») sera
   rejetée. C'est délibéré : normaliser silencieusement reviendrait à décider à la place du métier.
3. **Les franchises de stationnement n'existent qu'à l'import.** Les colonnes d'origine n'en
   prévoyaient aucune à l'export ; le script n'en invente pas. Si le métier en attend une, elle
   devra être saisie après reprise.
4. **`Parametres_FranchiseBranchementJours` reste une colonne scalaire** — la migration ne la
   normalise pas. Hors périmètre.
5. **La durée d'exécution n'a pas été mesurée à l'échelle de la production.** Sur un volume
   important, les `ALTER COLUMN ... NOT NULL` et la création des index sont les étapes coûteuses.
   À chronométrer sur la copie pour dimensionner la fenêtre de maintenance.
6. **Aucun plan de retour arrière après validation.** Une fois la transaction validée, les colonnes
   d'origine sont supprimées : le seul retour possible est la restauration de la sauvegarde de
   l'étape 5.
