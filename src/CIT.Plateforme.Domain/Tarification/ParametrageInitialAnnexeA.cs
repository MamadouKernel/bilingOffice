using CIT.Plateforme.Domain.Tarification.Parametres;
using CIT.Plateforme.Domain.Tarification.Referentiels;

namespace CIT.Plateforme.Domain.Tarification;

/// <summary>
/// Jeu de référentiels et de paramètres de l'Annexe A du CDC — valeurs de <b>départ</b>, jamais
/// des constantes de calcul.
///
/// POURQUOI CE TYPE EXISTE. Les référentiels tarifaires sont devenus des tables (analyse Merise
/// § 3.3). Une base vide ne permettrait donc plus la moindre simulation, alors qu'avant la
/// correction les énumérations C# tenaient ce rôle implicitement. Ce type rassemble en un seul
/// endroit les codes et les valeurs de l'Annexe A, pour l'approvisionnement initial comme pour
/// les jeux d'essai de l'Annexe A.8.
///
/// Aucune de ces valeurs n'est lue par le moteur : il ne connaît que ce que porte la grille
/// (RG-SIM-11 — tout est administrable sans développement).
/// </summary>
public static class ParametrageInitialAnnexeA
{
    public const string SensImportLocal = "IMPORT_LOCAL";
    public const string SensImportTransit = "IMPORT_TRANSIT";
    public const string SensExportLocal = "EXPORT_LOCAL";
    public const string SensExportTransit = "EXPORT_TRANSIT";

    public const string TailleVingtPieds = "20";
    public const string TailleQuarantePieds = "40";

    public const string TypeDry = "DRY";
    public const string TypeReefer = "REEFER";

    public const string BeneficiaireCit = "CIT";
    public const string BeneficiaireOic = "OIC";
    public const string BeneficiairePaa = "PAA";

    /// <summary>Annexe A.3 — franchise avant facturation du branchement frigorifique.</summary>
    public const int FranchiseBranchementJours = 2;

    public static readonly (string Code, string Libelle, int Ordre)[] Sens =
    [
        (SensImportLocal, "Import local", 10),
        (SensImportTransit, "Import transit", 20),
        (SensExportLocal, "Export local", 30),
        (SensExportTransit, "Export transit", 40),
    ];

    public static readonly (string Code, string Libelle, int Ordre)[] Tailles =
    [
        (TailleVingtPieds, "20 pieds", 10),
        (TailleQuarantePieds, "40 pieds", 20),
    ];

    /// <summary>RG-SIM-05 — seul le Reefer déclenche le poste « branchement frigorifique ».
    /// Porté ici comme une donnée du référentiel, non comme un test dans le moteur.</summary>
    public static readonly (string Code, string Libelle, bool Frigorifique, int Ordre)[] Types =
    [
        (TypeDry, "Dry", false, 10),
        (TypeReefer, "Reefer", true, 20),
    ];

    /// <summary>
    /// Annexe A.6 — les catégories sont scindées par sens : « C1 import » et « C1 export » ne
    /// désignent pas le même tarif, et C4 / C5 n'existent qu'en transit. C'est ce que traduit la
    /// clé étrangère de <see cref="CategorieMarchandise"/> vers le sens.
    /// </summary>
    public static readonly (string SensCode, string Code, string Libelle)[] Categories =
    [
        (SensImportLocal, "C1", "Catégorie 1 — import local"),
        (SensImportLocal, "C2", "Catégorie 2 — import local"),
        (SensImportLocal, "C3", "Catégorie 3 — import local"),
        (SensExportLocal, "C1", "Catégorie 1 — export local"),
        (SensExportLocal, "C2", "Catégorie 2 — export local"),
        (SensExportLocal, "C3", "Catégorie 3 — export local"),
        (SensImportTransit, "C4", "Catégorie 4 — import transit"),
        (SensExportTransit, "C5", "Catégorie 5 — export transit"),
    ];

    /// <summary>Construit un jeu de référentiels neuf, avec des identifiants frais.</summary>
    public static ReferentielsAnnexeA CreerReferentiels()
    {
        var sens = Sens.Select(s => SensOperation.Creer(s.Code, s.Libelle, s.Ordre)).ToList();
        var tailles = Tailles.Select(t => TailleConteneur.Creer(t.Code, t.Libelle, t.Ordre)).ToList();
        var types = Types.Select(t => TypeConteneur.Creer(t.Code, t.Libelle, t.Frigorifique, t.Ordre)).ToList();

        var categories = Categories
            .Select(c => CategorieMarchandise.Creer(
                c.Code, c.Libelle, sens.Single(s => s.Code == c.SensCode).Id))
            .ToList();

        return new ReferentielsAnnexeA(sens, tailles, types, categories);
    }

