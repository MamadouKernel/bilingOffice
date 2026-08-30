namespace CIT.Plateforme.Domain.Tarification;

/// <summary>
/// FEATURE 005 (research.md D4, SIM-06) : objet de valeur rattaché à une
/// <see cref="VersionGrilleTarifaire"/>, portant tous les taux/seuils/franchises/tranches/quotes-parts
/// administrables sans développement. Aucune valeur n'est codée en dur dans le moteur de calcul —
/// toute grille future porte ses propres paramètres.
/// </summary>
public sealed class ParametresTarification
{
    public decimal TauxMajorationLourd { get; }
    public decimal TauxMajorationDangereux { get; }
    public decimal TauxMajorationOog { get; }
    public decimal SeuilPoidsLourd20Pieds { get; }
    public decimal SeuilPoidsLourd40Pieds { get; }
    public int FranchiseBranchementJours { get; }

    /// <summary>
    /// FEATURE 005 (spec.md §11, data-model.md §4) : colonnes persistées (aplaties) des franchises de
    /// stationnement pour les seuls sens soumis au paramétrage initial. Export n'a volontairement
    /// aucune colonne — c'est la seule expression de son exclusion ; le moteur et
    /// <see cref="StationnementApplicable"/>/<see cref="FranchiseStationnementJours"/> ne testent
    /// jamais explicitement le sens, ils consultent uniquement <see cref="_franchisesParSens"/>.
    /// </summary>
    public int FranchiseStationnementImportLocalJours { get; }
    public int FranchiseStationnementImportTransitJours { get; }

    public decimal TarifStationnementTranche1Jour { get; }
    public decimal TarifStationnementTranche2Jour { get; }
    public decimal TarifStationnementTranche3Jour { get; }
    public decimal QuotePartCit { get; }
    public decimal QuotePartOic { get; }
    public decimal QuotePartPaa { get; }

    private readonly Dictionary<SensTrafic, int> _franchisesParSens;

    private ParametresTarification(
        decimal tauxMajorationLourd,
        decimal tauxMajorationDangereux,
        decimal tauxMajorationOog,
        decimal seuilPoidsLourd20Pieds,
        decimal seuilPoidsLourd40Pieds,
        int franchiseBranchementJours,
        int franchiseStationnementImportLocalJours,
        int franchiseStationnementImportTransitJours,
        decimal tarifStationnementTranche1Jour,
        decimal tarifStationnementTranche2Jour,
        decimal tarifStationnementTranche3Jour,
        decimal quotePartCit,
        decimal quotePartOic,
        decimal quotePartPaa)
    {
        TauxMajorationLourd = tauxMajorationLourd;
        TauxMajorationDangereux = tauxMajorationDangereux;
        TauxMajorationOog = tauxMajorationOog;
        SeuilPoidsLourd20Pieds = seuilPoidsLourd20Pieds;
        SeuilPoidsLourd40Pieds = seuilPoidsLourd40Pieds;
        FranchiseBranchementJours = franchiseBranchementJours;
        FranchiseStationnementImportLocalJours = franchiseStationnementImportLocalJours;
        FranchiseStationnementImportTransitJours = franchiseStationnementImportTransitJours;
        TarifStationnementTranche1Jour = tarifStationnementTranche1Jour;
        TarifStationnementTranche2Jour = tarifStationnementTranche2Jour;
        TarifStationnementTranche3Jour = tarifStationnementTranche3Jour;
        QuotePartCit = quotePartCit;
        QuotePartOic = quotePartOic;
        QuotePartPaa = quotePartPaa;

        // Seuls les sens explicitement soumis au stationnement dans le paramétrage initial
        // (spec.md §4.2.7) portent une entrée — Export n'y figure pas : aucun `if` sur le sens
        // ailleurs dans le code, ni dans le moteur, ni ici.
        _franchisesParSens = new Dictionary<SensTrafic, int>
        {
            [SensTrafic.ImportLocal] = franchiseStationnementImportLocalJours,
            [SensTrafic.ImportTransit] = franchiseStationnementImportTransitJours,
        };
    }

