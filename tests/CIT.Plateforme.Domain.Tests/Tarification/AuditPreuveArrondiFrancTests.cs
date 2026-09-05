using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Domain.Tarification.Parametres;
using CIT.Plateforme.Domain.Tarification.Referentiels;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Tarification;

/// <summary>
/// [AUDIT POST-IMPLÉMENTATION F005 — TEST DE PREUVE, §7/§10] : spec.md §4.2 énonce explicitement
/// « Chaque poste est calculé et **arrondi** au franc près » et research.md D2 affirme que
/// l'arrondi est garanti soit par <c>Math.Round</c>, soit parce que les taux/montants sont choisis
/// pour être toujours entiers. Un scan du code confirme l'absence totale de tout appel à
/// <c>Math.Round</c> dans <see cref="MoteurCalculProforma"/>. Ce test démontre que la garantie
/// « arrondi au franc » repose donc <b>entièrement</b> sur la propreté des données de grille
/// (Annexe A.1, dont les valeurs ont été choisies rondes) et non sur une protection active du
/// moteur : avec un taux de majoration non entier issu d'une grille administrée (SIM-06 autorise
/// explicitement leur modification sans développement), le moteur produit un montant
/// fractionnaire de XOF — une devise sans sous-unité. Ce n'est PAS une correction ; ce test se
/// contente de démontrer objectivement le comportement réel du code tel qu'il existe, pour
/// trancher le point sans ambiguïté (voir post-implementation-audit.md §7/§10).
///
/// La normalisation des référentiels (analyse Merise § 3.3) n'a rien changé à ce constat : le taux
/// vient désormais d'une ligne <c>PARAMETRE_MAJORATION</c> au lieu d'une colonne, mais reste
/// administrable, donc toujours susceptible d'être non entier.
/// </summary>
public sealed class AuditPreuveArrondiFrancTests
{
    [Fact]
    public void Majoration_Lourd_avec_taux_non_entier_produit_un_montant_XOF_fractionnaire()
    {
        // Référentiel minimal propre à ce test : la catégorie « AUDIT » n'appartient pas à
        // l'Annexe A, et n'a pas à y être ajoutée pour les besoins d'une démonstration.
        var sens = SensOperation.Creer("AUDIT_SENS", "Sens d'audit", 1);
        var taille = TailleConteneur.Creer("AUDIT_20", "20 pieds (audit)", 1);
        var type = TypeConteneur.Creer("AUDIT_DRY", "Dry (audit)", declencheBranchementFrigorifique: false, 1);
        var categorie = CategorieMarchandise.Creer("AUDIT", "Catégorie d'audit", sens.Id);
        var referentiels = new ReferentielsTarifaires([sens], [taille], [type], [categorie]);

        var version = VersionGrilleTarifaire.Creer(
            "AUDIT-ARRONDI",
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Guid.NewGuid(),
            DateTime.UtcNow,
            franchiseBranchementJours: 2);

        version.DefinirMajoration(TypeMajoration.Lourd, 0.333m);
        version.DefinirSeuilPoids(taille.Id, 15m);
        version.AjouterLigne(sens.Id, taille.Id, categorie.Id, acconage: 100m, relevage: 0m, tarifJourBranchement: 0m);

        var dossier = new DossierSimulation(
            sens.Id,
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 2, 1),
            [
                new ConteneurSimulation("AUDIT01", taille.Id, type.Id, PoidsTonnes: 16m,
                    EstDangereux: false, EstHorsGabarit: false, categorie.Id)
            ]);

        var resultat = new MoteurCalculProforma().Calculer(dossier, version, referentiels);
        var majorationLourd = resultat.LignesConteneurs[0].MajorationLourd;

        // PREUVE : 100 XOF x 0.333 = 33.3 XOF, une valeur fractionnaire — jamais arrondie par le
        // moteur. Si cette assertion venait à échouer après une correction future du moteur (ajout
        // d'un Math.Round), ce serait le signe que l'écart signalé a été traité — mais en l'état
        // actuel du code audité, l'assertion est vraie.
        Assert.Equal(33.3m, majorationLourd);
        Assert.NotEqual(decimal.Truncate(majorationLourd), majorationLourd);
    }
}
