namespace CIT.Plateforme.Domain.Tarification;

/// <summary>
/// Ligne tarifaire de base définie par le triplet Sens + Taille + Catégorie (SIM-03, Annexe A.1).
/// </summary>
public sealed class LigneTarifaire
{
    public Guid Id { get; private set; }
    public Guid VersionGrilleTarifaireId { get; private set; }
    public SensTrafic Sens { get; private set; }
    public TailleConteneur Taille { get; private set; }
    public string CategorieMarchandise { get; private set; } = string.Empty;
    public decimal Acconage { get; private set; }
    public decimal Relevage { get; private set; }
    public decimal TarifJourBranchement { get; private set; }

    private LigneTarifaire() { }

    public static LigneTarifaire Creer(
        Guid versionGrilleTarifaireId,
        SensTrafic sens,
        TailleConteneur taille,
        string categorieMarchandise,
        decimal acconage,
        decimal relevage,
        decimal tarifJourBranchement)
    {
        if (string.IsNullOrWhiteSpace(categorieMarchandise))
            throw new ArgumentException("La catégorie de marchandise est obligatoire.", nameof(categorieMarchandise));
        if (acconage < 0 || relevage < 0 || tarifJourBranchement < 0)
            throw new ArgumentException("Les montants tarifaires ne peuvent pas être négatifs.");

        return new LigneTarifaire
        {
            Id = Guid.NewGuid(),
            VersionGrilleTarifaireId = versionGrilleTarifaireId,
            Sens = sens,
            Taille = taille,
            CategorieMarchandise = categorieMarchandise.Trim(),
            Acconage = acconage,
            Relevage = relevage,
            TarifJourBranchement = tarifJourBranchement
        };
    }
}
