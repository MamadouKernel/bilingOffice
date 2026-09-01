using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Tickets;

/// <summary>
/// [TEST-SPEC] Exigence CDC: PRI-02 → US1 (F012).
/// PRI-02 : « La priorité peut influencer l'ordre du ticket dans la file, le SLA applicable, les alertes et les règles d'escalade. »
/// Démontre la déduction/assignation par défaut de la priorité à la création et l'ordonnancement préférentiel des tickets en file.
/// </summary>
public sealed class PriCert01PrioriteTests
{
    [Fact]
    public async Task PRI_CERT_01_A_ticket_cree_recoit_automatiquement_priorite_standard_par_defaut()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var ticketRepo = new TicketRepository(dbContext);
        var dossierRepo = new DossierRepository(dbContext);
        var prioriteRepo = new PrioriteRepository(dbContext);
        var numerotation = new NumerotationEnMemoirePourTests();

        var niveauStandard = NiveauPriorite.Definir("Normale", 1);
        var niveauUrgent = NiveauPriorite.Definir("Urgente", 2);
        dbContext.NiveauxPriorite.AddRange(niveauStandard, niveauUrgent);
        await dbContext.SaveChangesAsync();

        var clientId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var commande = new CreerTicketCommand(
            ticketRepo, dossierRepo, numerotation, journalisation, horloge, uniteDeTravail, prioriteRepo);

        var ticket = await commande.ExecuterAsync(new CreerTicketRequete(
            clientId, "BillingOffice", "Import", fileId, null));

        Assert.NotNull(ticket.PrioriteId);
        Assert.Equal(niveauStandard.Id, ticket.PrioriteId);
    }

    [Fact]
    public async Task PRI_CERT_01_B_file_ordonne_tickets_par_priorite_decroissante_puis_anciennete()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var autorisation = new AutorisationService(dbContext);
        var ticketRepo = new TicketRepository(dbContext);
        var prioriteRepo = new PrioriteRepository(dbContext);

        var agentId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentId, Permission.TicketPrendreEnCharge, fileId));

        var niveauNormale = NiveauPriorite.Definir("Normale", 1);
        var niveauUrgente = NiveauPriorite.Definir("Urgente", 2);
        var niveauCritique = NiveauPriorite.Definir("Critique", 3);
        dbContext.NiveauxPriorite.AddRange(niveauNormale, niveauUrgente, niveauCritique);

        var dossier = Dossier.Creer("DOS-PRI-001", Guid.NewGuid(), horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);

        // Ticket 1 : créé à T0, priorité Normale (rang 1)
        var ticket1 = Ticket.Creer("TCK-001", "BillingOffice", "Import", fileId, dossier.Id, horloge.MaintenantUtc);
        ticket1.DefinirPriorite(niveauNormale.Id);

        // Ticket 2 : créé à T0 + 10s, priorité Critique (rang 3)
        var ticket2 = Ticket.Creer("TCK-002", "BillingOffice", "Import", fileId, dossier.Id, horloge.MaintenantUtc.AddSeconds(10));
        ticket2.DefinirPriorite(niveauCritique.Id);

        // Ticket 3 : créé à T0 + 5s, priorité Urgente (rang 2)
        var ticket3 = Ticket.Creer("TCK-003", "BillingOffice", "Import", fileId, dossier.Id, horloge.MaintenantUtc.AddSeconds(5));
        ticket3.DefinirPriorite(niveauUrgente.Id);

        // Ticket 4 : créé à T0 + 20s, priorité Normale (rang 1)
        var ticket4 = Ticket.Creer("TCK-004", "BillingOffice", "Import", fileId, dossier.Id, horloge.MaintenantUtc.AddSeconds(20));
        ticket4.DefinirPriorite(niveauNormale.Id);

        dbContext.Tickets.AddRange(ticket1, ticket2, ticket3, ticket4);
        await dbContext.SaveChangesAsync();

        var query = new ConsulterFileQuery(ticketRepo, autorisation, prioriteRepo);
        var ticketsOrdonnes = await query.ExecuterAsync(agentId, fileId);

        Assert.Equal(4, ticketsOrdonnes.Count);
        // Ordre attendu : Critique (ticket2), Urgente (ticket3), Normale T0 (ticket1), Normale T0+20s (ticket4)
        Assert.Equal(ticket2.Id, ticketsOrdonnes[0].Id);
        Assert.Equal(ticket3.Id, ticketsOrdonnes[1].Id);
        Assert.Equal(ticket1.Id, ticketsOrdonnes[2].Id);
        Assert.Equal(ticket4.Id, ticketsOrdonnes[3].Id);
    }
}
