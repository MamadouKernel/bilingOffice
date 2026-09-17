using System;
using Bunit;
using CIT.Plateforme.Application.Administration;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Components.Pages.Administration;
using CIT.Plateforme.Web.Services;
using CIT.Plateforme.Web.Tests.Components.Layout;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Pages.Administration;

/// <summary>CDC §17.3 — rendu réel de l'écran de consultation des archives.</summary>
public sealed class ArchivesTests : BunitContext
{
    public ArchivesTests() => LocalisationDeTest.Enregistrer(Services);

    private static PlateformeDbContext ConstruireBase() => new(
        new DbContextOptionsBuilder<PlateformeDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private void EnregistrerServices(PlateformeDbContext db)
    {
        var tickets = new TicketRepository(db);
        var autorisation = new AutorisationService(db);
        Services.AddScoped(_ => new ConsulterArchivesQuery(tickets, autorisation));
    }

    private static void DefinirContexte(FakeContexteUtilisateurWeb fakeContexte, Guid utilisateurId)
    {
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = utilisateurId,
            Nature = NatureUtilisateur.Agent,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.AdministrationCit
        };
    }

    [Fact]
    public void AgentHabilite_AccedeALaPage_EtVoitEtatVide()
    {
        var db = ConstruireBase();
        var agentId = Guid.NewGuid();
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, agentId), Permission.AuditConsulter));
        db.SaveChanges();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        DefinirContexte(fakeContexte, agentId);
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<Archives>();

        Assert.DoesNotContain("CIT-SEC-403", cut.Markup);
        Assert.Contains("Aucun ticket archivé", cut.Markup);
    }

    [Fact]
    public void UtilisateurNonHabilite_AffichePermissionDenied()
    {
        var db = ConstruireBase();
        var fakeContexte = new FakeContexteUtilisateurWeb();
        DefinirContexte(fakeContexte, Guid.NewGuid());
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<Archives>();

        Assert.Contains("CIT-SEC-403", cut.Markup);
    }
}
