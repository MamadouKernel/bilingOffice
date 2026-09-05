using CIT.Plateforme.Application.Messagerie;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Messagerie;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Messagerie;

/// <summary>[TEST-SPEC] Exigence CDC: SOC-05, MSG-01 → US5 → scénario d'acceptation 1 de US5 →
/// Test. Aucun scénario CDC direct dans ce périmètre.</summary>
public sealed class MessagesTests
{
    private static (PlateformeDbContext DbContext, PublierMessageCommand Commande, Ticket Ticket, Guid ClientId, Guid AgentId)
        ConstruireContexte()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var ticketRepo = new TicketRepository(dbContext);
        var cloisonnement = new CloisonnementService(ticketRepo, CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.ControleAcces(dbContext));

        var clientId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentId), Permission.TicketPrendreEnCharge, fileId));

        var dossier = Dossier.Creer("DOS-MSG-000001", CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, clientId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-MSG-000001", "BillingOffice", "Import", fileId, dossier.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);
        dbContext.SaveChanges();

        var commande = new PublierMessageCommand(
            new MessageRepository(dbContext), ticketRepo, autorisation, cloisonnement, journalisation, horloge, uniteDeTravail);
        return (dbContext, commande, ticket, clientId, agentId);
    }

    [Fact]
    public async Task Message_client_est_horodate_et_visible_par_le_client_et_les_agents()
    {
        var (dbContext, commande, ticket, clientId, agentId) = ConstruireContexte();

        await commande.PublierMessageClientAsync(clientId, ticket.Id, "Bonjour, où en est mon dossier ?");
        await commande.PublierMessageAgentAsync(agentId, ticket.Id, "Bonjour, en cours de traitement.");

        var fil = dbContext.Messages.Where(m => m.TicketId == ticket.Id).OrderBy(m => m.HorodatageUtc).ToList();
        Assert.Equal(2, fil.Count);
        Assert.All(fil, m => Assert.Equal(TypeMessage.Externe, m.Type));

        var visiblesClient = await new MessageRepository(dbContext).ListerVisiblesParClientAsync(ticket.Id);
        Assert.Equal(2, visiblesClient.Count);
    }

    [Fact]
    public async Task Un_client_ne_peut_pas_publier_sur_un_ticket_qui_n_est_pas_le_sien()
    {
        var (_, commande, ticket, _, _) = ConstruireContexte();
        var autreClient = Guid.NewGuid();

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            commande.PublierMessageClientAsync(autreClient, ticket.Id, "Intrusion"));
    }
}
