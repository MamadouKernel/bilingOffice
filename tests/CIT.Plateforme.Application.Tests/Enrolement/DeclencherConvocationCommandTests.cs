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

// FEATURE 008 (Enrôlement Transporteurs) — T043. C8-07.
public class DeclencherConvocationCommandTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IAutorisationService> _autorisation = new();
    private readonly Mock<IJournalisationService> _journalisation = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();
    private readonly DeclencherConvocationCommand _command;

    public DeclencherConvocationCommandTests()
    {
        _command = new DeclencherConvocationCommand(
            _ticketRepo.Object,
            _autorisation.Object,
            _journalisation.Object,
            _uniteDeTravail.Object);
    }

    private static Ticket TicketEnrolementEnCours()
    {
        var ticket = Ticket.Creer("ENR-01", "Enrolement", "Enrolement", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        ticket.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);
        return ticket;
    }

    [Fact]
    public async Task Agent_habilite_declenche_la_convocation()
    {
        var agentId = Guid.NewGuid();
        var ticket = TicketEnrolementEnCours();

        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _autorisation.Setup(a => a.ExigerAsync(agentId, Permission.EnrolementInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        string? evenementPublie = null;
        _journalisation.Setup(j => j.PublierEvenementAsync("ConvocationDeclenchee", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, payload, _) => evenementPublie = payload)
            .Returns(Task.CompletedTask);

        await _command.ExecuterAsync(agentId, ticket.Id, CancellationToken.None);

        Assert.Equal(StatutTicket.EnCours, ticket.Statut);
        _journalisation.Verify(j => j.ConsignerHistoriqueAsync(ticket.Id, "ConvocationDeclenchee", null, agentId, It.IsAny<CancellationToken>()), Times.Once);

        // CLAR-ENR-06 : aucun canal SMS/WhatsApp/email dans le payload publié.
        Assert.NotNull(evenementPublie);
        Assert.DoesNotContain("sms", evenementPublie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("whatsapp", evenementPublie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("email", evenementPublie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Agent_sans_permission_est_refuse()
    {
        var agentId = Guid.NewGuid();
        var ticket = TicketEnrolementEnCours();

        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _autorisation.Setup(a => a.ExigerAsync(agentId, Permission.EnrolementInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AccesRefuseException("Refusé"));

        await Assert.ThrowsAsync<AccesRefuseException>(() => _command.ExecuterAsync(agentId, ticket.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Ticket_module_autre_que_Enrolement_leve_InvalidOperationException()
    {
        var agentId = Guid.NewGuid();
        var ticket = Ticket.Creer("REC-01", "CustomerComplaint", "Reclamation", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _command.ExecuterAsync(agentId, ticket.Id, CancellationToken.None));
    }
}
