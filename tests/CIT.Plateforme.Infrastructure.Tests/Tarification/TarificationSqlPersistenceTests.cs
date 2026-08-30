using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Infrastructure.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Tarification;

public sealed class TarificationSqlPersistenceTests
{
    private static PlateformeDbContext CreerDbContext() => BaseDeTestSql.CreerContexte();

    public static VersionGrilleTarifaire CreerGrilleInitialeV10()
    {
        var version = VersionGrilleTarifaire.Creer(
            "v1.0",
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Guid.NewGuid(),
            DateTime.UtcNow,
            ParametresTarification.CreerDefautAnnexeA());

        // Import Local
        version.AjouterLigne(SensTrafic.ImportLocal, TailleConteneur.VingtPieds, "C1", 66800m, 25000m, 35000m);
        version.AjouterLigne(SensTrafic.ImportLocal, TailleConteneur.VingtPieds, "C2", 95000m, 25000m, 35000m);
        version.AjouterLigne(SensTrafic.ImportLocal, TailleConteneur.VingtPieds, "C3", 140000m, 25000m, 35000m);
        version.AjouterLigne(SensTrafic.ImportLocal, TailleConteneur.QuarantePieds, "C1", 133600m, 50000m, 67000m);
        version.AjouterLigne(SensTrafic.ImportLocal, TailleConteneur.QuarantePieds, "C2", 190000m, 50000m, 67000m);
        version.AjouterLigne(SensTrafic.ImportLocal, TailleConteneur.QuarantePieds, "C3", 280000m, 50000m, 67000m);

        // Export Local
        version.AjouterLigne(SensTrafic.ExportLocal, TailleConteneur.VingtPieds, "C1", 69500m, 25000m, 35000m);
        version.AjouterLigne(SensTrafic.ExportLocal, TailleConteneur.VingtPieds, "C2", 122500m, 25000m, 35000m);
        version.AjouterLigne(SensTrafic.ExportLocal, TailleConteneur.VingtPieds, "C3", 130000m, 25000m, 35000m);
        version.AjouterLigne(SensTrafic.ExportLocal, TailleConteneur.QuarantePieds, "C1", 139000m, 50000m, 65000m);
        version.AjouterLigne(SensTrafic.ExportLocal, TailleConteneur.QuarantePieds, "C2", 245000m, 50000m, 65000m);
        version.AjouterLigne(SensTrafic.ExportLocal, TailleConteneur.QuarantePieds, "C3", 260000m, 50000m, 65000m);

        // Import Transit
        version.AjouterLigne(SensTrafic.ImportTransit, TailleConteneur.VingtPieds, "C4", 72230m, 25000m, 35000m);
        version.AjouterLigne(SensTrafic.ImportTransit, TailleConteneur.QuarantePieds, "C4", 144460m, 50000m, 67000m);

        // Export Transit
        version.AjouterLigne(SensTrafic.ExportTransit, TailleConteneur.VingtPieds, "C5", 69000m, 25000m, 35000m);
        version.AjouterLigne(SensTrafic.ExportTransit, TailleConteneur.QuarantePieds, "C5", 138000m, 50000m, 65000m);

        return version;
    }

    [Fact]
    public async Task Persistance_et_relecture_grille_tarifaire_et_ses_16_lignes()
    {
        using var db = CreerDbContext();
        var repo = new VersionGrilleTarifaireRepository(db);

        var grille = CreerGrilleInitialeV10();
        await repo.AjouterAsync(grille);

        var relecture = await repo.ObtenirParIdAsync(grille.Id);
        Assert.NotNull(relecture);
        Assert.Equal("v1.0", relecture.NumeroVersion);
        Assert.Equal(16, relecture.Lignes.Count);
        Assert.Equal(0.30m, relecture.Parametres.TauxMajorationLourd);
    }

    [Fact]
    public async Task ObtenirActiveADateUtcAsync_selectionne_la_bonne_version()
    {
        using var db = CreerDbContext();
        var repo = new VersionGrilleTarifaireRepository(db);

        var g1 = VersionGrilleTarifaire.Creer("v1.0", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Guid.NewGuid(), DateTime.UtcNow);
        var g2 = VersionGrilleTarifaire.Creer("v2.0", new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), Guid.NewGuid(), DateTime.UtcNow);

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

        var grille = CreerGrilleInitialeV10();
        var moteur = new MoteurCalculProforma();
        var dossier = new DossierSimulation(
            SensTrafic.ImportTransit,
            new DateOnly(2026, 2, 10),
            new DateOnly(2026, 3, 10),
            [new ConteneurSimulation("CONT01", TailleConteneur.VingtPieds, TypeConteneur.Dry, 100m, true, true, "C4")]);

        var res = moteur.Calculer(dossier, grille);
        var societeId = Guid.NewGuid();
        var auteurId = Guid.NewGuid();

        var sim = SimulationProforma.Creer(
            "SIM-20260830-0001",
            societeId,
            auteurId,
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
    }

    /// <summary>
    /// [TEST-FIRST] (brief d'implémentation §30) : les Golden Masters de l'Annexe A.8 doivent produire
    /// exactement le même montant qu'ils soient calculés sur une grille en mémoire (Domain.Tests) OU
    /// sur une grille rechargée depuis SQL Server réel — afin de détecter une mauvaise précision SQL,
    /// un mauvais mapping EF Core, ou le chargement d'une mauvaise version. Zéro tolérance : égalité
    /// stricte, comme dans GoldenMasterProformaTests.
    /// </summary>
    [Theory]
    [InlineData("ImportTransit", "VingtPieds", "Dry", 100, true, true, "C4", 199129)]
    [InlineData("ImportLocal", "QuarantePieds", "Reefer", 200, false, false, "C1", 2054980)]
    [InlineData("ExportLocal", "VingtPieds", "Dry", 300, false, true, "C2", 245500)]
    [InlineData("ExportTransit", "QuarantePieds", "Reefer", 100, true, false, "C5", 2053400)]
    [InlineData("ImportLocal", "QuarantePieds", "Reefer", 300, false, false, "C3", 2245300)]
    public async Task GoldenMaster_apres_rechargement_depuis_SqlServer_reel_produit_le_meme_montant(
        string sensStr, string tailleStr, string typeStr, decimal poids, bool dangereux, bool oog, string categorie, decimal totalAttendu)
    {
        using var db = CreerDbContext();
        var repo = new VersionGrilleTarifaireRepository(db);

        var grilleInitiale = CreerGrilleInitialeV10();
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

        var sens = Enum.Parse<SensTrafic>(sensStr);
        var taille = Enum.Parse<TailleConteneur>(tailleStr);
        var type = Enum.Parse<TypeConteneur>(typeStr);

        var moteur = new MoteurCalculProforma();
        var dossier = new DossierSimulation(
            sens,
            new DateOnly(2026, 2, 10),
            new DateOnly(2026, 3, 10),
            [new ConteneurSimulation("CONT-SQL", taille, type, poids, dangereux, oog, categorie)]);

        var resultat = moteur.Calculer(dossier, grilleRechargee);

        Assert.Equal(totalAttendu, resultat.TotalGeneralXof);
    }
}
