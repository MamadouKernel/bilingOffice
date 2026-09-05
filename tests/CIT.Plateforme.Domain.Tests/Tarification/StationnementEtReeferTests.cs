using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Domain.Tarification.Exceptions;
using Xunit;

using A = CIT.Plateforme.Domain.Tarification.ParametrageInitialAnnexeA;

namespace CIT.Plateforme.Domain.Tests.Tarification;

/// <summary>
/// [TEST-FIRST] Tests unitaires pour la durée de séjour, le stationnement par tranches et le
/// branchement Reefer (SIM-04).
///
/// Le branchement ne se décide plus sur le nom du type (<c>Type == TypeConteneur.Reefer</c>) mais
/// sur l'attribut <c>DeclencheBranchementFrigorifique</c> du référentiel (analyse Merise § 3.3).
/// </summary>
public sealed class StationnementEtReeferTests
{
    private static VersionGrilleTarifaire CreerGrilleAvecReefer()
    {
        var grille = GrilleDeTest.NouvelleGrille();
        grille.AjouterLigneCodes(A.SensImportLocal, A.TailleVingtPieds, "C1", 100000m, 20000m, 35000m);
        grille.AjouterLigneCodes(A.SensImportTransit, A.TailleVingtPieds, "C4", 100000m, 20000m, 35000m);
        grille.AjouterLigneCodes(A.SensExportLocal, A.TailleVingtPieds, "C1", 100000m, 20000m, 35000m);
        return grille;
    }

    [Theory]
    [InlineData("2026-02-10", "2026-02-10", 1)] // Même jour = 1 jour
    [InlineData("2026-02-10", "2026-02-11", 2)] // Lendemain = 2 jours
    [InlineData("2026-02-10", "2026-03-10", 29)] // 29 jours (Annexe A.8)
    [InlineData("2025-12-31", "2026-01-01", 2)] // Changement d'année
    public void Duree_de_sejour_est_calculee_bornes_incluses(string debut, string fin, int joursAttendus)
    {
        var dossier = GrilleDeTest.Dossier(
            A.SensImportLocal,
            DateOnly.Parse(debut),
            DateOnly.Parse(fin),
            GrilleDeTest.Conteneur(A.SensImportLocal, A.TailleVingtPieds, A.TypeDry, 10m));

        Assert.Equal(joursAttendus, dossier.DureeSejourJours);
    }

    [Theory]
    [InlineData(1, 0)]          // 1 jour <= 2j franchise -> 0 XOF
    [InlineData(2, 0)]          // 2 jours = franchise -> 0 XOF
    [InlineData(3, 35000)]      // 3 jours -> (3 - 2) * 35 000 = 35 000 XOF
    [InlineData(10, 280000)]    // 10 jours -> (10 - 2) * 35 000 = 280 000 XOF
    public void Branchement_reefer_deduit_franchise_2_jours(int dureeSejour, decimal montantAttendu)
    {
        var dateDebut = new DateOnly(2026, 1, 1);
        var dossier = GrilleDeTest.Dossier(
            A.SensImportLocal,
            dateDebut,
            dateDebut.AddDays(dureeSejour - 1),
            GrilleDeTest.Conteneur(A.SensImportLocal, A.TailleVingtPieds, A.TypeReefer, 10m));

        var res = new MoteurCalculProforma().Calculer(dossier, CreerGrilleAvecReefer(), GrilleDeTest.Index);

        Assert.Equal(montantAttendu, res.LignesConteneurs[0].BranchementFrigorifique);
    }

    [Fact]
    public void Conteneur_Dry_aucun_branchement_frigorifique_meme_avec_long_sejour()
    {
        var dossier = GrilleDeTest.Dossier(
            A.SensImportLocal,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 30),
            GrilleDeTest.Conteneur(A.SensImportLocal, A.TailleVingtPieds, A.TypeDry, 10m));

        var res = new MoteurCalculProforma().Calculer(dossier, CreerGrilleAvecReefer(), GrilleDeTest.Index);

        Assert.Equal(0m, res.LignesConteneurs[0].BranchementFrigorifique);
    }

    /// <summary>
    /// La combinaison est désormais toujours <b>valide</b> — sens, taille et catégorie sont
    /// choisis dans des référentiels — mais elle peut ne pas être <b>tarifée</b>. C'est ce trou
    /// dans la grille que l'exception signale maintenant, et plus une faute de frappe du client.
    /// </summary>
    [Fact]
    public void Combinaison_non_tarifee_declenche_TarifNonTrouveException()
    {
        var dossier = GrilleDeTest.Dossier(
            A.SensImportLocal,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 5),
            GrilleDeTest.Conteneur(A.SensImportLocal, A.TailleQuarantePieds, A.TypeDry, 10m));

        var grille = CreerGrilleAvecReefer();

        var exception = Assert.Throws<TarifNonTrouveException>(
            () => new MoteurCalculProforma().Calculer(dossier, grille, GrilleDeTest.Index));

        Assert.Equal(A.TailleQuarantePieds, exception.Taille.Code);
        Assert.Equal("C1", exception.Categorie.Code);
    }

    /// <summary>
    /// RG-SIM-08 — l'absence de franchise pour un sens vaut exclusion du stationnement. Vérifié
    /// sur l'export local, que l'Annexe A ne dote d'aucune franchise.
    /// </summary>
    [Fact]
    public void Un_sens_sans_franchise_enregistree_nest_pas_soumis_au_stationnement()
    {
        var dossier = GrilleDeTest.Dossier(
            A.SensExportLocal,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 3, 1),
            GrilleDeTest.Conteneur(A.SensExportLocal, A.TailleVingtPieds, A.TypeDry, 10m));

        var res = new MoteurCalculProforma().Calculer(dossier, CreerGrilleAvecReefer(), GrilleDeTest.Index);

        Assert.Equal(0m, res.LignesConteneurs[0].Stationnement);
    }
}
