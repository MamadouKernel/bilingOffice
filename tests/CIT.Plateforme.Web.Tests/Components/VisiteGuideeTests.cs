using Bunit;
using CIT.Plateforme.Web.Components.Layout;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Components;

/// <summary>Visite guidée : étapes rendues traduites dans un template inerte, clé propre à l'utilisateur.</summary>
public sealed class VisiteGuideeTests : BunitContext
{
    public VisiteGuideeTests() => LocalisationDeTest.Enregistrer(Services);

    [Fact]
    public void Les_etapes_sont_rendues_traduites_avec_une_cle_propre_a_l_utilisateur()
    {
        var utilisateur = Guid.NewGuid();

        var cut = Render<VisiteGuidee>(p => p.Add(c => c.UtilisateurId, utilisateur));

        var modele = cut.Find("template#cit-visite");
        Assert.Equal(utilisateur.ToString("N"), modele.GetAttribute("data-cle"));
        // Le contenu d'un <template> vit dans un fragment séparé (inerte) : c'est là que le script le lit.
        var etapes = ((AngleSharp.Html.Dom.IHtmlTemplateElement)modele).Content.QuerySelectorAll("[data-etape]").ToList();
        Assert.Equal(7, etapes.Count);
        // Aucune clé de traduction brute ne doit apparaître (ex. « Tour.MenuTitle »).
        Assert.DoesNotContain(etapes, e => e.GetAttribute("data-titre")!.StartsWith("Tour.", StringComparison.Ordinal)
                                         || e.TextContent.Trim().StartsWith("Tour.", StringComparison.Ordinal));
        Assert.Equal(string.Empty, etapes[0].GetAttribute("data-cible"));
    }
}
