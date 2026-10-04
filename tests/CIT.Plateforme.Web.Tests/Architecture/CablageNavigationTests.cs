using System.Text.RegularExpressions;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Architecture;

/// <summary>
/// [TEST-SPEC — UX] LE MENU LATÉRAL DOIT S'APPUYER SUR LES DROITS RÉELS.
///
/// REMÉDIATION RECETTE UAT (D-05/D-15). Le menu était filtré par espace fonctionnel et par lui
/// seul ; à l'intérieur d'un espace, toutes les entrées étaient rendues, quelles que soient les
/// habilitations. Jusqu'à 16 liens sur 22 menaient à « Accès Refusé ».
///
/// La correction déplace la décision dans <c>ConstruireNavigationQuery</c>. Ce test verrouille ce
/// déplacement : si <c>NavMenu.razor</c> recommençait un jour à décider lui-même — en rebranchant
/// un <c>switch</c> sur l'espace actif, ou en testant une permission dans le balisage — le test
/// échoue. C'est exactement la garde que le projet n'avait pas la première fois.
/// </summary>
public sealed class CablageNavigationTests
{
    private static string SourceNavMenu()
    {
        var chemin = Path.Combine(
            RacineDepot().FullName,
            "src", "CIT.Plateforme.Web", "Components", "Layout", "NavMenu.razor");

        Assert.True(File.Exists(chemin), "NavMenu.razor introuvable : " + chemin);
        return SansCommentaires(File.ReadAllText(chemin));
    }

    [Fact]
    public void Le_menu_lateral_delegue_la_construction_au_service_de_navigation()
    {
        var source = SourceNavMenu();

        // Depuis le 04/10/2026 le menu passe par MenuEnCache, qui délègue à ConstruireNavigationQuery
        // (cache de 30 s) : la décision de ce qui est visible reste au service de navigation.
        Assert.Contains("MenuEnCache", source);
        Assert.Contains("Menus", source);
        var cache = File.ReadAllText(Path.Combine(RacineDepot().FullName, "src", "CIT.Plateforme.Web", "Services", "MenuEnCache.cs"));
        Assert.Contains("GetRequiredService<ConstruireNavigationQuery>", cache);
    }

    [Fact]
    public void Le_menu_lateral_ne_decide_plus_lui_meme_de_ce_qu_il_affiche()
    {
        var source = SourceNavMenu();

        // Le motif exact du défaut : un aiguillage sur l'espace actif, qui rendait en bloc toutes
        // les entrées de cet espace.
        Assert.DoesNotContain("switch (_contexte.EspaceActif)", source);
        Assert.DoesNotContain("EspaceFonctionnel.Agent:", source);

        // Et aucune seconde expression de la règle d'autorisation dans le balisage.
        Assert.DoesNotMatch(new Regex(@"Permission\.[A-Za-z]"), source);
        Assert.DoesNotContain("Permissions.Contains", source);
        Assert.DoesNotContain("EstAutoriseAsync", source);
    }

    [Fact]
    public void La_regle_de_portee_n_est_ecrite_qu_a_un_seul_endroit()
    {
        // AutorisationService (le refus serveur) et ConstruireNavigationQuery (l'affichage)
        // appellent tous deux RegleHabilitation. Si l'un des deux la réécrivait, la divergence
        // finirait par rendre le menu plus permissif que le serveur.
        var racine = RacineDepot().FullName;

        var serviceAutorisation = File.ReadAllText(Path.Combine(
            racine, "src", "CIT.Plateforme.Infrastructure", "Identity", "AutorisationService.cs"));
        var constructionMenu = File.ReadAllText(Path.Combine(
            racine, "src", "CIT.Plateforme.Application", "Web", "ConstruireNavigationQuery.cs"));

        Assert.Contains("RegleHabilitation.Couvre", serviceAutorisation);
        Assert.Contains("RegleHabilitation.Couvre", constructionMenu);
    }

    [Fact]
    public void Chaque_entree_du_catalogue_porte_une_route_non_vide()
    {
        var chemin = Path.Combine(
            RacineDepot().FullName,
            "src", "CIT.Plateforme.Application", "Web", "CatalogueNavigation.cs");
        var source = File.ReadAllText(chemin);

        // Une entrée sans route serait un lien mort d'une autre espèce.
        Assert.DoesNotMatch(new Regex(@"new\([^)]*""\s*,\s*""\s*,\s*""""", RegexOptions.Singleline), source);
        Assert.Contains("/agent/dashboard", source);
    }

    private static DirectoryInfo RacineDepot()
    {
        var dossier = new DirectoryInfo(AppContext.BaseDirectory);
        while (dossier is not null && !Directory.Exists(Path.Combine(dossier.FullName, "src")))
        {
            dossier = dossier.Parent;
        }

        Assert.True(dossier is not null, "Racine du dépôt introuvable depuis " + AppContext.BaseDirectory);
        return dossier!;
    }

    private static string SansCommentaires(string source) =>
        Regex.Replace(source, @"@\*.*?\*@", " ", RegexOptions.Singleline);
}
