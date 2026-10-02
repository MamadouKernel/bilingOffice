using CIT.Plateforme.Application.Tarification;

namespace CIT.Plateforme.Application.Tests.Tarification;

public sealed class MontantEnLettresTests
{
    [Theory]
    [InlineData(0, "zéro franc CFA")]
    [InlineData(100, "cent francs CFA")]
    [InlineData(1000, "mille francs CFA")]
    [InlineData(21, "vingt-et-un francs CFA")]
    [InlineData(71, "soixante-onze francs CFA")]
    [InlineData(80, "quatre-vingts francs CFA")]
    // Cas de recette Annexe A — test06 (Import Local 40" Reefer C3) : total figé 2 245 300.
    [InlineData(2245300, "deux millions deux cent quarante-cinq mille trois cents francs CFA")]
    public void EnLettres_produit_le_libelle_attendu(long montant, string attendu)
    {
        Assert.Equal(attendu, MontantFrancsCfa.EnLettres(montant));
    }
}
