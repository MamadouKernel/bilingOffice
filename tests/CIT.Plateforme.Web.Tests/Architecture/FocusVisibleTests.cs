using System.Text.RegularExpressions;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Architecture;

/// <summary>
/// REMÉDIATION UI/UX — accessibilité (WCAG 2.4.7 « Focus Visible »), constatée EN NAVIGATEUR.
///
/// Bootstrap impose <c>.btn:focus-visible { outline: 0 }</c>. Notre halo de focus passait par
/// <c>box-shadow: var(--ring)</c> — mais la classe utilitaire <c>.shadow-sm</c> porte
/// <c>box-shadow … !important</c> et l'écrasait. Résultat mesuré au clavier sur l'accueil : le
/// bouton « Se Connecter » (ombré) recevait le focus SANS aucun repère visible (outline 0, ombre =
/// celle du bouton). Le repère passe désormais par un <c>outline</c>, qu'une box-shadow ne peut pas
/// écraser.
///
/// Cette garde vérifie que la règle <c>.btn:focus-visible</c> déclare bien un <c>outline</c> visible
/// (largeur non nulle). Revenir à un halo par box-shadow seul rouvrirait le défaut.
/// </summary>
public sealed class FocusVisibleTests
{
    [Fact]
    public void Le_focus_clavier_des_boutons_repose_sur_un_outline_non_ecrasable()
    {
        var brut = File.ReadAllText(CheminAppCss());

        // Retirer les commentaires CSS : ils peuvent contenir des exemples de règles (dont
        // « .btn:focus-visible { outline: 0 } » cité pour expliquer le défaut) qui fausseraient
        // la recherche de la règle réelle.
        var css = Regex.Replace(brut, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);

        var regle = Regex.Match(
            css,
            @"\.btn:focus-visible\s*\{(?<corps>[^}]*)\}",
            RegexOptions.IgnoreCase);

        Assert.True(regle.Success, "Règle .btn:focus-visible introuvable dans app.css.");

        var corps = regle.Groups["corps"].Value;

        Assert.Matches(@"outline\s*:\s*[^;]*\b(?:[1-9]\d*)px", corps);
        Assert.DoesNotMatch(@"outline\s*:\s*0(?:px)?\s*;", corps);
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
