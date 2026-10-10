using CIT.Plateforme.Application.BillingImport;
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
/// Passation de chiffre (relève d'équipe) : l'agent qui détient un ticket peut le rendre à sa file
/// pour que l'équipe entrante le reprenne ; un collègue non détenteur ne le peut pas (il faut être
/// l'agent assigné ou un superviseur).
/// </summary>
public sealed class RendreTicketALaFileTests
{
    private sealed record Contexte(PlateformeDbContext Db, RendreTicketALaFileCommand Rendre, Guid AgentA, Guid AgentB, Guid Superviseur, Guid TicketId);

    private static async Task<Contexte> ConstruireAsync()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(db, horloge);
        var unite = new UniteDeTravail(db);
        var tickets = new TicketRepository(db);
        var dossiers = new DossierRepository(db);
        var files = new FileDeTraitementRepository(db);
        var autorisation = new AutorisationService(db);

        db.Files.Add(FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, SoumettreDemandeImportCommand.FilePiloteManifeste));
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
            .ExecuterAsync(new SoumettreDemandeImportRequete(societe.Id, auteurId, ["BL-RDF-" + Guid.NewGuid().ToString("N")[..6]]));
        var ticket = soumission.TicketsCrees[0];
        ticket.PrendreEnCharge(agentA, DateTime.UtcNow);
        await db.SaveChangesAsync();

        return new Contexte(db, new RendreTicketALaFileCommand(tickets, autorisation, journalisation, horloge, unite),
            agentA, agentB, superviseur, ticket.Id);
    }

    [Fact]
    public async Task L_agent_qui_detient_le_ticket_le_rend_a_la_file()
    {
        var c = await ConstruireAsync();

        await c.Rendre.ExecuterAsync(c.AgentA, c.TicketId);

        var ticket = await new TicketRepository(c.Db).ObtenirParIdAsync(c.TicketId);
        Assert.Null(ticket!.OperateurAssigneId);
        Assert.Equal(StatutTicket.EnAttenteDePriseEnCharge, ticket.Statut);
        Assert.Contains(c.Db.HistoriqueTickets, h => h.TicketId == c.TicketId && h.Evenement == "TicketRenduALaFile" && h.AuteurId == c.AgentA);
    }

    [Fact]
    public async Task Un_collegue_non_detenteur_ne_peut_pas_rendre_le_ticket()
    {
        var c = await ConstruireAsync();

        await Assert.ThrowsAsync<TicketAffecteAUnAutreAgentException>(() => c.Rendre.ExecuterAsync(c.AgentB, c.TicketId));
    }

    [Fact]
    public async Task Un_superviseur_peut_rendre_le_ticket_a_la_file()
    {
        var c = await ConstruireAsync();

        await c.Rendre.ExecuterAsync(c.Superviseur, c.TicketId);

        var ticket = await new TicketRepository(c.Db).ObtenirParIdAsync(c.TicketId);
        Assert.Null(ticket!.OperateurAssigneId);
        Assert.Equal(StatutTicket.EnAttenteDePriseEnCharge, ticket.Statut);
    }
}
