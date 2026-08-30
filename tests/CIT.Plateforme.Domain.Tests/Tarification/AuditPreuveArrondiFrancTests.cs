using CIT.Plateforme.Domain.Tarification;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Tarification;

/// <summary>
/// [AUDIT POST-IMPLÉMENTATION F005 — TEST DE PREUVE, §7/§10] : spec.md §4.2 énonce explicitement
/// « Chaque poste est calculé et **arrondi** au franc près » et research.md D2 affirme que
/// l'arrondi est garanti soit par <c>Math.Round</c>, soit parce que les taux/montants sont choisis
/// pour être toujours entiers. Un scan du code confirme l'absence totale de tout appel à
/// <c>Math.Round</c> dans <see cref="MoteurCalculProforma"/> et <see cref="ParametresTarification"/>.
/// Ce test démontre que la garantie « arrondi au franc » repose donc **entièrement** sur la propreté
/// des données de grille (Annexe A.1, dont les valeurs ont été choisies rondes) et non sur une
/// protection active du moteur : avec un taux de majoration non entier issu d'une grille
/// administrée (SIM-06 autorise explicitement leur modification sans développement), le moteur
/// produit un montant fractionnaire de XOF — une devise sans sous-unité. Ce n'est PAS une
/// correction ; ce test se contente de démontrer objectivement le comportement réel du code tel
/// qu'il existe, pour trancher le point sans ambiguïté (voir post-implementation-audit.md §7/§10).
/// </summary>
public sealed class AuditPreuveArrondiFrancTests
{
    [Fact]
    public void Majoration_Lourd_avec_taux_non_entier_produit_un_montant_XOF_fractionnaire()
    {
        var parametres = ParametresTarification.Creer(
            tauxMajorationLourd: 0.333m,
            tauxMajorationDangereux: 0.50m,
            tauxMajorationOog: 0.50m,
            seuilPoidsLourd20Pieds: 15m,
            seuilPoidsLourd40Pieds: 26m,
            franchiseBranchementJours: 2,
            franchiseStationnementImportLocalJours: 11,
            franchiseStationnementImportTransitJours: 21,
            tarifStationnementTranche1Jour: 850m,
            tarifStationnementTranche2Jour: 1250m,
            tarifStationnementTranche3Jour: 1850m,
            quotePartCit: 0.30m,
            quotePartOic: 0.20m,
            quotePartPaa: 0.50m);

        var version = VersionGrilleTarifaire.Creer("AUDIT-ARRONDI", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Guid.NewGuid(), DateTime.UtcNow, parametres);
        version.AjouterLigne(SensTrafic.ImportLocal, TailleConteneur.VingtPieds, "AUDIT", acconage: 100m, relevage: 0m, tarifJourBranchement: 0m);

        var moteur = new MoteurCalculProforma();
        var dossier = new DossierSimulation(
            SensTrafic.ImportLocal,
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 2, 1),
            [new ConteneurSimulation("AUDIT01", TailleConteneur.VingtPieds, TypeConteneur.Dry, PoidsTonnes: 16m, EstDangereux: false, EstHorsGabarit: false, "AUDIT")]);

        var resultat = moteur.Calculer(dossier, version);
        var majorationLourd = resultat.LignesConteneurs[0].MajorationLourd;

        // PREUVE : 100 XOF x 0.333 = 33.3 XOF, une valeur fractionnaire — jamais arrondie par le
        // moteur. Si cette assertion venait à échouer après une correction future du moteur (ajout
        // d'un Math.Round), ce serait le signe que l'écart signalé a été traité — mais en l'état
        // actuel du code audité, l'assertion est vraie.
        Assert.Equal(33.3m, majorationLourd);
        Assert.NotEqual(decimal.Truncate(majorationLourd), majorationLourd);
    }
}
