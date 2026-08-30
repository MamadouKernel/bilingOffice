namespace CIT.Plateforme.Domain.Tarification;

/// <summary>
/// Agrégat racine représentant une version de grille tarifaire horodatée avec date d'effet (SIM-06, SIM-07).
/// </summary>
public sealed class VersionGrilleTarifaire
{
    public Guid Id { get; private set; }
    public string NumeroVersion { get; private set; } = string.Empty;
    public DateTime DateEffetUtc { get; private set; }
    public DateTime DateCreationUtc { get; private set; }
    public Guid AuteurId { get; private set; }
    public bool EstActive { get; private set; }

    public ParametresTarification Parametres { get; private set; } = null!;

    private readonly List<LigneTarifaire> _lignes = [];
    public IReadOnlyCollection<LigneTarifaire> Lignes => _lignes.AsReadOnly();

    private VersionGrilleTarifaire() { }

    public static VersionGrilleTarifaire Creer(
        string numeroVersion,
        DateTime dateEffetUtc,
        Guid auteurId,
        DateTime dateCreationUtc,
        ParametresTarification? parametres = null)
    {
        if (string.IsNullOrWhiteSpace(numeroVersion))
            throw new ArgumentException("Le numéro de version est obligatoire.", nameof(numeroVersion));

        return new VersionGrilleTarifaire
        {
            Id = Guid.NewGuid(),
            NumeroVersion = numeroVersion.Trim(),
            DateEffetUtc = dateEffetUtc,
            DateCreationUtc = dateCreationUtc,
            AuteurId = auteurId,
            EstActive = true,
            Parametres = parametres ?? ParametresTarification.CreerDefautAnnexeA()
        };
    }

    public void AjouterLigne(SensTrafic sens, TailleConteneur taille, string categorieMarchandise, decimal acconage, decimal relevage, decimal tarifJourBranchement)
    {
        var cleExiste = _lignes.Any(l => l.Sens == sens && l.Taille == taille && string.Equals(l.CategorieMarchandise, categorieMarchandise.Trim(), StringComparison.OrdinalIgnoreCase));
        if (cleExiste)
            throw new InvalidOperationException($"Une ligne existe déjà pour la combinaison {sens}, {taille}, {categorieMarchandise}.");

        _lignes.Add(LigneTarifaire.Creer(Id, sens, taille, categorieMarchandise, acconage, relevage, tarifJourBranchement));
    }

    public LigneTarifaire? ObtenirLigne(SensTrafic sens, TailleConteneur taille, string categorieMarchandise)
    {
        return _lignes.SingleOrDefault(l => l.Sens == sens && l.Taille == taille && string.Equals(l.CategorieMarchandise, categorieMarchandise.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public void Desactiver() => EstActive = false;
    public void Activer() => EstActive = true;
}
