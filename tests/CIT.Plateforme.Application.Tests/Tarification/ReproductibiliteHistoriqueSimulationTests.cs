using CIT.Plateforme.Application.Tarification;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Tarification;

/// <summary>
/// [P2-05] RG-SIM-12 — UNE SIMULATION HISTORIQUE RESTE REPRODUCTIBLE.
///
/// Texte de la règle : « Les grilles tarifaires sont versionnées avec une date d'effet ; une
/// simulation référence la version appliquée et l'historique des grilles est conservé. »
///
/// CE QUE CETTE SUITE AJOUTE. Le contre-audit avait constaté que le test existant vérifiait la
/// VERSION référencée — <c>EstGrilleObsolete</c>, <c>VersionGrilleLibelle</c> — mais <b>jamais les
/// montants</b>. Or c'est le montant qui fait la valeur d'une proforma : une simulation remise au
/// client est un engagement chiffré. Un recalcul silencieux avec la grille courante aurait modifié
/// le prix d'un dossier ancien sans qu'aucun test ne s'en aperçoive.
///
/// LE SCÉNARIO, joué de bout en bout sur le chemin de production :
/// grille V1 → simulation S1 → grille V2 activée → simulation S2 → consultation de S1.
/// </summary>
public sealed class ReproductibiliteHistoriqueSimulationTests
{
    private static readonly DateOnly Arrivee = new(2026, 2, 1);
    private static readonly DateOnly Enlevement = new(2026, 2, 5);

    private static SauvegarderSimulationProformaCommandHandler Sauvegarder(PlateformeDbContext db) => new(
        new SimulationProformaRepository(db),
        new VersionGrilleTarifaireRepository(db),
        new ReferentielsTarifairesRepository(db),
        new MoteurCalculProforma(),
        FabriqueRequetes.ControleAcces(db));

    private static ConsulterSimulationProformaQueryHandler Consulter(PlateformeDbContext db) => new(
        new SimulationProformaRepository(db),
        new VersionGrilleTarifaireRepository(db),
        new ReferentielsTarifairesRepository(db),
        FabriqueRequetes.ControleAcces(db));

    private static CalculerSimulationProformaRequete Requete(ReferentielsDeTest.ContexteTarifaire c) =>
        new(c.SensId, Arrivee, Enlevement,
            [new ConteneurSimulationRequete("CONT-SIM12", c.TailleId, c.TypeId, 10m, false, false, c.CategorieId)]);

    /// <summary>
    /// LE SCÉNARIO COMPLET exigé par la règle. Une seule méthode, parce que les assertions n'ont de
    /// sens que dans cette séquence : c'est l'ordre des événements qui constitue la démonstration.
    /// </summary>
    [Fact]
    public async Task Une_simulation_conserve_ses_montants_apres_revision_de_la_grille()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var societeId = Guid.NewGuid();
        var appelant = FabriqueRequetes.AppelantDeSociete(db, societeId);

        // ── GRILLE V1, puis SIMULATION S1 ──────────────────────────────────────────────────
        var v1 = await ReferentielsDeTest.PreparerAsync(db, "v1.0", DateTime.UtcNow.AddMonths(-2));
        var s1 = await Sauvegarder(db).HandleAsync(new SauvegarderSimulationProformaCommande(appelant, Requete(v1)));

        var montantS1ALaCreation = s1.MontantTotalXof;
        Assert.True(montantS1ALaCreation > 0m, "Témoin : la simulation doit produire un montant.");

        // ── GRILLE V2 ACTIVÉE — tous les tarifs changent ───────────────────────────────────
        var v2 = await ReferentielsDeTest.PreparerAsync(
            db, "v2.0", DateTime.UtcNow.AddDays(-1),
            acconage: 99000m, relevage: 44000m, tarifJourBranchement: 55000m,
            amorcerReferentiels: false);

        // ── SIMULATION S2, sur la grille courante ──────────────────────────────────────────
        var s2 = await Sauvegarder(db).HandleAsync(new SauvegarderSimulationProformaCommande(appelant, Requete(v2)));

        // La grille a réellement changé : sans cela, l'égalité de S1 ne prouverait rien.
        Assert.NotEqual(montantS1ALaCreation, s2.MontantTotalXof);

