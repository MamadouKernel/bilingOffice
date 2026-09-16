using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using CIT.Plateforme.Application.Abstractions;
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

namespace CIT.Plateforme.Web.Tests.Pages.Societe;

public sealed class DashboardSocieteTests : BunitContext
{
    public DashboardSocieteTests() => LocalisationDeTest.Enregistrer(Services);

    [Fact]
    public void DashboardSociete_RenduComplet_MetriquesEtMembres()
    {
        var socId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = adminId,
            SocieteId = socId,
            SocieteNom = "TRANS-AFRIQUE",
            EstAdministrateurSociete = true,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.AdministrateurSociete
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);

        var options = new DbContextOptionsBuilder<PlateformeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new PlateformeDbContext(options);

        var soc = SocieteCliente.Soumettre(adminId, "TRANS-AFRIQUE", TypeClient.Normal, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(soc, socId);
        soc.Valider();
        db.Societes.Add(soc);

        var user = new ApplicationUser
        {
            Id = adminId,
            UserName = "admin@trans.ci",
            Email = "admin@trans.ci",
            NomAffiche = "Admin Trans",
            Nature = NatureUtilisateur.Client,
            EstActif = true
        };
        db.Users.Add(user);

        var userSoc = UtilisateurSociete.Rattacher(adminId, socId, DateTime.UtcNow, true, true);
        db.UtilisateursSociete.Add(userSoc);
        db.SaveChanges();

        var dossierRepo = new DossierRepository(db);
        var ticketRepo = new TicketRepository(db);
        var socRepo = new SocieteClienteRepository(db);
        var userSocRepo = new UtilisateurSocieteRepository(db);
        var userInfoRepo = new UtilisateurInfoRepository(db);
        var query = new ObtenirDashboardSocieteQuery(dossierRepo, ticketRepo, socRepo, userSocRepo, userInfoRepo);
        Services.AddSingleton(query);

        var cut = Render<DashboardSociete>();

        Assert.Contains("Espace Entreprise", cut.Markup);
        Assert.Contains("TRANS-AFRIQUE", cut.Markup);
        Assert.Contains("Admin Trans", cut.Markup);
        Assert.Contains("Administrateur", cut.Markup);
        Assert.Contains("/societe/collaborateurs", cut.Markup);
    }
}
