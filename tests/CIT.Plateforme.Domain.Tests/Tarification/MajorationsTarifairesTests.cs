using CIT.Plateforme.Domain.Tarification;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Tarification;

/// <summary>
/// [TEST-FIRST] Tests unitaires des majorations tarifaires et seuils limites (SIM-02).
/// </summary>
public sealed class MajorationsTarifairesTests
{
    private static VersionGrilleTarifaire CreerGrilleSimple()
    {
        var grille = VersionGrilleTarifaire.Creer("v1.0", DateTime.UtcNow.AddDays(-1), Guid.NewGuid(), DateTime.UtcNow);
        grille.AjouterLigne(SensTrafic.ImportLocal, TailleConteneur.VingtPieds, "C1", 100000m, 20000m, 0m);
        grille.AjouterLigne(SensTrafic.ImportLocal, TailleConteneur.QuarantePieds, "C1", 200000m, 40000m, 0m);
        return grille;
    }

    [Theory]
    [InlineData(14.99, 0)]      // Sous le seuil 15t
    [InlineData(15.00, 0)]      // Au seuil exact 15t (non lourd car > 15t requis)
    [InlineData(15.01, 30000)]   // Au-dessus du seuil 15t (30% de 100 000)
    public void Majoration_lourd_20_pieds_respecte_seuil_strictement_superieur(decimal poids, decimal montantAttendu)
    {
        var grille = CreerGrilleSimple();
        var moteur = new MoteurCalculProforma();
        var dossier = new DossierSimulation(
            SensTrafic.ImportLocal,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 1),
            [new ConteneurSimulation("C1", TailleConteneur.VingtPieds, TypeConteneur.Dry, poids, EstDangereux: false, EstHorsGabarit: false, "C1")]);

        var res = moteur.Calculer(dossier, grille);
        Assert.Equal(montantAttendu, res.LignesConteneurs[0].MajorationLourd);
    }

    [Theory]
    [InlineData(25.99, 0)]
    [InlineData(26.00, 0)]
    [InlineData(26.01, 60000)] // 30% de 200 000
    public void Majoration_lourd_40_pieds_respecte_seuil_26t(decimal poids, decimal montantAttendu)
    {
        var grille = CreerGrilleSimple();
        var moteur = new MoteurCalculProforma();
        var dossier = new DossierSimulation(
            SensTrafic.ImportLocal,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 1),
            [new ConteneurSimulation("C1", TailleConteneur.QuarantePieds, TypeConteneur.Dry, poids, EstDangereux: false, EstHorsGabarit: false, "C1")]);

        var res = moteur.Calculer(dossier, grille);
        Assert.Equal(montantAttendu, res.LignesConteneurs[0].MajorationLourd);
    }

    [Fact]
    public void Majorations_cumul_additif_sur_acconage_sans_effet_de_cascade()
    {
        var grille = CreerGrilleSimple();
        var moteur = new MoteurCalculProforma();
        // 20 pieds, acconage = 100 000. Lourd (>15t) -> +30 000. Dangereux -> +50 000. OOG -> +50 000.
        var dossier = new DossierSimulation(
            SensTrafic.ImportLocal,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 1),
            [new ConteneurSimulation("C1", TailleConteneur.VingtPieds, TypeConteneur.Dry, 20m, EstDangereux: true, EstHorsGabarit: true, "C1")]);

        var res = moteur.Calculer(dossier, grille);
        var ligne = res.LignesConteneurs[0];

        Assert.Equal(30000m, ligne.MajorationLourd);
        Assert.Equal(50000m, ligne.MajorationDangereux);
        Assert.Equal(50000m, ligne.MajorationHorsGabarit);
        // Total = 100 000 (acc) + 20 000 (rel) + 130 000 (maj) + 0 (stat) = 250 000
        Assert.Equal(250000m, ligne.TotalLigne);
    }
}
