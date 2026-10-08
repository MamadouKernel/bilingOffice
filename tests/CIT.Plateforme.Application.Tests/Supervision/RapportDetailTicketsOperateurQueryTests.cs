using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Supervision;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Supervision;
using CIT.Plateforme.Domain.Tickets;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Supervision;

/// <summary>Détail « tickets traités par un agent » (temps par ticket), borné au périmètre.</summary>
public sealed class RapportDetailTicketsOperateurQueryTests
{
    private static readonly DateTime Debut = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Fin = new(2026, 10, 31, 23, 59, 59, DateTimeKind.Utc);

    [Fact]
    public async Task Liste_les_tickets_de_l_agent_avec_le_temps_de_traitement()
    {
        var superviseur = Guid.NewGuid();
        var agent = Guid.NewGuid();
        var autre = Guid.NewGuid();
        var fileA = FileDeTraitement.Creer("BillingImport", "Pilote Manifeste");
        var dossierId = Guid.NewGuid();

        var ticket = Ticket.Creer("TCK-ACT-0001", "BillingImport", "Import", fileA.Id, dossierId, Debut);

        var passage = PassageEnFile.Ouvrir(ticket.Id, fileA.Id, 1, new(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc));
        passage.PrendreEnCharge(agent, new(2026, 10, 5, 8, 20, 0, DateTimeKind.Utc));
        passage.Fermer(new(2026, 10, 5, 9, 0, 0, DateTimeKind.Utc));

        // Un passage d'un autre agent ne doit pas apparaître.
        var passageAutre = PassageEnFile.Ouvrir(Guid.NewGuid(), fileA.Id, 2, new(2026, 10, 6, 8, 0, 0, DateTimeKind.Utc));
        passageAutre.PrendreEnCharge(autre, new(2026, 10, 6, 8, 10, 0, DateTimeKind.Utc));

        var autorisation = new Mock<IAutorisationService>();
        var perimetres = new Mock<IPerimetreSupervisionRepository>();
        perimetres.Setup(p => p.ObtenirParSuperviseurAsync(superviseur, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PerimetreSupervision.Definir(superviseur, new[] { fileA.Id }));
        var files = new Mock<IFileDeTraitementRepository>();
        files.Setup(f => f.ListerAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FileDeTraitement> { fileA });
        var passages = new Mock<IPassageEnFileRepository>();
        passages.Setup(p => p.ListerParFilesEtPeriodeAsync(It.IsAny<IEnumerable<Guid>>(), Debut, Fin, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PassageEnFile> { passage, passageAutre });
        var tickets = new Mock<ITicketRepository>();
        tickets.Setup(t => t.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        var horloge = new Mock<IHorloge>();
        horloge.Setup(h => h.MaintenantUtc).Returns(Fin);

        var query = new RapportDetailTicketsOperateurQuery(
            passages.Object, files.Object, perimetres.Object, tickets.Object, horloge.Object, autorisation.Object);

        var res = await query.ExecuterAsync(superviseur, agent, Debut, Fin);

        autorisation.Verify(a => a.ExigerAsync(superviseur, Permission.SupervisionConsulter, null, It.IsAny<CancellationToken>()), Times.Once);
        var ligne = Assert.Single(res);
        Assert.Equal("TCK-ACT-0001", ligne.NumeroTicket);
        Assert.Equal(TimeSpan.FromMinutes(40), ligne.TempsTraitement);   // 08:20 -> 09:00
        Assert.Equal(TimeSpan.FromMinutes(20), ligne.TempsAttente);      // 08:00 -> 08:20
    }

    [Fact]
    public async Task Sans_perimetre_retourne_vide()
    {
        var superviseur = Guid.NewGuid();
        var perimetres = new Mock<IPerimetreSupervisionRepository>();
        perimetres.Setup(p => p.ObtenirParSuperviseurAsync(superviseur, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PerimetreSupervision?)null);
        var files = new Mock<IFileDeTraitementRepository>();
        files.Setup(f => f.ListerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<FileDeTraitement>());

        var query = new RapportDetailTicketsOperateurQuery(
            new Mock<IPassageEnFileRepository>().Object, files.Object, perimetres.Object,
            new Mock<ITicketRepository>().Object, new Mock<IHorloge>().Object, new Mock<IAutorisationService>().Object);

        var res = await query.ExecuterAsync(superviseur, Guid.NewGuid(), Debut, Fin);
        Assert.Empty(res);
    }
}
