using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Application.Notifications.Resolveurs;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Tickets;

/// <summary>
/// « Qui a pris en charge, traite » (05/10/2026) : un ticket affecté n'est traité que par son agent ou
/// par un superviseur ; les autres agents le voient en lecture seule et demandent sa réaffectation.
/// </summary>
public sealed class QuiPrendEnChargeTraiteTests
{
    private sealed record Contexte(PlateformeDbContext Db, ValiderTicketImportCommand Valider, DemanderReaffectationCommand Demander,
        ResolveurSuperviseursDeLaFile Resolveur, Guid AgentA, Guid AgentB, Guid Superviseur, Guid TicketId);

    private static async Task<Contexte> ConstruireAsync(bool affecterAgentA = true)
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(db, horloge);
        var unite = new UniteDeTravail(db);
        var tickets = new TicketRepository(db);
        var dossiers = new DossierRepository(db);
        var files = new FileDeTraitementRepository(db);
        var autorisation = new AutorisationService(db);
        var habilitations = new HabilitationRepository(db);

        var manifeste = FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, SoumettreDemandeImportCommand.FilePiloteManifeste);
        db.Files.AddRange(manifeste,
            FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, NomsFiles.Facturation),
            FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, NomsFiles.FileIes));
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        var auteurId = Guid.NewGuid();
        var (agentA, agentB, superviseur) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        db.Societes.Add(societe);
        db.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, auteurId, NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        foreach (var agent in new[] { agentA, agentB, superviseur })
        {
            db.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, agent), Permission.BillingImportManifesteInstruire));
        }
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(superviseur, Permission.TicketReaffecter));
        await db.SaveChangesAsync();

        var soumission = await new SoumettreDemandeImportCommand(tickets, dossiers, new SocieteClienteRepository(db), new UtilisateurSocieteRepository(db),
            files, new NumerotationEnMemoirePourTests(), journalisation, horloge, unite)
            .ExecuterAsync(new SoumettreDemandeImportRequete(societe.Id, auteurId, ["BL-AFF-" + Guid.NewGuid().ToString("N")[..6]]));
        var ticket = soumission.TicketsCrees[0];
        if (affecterAgentA)
        {
            ticket.PrendreEnCharge(agentA, DateTime.UtcNow);
            await db.SaveChangesAsync();
        }

        return new Contexte(db,
            new ValiderTicketImportCommand(tickets, dossiers, files, new SuiviBrancheIesRepository(db), autorisation, journalisation, horloge, unite),
            new DemanderReaffectationCommand(tickets, habilitations, journalisation, unite),
            new ResolveurSuperviseursDeLaFile(tickets, habilitations),
            agentA, agentB, superviseur, ticket.Id);
    }

    [Fact]
    public async Task Un_collegue_habilite_ne_peut_pas_traiter_un_ticket_pris_en_charge_par_un_autre_agent()
    {
        var c = await ConstruireAsync();

        await Assert.ThrowsAsync<TicketAffecteAUnAutreAgentException>(() => c.Valider.ExecuterAsync(c.AgentB, c.TicketId));
    }

    [Fact]
    public async Task L_agent_en_charge_traite_son_ticket()
    {
        var c = await ConstruireAsync();

        await c.Valider.ExecuterAsync(c.AgentA, c.TicketId);

        Assert.Contains(c.Db.HistoriqueTickets, h => h.TicketId == c.TicketId && h.Evenement == "TicketImportValide" && h.AuteurId == c.AgentA);
    }

    [Fact]
    public async Task Un_superviseur_peut_toujours_reprendre_la_main()
    {
        var c = await ConstruireAsync();

        await c.Valider.ExecuterAsync(c.Superviseur, c.TicketId);

        Assert.Contains(c.Db.HistoriqueTickets, h => h.TicketId == c.TicketId && h.Evenement == "TicketImportValide" && h.AuteurId == c.Superviseur);
    }

    [Fact]
    public async Task Un_ticket_non_affecte_reste_ouvert_a_tout_agent_habilite()
    {
        var c = await ConstruireAsync(affecterAgentA: false);

        await c.Valider.ExecuterAsync(c.AgentB, c.TicketId);

        Assert.Contains(c.Db.HistoriqueTickets, h => h.TicketId == c.TicketId && h.Evenement == "TicketImportValide");
    }

    [Fact]
    public async Task La_demande_de_reaffectation_notifie_les_superviseurs_et_l_agent_en_charge()
    {
        var c = await ConstruireAsync();

        await c.Demander.ExecuterAsync(c.AgentB, c.TicketId, "Agent A en congé");

        var evenement = Assert.Single(c.Db.JournalEvenements, e => e.TypeEvenement == "ReaffectationDemandee");
        var destinataires = await c.Resolveur.ResoudreAsync(evenement.TypeEvenement, evenement.ChargeUtile);
        Assert.Contains(c.Superviseur, destinataires);
        Assert.Contains(c.AgentA, destinataires);
        Assert.DoesNotContain(c.AgentB, destinataires);
        Assert.Contains(c.Db.HistoriqueTickets, h => h.TicketId == c.TicketId && h.Evenement == "ReaffectationDemandee" && h.Detail == "Agent A en congé");
    }

    [Fact]
    public async Task On_ne_demande_pas_la_reaffectation_de_son_propre_ticket()
    {
        var c = await ConstruireAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => c.Demander.ExecuterAsync(c.AgentA, c.TicketId, null));
    }
}
