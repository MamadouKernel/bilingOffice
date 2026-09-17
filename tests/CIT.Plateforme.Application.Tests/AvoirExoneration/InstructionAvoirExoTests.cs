using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.AvoirExoneration;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using Moq;

namespace CIT.Plateforme.Application.Tests.AvoirExoneration;

public sealed class InstructionAvoirExoTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IJournalisationService> _journalisation = new();
    private readonly Mock<IHorloge> _horloge = new();
    private readonly Mock<IAutorisationService> _autorisation = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();

    private readonly PrendreEnChargeAvoirExoCommand _prendreEnChargeCommand;
    private readonly DemanderComplementAvoirExoCommand _demanderComplementCommand;

    private readonly Guid _agentId = Guid.NewGuid();
    private readonly Guid _ticketId = Guid.NewGuid();
    private readonly DateTime _maintenantUtc = new(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);

    public InstructionAvoirExoTests()
    {
        _prendreEnChargeCommand = new PrendreEnChargeAvoirExoCommand(
            _ticketRepo.Object,
            _journalisation.Object,
            _horloge.Object,
            _autorisation.Object,
            _uniteDeTravail.Object);

        _demanderComplementCommand = new DemanderComplementAvoirExoCommand(
            _ticketRepo.Object,
            _journalisation.Object,
            _horloge.Object,
            _autorisation.Object,
            _uniteDeTravail.Object);

        _horloge.Setup(h => h.MaintenantUtc).Returns(_maintenantUtc);
    }

    [Fact]
    public async Task PrendreEnCharge_AgentHabilite_PasseAEnCours()
    {
        // Arrange
        var ticket = Ticket.CreerDemandeAvoirExo("AVO-001", "Avoir", Guid.NewGuid(), Guid.NewGuid(), "Obj", "Desc", 100m, _maintenantUtc);
        _ticketRepo.Setup(t => t.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
        // P3 : le contrôle porte désormais la FILE du ticket, et non plus `null` — qui ne
        // reconnaissait que les habilitations non bornées. Le montage décrit donc un agent
        // habilité sur la file de CE ticket : l'assertion est plus précise, pas plus permissive.
        _autorisation.Setup(a => a.EstAutoriseAsync(_agentId, Permission.AvoirInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _prendreEnChargeCommand.ExecuterAsync(_agentId, ticket.Id, CancellationToken.None);

        // Assert
        Assert.Equal(StatutTicket.EnCours, ticket.Statut);
        Assert.Equal(_agentId, ticket.OperateurAssigneId);
        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DemanderComplement_AgentHabilite_PasseAEnAttenteClientEtPublieEvenement()
    {
        // Arrange
        var ticket = Ticket.CreerDemandeAvoirExo("AVO-001", "Avoir", Guid.NewGuid(), Guid.NewGuid(), "Obj", "Desc", 100m, _maintenantUtc);
        ticket.PrendreEnCharge(_agentId, _maintenantUtc);

        _ticketRepo.Setup(t => t.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
        // P3 : le contrôle porte désormais la FILE du ticket, et non plus `null` — qui ne
        // reconnaissait que les habilitations non bornées. Le montage décrit donc un agent
        // habilité sur la file de CE ticket : l'assertion est plus précise, pas plus permissive.
        _autorisation.Setup(a => a.EstAutoriseAsync(_agentId, Permission.AvoirInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _demanderComplementCommand.ExecuterAsync(_agentId, ticket.Id, "Veuillez fournir le BL original", CancellationToken.None);

        // Assert
        Assert.Equal(StatutTicket.EnAttenteClient, ticket.Statut);
        _journalisation.Verify(j => j.PublierEvenementAsync("AvoirComplementDemande", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// AVE-02 (remédiation) : Avoir et Exonération suivent chacun leur propre workflow, avec leurs
    /// propres intervenants habilités. Un agent habilité uniquement pour l'Avoir ne doit pas pouvoir
    /// instruire une demande d'Exonération.
    /// </summary>
    [Fact]
    public async Task PrendreEnCharge_HabiliteAvoirSeulement_RefuseSurExoneration()
    {
        // Arrange
        var ticket = Ticket.CreerDemandeAvoirExo("EXO-001", "Exoneration", Guid.NewGuid(), Guid.NewGuid(), "Obj", "Desc", null, _maintenantUtc);
        _ticketRepo.Setup(t => t.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
        _autorisation.Setup(a => a.EstAutoriseAsync(_agentId, Permission.AvoirInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _autorisation.Setup(a => a.EstAutoriseAsync(_agentId, Permission.ExonerationInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<AccesRefuseException>(
            () => _prendreEnChargeCommand.ExecuterAsync(_agentId, ticket.Id, CancellationToken.None));
    }

    /// <summary>Symétrique : un agent habilité uniquement pour l'Exonération ne peut pas instruire un Avoir.</summary>
    [Fact]
    public async Task PrendreEnCharge_HabiliteExonerationSeulement_RefuseSurAvoir()
    {
        // Arrange
        var ticket = Ticket.CreerDemandeAvoirExo("AVO-002", "Avoir", Guid.NewGuid(), Guid.NewGuid(), "Obj", "Desc", 100m, _maintenantUtc);
        _ticketRepo.Setup(t => t.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
        _autorisation.Setup(a => a.EstAutoriseAsync(_agentId, Permission.ExonerationInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _autorisation.Setup(a => a.EstAutoriseAsync(_agentId, Permission.AvoirInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<AccesRefuseException>(
            () => _prendreEnChargeCommand.ExecuterAsync(_agentId, ticket.Id, CancellationToken.None));
    }
}
