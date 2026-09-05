using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Domain.Tarification.Referentiels;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Tarification;

/// <summary>
/// [TEST-FIRST] Tests de recette contractuels stricts Annexe A.8 (REC-SCE-06).
/// Exige la stricte égalité au franc près sans tolérance.
///
/// CES VALEURS N'ONT PAS BOUGÉ après la normalisation des référentiels et l'extraction des
/// paramètres tarifaires en tables (analyse Merise § 3.3) : c'est précisément ce que ce fichier
/// démontre. Le calcul est le même, seul son paramétrage a cessé d'être écrit en dur.
/// </summary>
public sealed class GoldenMasterProformaTests
{
    private static readonly ReferentielsAnnexeA Referentiels = ParametrageInitialAnnexeA.CreerReferentiels();

    private static VersionGrilleTarifaire CreerGrilleAnnexeA()
    {
        var version = VersionGrilleTarifaire.Creer(
            "v1.0",
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Guid.NewGuid(),
            DateTime.UtcNow,
            ParametrageInitialAnnexeA.FranchiseBranchementJours);

        ParametrageInitialAnnexeA.AppliquerParametres(version, Referentiels);

        var vingt = Referentiels.ParTaille(ParametrageInitialAnnexeA.TailleVingtPieds).Id;
        var quarante = Referentiels.ParTaille(ParametrageInitialAnnexeA.TailleQuarantePieds).Id;

        void Ligne(string sensCode, Guid tailleId, string categorieCode, decimal acconage, decimal relevage, decimal branchement) =>
            version.AjouterLigne(
                Referentiels.ParSens(sensCode).Id,
                tailleId,
                Referentiels.ParCategorie(sensCode, categorieCode).Id,
                acconage,
                relevage,
                branchement);

        // Import Local
        Ligne(ParametrageInitialAnnexeA.SensImportLocal, vingt, "C1", 66800m, 25000m, 35000m);
        Ligne(ParametrageInitialAnnexeA.SensImportLocal, vingt, "C2", 95000m, 25000m, 35000m);
        Ligne(ParametrageInitialAnnexeA.SensImportLocal, vingt, "C3", 140000m, 25000m, 35000m);
        Ligne(ParametrageInitialAnnexeA.SensImportLocal, quarante, "C1", 133600m, 50000m, 67000m);
        Ligne(ParametrageInitialAnnexeA.SensImportLocal, quarante, "C2", 190000m, 50000m, 67000m);
        Ligne(ParametrageInitialAnnexeA.SensImportLocal, quarante, "C3", 280000m, 50000m, 67000m);

        // Export Local
        Ligne(ParametrageInitialAnnexeA.SensExportLocal, vingt, "C1", 69500m, 25000m, 35000m);
        Ligne(ParametrageInitialAnnexeA.SensExportLocal, vingt, "C2", 122500m, 25000m, 35000m);
        Ligne(ParametrageInitialAnnexeA.SensExportLocal, vingt, "C3", 130000m, 25000m, 35000m);
        Ligne(ParametrageInitialAnnexeA.SensExportLocal, quarante, "C1", 139000m, 50000m, 65000m);
        Ligne(ParametrageInitialAnnexeA.SensExportLocal, quarante, "C2", 245000m, 50000m, 65000m);
        Ligne(ParametrageInitialAnnexeA.SensExportLocal, quarante, "C3", 260000m, 50000m, 65000m);

        // Import Transit
        Ligne(ParametrageInitialAnnexeA.SensImportTransit, vingt, "C4", 72230m, 25000m, 35000m);
        Ligne(ParametrageInitialAnnexeA.SensImportTransit, quarante, "C4", 144460m, 50000m, 67000m);

        // Export Transit
        Ligne(ParametrageInitialAnnexeA.SensExportTransit, vingt, "C5", 69000m, 25000m, 35000m);
        Ligne(ParametrageInitialAnnexeA.SensExportTransit, quarante, "C5", 138000m, 50000m, 65000m);

        return version;
    }

    private static DossierSimulation Dossier(
        string sensCode,
        string numeroConteneur,
        string tailleCode,
        string typeCode,
        decimal poidsTonnes,
        bool dangereux,
        bool horsGabarit,
        string categorieCode) =>
        new(
            Referentiels.ParSens(sensCode).Id,
            new DateOnly(2026, 2, 10),
            new DateOnly(2026, 3, 10), // 29 jours
            [
                new ConteneurSimulation(
                    numeroConteneur,
                    Referentiels.ParTaille(tailleCode).Id,
                    Referentiels.ParType(typeCode).Id,
                    poidsTonnes,
                    dangereux,
                    horsGabarit,
                    Referentiels.ParCategorie(sensCode, categorieCode).Id)
            ]);