        // ── CONSULTATION DE S1, APRÈS la révision ──────────────────────────────────────────
        var vueS1 = await Consulter(db).HandleAsync(new ConsulterSimulationProformaRequete(appelant, s1.Id));
        var vueS2 = await Consulter(db).HandleAsync(new ConsulterSimulationProformaRequete(appelant, s2.Id));

        Assert.NotNull(vueS1);
        Assert.NotNull(vueS2);

        // 1. S1 référence V1 ; S2 référence V2.
        Assert.Equal("v1.0", vueS1!.VersionGrilleLibelle);
        Assert.Equal(v1.Grille.Id, vueS1.VersionGrilleTarifaireId);
        Assert.Equal("v2.0", vueS2!.VersionGrilleLibelle);
        Assert.Equal(v2.Grille.Id, vueS2.VersionGrilleTarifaireId);

        // 2. S1 est signalée obsolète, S2 ne l'est pas.
        Assert.True(vueS1.EstGrilleObsolete);
        Assert.False(vueS2.EstGrilleObsolete);

        // 3. S1 CONSERVE SON MONTANT HISTORIQUE — l'assertion qui manquait.
        Assert.Equal(montantS1ALaCreation, vueS1.MontantTotalXof);

        // 4. S1 n'est PAS recalculée avec V2 : son total diffère de celui de S2, à dossier
        //    identique. Deux simulations du même dossier sur deux grilles ne peuvent coïncider.
        Assert.NotEqual(vueS2.MontantTotalXof, vueS1.MontantTotalXof);

        // 5. Le détail conteneur reste celui de V1, poste par poste.
        var ligneS1 = Assert.Single(vueS1.Lignes);
        var ligneS2 = Assert.Single(vueS2.Lignes);
        Assert.Equal("CONT-SIM12", ligneS1.NumeroConteneur);
        Assert.NotEqual(ligneS2.TotalLigne, ligneS1.TotalLigne);

        // 6. Le total du dossier est la somme de ses lignes — la vue historique reste cohérente
        //    avec elle-même, et non recomposée à partir de la grille courante.
        Assert.Equal(vueS1.Lignes.Sum(l => l.TotalLigne), vueS1.MontantTotalXof);
    }

    /// <summary>
    /// Le montant figé survit à un RECHARGEMENT COMPLET depuis la base. Sans ce test, une valeur
    /// conservée seulement en mémoire passerait le scénario précédent.
    /// </summary>
    [Fact]
    public async Task Le_montant_historique_survit_a_un_rechargement_depuis_la_base()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var appelant = FabriqueRequetes.AppelantDeSociete(db, Guid.NewGuid());

        var v1 = await ReferentielsDeTest.PreparerAsync(db, "v1.0", DateTime.UtcNow.AddMonths(-2));
        var s1 = await Sauvegarder(db).HandleAsync(new SauvegarderSimulationProformaCommande(appelant, Requete(v1)));
        var montantOrigine = s1.MontantTotalXof;

        await ReferentielsDeTest.PreparerAsync(
            db, "v2.0", DateTime.UtcNow.AddDays(-1),
            acconage: 99000m, relevage: 44000m, tarifJourBranchement: 55000m,
            amorcerReferentiels: false);

        // Le suivi de changements est vidé : la vue ne peut plus provenir que de la base.
        db.ChangeTracker.Clear();

        var vue = await Consulter(db).HandleAsync(new ConsulterSimulationProformaRequete(appelant, s1.Id));

        Assert.Equal(montantOrigine, vue!.MontantTotalXof);
        Assert.Equal("v1.0", vue.VersionGrilleLibelle);
    }

    /// <summary>
    /// L'historique des grilles est CONSERVÉ : la version d'origine reste lisible après
    /// l'activation de la suivante. Sans elle, la simulation référencerait une version disparue.
    /// </summary>
    [Fact]
    public async Task L_historique_des_grilles_est_conserve()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();

        var v1 = await ReferentielsDeTest.PreparerAsync(db, "v1.0", DateTime.UtcNow.AddMonths(-2));
        await ReferentielsDeTest.PreparerAsync(db, "v2.0", DateTime.UtcNow.AddDays(-1), amorcerReferentiels: false);

        var depot = new VersionGrilleTarifaireRepository(db);

        Assert.NotNull(await depot.ObtenirParIdAsync(v1.Grille.Id));
        Assert.Equal(2, (await depot.ListerToutesAsync()).Count);
    }
}
