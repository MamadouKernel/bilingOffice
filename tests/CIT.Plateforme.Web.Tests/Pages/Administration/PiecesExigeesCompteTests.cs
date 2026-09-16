using System;
using Bunit;
using CIT.Plateforme.Application.Comptes;
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

/// <summary>§17.3 / CPT-01 — rendu réel de l'écran pièces exigées à l'ouverture d'un compte.</summary>
public sealed class PiecesExigeesCompteTests : BunitContext
{
    public PiecesExigeesCompteTests() => LocalisationDeTest.Enregistrer(Services);

    private static PlateformeDbContext ConstruireBase() => new(
        new DbContextOptionsBuilder<PlateformeDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private void EnregistrerServices(PlateformeDbContext db)
    {
        var pieces = new PieceExigeeCompteRepository(db);
        var autorisation = new AutorisationService(db);
        var journalisation = new JournalisationService(db, new HorlogeSysteme());
        var uow = new UniteDeTravail(db);
        Services.AddScoped(_ => new ListerPiecesExigeesQuery(pieces));
        Services.AddScoped(_ => new GererPiecesExigeesCommand(pieces, autorisation, journalisation, uow));
    }

    private static void DefinirContexte(FakeContexteUtilisateurWeb fakeContexte, Guid utilisateurId, bool habilite)
    {
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = utilisateurId,
            Nature = NatureUtilisateur.Agent,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.AdministrationCit,
            Permissions = habilite ? [Permission.AdministrationComptes] : []
        };
    }

    [Fact]
    public void AdminHabilite_AccedeALaPage()
    {
        var db = ConstruireBase();
        var fakeContexte = new FakeContexteUtilisateurWeb();
        DefinirContexte(fakeContexte, Guid.NewGuid(), habilite: true);
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<PiecesExigeesCompte>();

        Assert.DoesNotContain("CIT-SEC-403", cut.Markup);
        Assert.Contains("Aucune pièce paramétrée", cut.Markup);
    }

    [Fact]
    public void UtilisateurNonHabilite_AffichePermissionDenied()
    {
        var db = ConstruireBase();
        var fakeContexte = new FakeContexteUtilisateurWeb();
        DefinirContexte(fakeContexte, Guid.NewGuid(), habilite: false);
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<PiecesExigeesCompte>();

        Assert.Contains("CIT-SEC-403", cut.Markup);
    }
}
