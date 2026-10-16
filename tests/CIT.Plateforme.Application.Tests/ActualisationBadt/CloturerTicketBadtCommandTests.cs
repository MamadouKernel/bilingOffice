using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.ActualisationBadt;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.ActualisationBadt;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.ActualisationBadt;

public class CloturerTicketBadtCommandTests
{
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<ILigneActualisationBadtRepository> _lignes = new();
    private readonly Mock<IAutorisationService> _autorisation = new();
    private readonly Mock<IJournalisationService> _journalisation = new();
    private readonly Mock<IHorloge> _horloge = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();

    private readonly Ticket _ticket = Ticket.CreerDemandeActualisationBadt(
        "TCK-BADT-001", Guid.NewGuid(), Guid.NewGuid(), "BADT expiré.", new DateTime(2026, 10, 16, 10, 0, 0, DateTimeKind.Utc));

    private readonly CloturerTicketBadtCommand _command;

    public CloturerTicketBadtCommandTests()
    {
        _command = new CloturerTicketBadtCommand(_tickets.Object, _lignes.Object, _autorisation.Object, _journalisation.Object, _horloge.Object, _uniteDeTravail.Object);
        _horloge.Setup(h => h.MaintenantUtc).Returns(new DateTime(2026, 10, 16, 10, 0, 0, DateTimeKind.Utc));
        _tickets.Setup(r => r.ObtenirParIdAsync(_ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_ticket);
        _autorisation.Setup(a => a.EstAutoriseAsync(It.IsAny<Guid>(), Permission.ActualisationBadtInstruire, It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    private LigneActualisationBadt Ligne() => LigneActualisationBadt.Creer(_ticket.Id, "BL1", "TCLU1");

    [Fact]
    public async Task Cloture_quand_toutes_les_lignes_sont_statuees()
    {
        var ligne = Ligne();
        ligne.Refuser("Hors délai.");
        _lignes.Setup(r => r.ListerParTicketAsync(_ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync([ligne]);

        await _command.ExecuterAsync(Guid.NewGuid(), _ticket.Id, CancellationToken.None);

        Assert.Equal(StatutTicket.Cloture, _ticket.Statut);
        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Refuse_la_cloture_si_une_ligne_reste_en_attente()
    {
        var statuee = Ligne();
        statuee.Refuser("x");
        var enAttente = Ligne();
        _lignes.Setup(r => r.ListerParTicketAsync(_ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync([statuee, enAttente]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _command.ExecuterAsync(Guid.NewGuid(), _ticket.Id, CancellationToken.None));

        Assert.NotEqual(StatutTicket.Cloture, _ticket.Statut);
        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Refuse_sans_permission()
    {
        _autorisation.Setup(a => a.EstAutoriseAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await Assert.ThrowsAsync<AccesRefuseException>(() => _command.ExecuterAsync(Guid.NewGuid(), _ticket.Id, CancellationToken.None));
    }
}
