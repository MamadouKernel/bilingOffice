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
    private readonly Mock<IBadgeEnrolementRepository> _badges = new();
    private readonly ConstaterRemiseBadgeCommand _command;

    public ConstaterRemiseBadgeCommandTests()
    {
        _command = new ConstaterRemiseBadgeCommand(
            _ticketRepo.Object,
            _badges.Object,
            _autorisation.Object,
            _journalisation.Object,
            _horloge.Object,
            _uniteDeTravail.Object);

        _horloge.Setup(h => h.MaintenantUtc).Returns(new DateTime(2026, 8, 31, 12, 0, 0, DateTimeKind.Utc));
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
    public async Task Agent_habilite_constate_la_remise_et_cloture_le_ticket()
    {
        var agentId = AgentEnCharge;
        var ticket = TicketEnrolementEnCours();

        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _autorisation.Setup(a => a.ExigerAsync(agentId, Permission.EnrolementInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _command.ExecuterAsync(agentId, ticket.Id, "BADGE-0001", new DateTime(2027, 8, 31, 0, 0, 0, DateTimeKind.Utc), CancellationToken.None);

        Assert.Equal(StatutTicket.Cloture, ticket.Statut);
        Assert.NotNull(ticket.DateClotureUtc);
        _journalisation.Verify(j => j.ConsignerHistoriqueAsync(ticket.Id, "BadgeRemisEtDossierCloture",
            It.Is<string>(d => d.Contains("BADGE-0001") && d.Contains("31/08/2027")), agentId, It.IsAny<CancellationToken>()), Times.Once);
        _badges.Verify(b => b.AjouterAsync(It.Is<CIT.Plateforme.Domain.Enrolement.BadgeEnrolement>(x =>
            x.TicketId == ticket.Id && x.Numero == "BADGE-0001" && x.AgentId == agentId), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Décision du 03/10/2026 : sans numéro (ou avec une expiration passée), la remise est
    /// refusée et le dossier n'est PAS clôturé — jamais de dossier clos sans badge enregistré.</summary>
    [Theory]
    [InlineData("", 2027)]
    [InlineData("BADGE-0001", 2025)]
    public async Task Une_remise_sans_numero_ou_deja_expiree_est_refusee_sans_cloturer(string numero, int anneeExpiration)
    {
        var agentId = AgentEnCharge;
        var ticket = TicketEnrolementEnCours();
        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _autorisation.Setup(a => a.ExigerAsync(agentId, Permission.EnrolementInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _command.ExecuterAsync(agentId, ticket.Id, numero, new DateTime(anneeExpiration, 1, 1, 0, 0, 0, DateTimeKind.Utc), CancellationToken.None));

        Assert.NotEqual(StatutTicket.Cloture, ticket.Statut);
        _badges.Verify(b => b.AjouterAsync(It.IsAny<CIT.Plateforme.Domain.Enrolement.BadgeEnrolement>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Agent_sans_permission_est_refuse()
    {
        var agentId = AgentEnCharge;
        var ticket = TicketEnrolementEnCours();

        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _autorisation.Setup(a => a.ExigerAsync(agentId, Permission.EnrolementInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AccesRefuseException("Refusé"));

        await Assert.ThrowsAsync<AccesRefuseException>(() => _command.ExecuterAsync(agentId, ticket.Id, "BADGE-0001", new DateTime(2027, 8, 31, 0, 0, 0, DateTimeKind.Utc), CancellationToken.None));
    }

    [Fact]
    public async Task Ticket_module_autre_que_Enrolement_leve_InvalidOperationException()
    {
        var agentId = AgentEnCharge;
        var ticket = Ticket.Creer("REC-01", "CustomerComplaint", "Reclamation", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _command.ExecuterAsync(agentId, ticket.Id, "BADGE-0001", new DateTime(2027, 8, 31, 0, 0, 0, DateTimeKind.Utc), CancellationToken.None));
    }

    [Fact]
    public async Task Double_remise_de_badge_leve_InvalidOperationException()
    {
        var agentId = AgentEnCharge;
        var ticket = TicketEnrolementEnCours();
        ticket.Cloturer(MotifClotureType.Normale, DateTime.UtcNow);

        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _autorisation.Setup(a => a.ExigerAsync(agentId, Permission.EnrolementInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _command.ExecuterAsync(agentId, ticket.Id, "BADGE-0001", new DateTime(2027, 8, 31, 0, 0, 0, DateTimeKind.Utc), CancellationToken.None));
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
