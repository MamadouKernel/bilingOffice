using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Tarification;
using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Tarification;

/// <summary>
/// [TEST-CDC] REC-SCE-06 (docs/04-matrice-recette.md §22, spec.md §3) : « Calcul multi-conteneurs
/// (Dry et Reefer, avec majorations lourd/dangereux/OOG et stationnement par tranches) contrôlé au
/// franc près contre l'outil Excel de référence sur les cas de l'Annexe A.8 ; vérification qu'une
/// modification de paramètre (tarif, taux, franchise) sans développement se répercute correctement
/// sur le calcul. » Rejoué ici de bout en bout à travers la résolution DI réelle de l'application
/// (T030) et une base SQL Server réelle — jamais en confiance sur le seul nom des tests.
/// </summary>
public sealed class RecSce06SimulationE2ETests(PlateformeWebApplicationFactory factory) : IClassFixture<PlateformeWebApplicationFactory>
{
    private static VersionGrilleTarifaire CreerGrilleAnnexeA(string numeroVersion, DateTime dateEffetUtc)
    {
        var version = VersionGrilleTarifaire.Creer(numeroVersion, dateEffetUtc, Guid.NewGuid(), DateTime.UtcNow);

        version.AjouterLigne(SensTrafic.ImportLocal, TailleConteneur.VingtPieds, "C1", 66800m, 25000m, 35000m);
        version.AjouterLigne(SensTrafic.ImportLocal, TailleConteneur.VingtPieds, "C2", 95000m, 25000m, 35000m);
        version.AjouterLigne(SensTrafic.ImportLocal, TailleConteneur.VingtPieds, "C3", 140000m, 25000m, 35000m);
        version.AjouterLigne(SensTrafic.ImportLocal, TailleConteneur.QuarantePieds, "C1", 133600m, 50000m, 67000m);
        version.AjouterLigne(SensTrafic.ImportLocal, TailleConteneur.QuarantePieds, "C2", 190000m, 50000m, 67000m);
        version.AjouterLigne(SensTrafic.ImportLocal, TailleConteneur.QuarantePieds, "C3", 280000m, 50000m, 67000m);

        version.AjouterLigne(SensTrafic.ExportLocal, TailleConteneur.VingtPieds, "C1", 69500m, 25000m, 35000m);
        version.AjouterLigne(SensTrafic.ExportLocal, TailleConteneur.VingtPieds, "C2", 122500m, 25000m, 35000m);
        version.AjouterLigne(SensTrafic.ExportLocal, TailleConteneur.VingtPieds, "C3", 130000m, 25000m, 35000m);
        version.AjouterLigne(SensTrafic.ExportLocal, TailleConteneur.QuarantePieds, "C1", 139000m, 50000m, 65000m);
        version.AjouterLigne(SensTrafic.ExportLocal, TailleConteneur.QuarantePieds, "C2", 245000m, 50000m, 65000m);
        version.AjouterLigne(SensTrafic.ExportLocal, TailleConteneur.QuarantePieds, "C3", 260000m, 50000m, 65000m);

        version.AjouterLigne(SensTrafic.ImportTransit, TailleConteneur.VingtPieds, "C4", 72230m, 25000m, 35000m);
        version.AjouterLigne(SensTrafic.ImportTransit, TailleConteneur.QuarantePieds, "C4", 144460m, 50000m, 67000m);

        version.AjouterLigne(SensTrafic.ExportTransit, TailleConteneur.VingtPieds, "C5", 69000m, 25000m, 35000m);
        version.AjouterLigne(SensTrafic.ExportTransit, TailleConteneur.QuarantePieds, "C5", 138000m, 50000m, 65000m);

        return version;
    }

    public static TheoryData<string, string, string, decimal, bool, bool, string, decimal> CasAnnexeA8 => new()
    {
        { "ImportTransit", "VingtPieds", "Dry", 100m, true, true, "C4", 199129m },
        { "ImportLocal", "QuarantePieds", "Reefer", 200m, false, false, "C1", 2054980m },
        { "ExportLocal", "VingtPieds", "Dry", 300m, false, true, "C2", 245500m },
        { "ExportTransit", "QuarantePieds", "Reefer", 100m, true, false, "C5", 2053400m },
        { "ImportLocal", "QuarantePieds", "Reefer", 300m, false, false, "C3", 2245300m },
    };

