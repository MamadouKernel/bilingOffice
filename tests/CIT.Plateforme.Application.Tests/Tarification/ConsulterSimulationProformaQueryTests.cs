using CIT.Plateforme.Application.Tarification;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Tarification;

public sealed class ConsulterSimulationProformaQueryTests
{
    [Fact]
    public async Task ConsulterSimulation_detecte_si_la_grille_a_ete_revisee()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var simRepo = new SimulationProformaRepository(db);
        var grilleRepo = new VersionGrilleTarifaireRepository(db);
        var referentielsRepo = new ReferentielsTarifairesRepository(db);

        var societeId = Guid.NewGuid();

        // Ancienne grille (v1.0), puis simulation établie dessus.
        var ancien = await ReferentielsDeTest.PreparerAsync(
            db, "v1.0", DateTime.UtcNow.AddMonths(-2));

        var simulation = SimulationProforma.Creer(
            "SIM-001", societeId, Guid.NewGuid(), DateTime.UtcNow.AddMonths(-1),
            ancien.Grille.Id, "v1.0", ancien.Dossier, ancien.Lignes);
        await simRepo.AjouterAsync(simulation);

        // Nouvelle grille active (v2.0) — les référentiels sont déjà amorcés.
        await ReferentielsDeTest.PreparerAsync(
            db, "v2.0", DateTime.UtcNow.AddDays(-1),
            acconage: 70000m, relevage: 28000m, tarifJourBranchement: 37000m,
            amorcerReferentiels: false);

        // Consulter → la simulation originale sur v1.0 doit signaler l'obsolescence
        var handler = new ConsulterSimulationProformaQueryHandler(simRepo, grilleRepo, referentielsRepo, CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.ControleAcces(db));
        var res = await handler.HandleAsync(new ConsulterSimulationProformaRequete(CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.AppelantDeSociete(db, societeId), simulation.Id));

        Assert.NotNull(res);
        Assert.True(res.EstGrilleObsolete); // v1.0 != v2.0
        Assert.Equal("v1.0", res.VersionGrilleLibelle);
        Assert.Equal("v2.0", res.VersionActiveActuelleLibelle);
    }

    [Fact]
    public async Task ConsulterSimulation_sur_grille_active_ne_signale_pas_obsolescence()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var simRepo = new SimulationProformaRepository(db);
        var grilleRepo = new VersionGrilleTarifaireRepository(db);
        var referentielsRepo = new ReferentielsTarifairesRepository(db);

        var societeId = Guid.NewGuid();
        var contexte = await ReferentielsDeTest.PreparerAsync(db);

        var simulation = SimulationProforma.Creer(
            "SIM-002", societeId, Guid.NewGuid(), DateTime.UtcNow,
            contexte.Grille.Id, "v1.0", contexte.Dossier, contexte.Lignes);
        await simRepo.AjouterAsync(simulation);

        var handler = new ConsulterSimulationProformaQueryHandler(simRepo, grilleRepo, referentielsRepo, CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.ControleAcces(db));
        var res = await handler.HandleAsync(new ConsulterSimulationProformaRequete(CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.AppelantDeSociete(db, societeId), simulation.Id));

        Assert.NotNull(res);
        Assert.False(res.EstGrilleObsolete);
        Assert.Equal("v1.0", res.VersionGrilleLibelle);
        Assert.Equal("v1.0", res.VersionActiveActuelleLibelle);

        // Les libellés du référentiel sont résolus à la lecture : l'instantané ne les stocke pas.
        Assert.Equal("Import local", res.Sens.Libelle);
        Assert.Equal("C1", res.Lignes[0].Categorie.Code);
    }
}
