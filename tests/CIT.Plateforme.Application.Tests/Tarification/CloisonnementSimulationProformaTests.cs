using CIT.Plateforme.Application.Tarification;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Tarification;

public sealed class CloisonnementSimulationProformaTests
{
    [Fact]
    public async Task ListerSimulations_filtre_strictement_par_societe_de_l_appelant()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var simRepo = new SimulationProformaRepository(db);
        var grilleRepo = new VersionGrilleTarifaireRepository(db);

        var societeA = Guid.NewGuid();
        var societeB = Guid.NewGuid();

        // Créer une grille et une simulation pour sociétéA
        var grille = VersionGrilleTarifaire.Creer("v1.0", DateTime.UtcNow.AddDays(-1), Guid.NewGuid(), DateTime.UtcNow);
        grille.AjouterLigne(SensTrafic.ImportLocal, TailleConteneur.VingtPieds, "C1", 66800m, 25000m, 35000m);
        await grilleRepo.AjouterAsync(grille);

        var conteneur = new ConteneurSimulation("C1", TailleConteneur.VingtPieds, TypeConteneur.Dry, 10m, false, false, "C1");
        var dossier = new DossierSimulation(SensTrafic.ImportLocal, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 5), [conteneur]);
        var ligne = new LigneResultatConteneur(conteneur, 66800m, 25000m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m);
        var simA = SimulationProforma.Creer("SIM-A", societeA, Guid.NewGuid(), DateTime.UtcNow, grille.Id, "v1.0", dossier, [ligne]);
        await simRepo.AjouterAsync(simA);

        // Créer une simulation pour sociétéB
        var simB = SimulationProforma.Creer("SIM-B", societeB, Guid.NewGuid(), DateTime.UtcNow, grille.Id, "v1.0", dossier, [ligne]);
        await simRepo.AjouterAsync(simB);

        // Lister pour sociétéA → ne retourne que SIM-A
        var handler = new ListerSimulationsProformaQueryHandler(simRepo);
        var resultat = await handler.HandleAsync(new ListerSimulationsProformaRequete(Guid.NewGuid(), societeA));

        Assert.Single(resultat);
        Assert.Equal("SIM-A", resultat[0].Reference);
    }

    [Fact]
    public async Task ConsulterSimulation_d_une_autre_societe_leve_UnauthorizedAccessException()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var simRepo = new SimulationProformaRepository(db);
        var grilleRepo = new VersionGrilleTarifaireRepository(db);

        var societeA = Guid.NewGuid();
        var societeB = Guid.NewGuid();

        var grille = VersionGrilleTarifaire.Creer("v1.0", DateTime.UtcNow.AddDays(-1), Guid.NewGuid(), DateTime.UtcNow);
        grille.AjouterLigne(SensTrafic.ImportLocal, TailleConteneur.VingtPieds, "C1", 66800m, 25000m, 35000m);
        await grilleRepo.AjouterAsync(grille);

        var conteneur = new ConteneurSimulation("C1", TailleConteneur.VingtPieds, TypeConteneur.Dry, 10m, false, false, "C1");
        var dossier = new DossierSimulation(SensTrafic.ImportLocal, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 5), [conteneur]);
        var ligne = new LigneResultatConteneur(conteneur, 66800m, 25000m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m);
        var simA = SimulationProforma.Creer("SIM-IDOR", societeA, Guid.NewGuid(), DateTime.UtcNow, grille.Id, "v1.0", dossier, [ligne]);
        await simRepo.AjouterAsync(simA);

        var handler = new ConsulterSimulationProformaQueryHandler(simRepo, grilleRepo);

        // SociétéB tente de consulter la simulation de sociétéA → IDOR refusé
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.HandleAsync(new ConsulterSimulationProformaRequete(Guid.NewGuid(), societeB, simA.Id)));
    }
}
