using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Client;
using CIT.Plateforme.Application.Dossiers;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Dossiers;

public sealed class ObtenirFicheDossierQueryTests
{
    private static (PlateformeDbContext Db, ObtenirFicheDossierQuery Query, Guid SocId, Guid ClientUserId) ConstruireContexte()
    {
        var options = new DbContextOptionsBuilder<PlateformeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new PlateformeDbContext(options);

        var socId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();

        var soc = SocieteCliente.Soumettre(clientUserId, "SOCIETE CI", TypeClient.Normal, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(soc, socId);
        soc.Valider();
        db.Societes.Add(soc);

        var userClient = new ApplicationUser
        {
            Id = clientUserId,
            UserName = "client@ci.ci",
            NomAffiche = "Client CI",
            Nature = NatureUtilisateur.Client,
            EstActif = true
        };
        db.Users.Add(userClient);

        var userSoc = UtilisateurSociete.Rattacher(clientUserId, socId, DateTime.UtcNow, true, true);
        db.UtilisateursSociete.Add(userSoc);

        var dossierRepo = new DossierRepository(db);
        var ticketRepo = new TicketRepository(db);
        var fileRepo = new FileDeTraitementRepository(db);
        var socRepo = new SocieteClienteRepository(db);
        var userSocRepo = new UtilisateurSocieteRepository(db);
        var userInfoRepo = new UtilisateurInfoRepository(db);

        var query = new ObtenirFicheDossierQuery(
            dossierRepo, ticketRepo, fileRepo, socRepo, userSocRepo, userInfoRepo);

        return (db, query, socId, clientUserId);
    }

    [Fact]
    public async Task FicheDossier_RetourneSyntheseEtListeTickets()
    {
        var (db, query, socId, clientUserId) = ConstruireContexte();

        var fileId = Guid.NewGuid();
        var file = FileDeTraitement.Creer("IMPORT", "Pilote Manifeste");
        typeof(FileDeTraitement).GetProperty("Id")!.SetValue(file, fileId);
        db.Files.Add(file);

        var dossier = Dossier.Creer("DOS-2026-99", socId, DateTime.UtcNow, clientUserId);
        var ticket1 = Ticket.Creer("TCK-01", "IMPORT", "STANDARD", fileId, dossier.Id, DateTime.UtcNow, "BL-01");
        var ticket2 = Ticket.Creer("TCK-02", "IMPORT", "STANDARD", fileId, dossier.Id, DateTime.UtcNow, "BL-02");

        db.Dossiers.Add(dossier);
        db.Tickets.AddRange(ticket1, ticket2);
        await db.SaveChangesAsync();

        var fiche = await query.ExecuterAsync(clientUserId, dossier.Id);

        Assert.Equal(dossier.Id, fiche.DossierId);
        Assert.Equal("DOS-2026-99", fiche.NumeroDossier);
        Assert.Equal(2, fiche.TotalTickets);
        Assert.Equal(2, fiche.Tickets.Count);
        Assert.Equal(StatutVisuelDossier.EnAttentePriseEnCharge, fiche.StatutGlobal);
    }
}
