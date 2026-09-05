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
/// [TEST-SPEC] Exigence CDC: SOC-02, SOC-03, SOC-06, SOC-08, WKF-01 → US1 → critères
/// d'acceptation 3, 5, 6 de US1 (FR-002, FR-003, FR-006, FR-008, FR-027) → Test.
/// Aucun scénario CDC direct dans ce périmètre (recipe-scope-audit.md).
/// </summary>
public sealed class TicketSocleTests
{
    private static (PlateformeDbContext DbContext, CreerTicketCommand Creer, PrendreEnChargeCommand Pec,
        ActionsWorkflowStandardCommand Actions, ConsulterStatutQuery Statut, HorlogeDeTest Horloge)
        ConstruireContexte()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var numerotation = new NumerotationEnMemoirePourTests();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);

        var creer = new CreerTicketCommand(
            new TicketRepository(dbContext), new DossierRepository(dbContext), numerotation, journalisation, horloge, uniteDeTravail);
        var pec = new PrendreEnChargeCommand(new TicketRepository(dbContext), autorisation, journalisation, horloge, uniteDeTravail);
        var actions = new ActionsWorkflowStandardCommand(new TicketRepository(dbContext), autorisation, journalisation, uniteDeTravail);
        var statutQuery = new ConsulterStatutQuery(
            new TicketRepository(dbContext),
            new CloisonnementService(new TicketRepository(dbContext), CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.ControleAcces(dbContext)));

        return (dbContext, creer, pec, actions, statutQuery, horloge);
    }

    [Fact]
    public async Task SOC_02_le_ticket_centralise_module_type_et_reste_consultable()
    {
        var (_, creer, _, _, _, _) = ConstruireContexte();
        var clientId = Guid.NewGuid();

        var ticket = await creer.ExecuterAsync(new CreerTicketRequete(clientId, "BillingOffice", "Import", Guid.NewGuid(), DossierExistantId: null));

        Assert.Equal("BillingOffice", ticket.Module);
        Assert.Equal("Import", ticket.TypeDemande);
        Assert.NotEqual(Guid.Empty, ticket.DossierId);
    }

    [Fact]
    public async Task WKF_01_le_workflow_est_determine_par_module_et_type_fournis_a_la_creation()
    {
        var (_, creer, _, _, _, _) = ConstruireContexte();
        var clientId = Guid.NewGuid();

        // SOC-03/WKF-01 : le socle applique le module+type transmis — c'est cette donnée qui,
        // en amont (module métier, hors périmètre), détermine le workflow applicable.
        var ticketImport = await creer.ExecuterAsync(new CreerTicketRequete(clientId, "BillingOffice", "Import", Guid.NewGuid(), null));
        var ticketReclamation = await creer.ExecuterAsync(new CreerTicketRequete(clientId, "CustomerComplaint", "Reclamation", Guid.NewGuid(), null));

        Assert.NotEqual(ticketImport.Module, ticketReclamation.Module);
    }

    [Fact]
    public async Task SOC_06_valider_rejeter_et_demander_complement_sont_journalises()
    {
        var (dbContext, creer, pec, actions, _, _) = ConstruireContexte();
        var clientId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var ticket = await creer.ExecuterAsync(new CreerTicketRequete(clientId, "BillingOffice", "Import", fileId, null));
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentId), Permission.TicketPrendreEnCharge, fileId));
        await dbContext.SaveChangesAsync();

        await pec.ExecuterAsync(agentId, ticket.Id);
        await actions.ValiderAsync(agentId, ticket.Id);

        var actionRejet = () => actions.RejeterAsync(agentId, ticket.Id, motif: "");
        await Assert.ThrowsAsync<ArgumentException>(actionRejet);

        await actions.RejeterAsync(agentId, ticket.Id, motif: "Document illisible");
        await actions.DemanderComplementAsync(agentId, ticket.Id, motif: "Facture manquante");

        var historique = dbContext.HistoriqueTickets.Where(h => h.TicketId == ticket.Id).ToList();
        Assert.Contains(historique, h => h.Evenement == "TicketValide");
        Assert.Contains(historique, h => h.Evenement == "TicketRejete" && h.Detail == "Document illisible");
        Assert.Contains(historique, h => h.Evenement == "ComplementDemande" && h.Detail == "Facture manquante");
    }

    [Fact]
    public async Task SOC_08_le_client_consulte_le_statut_de_son_ticket_en_temps_reel()
    {
        var (dbContext, creer, pec, _, statutQuery, _) = ConstruireContexte();
        // Le client consulte son propre ticket : il lui faut un compte réel et actif, la
        // consultation passant désormais par la garde centrale (consolidation sécurité).
        var clientId = CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(
            dbContext, Guid.NewGuid(), CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client);
        var agentId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var ticket = await creer.ExecuterAsync(new CreerTicketRequete(clientId, "BillingOffice", "Import", fileId, null));

        var vueAvant = await statutQuery.ExecuterAsync(clientId, ticket.Id);
        Assert.Equal(StatutTicket.EnAttenteDePriseEnCharge, vueAvant.Statut);

        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentId), Permission.TicketPrendreEnCharge, fileId));
        await dbContext.SaveChangesAsync();
        await pec.ExecuterAsync(agentId, ticket.Id);

        var vueApres = await statutQuery.ExecuterAsync(clientId, ticket.Id);
        Assert.Equal(StatutTicket.EnCours, vueApres.Statut);
    }
}
