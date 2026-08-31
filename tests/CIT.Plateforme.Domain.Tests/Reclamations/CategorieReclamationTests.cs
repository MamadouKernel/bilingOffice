using CIT.Plateforme.Domain.Reclamations;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Reclamations;

public class CategorieReclamationTests
{
    [Fact]
    public void Creer_AvecDonneesValides_CreeCategorieActive()
    {
        // Arrange & Act
        var categorie = CategorieReclamation.Creer("CODE_01", "Erreur facturation", null);

        // Assert
        Assert.NotEqual(Guid.Empty, categorie.Id);
        Assert.Equal("CODE_01", categorie.Code);
        Assert.Equal("Erreur facturation", categorie.Libelle);
        Assert.Null(categorie.FileCibleDefautId);
        Assert.True(categorie.EstActif);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Creer_CodeInvalide_LeveException(string? code)
    {
        // Assert
        Assert.Throws<ArgumentException>(() => CategorieReclamation.Creer(code!, "Libelle", null));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Creer_LibelleInvalide_LeveException(string? libelle)
    {
        // Assert
        Assert.Throws<ArgumentException>(() => CategorieReclamation.Creer("CODE", libelle!, null));
    }

    [Fact]
    public void Desactiver_DesactiveLaCategorie()
    {
        // Arrange
        var categorie = CategorieReclamation.Creer("CODE", "Libelle", null);

        // Act
        categorie.Desactiver();

        // Assert
        Assert.False(categorie.EstActif);
    }

    [Fact]
    public void Activer_ActiveLaCategorie()
    {
        // Arrange
        var categorie = CategorieReclamation.Creer("CODE", "Libelle", null);
        categorie.Desactiver();

        // Act
        categorie.Activer();

        // Assert
        Assert.True(categorie.EstActif);
    }

    [Fact]
    public void MettreAJour_MetAJourLesProprietes()
    {
        // Arrange
        var categorie = CategorieReclamation.Creer("CODE", "Libelle", null);
        var nouvelleFileId = Guid.NewGuid();

        // Act
        categorie.MettreAJour("Nouveau Libellé", nouvelleFileId);

        // Assert
        Assert.Equal("Nouveau Libellé", categorie.Libelle);
        Assert.Equal(nouvelleFileId, categorie.FileCibleDefautId);
    }
}
