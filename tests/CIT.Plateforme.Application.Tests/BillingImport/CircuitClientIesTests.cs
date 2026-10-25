using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CIT.Plateforme.Application.Tests.BillingImport;

/// <summary>
/// [TEST-SPEC/TEST-TECH] Exigence CDC: IMP-40, IMP-41 → US8 → critères d'acceptation 1-2 (FR-024,
/// FR-025) → Test. Circuit Client IES : traitement parallèle Manifeste/Contrôle Documentaire,
/// jointure dérivée (research.md D9), confidentialité par permission de branche (D10).
/// </summary>
public sealed class CircuitClientIesTests
{
    private sealed record Contexte(
        PlateformeDbContext DbContext,
        SoumettreDemandeImportCommand Soumettre,
        ValiderTicketImportCommand Valider,
        InstruireBrancheIesCommand Instruire,
        ConstaterJointureIesEtFinaliserCommand ConstaterJointure,
        Guid AgentManifesteId,
        Guid AgentControleDocumentaireId,
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
        var suivisBranchesIesRepository = new SuiviBrancheIesRepository(dbContext);
        var autorisation = new AutorisationService(dbContext);

        dbContext.Files.AddRange(
            FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, SoumettreDemandeImportCommand.FilePiloteManifeste),
            FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, NomsFiles.FileIes));

        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations", TypeClient.IES, DateTime.UtcNow);
        societe.Valider();
        var auteurId = Guid.NewGuid();
        var agentManifesteId = Guid.NewGuid();
        var agentControleDocumentaireId = Guid.NewGuid();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, auteurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        dbContext.Habilitations.AddRange(
            HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentManifesteId), Permission.BillingImportManifesteInstruire),
            HabilitationUtilisateur.Attribuer(agentManifesteId, Permission.BillingImportIesManifesteInstruire),
            HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentControleDocumentaireId), Permission.BillingImportIesControleDocumentaireInstruire));
        await dbContext.SaveChangesAsync();

        var soumettre = new SoumettreDemandeImportCommand(
            ticketRepository, dossierRepository, new SocieteClienteRepository(dbContext),
            new UtilisateurSocieteRepository(dbContext), fileRepository, numerotation, journalisation, horloge, uniteDeTravail);
        var valider = new ValiderTicketImportCommand(
            ticketRepository, dossierRepository, fileRepository, suivisBranchesIesRepository,
            autorisation, journalisation, horloge, uniteDeTravail);
        var instruire = new InstruireBrancheIesCommand(ticketRepository, suivisBranchesIesRepository, autorisation, Support.FabriqueRequetes.PorteeChampsIes(dbContext), journalisation, horloge, uniteDeTravail);
        var constaterJointure = new ConstaterJointureIesEtFinaliserCommand(ticketRepository, suivisBranchesIesRepository, autorisation, journalisation, horloge, uniteDeTravail);

        return new Contexte(
            dbContext, soumettre, valider, instruire, constaterJointure,
            agentManifesteId, agentControleDocumentaireId, societe.Id, auteurId);
    }

    private static async Task<Guid> CreerEtAiguillerTicketIesAsync(Contexte c, string numeroBl)
    {
        var soumission = await c.Soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(c.SocieteId, c.AuteurId, [numeroBl]));
        var ticketId = soumission.TicketsCrees[0].Id;
        await PriseEnChargeDeTest.PrendreAsync(c.DbContext, ticketId, c.AgentManifesteId);
        await c.Valider.ExecuterAsync(c.AgentManifesteId, ticketId);
        return ticketId;
    }

    [Fact]
    public async Task Ticket_ies_aiguille_cree_deux_suivis_de_branche_non_acheves()
    {
        var c = await ConstruireContexteAsync();
        var ticketId = await CreerEtAiguillerTicketIesAsync(c, "BL-IES1");

        var suivis = await c.DbContext.SuivisBranchesIes.Where(s => s.TicketId == ticketId).ToListAsync();

        Assert.Equal(2, suivis.Count);
        Assert.Contains(suivis, s => s.Branche == BrancheIes.Manifeste);
        Assert.Contains(suivis, s => s.Branche == BrancheIes.ControleDocumentaire);
        Assert.All(suivis, s => Assert.False(s.EstTerminee));
    }

    [Fact]
    public async Task Manifeste_achevee_seule_ne_restitue_pas_le_ticket()
    {
        var c = await ConstruireContexteAsync();
        var ticketId = await CreerEtAiguillerTicketIesAsync(c, "BL-IES2");

        await c.Instruire.ExecuterAsync(c.AgentManifesteId, ticketId, BrancheIes.Manifeste);

        var suivis = await c.DbContext.SuivisBranchesIes.Where(s => s.TicketId == ticketId).ToListAsync();
        Assert.True(suivis.Single(s => s.Branche == BrancheIes.Manifeste).EstTerminee);
        Assert.False(suivis.Single(s => s.Branche == BrancheIes.ControleDocumentaire).EstTerminee);

        await Assert.ThrowsAsync<JointureIesIncompleteException>(() => c.ConstaterJointure.ExecuterAsync(c.AgentManifesteId, ticketId));
        var ticket = await c.DbContext.Tickets.FindAsync(ticketId);
        Assert.NotEqual(StatutTicket.Cloture, ticket!.Statut);
    }

    [Fact]
    public async Task Controle_documentaire_achevee_seule_ne_restitue_pas_le_ticket()
    {
        var c = await ConstruireContexteAsync();
        var ticketId = await CreerEtAiguillerTicketIesAsync(c, "BL-IES3");

        await c.Instruire.ExecuterAsync(c.AgentControleDocumentaireId, ticketId, BrancheIes.ControleDocumentaire);

        var suivis = await c.DbContext.SuivisBranchesIes.Where(s => s.TicketId == ticketId).ToListAsync();
        Assert.True(suivis.Single(s => s.Branche == BrancheIes.ControleDocumentaire).EstTerminee);
        Assert.False(suivis.Single(s => s.Branche == BrancheIes.Manifeste).EstTerminee);

        await Assert.ThrowsAsync<JointureIesIncompleteException>(() => c.ConstaterJointure.ExecuterAsync(c.AgentControleDocumentaireId, ticketId));
        var ticket = await c.DbContext.Tickets.FindAsync(ticketId);
        Assert.NotEqual(StatutTicket.Cloture, ticket!.Statut);
    }

    [Fact]
    public async Task Ordre_manifeste_puis_controle_documentaire_atteint_la_jointure()
    {
        var c = await ConstruireContexteAsync();
        var ticketId = await CreerEtAiguillerTicketIesAsync(c, "BL-IES4");

        await c.Instruire.ExecuterAsync(c.AgentManifesteId, ticketId, BrancheIes.Manifeste);
        await c.Instruire.ExecuterAsync(c.AgentControleDocumentaireId, ticketId, BrancheIes.ControleDocumentaire);
        await c.ConstaterJointure.ExecuterAsync(c.AgentControleDocumentaireId, ticketId);

        var ticket = await c.DbContext.Tickets.FindAsync(ticketId);
        Assert.Equal(StatutTicket.Cloture, ticket!.Statut);
    }

    [Fact]
    public async Task Ordre_controle_documentaire_puis_manifeste_atteint_la_jointure_meme_resultat()
    {
        var c = await ConstruireContexteAsync();
        var ticketId = await CreerEtAiguillerTicketIesAsync(c, "BL-IES5");

        await c.Instruire.ExecuterAsync(c.AgentControleDocumentaireId, ticketId, BrancheIes.ControleDocumentaire);
        await c.Instruire.ExecuterAsync(c.AgentManifesteId, ticketId, BrancheIes.Manifeste);
        await c.ConstaterJointure.ExecuterAsync(c.AgentManifesteId, ticketId);

        var ticket = await c.DbContext.Tickets.FindAsync(ticketId);
        Assert.Equal(StatutTicket.Cloture, ticket!.Statut);
    }

    [Fact]
    public async Task Agent_ne_detenant_que_la_permission_manifeste_ne_peut_pas_instruire_la_branche_controle_documentaire()
    {
        var c = await ConstruireContexteAsync();
        var ticketId = await CreerEtAiguillerTicketIesAsync(c, "BL-IES6");

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            c.Instruire.ExecuterAsync(c.AgentManifesteId, ticketId, BrancheIes.ControleDocumentaire));

        var suivi = (await c.DbContext.SuivisBranchesIes.Where(s => s.TicketId == ticketId).ToListAsync())
            .Single(s => s.Branche == BrancheIes.ControleDocumentaire);
        Assert.False(suivi.EstTerminee);
    }

    [Fact]
    public async Task Agent_ne_detenant_que_la_permission_controle_documentaire_ne_peut_pas_instruire_la_branche_manifeste()
    {
        var c = await ConstruireContexteAsync();
        var ticketId = await CreerEtAiguillerTicketIesAsync(c, "BL-IES7");

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            c.Instruire.ExecuterAsync(c.AgentControleDocumentaireId, ticketId, BrancheIes.Manifeste));

        var suivi = (await c.DbContext.SuivisBranchesIes.Where(s => s.TicketId == ticketId).ToListAsync())
            .Single(s => s.Branche == BrancheIes.Manifeste);
        Assert.False(suivi.EstTerminee);
    }
}
