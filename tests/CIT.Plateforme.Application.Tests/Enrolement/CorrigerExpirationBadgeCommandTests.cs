using System;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Enrolement;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Enrolement;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Enrolement;

/// <summary>Correction de la date d'expiration du badge (carte transporteur) saisie par erreur
/// (décision 07/10/2026) — sans rouvrir le dossier.</summary>
public class CorrigerExpirationBadgeCommandTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IBadgeEnrolementRepository> _badges = new();
    private readonly Mock<IAutorisationService> _autorisation = new();
    private readonly Mock<IJournalisationService> _journalisation = new();
    private readonly Mock<IHorloge> _horloge = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();
    private readonly CorrigerExpirationBadgeCommand _command;
    private static readonly DateTime Maintenant = new(2026, 8, 31, 12, 0, 0, DateTimeKind.Utc);

    public CorrigerExpirationBadgeCommandTests()
    {
        _command = new CorrigerExpirationBadgeCommand(
            _ticketRepo.Object, _badges.Object, _autorisation.Object,
            _journalisation.Object, _horloge.Object, _uniteDeTravail.Object);
        _horloge.Setup(h => h.MaintenantUtc).Returns(Maintenant);
    }

    private static readonly Guid Agent = Guid.NewGuid();

    private (Ticket ticket, BadgeEnrolement badge) TicketClotureAvecBadge()
    {
        var ticket = Ticket.Creer("ENR-01", "Enrolement", "Enrolement", Guid.NewGuid(), Guid.NewGuid(), Maintenant);
        ticket.PrendreEnCharge(Agent, Maintenant);
        ticket.Cloturer(MotifClotureType.Normale, Maintenant);
        var badge = BadgeEnrolement.Remettre(ticket.Id, "BADGE-0001", new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc), Agent, Maintenant);
        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _badges.Setup(b => b.ObtenirParTicketAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(badge);
        _autorisation.Setup(a => a.ExigerAsync(Agent, Permission.EnrolementInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return (ticket, badge);
    }

    [Fact]
    public async Task Corrige_l_expiration_et_journalise()
    {
        var (ticket, badge) = TicketClotureAvecBadge();
        var nouvelle = new DateTime(2028, 6, 30, 0, 0, 0, DateTimeKind.Utc);

        await _command.ExecuterAsync(Agent, ticket.Id, nouvelle, CancellationToken.None);

        Assert.Equal(nouvelle, badge.DateExpirationUtc);
        _journalisation.Verify(j => j.ConsignerHistoriqueAsync(ticket.Id, "ExpirationBadgeCorrigee",
            It.Is<string>(d => d.Contains("30/06/2028")), Agent, It.IsAny<CancellationToken>()), Times.Once);
        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Date_passee_refusee_sans_journaliser()
    {
        var (ticket, badge) = TicketClotureAvecBadge();
        var ancienne = badge.DateExpirationUtc;

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _command.ExecuterAsync(Agent, ticket.Id, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), CancellationToken.None));

        Assert.Equal(ancienne, badge.DateExpirationUtc);
        _journalisation.Verify(j => j.ConsignerHistoriqueAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Sans_badge_remis_leve_InvalidOperationException()
    {
        var ticket = Ticket.Creer("ENR-01", "Enrolement", "Enrolement", Guid.NewGuid(), Guid.NewGuid(), Maintenant);
        ticket.PrendreEnCharge(Agent, Maintenant);
        ticket.Cloturer(MotifClotureType.Normale, Maintenant);
        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _badges.Setup(b => b.ObtenirParTicketAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync((BadgeEnrolement?)null);
        _autorisation.Setup(a => a.ExigerAsync(Agent, Permission.EnrolementInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _command.ExecuterAsync(Agent, ticket.Id, new DateTime(2028, 1, 1, 0, 0, 0, DateTimeKind.Utc), CancellationToken.None));
    }
}
