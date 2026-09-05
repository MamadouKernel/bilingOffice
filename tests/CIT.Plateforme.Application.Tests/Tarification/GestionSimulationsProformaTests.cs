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
        var referentielsRepo = new ReferentielsTarifairesRepository(db);

        var contexte = await ReferentielsDeTest.PreparerAsync(db);

        var handler = new SauvegarderSimulationProformaCommandHandler(
            simRepo, grilleRepo, referentielsRepo, new MoteurCalculProforma(), CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.ControleAcces(db));

        // P2-02 : la société n'est plus fournie — la simulation est rattachée à celle dont
        // l'appelant relève réellement. L'appelant doit donc être un compte rattaché.
        var societeId = Guid.NewGuid();
        var commande = new SauvegarderSimulationProformaCommande(
            CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.AppelantDeSociete(db, societeId),
            new CalculerSimulationProformaRequete(
                contexte.SensId,
                new DateOnly(2026, 2, 1),
                new DateOnly(2026, 2, 5),
                [new ConteneurSimulationRequete(
                    "C1", contexte.TailleId, contexte.TypeId, 10m, false, false, contexte.CategorieId)]));

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
        var referentielsRepo = new ReferentielsTarifairesRepository(db);

        var societeId = Guid.NewGuid();
        var contexte = await ReferentielsDeTest.PreparerAsync(db);

        var sim1 = SimulationProforma.Creer(
            "SIM-001", societeId, Guid.NewGuid(), DateTime.UtcNow.AddHours(-2),
            contexte.Grille.Id, "v1.0", contexte.Dossier, contexte.Lignes);
        await simRepo.AjouterAsync(sim1);

        var sim2 = SimulationProforma.Creer(
            "SIM-002", societeId, Guid.NewGuid(), DateTime.UtcNow,
            contexte.Grille.Id, "v1.0", contexte.Dossier, contexte.Lignes);
        await simRepo.AjouterAsync(sim2);

        var handler = new ListerSimulationsProformaQueryHandler(simRepo, referentielsRepo, CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.ControleAcces(db));
        var resultat = await handler.HandleAsync(new ListerSimulationsProformaRequete(CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.AppelantDeSociete(db, societeId)));

        Assert.Equal(2, resultat.Count);
        Assert.Equal("SIM-002", resultat[0].Reference); // Plus récente en premier
        Assert.Equal("SIM-001", resultat[1].Reference);
    }
}
