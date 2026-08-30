using Bunit;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Web.Components.Shared;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Components.Shared;

public sealed class StatutBadgeTests : BunitContext
{
    [Theory]
    [InlineData(StatutTicket.EnAttenteDePriseEnCharge, "cit-badge-attente", "En attente")]
    [InlineData(StatutTicket.EnCours, "cit-badge-encours", "En cours")]
    [InlineData(StatutTicket.EnAttenteClient, "cit-badge-client", "En attente client")]
    [InlineData(StatutTicket.Cloture, "cit-badge-cloture", "Clôturé")]
    [InlineData(StatutTicket.Annule, "cit-badge-annule", "Annulé")]
    public void StatutBadge_RenduCorrect_ClasseEtLibelle(StatutTicket statut, string classeAttendue, string libelleAttendu)
    {
        var cut = Render<StatutBadge>(parameters => parameters.Add(p => p.Statut, statut));

        cut.MarkupMatches($"<span class=\"cit-badge {classeAttendue}\" diff:ignoreChildren>{libelleAttendu}</span>");
    }

    [Fact]
    public void StatutBadge_RejetManifeste_RenduClasseDanger()
    {
        var cut = Render<StatutBadge>(parameters => parameters
            .Add(p => p.Statut, StatutTicket.Annule)
            .Add(p => p.EstRejetManifeste, true));

        Assert.Contains("cit-badge-rejet", cut.Markup);
        Assert.Contains("Rejeté", cut.Markup);
    }
}