    public static ParametresTarification Creer(
        decimal tauxMajorationLourd,
        decimal tauxMajorationDangereux,
        decimal tauxMajorationOog,
        decimal seuilPoidsLourd20Pieds,
        decimal seuilPoidsLourd40Pieds,
        int franchiseBranchementJours,
        int franchiseStationnementImportLocalJours,
        int franchiseStationnementImportTransitJours,
        decimal tarifStationnementTranche1Jour,
        decimal tarifStationnementTranche2Jour,
        decimal tarifStationnementTranche3Jour,
        decimal quotePartCit,
        decimal quotePartOic,
        decimal quotePartPaa)
    {
        if (quotePartCit + quotePartOic + quotePartPaa != 1m)
        {
            throw new ArgumentException(
                "La somme des quotes-parts de répartition du stationnement (CIT + OIC + PAA) doit être égale à 1 (100 %).");
        }

        return new ParametresTarification(
            tauxMajorationLourd,
            tauxMajorationDangereux,
            tauxMajorationOog,
            seuilPoidsLourd20Pieds,
            seuilPoidsLourd40Pieds,
            franchiseBranchementJours,
            franchiseStationnementImportLocalJours,
            franchiseStationnementImportTransitJours,
            tarifStationnementTranche1Jour,
            tarifStationnementTranche2Jour,
            tarifStationnementTranche3Jour,
            quotePartCit,
            quotePartOic,
            quotePartPaa);
    }

    /// <summary>
    /// Paramètres initiaux de l'Annexe A du CDC (spec.md §4.2, §4.3) — seed de la version v1.0 de la
    /// grille tarifaire. Ces valeurs ne sont utilisées qu'à la construction de la version initiale ;
    /// aucune valeur n'est codée en dur ailleurs dans le moteur.
    /// </summary>
    public static ParametresTarification CreerDefautAnnexeA() => Creer(
        tauxMajorationLourd: 0.30m,
        tauxMajorationDangereux: 0.50m,
        tauxMajorationOog: 0.50m,
        seuilPoidsLourd20Pieds: 15m,
        seuilPoidsLourd40Pieds: 26m,
        franchiseBranchementJours: 2,
        franchiseStationnementImportLocalJours: 11,
        franchiseStationnementImportTransitJours: 21,
        tarifStationnementTranche1Jour: 850m,
        tarifStationnementTranche2Jour: 1250m,
        tarifStationnementTranche3Jour: 1850m,
        quotePartCit: 0.30m,
        quotePartOic: 0.20m,
        quotePartPaa: 0.50m);

    /// <summary>
    /// Seuil de déclenchement de la majoration Lourd pour une taille donnée (spec.md §4.2.4) —
    /// jamais de conversion implicite entre tailles : chaque taille porte son propre seuil paramétré.
    /// </summary>
    public decimal SeuilPoidsLourd(TailleConteneur taille) => taille switch
    {
        TailleConteneur.VingtPieds => SeuilPoidsLourd20Pieds,
        TailleConteneur.QuarantePieds => SeuilPoidsLourd40Pieds,
        _ => throw new ArgumentOutOfRangeException(nameof(taille), taille, "Taille de conteneur non reconnue."),
    };

    /// <summary>Un sens est soumis au stationnement si et seulement si une franchise lui est
    /// associée dans la donnée de grille (jamais un test sur la valeur du sens lui-même).</summary>
    public bool StationnementApplicable(SensTrafic sens) => _franchisesParSens.ContainsKey(sens);

    /// <summary>Franchise de stationnement (en jours) pour un sens donné — n'est appelée que si
    /// <see cref="StationnementApplicable"/> est vraie.</summary>
    public int FranchiseStationnementJours(SensTrafic sens) => _franchisesParSens[sens];
}
