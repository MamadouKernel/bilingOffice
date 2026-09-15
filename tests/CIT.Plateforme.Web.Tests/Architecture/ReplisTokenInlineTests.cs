using System.Text.RegularExpressions;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Architecture;

/// <summary>
/// REMÉDIATION design/thème — pas de repli codé en dur derrière un token dans un style inline.
///
/// Un style comme <c>background-color: var(--surface, #ffffff)</c> paraît sûr, mais le repli
/// <c>#ffffff</c> est un piège de thème : si le token venait à manquer, le fond redeviendrait
/// CLAIR — y compris en sombre. La garde <see cref="FondsThematisesTests"/> ne le voyait pas :
/// elle ignore tout ce qui contient <c>var(--</c>. Comme <c>--surface</c> existe toujours, ces
/// replis sont en outre du code mort.
///
/// Cette garde refuse tout <c>var(--jeton, #hex)</c> dans un attribut <c>style=</c> Razor : on écrit
/// <c>var(--jeton)</c>, sans repli littéral. Les styles inline DYNAMIQUES ou tokenisés sans repli
/// restent permis — on ne bannit pas l'inline, seulement le repli codé en dur.
/// </summary>
public sealed class ReplisTokenInlineTests
{
    private static readonly Regex StyleInline = new("style=\"(?<contenu>[^\"]*)\"", RegexOptions.Compiled);
    private static readonly Regex RepliHex = new(@"var\(\s*--[a-z0-9-]+\s*,\s*#[0-9a-fA-F]{3,6}\s*\)", RegexOptions.Compiled);

    [Fact]
    public void Aucun_style_inline_ne_porte_un_repli_hexadecimal_derriere_un_token()
    {
        var fautifs = new List<string>();

        foreach (var fichier in FichiersRazor())
        {
            var source = File.ReadAllText(fichier);
            foreach (Match style in StyleInline.Matches(source))
            {
                if (RepliHex.IsMatch(style.Groups["contenu"].Value))
                {
                    var ligne = source.Take(style.Index).Count(c => c == '\n') + 1;
                    fautifs.Add($"{Path.GetFileName(fichier)}:{ligne}");
                }
            }
        }

        Assert.True(
            fautifs.Count == 0,
            "Repli hexadécimal derrière un token dans un style inline (piège de thème + code mort). "
            + "Écrivez var(--jeton) sans repli." + Environment.NewLine
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
