using System;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.ActualisationBadt;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.ActualisationBadt;

public class RemettreEnFileBadtCommandTests
{
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IAutorisationService> _autorisation = new();
    private readonly Mock<IJournalisationService> _journalisation = new();
    private readonly Mock<IHorloge> _horloge = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();

    private readonly Ticket _ticket = Ticket.CreerDemandeActualisationBadt(
        "TCK-BADT-001", Guid.NewGuid(), Guid.NewGuid(), "BADT expiré.", new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc));

    private readonly RemettreEnFileBadtCommand _command;

    public RemettreEnFileBadtCommandTests()
    {
        _command = new RemettreEnFileBadtCommand(_tickets.Object, _autorisation.Object, _journalisation.Object, _horloge.Object, _uniteDeTravail.Object);
        _horloge.Setup(h => h.MaintenantUtc).Returns(new DateTime(2026, 10, 16, 9, 0, 0, DateTimeKind.Utc));
        _tickets.Setup(r => r.ObtenirParIdAsync(_ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_ticket);
    }

    [Fact]
    public async Task Remet_en_file_consigne_le_commentaire_et_notifie()
    {
        var agentId = Guid.NewGuid();
        _autorisation.Setup(a => a.EstAutoriseAsync(agentId, Permission.ActualisationBadtInstruire, _ticket.FileCouranteId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await _command.ExecuterAsync(agentId, _ticket.Id, "Dossier à moitié qualifié, reste le BL 2.", CancellationToken.None);

        Assert.Equal(StatutTicket.EnAttenteDePriseEnCharge, _ticket.Statut);
        Assert.Null(_ticket.OperateurAssigneId);
        _journalisation.Verify(j => j.ConsignerHistoriqueAsync(_ticket.Id, "TicketRemisEnFile", "Dossier à moitié qualifié, reste le BL 2.", agentId, It.IsAny<CancellationToken>()), Times.Once);
        _journalisation.Verify(j => j.PublierEvenementAsync("BadtTicketRemisEnFile", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Commentaire_vide_est_refuse()
    {
        var agentId = Guid.NewGuid();
        _autorisation.Setup(a => a.EstAutoriseAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _command.ExecuterAsync(agentId, _ticket.Id, "   ", CancellationToken.None));
        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Sans_permission_refuse()
    {
        _autorisation.Setup(a => a.EstAutoriseAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            _command.ExecuterAsync(Guid.NewGuid(), _ticket.Id, "Commentaire.", CancellationToken.None));
        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
