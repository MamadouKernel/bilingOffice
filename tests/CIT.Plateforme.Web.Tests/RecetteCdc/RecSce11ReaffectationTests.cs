using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.RecetteCdc;

/// <summary>
/// [TEST-CDC] REC-SCE-11 : « Réaffectation entre opérateurs (A → B) selon habilitations, tracée
/// dans l'historique. » CDC couverts : TRF-04, WKF-03.
/// </summary>
public sealed class RecSce11ReaffectationTests(PlateformeWebApplicationFactory factory) : IClassFixture<PlateformeWebApplicationFactory>
{
    [Fact]
    public async Task Reaffectation_A_vers_B_est_appliquee_et_tracee()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<PlateformeDbContext>();
        var creerTicket = services.GetRequiredService<CreerTicketCommand>();
        var prendreEnCharge = services.GetRequiredService<PrendreEnChargeCommand>();
        var reaffecter = services.GetRequiredService<ReaffecterTicketCommand>();

        var clientId = Guid.NewGuid();
        var operateurA = Guid.NewGuid();
        var operateurB = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, operateurA), Permission.TicketPrendreEnCharge, fileId));
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, operateurA), Permission.TicketReaffecter, fileId));
        await dbContext.SaveChangesAsync();

        var ticket = await creerTicket.ExecuterAsync(new CreerTicketRequete(clientId, "BillingOffice", "Import", fileId, null));
        await prendreEnCharge.ExecuterAsync(operateurA, ticket.Id);

        await reaffecter.ExecuterAsync(operateurA, ticket.Id, operateurB);

        var ticketReaffecte = await dbContext.Tickets.FindAsync(ticket.Id);
        Assert.Equal(operateurB, ticketReaffecte!.OperateurAssigneId);

        var historique = dbContext.HistoriqueTickets.Where(h => h.TicketId == ticket.Id).ToList();
        Assert.Contains(historique, h => h.Evenement == "TicketReaffecte" && h.Detail!.Contains(operateurB.ToString()));

        var audit = dbContext.JournalAudit.Where(a => a.EntiteId == ticket.Id && a.Action == "TicketReaffecte").ToList();
        Assert.Single(audit);
        Assert.Equal(operateurA, audit[0].AuteurId);
    }

    [Fact]
    public async Task Reaffectation_sans_habilitation_est_refusee()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<PlateformeDbContext>();
        var creerTicket = services.GetRequiredService<CreerTicketCommand>();
        var prendreEnCharge = services.GetRequiredService<PrendreEnChargeCommand>();
        var reaffecter = services.GetRequiredService<ReaffecterTicketCommand>();

        var clientId = Guid.NewGuid();
        var operateurA = Guid.NewGuid();
        var operateurSansDroit = Guid.NewGuid();
        var operateurB = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, operateurA), Permission.TicketPrendreEnCharge, fileId));
        await dbContext.SaveChangesAsync();

        var ticket = await creerTicket.ExecuterAsync(new CreerTicketRequete(clientId, "BillingOffice", "Import", fileId, null));
        await prendreEnCharge.ExecuterAsync(operateurA, ticket.Id);

        await Assert.ThrowsAsync<CIT.Plateforme.Application.Securite.AccesRefuseException>(() =>
            reaffecter.ExecuterAsync(operateurSansDroit, ticket.Id, operateurB));
    }
}
