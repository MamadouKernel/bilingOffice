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

public class FileSupportTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IAutorisationService> _autorisation = new();
    private readonly ConsulterFileQuery _query;
    private readonly PrendreEnChargeCommand _command;
    private readonly Mock<IJournalisationService> _journalisation = new();
    private readonly Mock<IHorloge> _horloge = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();

    public FileSupportTests()
    {
        _query = new ConsulterFileQuery(_ticketRepo.Object, _autorisation.Object);
        _command = new PrendreEnChargeCommand(
            _ticketRepo.Object, 
            _autorisation.Object, 
            _journalisation.Object, 
            _horloge.Object, 
            _uniteDeTravail.Object);
            
        _horloge.Setup(h => h.MaintenantUtc).Returns(DateTime.UtcNow);
    }

    [Fact]
    public async Task Agent_avec_ComplaintInstruire_peut_voir_la_file()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        
        _autorisation.Setup(a => a.EstAutoriseAsync(agentId, Permission.TicketPrendreEnCharge, fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _autorisation.Setup(a => a.EstAutoriseAsync(agentId, Permission.ComplaintInstruire, fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
            
        _ticketRepo.Setup(r => r.ListerParFileAsync(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Ticket.Creer("REC-01", "CustomerComplaint", "Reclamation", fileId, Guid.NewGuid(), DateTime.UtcNow) });

        // Act
        var result = await _query.ExecuterAsync(agentId, fileId, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task Agent_sans_permission_ne_peut_pas_voir_la_file()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        
        _autorisation.Setup(a => a.EstAutoriseAsync(agentId, Permission.TicketPrendreEnCharge, fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _autorisation.Setup(a => a.EstAutoriseAsync(agentId, Permission.ComplaintInstruire, fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<AccesRefuseException>(() => _query.ExecuterAsync(agentId, fileId, null, CancellationToken.None));
    }
    
    [Fact]
    public async Task PrendreEnChargeCommand_exige_ComplaintInstruire_pour_TicketComplaint()
    {
        // Arrange
        var operateurId = Guid.NewGuid();
        var ticket = Ticket.Creer("REC-01", "CustomerComplaint", "Reclamation", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        
        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
            
        _autorisation.Setup(a => a.ExigerAsync(operateurId, Permission.ComplaintInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _command.ExecuterAsync(operateurId, ticket.Id, CancellationToken.None);

        // Assert
        Assert.Equal(StatutTicket.EnCours, ticket.Statut);
        Assert.Equal(operateurId, ticket.OperateurAssigneId);
        
        _autorisation.Verify(a => a.ExigerAsync(operateurId, Permission.ComplaintInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()), Times.Once);
        _autorisation.Verify(a => a.ExigerAsync(It.IsAny<Guid>(), Permission.TicketPrendreEnCharge, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
