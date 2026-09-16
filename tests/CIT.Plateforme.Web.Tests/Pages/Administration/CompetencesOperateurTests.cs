using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Affectation;
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

/// <summary>§17.3 / AFF-06 — rendu réel de l'écran compétences des opérateurs.</summary>
public sealed class CompetencesOperateurTests : BunitContext
{
    public CompetencesOperateurTests() => LocalisationDeTest.Enregistrer(Services);

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
        var competences = new CompetenceOperateurRepository(db);
        var comptes = new FakeAdministrationComptesRepository();
        var autorisation = new AutorisationService(db);
        var journalisation = new JournalisationService(db, new HorlogeSysteme());
        var uow = new UniteDeTravail(db);
        Services.AddScoped(_ => new ListerCompetencesOperateurQuery(competences, comptes, autorisation));
        Services.AddScoped(_ => new GererCompetencesOperateurCommand(competences, autorisation, journalisation, uow));
    }

    private static void DefinirContexte(FakeContexteUtilisateurWeb fakeContexte, Guid utilisateurId, bool habilite)
    {
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = utilisateurId,
            Nature = NatureUtilisateur.Agent,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.AdministrationCit,
            Permissions = habilite ? [Permission.CompetenceGerer] : []
        };
    }

    [Fact]
    public void AdminHabilite_AccedeALaPage()
    {
        var db = ConstruireBase();
        var adminId = Guid.NewGuid();
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, adminId), Permission.CompetenceGerer));
        db.SaveChanges();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        DefinirContexte(fakeContexte, adminId, habilite: true);
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<CompetencesOperateur>();

        Assert.DoesNotContain("CIT-SEC-403", cut.Markup);
        Assert.Contains("Choisissez un opérateur", cut.Markup);
    }

    [Fact]
    public void UtilisateurNonHabilite_AffichePermissionDenied()
    {
        var db = ConstruireBase();
        var fakeContexte = new FakeContexteUtilisateurWeb();
        DefinirContexte(fakeContexte, Guid.NewGuid(), habilite: false);
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<CompetencesOperateur>();

        Assert.Contains("CIT-SEC-403", cut.Markup);
    }
}
