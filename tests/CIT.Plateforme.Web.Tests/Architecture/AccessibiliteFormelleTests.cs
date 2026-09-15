using System.Text.RegularExpressions;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Architecture;

/// <summary>
/// Gardes issues de l'AUDIT WCAG 2.2 AA (build publié, pages authentifiées) :
///   - 2.4.11 « Focus Not Obscured » : l'en-tête sticky exige un <c>scroll-padding-top</c> pour que
///     les cibles de focus/ancre ne se calent pas dessous ;
///   - 4.1.3 « Status Messages » : les alertes dynamiques d'erreur (<c>alert-danger</c>) et de
///     succès (<c>alert-success</c>) doivent porter un rôle live (<c>role="alert"</c> /
///     <c>role="status"</c>) pour être annoncées au lecteur d'écran sans déplacer le focus.
/// </summary>
public sealed class AccessibiliteFormelleTests
{
    [Fact]
    public void En_tete_sticky_reserve_le_defilement_pour_le_focus_2_4_11()
    {
        var css = Regex.Replace(File.ReadAllText(CheminAppCss()), @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        Assert.Matches(@"scroll-padding-top:\s*var\(--header-h\)", css);
    }

    [Fact]
    public void Les_alertes_dynamiques_portent_un_role_live_4_1_3()
    {
        var fautifs = new List<string>();

        foreach (var fichier in FichiersRazor())
        {
            var nom = Path.GetFileName(fichier);
            foreach (var ligne in File.ReadAllLines(fichier))
            {
                var estAlerteDynamique =
                    ligne.Contains("class=\"alert alert-danger", StringComparison.Ordinal)
                    || ligne.Contains("class=\"alert alert-success", StringComparison.Ordinal);

                if (estAlerteDynamique
                    && !ligne.Contains("role=", StringComparison.Ordinal)
                    && !ligne.Contains("aria-live", StringComparison.Ordinal))
                {
                    fautifs.Add($"{nom} → {ligne.Trim()[..Math.Min(60, ligne.Trim().Length)]}");
                }
            }
        }

        Assert.True(
            fautifs.Count == 0,
            "Alerte d'erreur/succès dynamique sans rôle live (WCAG 4.1.3) : le lecteur d'écran ne "
            + "l'annoncera pas. Ajoutez role=\"alert\" (danger) ou role=\"status\" (success)."
            + Environment.NewLine + string.Join(Environment.NewLine, fautifs));
    }

    private static string CheminAppCss() => Path.Combine(Racine(), "src", "CIT.Plateforme.Web", "wwwroot", "app.css");

    private static IEnumerable<string> FichiersRazor()
    {
        var composants = Path.Combine(Racine(), "src", "CIT.Plateforme.Web", "Components");
        return Directory.EnumerateFiles(composants, "*.razor", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));
    }

    private static string Racine()
    {
        var racine = new DirectoryInfo(AppContext.BaseDirectory);
        while (racine is not null && !Directory.Exists(Path.Combine(racine.FullName, "src")))
        {
            racine = racine.Parent;
        }

        Assert.True(racine is not null, "Racine du dépôt introuvable depuis " + AppContext.BaseDirectory);
        return racine!.FullName;
    }
}
