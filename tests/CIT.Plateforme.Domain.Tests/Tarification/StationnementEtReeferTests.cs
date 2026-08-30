using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Domain.Tarification.Exceptions;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Tarification;

/// <summary>
/// [TEST-FIRST] Tests unitaires pour la durée de séjour, le stationnement par tranches et le branchement Reefer (SIM-04).
/// </summary>
public sealed class StationnementEtReeferTests
{
    private static VersionGrilleTarifaire CreerGrilleAvecReefer()
    {
        var grille = VersionGrilleTarifaire.Creer("v1.0", DateTime.UtcNow.AddDays(-1), Guid.NewGuid(), DateTime.UtcNow);
        grille.AjouterLigne(SensTrafic.ImportLocal, TailleConteneur.VingtPieds, "C1", 100000m, 20000m, 35000m);
        grille.AjouterLigne(SensTrafic.ImportTransit, TailleConteneur.VingtPieds, "C1", 100000m, 20000m, 35000m);
        grille.AjouterLigne(SensTrafic.ExportLocal, TailleConteneur.VingtPieds, "C1", 100000m, 20000m, 35000m);
        return grille;
    }

    [Theory]
    [InlineData("2026-02-10", "2026-02-10", 1)] // Même jour = 1 jour
    [InlineData("2026-02-10", "2026-02-11", 2)] // Lendemain = 2 jours
    [InlineData("2026-02-10", "2026-03-10", 29)] // 29 jours (Annexe A.8)
    [InlineData("2025-12-31", "2026-01-01", 2)] // Changement d'année
    public void Duree_de_sejour_est_calculee_bornes_incluses(string debut, string fin, int joursAttendus)
    {
        var dossier = new DossierSimulation(
            SensTrafic.ImportLocal,
            DateOnly.Parse(debut),
            DateOnly.Parse(fin),
            [new ConteneurSimulation("C1", TailleConteneur.VingtPieds, TypeConteneur.Dry, 10m, false, false, "C1")]);

        Assert.Equal(joursAttendus, dossier.DureeSejourJours);
    }

    [Theory]
    [InlineData(1, 0)]          // 1 jour <= 2j franchise -> 0 XOF
    [InlineData(2, 0)]          // 2 jours = franchise -> 0 XOF
    [InlineData(3, 35000)]      // 3 jours -> (3 - 2) * 35 000 = 35 000 XOF
    [InlineData(10, 280000)]    // 10 jours -> (10 - 2) * 35 000 = 280 000 XOF
    public void Branchement_reefer_deduit_franchise_2_jours(int dureeSejour, decimal montantAttendu)
    {
        var grille = CreerGrilleAvecReefer();
        var moteur = new MoteurCalculProforma();
        var dateDebut = new DateOnly(2026, 1, 1);
        var dateFin = dateDebut.AddDays(dureeSejour - 1);

        var dossier = new DossierSimulation(
            SensTrafic.ImportLocal,
            dateDebut,
            dateFin,
            [new ConteneurSimulation("C1", TailleConteneur.VingtPieds, TypeConteneur.Reefer, 10m, false, false, "C1")]);

        var res = moteur.Calculer(dossier, grille);
        Assert.Equal(montantAttendu, res.LignesConteneurs[0].BranchementFrigorifique);
    }

    [Fact]
    public void Conteneur_Dry_aucun_branchement_frigorifique_meme_avec_long_sejour()
    {
        var grille = CreerGrilleAvecReefer();
        var moteur = new MoteurCalculProforma();
        var dossier = new DossierSimulation(
            SensTrafic.ImportLocal,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 30),
            [new ConteneurSimulation("C1", TailleConteneur.VingtPieds, TypeConteneur.Dry, 10m, false, false, "C1")]);

        var res = moteur.Calculer(dossier, grille);
        Assert.Equal(0m, res.LignesConteneurs[0].BranchementFrigorifique);
    }

    [Fact]
    public void Combinaison_non_tarifee_declenche_TarifNonTrouveException()
    {
        var grille = CreerGrilleAvecReefer();
        var moteur = new MoteurCalculProforma();
        var dossier = new DossierSimulation(
            SensTrafic.ImportLocal,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 5),
            [new ConteneurSimulation("C1", TailleConteneur.QuarantePieds, TypeConteneur.Dry, 10m, false, false, "C99_INCONNUE")]);

        Assert.Throws<TarifNonTrouveException>(() => moteur.Calculer(dossier, grille));
    }
}
