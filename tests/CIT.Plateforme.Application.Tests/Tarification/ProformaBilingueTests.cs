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

    /// <summary>
    /// NON-RÉGRESSION 05/10/2026 — proforma restée en français sous une interface anglaise en prod :
    /// l'interface testait le NOM de culture (« en… »), la proforma <c>TwoLetterISOLanguageName</c>,
    /// non fiable selon les données de culture du serveur. Une seule règle : LangueCourante.EnAnglais.
    /// </summary>
    [Fact]
    public void Aucune_detection_de_langue_ne_contourne_LangueCourante()
    {
        var racine = new DirectoryInfo(AppContext.BaseDirectory);
        while (racine is not null && !Directory.Exists(Path.Combine(racine.FullName, "src")))
        {
            racine = racine.Parent;
        }

        Assert.NotNull(racine);
        var fautifs = Directory.EnumerateFiles(Path.Combine(racine!.FullName, "src"), "*.*", SearchOption.AllDirectories)
            .Where(f => (f.EndsWith(".cs", StringComparison.Ordinal) || f.EndsWith(".razor", StringComparison.Ordinal))
                        && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.EndsWith("LangueCourante.cs", StringComparison.Ordinal)
                        && File.ReadAllText(f).Contains("TwoLetterISOLanguageName", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToList();

        Assert.Empty(fautifs);
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
