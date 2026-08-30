namespace CIT.Plateforme.Domain.Tarification;

/// <summary>
/// Agrégat racine représentant une simulation de proforma enregistrée par un client (SIM-08).
/// </summary>
public sealed class SimulationProforma
{
    public Guid Id { get; private set; }
    public string Reference { get; private set; } = string.Empty;
    public Guid SocieteId { get; private set; }
    public Guid AuteurId { get; private set; }
    public DateTime DateCreationUtc { get; private set; }
    public Guid VersionGrilleTarifaireId { get; private set; }
    public string VersionGrilleLibelle { get; private set; } = string.Empty;

    public SensTrafic Sens { get; private set; }
    public DateOnly DateArrivee { get; private set; }
    public DateOnly DateEnlevementPrevisionnel { get; private set; }
    public int DureeSejourJours { get; private set; }

    public string? NomNavire { get; private set; }
    public string? NumeroVoyage { get; private set; }
    public string? NumeroBlOuBooking { get; private set; }

    public decimal MontantTotalXof { get; private set; }

    private readonly List<LigneResultatSimulationConteneur> _lignesConteneurs = [];
    public IReadOnlyCollection<LigneResultatSimulationConteneur> LignesConteneurs => _lignesConteneurs.AsReadOnly();

    private SimulationProforma() { }

    public static SimulationProforma Creer(
        string reference,
        Guid societeId,
        Guid auteurId,
        DateTime dateCreationUtc,
        Guid versionGrilleTarifaireId,
        string versionGrilleLibelle,
        DossierSimulation dossier,
        IReadOnlyList<LigneResultatConteneur> lignesResultat)
    {
        if (string.IsNullOrWhiteSpace(reference))
            throw new ArgumentException("La référence est obligatoire.", nameof(reference));
        if (lignesResultat.Count == 0)
            throw new ArgumentException("Au moins un conteneur est requis.", nameof(lignesResultat));

        var sim = new SimulationProforma
        {
            Id = Guid.NewGuid(),
            Reference = reference.Trim(),
            SocieteId = societeId,
            AuteurId = auteurId,
            DateCreationUtc = dateCreationUtc,
            VersionGrilleTarifaireId = versionGrilleTarifaireId,
            VersionGrilleLibelle = versionGrilleLibelle,
            Sens = dossier.Sens,
            DateArrivee = dossier.DateArrivee,
            DateEnlevementPrevisionnel = dossier.DateEnlevementPrevisionnel,
            DureeSejourJours = dossier.DureeSejourJours,
            NomNavire = dossier.NomNavire?.Trim(),
            NumeroVoyage = dossier.NumeroVoyage?.Trim(),
            NumeroBlOuBooking = dossier.NumeroBlOuBooking?.Trim(),
            MontantTotalXof = lignesResultat.Sum(l => l.TotalLigne)
        };

        foreach (var l in lignesResultat)
        {
            sim._lignesConteneurs.Add(LigneResultatSimulationConteneur.Creer(sim.Id, l));
        }

        return sim;
    }
}

/// <summary>
/// Ligne de résultat conteneur persistée comme snapshot immuable (SIM-08, D6).
/// </summary>
public sealed class LigneResultatSimulationConteneur
{
    public Guid Id { get; private set; }
    public Guid SimulationProformaId { get; private set; }

    public string NumeroConteneur { get; private set; } = string.Empty;
    public TailleConteneur Taille { get; private set; }
    public TypeConteneur Type { get; private set; }
    public decimal PoidsTonnes { get; private set; }
    public bool EstDangereux { get; private set; }
    public bool EstHorsGabarit { get; private set; }
    public string CategorieMarchandise { get; private set; } = string.Empty;

    public decimal MontantAcconage { get; private set; }
    public decimal MontantRelevage { get; private set; }
    public decimal MontantBranchement { get; private set; }
    public decimal MontantMajorationLourd { get; private set; }
    public decimal MontantMajorationDangereux { get; private set; }
    public decimal MontantMajorationOog { get; private set; }
    public decimal MontantStationnement { get; private set; }
    public decimal PartCit { get; private set; }
    public decimal PartOic { get; private set; }
    public decimal PartPaa { get; private set; }
    public decimal TotalLigneXof { get; private set; }

    private LigneResultatSimulationConteneur() { }

    public static LigneResultatSimulationConteneur Creer(Guid simulationProformaId, LigneResultatConteneur r)
    {
        return new LigneResultatSimulationConteneur
        {
            Id = Guid.NewGuid(),
            SimulationProformaId = simulationProformaId,
            NumeroConteneur = r.Conteneur.NumeroConteneur.Trim(),
            Taille = r.Conteneur.Taille,
            Type = r.Conteneur.Type,
            PoidsTonnes = r.Conteneur.PoidsTonnes,
            EstDangereux = r.Conteneur.EstDangereux,
            EstHorsGabarit = r.Conteneur.EstHorsGabarit,
            CategorieMarchandise = r.Conteneur.CategorieMarchandise.Trim(),
            MontantAcconage = r.Acconage,
            MontantRelevage = r.Relevage,
            MontantBranchement = r.BranchementFrigorifique,
            MontantMajorationLourd = r.MajorationLourd,
            MontantMajorationDangereux = r.MajorationDangereux,
            MontantMajorationOog = r.MajorationHorsGabarit,
            MontantStationnement = r.Stationnement,
            PartCit = r.PartCit,
            PartOic = r.PartOic,
            PartPaa = r.PartPaa,
            TotalLigneXof = r.TotalLigne
        };
    }
}
