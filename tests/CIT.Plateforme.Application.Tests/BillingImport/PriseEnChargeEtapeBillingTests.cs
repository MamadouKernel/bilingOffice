using CIT.Plateforme.Application.BillingExport;
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

namespace CIT.Plateforme.Application.Tests.BillingImport;

/// <summary>
/// Option B (décision du 05/10/2026) — prise en charge explicite par étape sur les parcours Billing
/// IMPORT et EXPORT : une action de traitement n'est permise qu'à l'agent qui a pris le ticket en
/// charge sur l'étape courante (<see cref="Ticket.ExigerPriseEnChargePar"/>), et la prise en charge
/// elle-même (<see cref="PrendreEnChargeEtapeBillingCommand"/>) exige une habilitation d'étape.
/// </summary>
public sealed class PriseEnChargeEtapeBillingTests
{
    private sealed record Socle(
        PlateformeDbContext Db,
        PrendreEnChargeEtapeBillingCommand Prendre,
        GestionComplementManifesteCommand ComplementImport,
        GestionComplementExportCommand ComplementExport,
        Guid AgentId);

    private const string FileImport = "Pilote Manifeste";
    private const string FileExport = "Back Office Export";

    private static Socle Construire(Guid agentId, params string[] permissionsAgent)
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(db, horloge);
        var unite = new UniteDeTravail(db);
        var tickets = new TicketRepository(db);
        var autorisation = new AutorisationService(db);
        var cloisonnement = new CloisonnementService(tickets, FabriqueRequetes.ControleAcces(db));

