using System.Text.RegularExpressions;

namespace CIT.Plateforme.Web.Tests.Securite;

/// <summary>
/// GARDE-FOU — la plateforme ne charge AUCUNE ressource depuis un serveur tiers.
///
/// Motivation, dans l'ordre de gravité :
///  1. Fuite de données. Un QR code 2FA était rendu par api.qrserver.com : le secret TOTP
///     partagé — qui EST le second facteur — partait chez un tiers dans une query string.
///  2. Disponibilité. Un terminal portuaire n'a pas de connectivité sortante garantie ; toute
///     ressource distante est un point de panne hors du contrôle de la plateforme.
///  3. Chaîne d'approvisionnement. Un fichier servi par un tiers change sans que le dépôt bouge :
///     ni reproductible, ni auditable.
///  4. Confidentialité. Chaque appel divulgue au tiers l'IP du poste client et l'URL appelante.
///
/// Ce test existe parce qu'une revue ponctuelle ne tient pas dans le temps. Il couvre
/// DÉLIBÉRÉMENT deux surfaces, car la première seule laisse passer le cas le plus grave :
///  - le balisage (src=, href=, url()) ;
///  - les littéraux d'URL en C# — c'est ainsi que l'appel à api.qrserver.com était construit,
///    puis injecté via src="@qrCodeUrl". Un scan limité aux attributs ne l'aurait jamais vu.
/// </summary>
public class AucuneRessourceExterneTests
{
    /// <summary>
    /// Hôtes tolérés : aucun n'est JAMAIS requêté par un navigateur. Toute autre valeur fait
    /// échouer le test. Cette liste est volontairement close et nominative — on n'y ajoute pas
    /// un hôte pour faire passer la construction, on retire la ressource externe.
    /// </summary>
    private static readonly HashSet<string> HotesTolerees = new(StringComparer.OrdinalIgnoreCase)
    {
        "www.w3.org",             // espaces de noms XML / SVG (xmlns), jamais résolus
        "schemas.microsoft.com",  // idem, déclarations de schéma
        "json.schemastore.org",   // schéma de launchSettings.json, consommé par l'IDE
        "aka.ms",                 // commentaire du gabarit ASP.NET (UseHsts)
        "localhost",
        "127.0.0.1",
    };

    /// <summary>
    /// wwwroot/lib est du tiers vendorisé (Bootstrap) : il est servi EN LOCAL. Vérifié sur
    /// pièces — les URL qu'il contient sont exclusivement des commentaires et des
    /// sourceMappingURL, aucune dans un attribut src/href. Le réécrire n'aurait aucun sens.
    /// </summary>
    private static readonly string[] CheminsExclus =
    [
        $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
        $"wwwroot{Path.DirectorySeparatorChar}lib{Path.DirectorySeparatorChar}",
    ];

    private static readonly Regex UrlExterne =
        new(@"https?://(?<hote>[^/\s""'<>()\]]+)", RegexOptions.Compiled);

    private static string RacineDepot()
    {
        var repertoire = new DirectoryInfo(AppContext.BaseDirectory);
        while (repertoire is not null && !Directory.Exists(Path.Combine(repertoire.FullName, "src")))
        {
            repertoire = repertoire.Parent;
        }

        Assert.NotNull(repertoire);
        return repertoire!.FullName;
    }

    private static IEnumerable<string> FichiersAAnalyser()
    {
        var racineSrc = Path.Combine(RacineDepot(), "src");
        string[] extensions = ["*.razor", "*.cshtml", "*.cs", "*.css", "*.js", "*.html"];

        return extensions
            .SelectMany(e => Directory.EnumerateFiles(racineSrc, e, SearchOption.AllDirectories))
            .Where(f => !CheminsExclus.Any(f.Contains));
    }

    [Fact]
    public void AucunFichierLivre_NeReferenceUnHoteExterne()
    {
        var infractions = new List<string>();

        foreach (var fichier in FichiersAAnalyser())
        {
            var lignes = File.ReadAllLines(fichier);
            for (var i = 0; i < lignes.Length; i++)
            {
                foreach (Match correspondance in UrlExterne.Matches(lignes[i]))
                {
                    var hote = correspondance.Groups["hote"].Value;
                    if (HotesTolerees.Contains(hote))
                    {
                        continue;
                    }

                    var chemin = Path.GetRelativePath(RacineDepot(), fichier);
                    infractions.Add($"{chemin}:{i + 1} → {hote}");
                }
            }
        }

        Assert.True(
            infractions.Count == 0,
            "Ressource(s) externe(s) détectée(s) — la plateforme doit tout servir en local.\n"
            + "Vendoriser le fichier sous wwwroot/, ou remplacer l'appel par un traitement local\n"
            + "(cf. IGenerateurQrCode pour le QR code 2FA). N'ajoutez PAS l'hôte à la liste tolérée.\n\n"
            + string.Join("\n", infractions));
    }

    /// <summary>
    /// Régression nominative : le secret TOTP a réellement fui vers ce service. Le test
    /// précédent le couvre déjà, mais celui-ci nomme le défaut pour qu'un retour en arrière
    /// soit lisible immédiatement dans le rapport d'exécution.
    /// </summary>
    [Fact]
    public void EnrolementDeuxiemeFacteur_NeSousTraitePasLeRenduDuQrCode()
    {
        var page = Path.Combine(
            RacineDepot(),
            "src", "CIT.Plateforme.Web", "Components", "Pages", "Comptes",
            "ActiverDoubleAuthentification.razor");

        var source = File.ReadAllText(page);

        Assert.DoesNotContain("qrserver.com", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GenererDataUri", source, StringComparison.Ordinal);
    }
}
