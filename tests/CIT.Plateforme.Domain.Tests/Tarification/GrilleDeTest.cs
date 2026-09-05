using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Domain.Tarification.Referentiels;

namespace CIT.Plateforme.Domain.Tests.Tarification;

/// <summary>
/// Fabrique commune aux tests tarifaires depuis que les référentiels sont des tables
/// (analyse Merise § 3.3) : chaque dossier de simulation désigne désormais un sens, une taille,
/// un type et une catégorie par identifiant, et le moteur reçoit les référentiels résolus.
///
/// Un jeu unique et partagé, pour que les identifiants soient stables d'un test à l'autre.
/// </summary>
internal static class GrilleDeTest
{
    public static readonly ReferentielsAnnexeA Referentiels = ParametrageInitialAnnexeA.CreerReferentiels();

    public static ReferentielsTarifaires Index => Referentiels.Index;

    public static Guid Sens(string code) => Referentiels.ParSens(code).Id;

    public static Guid Taille(string code) => Referentiels.ParTaille(code).Id;

    public static Guid Type(string code) => Referentiels.ParType(code).Id;

    public static Guid Categorie(string sensCode, string code) => Referentiels.ParCategorie(sensCode, code).Id;

    /// <summary>Grille vide, portant les taux, seuils, tranches, franchises et quotes-parts de
    /// l'Annexe A. Les lignes tarifaires restent à la charge du test.</summary>
    public static VersionGrilleTarifaire NouvelleGrille(string numeroVersion = "v1.0")
    {
        var grille = VersionGrilleTarifaire.Creer(
            numeroVersion,
            DateTime.UtcNow.AddDays(-1),
            Guid.NewGuid(),
            DateTime.UtcNow,
            ParametrageInitialAnnexeA.FranchiseBranchementJours);

        ParametrageInitialAnnexeA.AppliquerParametres(grille, Referentiels);
        return grille;
    }

    public static void AjouterLigneCodes(
        this VersionGrilleTarifaire grille,
        string sensCode,
        string tailleCode,
        string categorieCode,
        decimal acconage,
        decimal relevage,
        decimal tarifJourBranchement) =>
        grille.AjouterLigne(
            Sens(sensCode),
            Taille(tailleCode),
            Categorie(sensCode, categorieCode),
            acconage,
            relevage,
            tarifJourBranchement);

    public static ConteneurSimulation Conteneur(
        string sensCode,
        string tailleCode,
        string typeCode,
        decimal poidsTonnes,
        bool dangereux = false,
        bool horsGabarit = false,
        string categorieCode = "C1",
        string numero = "CONT01") =>
        new(numero, Taille(tailleCode), Type(typeCode), poidsTonnes, dangereux, horsGabarit,
            Categorie(sensCode, categorieCode));

    public static DossierSimulation Dossier(
        string sensCode,
        DateOnly arrivee,
        DateOnly enlevement,
        params ConteneurSimulation[] conteneurs) =>
        new(Sens(sensCode), arrivee, enlevement, conteneurs);
}
