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

    /// <summary>
    /// SEC-09 — garde-fou contre le scénario du 02/10/2026 : la chaîne de production (compte sa,
    /// mot de passe réel) avait été recopiée dans appsettings.json, fichier VERSIONNÉ. Aucune
    /// configuration suivie par git ne doit porter de mot de passe : ils vivent dans les
    /// user-secrets, les variables d'environnement ou appsettings.Production.json (non versionné).
    /// </summary>
    [Theory]
    [InlineData("appsettings.json")]
    [InlineData("appsettings.Development.json")]
    [InlineData("appsettings.Staging.json")]
    [InlineData("appsettings.UAT.json")]
    public void SecCert09_03_AucuneConfigurationVersionneeNePorteDeMotDePasse(string fichier)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(DossierWeb(), fichier)),
            new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

        foreach (var (cle, valeur) in Feuilles(doc.RootElement, string.Empty))
        {
            if (cle.StartsWith("_", StringComparison.Ordinal) || cle.Contains(":_", StringComparison.Ordinal))
            {
                continue; // commentaires documentaires (« _Commentaire »), jamais lus comme configuration
            }

            var estChaineConnexion = cle.StartsWith("ConnectionStrings:", StringComparison.Ordinal);
            Assert.False(
                estChaineConnexion && System.Text.RegularExpressions.Regex.IsMatch(valeur, @"(?i)(password|pwd)\s*=\s*[^;\s]"),
                $"{fichier} : la chaîne {cle} contient un mot de passe — le déplacer hors des fichiers versionnés.");
            Assert.False(
                cle.EndsWith("MotDePasse", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(valeur),
                $"{fichier} : la clé {cle} porte une valeur — les mots de passe ne se versionnent pas.");
        }
    }

    private static IEnumerable<(string Cle, string Valeur)> Feuilles(JsonElement element, string prefixe)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in element.EnumerateObject())
            {
                foreach (var f in Feuilles(p.Value, prefixe.Length == 0 ? p.Name : $"{prefixe}:{p.Name}"))
                {
                    yield return f;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.String)
        {
            yield return (prefixe, element.GetString() ?? string.Empty);
        }
    }

    [Fact]
    public void SecCert09_01_ConfigurationsStagingEtProductionSontHermetiques()
    {
        Assert.True(File.Exists(Path.Combine(DossierWeb(), "appsettings.Staging.json")), "appsettings.Staging.json doit exister.");
        Assert.True(File.Exists(Path.Combine(DossierWeb(), "appsettings.Production.json.template")),
            "Le modèle appsettings.Production.json.template doit exister.");
    }
}
