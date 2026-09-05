using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Tarification;
using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Domain.Tarification.Referentiels;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using A = CIT.Plateforme.Domain.Tarification.ParametrageInitialAnnexeA;

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
    private static VersionGrilleTarifaire CreerGrilleAnnexeA(
        string numeroVersion, DateTime dateEffetUtc, ReferentielsTarifaires referentiels)
    {
        var version = VersionGrilleTarifaire.Creer(
            numeroVersion, dateEffetUtc, Guid.NewGuid(), DateTime.UtcNow, A.FranchiseBranchementJours);

        ParametrageInitialAnnexeA.AppliquerParametres(version, referentiels);

        void Ligne(string sensCode, string tailleCode, string categorieCode, decimal acconage, decimal relevage, decimal branchement)
        {
            var sens = referentiels.SensParCode(sensCode)!;
            version.AjouterLigne(
                sens.Id,
                referentiels.TailleParCode(tailleCode)!.Id,
                referentiels.CategorieParCode(sens.Id, categorieCode)!.Id,
                acconage, relevage, branchement);
        }

        Ligne(A.SensImportLocal, A.TailleVingtPieds, "C1", 66800m, 25000m, 35000m);
        Ligne(A.SensImportLocal, A.TailleVingtPieds, "C2", 95000m, 25000m, 35000m);
        Ligne(A.SensImportLocal, A.TailleVingtPieds, "C3", 140000m, 25000m, 35000m);
        Ligne(A.SensImportLocal, A.TailleQuarantePieds, "C1", 133600m, 50000m, 67000m);
        Ligne(A.SensImportLocal, A.TailleQuarantePieds, "C2", 190000m, 50000m, 67000m);
        Ligne(A.SensImportLocal, A.TailleQuarantePieds, "C3", 280000m, 50000m, 67000m);

        Ligne(A.SensExportLocal, A.TailleVingtPieds, "C1", 69500m, 25000m, 35000m);
        Ligne(A.SensExportLocal, A.TailleVingtPieds, "C2", 122500m, 25000m, 35000m);
        Ligne(A.SensExportLocal, A.TailleVingtPieds, "C3", 130000m, 25000m, 35000m);
        Ligne(A.SensExportLocal, A.TailleQuarantePieds, "C1", 139000m, 50000m, 65000m);
        Ligne(A.SensExportLocal, A.TailleQuarantePieds, "C2", 245000m, 50000m, 65000m);
        Ligne(A.SensExportLocal, A.TailleQuarantePieds, "C3", 260000m, 50000m, 65000m);

        Ligne(A.SensImportTransit, A.TailleVingtPieds, "C4", 72230m, 25000m, 35000m);
        Ligne(A.SensImportTransit, A.TailleQuarantePieds, "C4", 144460m, 50000m, 67000m);

        Ligne(A.SensExportTransit, A.TailleVingtPieds, "C5", 69000m, 25000m, 35000m);
        Ligne(A.SensExportTransit, A.TailleQuarantePieds, "C5", 138000m, 50000m, 65000m);

        return version;
    }

    public static TheoryData<string, string, string, decimal, bool, bool, string, decimal> CasAnnexeA8 => new()
    {
        { A.SensImportTransit, A.TailleVingtPieds, A.TypeDry, 100m, true, true, "C4", 199129m },
        { A.SensImportLocal, A.TailleQuarantePieds, A.TypeReefer, 200m, false, false, "C1", 2054980m },
        { A.SensExportLocal, A.TailleVingtPieds, A.TypeDry, 300m, false, true, "C2", 245500m },
        { A.SensExportTransit, A.TailleQuarantePieds, A.TypeReefer, 100m, true, false, "C5", 2053400m },
        { A.SensImportLocal, A.TailleQuarantePieds, A.TypeReefer, 300m, false, false, "C3", 2245300m },
    };

    [Theory]
    [MemberData(nameof(CasAnnexeA8))]
    public async Task Calcul_multi_conteneurs_controle_au_franc_pres_contre_Annexe_A8(
        string sensCode, string tailleCode, string typeCode, decimal poids, bool dangereux, bool oog, string categorieCode, decimal totalAttendu)
    {
        using var scope = factory.Services.CreateScope();
        var versionRepository = scope.ServiceProvider.GetRequiredService<IVersionGrilleTarifaireRepository>();
        var referentielsRepository = scope.ServiceProvider.GetRequiredService<IReferentielsTarifairesRepository>();
        var calculer = scope.ServiceProvider.GetRequiredService<CalculerSimulationProformaQueryHandler>();

        var referentiels = await referentielsRepository.ChargerAsync();

        var version = CreerGrilleAnnexeA($"E2E-{Guid.NewGuid():N}"[..20], DateTime.UtcNow.AddDays(-1), referentiels);
        await versionRepository.AjouterAsync(version);

        var sens = referentiels.SensParCode(sensCode)!;
        var requete = new CalculerSimulationProformaRequete(
            sens.Id,
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1).AddDays(28)),
            [
                new ConteneurSimulationRequete(
                    "CONT-E2E",
                    referentiels.TailleParCode(tailleCode)!.Id,
                    referentiels.TypeParCode(typeCode)!.Id,
                    poids, dangereux, oog,
                    referentiels.CategorieParCode(sens.Id, categorieCode)!.Id)
            ]);

        var resultat = await calculer.HandleAsync(requete);

        Assert.Equal(totalAttendu, resultat.TotalGeneralXof);
        Assert.Contains("indicati", resultat.MentionLegale, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// REC-SCE-06, seconde moitié : une révision tarifaire se répercute sans développement.
    ///
    /// Le test crée aussi une <b>catégorie de marchandise inédite</b> par simple ajout au
    /// référentiel — ce qui n'était possible auparavant qu'en tapant un libellé libre non contrôlé.
    /// C'est la démonstration directe de RG-SIM-11 après la normalisation (analyse Merise § 3.3).
    /// </summary>
    [Fact]
    public async Task Modification_de_parametre_tarifaire_sans_developpement_se_repercute_immediatement_sur_le_calcul()
    {
        using var scope = factory.Services.CreateScope();
        var versionRepository = scope.ServiceProvider.GetRequiredService<IVersionGrilleTarifaireRepository>();
        var referentielsRepository = scope.ServiceProvider.GetRequiredService<IReferentielsTarifairesRepository>();
        var calculer = scope.ServiceProvider.GetRequiredService<CalculerSimulationProformaQueryHandler>();

        var maintenant = DateTime.UtcNow;
        var suffixe = Guid.NewGuid().ToString("N")[..8];

        var referentielsInitiaux = await referentielsRepository.ChargerAsync();
        var sens = referentielsInitiaux.SensParCode(A.SensImportLocal)!;
        var taille = referentielsInitiaux.TailleParCode(A.TailleVingtPieds)!;
        var type = referentielsInitiaux.TypeParCode(A.TypeDry)!;

        // Catégorie propre à ce test, ajoutée par paramétrage — aucun déploiement.
        var categorie = CategorieMarchandise.Creer($"CX{suffixe}", $"Catégorie d'essai {suffixe}", sens.Id);
        await referentielsRepository.AjouterCategorieAsync(categorie);

        // v1.0 : Acconage Import Local 20" = 66 800 XOF (valeur Annexe A.1). Datée au plus près de
        // « maintenant » (jamais dans le passé lointain) afin qu'aucune autre grille concurrente
        // insérée par un autre test de cette même suite partagée ne puisse la supplanter comme
        // version active la plus récente.
        var v1 = VersionGrilleTarifaire.Creer($"P1-{suffixe}", maintenant, Guid.NewGuid(), maintenant, A.FranchiseBranchementJours);
        v1.AjouterLigne(sens.Id, taille.Id, categorie.Id, 66800m, 25000m, 35000m);
        await versionRepository.AjouterAsync(v1);

        var requete = new CalculerSimulationProformaRequete(
            sens.Id,
            DateOnly.FromDateTime(maintenant.AddDays(-5)),
            DateOnly.FromDateTime(maintenant.AddDays(-5)),
            [new ConteneurSimulationRequete("CONT-PARAM", taille.Id, type.Id, 5m, false, false, categorie.Id)]);

        var resultatAvant = await calculer.HandleAsync(requete);
        Assert.Equal(66800m + 25000m, resultatAvant.TotalGeneralXof);

        // Un administrateur fonctionnel révise le tarif : nouvelle version, sans aucune
        // modification de code, avec un Acconage différent pour la même combinaison. Datée à
        // l'instant présent (postérieur à v1 puisque le temps réel ne recule jamais) plutôt qu'à un
        // décalage arbitraire dans le futur, qui échouerait le filtre « DateEffetUtc <= maintenant »
        // du moteur de sélection si l'horloge réelle n'avait pas encore atteint ce décalage.
        var v2 = VersionGrilleTarifaire.Creer($"P2-{suffixe}", DateTime.UtcNow, Guid.NewGuid(), DateTime.UtcNow, A.FranchiseBranchementJours);
        v2.AjouterLigne(sens.Id, taille.Id, categorie.Id, 99000m, 25000m, 35000m);
        await versionRepository.AjouterAsync(v2);

        var resultatApres = await calculer.HandleAsync(requete);

        Assert.Equal(99000m + 25000m, resultatApres.TotalGeneralXof);
        Assert.NotEqual(resultatAvant.TotalGeneralXof, resultatApres.TotalGeneralXof);
        Assert.Equal(v2.Id, resultatApres.VersionGrilleId);
    }
}
