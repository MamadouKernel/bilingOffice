using System;
using Bunit;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tarification;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Web.Services;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Components.Pages.Tarification;
using CIT.Plateforme.Web.Tests.Components.Layout;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Pages.Tarification;

/// <summary>§11.5 / SIM-06,SIM-07,SIM-10 — rendu réel de l'écran gestion des grilles tarifaires.</summary>
public sealed class GestionGrillesTarifairesTests : BunitContext
{
    public GestionGrillesTarifairesTests() => LocalisationDeTest.Enregistrer(Services);

    private static PlateformeDbContext ConstruireBase() => new(
        new DbContextOptionsBuilder<PlateformeDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private void EnregistrerServices(PlateformeDbContext db)
    {
        var versions = new VersionGrilleTarifaireRepository(db);
        var referentiels = new ReferentielsTarifairesRepository(db);
        var importateur = new ImportateurGrilleTarifaireCsv();
        var autorisation = new AutorisationService(db);
        Services.AddScoped(_ => new ConsulterVersionsGrillesTarifairesQueryHandler(versions, referentiels, autorisation));
        Services.AddScoped(_ => new ImporterGrilleTarifaireCsvCommandHandler(versions, referentiels, importateur, autorisation));
        Services.AddScoped(_ => new ExporterGrilleTarifaireExcelQuery(versions, referentiels, autorisation));
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
    public void AdminHabilite_AccedeALaPage()
    {
        var db = ConstruireBase();
        var adminId = Guid.NewGuid();
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, adminId), Permission.TarificationAdministrer));
        db.SaveChanges();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        DefinirContexte(fakeContexte, adminId);
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<GestionGrillesTarifaires>();

        Assert.DoesNotContain("Vous ne disposez pas de l'habilitation d'administration tarifaire", cut.Markup);
        Assert.Contains("Aucune grille tarifaire", cut.Markup);
    }

    [Fact]
    public void UtilisateurNonHabilite_AfficheMessageRefus()
    {
        var db = ConstruireBase();
        var fakeContexte = new FakeContexteUtilisateurWeb();
        DefinirContexte(fakeContexte, Guid.NewGuid());
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<GestionGrillesTarifaires>();

        Assert.Contains("Vous ne disposez pas de l'habilitation d'administration tarifaire", cut.Markup);
    }
}
