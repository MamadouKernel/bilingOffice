using System.Net;
using CIT.Plateforme.Infrastructure.Notifications;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Notifications;

/// <summary>Habillage HTML de marque commun à tous les e-mails : structure, bouton d'action à
/// partir d'un lien, et échappement du contenu (anti-injection HTML).</summary>
public sealed class GabaritEmailHtmlTests
{
    [Fact]
    public void Construire_produit_un_document_html_de_marque()
    {
        var html = GabaritEmailHtml.Construire("Votre ticket TCK-42 a été transféré", "Bonjour,\nvotre dossier avance.");

        Assert.StartsWith("<!DOCTYPE html>", html);
        Assert.Contains("YABA", html);
        // Le contenu est HTML-encodé (accents → entités numériques), donc on compare la forme encodée.
        Assert.Contains(WebUtility.HtmlEncode("Votre ticket TCK-42 a été transféré"), html);
        Assert.Contains("votre dossier avance.", html);
    }

    [Fact]
    public void Construire_transforme_une_ligne_lien_en_bouton_cliquable()
    {
        var corps = "Cliquez pour définir votre mot de passe.\n\nDéfinir mon mot de passe : https://portail.example.com/reinit?j=abc";

        var html = GabaritEmailHtml.Construire("Invitation", corps);

        // Le libellé devient le texte du bouton et l'URL son href.
        Assert.Contains(WebUtility.HtmlEncode("Définir mon mot de passe"), html);
        Assert.Contains("href=\"https://portail.example.com/reinit?j=abc\"", html);
    }

    [Fact]
    public void Construire_echappe_le_contenu_pour_neutraliser_toute_injection_html()
    {
        var html = GabaritEmailHtml.Construire("<script>alert(1)</script>", "corps <b>brut</b>");

        Assert.DoesNotContain("<script>alert(1)</script>", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("&lt;b&gt;brut&lt;/b&gt;", html);
    }
}
