using CIT.Plateforme.Application.Tarification;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

using A = CIT.Plateforme.Domain.Tarification.ParametrageInitialAnnexeA;

namespace CIT.Plateforme.Application.Tests.Tarification;

/// <summary>[TEST-SPEC] Exigences CDC : famille SIM (SIM-01 à SIM-11) — moteur de simulation
/// tarifaire proforma contrôlé au franc près contre l'annexe A8. (Traçabilité §6 docs/88.)</summary>
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

    /// <summary>Grille de l'annexe A (import local) : 20" C1, 20" C3 et 40" C1.</summary>
    private static async Task<(CalculerSimulationProformaQueryHandler Handler, Guid Sens, Guid T20, Guid T40, Guid Dry, Guid C1, Guid C3)> GrilleAnnexeAsync(CIT.Plateforme.Infrastructure.Persistence.PlateformeDbContext db)
    {
        ReferentielsDeTest.Amorcer(db);
        var repo = new VersionGrilleTarifaireRepository(db);
        var referentielsRepo = new ReferentielsTarifairesRepository(db);
        var referentiels = await referentielsRepo.ChargerAsync();
        var grille = VersionGrilleTarifaire.Creer("v1.0", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Guid.NewGuid(), DateTime.UtcNow, A.FranchiseBranchementJours);
        ParametrageInitialAnnexeA.AppliquerParametres(grille, referentiels);
        var sens = referentiels.SensParCode(A.SensImportLocal)!;
        var t20 = referentiels.TailleParCode(A.TailleVingtPieds)!;
        var t40 = referentiels.TailleParCode(A.TailleQuarantePieds)!;
        var c1 = referentiels.CategorieParCode(sens.Id, "C1")!;
        var c3 = referentiels.CategorieParCode(sens.Id, "C3")!;
        grille.AjouterLigne(sens.Id, t20.Id, c1.Id, 66800m, 25000m, 35000m);
        grille.AjouterLigne(sens.Id, t20.Id, c3.Id, 140000m, 25000m, 35000m);
        grille.AjouterLigne(sens.Id, t40.Id, c1.Id, 133600m, 50000m, 67000m);
        await repo.AjouterAsync(grille);
        return (new CalculerSimulationProformaQueryHandler(repo, referentielsRepo, new MoteurCalculProforma()),
            sens.Id, t20.Id, t40.Id, referentiels.TypeParCode(A.TypeDry)!.Id, c1.Id, c3.Id);
    }

    /// <summary>SIM-03 — le tarif est déterminé par la clé Sens + Taille + Catégorie : trois
    /// conteneurs de clés différentes reçoivent chacun l'acconage et le relevage de leur clé.</summary>
    [Fact]
    public async Task SIM03_le_tarif_suit_la_cle_sens_taille_categorie()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var g = await GrilleAnnexeAsync(db);

        var resultat = await g.Handler.HandleAsync(new CalculerSimulationProformaRequete(g.Sens, new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 1),
        [
            new ConteneurSimulationRequete("A", g.T20, g.Dry, 10m, false, false, g.C1),
            new ConteneurSimulationRequete("B", g.T20, g.Dry, 10m, false, false, g.C3),
            new ConteneurSimulationRequete("C", g.T40, g.Dry, 10m, false, false, g.C1),
        ]));

        Assert.Equal((66800m, 25000m), (resultat.Lignes.Single(l => l.NumeroConteneur == "A").Acconage, resultat.Lignes.Single(l => l.NumeroConteneur == "A").Relevage));
        Assert.Equal((140000m, 25000m), (resultat.Lignes.Single(l => l.NumeroConteneur == "B").Acconage, resultat.Lignes.Single(l => l.NumeroConteneur == "B").Relevage));
        Assert.Equal((133600m, 50000m), (resultat.Lignes.Single(l => l.NumeroConteneur == "C").Acconage, resultat.Lignes.Single(l => l.NumeroConteneur == "C").Relevage));
    }

    /// <summary>SIM-05 — le résultat détaille chaque conteneur, donne un total général égal à la
    /// somme des lignes et affiche la mention légale (indicatif, n'engage pas CIT).</summary>
    [Fact]
    public async Task SIM05_detail_par_conteneur_total_general_et_mention_legale()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var g = await GrilleAnnexeAsync(db);

        var resultat = await g.Handler.HandleAsync(new CalculerSimulationProformaRequete(g.Sens, new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 1),
        [
            new ConteneurSimulationRequete("A", g.T20, g.Dry, 10m, false, false, g.C1),
            new ConteneurSimulationRequete("B", g.T40, g.Dry, 10m, false, false, g.C1),
        ]));

        Assert.Equal(2, resultat.Lignes.Count);
        Assert.Equal(resultat.Lignes.Sum(l => l.TotalLigne), resultat.TotalGeneralXof);
        Assert.Equal(91800m + 183600m, resultat.TotalGeneralXof);
        Assert.Contains("n'engage pas Côte d'Ivoire Terminal", resultat.MentionLegale);
        Assert.Contains("indicatif", resultat.MentionLegale);
    }
}
