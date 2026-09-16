using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using CIT.Plateforme.Application.Abstractions;
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

/// <summary>§17.3 / SEC-11 — rendu réel de l'écran comptes agents.</summary>
public sealed class ComptesAgentsTests : BunitContext
{
    public ComptesAgentsTests() => LocalisationDeTest.Enregistrer(Services);

    private sealed class FakeAdministrationComptesRepository : IAdministrationComptesRepository
    {
        public Task<IReadOnlyList<UtilisateurInfo>> ListerAgentsAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<UtilisateurInfo>>([]);
        public Task<ResultatCreationCompte> CreerAgentAsync(string email, string nomAffiche, string motDePasseInitial, CancellationToken ct = default) =>
            throw new NotImplementedException();
        public Task DefinirActivationAsync(Guid utilisateurId, bool actif, CancellationToken ct = default) =>
            throw new NotImplementedException();
        public Task<bool> VerifierMotDePasseAsync(Guid utilisateurId, string motDePasse, CancellationToken ct = default) =>
            throw new NotImplementedException();
    }

    private static PlateformeDbContext ConstruireBase() => new(
        new DbContextOptionsBuilder<PlateformeDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private void EnregistrerServices(PlateformeDbContext db)
    {
        var comptes = new FakeAdministrationComptesRepository();
        var habilitations = new HabilitationRepository(db);
        var autorisation = new AutorisationService(db);
        Services.AddScoped(_ => new ListerComptesAgentsQuery(comptes, habilitations, autorisation));
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
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, adminId), Permission.AdministrationComptes));
        db.SaveChanges();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        DefinirContexte(fakeContexte, adminId);
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<ComptesAgents>();

        Assert.DoesNotContain("CIT-SEC-403", cut.Markup);
        Assert.Contains("Aucun compte agent enregistré", cut.Markup);
    }

    [Fact]
    public void UtilisateurNonHabilite_AffichePermissionDenied()
    {
        var db = ConstruireBase();
        var fakeContexte = new FakeContexteUtilisateurWeb();
        DefinirContexte(fakeContexte, Guid.NewGuid());
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<ComptesAgents>();

        Assert.Contains("CIT-SEC-403", cut.Markup);
    }
}
