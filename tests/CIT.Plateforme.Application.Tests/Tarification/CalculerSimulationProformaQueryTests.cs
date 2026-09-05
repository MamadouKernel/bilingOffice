using CIT.Plateforme.Application.Tarification;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

using A = CIT.Plateforme.Domain.Tarification.ParametrageInitialAnnexeA;

namespace CIT.Plateforme.Application.Tests.Tarification;

public sealed class CalculerSimulationProformaQueryTests
{
    [Fact]
    public async Task CalculerSimulation_utilise_la_grille_active_et_retourne_le_resultat_detaille()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        ReferentielsDeTest.Amorcer(db);

        var repo = new VersionGrilleTarifaireRepository(db);
        var referentielsRepo = new ReferentielsTarifairesRepository(db);
        var referentiels = await referentielsRepo.ChargerAsync();

        var grille = VersionGrilleTarifaire.Creer(
            "v1.0", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Guid.NewGuid(), DateTime.UtcNow,
            A.FranchiseBranchementJours);
        ParametrageInitialAnnexeA.AppliquerParametres(grille, referentiels);

        var sens = referentiels.SensParCode(A.SensImportLocal)!;
        var taille = referentiels.TailleParCode(A.TailleVingtPieds)!;
        var type = referentiels.TypeParCode(A.TypeDry)!;
        var categorie = referentiels.CategorieParCode(sens.Id, "C1")!;

        grille.AjouterLigne(sens.Id, taille.Id, categorie.Id, 66800m, 25000m, 35000m);
        await repo.AjouterAsync(grille);

        var handler = new CalculerSimulationProformaQueryHandler(repo, referentielsRepo, new MoteurCalculProforma());
        var requete = new CalculerSimulationProformaRequete(
            sens.Id,
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 2, 5),
            [new ConteneurSimulationRequete("C1", taille.Id, type.Id, 10m, false, false, categorie.Id)]);

        var resultat = await handler.HandleAsync(requete);

        Assert.NotNull(resultat);
        Assert.Equal(91800m, resultat.TotalGeneralXof); // 66800 + 25000 = 91800
        Assert.Equal("v1.0", resultat.VersionGrilleLibelle);
        Assert.Single(resultat.Lignes);
        Assert.Contains("purement indicatif", resultat.MentionLegale);

        // Les libellés du référentiel remontent jusqu'à la vue : c'est ce qui remplace l'ancien
        // affichage des valeurs d'énumération.
        Assert.Equal("Import local", resultat.Sens.Libelle);
        Assert.Equal("20 pieds", resultat.Lignes[0].Taille.Libelle);
        Assert.Equal("C1", resultat.Lignes[0].Categorie.Code);
    }

    /// <summary>
    /// Le dépôt doit charger les paramètres avec la grille. S'il ne le faisait pas, le moteur
    /// verrait une grille sans taux ni tranches et produirait des montants nuls <b>sans erreur</b> :
    /// c'est la régression que ce test verrouille.
    /// </summary>
    [Fact]
    public async Task La_grille_relue_porte_ses_parametres_et_les_majorations_sappliquent()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        ReferentielsDeTest.Amorcer(db);

        var repo = new VersionGrilleTarifaireRepository(db);
        var referentielsRepo = new ReferentielsTarifairesRepository(db);
        var referentiels = await referentielsRepo.ChargerAsync();

        var grille = VersionGrilleTarifaire.Creer(
            "v1.0", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Guid.NewGuid(), DateTime.UtcNow,
            A.FranchiseBranchementJours);
        ParametrageInitialAnnexeA.AppliquerParametres(grille, referentiels);

        var sens = referentiels.SensParCode(A.SensImportLocal)!;
        var taille = referentiels.TailleParCode(A.TailleVingtPieds)!;
        var type = referentiels.TypeParCode(A.TypeDry)!;
        var categorie = referentiels.CategorieParCode(sens.Id, "C1")!;

        grille.AjouterLigne(sens.Id, taille.Id, categorie.Id, 100000m, 0m, 0m);
        await repo.AjouterAsync(grille);

        var handler = new CalculerSimulationProformaQueryHandler(repo, referentielsRepo, new MoteurCalculProforma());

        // 20 t dépasse le seuil de 15 t : la majoration lourd (30 %) doit s'appliquer.
        var resultat = await handler.HandleAsync(new CalculerSimulationProformaRequete(
            sens.Id,
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 2, 1),
            [new ConteneurSimulationRequete("C1", taille.Id, type.Id, 20m, false, false, categorie.Id)]));

        Assert.Equal(30000m, resultat.Lignes[0].MajorationLourd);
    }
}
