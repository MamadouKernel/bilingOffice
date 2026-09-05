using CIT.Plateforme.Domain.Tarification;
using Xunit;

using A = CIT.Plateforme.Domain.Tarification.ParametrageInitialAnnexeA;

namespace CIT.Plateforme.Domain.Tests.Tarification;

/// <summary>
/// [TEST-FIRST] Tests unitaires des majorations tarifaires et seuils limites (SIM-02).
///
/// Les seuils de poids ne sont plus deux colonnes nommées par la taille : ils viennent de
/// <c>SEUIL_POIDS_MAJORATION</c>, une ligne par couple grille/taille (analyse Merise § 3.3,
/// correction n° 2). Les valeurs vérifiées ici sont inchangées — 15 t en 20", 26 t en 40".
/// </summary>
public sealed class MajorationsTarifairesTests
{
    private static VersionGrilleTarifaire CreerGrilleSimple()
    {
        var grille = GrilleDeTest.NouvelleGrille();
        grille.AjouterLigneCodes(A.SensImportLocal, A.TailleVingtPieds, "C1", 100000m, 20000m, 0m);
        grille.AjouterLigneCodes(A.SensImportLocal, A.TailleQuarantePieds, "C1", 200000m, 40000m, 0m);
        return grille;
    }

    [Theory]
    [InlineData(14.99, 0)]      // Sous le seuil 15t
    [InlineData(15.00, 0)]      // Au seuil exact 15t (non lourd car > 15t requis)
    [InlineData(15.01, 30000)]   // Au-dessus du seuil 15t (30% de 100 000)
    public void Majoration_lourd_20_pieds_respecte_seuil_strictement_superieur(decimal poids, decimal montantAttendu)
    {
        var dossier = GrilleDeTest.Dossier(
            A.SensImportLocal,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 1),
            GrilleDeTest.Conteneur(A.SensImportLocal, A.TailleVingtPieds, A.TypeDry, poids));

        var res = new MoteurCalculProforma().Calculer(dossier, CreerGrilleSimple(), GrilleDeTest.Index);

        Assert.Equal(montantAttendu, res.LignesConteneurs[0].MajorationLourd);
    }

    [Theory]
    [InlineData(25.99, 0)]
    [InlineData(26.00, 0)]
    [InlineData(26.01, 60000)] // 30% de 200 000
    public void Majoration_lourd_40_pieds_respecte_seuil_26t(decimal poids, decimal montantAttendu)
    {
        var dossier = GrilleDeTest.Dossier(
            A.SensImportLocal,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 1),
            GrilleDeTest.Conteneur(A.SensImportLocal, A.TailleQuarantePieds, A.TypeDry, poids));

        var res = new MoteurCalculProforma().Calculer(dossier, CreerGrilleSimple(), GrilleDeTest.Index);

        Assert.Equal(montantAttendu, res.LignesConteneurs[0].MajorationLourd);
    }

    [Fact]
    public void Majorations_cumul_additif_sur_acconage_sans_effet_de_cascade()
    {
        // 20 pieds, acconage = 100 000. Lourd (>15t) -> +30 000. Dangereux -> +50 000. OOG -> +50 000.
        var dossier = GrilleDeTest.Dossier(
            A.SensImportLocal,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 1),
            GrilleDeTest.Conteneur(A.SensImportLocal, A.TailleVingtPieds, A.TypeDry, 20m,
                dangereux: true, horsGabarit: true));

        var ligne = new MoteurCalculProforma()
            .Calculer(dossier, CreerGrilleSimple(), GrilleDeTest.Index)
            .LignesConteneurs[0];

        Assert.Equal(30000m, ligne.MajorationLourd);
        Assert.Equal(50000m, ligne.MajorationDangereux);
        Assert.Equal(50000m, ligne.MajorationHorsGabarit);
        // Total = 100 000 (acc) + 20 000 (rel) + 130 000 (maj) + 0 (stat) = 250 000
        Assert.Equal(250000m, ligne.TotalLigne);
    }

    /// <summary>
    /// RG-SIM-06 — le seuil suit désormais la taille par une ligne de paramétrage. Une taille
    /// dont le seuil est relevé change de comportement sans qu'aucun code ne bouge ; c'était
    /// impossible tant que « 20 pieds » et « 40 pieds » étaient des noms de colonnes.
    /// </summary>
    [Fact]
    public void Le_seuil_de_poids_dune_taille_est_administrable()
    {
        var grille = CreerGrilleSimple();
        grille.DefinirSeuilPoids(GrilleDeTest.Taille(A.TailleVingtPieds), 50m);

        var dossier = GrilleDeTest.Dossier(
            A.SensImportLocal,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 1),
            GrilleDeTest.Conteneur(A.SensImportLocal, A.TailleVingtPieds, A.TypeDry, 20m));

        var res = new MoteurCalculProforma().Calculer(dossier, grille, GrilleDeTest.Index);

        // 20 t dépassait l'ancien seuil de 15 t ; il passe sous le nouveau seuil de 50 t.
        Assert.Equal(0m, res.LignesConteneurs[0].MajorationLourd);
    }
}
