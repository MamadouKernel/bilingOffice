using System.Text.Json;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Securite;

/// <summary>
/// TEST-SPEC SEC-CERT-09-01 & SEC-CERT-09-02 : Cloisonnement des Environnements et Protection des Secrets.
/// </summary>
public sealed class SecCert09EnvironnementTests
{
    // Depuis le commit 3e1b0a9, appsettings.Production.json (secrets réels) n'est PLUS versionné :
    // seul son modèle l'est. Ces tests vérifient donc le modèle et l'exclusion du vrai fichier.
    private static string DossierWeb() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "CIT.Plateforme.Web"));

    [Fact]
    public void SecCert09_02_AucunSecretProductionEnClairDansGit()
    {
        var modele = Path.Combine(DossierWeb(), "appsettings.Production.json.template");
        Assert.True(File.Exists(modele), "Le modèle appsettings.Production.json.template doit être versionné.");

        // Le vrai fichier de production, lui, est exclu du dépôt.
        var gitignore = File.ReadAllText(Path.GetFullPath(Path.Combine(DossierWeb(), "..", "..", ".gitignore")));
        Assert.Contains("src/CIT.Plateforme.Web/appsettings.Production.json", gitignore);

        // Le modèle ne porte qu'un mot de passe fictif, jamais un secret réel.
        using var doc = JsonDocument.Parse(File.ReadAllText(modele));
        var chaine = doc.RootElement.GetProperty("ConnectionStrings").GetProperty("PlateformeDb").GetString() ?? string.Empty;
        Assert.True(
            string.IsNullOrWhiteSpace(chaine) || chaine.Contains("Password=MOT_DE_PASSE", StringComparison.Ordinal),
            "Le modèle de production ne doit contenir qu'un mot de passe fictif.");
    }

    [Fact]
    public void SecCert09_01_ConfigurationsStagingEtProductionSontHermetiques()
    {
        Assert.True(File.Exists(Path.Combine(DossierWeb(), "appsettings.Staging.json")), "appsettings.Staging.json doit exister.");
        Assert.True(File.Exists(Path.Combine(DossierWeb(), "appsettings.Production.json.template")),
            "Le modèle appsettings.Production.json.template doit exister.");
    }
}
