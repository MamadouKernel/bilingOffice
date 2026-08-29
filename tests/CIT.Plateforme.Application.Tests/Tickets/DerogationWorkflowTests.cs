using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Tickets;

/// <summary>Complète la couverture directe de WKF-02 (le circuit n'est jamais rigide : un
/// utilisateur habilité peut déroger au circuit nominal) et WKF-03 (la dérogation est tracée).</summary>
public sealed class DerogationWorkflowTests
{
    [Fact]
    public async Task Un_utilisateur_habilite_a_deroger_peut_transferer_hors_matrice_et_c_est_trace()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var ticketRepo = new TicketRepository(dbContext);

        var initiateurId = Guid.NewGuid();
        var fileSource = Guid.NewGuid();
        var fileCibleHorsMatrice = Guid.NewGuid();
        // Habilité à DÉROGER — permission distincte de TicketTransferer (aucune entrée dans
        // TransitionsAutorisees pour fileSource -> fileCibleHorsMatrice).
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(initiateurId, DerogationWorkflowService.PermissionDeroger, fileSource));

        var dossier = Dossier.Creer("DOS-WKF-000001", Guid.NewGuid(), horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-WKF-000001", "BillingOffice", "Import", fileSource, dossier.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();

        var derogation = new DerogationWorkflowService(ticketRepo, autorisation, journalisation, horloge, uniteDeTravail);
        await derogation.DerogerTransfertAsync(initiateurId, ticket.Id, fileCibleHorsMatrice, "Situation opérationnelle exceptionnelle");

        var ticketApres = await dbContext.Tickets.FindAsync(ticket.Id);
        Assert.Equal(fileCibleHorsMatrice, ticketApres!.FileCouranteId);

        // WKF-03 : la dérogation est tracée explicitement, distinctement d'un transfert normal.
        var historique = dbContext.HistoriqueTickets.Where(h => h.TicketId == ticket.Id).ToList();
        Assert.Contains(historique, h => h.Evenement == "DerogationCircuitNominal");
        var audit = dbContext.JournalAudit.Where(a => a.EntiteId == ticket.Id).ToList();
        Assert.Contains(audit, a => a.Action == "DerogationCircuitNominal");
    }

    [Fact]
    public async Task Un_utilisateur_non_habilite_a_deroger_ne_peut_pas_contourner_la_matrice()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var ticketRepo = new TicketRepository(dbContext);

        var initiateurSansDroit = Guid.NewGuid();
        var dossier = Dossier.Creer("DOS-WKF-000002", Guid.NewGuid(), horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-WKF-000002", "BillingOffice", "Import", Guid.NewGuid(), dossier.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();

        var derogation = new DerogationWorkflowService(ticketRepo, autorisation, journalisation, horloge, uniteDeTravail);

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            derogation.DerogerTransfertAsync(initiateurSansDroit, ticket.Id, Guid.NewGuid(), "Tentative non habilitée"));
    }
}