        db.Files.AddRange(
            FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, FileImport),
            FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, FileExport));

        var compte = CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, agentId);
        foreach (var permission in permissionsAgent)
        {
            db.Habilitations.Add(HabilitationUtilisateur.Attribuer(compte, permission));
        }
        db.SaveChanges();

        return new Socle(
            db,
            new PrendreEnChargeEtapeBillingCommand(tickets, autorisation, journalisation, horloge, unite),
            new GestionComplementManifesteCommand(tickets, cloisonnement, autorisation, journalisation, horloge, unite),
            new GestionComplementExportCommand(tickets, cloisonnement, autorisation, journalisation, horloge, unite),
            agentId);
    }

    private static async Task<Guid> CreerTicketAsync(Socle s, string module, string fileNom, string numero)
    {
        var fileId = s.Db.Files.Single(f => f.Module == module && f.Nom == fileNom).Id;
        var ticket = Ticket.Creer(numero, module, "STANDARD", fileId, Guid.NewGuid(), DateTime.UtcNow, numeroBl: numero);
        s.Db.Tickets.Add(ticket);
        await s.Db.SaveChangesAsync();
        return ticket.Id;
    }

    // ---- IMPORT ----------------------------------------------------------

    [Fact]
    public async Task Import_le_traitement_sans_prise_en_charge_est_refuse()
    {
        var agent = Guid.NewGuid();
        var s = Construire(agent, Permission.BillingImportManifesteInstruire);
        var ticketId = await CreerTicketAsync(s, SoumettreDemandeImportCommand.Module, FileImport, "BL-PEC-IMP-1");

        // Ticket en attente de prise en charge : demander un complément sans l'avoir pris est refusé.
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => s.ComplementImport.DemanderComplementAsync(agent, ticketId, "Facture manquante"));
    }

    [Fact]
    public async Task Import_apres_prise_en_charge_le_traitement_reussit()
    {
        var agent = Guid.NewGuid();
        var s = Construire(agent, Permission.BillingImportManifesteInstruire);
        var ticketId = await CreerTicketAsync(s, SoumettreDemandeImportCommand.Module, FileImport, "BL-PEC-IMP-2");

        await s.Prendre.ExecuterAsync(agent, ticketId, [Permission.BillingImportManifesteInstruire]);
        await s.ComplementImport.DemanderComplementAsync(agent, ticketId, "Facture manquante");

        var ticket = (await s.Db.Tickets.FindAsync(ticketId))!;
        Assert.Equal(StatutTicket.EnAttenteClient, ticket.Statut);
        Assert.Equal(agent, ticket.OperateurAssigneId);
    }

    // ---- EXPORT ----------------------------------------------------------

    [Fact]
    public async Task Export_le_traitement_sans_prise_en_charge_est_refuse()
    {
        var agent = Guid.NewGuid();
        var s = Construire(agent, Permission.BillingExportBackOfficeInstruire);
        var ticketId = await CreerTicketAsync(s, SoumettreDemandeExportCommand.Module, FileExport, "BK-PEC-EXP-1");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => s.ComplementExport.DemanderComplementAsync(agent, ticketId, "Pièce manquante"));
    }

    [Fact]
    public async Task Export_apres_prise_en_charge_le_traitement_reussit()
    {
        var agent = Guid.NewGuid();
        var s = Construire(agent, Permission.BillingExportBackOfficeInstruire);
        var ticketId = await CreerTicketAsync(s, SoumettreDemandeExportCommand.Module, FileExport, "BK-PEC-EXP-2");

        await s.Prendre.ExecuterAsync(agent, ticketId, [Permission.BillingExportBackOfficeInstruire]);
        await s.ComplementExport.DemanderComplementAsync(agent, ticketId, "Pièce manquante");

        var ticket = (await s.Db.Tickets.FindAsync(ticketId))!;
        Assert.Equal(StatutTicket.EnAttenteClient, ticket.Statut);
        Assert.Equal(agent, ticket.OperateurAssigneId);
    }

    // ---- PrendreEnChargeEtapeBillingCommand ------------------------------

    [Fact]
    public async Task Prise_en_charge_sans_permission_d_etape_est_refusee()
    {
        var agent = Guid.NewGuid();
        // Agent habilité sur une AUTRE étape que celle exigée.
        var s = Construire(agent, Permission.BillingImportFacturationEditer);
        var ticketId = await CreerTicketAsync(s, SoumettreDemandeImportCommand.Module, FileImport, "BL-PEC-IMP-3");

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => s.Prendre.ExecuterAsync(agent, ticketId, [Permission.BillingImportManifesteInstruire]));

        var ticket = (await s.Db.Tickets.FindAsync(ticketId))!;
        Assert.Equal(StatutTicket.EnAttenteDePriseEnCharge, ticket.Statut);
        Assert.Null(ticket.OperateurAssigneId);
    }

    [Fact]
    public async Task Prise_en_charge_avec_permission_rend_le_ticket_en_cours_et_affecte()
    {
        var agent = Guid.NewGuid();
        var s = Construire(agent, Permission.BillingImportManifesteInstruire);
        var ticketId = await CreerTicketAsync(s, SoumettreDemandeImportCommand.Module, FileImport, "BL-PEC-IMP-4");

        await s.Prendre.ExecuterAsync(agent, ticketId, [Permission.BillingImportManifesteInstruire]);

        var ticket = (await s.Db.Tickets.FindAsync(ticketId))!;
        Assert.Equal(StatutTicket.EnCours, ticket.Statut);
        Assert.Equal(agent, ticket.OperateurAssigneId);
    }

    [Fact]
    public async Task Un_seul_agent_gagne_la_prise_en_charge_le_second_est_refuse()
    {
        var agent1 = Guid.NewGuid();
        var s = Construire(agent1, Permission.BillingImportManifesteInstruire);
        var agent2 = Guid.NewGuid();
        s.Db.Habilitations.Add(HabilitationUtilisateur.Attribuer(
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(s.Db, agent2), Permission.BillingImportManifesteInstruire));
        await s.Db.SaveChangesAsync();
        var ticketId = await CreerTicketAsync(s, SoumettreDemandeImportCommand.Module, FileImport, "BL-PEC-IMP-5");

        await s.Prendre.ExecuterAsync(agent1, ticketId, [Permission.BillingImportManifesteInstruire]);

        // Le ticket n'est plus « en attente » : le second agent, pourtant habilité, ne peut plus le
        // prendre — un seul propriétaire par étape. (La concurrence stricte simultanée est en outre
        // arbitrée par le jeton de concurrence EF via UniteDeTravail, couvert côté Infrastructure.)
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => s.Prendre.ExecuterAsync(agent2, ticketId, [Permission.BillingImportManifesteInstruire]));

        var ticket = (await s.Db.Tickets.FindAsync(ticketId))!;
        Assert.Equal(agent1, ticket.OperateurAssigneId);
    }
}
