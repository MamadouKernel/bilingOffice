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

namespace CIT.Plateforme.Application.Tests.Enrolement;

// FEATURE 008 (Enrôlement Transporteurs) — T025-T026. C8-10 (ConsulterFileQuery) et C8-02
// (PrendreEnChargeCommand), mirroring FileSupportTests.cs (CustomerComplaint).
public class FileAgentGateTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IAutorisationService> _autorisation = new();
    private readonly ConsulterFileQuery _query;
    private readonly PrendreEnChargeCommand _command;
    private readonly Mock<IJournalisationService> _journalisation = new();
    private readonly Mock<IHorloge> _horloge = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();

    public FileAgentGateTests()
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
    public async Task Agent_avec_EnrolementInstruire_seul_peut_voir_la_file_Agent_Gate()
    {
        var agentId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        // Ni TicketPrendreEnCharge ni ComplaintInstruire — uniquement EnrolementInstruire.
        _autorisation.Setup(a => a.EstAutoriseAsync(agentId, Permission.TicketPrendreEnCharge, fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _autorisation.Setup(a => a.EstAutoriseAsync(agentId, Permission.ComplaintInstruire, fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _autorisation.Setup(a => a.EstAutoriseAsync(agentId, Permission.EnrolementInstruire, fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _ticketRepo.Setup(r => r.ListerParFileAsync(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Ticket.Creer("ENR-01", "Enrolement", "Enrolement", fileId, Guid.NewGuid(), DateTime.UtcNow)]);

        var result = await _query.ExecuterAsync(agentId, fileId, null, CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task Agent_sans_aucune_des_3_permissions_ne_peut_pas_voir_la_file()
    {
        var agentId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        _autorisation.Setup(a => a.EstAutoriseAsync(agentId, Permission.TicketPrendreEnCharge, fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _autorisation.Setup(a => a.EstAutoriseAsync(agentId, Permission.ComplaintInstruire, fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _autorisation.Setup(a => a.EstAutoriseAsync(agentId, Permission.EnrolementInstruire, fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<AccesRefuseException>(() => _query.ExecuterAsync(agentId, fileId, null, CancellationToken.None));
    }

    [Fact]
    public async Task PrendreEnChargeCommand_exige_EnrolementInstruire_pour_TicketEnrolement()
    {
        var operateurId = Guid.NewGuid();
        var ticket = Ticket.Creer("ENR-01", "Enrolement", "Enrolement", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _autorisation.Setup(a => a.ExigerAsync(operateurId, Permission.EnrolementInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _command.ExecuterAsync(operateurId, ticket.Id, CancellationToken.None);

        Assert.Equal(StatutTicket.EnCours, ticket.Statut);
        Assert.Equal(operateurId, ticket.OperateurAssigneId);

        _autorisation.Verify(a => a.ExigerAsync(operateurId, Permission.EnrolementInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()), Times.Once);
        _autorisation.Verify(a => a.ExigerAsync(It.IsAny<Guid>(), Permission.TicketPrendreEnCharge, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        _autorisation.Verify(a => a.ExigerAsync(It.IsAny<Guid>(), Permission.ComplaintInstruire, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PrendreEnChargeCommand_agent_sans_EnrolementInstruire_est_refuse()
    {
        var operateurId = Guid.NewGuid();
        var ticket = Ticket.Creer("ENR-01", "Enrolement", "Enrolement", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _autorisation.Setup(a => a.ExigerAsync(operateurId, Permission.EnrolementInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AccesRefuseException("Refusé"));

        await Assert.ThrowsAsync<AccesRefuseException>(() => _command.ExecuterAsync(operateurId, ticket.Id, CancellationToken.None));
    }
}
