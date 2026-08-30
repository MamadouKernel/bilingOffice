using System;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Societe;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Societe;

public sealed class ObtenirDashboardSocieteQueryTests
{
    private static (PlateformeDbContext Db, ObtenirDashboardSocieteQuery Query, Guid SocieteId, Guid AdminId) ConstruireContexte(bool estAdmin = true)
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var socId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        var soc = SocieteCliente.Soumettre(adminId, "TRANS-AFRIQUE", TypeClient.Normal, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(soc, socId);
        soc.Valider();
        db.Societes.Add(soc);

        var adminUser = new ApplicationUser
        {
            Id = adminId,
            UserName = "admin@trans.ci",
            Email = "admin@trans.ci",
            NomAffiche = "Admin Trans",
            Nature = NatureUtilisateur.Client,
            EstActif = true
        };
        db.Users.Add(adminUser);

        var userSoc = UtilisateurSociete.Rattacher(adminId, socId, DateTime.UtcNow, estAdmin, true);
        userSoc.ModifierDroits(DroitConsultation.ToutesDemandesSociete, true);
        db.UtilisateursSociete.Add(userSoc);

        var dossierRepo = new DossierRepository(db);
        var ticketRepo = new TicketRepository(db);
        var socRepo = new SocieteClienteRepository(db);
        var userSocRepo = new UtilisateurSocieteRepository(db);
        var userInfoRepo = new UtilisateurInfoRepository(db);

        var query = new ObtenirDashboardSocieteQuery(dossierRepo, ticketRepo, socRepo, userSocRepo, userInfoRepo);

        return (db, query, socId, adminId);
    }

    [Fact]
    public async Task AdminSociete_RetourneDashboardSocieteComplet()
    {
        var (db, query, socId, adminId) = ConstruireContexte(estAdmin: true);

        // Ajouter un collaborateur standard
        var collId = Guid.NewGuid();
        var collUser = new ApplicationUser
        {
            Id = collId,
            UserName = "agent@trans.ci",
            Email = "agent@trans.ci",
            NomAffiche = "Collaborateur 1",
            Nature = NatureUtilisateur.Client,
            EstActif = true
        };
        db.Users.Add(collUser);
        var userSoc2 = UtilisateurSociete.Rattacher(collId, socId, DateTime.UtcNow, false, true);
        db.UtilisateursSociete.Add(userSoc2);

        var dossier = Dossier.Creer("DOS-1", socId, DateTime.UtcNow, collId);
        var ticket = Ticket.Creer("TCK-1", "IMPORT", "STANDARD", Guid.NewGuid(), dossier.Id, DateTime.UtcNow, "BL-1");
        ticket.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);
        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);

        await db.SaveChangesAsync();

        var dashboard = await query.ExecuterAsync(adminId, socId);

        Assert.Equal(socId, dashboard.SocieteId);
        Assert.Equal("TRANS-AFRIQUE", dashboard.SocieteNom);
        Assert.Equal(2, dashboard.Collaborateurs.Count);
        Assert.Equal(1, dashboard.TotalDossiersSociete);
        Assert.Equal(1, dashboard.TotalTicketsEnCours);
    }

    [Fact]
    public async Task NonAdmin_LeveAccesRefuseException()
    {
        var (_, query, socId, nonAdminId) = ConstruireContexte(estAdmin: false);

        await Assert.ThrowsAsync<AccesRefuseException>(() => query.ExecuterAsync(nonAdminId, socId));
    }
}
