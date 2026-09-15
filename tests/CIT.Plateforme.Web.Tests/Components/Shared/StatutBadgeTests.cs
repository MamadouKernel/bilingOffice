using System.Globalization;
using Bunit;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Web.Components.Shared;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Components.Shared;

/// <summary>
/// StatutBadge — classe CSS + libellé. Depuis NFR-07, le libellé est **localisé** : le test épingle
/// donc explicitement la culture (comme <c>NfrCert07IntegrationTests</c>), sinon la culture de
/// l'hôte de test rendrait le résultat non déterministe.
/// </summary>
public sealed class StatutBadgeTests : BunitContext
{
    [Theory]
    [InlineData(StatutTicket.EnAttenteDePriseEnCharge, "cit-badge-attente", "En attente")]
    [InlineData(StatutTicket.EnCours, "cit-badge-encours", "En cours")]
    [InlineData(StatutTicket.EnAttenteClient, "cit-badge-client", "En attente client")]
    [InlineData(StatutTicket.Cloture, "cit-badge-cloture", "Clôturé")]
    [InlineData(StatutTicket.Annule, "cit-badge-annule", "Annulé")]
    public void StatutBadge_RenduCorrect_ClasseEtLibelle_FR(StatutTicket statut, string classeAttendue, string libelleAttendu)
    {
        AvecCulture("fr-FR", () =>
        {
            var cut = Render<StatutBadge>(parameters => parameters.Add(p => p.Statut, statut));
            cut.MarkupMatches($"<span class=\"cit-badge {classeAttendue}\" diff:ignoreChildren>{libelleAttendu}</span>");
        });
    }

    [Theory]
    [InlineData(StatutTicket.EnAttenteDePriseEnCharge, "Pending")]
    [InlineData(StatutTicket.EnCours, "In progress")]
    [InlineData(StatutTicket.EnAttenteClient, "Awaiting client")]
    [InlineData(StatutTicket.Cloture, "Closed")]
    [InlineData(StatutTicket.Annule, "Cancelled")]
    public void StatutBadge_LibelleSuitLaCulture_EN(StatutTicket statut, string libelleAttendu)
    {
        AvecCulture("en-US", () =>
        {
            var cut = Render<StatutBadge>(parameters => parameters.Add(p => p.Statut, statut));
            Assert.Contains(libelleAttendu, cut.Markup);
        });
    }

    [Fact]
    public void StatutBadge_RejetManifeste_RenduClasseDanger()
    {
        AvecCulture("fr-FR", () =>
        {
            var cut = Render<StatutBadge>(parameters => parameters
                .Add(p => p.Statut, StatutTicket.Annule)
                .Add(p => p.EstRejetManifeste, true));

            Assert.Contains("cit-badge-rejet", cut.Markup);
            Assert.Contains("Rejeté", cut.Markup);
        });
    }

    [Fact]
    public void StatutBadge_RejetManifeste_SuitLaCulture_EN()
    {
        AvecCulture("en-US", () =>
        {
            var cut = Render<StatutBadge>(parameters => parameters
                .Add(p => p.Statut, StatutTicket.Annule)
                .Add(p => p.EstRejetManifeste, true));

            Assert.Contains("Rejected", cut.Markup);
        });
    }

    private static void AvecCulture(string culture, System.Action action)
    {
        var precedente = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo(culture);
            action();
        }
        finally
        {
            CultureInfo.CurrentUICulture = precedente;
        }
    }
}