    [Theory]
    [MemberData(nameof(CasAnnexeA8))]
    public async Task Calcul_multi_conteneurs_controle_au_franc_pres_contre_Annexe_A8(
        string sensStr, string tailleStr, string typeStr, decimal poids, bool dangereux, bool oog, string categorie, decimal totalAttendu)
    {
        using var scope = factory.Services.CreateScope();
        var versionRepository = scope.ServiceProvider.GetRequiredService<IVersionGrilleTarifaireRepository>();
        var calculer = scope.ServiceProvider.GetRequiredService<CalculerSimulationProformaQueryHandler>();

        var version = CreerGrilleAnnexeA($"E2E-{Guid.NewGuid():N}"[..20], DateTime.UtcNow.AddDays(-1));
        await versionRepository.AjouterAsync(version);

        var requete = new CalculerSimulationProformaRequete(
            Enum.Parse<SensTrafic>(sensStr),
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1).AddDays(28)),
            [new ConteneurSimulationRequete("CONT-E2E", Enum.Parse<TailleConteneur>(tailleStr), Enum.Parse<TypeConteneur>(typeStr), poids, dangereux, oog, categorie)]);

        var resultat = await calculer.HandleAsync(requete);

        Assert.Equal(totalAttendu, resultat.TotalGeneralXof);
        Assert.Contains("indicati", resultat.MentionLegale, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Modification_de_parametre_tarifaire_sans_developpement_se_repercute_immediatement_sur_le_calcul()
    {
        using var scope = factory.Services.CreateScope();
        var versionRepository = scope.ServiceProvider.GetRequiredService<IVersionGrilleTarifaireRepository>();
        var calculer = scope.ServiceProvider.GetRequiredService<CalculerSimulationProformaQueryHandler>();

        var maintenant = DateTime.UtcNow;
        var suffixe = Guid.NewGuid().ToString("N")[..8];

        // v1.0 : Acconage Import Local 20" C1 = 66 800 XOF (valeur Annexe A.1). Datée au plus près de
        // « maintenant » (jamais dans le passé lointain) afin qu'aucune autre grille concurrente
        // insérée par un autre test de cette même suite partagée ne puisse la supplanter comme
        // version active la plus récente.
        var v1 = VersionGrilleTarifaire.Creer($"P1-{suffixe}", maintenant, Guid.NewGuid(), maintenant);
        v1.AjouterLigne(SensTrafic.ImportLocal, TailleConteneur.VingtPieds, $"CX-{suffixe}", 66800m, 25000m, 35000m);
        await versionRepository.AjouterAsync(v1);

        var requete = new CalculerSimulationProformaRequete(
            SensTrafic.ImportLocal,
            DateOnly.FromDateTime(maintenant.AddDays(-5)),
            DateOnly.FromDateTime(maintenant.AddDays(-5)),
            [new ConteneurSimulationRequete("CONT-PARAM", TailleConteneur.VingtPieds, TypeConteneur.Dry, 5m, false, false, $"CX-{suffixe}")]);

        var resultatAvant = await calculer.HandleAsync(requete);
        Assert.Equal(66800m + 25000m, resultatAvant.TotalGeneralXof);

        // Un administrateur fonctionnel révise le tarif : nouvelle version v2.0, sans aucune
        // modification de code, avec un Acconage différent pour la même combinaison. Datée à
        // l'instant présent (postérieur à v1 puisque le temps réel ne recule jamais) plutôt qu'à un
        // décalage arbitraire dans le futur, qui échouerait le filtre « DateEffetUtc <= maintenant »
        // du moteur de sélection si l'horloge réelle n'avait pas encore atteint ce décalage.
        var v2 = VersionGrilleTarifaire.Creer($"P2-{suffixe}", DateTime.UtcNow, Guid.NewGuid(), DateTime.UtcNow);
        v2.AjouterLigne(SensTrafic.ImportLocal, TailleConteneur.VingtPieds, $"CX-{suffixe}", 99000m, 25000m, 35000m);
        await versionRepository.AjouterAsync(v2);

        var resultatApres = await calculer.HandleAsync(requete);

        Assert.Equal(99000m + 25000m, resultatApres.TotalGeneralXof);
        Assert.NotEqual(resultatAvant.TotalGeneralXof, resultatApres.TotalGeneralXof);
        Assert.Equal(v2.Id, resultatApres.VersionGrilleId);
    }
}
