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

/// <summary>§17.3 / TRF-06 — rendu réel de l'écran matrice des transitions.</summary>
public sealed class MatriceTransitionsTests : BunitContext
{
    public MatriceTransitionsTests() => LocalisationDeTest.Enregistrer(Services);

    private static PlateformeDbContext ConstruireBase() => new(
        new DbContextOptionsBuilder<PlateformeDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private void EnregistrerServices(PlateformeDbContext db)
    {
        var transitions = new TransitionAutoriseeRepository(db);
        var files = new FileDeTraitementRepository(db);
        var autorisation = new AutorisationService(db);
        var journalisation = new JournalisationService(db, new HorlogeSysteme());
        var uow = new UniteDeTravail(db);
        Services.AddScoped(_ => new GererMatriceTransitionsCommand(transitions, files, autorisation, journalisation, uow));
    }

    private static void DefinirContexte(FakeContexteUtilisateurWeb fakeContexte, Guid utilisateurId, bool habilite)
    {
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = utilisateurId,
            Nature = NatureUtilisateur.Agent,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.AdministrationCit,
            Permissions = habilite ? [Permission.AdministrationFiles] : []
        };
    }

    [Fact]
    public void AdminHabilite_AccedeALaPage()
    {
        var db = ConstruireBase();
        var adminId = Guid.NewGuid();
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, adminId), Permission.AdministrationFiles));
        db.SaveChanges();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        DefinirContexte(fakeContexte, adminId, habilite: true);
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<MatriceTransitions>();

        Assert.DoesNotContain("CIT-SEC-403", cut.Markup);
        Assert.Contains("Au moins deux files sont nécessaires", cut.Markup);
    }

    [Fact]
    public void UtilisateurNonHabilite_AffichePermissionDenied()
    {
        var db = ConstruireBase();
        var fakeContexte = new FakeContexteUtilisateurWeb();
        DefinirContexte(fakeContexte, Guid.NewGuid(), habilite: false);
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<MatriceTransitions>();

        Assert.Contains("CIT-SEC-403", cut.Markup);
    }
}
