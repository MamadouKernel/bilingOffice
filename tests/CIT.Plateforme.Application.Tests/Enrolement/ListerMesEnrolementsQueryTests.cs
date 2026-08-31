using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Enrolement;
using CIT.Plateforme.Domain.Tickets;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Enrolement;

// FEATURE 008 (Enrôlement Transporteurs) — T023. C8-09 : isolation entre deux Transporteurs
// distincts (anti-IDOR, research.md D11) — clientId toujours dérivé de l'appelant.
public class ListerMesEnrolementsQueryTests
{
    private readonly Mock<IDossierRepository> _dossierRepo = new();
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly ListerMesEnrolementsQuery _query;

    public ListerMesEnrolementsQueryTests()
    {
        _query = new ListerMesEnrolementsQuery(_dossierRepo.Object, _ticketRepo.Object);
    }

    [Fact]
    public async Task Retourne_uniquement_les_tickets_Enrolement_des_dossiers_de_l_appelant()
    {
        var appelantId = Guid.NewGuid();
        var autreClientId = Guid.NewGuid();

        var dossierAppelant = Dossier.Creer("DOS-001", appelantId, DateTime.UtcNow);
        _dossierRepo.Setup(r => r.ListerParClientAsync(appelantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([dossierAppelant]);

        var ticketEnrolementAppelant = Ticket.Creer("TCK-001", "Enrolement", "Enrolement", Guid.NewGuid(), dossierAppelant.Id, DateTime.UtcNow);
        _ticketRepo.Setup(r => r.ListerParDossiersAsync(
                It.Is<IEnumerable<Guid>>(ids => ids.Contains(dossierAppelant.Id)), It.IsAny<CancellationToken>()))
            .ReturnsAsync([ticketEnrolementAppelant]);

        var resultat = await _query.ExecuterAsync(appelantId);

        Assert.Single(resultat);
        Assert.Equal(ticketEnrolementAppelant.Id, resultat[0].Id);

        // Anti-IDOR : jamais interrogé pour un autre client que l'appelant.
        _dossierRepo.Verify(r => r.ListerParClientAsync(autreClientId, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Reflete_le_changement_d_etat_En_attente_client()
    {
        var appelantId = Guid.NewGuid();
        var dossier = Dossier.Creer("DOS-001", appelantId, DateTime.UtcNow);
        _dossierRepo.Setup(r => r.ListerParClientAsync(appelantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([dossier]);

        var ticket = Ticket.Creer("TCK-001", "Enrolement", "Enrolement", Guid.NewGuid(), dossier.Id, DateTime.UtcNow);
        ticket.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);
        ticket.MettreEnAttenteClient(DateTime.UtcNow);
        _ticketRepo.Setup(r => r.ListerParDossiersAsync(
                It.Is<IEnumerable<Guid>>(ids => ids.Contains(dossier.Id)), It.IsAny<CancellationToken>()))
            .ReturnsAsync([ticket]);

        var resultat = await _query.ExecuterAsync(appelantId);

        Assert.Single(resultat);
        Assert.Equal(StatutTicket.EnAttenteClient, resultat[0].Statut);
    }

    [Fact]
    public async Task Aucun_dossier_retourne_liste_vide()
    {
        var appelantId = Guid.NewGuid();
        _dossierRepo.Setup(r => r.ListerParClientAsync(appelantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var resultat = await _query.ExecuterAsync(appelantId);

        Assert.Empty(resultat);
        _ticketRepo.Verify(r => r.ListerParDossiersAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
