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

namespace CIT.Plateforme.Application.Tests.ActualisationBadt;

// FEATURE 010 — écart 1 : la messagerie du ticket doit marcher sur un ticket Actualisation BADT,
// gouvernée par la permission ActualisationBadtInstruire (et non TicketPrendreEnCharge).
public class PublierMessageBadtTests
{
    private readonly Mock<IMessageRepository> _messageRepo = new();
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IAutorisationService> _autorisation = new();
    private readonly CloisonnementService _cloisonnement;
    private readonly Mock<IJournalisationService> _journalisation = new();
    private readonly Mock<IHorloge> _horloge = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();
    private readonly PublierMessageCommand _command;

    private readonly Ticket _ticket = Ticket.CreerDemandeActualisationBadt(
        "TCK-BADT-001", Guid.NewGuid(), Guid.NewGuid(), "BADT expiré.", DateTime.UtcNow);

    public PublierMessageBadtTests()
    {
        _cloisonnement = new CloisonnementService(Mock.Of<ITicketRepository>(), CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.ControleAccesInerte());
        _command = new PublierMessageCommand(
            _messageRepo.Object, _ticketRepo.Object, _autorisation.Object, _cloisonnement,
            _journalisation.Object, _horloge.Object, _uniteDeTravail.Object);

        _horloge.Setup(h => h.MaintenantUtc).Returns(DateTime.UtcNow);
        _ticketRepo.Setup(r => r.ObtenirParIdAsync(_ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_ticket);
    }

    [Fact]
    public async Task Agent_publie_message_externe_avec_ActualisationBadtInstruire()
    {
        var agentId = Guid.NewGuid();
        _autorisation.Setup(a => a.ExigerAsync(agentId, Permission.ActualisationBadtInstruire, _ticket.FileCouranteId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var message = await _command.PublierMessageAgentAsync(agentId, _ticket.Id, "Pouvez-vous préciser le BL ?", CancellationToken.None);

        Assert.Equal(TypeMessage.Externe, message.Type);
        _autorisation.Verify(a => a.ExigerAsync(agentId, Permission.ActualisationBadtInstruire, _ticket.FileCouranteId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Agent_publie_note_interne_avec_ActualisationBadtInstruire()
    {
        var agentId = Guid.NewGuid();
        _autorisation.Setup(a => a.ExigerAsync(agentId, Permission.ActualisationBadtInstruire, _ticket.FileCouranteId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var message = await _command.PublierNoteInterneAsync(agentId, _ticket.Id, "À escalader.", CancellationToken.None);

        Assert.Equal(TypeMessage.NoteInterne, message.Type);
    }

    [Fact]
    public async Task Agent_sans_permission_est_refuse()
    {
        var agentId = Guid.NewGuid();
        _autorisation.Setup(a => a.ExigerAsync(agentId, Permission.ActualisationBadtInstruire, _ticket.FileCouranteId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AccesRefuseException("Refusé"));

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            _command.PublierMessageAgentAsync(agentId, _ticket.Id, "Message", CancellationToken.None));
    }
}
