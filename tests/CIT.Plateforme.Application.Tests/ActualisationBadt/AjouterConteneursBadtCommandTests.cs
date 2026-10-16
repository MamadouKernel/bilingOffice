using System;
using System.Collections.Generic;
using System.Linq;
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

// FEATURE 010 — écart 2 : l'agent déplie un BL en ajoutant des conteneurs (saisie manuelle, pas de Navis).
public class AjouterConteneursBadtCommandTests
{
    private readonly Mock<ILigneActualisationBadtRepository> _lignes = new();
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IAutorisationService> _autorisation = new();
    private readonly Mock<IJournalisationService> _journalisation = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();
    private readonly AjouterConteneursBadtCommand _command;

    private readonly Ticket _ticket = Ticket.CreerDemandeActualisationBadt(
        "TCK-BADT-001", Guid.NewGuid(), Guid.NewGuid(), "BADT expiré.", DateTime.UtcNow);

    public AjouterConteneursBadtCommandTests()
    {
        _command = new AjouterConteneursBadtCommand(_lignes.Object, _tickets.Object, _autorisation.Object, _journalisation.Object, _uniteDeTravail.Object);
        _tickets.Setup(r => r.ObtenirParIdAsync(_ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_ticket);
        _autorisation.Setup(a => a.EstAutoriseAsync(It.IsAny<Guid>(), Permission.ActualisationBadtInstruire, _ticket.FileCouranteId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    [Fact]
    public async Task Premier_conteneur_comble_la_ligne_BL_seul_les_suivants_creent_des_lignes()
    {
        var placeholder = LigneActualisationBadt.Creer(_ticket.Id, "BL123");
        _lignes.Setup(r => r.ListerParTicketAsync(_ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LigneActualisationBadt> { placeholder });
        var nouvelles = new List<LigneActualisationBadt>();
        _lignes.Setup(r => r.AjouterAsync(It.IsAny<LigneActualisationBadt>(), It.IsAny<CancellationToken>()))
            .Callback<LigneActualisationBadt, CancellationToken>((l, _) => nouvelles.Add(l)).Returns(Task.CompletedTask);

        await _command.ExecuterAsync(Guid.NewGuid(), _ticket.Id, "BL123", new[] { "TCLU1111111", "TCLU2222222" }, CancellationToken.None);

        Assert.Equal("TCLU1111111", placeholder.NumeroConteneur);
        Assert.Single(nouvelles);
        Assert.Equal("TCLU2222222", nouvelles[0].NumeroConteneur);
        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Conteneur_deja_present_est_ignore()
    {
        var existante = LigneActualisationBadt.Creer(_ticket.Id, "BL123", "TCLU1111111");
        _lignes.Setup(r => r.ListerParTicketAsync(_ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LigneActualisationBadt> { existante });

        await _command.ExecuterAsync(Guid.NewGuid(), _ticket.Id, "BL123", new[] { "TCLU1111111" }, CancellationToken.None);

        _lignes.Verify(r => r.AjouterAsync(It.IsAny<LigneActualisationBadt>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Sans_permission_refuse()
    {
        _autorisation.Setup(a => a.EstAutoriseAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            _command.ExecuterAsync(Guid.NewGuid(), _ticket.Id, "BL123", new[] { "TCLU1111111" }, CancellationToken.None));
    }
}
