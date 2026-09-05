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
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentId), Permission.TicketPrendreEnCharge, fileId));

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

    /// <summary>
    /// SOC-10 depuis une fiche : l'agent désigne l'autre ticket par son NUMÉRO. C'est la seule
    /// référence qu'il ait sous les yeux ; personne ne retient un GUID. La résolution vit dans la
    /// commande, pour ne pas exposer une recherche de ticket non cloisonnée à la couche Web.
    /// </summary>
    [Fact]
    public async Task Un_ticket_peut_etre_lie_par_son_numero()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var ticketRepo = new TicketRepository(dbContext);

        var agentId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentId), Permission.TicketPrendreEnCharge, fileId));

        var dossier = Dossier.Creer("DOS-LIA-000010", Guid.NewGuid(), horloge.MaintenantUtc);
        var origine = Ticket.Creer("TCK-LIA-000010", "BillingOffice", "Import", fileId, dossier.Id, horloge.MaintenantUtc);
        var cible = Ticket.Creer("TCK-LIA-000011", "CustomerComplaint", "Reclamation", fileId, dossier.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.AddRange(origine, cible);
        await dbContext.SaveChangesAsync();

        var commande = new LierTicketsCommand(new LiaisonTicketRepository(dbContext), ticketRepo, autorisation, journalisation, horloge, uniteDeTravail);
        await commande.ExecuterParNumeroAsync(agentId, origine.Id, "  TCK-LIA-000011  ");

        var liaisons = await new LiaisonTicketRepository(dbContext).ListerParTicketAsync(origine.Id);
        Assert.Single(liaisons);
        Assert.Equal(cible.Id, liaisons[0].TicketLieId);
    }

    [Fact]
    public async Task Un_numero_de_ticket_inconnu_est_refuse_explicitement()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var ticketRepo = new TicketRepository(dbContext);

        var agentId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentId), Permission.TicketPrendreEnCharge, fileId));

        var dossier = Dossier.Creer("DOS-LIA-000020", Guid.NewGuid(), horloge.MaintenantUtc);
        var origine = Ticket.Creer("TCK-LIA-000020", "BillingOffice", "Import", fileId, dossier.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(origine);
        await dbContext.SaveChangesAsync();

        var commande = new LierTicketsCommand(new LiaisonTicketRepository(dbContext), ticketRepo, autorisation, journalisation, horloge, uniteDeTravail);

        var erreur = await Assert.ThrowsAsync<InvalidOperationException>(
            () => commande.ExecuterParNumeroAsync(agentId, origine.Id, "TCK-INEXISTANT"));

        // Le message nomme le numéro saisi : sans cela, l'agent ne saurait pas s'il s'est trompé
        // de numéro ou si la liaison a échoué pour une autre raison.
        Assert.Contains("TCK-INEXISTANT", erreur.Message, StringComparison.Ordinal);
    }
}
