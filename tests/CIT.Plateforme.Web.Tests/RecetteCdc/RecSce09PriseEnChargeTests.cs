using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.RecetteCdc;

/// <summary>
/// [TEST-CDC] REC-SCE-09 (`docs/04-matrice-recette.md`) : « Prise en charge : un ticket arrive en
/// file en « attente de prise en charge », un opérateur l'accepte ; les horodatages (arrivée,
/// prise en charge, dernière activité) sont enregistrés et exploités dans les indicateurs. »
/// CDC couverts : SOC-12, PEC-01, PEC-02, PEC-03, PEC-04, TPS-01.
/// <para>
/// Exécuté contre l'application réellement composée (DI, Identity, EF Core — <see cref="PlateformeWebApplicationFactory"/>),
/// ce qui prouve que l'assemblage complet démarre et fonctionne, pas seulement les cas d'usage
/// isolés (déjà couverts unitairement dans CIT.Plateforme.Application.Tests).
/// </para>
/// </summary>
public sealed class RecSce09PriseEnChargeTests : IClassFixture<PlateformeWebApplicationFactory>
{
    private readonly PlateformeWebApplicationFactory factory;

    public RecSce09PriseEnChargeTests(PlateformeWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task Un_ticket_arrive_en_attente_de_pec_puis_est_accepte_avec_horodatages_exploitables()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        var dbContext = services.GetRequiredService<PlateformeDbContext>();
        var creerTicket = services.GetRequiredService<CreerTicketCommand>();
        var prendreEnCharge = services.GetRequiredService<PrendreEnChargeCommand>();

        var clientId = Guid.NewGuid();
        var operateurId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        // Arrivée en file : SOC-12/PEC-01.
        var ticket = await creerTicket.ExecuterAsync(new CreerTicketRequete(clientId, "BillingOffice", "Import", fileId, null));
        Assert.Equal(StatutTicket.EnAttenteDePriseEnCharge, ticket.Statut);
        Assert.Null(ticket.DatePriseEnChargeUtc);

        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(operateurId, Permission.TicketPrendreEnCharge, fileId));
        await dbContext.SaveChangesAsync();

        // Acceptation par un opérateur : PEC-02.
        await prendreEnCharge.ExecuterAsync(operateurId, ticket.Id);

        var ticketMisAJour = await dbContext.Tickets.FindAsync(ticket.Id);
        Assert.NotNull(ticketMisAJour);
        Assert.Equal(StatutTicket.EnCours, ticketMisAJour!.Statut);
        Assert.Equal(operateurId, ticketMisAJour.OperateurAssigneId);

        // Horodatages enregistrés et exploitables (PEC-03/PEC-04, TPS-01) : arrivée <= PEC <= dernière activité.
        Assert.NotNull(ticketMisAJour.DatePriseEnChargeUtc);
        Assert.True(ticketMisAJour.DateArriveeEnFileUtc <= ticketMisAJour.DatePriseEnChargeUtc);
        Assert.True(ticketMisAJour.DatePriseEnChargeUtc <= ticketMisAJour.DateDerniereActiviteUtc);
        var delaiDePriseEnCharge = ticketMisAJour.DatePriseEnChargeUtc!.Value - ticketMisAJour.DateArriveeEnFileUtc; // PEC-04
        Assert.True(delaiDePriseEnCharge >= TimeSpan.Zero);

        // Historique + audit consignés dans la même opération (SOC-13).
        var historique = dbContext.HistoriqueTickets.Where(h => h.TicketId == ticket.Id).ToList();
        Assert.Contains(historique, h => h.Evenement == "TicketCree");
        Assert.Contains(historique, h => h.Evenement == "TicketPrisEnCharge");
    }
}
