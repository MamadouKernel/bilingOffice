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

        var societeId = Guid.NewGuid();

        // Créer l'ancienne grille (v1.0) et la désactiver
        var ancienneGrille = VersionGrilleTarifaire.Creer("v1.0", DateTime.UtcNow.AddMonths(-2), Guid.NewGuid(), DateTime.UtcNow.AddMonths(-2));
        ancienneGrille.AjouterLigne(SensTrafic.ImportLocal, TailleConteneur.VingtPieds, "C1", 66800m, 25000m, 35000m);
        await grilleRepo.AjouterAsync(ancienneGrille);

        // Créer la simulation sur l'ancienne grille
        var conteneur = new ConteneurSimulation("C1", TailleConteneur.VingtPieds, TypeConteneur.Dry, 10m, false, false, "C1");
        var dossier = new DossierSimulation(SensTrafic.ImportLocal, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 5), [conteneur]);
        var ligne = new LigneResultatConteneur(conteneur, 66800m, 25000m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m);
        var simulation = SimulationProforma.Creer("SIM-001", societeId, Guid.NewGuid(), DateTime.UtcNow.AddMonths(-1), ancienneGrille.Id, "v1.0", dossier, [ligne]);
        await simRepo.AjouterAsync(simulation);

        // Créer la nouvelle grille active (v2.0)
        var nouvelleGrille = VersionGrilleTarifaire.Creer("v2.0", DateTime.UtcNow.AddDays(-1), Guid.NewGuid(), DateTime.UtcNow);
        nouvelleGrille.AjouterLigne(SensTrafic.ImportLocal, TailleConteneur.VingtPieds, "C1", 70000m, 28000m, 37000m);
        await grilleRepo.AjouterAsync(nouvelleGrille);

        // Consulter → la simulation originale sur v1.0 doit signaler l'obsolescence
        var handler = new ConsulterSimulationProformaQueryHandler(simRepo, grilleRepo);
        var res = await handler.HandleAsync(new ConsulterSimulationProformaRequete(Guid.NewGuid(), societeId, simulation.Id));

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

        var societeId = Guid.NewGuid();

        // Créer la seule grille active
        var grille = VersionGrilleTarifaire.Creer("v1.0", DateTime.UtcNow.AddDays(-1), Guid.NewGuid(), DateTime.UtcNow);
        grille.AjouterLigne(SensTrafic.ImportLocal, TailleConteneur.VingtPieds, "C1", 66800m, 25000m, 35000m);
        await grilleRepo.AjouterAsync(grille);

        var conteneur = new ConteneurSimulation("C1", TailleConteneur.VingtPieds, TypeConteneur.Dry, 10m, false, false, "C1");
        var dossier = new DossierSimulation(SensTrafic.ImportLocal, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 5), [conteneur]);
        var ligne = new LigneResultatConteneur(conteneur, 66800m, 25000m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m);
        var simulation = SimulationProforma.Creer("SIM-002", societeId, Guid.NewGuid(), DateTime.UtcNow, grille.Id, "v1.0", dossier, [ligne]);
        await simRepo.AjouterAsync(simulation);

        var handler = new ConsulterSimulationProformaQueryHandler(simRepo, grilleRepo);
        var res = await handler.HandleAsync(new ConsulterSimulationProformaRequete(Guid.NewGuid(), societeId, simulation.Id));

        Assert.NotNull(res);
        Assert.False(res.EstGrilleObsolete);
        Assert.Equal("v1.0", res.VersionGrilleLibelle);
        Assert.Equal("v1.0", res.VersionActiveActuelleLibelle);
    }
}
