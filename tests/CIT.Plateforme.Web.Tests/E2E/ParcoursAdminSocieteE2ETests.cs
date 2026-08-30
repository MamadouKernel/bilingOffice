using System;
using System.Threading.Tasks;
using Bunit;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Societe;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Components.Pages.Societe;
using CIT.Plateforme.Web.Services;
using CIT.Plateforme.Web.Tests.Components.Layout;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.E2E;

public sealed class ParcoursAdminSocieteE2ETests : BunitContext
{
    [Fact]
    public async Task ParcoursAdminSociete_GestionCollaborateursComplete()
    {
        var options = new DbContextOptionsBuilder<PlateformeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new PlateformeDbContext(options);

        var adminId = Guid.NewGuid();
        var socId = Guid.NewGuid();
        var collId = Guid.NewGuid();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = adminId,
            SocieteId = socId,
            SocieteNom = "SOC-ADMIN-E2E",
            NomAffiche = "Admin Société",
            Nature = NatureUtilisateur.Client,
            EstAuthentifie = true,
            EstAdministrateurSociete = true,
            EspaceActif = EspaceFonctionnel.AdministrateurSociete
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);

        var soc = SocieteCliente.Soumettre(adminId, "SOC-ADMIN-E2E", TypeClient.Normal, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(soc, socId);
        soc.Valider();
        db.Societes.Add(soc);

        var userAdmin = new ApplicationUser
        {
            Id = adminId,
            UserName = "admin@soc.ci",
            Email = "admin@soc.ci",
            NomAffiche = "Admin Société",
            Nature = NatureUtilisateur.Client,
            EstActif = true
        };
        var userColl = new ApplicationUser
        {
            Id = collId,
            UserName = "coll@soc.ci",
            Email = "coll@soc.ci",
            NomAffiche = "Collaborateur Standard",
            Nature = NatureUtilisateur.Client,
            EstActif = true
        };
        db.Users.AddRange(userAdmin, userColl);

        var userSocAdmin = UtilisateurSociete.Rattacher(adminId, socId, DateTime.UtcNow, true, true);
        var userSocColl = UtilisateurSociete.Rattacher(collId, socId, DateTime.UtcNow, false, true);
        db.UtilisateursSociete.AddRange(userSocAdmin, userSocColl);
        await db.SaveChangesAsync();

        var dossierRepo = new DossierRepository(db);
        var ticketRepo = new TicketRepository(db);
        var socRepo = new SocieteClienteRepository(db);
        var userSocRepo = new UtilisateurSocieteRepository(db);
        var userInfoRepo = new UtilisateurInfoRepository(db);
        var horloge = new HorlogeSysteme();
        var journalisation = new JournalisationService(db, horloge);
        var uow = new UniteDeTravail(db);

        var dashboardQuery = new ObtenirDashboardSocieteQuery(dossierRepo, ticketRepo, socRepo, userSocRepo, userInfoRepo);
        var rattacherCmd = new RattacherUtilisateurCommand(userSocRepo, journalisation, horloge, uow);
        var gererRoleCmd = new GererRoleAdministrateurCommand(userSocRepo, socRepo, journalisation, uow);
        var modifierDroitsCmd = new ModifierDroitsUtilisateurCommand(userSocRepo, journalisation, uow);
        var activerDesactiverCmd = new ActiverDesactiverUtilisateurCommand(userSocRepo, socRepo, journalisation, uow);

        Services.AddSingleton(dashboardQuery);
        Services.AddSingleton(rattacherCmd);
        Services.AddSingleton(gererRoleCmd);
        Services.AddSingleton(modifierDroitsCmd);
        Services.AddSingleton(activerDesactiverCmd);

        // 1. Dashboard Société
        var cutDashboard = Render<DashboardSociete>();
        Assert.Contains("SOC-ADMIN-E2E", cutDashboard.Markup);
        Assert.Contains("Collaborateur Standard", cutDashboard.Markup);

        // 2. Gestion Collaborateurs
        var cutCollaborateurs = Render<Collaborateurs>();
        Assert.Contains("Admin Société", cutCollaborateurs.Markup);
        Assert.Contains("Collaborateur Standard", cutCollaborateurs.Markup);
    }
}
