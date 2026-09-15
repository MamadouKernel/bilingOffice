using Xunit;

namespace CIT.Plateforme.Web.Tests.Architecture;

/// <summary>
/// [TEST-SPEC — UI] LA BARRE D'EN-TÊTE NE DOIT PAS DÉBORDER SUR TABLETTE (D-17).
///
/// La recette a mesuré un débordement horizontal de toute la page — +160 px à 768 px, +170 px sur
/// le seul <c>.cit-header</c> — pour un compte porteur d'un nom de société. Entre 768 et ~1100 px,
/// la barre affichait simultanément la marque, le bandeau société (nom + profil) et l'identité
/// utilisateur (nom + rôle), sans jamais les réduire.
///
/// La correction relève les points de bascule (le bandeau société ne réapparaît qu'en XL,
/// l'identité qu'en LG) et tronque le nom de société. Ce test verrouille ces trois décisions : une
/// régression vers <c>d-md-flex</c> / <c>d-sm-flex</c>, ou la perte de la troncature, le fait échouer.
/// </summary>
public sealed class BarreEnteteResponsiveTests
{
    private static string SourceHeader()
    {
        var racine = new DirectoryInfo(AppContext.BaseDirectory);
        while (racine is not null && !Directory.Exists(Path.Combine(racine.FullName, "src")))
        {
            racine = racine.Parent;
        }

        Assert.True(racine is not null, "Racine du dépôt introuvable depuis " + AppContext.BaseDirectory);
        return File.ReadAllText(Path.Combine(
            racine!.FullName, "src", "CIT.Plateforme.Web", "Components", "Layout", "HeaderBar.razor"));
    }

    [Fact]
    public void Le_bandeau_societe_ne_reapparait_qu_en_XL_et_tronque_le_nom()
    {
        var source = SourceHeader();

        // Le bandeau société est masqué en dessous de XL — il ne s'affiche plus sur la plage tablette.
        Assert.Contains("d-none d-xl-flex", source);
        Assert.DoesNotContain("d-none d-md-flex align-items-center gap-2 ms-2 px-3 py-1 bg-light", source);

        // Le nom, potentiellement long, est tronqué au lieu de pousser la barre.
        Assert.Contains("text-truncate", source);
    }

    [Fact]
    public void L_identite_utilisateur_ne_s_affiche_qu_en_LG()
    {
        var source = SourceHeader();

        Assert.Contains("cit-user-identity d-none d-lg-flex", source);
        Assert.DoesNotContain("cit-user-identity d-none d-sm-flex", source);
    }
}
