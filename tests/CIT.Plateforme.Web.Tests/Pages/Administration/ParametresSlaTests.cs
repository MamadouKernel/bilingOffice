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

/// <summary>
/// §17.3 / SLA-05 — vérifie que l'écran d'administration des paramètres SLA se rend réellement
/// (pas seulement que la commande backend fonctionne). Écart identifié en re-vérification
/// d'audit : 17 des 18 écrans d'administration n'avaient aucun test bUnit qui instancie le
/// composant Razor.
/// </summary>
public sealed class ParametresSlaTests : BunitContext
{
    public ParametresSlaTests() => LocalisationDeTest.Enregistrer(Services);

    private static PlateformeDbContext ConstruireBase()
    {
        var options = new DbContextOptionsBuilder<PlateformeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PlateformeDbContext(options);
    }

    private void EnregistrerServices(PlateformeDbContext db)
    {
        var parametres = new ParametreSlaRepository(db);
        var files = new FileDeTraitementRepository(db);
        var priorites = new PrioriteRepository(db);
        var autorisation = new AutorisationService(db);
        var horloge = new HorlogeSysteme();
        var journalisation = new JournalisationService(db, horloge);
        var uow = new UniteDeTravail(db);

        Services.AddScoped(_ => new GererParametresSlaCommand(parametres, files, priorites, autorisation, journalisation, uow));
    }

    [Fact]
    public void AdminHabilite_AccedeALaPage()
    {
        var db = ConstruireBase();
        var adminId = Guid.NewGuid();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = adminId,
            Nature = NatureUtilisateur.Agent,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.AdministrationCit,
            Permissions = [Permission.AdministrationSla]
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<ParametresSla>();

        Assert.DoesNotContain("CIT-SEC-403", cut.Markup);
        Assert.Contains("Aucun paramétrage SLA", cut.Markup);
    }

    [Fact]
    public void UtilisateurNonHabilite_AffichePermissionDenied()
    {
        var db = ConstruireBase();
        var utilisateurId = Guid.NewGuid();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = utilisateurId,
            Nature = NatureUtilisateur.Agent,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.AdministrationCit,
            Permissions = [] // Aucune habilitation AdministrationSla.
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<ParametresSla>();

        Assert.Contains("CIT-SEC-403", cut.Markup);
    }
}
