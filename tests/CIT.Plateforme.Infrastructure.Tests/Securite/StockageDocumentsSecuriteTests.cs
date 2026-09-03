using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Infrastructure.Documents;
using CIT.Plateforme.Infrastructure.Tests.Support;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Securite;

/// <summary>
/// [TEST-SPEC] SEC — stockage documentaire : traversée de répertoire et repli dangereux.
///
/// Deux défauts traités ici :
///
/// 1. <c>LireAsync</c> composait la racine et la clé par <c>Path.Combine</c> sans contrôle. Or
///    <c>Path.Combine</c> ne protège de rien : un second argument absolu remplace le premier, et
///    « ../ » remonte l'arborescence. Une clé altérée en base aurait permis de lire n'importe quel
///    fichier lisible par le compte de service.
///
/// 2. En l'absence de <c>Stockage:RepertoireDocuments</c>, le stockage se rabattait silencieusement
///    sur le répertoire temporaire du système — purgé périodiquement. Le dépôt réussissait, la base
///    gardait la référence, le fichier disparaissait plus tard.
/// </summary>
public sealed class StockageDocumentsSecuriteTests
{
    private static StockageDocumentsFichiers CreerStockage(string? repertoire, string environnement)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [StockageDocumentsFichiers.CleRepertoire] = repertoire,
            })
            .Build();

        return new StockageDocumentsFichiers(configuration, new EnvironnementDeTest(environnement));
    }

    private static string CreerRepertoireTemporaire()
    {
        var chemin = Path.Combine(Path.GetTempPath(), "cit-tests-stockage-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(chemin);
        return chemin;
    }

    // ================================================================================
    // Traversée de répertoire
    // ================================================================================

    [Theory]
    [InlineData("../appsettings.json")]
    [InlineData("..\\appsettings.json")]
    [InlineData("sous/dossier/fichier.pdf")]
    [InlineData("sous\\dossier\\fichier.pdf")]
    [InlineData("../../../Windows/System32/drivers/etc/hosts")]
    public async Task Une_cle_contenant_une_composante_de_repertoire_est_refusee(string cleMalveillante)
    {
        var repertoire = CreerRepertoireTemporaire();
        try
        {
            var stockage = CreerStockage(repertoire, "Production");

            await Assert.ThrowsAsync<ArgumentException>(() => stockage.LireAsync(cleMalveillante));
        }
        finally
        {
            Directory.Delete(repertoire, recursive: true);
        }
    }

    [Fact]
    public async Task Une_cle_absolue_est_refusee()
    {
        var repertoire = CreerRepertoireTemporaire();
        try
        {
            var stockage = CreerStockage(repertoire, "Production");
            var cheminAbsolu = Path.Combine(Path.GetTempPath(), "cible.txt");

            // Path.Combine(racine, cheminAbsolu) retourne le chemin absolu seul : la racine est
            // purement et simplement ignorée. C'est le piège que ce contrôle ferme.
            await Assert.ThrowsAsync<ArgumentException>(() => stockage.LireAsync(cheminAbsolu));
        }
        finally
        {
            Directory.Delete(repertoire, recursive: true);
        }
    }

    [Fact]
    public async Task Une_cle_vide_est_refusee()
    {
        var repertoire = CreerRepertoireTemporaire();
        try
        {
            var stockage = CreerStockage(repertoire, "Production");
            await Assert.ThrowsAsync<ArgumentException>(() => stockage.LireAsync("   "));
        }
        finally
        {
            Directory.Delete(repertoire, recursive: true);
        }
    }

    [Fact]
    public async Task Un_document_depose_reste_relisible_par_sa_cle()
    {
        var repertoire = CreerRepertoireTemporaire();
        try
        {
            var stockage = CreerStockage(repertoire, "Production");
            using var contenu = new MemoryStream("contenu de test"u8.ToArray());

            var cle = await stockage.EnregistrerAsync(contenu, "rapport.pdf");

            await using var relu = await stockage.LireAsync(cle);
            using var lecteur = new StreamReader(relu);

            Assert.Equal("contenu de test", await lecteur.ReadToEndAsync());
        }
        finally
        {
            Directory.Delete(repertoire, recursive: true);
        }
    }

    [Fact]
    public async Task Un_nom_de_fichier_malveillant_au_depot_est_neutralise()
    {
        var repertoire = CreerRepertoireTemporaire();
        try
        {
            var stockage = CreerStockage(repertoire, "Production");
            using var contenu = new MemoryStream("x"u8.ToArray());

            var cle = await stockage.EnregistrerAsync(contenu, "../../evasion.txt");

            // La clé rendue ne contient aucune composante de répertoire, et le fichier a bien été
            // écrit DANS la racine.
            Assert.Equal(cle, Path.GetFileName(cle));
            Assert.True(File.Exists(Path.Combine(repertoire, cle)));
        }
        finally
        {
            Directory.Delete(repertoire, recursive: true);
        }
    }

    // ================================================================================
    // Repli sur le répertoire temporaire
    // ================================================================================

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task Hors_developpement_l_absence_de_repertoire_configure_leve_une_erreur_explicite(string environnement)
    {
        var stockage = CreerStockage(repertoire: null, environnement);
        using var contenu = new MemoryStream("x"u8.ToArray());

        var erreur = await Assert.ThrowsAsync<InvalidOperationException>(
            () => stockage.EnregistrerAsync(contenu, "document.pdf"));

        Assert.Contains(StockageDocumentsFichiers.CleRepertoire, erreur.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task En_developpement_le_repli_temporaire_reste_tolere()
    {
        // Le repli garde son utilité locale : il évite d'imposer une configuration à quiconque
        // clone le dépôt. Ce qui était fautif, c'est qu'il s'appliquait aussi en production.
        var stockage = CreerStockage(repertoire: null, "Development");
        using var contenu = new MemoryStream("x"u8.ToArray());

        var cle = await stockage.EnregistrerAsync(contenu, "document.pdf");

        Assert.False(string.IsNullOrWhiteSpace(cle));
    }

    // ================================================================================
    // L'antivirus « sans opération » ne doit jamais être celui qui sert
    // ================================================================================

    [Fact]
    public void L_implementation_antivirus_enregistree_analyse_reellement()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PlateformeDb"] = "Server=(localdb)\\MSSQLLocalDB;Database=CitPlateformeTestsInexistante;Trusted_Connection=True;",
            })
            .Build();

        CIT.Plateforme.Infrastructure.DependencyInjection.AjouterInfrastructure(services, configuration);

        var descripteur = services.FirstOrDefault(d => d.ServiceType == typeof(IAnalyseAntivirus));

        Assert.NotNull(descripteur);
        Assert.NotEqual(typeof(AucuneAnalyseAntivirusConfiguree), descripteur!.ImplementationType);
        Assert.Equal(typeof(ScannerAntivirusDefender), descripteur.ImplementationType);
    }
}
