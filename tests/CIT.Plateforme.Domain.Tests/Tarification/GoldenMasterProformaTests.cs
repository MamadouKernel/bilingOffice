using CIT.Plateforme.Domain.Tarification;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Tarification;

/// <summary>
/// [TEST-FIRST] Tests de recette contractuels stricts Annexe A.8 (REC-SCE-06).
/// Exige la stricte égalité au franc près sans tolérance.
/// </summary>
public sealed class GoldenMasterProformaTests
{
    private static VersionGrilleTarifaire CreerGrilleAnnexeA()
    {
        var version = VersionGrilleTarifaire.Creer("v1.0", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Guid.NewGuid(), DateTime.UtcNow);

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
    public void GOLDEN_SIM_01_test01_Import_Transit_20_Dry_100t_OOG_Dang_C4()
    {
        // test01 : Import Transit, 20", Dry, 100t, Oui OOG, Oui Dang, C4 => Attendu : 199 129 XOF
        var grille = CreerGrilleAnnexeA();
        var moteur = new MoteurCalculProforma();
        var dossier = new DossierSimulation(
            SensTrafic.ImportTransit,
            new DateOnly(2026, 2, 10),
            new DateOnly(2026, 3, 10), // 29 jours
            [new ConteneurSimulation("CONT01", TailleConteneur.VingtPieds, TypeConteneur.Dry, 100m, EstDangereux: true, EstHorsGabarit: true, "C4")]);

        var resultat = moteur.Calculer(dossier, grille);

        Assert.Equal(199129m, resultat.TotalGeneralXof);
        var ligne = resultat.LignesConteneurs[0];
        Assert.Equal(72230m, ligne.Acconage);
        Assert.Equal(25000m, ligne.Relevage);
        Assert.Equal(0m, ligne.BranchementFrigorifique);
        Assert.Equal(21669m, ligne.MajorationLourd);
        Assert.Equal(36115m, ligne.MajorationDangereux);
        Assert.Equal(36115m, ligne.MajorationHorsGabarit);
        Assert.Equal(8000m, ligne.Stationnement);
    }

    [Fact]
    public void GOLDEN_SIM_02_test02_Import_Local_40_Reefer_200t_C1()
    {
        // test02 : Import Local, 40", Reefer, 200t, Non OOG, Non Dang, C1 => Attendu : 2 054 980 XOF
        var grille = CreerGrilleAnnexeA();
        var moteur = new MoteurCalculProforma();
        var dossier = new DossierSimulation(
            SensTrafic.ImportLocal,
            new DateOnly(2026, 2, 10),
            new DateOnly(2026, 3, 10), // 29 jours
            [new ConteneurSimulation("CONT02", TailleConteneur.QuarantePieds, TypeConteneur.Reefer, 200m, EstDangereux: false, EstHorsGabarit: false, "C1")]);

        var resultat = moteur.Calculer(dossier, grille);

        Assert.Equal(2054980m, resultat.TotalGeneralXof);
        var ligne = resultat.LignesConteneurs[0];
        Assert.Equal(133600m, ligne.Acconage);
        Assert.Equal(50000m, ligne.Relevage);
        Assert.Equal(1809000m, ligne.BranchementFrigorifique);
        Assert.Equal(40080m, ligne.MajorationLourd);
        Assert.Equal(0m, ligne.MajorationDangereux);
        Assert.Equal(0m, ligne.MajorationHorsGabarit);
        Assert.Equal(22300m, ligne.Stationnement);
    }

    [Fact]
    public void GOLDEN_SIM_03_test03_Export_Local_20_Dry_300t_OOG_C2()
    {
        // test03 : Export Local, 20", Dry, 300t, Oui OOG, Non Dang, C2 => Attendu : 245 500 XOF
        var grille = CreerGrilleAnnexeA();
        var moteur = new MoteurCalculProforma();
        var dossier = new DossierSimulation(
            SensTrafic.ExportLocal,
            new DateOnly(2026, 2, 10),
            new DateOnly(2026, 3, 10), // 29 jours
            [new ConteneurSimulation("CONT03", TailleConteneur.VingtPieds, TypeConteneur.Dry, 300m, EstDangereux: false, EstHorsGabarit: true, "C2")]);

        var resultat = moteur.Calculer(dossier, grille);

        Assert.Equal(245500m, resultat.TotalGeneralXof);
        var ligne = resultat.LignesConteneurs[0];
        Assert.Equal(122500m, ligne.Acconage);
        Assert.Equal(25000m, ligne.Relevage);
        Assert.Equal(0m, ligne.BranchementFrigorifique);
        Assert.Equal(36750m, ligne.MajorationLourd);
        Assert.Equal(0m, ligne.MajorationDangereux);
        Assert.Equal(61250m, ligne.MajorationHorsGabarit);
        Assert.Equal(0m, ligne.Stationnement);
    }

    [Fact]
    public void GOLDEN_SIM_04_test04_Export_Transit_40_Reefer_100t_Dang_C5()
    {
        // test04 : Export Transit, 40", Reefer, 100t, Non OOG, Oui Dang, C5 => Attendu : 2 053 400 XOF
        var grille = CreerGrilleAnnexeA();
        var moteur = new MoteurCalculProforma();
        var dossier = new DossierSimulation(
            SensTrafic.ExportTransit,
            new DateOnly(2026, 2, 10),
            new DateOnly(2026, 3, 10), // 29 jours
            [new ConteneurSimulation("CONT04", TailleConteneur.QuarantePieds, TypeConteneur.Reefer, 100m, EstDangereux: true, EstHorsGabarit: false, "C5")]);

        var resultat = moteur.Calculer(dossier, grille);

        Assert.Equal(2053400m, resultat.TotalGeneralXof);
        var ligne = resultat.LignesConteneurs[0];
        Assert.Equal(138000m, ligne.Acconage);
        Assert.Equal(50000m, ligne.Relevage);
        Assert.Equal(1755000m, ligne.BranchementFrigorifique);
        Assert.Equal(41400m, ligne.MajorationLourd);
        Assert.Equal(69000m, ligne.MajorationDangereux);
        Assert.Equal(0m, ligne.MajorationHorsGabarit);
        Assert.Equal(0m, ligne.Stationnement);
    }

    [Fact]
    public void GOLDEN_SIM_05_test06_Import_Local_40_Reefer_300t_C3()
    {
        // test06 : Import Local, 40", Reefer, 300t, Non OOG, Non Dang, C3 => Attendu : 2 245 300 XOF
        var grille = CreerGrilleAnnexeA();
        var moteur = new MoteurCalculProforma();
        var dossier = new DossierSimulation(
            SensTrafic.ImportLocal,
            new DateOnly(2026, 2, 10),
            new DateOnly(2026, 3, 10), // 29 jours
            [new ConteneurSimulation("CONT06", TailleConteneur.QuarantePieds, TypeConteneur.Reefer, 300m, EstDangereux: false, EstHorsGabarit: false, "C3")]);

        var resultat = moteur.Calculer(dossier, grille);

        Assert.Equal(2245300m, resultat.TotalGeneralXof);
        var ligne = resultat.LignesConteneurs[0];
        Assert.Equal(280000m, ligne.Acconage);
        Assert.Equal(50000m, ligne.Relevage);
        Assert.Equal(1809000m, ligne.BranchementFrigorifique);
        Assert.Equal(84000m, ligne.MajorationLourd);
        Assert.Equal(0m, ligne.MajorationDangereux);
        Assert.Equal(0m, ligne.MajorationHorsGabarit);
        Assert.Equal(22300m, ligne.Stationnement);
        Assert.Equal(6690m, ligne.PartCit);
        Assert.Equal(4460m, ligne.PartOic);
        Assert.Equal(11150m, ligne.PartPaa);
    }
}