    /// <summary>
    /// Applique à une grille les taux, seuils, tranches, franchises et quotes-parts de l'Annexe A
    /// (A.2, A.4, A.5). N'ajoute aucune ligne tarifaire : celles-ci relèvent de l'import CSV ou de
    /// la saisie de l'administrateur tarifaire.
    /// </summary>
    public static void AppliquerParametres(VersionGrilleTarifaire grille, ReferentielsAnnexeA referentiels)
    {
        ArgumentNullException.ThrowIfNull(referentiels);
        AppliquerParametres(grille, referentiels.Index);
    }

    /// <summary>
    /// Même paramétrage, appliqué à des référentiels chargés depuis la base : les identifiants
    /// n'y sont pas ceux d'un jeu fraîchement construit, seuls les codes sont stables. Un code
    /// absent fait échouer l'application plutôt que de produire une grille silencieusement
    /// incomplète — une franchise manquante exonérerait tout un sens de stationnement.
    /// </summary>
    public static void AppliquerParametres(VersionGrilleTarifaire grille, ReferentielsTarifaires referentiels)
    {
        ArgumentNullException.ThrowIfNull(grille);
        ArgumentNullException.ThrowIfNull(referentiels);

        TailleConteneur Taille(string code) => referentiels.TailleParCode(code)
            ?? throw new InvalidOperationException(
                $"Le référentiel des tailles de conteneur ne comporte pas le code « {code} », requis par le paramétrage de l'Annexe A.");

        SensOperation Sens(string code) => referentiels.SensParCode(code)
            ?? throw new InvalidOperationException(
                $"Le référentiel des sens d'opération ne comporte pas le code « {code} », requis par le paramétrage de l'Annexe A.");

        // Annexe A.2 — taux de majoration.
        grille.DefinirMajoration(TypeMajoration.Lourd, 0.30m);
        grille.DefinirMajoration(TypeMajoration.Dangereux, 0.50m);
        grille.DefinirMajoration(TypeMajoration.HorsGabarit, 0.50m);

        // Annexe A.2 — seuils de poids, un par taille.
        grille.DefinirSeuilPoids(Taille(TailleVingtPieds).Id, 15m);
        grille.DefinirSeuilPoids(Taille(TailleQuarantePieds).Id, 26m);

        // Annexe A.4 — tranches de stationnement, bornes comprises.
        grille.AjouterTranche(1, 5, 850m);
        grille.AjouterTranche(6, 15, 1250m);
        grille.AjouterTranche(16, null, 1850m);

        // Annexe A.4 — franchises. L'export n'en porte aucune : RG-SIM-08 précise que le
        // stationnement ne lui est pas appliqué dans l'outil actuel. Cette absence EST la règle.
        grille.DefinirFranchiseStationnement(Sens(SensImportLocal).Id, 11);
        grille.DefinirFranchiseStationnement(Sens(SensImportTransit).Id, 21);

        // Annexe A.5 — répartition du stationnement.
        grille.DefinirQuotePart(BeneficiaireCit, 0.30m);
        grille.DefinirQuotePart(BeneficiaireOic, 0.20m);
        grille.DefinirQuotePart(BeneficiairePaa, 0.50m);

        grille.ValiderRepartitionStationnement();
    }
}

/// <summary>
/// Référentiels de l'Annexe A instanciés, avec accès par code — les codes sont l'identité stable
/// que désignent le CDC, les imports CSV et les jeux d'essai.
/// </summary>
public sealed class ReferentielsAnnexeA(
    IReadOnlyList<SensOperation> sens,
    IReadOnlyList<TailleConteneur> tailles,
    IReadOnlyList<TypeConteneur> types,
    IReadOnlyList<CategorieMarchandise> categories)
{
    public IReadOnlyList<SensOperation> Sens { get; } = sens;

    public IReadOnlyList<TailleConteneur> Tailles { get; } = tailles;

    public IReadOnlyList<TypeConteneur> Types { get; } = types;

    public IReadOnlyList<CategorieMarchandise> Categories { get; } = categories;

    public ReferentielsTarifaires Index => new(Sens, Tailles, Types, Categories);

    public SensOperation ParSens(string code) => Sens.Single(s => s.Code == code);

    public TailleConteneur ParTaille(string code) => Tailles.Single(t => t.Code == code);

    public TypeConteneur ParType(string code) => Types.Single(t => t.Code == code);

    /// <summary>Une catégorie ne se désigne que par le couple (sens, code) — Annexe A.6.</summary>
    public CategorieMarchandise ParCategorie(string sensCode, string code)
    {
        var sensId = ParSens(sensCode).Id;
        return Categories.Single(c => c.SensOperationId == sensId && c.Code == code);
    }
}
