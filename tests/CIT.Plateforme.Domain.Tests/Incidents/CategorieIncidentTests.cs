using CIT.Plateforme.Domain.Incidents;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Incidents;

public class CategorieIncidentTests
{
    [Fact]
    public void Creer_AvecDonneesValides_CreeCategorieActive()
    {
        var categorie = CategorieIncident.Creer("CONNEXION", "Connexion / Authentification", null);

        Assert.NotEqual(Guid.Empty, categorie.Id);
        Assert.Equal("CONNEXION", categorie.Code);
        Assert.Equal("Connexion / Authentification", categorie.Libelle);
        Assert.Null(categorie.FileCibleDefautId);
        Assert.True(categorie.EstActif);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Creer_CodeInvalide_LeveException(string? code)
    {
        Assert.Throws<ArgumentException>(() => CategorieIncident.Creer(code!, "Libelle", null));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Creer_LibelleInvalide_LeveException(string? libelle)
    {
        Assert.Throws<ArgumentException>(() => CategorieIncident.Creer("CODE", libelle!, null));
    }

    [Fact]
    public void Desactiver_puis_Activer_bascule_l_etat()
    {
        var categorie = CategorieIncident.Creer("CODE", "Libelle", null);

        categorie.Desactiver();
        Assert.False(categorie.EstActif);

        categorie.Activer();
        Assert.True(categorie.EstActif);
    }

    [Fact]
    public void MettreAJour_MetAJourLesProprietes()
    {
        var categorie = CategorieIncident.Creer("CODE", "Libelle", null);
        var nouvelleFileId = Guid.NewGuid();

        categorie.MettreAJour("Nouveau libellé", nouvelleFileId);

        Assert.Equal("Nouveau libellé", categorie.Libelle);
        Assert.Equal(nouvelleFileId, categorie.FileCibleDefautId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void MettreAJour_LibelleInvalide_LeveException(string? libelle)
    {
        var categorie = CategorieIncident.Creer("CODE", "Libelle", null);
        Assert.Throws<ArgumentException>(() => categorie.MettreAJour(libelle!, null));
    }
}
