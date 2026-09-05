using CIT.Plateforme.Domain.Tarification.Parametres;

namespace CIT.Plateforme.Domain.Tarification;

/// <summary>
/// Agrégat racine : une version datée de grille tarifaire (SIM-06, SIM-07, RG-SIM-12).
///
/// PORTAIT AUPARAVANT un type possédé <c>ParametresTarification</c> de quatorze colonnes, dont
/// huit encodaient dans leur <b>nom</b> une dimension (taille, sens, rang de tranche,
/// bénéficiaire) qui aurait dû être une valeur. L'analyse Merise § 3.3 (correction n° 2) les a
/// extraites en cinq entités. Conséquence directe : une taille de conteneur, une tranche de
/// stationnement ou un bénéficiaire supplémentaires sont désormais des <b>données</b>, plus des
/// migrations — ce qu'exige RG-SIM-11 (SIM-06).
///
/// <see cref="FranchiseBranchementJours"/> reste un scalaire : il ne décline aucune dimension
/// (une seule franchise par grille, RG-SIM-05), et n'est donc pas un repeating group. Le MPD de
/// l'analyse ne lui donne d'ailleurs pas de table.
/// </summary>
public sealed class VersionGrilleTarifaire
{
    public Guid Id { get; private set; }

    public string NumeroVersion { get; private set; } = string.Empty;

    public DateTime DateEffetUtc { get; private set; }

    public DateTime DateCreationUtc { get; private set; }

    public Guid AuteurId { get; private set; }

    public bool EstActive { get; private set; }

    /// <summary>RG-SIM-05 — franchise en jours déduite de la durée de séjour avant facturation du
    /// branchement frigorifique (2 jours dans le paramétrage initial de l'Annexe A.3).</summary>
    public int FranchiseBranchementJours { get; private set; }

    private readonly List<LigneTarifaire> _lignes = [];
    public IReadOnlyCollection<LigneTarifaire> Lignes => _lignes.AsReadOnly();

    private readonly List<ParametreMajoration> _majorations = [];
    public IReadOnlyCollection<ParametreMajoration> Majorations => _majorations.AsReadOnly();

    private readonly List<SeuilPoidsMajoration> _seuilsPoids = [];
    public IReadOnlyCollection<SeuilPoidsMajoration> SeuilsPoids => _seuilsPoids.AsReadOnly();

    private readonly List<TrancheStationnement> _tranches = [];
    public IReadOnlyCollection<TrancheStationnement> Tranches => _tranches.AsReadOnly();

    private readonly List<FranchiseStationnement> _franchises = [];
    public IReadOnlyCollection<FranchiseStationnement> Franchises => _franchises.AsReadOnly();

    private readonly List<QuotePartBeneficiaire> _quotesParts = [];
    public IReadOnlyCollection<QuotePartBeneficiaire> QuotesParts => _quotesParts.AsReadOnly();

    private VersionGrilleTarifaire() { }

    public static VersionGrilleTarifaire Creer(
        string numeroVersion,
        DateTime dateEffetUtc,
        Guid auteurId,
        DateTime dateCreationUtc,
        int franchiseBranchementJours)
    {
        if (string.IsNullOrWhiteSpace(numeroVersion))
            throw new ArgumentException("Le numéro de version est obligatoire.", nameof(numeroVersion));
        if (franchiseBranchementJours < 0)
            throw new ArgumentException("La franchise de branchement ne peut pas être négative.", nameof(franchiseBranchementJours));

        return new VersionGrilleTarifaire
        {
            Id = Guid.NewGuid(),
            NumeroVersion = numeroVersion.Trim(),
            DateEffetUtc = dateEffetUtc,
            DateCreationUtc = dateCreationUtc,
            AuteurId = auteurId,
            EstActive = true,
            FranchiseBranchementJours = franchiseBranchementJours,
        };
    }

    // ---------------------------------------------------------------------
    // Constitution de la grille
    // ---------------------------------------------------------------------

    public void AjouterLigne(
        Guid sensOperationId,
        Guid tailleConteneurId,
        Guid categorieMarchandiseId,
        decimal acconage,
        decimal relevage,
        decimal tarifJourBranchement)
    {
        if (_lignes.Any(l => l.SensOperationId == sensOperationId
                          && l.TailleConteneurId == tailleConteneurId
                          && l.CategorieMarchandiseId == categorieMarchandiseId))
        {
            throw new InvalidOperationException(
                "Une ligne tarifaire existe déjà pour cette combinaison sens / taille / catégorie de marchandise.");
        }

        _lignes.Add(LigneTarifaire.Creer(
            Id, sensOperationId, tailleConteneurId, categorieMarchandiseId, acconage, relevage, tarifJourBranchement));
    }

