using System.Globalization;
using System.Text.RegularExpressions;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Architecture;

/// <summary>
/// [TEST-SPEC — UI] UN FOND CLAIR CODÉ EN DUR CASSE LE THÈME SOMBRE.
///
/// REMÉDIATION UI-R02. Le système de thème était correct : <c>.cit-card</c> peint
/// <c>var(--surface)</c>, et <c>.text-dark</c> suit <c>var(--ink)</c>. Ce qui l'a mis en défaut,
/// ce sont quatre attributs <c>style=</c> en ligne qui figeaient le fond à <c>#ffffff</c> /
/// <c>#f8fafc</c> — l'inline l'emporte sur la classe pour le fond, mais le texte, lui, continuait
/// de suivre le token. En thème sombre : encre claire sur carte blanche, <b>1,17:1</b>.
///
/// Le défaut n'était pas visible en revue de code : chaque ligne prise isolément semblait anodine.
/// Il ne l'était pas non plus en tests : aucun ne regardait la couleur. Cette garde comble ce trou.
///
/// <para><b>Périmètre volontairement étroit.</b> Seuls les <b>fonds clairs</b> codés en dur sont
/// refusés. Restent permis : les dégradés sombres de bandeau (texte blanc, lisibles dans les deux
/// thèmes), les couleurs d'accent sur une bordure ou une icône, et les valeurs de repli d'un token
/// (<c>var(--surface, #ffffff)</c>) — qui ne s'appliquent que si le token manque.</para>
/// </summary>
public sealed class FondsThematisesTests
{
    /// <summary>
    /// Luminance relative au-delà de laquelle un fond est « clair » : il portera donc du texte
    /// sombre, et deviendra illisible dès que l'encre bascule avec le thème.
    /// </summary>
    private const double SeuilClarte = 0.45;

    private static readonly Regex Fond = new(
        @"background(?:-color)?\s*:\s*(?<valeur>[^;""']+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex Hexadecimal = new(
        @"#(?<hex>[0-9a-fA-F]{6}|[0-9a-fA-F]{3})\b", RegexOptions.Compiled);

    [Fact]
    public void Aucune_page_ne_fige_un_fond_clair_hors_des_tokens_de_theme()
    {
        var fautifs = new List<string>();

        foreach (var fichier in FichiersRazor())
        {
            var source = File.ReadAllText(fichier);
            var nom = Path.GetFileName(fichier);

            foreach (Match declaration in Fond.Matches(source))
            {
                var valeur = declaration.Groups["valeur"].Value;

                // Repli d'un token : la valeur en dur ne sert que si le token n'existe pas.
                if (valeur.Contains("var(--", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (Match couleur in Hexadecimal.Matches(valeur))
                {
                    if (Luminance(couleur.Groups["hex"].Value) > SeuilClarte)
                    {
                        var ligne = source.Take(declaration.Index).Count(c => c == '\n') + 1;
                        fautifs.Add($"{nom}:{ligne} → {declaration.Value.Trim()}");
                    }
                }
            }
        }

        Assert.True(
            fautifs.Count == 0,
            "Fond clair codé en dur : il restera clair en thème sombre alors que le texte "
            + "basculera, et le contenu deviendra illisible (UI-R02). Utilisez var(--surface), "
            + "var(--surface-2) ou var(--surface-sunken)." + Environment.NewLine
            + string.Join(Environment.NewLine, fautifs));
    }

    private static double Luminance(string hex)
    {
        if (hex.Length == 3)
        {
            hex = string.Concat(hex.Select(c => new string(c, 2)));
        }

        double Canal(int position)
        {
            var valeur = int.Parse(hex.Substring(position, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
            return valeur <= 0.03928 ? valeur / 12.92 : Math.Pow((valeur + 0.055) / 1.055, 2.4);
        }

        return (0.2126 * Canal(0)) + (0.7152 * Canal(2)) + (0.0722 * Canal(4));
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
