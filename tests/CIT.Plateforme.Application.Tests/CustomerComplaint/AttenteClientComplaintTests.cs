using System;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.CustomerComplaint;

public class AttenteClientComplaintTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IPeriodeAttenteClientRepository> _periodeRepo = new();
    private readonly Mock<IAutorisationService> _autorisation = new();
    private readonly Mock<IJournalisationService> _journalisation = new();
    private readonly Mock<IHorloge> _horloge = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();
    private readonly AttenteClientCommand _command;

    public AttenteClientComplaintTests()
    {
        _command = new AttenteClientCommand(
            _ticketRepo.Object,
            _periodeRepo.Object,
            _autorisation.Object,
            _journalisation.Object,
            _horloge.Object,
            _uniteDeTravail.Object);
            
        _horloge.Setup(h => h.MaintenantUtc).Returns(DateTime.UtcNow);
    }

    [Fact]
    public async Task Agent_avec_ComplaintInstruire_peut_mettre_en_attente()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var ticket = Ticket.Creer("REC-01", "CustomerComplaint", "Reclamation", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        ticket.PrendreEnCharge(agentId, DateTime.UtcNow);
        
        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
            
        _autorisation.Setup(a => a.ExigerAsync(agentId, Permission.ComplaintInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _command.MettreEnAttenteAsync(agentId, ticket.Id, "Besoin document", CancellationToken.None);

        // Assert
        Assert.Equal(StatutTicket.EnAttenteClient, ticket.Statut);
        
        _autorisation.Verify(a => a.ExigerAsync(agentId, Permission.ComplaintInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()), Times.Once);
        _autorisation.Verify(a => a.ExigerAsync(It.IsAny<Guid>(), Permission.TicketPrendreEnCharge, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Agent_sans_permission_ne_peut_pas_mettre_en_attente()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var ticket = Ticket.Creer("REC-01", "CustomerComplaint", "Reclamation", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        ticket.PrendreEnCharge(agentId, DateTime.UtcNow);
        
        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
            
        _autorisation.Setup(a => a.ExigerAsync(agentId, Permission.ComplaintInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AccesRefuseException("Refusé"));

        // Act & Assert
        await Assert.ThrowsAsync<AccesRefuseException>(() => _command.MettreEnAttenteAsync(agentId, ticket.Id, "Besoin document", CancellationToken.None));
    }
}