    [Fact]
    public void GOLDEN_SIM_01_test01_Import_Transit_20_Dry_100t_OOG_Dang_C4()
    {
        // test01 : Import Transit, 20", Dry, 100t, Oui OOG, Oui Dang, C4 => Attendu : 199 129 XOF
        var resultat = new MoteurCalculProforma().Calculer(
            Dossier(ParametrageInitialAnnexeA.SensImportTransit, "CONT01",
                ParametrageInitialAnnexeA.TailleVingtPieds, ParametrageInitialAnnexeA.TypeDry,
                100m, dangereux: true, horsGabarit: true, "C4"),
            CreerGrilleAnnexeA(),
            Referentiels.Index);

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
        var resultat = new MoteurCalculProforma().Calculer(
            Dossier(ParametrageInitialAnnexeA.SensImportLocal, "CONT02",
                ParametrageInitialAnnexeA.TailleQuarantePieds, ParametrageInitialAnnexeA.TypeReefer,
                200m, dangereux: false, horsGabarit: false, "C1"),
            CreerGrilleAnnexeA(),
            Referentiels.Index);

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
        var resultat = new MoteurCalculProforma().Calculer(
            Dossier(ParametrageInitialAnnexeA.SensExportLocal, "CONT03",
                ParametrageInitialAnnexeA.TailleVingtPieds, ParametrageInitialAnnexeA.TypeDry,
                300m, dangereux: false, horsGabarit: true, "C2"),
            CreerGrilleAnnexeA(),
            Referentiels.Index);

        Assert.Equal(245500m, resultat.TotalGeneralXof);
        var ligne = resultat.LignesConteneurs[0];
        Assert.Equal(122500m, ligne.Acconage);
        Assert.Equal(25000m, ligne.Relevage);
        Assert.Equal(0m, ligne.BranchementFrigorifique);
        Assert.Equal(36750m, ligne.MajorationLourd);
        Assert.Equal(0m, ligne.MajorationDangereux);
        Assert.Equal(61250m, ligne.MajorationHorsGabarit);

        // RG-SIM-08 — l'export ne porte aucune franchise, donc aucun stationnement. C'est
        // l'absence de ligne FRANCHISE_STATIONNEMENT qui le dit, jamais un test sur le sens.
        Assert.Equal(0m, ligne.Stationnement);
    }

    [Fact]
    public void GOLDEN_SIM_04_test04_Export_Transit_40_Reefer_100t_Dang_C5()
    {
        // test04 : Export Transit, 40", Reefer, 100t, Non OOG, Oui Dang, C5 => Attendu : 2 053 400 XOF
        var resultat = new MoteurCalculProforma().Calculer(
            Dossier(ParametrageInitialAnnexeA.SensExportTransit, "CONT04",
                ParametrageInitialAnnexeA.TailleQuarantePieds, ParametrageInitialAnnexeA.TypeReefer,
                100m, dangereux: true, horsGabarit: false, "C5"),
            CreerGrilleAnnexeA(),
            Referentiels.Index);

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
        var resultat = new MoteurCalculProforma().Calculer(
            Dossier(ParametrageInitialAnnexeA.SensImportLocal, "CONT06",
                ParametrageInitialAnnexeA.TailleQuarantePieds, ParametrageInitialAnnexeA.TypeReefer,
                300m, dangereux: false, horsGabarit: false, "C3"),
            CreerGrilleAnnexeA(),
            Referentiels.Index);

        Assert.Equal(2245300m, resultat.TotalGeneralXof);
        var ligne = resultat.LignesConteneurs[0];
        Assert.Equal(280000m, ligne.Acconage);
        Assert.Equal(50000m, ligne.Relevage);
        Assert.Equal(1809000m, ligne.BranchementFrigorifique);
        Assert.Equal(84000m, ligne.MajorationLourd);
        Assert.Equal(0m, ligne.MajorationDangereux);
        Assert.Equal(0m, ligne.MajorationHorsGabarit);
        Assert.Equal(22300m, ligne.Stationnement);
        Assert.Equal(6690m, ligne.Part(ParametrageInitialAnnexeA.BeneficiaireCit));
        Assert.Equal(4460m, ligne.Part(ParametrageInitialAnnexeA.BeneficiaireOic));
        Assert.Equal(11150m, ligne.Part(ParametrageInitialAnnexeA.BeneficiairePaa));
    }

    /// <summary>
    /// RG-SIM-11 — la démonstration que le découpage des tranches est devenu une donnée : une
    /// quatrième tranche s'ajoute par paramétrage, et le moteur en tient compte sans qu'une seule
    /// ligne de son code ait changé. C'était impossible tant que les bornes 5 et 15 vivaient dans
    /// <c>Math.Min(5, …)</c> et <c>joursStationnement - 15</c>.
    /// </summary>
    [Fact]
    public void Une_quatrieme_tranche_de_stationnement_sajoute_sans_developpement()
    {
        var grille = VersionGrilleTarifaire.Creer(
            "v2.0", new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), Guid.NewGuid(), DateTime.UtcNow,
            ParametrageInitialAnnexeA.FranchiseBranchementJours);

        var sensId = Referentiels.ParSens(ParametrageInitialAnnexeA.SensImportLocal).Id;
        var tailleId = Referentiels.ParTaille(ParametrageInitialAnnexeA.TailleVingtPieds).Id;

        grille.AjouterLigne(
            sensId, tailleId,
            Referentiels.ParCategorie(ParametrageInitialAnnexeA.SensImportLocal, "C1").Id,
            66800m, 25000m, 35000m);
        grille.DefinirFranchiseStationnement(sensId, 11);

        // Découpage inédit, à quatre tranches, dont une au-delà de 30 jours.
        grille.AjouterTranche(1, 5, 850m);
        grille.AjouterTranche(6, 15, 1250m);
        grille.AjouterTranche(16, 30, 1850m);
        grille.AjouterTranche(31, null, 3000m);

        // 51 jours de séjour − 11 de franchise = 40 jours taxables :
        // 5 × 850 + 10 × 1 250 + 15 × 1 850 + 10 × 3 000 = 4 250 + 12 500 + 27 750 + 30 000.
        var dossier = new DossierSimulation(
            sensId,
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 8, 20),
            [
                new ConteneurSimulation(
                    "CONT99", tailleId,
                    Referentiels.ParType(ParametrageInitialAnnexeA.TypeDry).Id,
                    10m, EstDangereux: false, EstHorsGabarit: false,
                    Referentiels.ParCategorie(ParametrageInitialAnnexeA.SensImportLocal, "C1").Id)
            ]);

        var resultat = new MoteurCalculProforma().Calculer(dossier, grille, Referentiels.Index);

        Assert.Equal(74500m, resultat.LignesConteneurs[0].Stationnement);
    }
}
