using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Enrolement;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Enrolement;

public sealed class EnrCert05RenouvellementTests
{
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IDossierRepository> _dossiers = new();
    private readonly Mock<IAutorisationService> _autorisation = new();
    private readonly Mock<IJournalisationService> _journalisation = new();
    private readonly Mock<IParametresEnrolementProvider> _parametres = new();
    private readonly Mock<IHorloge> _horloge = new();
    private readonly Mock<IUniteDeTravail> _uow = new();
    private readonly DateTime _maintenant = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

    public EnrCert05RenouvellementTests()
    {
        _horloge.Setup(h => h.MaintenantUtc).Returns(_maintenant);
    }

    [Fact]
    public async Task Rappeler_BadgeProcheEcheance_EmetEvenementF009()
    {
        // Arrange : Badge délivré il y a 340 jours, configuré pour 365 jours de validité avec 30 jours de préavis
        _parametres.Setup(p => p.ObtenirDureeValiditeBadgeJours()).Returns(365);
        _parametres.Setup(p => p.ObtenirDelaiRappelExpirationJours()).Returns(30);

        var clientId = Guid.NewGuid();
        var dossier = Dossier.Creer("DOS-ENR-01", clientId, _maintenant.AddDays(-340), null, TypeClient.Normal);
        var dossierId = dossier.Id;

        var ticket = Ticket.Creer(
            "TCK-ENR-01",
            "Enrolement",
            "EnrolementTransporteur",
            Guid.NewGuid(),
            dossierId,
            _maintenant.AddDays(-340));

        ticket.Cloturer(MotifClotureType.Normale, _maintenant.AddDays(-340));

        _dossiers.Setup(d => d.ListerTousAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([dossier]);
        _tickets.Setup(t => t.ListerParDossierAsync(dossierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([ticket]);

        var cmd = new RappelerExpirationBadgesCommand(_dossiers.Object, _tickets.Object, _journalisation.Object, _horloge.Object, _parametres.Object, _uow.Object);

        // Act
        var nbRappels = await cmd.ExecuterAsync();

        // Assert
        Assert.Equal(1, nbRappels);
        _journalisation.Verify(j => j.PublierEvenementAsync("RappelExpirationBadge", It.Is<string>(p => p.Contains(dossierId.ToString())), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Rappeler_ConfigurationAbsente_LeveExceptionExplicite()
    {
        // Arrange
        _parametres.Setup(p => p.ObtenirDureeValiditeBadgeJours()).Returns((int?)null);
        _parametres.Setup(p => p.ObtenirDelaiRappelExpirationJours()).Returns((int?)null);

        var cmd = new RappelerExpirationBadgesCommand(_dossiers.Object, _tickets.Object, _journalisation.Object, _horloge.Object, _parametres.Object, _uow.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => cmd.ExecuterAsync());
    }

    [Fact]
    public async Task Renouveler_DossierExistant_CreeNouveauTicketDansMemeDossier()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var dossier = Dossier.Creer("DOS-ENR-02", clientId, _maintenant.AddYears(-1), null, TypeClient.Normal);
        var dossierId = dossier.Id;

        _dossiers.Setup(d => d.ObtenirParIdAsync(dossierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dossier);

        var numService = new Mock<IServiceNumerotation>();
        numService.Setup(n => n.ProchainNumeroTicketAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("TCK-ENR-RENOUV-01");

        var cmd = new DemanderRenouvellementBadgeCommand(
            _dossiers.Object,
            _tickets.Object,
            _autorisation.Object,
            _journalisation.Object,
            numService.Object,
            _horloge.Object,
            _uow.Object);

        // Act
        var nouveauTicket = await cmd.ExecuterAsync(clientId, dossierId, fileId);

        // Assert
        Assert.NotNull(nouveauTicket);
        Assert.Equal("TCK-ENR-RENOUV-01", nouveauTicket.Numero);
        Assert.Equal("RenouvellementBadge", nouveauTicket.TypeDemande);
        Assert.Equal(dossierId, nouveauTicket.DossierId);
        _tickets.Verify(t => t.AjouterAsync(It.Is<Ticket>(t => t.DossierId == dossierId && t.TypeDemande == "RenouvellementBadge"), It.IsAny<CancellationToken>()), Times.Once);
        _journalisation.Verify(j => j.ConsignerAuditAsync(clientId, "DemandeRenouvellementBadgeInitiee", nameof(Ticket), It.IsAny<Guid>(), null, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
