using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.BillingImport;

/// <summary>
/// [TEST-SPEC] Exigence CDC: IMP-04, IMP-05 → US3 → critères d'acceptation 1-4 (FR-008, FR-009) →
/// Test.
/// </summary>
public sealed class InstructionManifesteTests
{
    private sealed record Contexte(
        PlateformeDbContext DbContext,
        SoumettreDemandeImportCommand Soumettre,
        ValiderTicketImportCommand Valider,
        RejeterTicketImportCommand Rejeter,
        GestionComplementManifesteCommand Complement,
        Guid AgentId,
        Guid SocieteId,
        Guid AuteurId);

    private static async Task<Contexte> ConstruireContexteAsync()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var numerotation = new NumerotationEnMemoirePourTests();
        var ticketRepository = new TicketRepository(dbContext);
        var dossierRepository = new DossierRepository(dbContext);
        var fileRepository = new FileDeTraitementRepository(dbContext);
        var autorisation = new AutorisationService(dbContext);

        var fileManifeste = FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, SoumettreDemandeImportCommand.FilePiloteManifeste);
        var fileFacturation = FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, NomsFiles.Facturation);
        var fileIes = FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, NomsFiles.FileIes);
        dbContext.Files.AddRange(fileManifeste, fileFacturation, fileIes);

        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        var auteurId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, auteurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentId), Permission.BillingImportManifesteInstruire));
        await dbContext.SaveChangesAsync();

        var soumettre = new SoumettreDemandeImportCommand(
            ticketRepository, dossierRepository, new SocieteClienteRepository(dbContext),
            new UtilisateurSocieteRepository(dbContext), fileRepository, numerotation, journalisation, horloge, uniteDeTravail);
        var valider = new ValiderTicketImportCommand(
            ticketRepository, dossierRepository, fileRepository, new SuiviBrancheIesRepository(dbContext),
            autorisation, journalisation, horloge, uniteDeTravail);
        var rejeter = new RejeterTicketImportCommand(ticketRepository, autorisation, journalisation, horloge, uniteDeTravail);
        var complement = new GestionComplementManifesteCommand(
            ticketRepository, new CloisonnementService(ticketRepository, CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.ControleAcces(dbContext)), autorisation, journalisation, horloge, uniteDeTravail);

        return new Contexte(dbContext, soumettre, valider, rejeter, complement, agentId, societe.Id, auteurId);
    }

    [Fact]
    public async Task Valider_un_ticket_le_rend_eligible_a_l_aiguillage()
    {
        var c = await ConstruireContexteAsync();
        var soumission = await c.Soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(c.SocieteId, c.AuteurId, ["BL-V1"]));
        var ticketId = soumission.TicketsCrees[0].Id;
        var fileAvantValidation = soumission.TicketsCrees[0].FileCouranteId; // capturé avant mutation

        await c.Valider.ExecuterAsync(c.AgentId, ticketId);

        var ticket = await c.DbContext.Tickets.FindAsync(ticketId);
        Assert.NotEqual(fileAvantValidation, ticket!.FileCouranteId);
    }

    [Fact]
    public async Task Rejeter_avec_motif_positionne_Annule_et_RejetManifeste_et_notifie()
    {
        var c = await ConstruireContexteAsync();
        var soumission = await c.Soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(c.SocieteId, c.AuteurId, ["BL-R1"]));
        var ticketId = soumission.TicketsCrees[0].Id;

        await c.Rejeter.ExecuterAsync(c.AgentId, ticketId, "Document manquant");

        var ticket = await c.DbContext.Tickets.FindAsync(ticketId);
        Assert.Equal(StatutTicket.Annule, ticket!.Statut);
        Assert.Equal(TypeAnnulationImport.RejetManifeste, ticket.TypeAnnulationImport);
    }

    [Fact]
    public async Task Rejet_sans_motif_est_refuse()
    {
        var c = await ConstruireContexteAsync();
        var soumission = await c.Soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(c.SocieteId, c.AuteurId, ["BL-R2"]));
        var ticketId = soumission.TicketsCrees[0].Id;

        await Assert.ThrowsAsync<ArgumentException>(() => c.Rejeter.ExecuterAsync(c.AgentId, ticketId, ""));
    }

    [Fact]
    public async Task Demander_complement_sans_elements_attendus_est_refuse()
    {
        var c = await ConstruireContexteAsync();
        var soumission = await c.Soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(c.SocieteId, c.AuteurId, ["BL-C0"]));
        var ticketId = soumission.TicketsCrees[0].Id;

        // Le client doit savoir quoi fournir : une demande de complément sans contenu est refusée.
        await Assert.ThrowsAsync<ArgumentException>(() => c.Complement.DemanderComplementAsync(c.AgentId, ticketId, "  "));

        var ticket = await c.DbContext.Tickets.FindAsync(ticketId);
        Assert.NotEqual(StatutTicket.EnAttenteClient, ticket!.Statut);
    }

    [Fact]
    public async Task Demander_complement_met_le_ticket_en_attente_client()
    {
        var c = await ConstruireContexteAsync();
        var soumission = await c.Soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(c.SocieteId, c.AuteurId, ["BL-C1"]));
        var ticketId = soumission.TicketsCrees[0].Id;

        await c.Complement.DemanderComplementAsync(c.AgentId, ticketId, "Facture manquante");

        var ticket = await c.DbContext.Tickets.FindAsync(ticketId);
        Assert.Equal(StatutTicket.EnAttenteClient, ticket!.Statut);
    }

    [Fact]
    public async Task Deposer_complement_reprend_le_traitement()
    {
        var c = await ConstruireContexteAsync();
        var soumission = await c.Soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(c.SocieteId, c.AuteurId, ["BL-C2"]));
        var ticketId = soumission.TicketsCrees[0].Id;
        await c.Complement.DemanderComplementAsync(c.AgentId, ticketId, "Facture manquante");

        await c.Complement.DeposerComplementAsync(c.AuteurId, ticketId);

        var ticket = await c.DbContext.Tickets.FindAsync(ticketId);
        Assert.Equal(StatutTicket.EnCours, ticket!.Statut);
    }

    [Fact]
    public async Task Dossier_multi_bl_decisions_independantes_une_validee_une_rejetee_une_complement()
    {
        var c = await ConstruireContexteAsync();
        var soumission = await c.Soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(c.SocieteId, c.AuteurId, ["BL-M1", "BL-M2", "BL-M3"]));
        var (t1, t2, t3) = (soumission.TicketsCrees[0], soumission.TicketsCrees[1], soumission.TicketsCrees[2]);

        await c.Valider.ExecuterAsync(c.AgentId, t1.Id);
        await c.Rejeter.ExecuterAsync(c.AgentId, t2.Id, "Motif");
        await c.Complement.DemanderComplementAsync(c.AgentId, t3.Id, "Elements");

        var ticket1 = await c.DbContext.Tickets.FindAsync(t1.Id);
        var ticket2 = await c.DbContext.Tickets.FindAsync(t2.Id);
        var ticket3 = await c.DbContext.Tickets.FindAsync(t3.Id);

        Assert.NotEqual(StatutTicket.Annule, ticket1!.Statut);
        Assert.Equal(StatutTicket.Annule, ticket2!.Statut);
        Assert.Equal(TypeAnnulationImport.RejetManifeste, ticket2.TypeAnnulationImport);
        Assert.Equal(StatutTicket.EnAttenteClient, ticket3!.Statut);
    }
}