    public void DefinirMajoration(TypeMajoration type, decimal taux)
    {
        _majorations.RemoveAll(m => m.Type == type);
        _majorations.Add(ParametreMajoration.Creer(Id, type, taux));
    }

    public void DefinirSeuilPoids(Guid tailleConteneurId, decimal seuilPoidsTonnes)
    {
        _seuilsPoids.RemoveAll(s => s.TailleConteneurId == tailleConteneurId);
        _seuilsPoids.Add(SeuilPoidsMajoration.Creer(Id, tailleConteneurId, seuilPoidsTonnes));
    }

    public void AjouterTranche(int borneMinJour, int? borneMaxJour, decimal tarifJournalier)
    {
        var nouvelle = TrancheStationnement.Creer(Id, borneMinJour, borneMaxJour, tarifJournalier);

        if (_tranches.Any(t => t.BorneMinJour == nouvelle.BorneMinJour))
        {
            throw new InvalidOperationException(
                $"Une tranche de stationnement commençant au jour {borneMinJour} est déjà définie pour cette grille.");
        }

        _tranches.Add(nouvelle);
    }

    public void DefinirFranchiseStationnement(Guid sensOperationId, int nombreJours)
    {
        _franchises.RemoveAll(f => f.SensOperationId == sensOperationId);
        _franchises.Add(FranchiseStationnement.Creer(Id, sensOperationId, nombreJours));
    }

    public void DefinirQuotePart(string beneficiaire, decimal pourcentage)
    {
        var code = beneficiaire.Trim().ToUpperInvariant();
        _quotesParts.RemoveAll(q => q.Beneficiaire == code);
        _quotesParts.Add(QuotePartBeneficiaire.Creer(Id, code, pourcentage));
    }

    // ---------------------------------------------------------------------
    // Lecture par le moteur de calcul
    // ---------------------------------------------------------------------

    public LigneTarifaire? ObtenirLigne(Guid sensOperationId, Guid tailleConteneurId, Guid categorieMarchandiseId) =>
        _lignes.SingleOrDefault(l => l.SensOperationId == sensOperationId
                                  && l.TailleConteneurId == tailleConteneurId
                                  && l.CategorieMarchandiseId == categorieMarchandiseId);

    /// <summary>Taux d'une majoration. Une majoration non paramétrée vaut zéro : elle ne
    /// s'applique pas, plutôt que de faire échouer le calcul.</summary>
    public decimal TauxMajoration(TypeMajoration type) =>
        _majorations.SingleOrDefault(m => m.Type == type)?.Taux ?? 0m;

    /// <summary>Seuil de déclenchement de la majoration « lourd » pour une taille.
    /// <c>null</c> quand aucun seuil n'est paramétré : la majoration ne s'applique alors pas,
    /// plutôt que de s'appliquer à un seuil deviné.</summary>
    public decimal? SeuilPoids(Guid tailleConteneurId) =>
        _seuilsPoids.SingleOrDefault(s => s.TailleConteneurId == tailleConteneurId)?.SeuilPoidsTonnes;

    /// <summary>RG-SIM-08 — un sens est soumis au stationnement si et seulement si une franchise
    /// lui est enregistrée. Le moteur ne teste jamais la valeur du sens lui-même.</summary>
    public bool StationnementApplicable(Guid sensOperationId) =>
        _franchises.Any(f => f.SensOperationId == sensOperationId);

    public int FranchiseStationnementJours(Guid sensOperationId) =>
        _franchises.Single(f => f.SensOperationId == sensOperationId).NombreJours;

    public IReadOnlyList<TrancheStationnement> TranchesOrdonnees =>
        [.. _tranches.OrderBy(t => t.BorneMinJour)];

    // ---------------------------------------------------------------------
    // Invariants
    // ---------------------------------------------------------------------

    /// <summary>
    /// RG-SIM-09 — la répartition du stationnement doit être totale. Vérifié ici, sur l'agrégat,
    /// seul à connaître l'ensemble des quotes-parts : une ligne isolée ne peut pas se valider.
    /// Une grille sans aucune quote-part est acceptée — elle n'applique alors aucune répartition.
    /// </summary>
    public void ValiderRepartitionStationnement()
    {
        if (_quotesParts.Count == 0)
        {
            return;
        }

        var somme = _quotesParts.Sum(q => q.Pourcentage);
        if (somme != 1m)
        {
            throw new InvalidOperationException(
                $"La somme des quotes-parts de répartition du stationnement doit être égale à 1 (100 %). Somme constatée : {somme}.");
        }
    }

    public void Desactiver() => EstActive = false;

    public void Activer() => EstActive = true;
}
