using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Client;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Components.Pages.Client;
using CIT.Plateforme.Web.Services;
using CIT.Plateforme.Web.Tests.Components.Layout;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Pages.Client;

public sealed class DashboardClientTests : BunitContext
{
    public DashboardClientTests() => LocalisationDeTest.Enregistrer(Services);

    [Fact]
    public void DashboardClient_RenduComplet_MetriquesEtActionsRequises()
    {
        var socId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = userId,
            SocieteId = socId,
            SocieteNom = "TRANS-AFRIQUE",
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.Client
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);

        var options = new DbContextOptionsBuilder<PlateformeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new PlateformeDbContext(options);

        var soc = SocieteCliente.Soumettre(userId, "TRANS-AFRIQUE", TypeClient.Normal, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(soc, socId);
        soc.Valider();
        db.Societes.Add(soc);

        var userSoc = UtilisateurSociete.Rattacher(userId, socId, DateTime.UtcNow, false, true);
        db.UtilisateursSociete.Add(userSoc);

        var dossier = Dossier.Creer("DOS-TEST-1", socId, DateTime.UtcNow, userId);
        var fileId = Guid.NewGuid();
        var ticket = Ticket.Creer("TCK-TEST-1", "IMPORT", "STANDARD", fileId, dossier.Id, DateTime.UtcNow, "BL-9999");
        ticket.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);
        ticket.MettreEnAttenteClient(DateTime.UtcNow);
        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);
        db.SaveChanges();

        var dossierRepo = new DossierRepository(db);
        var ticketRepo = new TicketRepository(db);
        var userSocRepo = new UtilisateurSocieteRepository(db);
        var socRepo = new SocieteClienteRepository(db);
        var query = new ObtenirDashboardClientQuery(dossierRepo, ticketRepo, socRepo, userSocRepo);
        Services.AddSingleton(query);

        var cut = Render<DashboardClient>();

        Assert.Contains("TRANS-AFRIQUE", cut.Markup);
        Assert.Contains("Mes actions requises (1)", cut.Markup);
        Assert.Contains("BL-9999", cut.Markup);
        Assert.Contains("DOS-TEST-1", cut.Markup);
        Assert.Contains("/billing-import/soumettre", cut.Markup);
    }

    [Fact]
    public void AM02_CollaborateurLimiteSansDroitSoumission_NeVoitPasLeRaccourciDeSoumission()
    {
        var socId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = userId,
            SocieteId = socId,
            SocieteNom = "TRANS-AFRIQUE",
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.Client
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);

        var options = new DbContextOptionsBuilder<PlateformeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new PlateformeDbContext(options);

        var soc = SocieteCliente.Soumettre(userId, "TRANS-AFRIQUE", TypeClient.Normal, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(soc, socId);
        soc.Valider();
        db.Societes.Add(soc);

        // Collaborateur limité : rattachement actif mais SANS droit de soumission.
        var userSoc = UtilisateurSociete.Rattacher(userId, socId, DateTime.UtcNow, false, false);
        db.UtilisateursSociete.Add(userSoc);
        db.SaveChanges();

        var dossierRepo = new DossierRepository(db);
        var ticketRepo = new TicketRepository(db);
        var userSocRepo = new UtilisateurSocieteRepository(db);
        var socRepo = new SocieteClienteRepository(db);
        var query = new ObtenirDashboardClientQuery(dossierRepo, ticketRepo, socRepo, userSocRepo);
        Services.AddSingleton(query);

        var cut = Render<DashboardClient>();

        // La page s'affiche, mais AUCUN raccourci de soumission n'est proposé.
        Assert.Contains("TRANS-AFRIQUE", cut.Markup);
        Assert.DoesNotContain("/billing-import/soumettre", cut.Markup);
        Assert.DoesNotContain("Nouvelle demande IMPORT", cut.Markup);
    }
}
