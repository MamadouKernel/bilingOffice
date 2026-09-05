using CIT.Plateforme.Application.Tarification;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Domain.Tarification.Referentiels;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

using A = CIT.Plateforme.Domain.Tarification.ParametrageInitialAnnexeA;

namespace CIT.Plateforme.Application.Tests.Tarification;

public sealed class CloisonnementSimulationProformaTests
{
    /// <summary>Grille persistée + résultat calculé, de quoi produire des simulations réalistes
    /// sans que chaque test redécrive tout le paramétrage.</summary>
    private static async Task<(VersionGrilleTarifaire Grille, DossierSimulation Dossier, IReadOnlyList<LigneResultatConteneur> Lignes)>
        PreparerAsync(PlateformeDbContext db, ReferentielsTarifaires referentiels)
    {
        var grilleRepo = new VersionGrilleTarifaireRepository(db);

        var grille = VersionGrilleTarifaire.Creer(
            "v1.0", DateTime.UtcNow.AddDays(-1), Guid.NewGuid(), DateTime.UtcNow, A.FranchiseBranchementJours);
        ParametrageInitialAnnexeA.AppliquerParametres(grille, referentiels);

        var sens = referentiels.SensParCode(A.SensImportLocal)!;
        var taille = referentiels.TailleParCode(A.TailleVingtPieds)!;
        var type = referentiels.TypeParCode(A.TypeDry)!;
        var categorie = referentiels.CategorieParCode(sens.Id, "C1")!;

        grille.AjouterLigne(sens.Id, taille.Id, categorie.Id, 66800m, 25000m, 35000m);
        await grilleRepo.AjouterAsync(grille);

        var dossier = new DossierSimulation(
            sens.Id,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 5),
            [new ConteneurSimulation("C1", taille.Id, type.Id, 10m, false, false, categorie.Id)]);

        var resultat = new MoteurCalculProforma().Calculer(dossier, grille, referentiels);
        return (grille, dossier, resultat.LignesConteneurs);
    }

    [Fact]
    public async Task ListerSimulations_filtre_strictement_par_societe_de_l_appelant()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var referentiels = ReferentielsDeTest.Amorcer(db);
        var simRepo = new SimulationProformaRepository(db);
        var referentielsRepo = new ReferentielsTarifairesRepository(db);

        var societeA = Guid.NewGuid();
        var societeB = Guid.NewGuid();

        var (grille, dossier, lignes) = await PreparerAsync(db, referentiels);

        var simA = SimulationProforma.Creer("SIM-A", societeA, Guid.NewGuid(), DateTime.UtcNow, grille.Id, "v1.0", dossier, lignes);
        await simRepo.AjouterAsync(simA);

        var simB = SimulationProforma.Creer("SIM-B", societeB, Guid.NewGuid(), DateTime.UtcNow, grille.Id, "v1.0", dossier, lignes);
        await simRepo.AjouterAsync(simB);

        // Lister pour sociétéA → ne retourne que SIM-A
        var handler = new ListerSimulationsProformaQueryHandler(simRepo, referentielsRepo, CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.ControleAcces(db));
        var resultat = await handler.HandleAsync(new ListerSimulationsProformaRequete(CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.AppelantDeSociete(db, societeA)));

        Assert.Single(resultat);
        Assert.Equal("SIM-A", resultat[0].Reference);
        Assert.Equal(A.SensImportLocal, resultat[0].Sens.Code);
    }

    [Fact]
    public async Task ConsulterSimulation_d_une_autre_societe_leve_UnauthorizedAccessException()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var referentiels = ReferentielsDeTest.Amorcer(db);
        var simRepo = new SimulationProformaRepository(db);
        var grilleRepo = new VersionGrilleTarifaireRepository(db);
        var referentielsRepo = new ReferentielsTarifairesRepository(db);

        var societeA = Guid.NewGuid();
        var societeB = Guid.NewGuid();

        var (grille, dossier, lignes) = await PreparerAsync(db, referentiels);

        var simA = SimulationProforma.Creer("SIM-IDOR", societeA, Guid.NewGuid(), DateTime.UtcNow, grille.Id, "v1.0", dossier, lignes);
        await simRepo.AjouterAsync(simA);

        var handler = new ConsulterSimulationProformaQueryHandler(simRepo, grilleRepo, referentielsRepo, CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.ControleAcces(db));

        // SociétéB tente de consulter la simulation de sociétéA → IDOR refusé
        await Assert.ThrowsAsync<CIT.Plateforme.Application.Securite.AccesRefuseException>(
            () => handler.HandleAsync(new ConsulterSimulationProformaRequete(CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.AppelantDeSociete(db, societeB), simA.Id)));
    }
}
