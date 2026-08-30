using CIT.Plateforme.Application.Tarification;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Tarification;

public sealed class GestionSimulationsProformaTests
{
    [Fact]
    public async Task SauvegarderSimulation_genere_une_reference_unique_et_persiste_le_snapshot()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var simRepo = new SimulationProformaRepository(db);
        var grilleRepo = new VersionGrilleTarifaireRepository(db);

        var grille = VersionGrilleTarifaire.Creer("v1.0", DateTime.UtcNow.AddDays(-1), Guid.NewGuid(), DateTime.UtcNow);
        grille.AjouterLigne(SensTrafic.ImportLocal, TailleConteneur.VingtPieds, "C1", 66800m, 25000m, 35000m);
        await grilleRepo.AjouterAsync(grille);

        var handler = new SauvegarderSimulationProformaCommandHandler(simRepo, grilleRepo, new MoteurCalculProforma());
        var commande = new SauvegarderSimulationProformaCommande(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new CalculerSimulationProformaRequete(
                SensTrafic.ImportLocal,
                new DateOnly(2026, 2, 1),
                new DateOnly(2026, 2, 5),
                [new ConteneurSimulationRequete("C1", TailleConteneur.VingtPieds, TypeConteneur.Dry, 10m, false, false, "C1")]));

        var reponse = await handler.HandleAsync(commande);

        Assert.NotNull(reponse.Reference);
        Assert.StartsWith("SIM-", reponse.Reference);
        Assert.Equal(91800m, reponse.MontantTotalXof);

        // Vérifier la persistance réelle
        var persistee = await simRepo.ObtenirParReferenceAsync(reponse.Reference);
        Assert.NotNull(persistee);
        Assert.Equal(reponse.Reference, persistee.Reference);
        Assert.Equal(91800m, persistee.MontantTotalXof);
    }

    [Fact]
    public async Task ListerSimulations_retourne_les_simulations_de_la_societe_ordonnees_par_date()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var simRepo = new SimulationProformaRepository(db);
        var grilleRepo = new VersionGrilleTarifaireRepository(db);

        var societeId = Guid.NewGuid();

        var grille = VersionGrilleTarifaire.Creer("v1.0", DateTime.UtcNow.AddDays(-1), Guid.NewGuid(), DateTime.UtcNow);
        grille.AjouterLigne(SensTrafic.ImportLocal, TailleConteneur.VingtPieds, "C1", 66800m, 25000m, 35000m);
        await grilleRepo.AjouterAsync(grille);

        var conteneur = new ConteneurSimulation("C1", TailleConteneur.VingtPieds, TypeConteneur.Dry, 10m, false, false, "C1");
        var dossier = new DossierSimulation(SensTrafic.ImportLocal, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 5), [conteneur]);
        var ligne = new LigneResultatConteneur(conteneur, 66800m, 25000m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m);

        var sim1 = SimulationProforma.Creer("SIM-001", societeId, Guid.NewGuid(), DateTime.UtcNow.AddHours(-2), grille.Id, "v1.0", dossier, [ligne]);
        await simRepo.AjouterAsync(sim1);
        var sim2 = SimulationProforma.Creer("SIM-002", societeId, Guid.NewGuid(), DateTime.UtcNow, grille.Id, "v1.0", dossier, [ligne]);
        await simRepo.AjouterAsync(sim2);

        var handler = new ListerSimulationsProformaQueryHandler(simRepo);
        var resultat = await handler.HandleAsync(new ListerSimulationsProformaRequete(Guid.NewGuid(), societeId));

        Assert.Equal(2, resultat.Count);
        Assert.Equal("SIM-002", resultat[0].Reference); // Plus récente en premier
        Assert.Equal("SIM-001", resultat[1].Reference);
    }
}
