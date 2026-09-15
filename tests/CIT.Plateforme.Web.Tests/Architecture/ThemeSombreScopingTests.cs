using System.Text.RegularExpressions;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Architecture;

/// <summary>
/// REMÉDIATION UI/UX — thème (WCAG 1.4.3), défaut constaté EN NAVIGATEUR.
///
/// <para><c>theme-manager.js</c> ne pose <c>data-theme</c> QUE lorsque le sombre est demandé
/// (préférence système sombre, ou bascule enregistrée). Pour un visiteur en clair par défaut,
/// <c>data-theme</c> est ABSENT — pas « light ».</para>
///
/// <para>Conséquence : une règle sombre écrite <c>:root:not([data-theme="light"])</c> s'applique
/// aussi à l'état clair par défaut (absent ≠ « light »). C'est ainsi que <c>.text-primary</c>
/// prenait l'azur clair (3,16:1 sur blanc) chez tout visiteur en clair, et que les pastilles
/// « subtle » gardaient un fond clair avec une encre claire (2,9:1) en sombre.</para>
///
/// <para>La seule portée correcte pour un style RÉSERVÉ au sombre est <c>:root[data-theme="dark"]</c>
/// — exactement la façon dont les jetons du thème sombre sont déclarés. Cette garde interdit le
/// motif fautif dans <c>app.css</c>.</para>
/// </summary>
public sealed class ThemeSombreScopingTests
{
    [Fact]
    public void Aucune_regle_sombre_ne_cible_l_etat_clair_par_defaut()
    {
        var css = File.ReadAllText(CheminAppCss());

        // Retirer les commentaires (ils citent le motif fautif pour l'expliquer).
        css = Regex.Replace(css, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);

        var occurrences = Regex.Matches(css, @":root:not\(\[data-theme=""light""\]\)").Count;

        Assert.True(
            occurrences == 0,
            $"{occurrences} règle(s) utilisent :root:not([data-theme=\"light\"]). Ce sélecteur "
            + "capture l'état clair PAR DÉFAUT (data-theme absent), où les styles sombres cassent le "
            + "contraste. Utilisez :root[data-theme=\"dark\"] pour un style réservé au sombre.");
    }

    private static string CheminAppCss()
    {
        var racine = new DirectoryInfo(AppContext.BaseDirectory);
        while (racine is not null && !Directory.Exists(Path.Combine(racine.FullName, "src")))
        {
            racine = racine.Parent;
        }

        Assert.True(racine is not null, "Racine du dépôt introuvable depuis " + AppContext.BaseDirectory);

        return Path.Combine(racine!.FullName, "src", "CIT.Plateforme.Web", "wwwroot", "app.css");
    }
}
