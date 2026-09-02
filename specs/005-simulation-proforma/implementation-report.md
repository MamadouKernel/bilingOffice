# Rapport d'Implémentation — Feature 005 : Simulation de Proforma

> **Dossier** : `specs/005-simulation-proforma/`
> **Date** : 30 août 2026
> **Exécuté par** : Claude (session `/speckit.implement`)
> **Sources de vérité** : `spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/use-cases.md`, `quickstart.md`, `tasks.md`, `.specify/memory/constitution.md`

---

## 1. Tâches Exécutées

39/40 tâches `tasks.md` sont marquées `[X]`. **T036** (`SIM-11`, contrôles de cohérence dynamiques,
priorité *Could*) est **délibérément différée** : elle ne conditionne aucune exigence `Must`/`Should`,
conformément à l'autorisation explicite du brief d'implémentation (§27 : *« ne doit pas bloquer les
Musts ; si elle génère une complexité disproportionnée, la signaler sans impacter les Musts »*).
Aucune autre tâche n'a été laissée incomplète.

Une part substantielle des tâches T001-T029 (moteur, entités domaine, persistance EF Core,
migration, CSV, sauvegarde/historique, PDF, rattachement Billing) existait déjà à l'ouverture de
cette session — produite lors d'une exécution antérieure interrompue par une réinitialisation de
limite d'usage. Chaque tâche pré-existante a été **revérifiée indépendamment** (relecture du code,
non confiance dans les noms de tests ou les cases cochées) avant d'être validée, et **quatre défauts
réels ont été détectés et corrigés** lors de cette revérification (§17).

## 2. Moteur de Calcul (`MoteurCalculProforma`)

Service de domaine pur (`src/CIT.Plateforme.Domain/Tarification/MoteurCalculProforma.cs`), sans
dépendance EF Core, HTTP, Blazor, horloge interne, configuration ou accès fichier — toutes les
données nécessaires (dossier, conteneurs, grille tarifaire active) lui sont injectées en paramètres.
Aucune valeur tarifaire n'est codée en dur dans le moteur : toutes proviennent de
`VersionGrilleTarifaire.Parametres` et `LigneTarifaire`.

Type monétaire : `decimal` exclusivement. Scan final : **0 occurrence** de `float`/`double` dans
`src/CIT.Plateforme.Domain/Tarification/` ni `src/CIT.Plateforme.Application/Tarification/`.

## 3. Golden Masters (Annexe A.8)

Les 5 cas `GOLDEN-SIM-01` à `GOLDEN-SIM-05` sont vérifiés par égalité stricte (`Assert.Equal`, aucune
tolérance, aucun `Math.Abs`, aucun `InRange`) dans
`tests/CIT.Plateforme.Domain.Tests/Tarification/GoldenMasterProformaTests.cs`, **et rejoués une
seconde fois après un rechargement complet depuis SQL Server réel** (nouvelle instance de
`DbContext`, donc aucun cache de premier niveau) dans
`tests/CIT.Plateforme.Infrastructure.Tests/Tarification/TarificationSqlPersistenceTests.cs` — exigence
explicite du brief (§30) pour détecter une éventuelle dérive de précision SQL, un mauvais mapping EF
Core ou le chargement d'une mauvaise version.

