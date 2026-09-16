using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Tickets;

/// <summary>[TEST-SPEC] Exigence CDC: CYC-06, CYC-07 → US4 → scénario d'acceptation 4 de US4
/// (CLAR-02) → Test.</summary>
public sealed class ReouvertureTests
{
    private static (PlateformeDbContext DbContext, ReouvrirTicketCommand Commande, Ticket Ticket, Guid ClientId, Guid AgentId)
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
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentId), Permission.TicketRouvrir, fileId));

        var dossier = Dossier.Creer("DOS-ROU-000001", CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, clientId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-ROU-000001", "BillingOffice", "Import", fileId, dossier.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);
        dbContext.SaveChanges();

        var commande = new ReouvrirTicketCommand(ticketRepo, autorisation, journalisation, cloisonnement, horloge, uniteDeTravail);
        return (dbContext, commande, ticket, clientId, agentId);
    }

    [Fact]
    public async Task CLAR_02_reouverture_refusee_pour_un_ticket_annule()
    {
        var (dbContext, commande, ticket, _, agentId) = ConstruireContexte();
        ticket.Annuler(DateTime.UtcNow);
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => commande.RouvrirAsync(agentId, ticket.Id, "Motif"));
    }

    [Fact]
    public async Task CLAR_02_reouverture_acceptee_pour_un_ticket_cloture()
    {
        var (dbContext, commande, ticket, _, agentId) = ConstruireContexte();
        ticket.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);
        ticket.Cloturer(MotifClotureType.Normale, DateTime.UtcNow);
        await dbContext.SaveChangesAsync();

        await commande.RouvrirAsync(agentId, ticket.Id, "Nouvelle information reçue");

        var apres = await dbContext.Tickets.FindAsync(ticket.Id);
        Assert.Equal(StatutTicket.EnAttenteDePriseEnCharge, apres!.Statut);
    }

    [Fact]
    public async Task CYC_06_un_client_ne_peut_pas_rouvrir_directement_meme_avec_le_bon_ticket()
    {
        // Écart de re-vérification d'audit : le chemin agent était testé positivement
        // (CLAR_02_reouverture_acceptee_pour_un_ticket_cloture) mais aucun test ne prouvait
        // qu'un CLIENT appelant directement RouvrirAsync (plutôt que DemanderReouvertureAsync)
        // se voit refuser l'accès — seule l'absence d'un tel bouton côté UI le suggérait.
        var (dbContext, commande, ticket, clientId, _) = ConstruireContexte();
        ticket.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);
        ticket.Cloturer(MotifClotureType.Normale, DateTime.UtcNow);
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<AccesRefuseException>(() => commande.RouvrirAsync(clientId, ticket.Id, "Je rouvre moi-même"));

        var apres = await dbContext.Tickets.FindAsync(ticket.Id);
        Assert.Equal(StatutTicket.Cloture, apres!.Statut);
    }

    [Fact]
    public async Task CYC_07_le_client_ne_rouvre_jamais_directement_il_demande_seulement()
    {
        var (dbContext, commande, ticket, clientId, _) = ConstruireContexte();
        ticket.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);
        ticket.Cloturer(MotifClotureType.Normale, DateTime.UtcNow);
        await dbContext.SaveChangesAsync();

        await commande.DemanderReouvertureAsync(clientId, ticket.Id, "Merci de rouvrir mon dossier");

        // La demande est tracée mais ne change jamais le statut elle-même (CYC-07).
        var apres = await dbContext.Tickets.FindAsync(ticket.Id);
        Assert.Equal(StatutTicket.Cloture, apres!.Statut);
    }
}
