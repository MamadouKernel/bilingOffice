using System.Text.RegularExpressions;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Architecture;

/// <summary>
/// Gardes des défauts corrigés pendant la RECETTE AUTHENTIFIÉE (build publié), constatés en
/// navigateur sur les pages authentifiées :
///   1. l'en-tête authentifié débordait la PAGE à 320/390 px (logo large + langue + thème + cloche
///      + menu compte) → borne du logo + masquage de la langue sous 576 px ;
///   2. en thème sombre, .btn-outline-primary (4,23:1), .cit-user-avatar (3,23:1) et .btn-light
///      (fond clair figé, 2,96:1) étaient sous le seuil → ré-encrage/re-teinte pour le sombre.
///
/// Ces gardes lisent app.css. Retirer une correction fait tomber la garde correspondante
/// (mutation vérifiée).
/// </summary>
public sealed class RecetteAuthentifieePublishTests
{
    private static string Css() => File.ReadAllText(CheminAppCss());

    [Fact]
    public void En_tete_mobile_borne_le_logo_et_masque_la_langue()
    {
        var css = SansCommentaires(Css());

        // Le point de rupture mobile (max-width: 575.98px) doit exister, borner le logo de marque et
        // masquer le sélecteur de langue — les deux leviers qui suppriment le débordement de page.
        Assert.Matches(@"@media\s*\(max-width:\s*575\.98px\)", css);
        Assert.Matches(@"\.cit-brand-logo\s*\{[^}]*max-width", css);
        Assert.Matches(@"\.cit-header\s+\.cit-language-switcher\s*\{[^}]*display:\s*none", css);
    }

    [Theory]
    [InlineData(@":root\[data-theme=""dark""\]\s*\.btn-outline-primary\s*\{[^}]*color:\s*var\(--azure-400\)")]
    [InlineData(@":root\[data-theme=""dark""\]\s*\.cit-user-avatar\s*\{[^}]*color:")]
    [InlineData(@":root\[data-theme=""dark""\]\s*\.btn-light\s*\{[^}]*background-color:\s*var\(--surface-2\)")]
    public void Corrections_de_contraste_sombre_sont_presentes(string motif)
    {
        Assert.Matches(motif, SansCommentaires(Css()));
    }

    private static string SansCommentaires(string css) =>
        Regex.Replace(css, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);

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
