namespace CIT.Plateforme.Domain.Tarification.Parametres;

/// <summary>
/// Quote-part de répartition du montant de stationnement entre bénéficiaires — Annexe A.5,
/// RG-SIM-09 : « par défaut CIT 30 %, OIC 20 %, PAA 50 % ».
///
/// ÉTAIT TROIS COLONNES <c>QuotePartCit</c> / <c>QuotePartOic</c> / <c>QuotePartPaa</c>, qui
/// nommaient les bénéficiaires dans le schéma. Ajouter ou retirer un bénéficiaire imposait une
/// migration ; RG-SIM-09 les déclare pourtant « paramétrables ».
///
/// La somme des quotes-parts d'une grille doit valoir 1 — invariant vérifié par l'agrégat
/// <c>VersionGrilleTarifaire</c>, seul à connaître l'ensemble des lignes.
/// </summary>
public sealed class QuotePartBeneficiaire
{
    public Guid Id { get; private set; }

    public Guid VersionGrilleTarifaireId { get; private set; }

    /// <summary>Code du bénéficiaire (ex. « CIT », « OIC », « PAA »).</summary>
    public string Beneficiaire { get; private set; } = string.Empty;

    /// <summary>Part exprimée en fraction (0,30 = 30 %).</summary>
    public decimal Pourcentage { get; private set; }

    private QuotePartBeneficiaire() { }

    public static QuotePartBeneficiaire Creer(Guid versionGrilleTarifaireId, string beneficiaire, decimal pourcentage)
    {
        if (string.IsNullOrWhiteSpace(beneficiaire))
            throw new ArgumentException("Le bénéficiaire est obligatoire.", nameof(beneficiaire));
        if (pourcentage < 0)
            throw new ArgumentException("Une quote-part ne peut pas être négative.", nameof(pourcentage));

        return new QuotePartBeneficiaire
        {
            Id = Guid.NewGuid(),
            VersionGrilleTarifaireId = versionGrilleTarifaireId,
            Beneficiaire = beneficiaire.Trim().ToUpperInvariant(),
            Pourcentage = pourcentage,
        };
    }
}
