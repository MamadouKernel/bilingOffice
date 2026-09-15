using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Formulaires;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Formulaires;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Formulaires;

/// <summary>[TEST-SPEC] Exigence CDC : ERR-01 — sauvegarde et reprise des brouillons de saisie
/// (aucune perte de données en cas d'interruption). (Traçabilité §6 docs/88.)</summary>
public sealed class ErrCert01BrouillonsTests
{
    private readonly Mock<IBrouillonRepository> _brouillons = new();
    private readonly Mock<IAutorisationService> _autorisation = new();
    private readonly Mock<IHorloge> _horloge = new();
    private readonly Mock<IUniteDeTravail> _uow = new();
    private readonly DateTime _maintenant = new(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);

    public ErrCert01BrouillonsTests()
    {
        _horloge.Setup(h => h.MaintenantUtc).Returns(_maintenant);
    }

    [Fact]
    public async Task Enregistrer_NouveauBrouillon_SauvegardeEtRetourneId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        _brouillons.Setup(r => r.ObtenirAsync(userId, "DemandeImport", It.IsAny<CancellationToken>()))
            .ReturnsAsync((BrouillonSaisie?)null);

        var handler = new EnregistrerBrouillonCommand(_brouillons.Object, _horloge.Object, _uow.Object);

        // Act
        var resultatId = await handler.ExecuterAsync(userId, societeId, "DemandeImport", "{\"bl\":\"BL123\"}");

        // Assert
        Assert.NotEqual(Guid.Empty, resultatId);
        _brouillons.Verify(r => r.AjouterAsync(It.Is<BrouillonSaisie>(b =>
            b.UtilisateurId == userId &&
            b.SocieteId == societeId &&
            b.TypeFormulaire == "DemandeImport" &&
            b.DonneesJson == "{\"bl\":\"BL123\"}"), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Enregistrer_BrouillonExistant_MetAJourAvecDerniereVersion_LastWriteWins()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        var existant = BrouillonSaisie.Creer(Guid.NewGuid(), userId, societeId, "DemandeImport", "{\"bl\":\"V1\"}", _maintenant.AddMinutes(-10));

        _brouillons.Setup(r => r.ObtenirAsync(userId, "DemandeImport", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existant);

        var handler = new EnregistrerBrouillonCommand(_brouillons.Object, _horloge.Object, _uow.Object);

        // Act
        var resultatId = await handler.ExecuterAsync(userId, societeId, "DemandeImport", "{\"bl\":\"V2\"}");

        // Assert
        Assert.Equal(existant.Id, resultatId);
        Assert.Equal("{\"bl\":\"V2\"}", existant.DonneesJson);
        Assert.Equal(_maintenant, existant.DateDerniereModificationUtc);
        _uow.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Obtenir_BrouillonExistant_RetourneDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        var existant = BrouillonSaisie.Creer(Guid.NewGuid(), userId, societeId, "DemandeExport", "{\"ref\":\"EXP-01\"}", _maintenant);

        _brouillons.Setup(r => r.ObtenirAsync(userId, "DemandeExport", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existant);

        var handler = new ObtenirBrouillonQuery(_brouillons.Object);

        // Act
        var dto = await handler.ExecuterAsync(userId, "DemandeExport");

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(existant.Id, dto.Id);
        Assert.Equal("DemandeExport", dto.TypeFormulaire);
        Assert.Equal("{\"ref\":\"EXP-01\"}", dto.DonneesJson);
    }

    [Fact]
    public async Task Obtenir_BrouillonInexistant_RetourneNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _brouillons.Setup(r => r.ObtenirAsync(userId, "Reclamation", It.IsAny<CancellationToken>()))
            .ReturnsAsync((BrouillonSaisie?)null);

        var handler = new ObtenirBrouillonQuery(_brouillons.Object);

        // Act
        var dto = await handler.ExecuterAsync(userId, "Reclamation");

        // Assert
        Assert.Null(dto);
    }

    [Fact]
    public async Task Supprimer_BrouillonExistant_SupprimeEtEnregistre()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var handler = new SupprimerBrouillonCommand(_brouillons.Object, _uow.Object);

        // Act
        await handler.ExecuterAsync(userId, "DemandeImport");

        // Assert
        _brouillons.Verify(r => r.SupprimerAsync(userId, "DemandeImport", It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Enregistrer_ParametresInvalides_LeveException()
    {
        // Arrange
        var handler = new EnregistrerBrouillonCommand(_brouillons.Object, _horloge.Object, _uow.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.ExecuterAsync(Guid.Empty, Guid.NewGuid(), "DemandeImport", "{}"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.ExecuterAsync(Guid.NewGuid(), Guid.Empty, "DemandeImport", "{}"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.ExecuterAsync(Guid.NewGuid(), Guid.NewGuid(), "", "{}"));
    }
}
