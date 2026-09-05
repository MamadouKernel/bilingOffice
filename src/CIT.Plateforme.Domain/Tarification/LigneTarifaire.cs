using CIT.Plateforme.Domain.Tarification.Referentiels;

namespace CIT.Plateforme.Domain.Tarification;

/// <summary>
/// Ligne tarifaire de base, identifiée par le triplet Sens + Taille + Catégorie de marchandise
/// (SIM-03, Annexe A.1).
///
/// PORTAIT AUPARAVANT ces trois composantes en clair : deux énumérations et une chaîne libre.
/// L'analyse Merise § 3.3 (correction n° 1) les remplace par des clés étrangères vers les
/// référentiels <see cref="SensOperation"/>, <see cref="TailleConteneur"/> et
/// <see cref="CategorieMarchandise"/>. La ligne ne porte donc plus que ses montants et ses
/// rattachements — c'est ce que la 3FN demande.
/// </summary>
public sealed class LigneTarifaire
{
    public Guid Id { get; private set; }

    public Guid VersionGrilleTarifaireId { get; private set; }

    public Guid SensOperationId { get; private set; }

    public Guid TailleConteneurId { get; private set; }

    public Guid CategorieMarchandiseId { get; private set; }

    public decimal Acconage { get; private set; }

    public decimal Relevage { get; private set; }

    /// <summary>Tarif journalier de branchement frigorifique, appliqué aux seuls types de
    /// conteneur marqués <see cref="TypeConteneur.DeclencheBranchementFrigorifique"/>.</summary>
    public decimal TarifJourBranchement { get; private set; }

    private LigneTarifaire() { }

    public static LigneTarifaire Creer(
        Guid versionGrilleTarifaireId,
        Guid sensOperationId,
        Guid tailleConteneurId,
        Guid categorieMarchandiseId,
        decimal acconage,
        decimal relevage,
        decimal tarifJourBranchement)
    {
        if (sensOperationId == Guid.Empty)
            throw new ArgumentException("Le sens d'opération est obligatoire.", nameof(sensOperationId));
        if (tailleConteneurId == Guid.Empty)
            throw new ArgumentException("La taille de conteneur est obligatoire.", nameof(tailleConteneurId));
        if (categorieMarchandiseId == Guid.Empty)
            throw new ArgumentException("La catégorie de marchandise est obligatoire.", nameof(categorieMarchandiseId));
        if (acconage < 0 || relevage < 0 || tarifJourBranchement < 0)
            throw new ArgumentException("Les montants tarifaires ne peuvent pas être négatifs.");

        return new LigneTarifaire
        {
            Id = Guid.NewGuid(),
            VersionGrilleTarifaireId = versionGrilleTarifaireId,
            SensOperationId = sensOperationId,
            TailleConteneurId = tailleConteneurId,
            CategorieMarchandiseId = categorieMarchandiseId,
            Acconage = acconage,
            Relevage = relevage,
            TarifJourBranchement = tarifJourBranchement,
        };
    }
}
