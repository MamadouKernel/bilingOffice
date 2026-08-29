using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Tickets;

/// <summary>[TEST-TECH] Decision: SOC-10 (couverture Partielle via REC-SCE-14,
/// recipe-scope-audit.md).</summary>
public sealed class LiaisonInterTicketsTests
{
    [Fact]
    public async Task Deux_tickets_de_modules_differents_peuvent_etre_lies_par_un_agent_habilite()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var ticketRepo = new TicketRepository(dbContext);

        var agentId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentId, Permission.TicketPrendreEnCharge, fileId));

        var dossier1 = Dossier.Creer("DOS-LIA-000001", Guid.NewGuid(), horloge.MaintenantUtc);
        var dossier2 = Dossier.Creer("DOS-LIA-000002", Guid.NewGuid(), horloge.MaintenantUtc);
        var ticketBilling = Ticket.Creer("TCK-LIA-000001", "BillingOffice", "Import", fileId, dossier1.Id, horloge.MaintenantUtc);
        var ticketReclamation = Ticket.Creer("TCK-LIA-000002", "CustomerComplaint", "Reclamation", fileId, dossier2.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.AddRange(dossier1, dossier2);
        dbContext.Tickets.AddRange(ticketBilling, ticketReclamation);
        await dbContext.SaveChangesAsync();

        var commande = new LierTicketsCommand(new LiaisonTicketRepository(dbContext), ticketRepo, autorisation, journalisation, horloge, uniteDeTravail);
        await commande.ExecuterAsync(agentId, ticketBilling.Id, ticketReclamation.Id);

        var liaisonRepo = new LiaisonTicketRepository(dbContext);
        var liaisonsDepuisBilling = await liaisonRepo.ListerParTicketAsync(ticketBilling.Id);
        var liaisonsDepuisReclamation = await liaisonRepo.ListerParTicketAsync(ticketReclamation.Id);

        Assert.Single(liaisonsDepuisBilling);
        Assert.Single(liaisonsDepuisReclamation); // visible depuis les deux tickets liés
    }
}
