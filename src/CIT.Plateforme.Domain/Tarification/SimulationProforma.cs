namespace CIT.Plateforme.Domain.Tarification;

/// <summary>
/// Agrégat racine : une simulation de proforma enregistrée par un client (SIM-08).
///
/// Une simulation est un <b>fait daté</b>, non une vue sur la grille courante : elle référence la
/// version de grille appliquée (RG-SIM-12) et conserve les montants tels qu'ils ont été présentés,
/// afin de rester reproductible après une évolution tarifaire (SIM-09). C'est la dérogation à la
/// 3FN documentée et justifiée en § 3.3 de l'analyse Merise — l'équivalent du prix unitaire figé
/// sur une ligne de facture.
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

    public Guid SensOperationId { get; private set; }

    public DateOnly DateArrivee { get; private set; }

    public DateOnly DateEnlevementPrevisionnel { get; private set; }

    public int DureeSejourJours { get; private set; }

    public string? NomNavire { get; private set; }

    public string? NumeroVoyage { get; private set; }

    public string? NumeroBlOuBooking { get; private set; }

    public string? ClientAFacturer { get; private set; }

    public string? TypeClientLibelle { get; private set; }

    public string? Escale { get; private set; }

    public string? PortChargement { get; private set; }

    public string? PortDechargement { get; private set; }

    public decimal MontantTotalXof { get; private set; }

    /// <summary>SIM-08 — rattachement a posteriori à un ticket de parcours client.</summary>
    public Guid? TicketId { get; private set; }

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
            SensOperationId = dossier.SensOperationId,
            DateArrivee = dossier.DateArrivee,
            DateEnlevementPrevisionnel = dossier.DateEnlevementPrevisionnel,
            DureeSejourJours = dossier.DureeSejourJours,
            NomNavire = dossier.NomNavire?.Trim(),
            NumeroVoyage = dossier.NumeroVoyage?.Trim(),
            NumeroBlOuBooking = dossier.NumeroBlOuBooking?.Trim(),
            ClientAFacturer = dossier.ClientAFacturer?.Trim(),
            TypeClientLibelle = dossier.TypeClientLibelle?.Trim(),
            Escale = dossier.Escale?.Trim(),
            PortChargement = dossier.PortChargement?.Trim(),
            PortDechargement = dossier.PortDechargement?.Trim(),
            MontantTotalXof = lignesResultat.Sum(l => l.TotalLigne),
        };

        foreach (var l in lignesResultat)
        {
            sim._lignesConteneurs.Add(LigneResultatSimulationConteneur.Creer(sim.Id, l));
        }

        return sim;
    }

    /// <summary>SIM-08 — rattache la simulation à un ticket créé après coup. Idempotent.</summary>
    public void RattacherAuTicket(Guid ticketId)
    {
        if (ticketId == Guid.Empty)
            throw new ArgumentException("Le ticket de rattachement est obligatoire.", nameof(ticketId));

        TicketId = ticketId;
    }
}

/// <summary>
/// Ligne de résultat conteneur, persistée comme instantané immuable (SIM-08, D6).
///
/// Taille, type et catégorie sont des clés étrangères vers les référentiels normalisés
/// (analyse Merise § 3.3, correction n° 1) : le libellé se résout à la lecture, si bien qu'un
/// renommage de référentiel ne réécrit pas l'histoire mais reste correctement rattaché.
/// </summary>
public sealed class LigneResultatSimulationConteneur
{
    public Guid Id { get; private set; }

    public Guid SimulationProformaId { get; private set; }

    public string NumeroConteneur { get; private set; } = string.Empty;

    public Guid TailleConteneurId { get; private set; }

    public Guid TypeConteneurId { get; private set; }

    public Guid CategorieMarchandiseId { get; private set; }

    public decimal PoidsTonnes { get; private set; }

    public bool EstDangereux { get; private set; }

    public bool EstHorsGabarit { get; private set; }

    public decimal MontantAcconage { get; private set; }

    public decimal MontantRelevage { get; private set; }

    public decimal MontantBranchement { get; private set; }

    public decimal MontantMajorationLourd { get; private set; }

    public decimal MontantMajorationDangereux { get; private set; }

    public decimal MontantMajorationOog { get; private set; }

    public decimal MontantStationnement { get; private set; }

    public decimal TotalLigneXof { get; private set; }

    private readonly List<PartStationnementSimulee> _repartitionStationnement = [];

    /// <summary>
    /// RG-SIM-09 — ventilation du stationnement telle qu'elle a été présentée au client.
    ///
    /// ÉTAIT TROIS COLONNES <c>PartCit</c> / <c>PartOic</c> / <c>PartPaa</c>. Les bénéficiaires
    /// étant devenus paramétrables, l'instantané ne peut plus les nommer dans son schéma.
    /// </summary>
    public IReadOnlyCollection<PartStationnementSimulee> RepartitionStationnement => _repartitionStationnement.AsReadOnly();

    private LigneResultatSimulationConteneur() { }

    public static LigneResultatSimulationConteneur Creer(Guid simulationProformaId, LigneResultatConteneur r)
    {
        var ligne = new LigneResultatSimulationConteneur
        {
            Id = Guid.NewGuid(),
            SimulationProformaId = simulationProformaId,
            NumeroConteneur = r.Conteneur.NumeroConteneur.Trim(),
            TailleConteneurId = r.Taille.Id,
            TypeConteneurId = r.Type.Id,
            CategorieMarchandiseId = r.Categorie.Id,
            PoidsTonnes = r.Conteneur.PoidsTonnes,
            EstDangereux = r.Conteneur.EstDangereux,
            EstHorsGabarit = r.Conteneur.EstHorsGabarit,
            MontantAcconage = r.Acconage,
            MontantRelevage = r.Relevage,
            MontantBranchement = r.BranchementFrigorifique,
            MontantMajorationLourd = r.MajorationLourd,
            MontantMajorationDangereux = r.MajorationDangereux,
            MontantMajorationOog = r.MajorationHorsGabarit,
            MontantStationnement = r.Stationnement,
            TotalLigneXof = r.TotalLigne,
        };

        foreach (var part in r.RepartitionStationnement)
        {
            ligne._repartitionStationnement.Add(
                PartStationnementSimulee.Creer(ligne.Id, part.Beneficiaire, part.Montant));
        }

        return ligne;
    }

    /// <summary>Part d'un bénéficiaire nommé, ou zéro s'il n'en portait pas sur cette grille.</summary>
    public decimal Part(string beneficiaire) =>
        _repartitionStationnement
            .SingleOrDefault(p => string.Equals(p.Beneficiaire, beneficiaire, StringComparison.OrdinalIgnoreCase))
            ?.Montant ?? 0m;
}

/// <summary>Part figée d'un bénéficiaire dans le stationnement d'un conteneur simulé.</summary>
public sealed class PartStationnementSimulee
{
    public Guid Id { get; private set; }

    public Guid LigneResultatSimulationConteneurId { get; private set; }

    public string Beneficiaire { get; private set; } = string.Empty;

    public decimal Montant { get; private set; }

    private PartStationnementSimulee() { }

    public static PartStationnementSimulee Creer(Guid ligneId, string beneficiaire, decimal montant) => new()
    {
        Id = Guid.NewGuid(),
        LigneResultatSimulationConteneurId = ligneId,
        Beneficiaire = beneficiaire.Trim().ToUpperInvariant(),
        Montant = montant,
    };
}
