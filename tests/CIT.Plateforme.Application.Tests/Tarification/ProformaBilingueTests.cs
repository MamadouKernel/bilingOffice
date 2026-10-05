using System.Globalization;
using CIT.Plateforme.Application.Tarification;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Tarification;

/// <summary>Facture proforma (aperçu et PDF) dans la langue de l'utilisateur.</summary>
public sealed class ProformaBilingueTests
{
    [Theory]
    [InlineData(0, "zero CFA francs")]
    [InlineData(21, "twenty-one CFA francs")]
    [InlineData(1200, "one thousand two hundred CFA francs")]
    [InlineData(2_500_315, "two million five hundred thousand three hundred fifteen CFA francs")]
    public void Le_montant_en_lettres_anglais_est_correct(int montant, string attendu)
    {
        Assert.Equal(attendu, MontantFrancsCfa.EnLettresAnglais(montant));
    }

    [Fact]
    public void Les_libelles_suivent_la_langue_de_l_utilisateur()
    {
        Assert.Equal("Total Amount Payable", AvecCulture("en-US", () => TexteProforma.T("Montant Total à Payer")));
        Assert.Equal("Montant Total à Payer", AvecCulture("fr-FR", () => TexteProforma.T("Montant Total à Payer")));
        Assert.Equal("one thousand CFA francs", AvecCulture("en-US", () => TexteProforma.MontantEnLettres(1000)));
        Assert.Equal(MontantFrancsCfa.EnLettres(1000), AvecCulture("fr-FR", () => TexteProforma.MontantEnLettres(1000)));
    }

    private static string AvecCulture(string culture, Func<string> action)
    {
        var precedente = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
        try
        {
            return action();
        }
        finally
        {
            CultureInfo.CurrentUICulture = precedente;
        }
    }
}
