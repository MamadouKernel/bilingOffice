using System.Text.RegularExpressions;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Architecture;

/// <summary>
/// REMÉDIATION UI/UX — cohérence / SSR. Trois pages tarification rendaient DEUX fois le même
/// &lt;PageTitle&gt; (copié-collé). En rendu, deux &lt;PageTitle&gt; identiques écrivent deux fois
/// dans &lt;HeadOutlet&gt; — au mieux redondant, au pire deux balises &lt;title&gt; concurrentes.
///
/// Cette garde interdit qu'un même titre apparaisse deux fois dans un même fichier. Les titres
/// CONDITIONNELS (branches if/else portant des libellés différents) restent permis : seuls les
/// doublons à l'identique sont refusés.
/// </summary>
public sealed class TitresPageTests
{
    private static readonly Regex PageTitle = new(
        @"<PageTitle>(?<contenu>.*?)</PageTitle>",
        RegexOptions.Singleline | RegexOptions.Compiled);

    [Fact]
    public void Aucune_page_ne_repete_un_meme_PageTitle_a_l_identique()
    {
        var fautifs = new List<string>();

        foreach (var fichier in FichiersRazor())
        {
            var source = File.ReadAllText(fichier);

            var doublons = PageTitle.Matches(source)
                .Select(m => m.Groups["contenu"].Value.Trim())
                .GroupBy(t => t)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var titre in doublons)
            {
                fautifs.Add($"{Path.GetFileName(fichier)} → « {titre} » ×{PageTitle.Matches(source).Count(m => m.Groups["contenu"].Value.Trim() == titre)}");
            }
        }

        Assert.True(
            fautifs.Count == 0,
            "PageTitle dupliqué à l'identique (rendu deux fois dans HeadOutlet)." + Environment.NewLine
            + string.Join(Environment.NewLine, fautifs));
    }

    private static IEnumerable<string> FichiersRazor()
    {
        var racine = new DirectoryInfo(AppContext.BaseDirectory);
        while (racine is not null && !Directory.Exists(Path.Combine(racine.FullName, "src")))
        {
            racine = racine.Parent;
        }

        Assert.True(racine is not null, "Racine du dépôt introuvable depuis " + AppContext.BaseDirectory);

        var composants = Path.Combine(racine!.FullName, "src", "CIT.Plateforme.Web", "Components");
        return Directory.EnumerateFiles(composants, "*.razor", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));
    }
}
