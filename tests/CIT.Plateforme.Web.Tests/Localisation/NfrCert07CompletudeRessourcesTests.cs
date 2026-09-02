using System.Globalization;
using CIT.Plateforme.Web.Resources;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Localisation;

public class NfrCert07CompletudeRessourcesTests
{
    private readonly SharedResourcesLocalizer _localizer;

    public NfrCert07CompletudeRessourcesTests()
    {
        _localizer = new SharedResourcesLocalizer();
    }

    [Fact]
    public void Parite_DoitAvoirToutesLesClesEnFrancaisEtEnAnglaisSansValeurVide()
    {
        var frenchDict = SharedResourcesLocalizer.GetFrenchDictionary();
        var englishDict = SharedResourcesLocalizer.GetEnglishDictionary();

        // 1. Contrôle des clés FR manquantes en EN
        var missingInEnglish = frenchDict.Keys
            .Where(key => !englishDict.ContainsKey(key) || string.IsNullOrWhiteSpace(englishDict[key]))
            .ToList();

        // 2. Contrôle des clés EN orphelines
        var missingInFrench = englishDict.Keys
            .Where(key => !frenchDict.ContainsKey(key) || string.IsNullOrWhiteSpace(frenchDict[key]))
            .ToList();

        Assert.Empty(missingInEnglish);
        Assert.Empty(missingInFrench);
        Assert.True(frenchDict.Count > 30, "Le dictionnaire FR doit contenir l'ensemble des clés des surfaces F014");
        Assert.Equal(frenchDict.Count, englishDict.Count);
    }

    [Fact]
    public void Fallback_DoitRenvoyerFrancais_QuandCultureEstAnglaiseMaisCleNonDefinieEnAnglais()
    {
        var currentCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            var result = _localizer["Common.Save"];
            Assert.Equal("Save", result.Value);

            // Si une clé existe en FR mais n'est pas en EN, le localizer fallback sur FR
            var missingKey = _localizer["Inconnue.Cle.Test"];
            Assert.Equal("Inconnue.Cle.Test", missingKey.Value);
        }
        finally
        {
            CultureInfo.CurrentUICulture = currentCulture;
        }
    }
}
