using System.Globalization;
using Bunit;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Web.Components.Shared;
using CIT.Plateforme.Web.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Components.Shared;

/// <summary>Historique lisible : libellés accentués, détails techniques traduits, GUID masqués.</summary>
public sealed class ChronologieTicketTests : BunitContext
{
    public ChronologieTicketTests() =>
        Services.AddSingleton<IStringLocalizer<SharedResources>, SharedResourcesLocalizer>();

    [Fact]
    public void Historique_AfficheLibellesEtDetailsLisibles()
    {
        var precedente = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
        try
        {
            ChronologieItemVue Ev(string code, string? detail) => new(Guid.NewGuid(), code, detail, null, "Agent", DateTime.UtcNow);
            var cut = Render<ChronologieTicket>(p => p.Add(c => c.Evenements, new[]
            {
                Ev("DocumentDepose", "permis.pdf"),
                Ev("TicketPrisEnCharge", $"operateur={Guid.NewGuid()}"),
                Ev("TicketImportCree", "numeroBl=BL123"),
            }));

            Assert.Contains("Document déposé", cut.Markup);
            Assert.Contains("permis.pdf", cut.Markup);
            Assert.Contains("Ticket pris en charge", cut.Markup);
            Assert.DoesNotContain("operateur=", cut.Markup);
            Assert.Contains("N° BL : BL123", cut.Markup);
        }
        finally
        {
            CultureInfo.CurrentUICulture = precedente;
        }
    }
}
