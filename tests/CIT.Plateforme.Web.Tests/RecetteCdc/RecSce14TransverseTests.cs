using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.RecetteCdc;

/// <summary>
/// [TEST-CDC] REC-SCE-14 (portion socle uniquement — notifications/continuité/cloisonnement
/// client relèvent d'autres features, hors périmètre F001, voir recipe-scope-audit.md) :
/// regroupement en dossier (T065, SOC-11) et journal d'audit inviolable (T095, SOC-13) pour les
/// actions sensibles.
/// </summary>
public sealed class RecSce14TransverseTests(PlateformeWebApplicationFactory factory) : IClassFixture<PlateformeWebApplicationFactory>
{
    [Fact]
    public async Task Plusieurs_tickets_du_meme_client_sont_regroupes_dans_un_dossier_avec_suivi_global()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var creerTicket = services.GetRequiredService<CreerTicketCommand>();
        var consulterDossier = services.GetRequiredService<ConsulterDossierQuery>();

        // REMÉDIATION P0 : la consultation d'un dossier exige désormais l'identité de l'appelant.
        // Ce test porte sur le REGROUPEMENT (SOC-11), pas sur l'autorisation ; le propriétaire du
        // dossier est donc un compte réel, et c'est lui qui consulte.
        var dbContext = services.GetRequiredService<PlateformeDbContext>();
        var proprietaire = new CIT.Plateforme.Infrastructure.Identity.ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"soc11-{Guid.NewGuid():N}@test.ci",
            Email = $"soc11-{Guid.NewGuid():N}@test.ci",
            SecurityStamp = Guid.NewGuid().ToString(),
            NomAffiche = "Propriétaire dossier",
            Nature = CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client,
            EstActif = true,
        };
        dbContext.Users.Add(proprietaire);
        await dbContext.SaveChangesAsync();

        var clientId = proprietaire.Id;
        var fileId = Guid.NewGuid();

        var ticket1 = await creerTicket.ExecuterAsync(new CreerTicketRequete(clientId, "BillingOffice", "Import", fileId, null));
        var ticket2 = await creerTicket.ExecuterAsync(new CreerTicketRequete(clientId, "BillingOffice", "Import", fileId, ticket1.DossierId));

        var vue = await consulterDossier.ExecuterAsync(clientId, ticket1.DossierId);

        Assert.Equal(2, vue.Tickets.Count);
        Assert.Contains(vue.Tickets, t => t.Id == ticket1.Id);
        Assert.Contains(vue.Tickets, t => t.Id == ticket2.Id);
    }

    [Fact]
    public async Task Les_actions_sensibles_du_socle_produisent_une_entree_d_audit_verifiable()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<PlateformeDbContext>();
        var creerTicket = services.GetRequiredService<CreerTicketCommand>();
        var prendreEnCharge = services.GetRequiredService<PrendreEnChargeCommand>();
        var consulterAudit = services.GetRequiredService<ConsulterJournalAuditQuery>();

        var administrateurTechniqueId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, administrateurTechniqueId), Permission.AuditConsulter));
        var agentId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentId), Permission.TicketPrendreEnCharge, fileId));
        await dbContext.SaveChangesAsync();

        var ticket = await creerTicket.ExecuterAsync(new CreerTicketRequete(Guid.NewGuid(), "BillingOffice", "Import", fileId, null));
        await prendreEnCharge.ExecuterAsync(agentId, ticket.Id);

        // SOC-13 : auteur, action, date/heure, détail — consultable par l'administrateur technique.
        var entreesDuTicket = await consulterAudit.ExecuterAsync(administrateurTechniqueId, ticket.Id);

        Assert.Contains(entreesDuTicket, e => e.Action == "TicketCree");
        Assert.Contains(entreesDuTicket, e => e.Action == "TicketPrisEnCharge" && e.AuteurId == agentId);
        Assert.All(entreesDuTicket, e => Assert.True(e.HorodatageUtc > DateTime.MinValue));
    }
}
