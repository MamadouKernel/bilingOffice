using System;
using System.Linq;
using Bunit;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Components.Pages.Comptes;
using CIT.Plateforme.Web.Services;
using CIT.Plateforme.Web.Tests.Components.Layout;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Pages.Comptes;

/// <summary>SEC-06 (« droits des personnes ») — rendu réel de l'écran « Mes données ».</summary>
public sealed class MesDonneesTests : BunitContext
{
    public MesDonneesTests() => LocalisationDeTest.Enregistrer(Services);

    private static PlateformeDbContext ConstruireBase() => new(
        new DbContextOptionsBuilder<PlateformeDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private void EnregistrerServices(PlateformeDbContext db)
    {
        var horloge = new HorlogeSysteme();
        var utilisateursSociete = new UtilisateurSocieteRepository(db);
        var dossiers = new DossierRepository(db);
        var tickets = new TicketRepository(db);
        var documents = new DocumentRepository(db);
        var journalisation = new JournalisationService(db, horloge);
        var uow = new UniteDeTravail(db);

        Services.AddScoped<CIT.Plateforme.Application.Abstractions.IDossierRepository>(_ => dossiers);
        Services.AddScoped<CIT.Plateforme.Application.Abstractions.IUtilisateurSocieteRepository>(_ => utilisateursSociete);
        Services.AddScoped(_ => new ExporterDonneesSocieteQuery(utilisateursSociete, dossiers, tickets, documents, horloge));
        Services.AddScoped(_ => new AnonymiserDossierCommand(utilisateursSociete, dossiers, tickets, horloge, uow, journalisation));
    }

    private static void DefinirContexte(FakeContexteUtilisateurWeb fakeContexte, Guid utilisateurId)
    {
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = utilisateurId,
            Nature = NatureUtilisateur.Client,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.Client
        };
    }

    [Fact]
    public void AdministrateurSociete_PeutGenererLexportDeSesDonnees()
    {
        var db = ConstruireBase();
        var adminId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        db.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(adminId, societeId, DateTime.UtcNow, estAdministrateur: true));
        var dossier = Dossier.Creer("DOS-EXP-001", societeId, DateTime.UtcNow);
        db.Dossiers.Add(dossier);
        db.SaveChanges();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        DefinirContexte(fakeContexte, adminId);
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<MesDonnees>();
        cut.Find("button.btn-primary").Click();

        Assert.Contains("DOS-EXP-001", cut.Markup);
    }

    [Fact]
    public void AdministrateurSociete_AnonymiseUnDossierClosDeSaPropreSociete()
    {
        var db = ConstruireBase();
        var adminId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        db.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(adminId, societeId, DateTime.UtcNow, estAdministrateur: true));
        var dossier = Dossier.Creer("DOS-ANO-001", societeId, DateTime.UtcNow);
        db.Dossiers.Add(dossier);
        var ticket = Ticket.Creer("TCK-ANO-001", "CustomerComplaint", "Reclamation", Guid.NewGuid(), dossier.Id, DateTime.UtcNow);
        ticket.DefinirContenuReclamation("Objet personnel", "Description personnelle");
        ticket.Cloturer(MotifClotureType.Normale, DateTime.UtcNow);
        db.Tickets.Add(ticket);
        db.SaveChanges();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        DefinirContexte(fakeContexte, adminId);
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<MesDonnees>();
        cut.Find("#dossierNumero").Change("DOS-ANO-001");
        cut.Find("button.btn-outline-danger").Click();
        cut.Find("button.btn-danger.btn-sm").Click();

        Assert.Contains("anonymisé", cut.Markup);

        var ticketRecharge = db.Tickets.First(t => t.Numero == "TCK-ANO-001");
        Assert.True(ticketRecharge.EstAnonymise);
        Assert.Equal(Ticket.ValeurAnonymisee, ticketRecharge.Objet);
    }

    [Fact]
    public void AdministrateurSociete_NePeutPasAnonymiserLeDossierDuneAutreSociete()
    {
        var db = ConstruireBase();
        var adminId = Guid.NewGuid();
        var societeAppelant = Guid.NewGuid();
        var societeAutre = Guid.NewGuid();
        db.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(adminId, societeAppelant, DateTime.UtcNow, estAdministrateur: true));
        var dossierAutre = Dossier.Creer("DOS-TIERS-001", societeAutre, DateTime.UtcNow);
        db.Dossiers.Add(dossierAutre);
        db.SaveChanges();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        DefinirContexte(fakeContexte, adminId);
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<MesDonnees>();
        cut.Find("#dossierNumero").Change("DOS-TIERS-001");
        cut.Find("button.btn-outline-danger").Click();
        cut.Find("button.btn-danger.btn-sm").Click();

        Assert.DoesNotContain("anonymisé(s) avec succès", cut.Markup);
        var dossierIntact = db.Dossiers.First(d => d.Numero == "DOS-TIERS-001");
        Assert.Equal(societeAutre, dossierIntact.ClientId);
    }
}
