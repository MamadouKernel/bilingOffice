using System;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Messagerie;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Messagerie;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Enrolement;

// FEATURE 008 (Enrôlement Transporteurs) — T027. C8-03, mirroring PublierMessageComplaintTests.cs.
public class PublierMessageEnrolementTests
{
    private readonly Mock<IMessageRepository> _messageRepo = new();
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IAutorisationService> _autorisation = new();
    private readonly CloisonnementService _cloisonnement;
    private readonly Mock<IJournalisationService> _journalisation = new();
    private readonly Mock<IHorloge> _horloge = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();
    private readonly PublierMessageCommand _command;

    public PublierMessageEnrolementTests()
    {
        _cloisonnement = new CloisonnementService(Mock.Of<ITicketRepository>(), CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.ControleAccesInerte());
        _command = new PublierMessageCommand(
            _messageRepo.Object,
            _ticketRepo.Object,
            _autorisation.Object,
            _cloisonnement,
            _journalisation.Object,
            _horloge.Object,
            _uniteDeTravail.Object);

        _horloge.Setup(h => h.MaintenantUtc).Returns(DateTime.UtcNow);
    }

    [Fact]
    public async Task Agent_peut_publier_note_interne_avec_EnrolementInstruire()
    {
        var agentId = Guid.NewGuid();
        var ticket = Ticket.Creer("ENR-01", "Enrolement", "Enrolement", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _autorisation.Setup(a => a.ExigerAsync(agentId, Permission.EnrolementInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var message = await _command.PublierNoteInterneAsync(agentId, ticket.Id, "Note interne", CancellationToken.None);

        Assert.Equal(TypeMessage.NoteInterne, message.Type);
        _autorisation.Verify(a => a.ExigerAsync(agentId, Permission.EnrolementInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Agent_peut_publier_message_externe_avec_EnrolementInstruire()
    {
        var agentId = Guid.NewGuid();
        var ticket = Ticket.Creer("ENR-01", "Enrolement", "Enrolement", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _autorisation.Setup(a => a.ExigerAsync(agentId, Permission.EnrolementInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var message = await _command.PublierMessageAgentAsync(agentId, ticket.Id, "Message chauffeur", CancellationToken.None);

        Assert.Equal(TypeMessage.Externe, message.Type);
        _autorisation.Verify(a => a.ExigerAsync(agentId, Permission.EnrolementInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Agent_sans_EnrolementInstruire_est_refuse()
    {
        var agentId = Guid.NewGuid();
        var ticket = Ticket.Creer("ENR-01", "Enrolement", "Enrolement", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _autorisation.Setup(a => a.ExigerAsync(agentId, Permission.EnrolementInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AccesRefuseException("Refusé"));

        await Assert.ThrowsAsync<AccesRefuseException>(() => _command.PublierMessageAgentAsync(agentId, ticket.Id, "Message", CancellationToken.None));
    }
}
