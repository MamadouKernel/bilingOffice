using System.Text.RegularExpressions;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Architecture;

/// <summary>
/// REMÉDIATION routage — deux pages ne doivent jamais revendiquer le même GABARIT de route.
///
/// Constaté en recette authentifiée : <c>FicheDossier</c> (<c>/dossiers/{Id:guid}</c>) et
/// <c>DetailDossier</c> (<c>/dossiers/{DossierId:guid}</c>) partageaient le même gabarit
/// (le nom du paramètre ne les distingue pas pour le routeur). Résultat : <c>AmbiguousMatchException</c>
/// et 500 sur TOUTE consultation de dossier — invisible en tests unitaires, qui rendent les
/// composants sans passer par le routeur.
///
/// Cette garde normalise chaque route (paramètres réduits à <c>{}</c>) et refuse tout doublon.
/// </summary>
public sealed class RoutesUniquesTests
{
    [Fact]
    public void Aucune_route_n_est_revendiquee_par_deux_pages()
    {
        var routes = new Dictionary<string, List<string>>();

        foreach (var fichier in FichiersRazor())
        {
            var source = File.ReadAllText(fichier);
            foreach (Match m in Regex.Matches(source, @"@page\s+""(?<route>[^""]+)"""))
            {
                var normalisee = Regex.Replace(m.Groups["route"].Value, @"\{[^}]+\}", "{}");
                if (!routes.TryGetValue(normalisee, out var liste))
                {
                    routes[normalisee] = liste = [];
                }

                liste.Add(Path.GetFileName(fichier));
            }
        }

        var doublons = routes.Where(kv => kv.Value.Distinct().Count() > 1)
            .Select(kv => $"{kv.Key} → {string.Join(", ", kv.Value.Distinct())}")
            .ToList();

        Assert.True(
            doublons.Count == 0,
            "Gabarit(s) de route revendiqué(s) par plusieurs pages (AmbiguousMatchException → 500) :"
            + Environment.NewLine + string.Join(Environment.NewLine, doublons));
    }

    private static IEnumerable<string> FichiersRazor()
    {
        var racine = new DirectoryInfo(AppContext.BaseDirectory);
        while (racine is not null && !Directory.Exists(Path.Combine(racine.FullName, "src")))
        {
            racine = racine.Parent;
        }

        Assert.True(racine is not null, "Racine du dépôt introuvable depuis " + AppContext.BaseDirectory);

        var pages = Path.Combine(racine!.FullName, "src", "CIT.Plateforme.Web", "Components", "Pages");
        return Directory.EnumerateFiles(pages, "*.razor", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));
    }
}
