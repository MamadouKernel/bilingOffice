using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Domain.Tarification.Parametres;
using CIT.Plateforme.Domain.Tarification.Referentiels;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Infrastructure.Tests.Support;
using Xunit;

using A = CIT.Plateforme.Domain.Tarification.ParametrageInitialAnnexeA;

namespace CIT.Plateforme.Infrastructure.Tests.Tarification;

public sealed class TarificationSqlPersistenceTests
{
    private static PlateformeDbContext CreerDbContext() => BaseDeTestSql.CreerContexte();

    private static async Task<ReferentielsTarifaires> ReferentielsAsync(PlateformeDbContext db) =>
        await new ReferentielsTarifairesRepository(db).ChargerAsync();

    public static VersionGrilleTarifaire CreerGrilleInitialeV10(ReferentielsTarifaires referentiels)
    {
        var version = VersionGrilleTarifaire.Creer(
            "v1.0",
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Guid.NewGuid(),
            DateTime.UtcNow,
            A.FranchiseBranchementJours);

        ParametrageInitialAnnexeA.AppliquerParametres(version, referentiels);

        void Ligne(string sensCode, string tailleCode, string categorieCode, decimal acconage, decimal relevage, decimal branchement)
        {
            var sens = referentiels.SensParCode(sensCode)!;
            version.AjouterLigne(
                sens.Id,
                referentiels.TailleParCode(tailleCode)!.Id,
                referentiels.CategorieParCode(sens.Id, categorieCode)!.Id,
                acconage, relevage, branchement);
        }

        // Import Local
        Ligne(A.SensImportLocal, A.TailleVingtPieds, "C1", 66800m, 25000m, 35000m);
        Ligne(A.SensImportLocal, A.TailleVingtPieds, "C2", 95000m, 25000m, 35000m);
        Ligne(A.SensImportLocal, A.TailleVingtPieds, "C3", 140000m, 25000m, 35000m);
        Ligne(A.SensImportLocal, A.TailleQuarantePieds, "C1", 133600m, 50000m, 67000m);
        Ligne(A.SensImportLocal, A.TailleQuarantePieds, "C2", 190000m, 50000m, 67000m);
        Ligne(A.SensImportLocal, A.TailleQuarantePieds, "C3", 280000m, 50000m, 67000m);

        // Export Local
        Ligne(A.SensExportLocal, A.TailleVingtPieds, "C1", 69500m, 25000m, 35000m);
        Ligne(A.SensExportLocal, A.TailleVingtPieds, "C2", 122500m, 25000m, 35000m);
        Ligne(A.SensExportLocal, A.TailleVingtPieds, "C3", 130000m, 25000m, 35000m);
        Ligne(A.SensExportLocal, A.TailleQuarantePieds, "C1", 139000m, 50000m, 65000m);
        Ligne(A.SensExportLocal, A.TailleQuarantePieds, "C2", 245000m, 50000m, 65000m);
        Ligne(A.SensExportLocal, A.TailleQuarantePieds, "C3", 260000m, 50000m, 65000m);

        // Import Transit
        Ligne(A.SensImportTransit, A.TailleVingtPieds, "C4", 72230m, 25000m, 35000m);
        Ligne(A.SensImportTransit, A.TailleQuarantePieds, "C4", 144460m, 50000m, 67000m);

        // Export Transit
        Ligne(A.SensExportTransit, A.TailleVingtPieds, "C5", 69000m, 25000m, 35000m);
        Ligne(A.SensExportTransit, A.TailleQuarantePieds, "C5", 138000m, 50000m, 65000m);

        return version;
    }

    [Fact]
    public async Task Persistance_et_relecture_grille_tarifaire_et_ses_16_lignes()
    {
        using var db = CreerDbContext();
        var repo = new VersionGrilleTarifaireRepository(db);

        var grille = CreerGrilleInitialeV10(await ReferentielsAsync(db));
        await repo.AjouterAsync(grille);

        var relecture = await repo.ObtenirParIdAsync(grille.Id);
        Assert.NotNull(relecture);
        Assert.Equal("v1.0", relecture.NumeroVersion);
        Assert.Equal(16, relecture.Lignes.Count);

        // Les paramètres sont désormais des tables (analyse Merise § 3.3) : leur relecture doit
        // être vérifiée explicitement, car un Include oublié les rendrait simplement vides — le
        // moteur calculerait alors des montants nuls sans lever d'erreur.
        Assert.Equal(0.30m, relecture.TauxMajoration(TypeMajoration.Lourd));
        Assert.Equal(3, relecture.Tranches.Count);
        Assert.Equal(2, relecture.SeuilsPoids.Count);
        Assert.Equal(2, relecture.Franchises.Count);
        Assert.Equal(3, relecture.QuotesParts.Count);
    }

    [Fact]
    public async Task ObtenirActiveADateUtcAsync_selectionne_la_bonne_version()
    {
        using var db = CreerDbContext();
        var repo = new VersionGrilleTarifaireRepository(db);

        var g1 = VersionGrilleTarifaire.Creer(
            "v1.0", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Guid.NewGuid(), DateTime.UtcNow, A.FranchiseBranchementJours);
        var g2 = VersionGrilleTarifaire.Creer(
            "v2.0", new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), Guid.NewGuid(), DateTime.UtcNow, A.FranchiseBranchementJours);

        await repo.AjouterAsync(g1);
        await repo.AjouterAsync(g2);

