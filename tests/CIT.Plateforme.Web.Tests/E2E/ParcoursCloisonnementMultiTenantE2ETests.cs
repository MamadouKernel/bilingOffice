using System;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Client;
using CIT.Plateforme.Application.Dossiers;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Societe;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CIT.Plateforme.Web.Tests.E2E;

public sealed class ParcoursCloisonnementMultiTenantE2ETests
{
    [Fact]
    public async Task Cloisonnement_ClientSocieteA_NePeutConsulterNiDashboardNiDossierNiTicketDeSocieteB()
    {
        var options = new DbContextOptionsBuilder<PlateformeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new PlateformeDbContext(options);

        var userAId = Guid.NewGuid();
        var socAId = Guid.NewGuid();
        var userBId = Guid.NewGuid();
        var socBId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var socA = SocieteCliente.Soumettre(userAId, "SOCIETE-A", TypeClient.Normal, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(socA, socAId);
        socA.Valider();

        var socB = SocieteCliente.Soumettre(userBId, "SOCIETE-B", TypeClient.Normal, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(socB, socBId);
        socB.Valider();
        db.Societes.AddRange(socA, socB);

        var userA = new ApplicationUser { Id = userAId, UserName = "a@a.ci", NomAffiche = "User A", Nature = NatureUtilisateur.Client, EstActif = true };
        var userB = new ApplicationUser { Id = userBId, UserName = "b@b.ci", NomAffiche = "User B", Nature = NatureUtilisateur.Client, EstActif = true };
        db.Users.AddRange(userA, userB);

        var userSocA = UtilisateurSociete.Rattacher(userAId, socAId, DateTime.UtcNow, false, true);
        var userSocB = UtilisateurSociete.Rattacher(userBId, socBId, DateTime.UtcNow, false, true);
        db.UtilisateursSociete.AddRange(userSocA, userSocB);

        var file = FileDeTraitement.Creer("BillingImport", "Pilote Manifeste");
        typeof(FileDeTraitement).GetProperty("Id")!.SetValue(file, fileId);
        db.Files.Add(file);

        var dossierB = Dossier.Creer("DOS-SOC-B", socBId, DateTime.UtcNow, userBId);
        var ticketB = Ticket.Creer("TCK-SOC-B", "BillingImport", "IMPORT", fileId, dossierB.Id, DateTime.UtcNow, "BL-B");
        db.Dossiers.Add(dossierB);
        db.Tickets.Add(ticketB);
        await db.SaveChangesAsync();

        var dossierRepo = new DossierRepository(db);
        var ticketRepo = new TicketRepository(db);
        var fileRepo = new FileDeTraitementRepository(db);
        var socRepo = new SocieteClienteRepository(db);
        var userSocRepo = new UtilisateurSocieteRepository(db);
        var userInfoRepo = new UtilisateurInfoRepository(db);
        var histRepo = new HistoriqueTicketRepository(db);
        var docRepo = new DocumentRepository(db);
        var msgRepo = new MessageRepository(db);
        var habRepo = new HabilitationRepository(db);

        var dashboardClientQuery = new ObtenirDashboardClientQuery(dossierRepo, ticketRepo, socRepo, userSocRepo);
        var dashboardSocieteQuery = new ObtenirDashboardSocieteQuery(dossierRepo, ticketRepo, socRepo, userSocRepo, userInfoRepo);
        var ficheDossierQuery = new ObtenirFicheDossierQuery(dossierRepo, ticketRepo, fileRepo, socRepo, userSocRepo, userInfoRepo);
        var ficheTicketQuery = new ObtenirFicheTicketQuery(ticketRepo, dossierRepo, fileRepo, socRepo, userSocRepo, userInfoRepo, histRepo, docRepo, msgRepo, habRepo);

        // 1. User A tente d'accéder au dashboard de Société B -> Refusé
        await Assert.ThrowsAsync<AccesRefuseException>(() => dashboardClientQuery.ExecuterAsync(userAId, socBId));

        // 2. User A tente d'accéder au dashboard admin de Société B -> Refusé
        await Assert.ThrowsAsync<AccesRefuseException>(() => dashboardSocieteQuery.ExecuterAsync(userAId, socBId));

        // 3. User A tente de consulter le dossier de Société B -> Refusé
        await Assert.ThrowsAsync<AccesRefuseException>(() => ficheDossierQuery.ExecuterAsync(userAId, dossierB.Id));

        // 4. User A tente de consulter le ticket de Société B -> Refusé
        await Assert.ThrowsAsync<AccesRefuseException>(() => ficheTicketQuery.ExecuterAsync(userAId, ticketB.Id));
    }
}
