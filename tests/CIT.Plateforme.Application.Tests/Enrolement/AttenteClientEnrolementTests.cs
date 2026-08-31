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

// FEATURE 008 (Enrôlement Transporteurs) — T035-T036. C8-04, mirroring AttenteClientComplaintTests.cs.
public class AttenteClientEnrolementTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IPeriodeAttenteClientRepository> _periodeRepo = new();
    private readonly Mock<IAutorisationService> _autorisation = new();
    private readonly Mock<IJournalisationService> _journalisation = new();
    private readonly Mock<IHorloge> _horloge = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();
    private readonly AttenteClientCommand _command;

    public AttenteClientEnrolementTests()
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
    public async Task Agent_avec_EnrolementInstruire_peut_mettre_en_attente()
    {
        var agentId = Guid.NewGuid();
        var ticket = Ticket.Creer("ENR-01", "Enrolement", "Enrolement", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        ticket.PrendreEnCharge(agentId, DateTime.UtcNow);

        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _autorisation.Setup(a => a.ExigerAsync(agentId, Permission.EnrolementInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _command.MettreEnAttenteAsync(agentId, ticket.Id, "Pièce manquante", CancellationToken.None);

        Assert.Equal(StatutTicket.EnAttenteClient, ticket.Statut);
        _autorisation.Verify(a => a.ExigerAsync(agentId, Permission.EnrolementInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()), Times.Once);
        _autorisation.Verify(a => a.ExigerAsync(It.IsAny<Guid>(), Permission.TicketPrendreEnCharge, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Agent_sans_permission_ne_peut_pas_mettre_en_attente()
    {
        var agentId = Guid.NewGuid();
        var ticket = Ticket.Creer("ENR-01", "Enrolement", "Enrolement", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        ticket.PrendreEnCharge(agentId, DateTime.UtcNow);

        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _autorisation.Setup(a => a.ExigerAsync(agentId, Permission.EnrolementInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AccesRefuseException("Refusé"));

        await Assert.ThrowsAsync<AccesRefuseException>(() => _command.MettreEnAttenteAsync(agentId, ticket.Id, "Pièce manquante", CancellationToken.None));
    }

    [Fact]
    public async Task RependreApresReponseAsync_generique_fonctionne_deja_sans_modification_sur_ticket_Enrolement()
    {
        var ticket = Ticket.Creer("ENR-01", "Enrolement", "Enrolement", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        ticket.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);
        ticket.MettreEnAttenteClient(DateTime.UtcNow);

        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);

        await _command.RependreApresReponseAsync(ticket.Id, CancellationToken.None);

        Assert.Equal(StatutTicket.EnCours, ticket.Statut);
    }
}