        var activeEnMars = await repo.ObtenirActiveADateUtcAsync(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        Assert.NotNull(activeEnMars);
        Assert.Equal("v1.0", activeEnMars.NumeroVersion);

        var activeEnJuillet = await repo.ObtenirActiveADateUtcAsync(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc));
        Assert.NotNull(activeEnJuillet);
        Assert.Equal("v2.0", activeEnJuillet.NumeroVersion);
    }

    [Fact]
    public async Task Persistance_et_relecture_simulation_proforma_snapshot()
    {
        using var db = CreerDbContext();
        var repo = new SimulationProformaRepository(db);
        var referentiels = await ReferentielsAsync(db);

        var grille = CreerGrilleInitialeV10(referentiels);

        var sens = referentiels.SensParCode(A.SensImportTransit)!;
        var dossier = new DossierSimulation(
            sens.Id,
            new DateOnly(2026, 2, 10),
            new DateOnly(2026, 3, 10),
            [
                new ConteneurSimulation(
                    "CONT01",
                    referentiels.TailleParCode(A.TailleVingtPieds)!.Id,
                    referentiels.TypeParCode(A.TypeDry)!.Id,
                    100m, true, true,
                    referentiels.CategorieParCode(sens.Id, "C4")!.Id)
            ]);

        var res = new MoteurCalculProforma().Calculer(dossier, grille, referentiels);

        var sim = SimulationProforma.Creer(
            "SIM-20260830-0001",
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            grille.Id,
            grille.NumeroVersion,
            dossier,
            res.LignesConteneurs);

        await repo.AjouterAsync(sim);

        var relecture = await repo.ObtenirParIdAsync(sim.Id);
        Assert.NotNull(relecture);
        Assert.Equal("SIM-20260830-0001", relecture.Reference);
        Assert.Equal(199129m, relecture.MontantTotalXof);
        Assert.Single(relecture.LignesConteneurs);
        Assert.Equal(72230m, relecture.LignesConteneurs.First().MontantAcconage);

        // RG-SIM-09 — la ventilation entre bénéficiaires est un enfant de la ligne conteneur
        // depuis que les bénéficiaires sont paramétrables ; elle doit revenir avec elle.
        Assert.Equal(3, relecture.LignesConteneurs.First().RepartitionStationnement.Count);
        Assert.Equal(2400m, relecture.LignesConteneurs.First().Part(A.BeneficiaireCit));
    }

    /// <summary>
    /// [TEST-FIRST] (brief d'implémentation §30) : les Golden Masters de l'Annexe A.8 doivent produire
    /// exactement le même montant qu'ils soient calculés sur une grille en mémoire (Domain.Tests) OU
    /// sur une grille rechargée depuis SQL Server réel — afin de détecter une mauvaise précision SQL,
    /// un mauvais mapping EF Core, ou le chargement d'une mauvaise version. Zéro tolérance : égalité
    /// stricte, comme dans GoldenMasterProformaTests.
    ///
    /// CE TEST PREND UNE IMPORTANCE NOUVELLE depuis que les paramètres tarifaires sont des tables
    /// (analyse Merise § 3.3) : c'est lui qui échouerait si le dépôt oubliait de les charger avec la
    /// grille, cas où le moteur produirait des montants nuls sans lever la moindre erreur.
    /// </summary>
    [Theory]
    [InlineData(A.SensImportTransit, A.TailleVingtPieds, A.TypeDry, 100, true, true, "C4", 199129)]
    [InlineData(A.SensImportLocal, A.TailleQuarantePieds, A.TypeReefer, 200, false, false, "C1", 2054980)]
    [InlineData(A.SensExportLocal, A.TailleVingtPieds, A.TypeDry, 300, false, true, "C2", 245500)]
    [InlineData(A.SensExportTransit, A.TailleQuarantePieds, A.TypeReefer, 100, true, false, "C5", 2053400)]
    [InlineData(A.SensImportLocal, A.TailleQuarantePieds, A.TypeReefer, 300, false, false, "C3", 2245300)]
    public async Task GoldenMaster_apres_rechargement_depuis_SqlServer_reel_produit_le_meme_montant(
        string sensCode, string tailleCode, string typeCode, decimal poids, bool dangereux, bool oog, string categorieCode, decimal totalAttendu)
    {
        using var db = CreerDbContext();
        var repo = new VersionGrilleTarifaireRepository(db);

        var grilleInitiale = CreerGrilleInitialeV10(await ReferentielsAsync(db));
        await repo.AjouterAsync(grilleInitiale);

        // Rechargement intégral depuis SQL Server (nouvelle instance de contexte, donc nouveau
        // change tracker EF Core sans aucune entité en cache) : la grille utilisée par le moteur
        // n'est plus l'objet en mémoire créé ci-dessus mais bien celle reconstruite depuis les
        // lignes et colonnes de la même base SQL Server réelle.
        using var dbRelecture = BaseDeTestSql.CreerContexte();
        var repoRelecture = new VersionGrilleTarifaireRepository(dbRelecture);
        var grilleRechargee = await repoRelecture.ObtenirParIdAsync(grilleInitiale.Id);
        Assert.NotNull(grilleRechargee);
        Assert.Equal(16, grilleRechargee.Lignes.Count);

        var referentielsRelus = await ReferentielsAsync(dbRelecture);
        var sens = referentielsRelus.SensParCode(sensCode)!;

        var dossier = new DossierSimulation(
            sens.Id,
            new DateOnly(2026, 2, 10),
            new DateOnly(2026, 3, 10),
            [
                new ConteneurSimulation(
                    "CONT-SQL",
                    referentielsRelus.TailleParCode(tailleCode)!.Id,
                    referentielsRelus.TypeParCode(typeCode)!.Id,
                    poids, dangereux, oog,
                    referentielsRelus.CategorieParCode(sens.Id, categorieCode)!.Id)
            ]);

        var resultat = new MoteurCalculProforma().Calculer(dossier, grilleRechargee, referentielsRelus);

        Assert.Equal(totalAttendu, resultat.TotalGeneralXof);
    }
}
