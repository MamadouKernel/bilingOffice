using System;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Reclamations;
using CIT.Plateforme.Domain.Securite;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.CustomerComplaint;

public class ModifierCategoriesReclamationCommandTests
{
    private readonly Mock<ICategorieReclamationRepository> _repo = new();
    private readonly Mock<IAutorisationService> _autorisation = new();
    private readonly Mock<IJournalisationService> _journalisation = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();
    private readonly ModifierCategoriesReclamationCommand _command;

    public ModifierCategoriesReclamationCommandTests()
    {
        _command = new ModifierCategoriesReclamationCommand(_repo.Object, GardeFilesDeRecette(),
            _autorisation.Object,
            _journalisation.Object,
            _uniteDeTravail.Object);
    }

    [Fact]
    public async Task Admin_peut_modifier_categorie()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var catId = Guid.NewGuid();
        var categorie = CategorieReclamation.Creer("CAT1", "Cat 1", null);
        typeof(CategorieReclamation).GetProperty("Id")!.SetValue(categorie, catId);
        
        _repo.Setup(r => r.ObtenirParIdAsync(catId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(categorie);
            
        _autorisation.Setup(a => a.ExigerAsync(adminId, Permission.AdministrationCategoriesReclamation, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _command.ExecuterAsync(new ModifierCategoriesReclamationRequete(adminId, catId, "Nouveau libellé", Guid.NewGuid(), false), CancellationToken.None);

        // Assert
        Assert.Equal("Nouveau libellé", categorie.Libelle);
        Assert.False(categorie.EstActif);
        
        _autorisation.Verify(a => a.ExigerAsync(adminId, Permission.AdministrationCategoriesReclamation, null, It.IsAny<CancellationToken>()), Times.Once);
        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Non_Admin_ne_peut_pas_modifier()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        
        _autorisation.Setup(a => a.ExigerAsync(agentId, Permission.AdministrationCategoriesReclamation, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AccesRefuseException("Accès refusé"));

        // Act & Assert
        await Assert.ThrowsAsync<AccesRefuseException>(() => 
            _command.ExecuterAsync(new ModifierCategoriesReclamationRequete(agentId, Guid.NewGuid(), "Libelle", null, true), CancellationToken.None));
    }

    /// <summary>
    /// REMÉDIATION RECETTE UAT (D-14) : les deux commandes vérifient désormais que la file cible
    /// existe et relève du module des réclamations. Ce double renvoie une file conforme pour tout
    /// identifiant, afin que ces tests continuent d'éprouver ce qu'ils éprouvaient — la création et
    /// la modification — et non la nouvelle garde, couverte par ses propres tests.
    /// </summary>
    private static CIT.Plateforme.Application.CustomerComplaint.GardeFileCibleReclamation GardeFilesDeRecette()
    {
        var files = new Mock<IFileDeTraitementRepository>();
        files.Setup(f => f.ObtenirParIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CIT.Plateforme.Domain.Files.FileDeTraitement.Creer(
                CIT.Plateforme.Application.CustomerComplaint.SoumettreReclamationCommand.Module,
                "Support Opérationnel"));
        return new CIT.Plateforme.Application.CustomerComplaint.GardeFileCibleReclamation(files.Object);
    }
}
