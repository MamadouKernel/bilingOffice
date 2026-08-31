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

// FEATURE 008 (Enrôlement Transporteurs) — T044. C8-08.
public class ConstaterRemiseBadgeCommandTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IAutorisationService> _autorisation = new();
    private readonly Mock<IJournalisationService> _journalisation = new();
    private readonly Mock<IHorloge> _horloge = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();
    private readonly ConstaterRemiseBadgeCommand _command;

    public ConstaterRemiseBadgeCommandTests()
    {
        _command = new ConstaterRemiseBadgeCommand(
            _ticketRepo.Object,
            _autorisation.Object,
            _journalisation.Object,
            _horloge.Object,
            _uniteDeTravail.Object);

        _horloge.Setup(h => h.MaintenantUtc).Returns(new DateTime(2026, 8, 31, 12, 0, 0, DateTimeKind.Utc));
    }

    private static Ticket TicketEnrolementEnCours()
    {
        var ticket = Ticket.Creer("ENR-01", "Enrolement", "Enrolement", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        ticket.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);
        return ticket;
    }

    [Fact]
    public async Task Agent_habilite_constate_la_remise_et_cloture_le_ticket()
    {
        var agentId = Guid.NewGuid();
        var ticket = TicketEnrolementEnCours();

        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _autorisation.Setup(a => a.ExigerAsync(agentId, Permission.EnrolementInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _command.ExecuterAsync(agentId, ticket.Id, CancellationToken.None);

        Assert.Equal(StatutTicket.Cloture, ticket.Statut);
        Assert.NotNull(ticket.DateClotureUtc);
        _journalisation.Verify(j => j.ConsignerHistoriqueAsync(ticket.Id, "BadgeRemisEtDossierCloture", null, agentId, It.IsAny<CancellationToken>()), Times.Once);
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

    [Fact]
    public async Task Double_remise_de_badge_leve_InvalidOperationException()
    {
        var agentId = Guid.NewGuid();
        var ticket = TicketEnrolementEnCours();
        ticket.Cloturer(MotifClotureType.Normale, DateTime.UtcNow);

        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _autorisation.Setup(a => a.ExigerAsync(agentId, Permission.EnrolementInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _command.ExecuterAsync(agentId, ticket.Id, CancellationToken.None));
    }

    [Fact]
    public void AucunePropriete_NumeroBadge_ou_Technologie_sur_Ticket()
    {
        // CLAR-ENR-05 : le badge reste exclusivement un événement d'historique — aucune propriété
        // "numéro de badge" ni "technologie" n'existe sur Ticket (vérification par réflexion,
        // garde-fou contre une invention future).
        var proprietes = typeof(Ticket).GetProperties();
        Assert.DoesNotContain(proprietes, p => p.Name.Contains("Badge", StringComparison.OrdinalIgnoreCase));
    }
}
