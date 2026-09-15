using Bunit;
using CIT.Plateforme.Web.Components.Shared;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Components;

/// <summary>
/// REMÉDIATION UI/UX — navigation. Le fil d'Ariane doit être un vrai landmark de navigation :
/// un &lt;nav aria-label="Fil d'Ariane"&gt; contenant une liste ordonnée dont le dernier segment
/// porte aria-current="page" et n'est PAS un lien (position courante), les précédents restant
/// cliquables lorsqu'une destination est fournie.
/// </summary>
public sealed class FilArianeTests : BunitContext
{
    private static List<FilAriane.ElementAriane> Trois() =>
    [
        new("Accueil", "/"),
        new("Dossier DOS-1", "/dossiers/1"),
        new("Ticket TCK-1")
    ];

    [Fact]
    public void Sans_element_ne_rend_rien()
    {
        var rendu = Render<FilAriane>(p => p.Add(c => c.Elements, new List<FilAriane.ElementAriane>()));

        Assert.Empty(rendu.Markup.Trim());
    }

    [Fact]
    public void Expose_un_landmark_de_navigation_nomme()
    {
        var rendu = Render<FilAriane>(p => p.Add(c => c.Elements, Trois()));

        var nav = rendu.Find("nav");
        Assert.Equal("Fil d'Ariane", nav.GetAttribute("aria-label"));
        Assert.NotNull(rendu.Find("ol"));
    }

    [Fact]
    public void Le_dernier_segment_est_la_page_courante_non_cliquable()
    {
        var rendu = Render<FilAriane>(p => p.Add(c => c.Elements, Trois()));

        var courant = rendu.Find("[aria-current=\"page\"]");
        Assert.Equal("Ticket TCK-1", courant.TextContent.Trim());
        Assert.Equal("span", courant.NodeName, ignoreCase: true);
    }

    [Fact]
    public void Les_segments_precedents_avec_destination_sont_des_liens()
    {
        var rendu = Render<FilAriane>(p => p.Add(c => c.Elements, Trois()));

        var liens = rendu.FindAll("a");
        Assert.Equal(2, liens.Count);
        Assert.Equal("/", liens[0].GetAttribute("href"));
        Assert.Equal("/dossiers/1", liens[1].GetAttribute("href"));
    }
}