| Cas | Attendu | Moteur (mémoire) | Moteur (post-rechargement SQL) |
| :--- | ---: | :---: | :---: |
| `test01` (Import Transit, 20", Dry, 100t, OOG+Dang) | 199 129 XOF | ✅ | ✅ |
| `test02` (Import Local, 40", Reefer, 200t) | 2 054 980 XOF | ✅ | ✅ |
| `test03` (Export Local, 20", Dry, 300t, OOG) | 245 500 XOF | ✅ | ✅ |
| `test04` (Export Transit, 40", Reefer, 100t, Dang) | 2 053 400 XOF | ✅ | ✅ |
| `test06` (Import Local, 40", Reefer, 300t) | 2 245 300 XOF | ✅ | ✅ |

Ces 5 valeurs ont en outre été **vérifiées manuellement par calcul indépendant** (à la main, contre
les formules exactes de `research.md`/`spec.md`) avant toute écriture de code, conformément à la
discipline TEST-FIRST exigée.

## 4. Majorations (Lourd / Dangereux / OOG)

Chaque majoration est calculée **indépendamment sur la base Acconage**, jamais en cascade sur un
montant déjà majoré : `MajLourd = 30 % × Acconage`, `MajDangereux = 50 % × Acconage`,
`MajOog = 50 % × Acconage`. Seuils Lourd : `> 15t` (20"), `> 26t` (40") — strictement supérieur, non
inclusif. Testé à `-1`, exact, `+1` et en combinaisons (aucune, une seule, deux à deux, les trois
simultanées) dans `MajorationsTarifairesTests.cs`.

## 5. Stationnement

Implémenté via **donnée de grille**, jamais via un `if` codé en dur sur le sens de trafic :
`ParametresTarification` porte un dictionnaire interne `{Sens → Franchise}` construit à partir des
deux colonnes persistées `FranchiseStationnementImportLocalJours`/`...ImportTransitJours` (11j / 21j).
**Export n'y figure simplement pas** — c'est la seule expression de son exclusion initiale ; le moteur
et `ParametresTarification.StationnementApplicable(sens)` ne contiennent aucune mention littérale
d'« Export ». Une révision de grille future pourrait rendre l'Export facturable en ajoutant une
simple entrée de données, sans modification de code. Tranches 1-5j (850 XOF), 6-15j (1 250 XOF),
16j+ (1 850 XOF), répartition CIT 30 % / OIC 20 % / PAA 50 % (somme validée = 100 % à la création des
paramètres).

## 6. Branchement Frigorifique (Reefer)

Franchise 2 jours ; `JoursFacturables = max(0, DuréeSéjour − 2)` ; montant nul pour tout conteneur
`Dry`. Testé à 0/1/2 jours facturables (nul après franchise), franchise+1, longs séjours, et cumul
avec majorations/stationnement dans `StationnementEtReeferTests.cs`.

## 7. Grilles Tarifaires & Versionnage

`VersionGrilleTarifaire` (agrégat racine) porte `NumeroVersion`, `DateEffetUtc`, `DateCreationUtc`,
`AuteurId`, `EstActive`, `Parametres` (objet de valeur) et ses `LigneTarifaire` (clé composite
Sens+Taille+Catégorie, unicité applicative et contrainte SQL `IX_LignesTarifaires_...` unique). La
version active à une date donnée est sélectionnée par `DateEffetUtc` la plus récente `≤` la date de
calcul (`ObtenirActiveADateUtcAsync`), jamais par une valeur codée en dur. Vérifié en base réelle
(`ObtenirActiveADateUtcAsync_selectionne_la_bonne_version`) et en bout en bout via REC-SCE-06 (§16).

## 8. Historique & Immutabilité

`SimulationProforma` est un instantané figé : `VersionGrilleTarifaireId`, `VersionGrilleLibelle` et le
détail complet par conteneur (`LigneResultatSimulationConteneur`) sont persistés au moment de la
sauvegarde et **ne sont jamais recalculés**. `ConsulterSimulationProformaQuery` (C6-03) compare la
version stockée à la version active courante et expose `EstGrilleObsolete` — un simple indicateur
d'avertissement, sans recalcul silencieux (`ConsulterSimulationProformaQueryTests.cs`, 2 cas : grille
active / grille révisée).

## 9. Import CSV Atomique

`ImportateurGrilleTarifaireCsv` : UTF-8, séparateur `;`, en-tête obligatoire
`Sens;Taille;Categorie;Acconage;Relevage;BranchementJour`. Pipeline strict : analyse et validation
intégrales du fichier **avant** toute persistance ; la moindre erreur (en-tête invalide, montant
négatif, ligne incorrecte au milieu du fichier, doublon de clé composite) **bloque la totalité de
l'import** — `ImporterGrilleTarifaireCsvCommandHandler` ne persiste jamais de version partielle
(`ImportGrilleTarifaireCsvTests.cs`, 4 cas négatifs + 1 cas valide).

## 10. Rapport d'Erreurs CSV

`ResultatImportCsvVue.Erreurs` restitue des messages actionnables (numéro de ligne, nature de
l'erreur) — jamais de stacktrace ni de détail d'exception technique exposé à l'administrateur.

## 11. Export PDF (C6-05)

Abstraction `IGenererDocumentProformaPdf` définie côté Application ; implémentation
`GenerateurDocumentProformaPdf` côté Infrastructure — le Domain ne dépend d'aucune bibliothèque PDF.
`GenererDocumentProformaPdfQueryHandler` vérifie le cloisonnement société avant génération (test IDOR
dédié). Contenu vérifié fonctionnellement (référence, décomposition, total, version, mention légale)
dans `GenererDocumentProformaPdfTests.cs`.

## 12. Rattachement Billing Import/Export (Additif)

`Dossier.ReferenceSimulationProforma` (`string?`, additif, sans contrainte FK — autonomie du module
conservée) ; `SoumettreDemandeImportCommand`/`SoumettreDemandeExportCommand` acceptent le champ
optionnel et le transmettent tel quel. Aucune conversion automatique Simulation→Ticket (vérifié par
scan final, §19). `RattachementSimulationBillingTests.cs` couvre référence valide, absente,
whitespace, et les deux chemins Import/Export.

## 13. Interface Utilisateur Client

4 pages Blazor créées sous `src/CIT.Plateforme.Web/Components/Pages/Tarification/` :
`SimulationProformaPage.razor` (`/simulation/proforma` — saisie multi-conteneurs, calcul interactif,
décomposition par poste, mention légale, sauvegarde), `HistoriqueSimulationsPage.razor`
(`/simulation/historique`), `DetailSimulationPage.razor` (`/simulation/{id}` — détail figé, badge de
grille révisée, téléchargement PDF via un petit script `wwwroot/js/telechargements.js` déclenchant un
Blob côté navigateur), `GestionGrillesTarifaires.razor` (`/admin/tarification` — liste des versions,
import CSV avec rapport d'erreurs). Intégrées à `NavMenu.razor` (espaces Client, Administrateur
Société, Administration CIT) et au formulaire de soumission Billing Import/Export (champ optionnel de
rattachement).

## 14. Sécurité

- **Cloisonnement multi-tenant** : `ConsulterSimulationProformaQuery`, `GenererDocumentProformaPdfQuery`
  et `ListerSimulationsProformaQuery` vérifient la société de l'appelant côté serveur
  (`UnauthorizedAccessException` sinon) — testé par IDOR direct (société A → simulation société B →
  refusé) dans `CloisonnementSimulationProformaTests.cs` et `GenererDocumentProformaPdfTests.cs`.
- **Administration tarifaire** : **écart réel détecté et corrigé pendant cette session** (voir §17) —
  `CreerVersionGrilleTarifaireCommandHandler`, `ImporterGrilleTarifaireCsvCommandHandler` et
  `ConsulterVersionsGrillesTarifairesQueryHandler` exigent désormais explicitement
  `Permission.TarificationAdministrer` via `IAutorisationService.ExigerAsync` — jamais de rôle
  Identity figé, jamais une simple authentification suffisante. Testé négativement et positivement
  dans `AdministrationTarifaireAutorisationTests.cs` (4 tests).
- **Zéro GUID manuel** : les 4 pages Blazor résolvent l'identité exclusivement via
  `IContexteUtilisateurWeb.ObtenirContexteAsync()` — vérifié par scan automatisé du code source réel
  dans `SimulationProformaWebTests.cs` (absence de `Guid.Parse`/`Guid.TryParse`/champ « identifiant
  (démonstration) »).
- **Zéro accès direct à l'infrastructure depuis Razor** : aucune des 4 pages n'injecte
  `PlateformeDbContext` ni un repository Infrastructure — vérifié par le même scan automatisé.

## 15. Migrations

Migration unique `20260830111757_AjoutSupportSimulationProforma` : 1 `AddColumn`
(`Dossier.ReferenceSimulationProforma`) + 4 `CreateTable` (`VersionsGrillesTarifaires`,
`LignesTarifaires`, `SimulationsProforma`, `LignesResultatSimulationConteneur`) — inspectée
manuellement, aucune opération inattendue. `dotnet ef migrations has-pending-model-changes` confirme
**0 changement en attente** : le modèle EF Core en mémoire (après les corrections du §17) correspond
exactement à la migration déjà générée. La persistance et la précision `decimal` ont été vérifiées
contre une base SQL Server réelle (LocalDB) par `TarificationSqlPersistenceTests.cs`, y compris après
un rechargement complet (§3). *Note de transparence* : une réapplication complète des migrations
F001→005 sur une base strictement vide via `dotnet ef database update` suivie d'une inspection
`sqlcmd` indépendante (pratique déjà appliquée pour les Features 001-004) n'a pas été rejouée
séparément dans cette session — la parité modèle/migration (0 changement en attente) et les tests
d'intégration SQL Server réels ci-dessus en constituent une preuve équivalente mais non identique.

## 16. REC-SCE-06

Rejoué intégralement en bout en bout (`tests/CIT.Plateforme.Web.Tests/Tarification/RecSce06SimulationE2ETests.cs`,
via `PlateformeWebApplicationFactory` et résolution DI réelle, jamais en confiance sur le seul nom du
test) :

| Cas CDC | Entrées | Calcul | Résultat attendu | Résultat obtenu | Statut |
| :--- | :--- | :--- | ---: | ---: | :---: |
| `test01` | Import Transit, 20", Dry, 100t, OOG+Dang, C4 | Moteur réel via DI | 199 129 XOF | 199 129 XOF | ✅ |
| `test02` | Import Local, 40", Reefer, 200t, C1 | Moteur réel via DI | 2 054 980 XOF | 2 054 980 XOF | ✅ |
| `test03` | Export Local, 20", Dry, 300t, OOG, C2 | Moteur réel via DI | 245 500 XOF | 245 500 XOF | ✅ |
| `test04` | Export Transit, 40", Reefer, 100t, Dang, C5 | Moteur réel via DI | 2 053 400 XOF | 2 053 400 XOF | ✅ |
| `test06` | Import Local, 40", Reefer, 300t, C3 | Moteur réel via DI | 2 245 300 XOF | 2 245 300 XOF | ✅ |
| Modification de paramètre sans développement | v1.0 (Acconage 66 800) → v2.0 (Acconage 99 000), même combinaison | Recalcul après révision admin | Répercussion immédiate, sans code | 66 800→99 000 répercuté | ✅ |

## 17. Migrations de Code Détectées et Corrigées Pendant Cette Session

Quatre défauts réels ont été trouvés en revérifiant le travail pré-existant (aucun n'a été supposé
correct sur la seule foi des noms de fichiers ou de tests) :

1. **Bogue de compilation bloquant** : `ParametresTarification` assignait un champ `readonly` via
   syntaxe d'initialiseur d'objet (`CS0191`) — corrigé par un constructeur explicite.
2. **Rupture de mapping EF Core (bloquait 100 % des tests touchant la base SQL Server)** : les
   propriétés `FranchiseBranchementJours`/`FranchiseStationnementImport{Local,Transit}Jours` n'étaient
   pas explicitement déclarées dans la configuration `OwnsOne` de `PlateformeDbContext` — corrigé en
   les y ajoutant. Cette régression, non détectée par un simple `dotnet build`, n'a été révélée que par
   l'exécution réelle de la suite de tests (`dotnet test`), confirmant l'importance de ne jamais se
   fier à la seule compilation.
3. **`if` codé en dur sur le sens de trafic** pour l'applicabilité du stationnement dans
   `MoteurCalculProforma` — violait explicitement §11 du brief d'implémentation. Corrigé par une
   consultation de donnée (`ParametresTarification.StationnementApplicable`).
4. **Absence totale de contrôle d'autorisation** sur la création de version de grille, l'import CSV et
   la consultation des versions (§14, §20 du brief). Corrigé par l'ajout de
   `IAutorisationService.ExigerAsync(..., Permission.TarificationAdministrer)` dans les 3 handlers
   concernés, avec tests négatifs et positifs dédiés.

## 18. Dettes Assumées

- **`SIM-11`** (T036, Could) : non implémenté — voir §1. Aucun impact sur les exigences Must/Should.
- **Formulaire de création manuelle de grille** (hors CSV) : `CreerVersionGrilleTarifaireCommand`
  existe et est testé au niveau Application, mais aucune page Blazor dédiée à la saisie ligne par
  ligne d'une grille n'a été construite — le CSV est le canal d'administration principal retenu (CDC
  §SIM-10, Should) ; construire un éditeur de grille manuel complet aurait constitué une
  fonctionnalité non demandée par le CDC.
- **Réapplication `dotnet ef database update` sur base vide + inspection `sqlcmd` indépendante** :
  voir la note de transparence du §15.

## 19. Écarts

Aucun écart non documenté. Les 4 corrections du §17 sont les seuls écarts entre l'état du code au
début de cette session et l'état final ; elles sont exhaustivement documentées ci-dessus.

## 20. Statut Final

**Baseline de non-régression** : 424/424 tests verts (62 Domain, 217 Application, 60 Infrastructure,
85 Web) — 0 échec, aucun test ignoré. La baseline de départ réelle de cette session était de 396
tests (le chiffre de 352 annoncé par `tasks.md` correspondait à l'état avant l'introduction non
autorisée de la Feature 004 par un outil tiers dans une session antérieure — écart déjà signalé
transparemment lors du preflight et non résolu ici, hors périmètre de cette instruction).

Scans finaux (§33 du brief) : 0 `float`/`double` dans le moteur/l'Application Tarification ;
0 `PlateformeDbContext`/repository/`Guid.Parse`/GUID manuel dans les 4 pages Razor de la feature ;
0 valeur tarifaire codée en dur hors `ParametresTarification`/CSV/seed de test ; 0 conversion
automatique Simulation→Ticket ; 0 changement de modèle EF Core en attente.

**VERDICT : READY FOR POST-IMPLEMENTATION AUDIT**
