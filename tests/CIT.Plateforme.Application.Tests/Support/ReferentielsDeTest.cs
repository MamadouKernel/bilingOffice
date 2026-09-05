using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Domain.Tarification.Referentiels;
using CIT.Plateforme.Infrastructure.Persistence;

namespace CIT.Plateforme.Application.Tests.Support;

/// <summary>
/// Approvisionne un contexte de test avec les référentiels tarifaires de l'Annexe A.6.
///
/// NÉCESSAIRE DEPUIS LA NORMALISATION (analyse Merise § 3.3) : sens, taille, type et catégorie de
/// marchandise étaient trois énumérations et une chaîne libre, disponibles sans rien persister.
/// Devenus des tables reliées par clés étrangères, ils doivent exister avant toute simulation.
/// </summary>
public static class ReferentielsDeTest
{
    /// <summary>Insère les référentiels dans le contexte et rend l'index résolu attendu par le
    /// moteur de calcul.</summary>
    public static ReferentielsTarifaires Amorcer(PlateformeDbContext db)
    {
        var referentiels = ParametrageInitialAnnexeA.CreerReferentiels();

        db.SensOperations.AddRange(referentiels.Sens);
        db.TaillesConteneur.AddRange(referentiels.Tailles);
        db.TypesConteneur.AddRange(referentiels.Types);
        db.CategoriesMarchandise.AddRange(referentiels.Categories);
        db.SaveChanges();

        return referentiels.Index;
    }

    /// <summary>Jeu de référentiels non persisté, pour les tests qui n'ont pas de base.</summary>
    public static ReferentielsAnnexeA EnMemoire() => ParametrageInitialAnnexeA.CreerReferentiels();

    public static Guid Sens(this ReferentielsAnnexeA r, string code) => r.ParSens(code).Id;

    public static Guid Taille(this ReferentielsAnnexeA r, string code) => r.ParTaille(code).Id;

    public static Guid Type(this ReferentielsAnnexeA r, string code) => r.ParType(code).Id;

    public static Guid Categorie(this ReferentielsAnnexeA r, string sensCode, string code) =>
        r.ParCategorie(sensCode, code).Id;

    /// <summary>Tout ce qu'un test de simulation doit avoir sous la main depuis que les
    /// référentiels sont des tables : les identifiants résolus, la grille et un résultat calculé.</summary>
    public sealed record ContexteTarifaire(
        ReferentielsTarifaires Referentiels,
        VersionGrilleTarifaire Grille,
        DossierSimulation Dossier,
        IReadOnlyList<LigneResultatConteneur> Lignes,
        Guid SensId,
        Guid TailleId,
        Guid TypeId,
        Guid CategorieId);

    /// <summary>
    /// Amorce les référentiels, persiste une grille portant les paramètres de l'Annexe A et une
    /// ligne tarifaire, puis calcule un résultat exploitable comme instantané de simulation.
    /// </summary>
    public static async Task<ContexteTarifaire> PreparerAsync(
        PlateformeDbContext db,
        string numeroVersion = "v1.0",
        DateTime? dateEffetUtc = null,
        decimal acconage = 66800m,
        decimal relevage = 25000m,
        decimal tarifJourBranchement = 35000m,
        bool amorcerReferentiels = true)
    {
        var referentiels = amorcerReferentiels
            ? Amorcer(db)
            : await new ReferentielsTarifairesRepository(db).ChargerAsync();

        var sens = referentiels.SensParCode(ParametrageInitialAnnexeA.SensImportLocal)!;
        var taille = referentiels.TailleParCode(ParametrageInitialAnnexeA.TailleVingtPieds)!;
        var type = referentiels.TypeParCode(ParametrageInitialAnnexeA.TypeDry)!;
        var categorie = referentiels.CategorieParCode(sens.Id, "C1")!;

        var grille = VersionGrilleTarifaire.Creer(
            numeroVersion,
            dateEffetUtc ?? DateTime.UtcNow.AddDays(-1),
            Guid.NewGuid(),
            DateTime.UtcNow,
            ParametrageInitialAnnexeA.FranchiseBranchementJours);

        ParametrageInitialAnnexeA.AppliquerParametres(grille, referentiels);
        grille.AjouterLigne(sens.Id, taille.Id, categorie.Id, acconage, relevage, tarifJourBranchement);

        await new VersionGrilleTarifaireRepository(db).AjouterAsync(grille);

        var dossier = new DossierSimulation(
            sens.Id,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 5),
            [new ConteneurSimulation("C1", taille.Id, type.Id, 10m, false, false, categorie.Id)]);

        var resultat = new MoteurCalculProforma().Calculer(dossier, grille, referentiels);

        return new ContexteTarifaire(
            referentiels, grille, dossier, resultat.LignesConteneurs,
            sens.Id, taille.Id, type.Id, categorie.Id);
    }

    /// <summary>Grille portant les paramètres de l'Annexe A et une ligne tarifaire minimale, de
    /// quoi faire aboutir un calcul sans que le test ait à décrire toute une grille.</summary>
    public static VersionGrilleTarifaire GrilleMinimale(
        ReferentielsAnnexeA referentiels,
        string sensCode = ParametrageInitialAnnexeA.SensImportLocal,
        string categorieCode = "C1",
        decimal acconage = 66800m,
        decimal relevage = 25000m,
        decimal tarifJourBranchement = 35000m)
    {
        var grille = VersionGrilleTarifaire.Creer(
            "v1.0",
            DateTime.UtcNow.AddDays(-1),
            Guid.NewGuid(),
            DateTime.UtcNow,
            ParametrageInitialAnnexeA.FranchiseBranchementJours);

        ParametrageInitialAnnexeA.AppliquerParametres(grille, referentiels);

        foreach (var taille in referentiels.Tailles)
        {
            grille.AjouterLigne(
                referentiels.Sens(sensCode),
                taille.Id,
                referentiels.Categorie(sensCode, categorieCode),
                acconage,
                relevage,
                tarifJourBranchement);
        }

        return grille;
    }
}
