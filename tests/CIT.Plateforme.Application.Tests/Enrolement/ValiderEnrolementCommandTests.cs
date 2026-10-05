using System;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Enrolement;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Enrolement;

// FEATURE 008 (Enrôlement Transporteurs) — T028. C8-05.
public class ValiderEnrolementCommandTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IAutorisationService> _autorisation = new();
    private readonly Mock<IJournalisationService> _journalisation = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();
    private readonly ValiderEnrolementCommand _command;

    public ValiderEnrolementCommandTests()
    {
        _command = new ValiderEnrolementCommand(
            _ticketRepo.Object,
            _autorisation.Object,
            _journalisation.Object,
            _uniteDeTravail.Object);
    }

    // « Qui a pris en charge, traite » : le ticket est tenu par l'agent qui agit dans ces tests.
    private static readonly Guid AgentEnCharge = Guid.NewGuid();

    private static Ticket TicketEnrolementEnCours()
    {
        var ticket = Ticket.Creer("ENR-01", "Enrolement", "Enrolement", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        ticket.PrendreEnCharge(AgentEnCharge, DateTime.UtcNow);
        return ticket;
    }

    [Fact]
    public async Task Agent_habilite_valide_le_dossier_statut_reste_EnCours()
    {
        var agentId = AgentEnCharge;
        var ticket = TicketEnrolementEnCours();

        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _autorisation.Setup(a => a.ExigerAsync(agentId, Permission.EnrolementInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _command.ExecuterAsync(agentId, ticket.Id, CancellationToken.None);

        Assert.Equal(StatutTicket.EnCours, ticket.Statut);
        _journalisation.Verify(j => j.ConsignerHistoriqueAsync(ticket.Id, "EnrolementValide", null, agentId, It.IsAny<CancellationToken>()), Times.Once);
        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Agent_sans_permission_est_refuse()
    {
        var agentId = AgentEnCharge;
        var ticket = TicketEnrolementEnCours();

        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _autorisation.Setup(a => a.ExigerAsync(agentId, Permission.EnrolementInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AccesRefuseException("Refusé"));

        await Assert.ThrowsAsync<AccesRefuseException>(() => _command.ExecuterAsync(agentId, ticket.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Ticket_module_autre_que_Enrolement_leve_InvalidOperationException()
    {
        var agentId = AgentEnCharge;
        var ticket = Ticket.Creer("REC-01", "CustomerComplaint", "Reclamation", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _command.ExecuterAsync(agentId, ticket.Id, CancellationToken.None));

        _autorisation.Verify(a => a.ExigerAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
