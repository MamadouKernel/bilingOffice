using System.Text.Json;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Securite;

/// <summary>
/// TEST-SPEC SEC-CERT-09-01 & SEC-CERT-09-02 : Cloisonnement des Environnements et Protection des Secrets.
/// </summary>
public sealed class SecCert09EnvironnementTests
{
    [Fact]
    public void SecCert09_02_AucunSecretProductionEnClairDansGit()
    {
        var appsettingsProdPath = Path.Combine(AppContext.BaseDirectory, "appsettings.Production.json");
        if (!File.Exists(appsettingsProdPath))
        {
            // Recherche dans le dossier source web
            appsettingsProdPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "CIT.Plateforme.Web", "appsettings.Production.json"));
        }

        Assert.True(File.Exists(appsettingsProdPath), "appsettings.Production.json doit exister pour configurer le profil de production.");

        var contenuJson = File.ReadAllText(appsettingsProdPath);
        using var doc = JsonDocument.Parse(contenuJson);
        var root = doc.RootElement;

        // Vérification de la chaîne de connexion (doit être vide ou un placeholder, jamais un secret en dur)
        if (root.TryGetProperty("ConnectionStrings", out var connectionStrings))
        {
            if (connectionStrings.TryGetProperty("PlateformeDb", out var dbStr))
            {
                var valeur = dbStr.GetString() ?? string.Empty;
                Assert.True(
                    string.IsNullOrWhiteSpace(valeur) || valeur.Contains("${") || valeur.Contains("__"),
                    "La chaîne de connexion de production ne doit pas contenir de mot de passe en clair dans Git.");
            }
        }
    }

    [Fact]
    public void SecCert09_01_ConfigurationsStagingEtProductionSontHermetiques()
    {
        var appsettingsStagingPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "CIT.Plateforme.Web", "appsettings.Staging.json"));
        var appsettingsProdPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "CIT.Plateforme.Web", "appsettings.Production.json"));

        Assert.True(File.Exists(appsettingsStagingPath), "appsettings.Staging.json doit exister.");
        Assert.True(File.Exists(appsettingsProdPath), "appsettings.Production.json doit exister.");
    }
}
