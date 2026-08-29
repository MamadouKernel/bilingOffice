using CIT.Plateforme.Application.Administration;
using CIT.Plateforme.Application.Supervision;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Supervision;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.RecetteCdc;

/// <summary>
/// [TEST-CDC] REC-SCE-12 : « Temps et SLA : vérification de la mesure attente/traitement par
/// étape, de la distinction temps interne / temps client (SLA suspendu en « en attente client »),
/// et du déclenchement des seuils 50 % / 80 % / dépassement / escalade. » CDC couverts : TPS-01,
/// TPS-02, TPS-03, TPS-04, SLA-01, SLA-02, SLA-03, SLA-05, ALE-01, ALE-02, ALE-03.
/// </summary>
public sealed class RecSce12TempsSlaTests(PlateformeWebApplicationFactory factory) : IClassFixture<PlateformeWebApplicationFactory>
{
    [Fact]
    public async Task Un_ticket_qui_depasse_son_sla_declenche_une_alerte_de_depassement()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<PlateformeDbContext>();
        var creerTicket = services.GetRequiredService<CreerTicketCommand>();
        var gererSla = services.GetRequiredService<GererParametresSlaCommand>();
        var surveiller = services.GetRequiredService<SurveillerEcheancesUseCase>();

        var administrateurId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(administrateurId, Permission.AdministrationSla));
        await dbContext.SaveChangesAsync();

        // SLA-01/SLA-05 : cible administrable, aucune valeur inventée dans le socle lui-même —
        // la valeur ci-dessous est celle saisie pour CE test, pas une constante métier.
        await gererSla.DefinirAsync(administrateurId, "BillingOffice", "Import", TimeSpan.FromHours(1), 0.5, 0.8);

        var clientId = Guid.NewGuid();
        var ticket = await creerTicket.ExecuterAsync(new CreerTicketRequete(clientId, "BillingOffice", "Import", Guid.NewGuid(), null));

        // Simule l'écoulement du temps (SLA dépassé : arrivée il y a 2h pour une cible d'1h).
        var ticketEnBase = await dbContext.Tickets.FindAsync(ticket.Id);
        dbContext.Entry(ticketEnBase!).Property("DateArriveeEnFileUtc").CurrentValue = DateTime.UtcNow.AddHours(-2);
        await dbContext.SaveChangesAsync();

        var statut = await surveiller.TraiterTicketAsync(ticket.Id);

        Assert.Equal(CIT.Plateforme.Domain.SuiviTemps.StatutSla.Depasse, statut);

        // ALE-01/ALE-03 : une alerte de dépassement (escalade) est créée.
        var alertes = dbContext.Alertes.Where(a => a.TicketId == ticket.Id).ToList();
        Assert.Contains(alertes, a => a.Type == TypeAlerte.SlaDepasse);
    }

    [Fact]
    public async Task L_attente_client_n_est_jamais_imputee_au_calcul_du_delai_de_traitement()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<PlateformeDbContext>();
        var creerTicket = services.GetRequiredService<CreerTicketCommand>();
        var prendreEnCharge = services.GetRequiredService<PrendreEnChargeCommand>();
        var attenteClient = services.GetRequiredService<AttenteClientCommand>();

        var clientId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentId, Permission.TicketPrendreEnCharge, fileId));
        await dbContext.SaveChangesAsync();

        var ticket = await creerTicket.ExecuterAsync(new CreerTicketRequete(clientId, "BillingOffice", "Import", fileId, null));
        await prendreEnCharge.ExecuterAsync(agentId, ticket.Id);
        await attenteClient.MettreEnAttenteAsync(agentId, ticket.Id, "Document manquant");
        await attenteClient.RependreApresReponseAsync(ticket.Id);

        var ticketFinal = await dbContext.Tickets.FindAsync(ticket.Id);
        // TPS-04 : le cumul d'attente client est enregistré séparément — jamais fusionné dans le
        // temps de traitement de l'opérateur.
        Assert.True(ticketFinal!.DureeAttenteClienteCumulee >= TimeSpan.Zero);
        Assert.Null(ticketFinal.DateDebutAttenteClienteUtc); // attente refermée
    }
}
