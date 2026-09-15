using CIT.Plateforme.Application.BillingExport;
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

namespace CIT.Plateforme.Application.Tests.BillingExport;

/// <summary>
/// [TEST-SPEC] EXP-03, EXP-04 / Contrat C5-08 — Circuit Client IES Export (Parallèle Back Office + Contrôle Doc, Jointure & Clôture).
/// </summary>
public sealed class CircuitClientIesExportTests
{
    private sealed record Contexte(
        PlateformeDbContext DbContext,
        SoumettreDemandeExportCommand Soumettre,
        ValiderTicketExportCommand Valider,
        InstruireBrancheIesExportCommand InstruireBranche,
        ConstaterJointureIesExportEtFinaliserCommand FinaliserIes,
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
        var suivisBranchesIes = new SuiviBrancheIesRepository(dbContext);
        var autorisation = new AutorisationService(dbContext);

        dbContext.Files.AddRange(
            FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, NomsFilesExport.BackOfficeExport),
            FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, NomsFilesExport.FileIes));

        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "SocieteExportIES", TypeClient.IES, DateTime.UtcNow);
        societe.Valider();
        var auteurId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, auteurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        dbContext.Habilitations.AddRange(
            HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentId), Permission.BillingExportBackOfficeInstruire),
            HabilitationUtilisateur.Attribuer(agentId, Permission.BillingExportIesBackOfficeInstruire),
            HabilitationUtilisateur.Attribuer(agentId, Permission.BillingExportIesControleDocumentaireInstruire));
        await dbContext.SaveChangesAsync();

        var soumettre = new SoumettreDemandeExportCommand(
            ticketRepository, dossierRepository, new SocieteClienteRepository(dbContext),
            new UtilisateurSocieteRepository(dbContext), fileRepository, numerotation, journalisation, horloge, uniteDeTravail);
        var valider = new ValiderTicketExportCommand(
            ticketRepository, dossierRepository, fileRepository, suivisBranchesIes,
            autorisation, journalisation, horloge, uniteDeTravail);
        var instruireBranche = new InstruireBrancheIesExportCommand(
            ticketRepository, suivisBranchesIes, autorisation, Support.FabriqueRequetes.PorteeChampsIes(dbContext), journalisation, horloge, uniteDeTravail);
        var finaliserIes = new ConstaterJointureIesExportEtFinaliserCommand(
            ticketRepository, suivisBranchesIes, autorisation, journalisation, horloge, uniteDeTravail);

        return new Contexte(dbContext, soumettre, valider, instruireBranche, finaliserIes, agentId, societe.Id, auteurId);
    }

    [Fact]
    public async Task Parcours_complet_client_ies_export_double_branche_et_cloture_apres_jointure()
    {
        var c = await ConstruireContexteAsync();
        var soumission = await c.Soumettre.ExecuterAsync(new SoumettreDemandeExportRequete(c.SocieteId, c.AuteurId, ["BKG-IES-01"]));
        var ticket = soumission.TicketsCrees[0];

        // 1. Validation Back Office -> Aiguillage File IES et création des 2 branches (BackOffice + ControleDoc)
        await c.Valider.ExecuterAsync(c.AgentId, ticket.Id);

        // Tentative de finalisation prématurée -> doit lever JointureIesIncompleteException
        await Assert.ThrowsAsync<JointureIesIncompleteException>(() => c.FinaliserIes.ExecuterAsync(c.AgentId, ticket.Id));

        // 2. Achèvement branche BackOffice
        await c.InstruireBranche.ExecuterAsync(c.AgentId, ticket.Id, BrancheIes.BackOffice);

        // Tentative encore incomplète -> doit lever JointureIesIncompleteException
        await Assert.ThrowsAsync<JointureIesIncompleteException>(() => c.FinaliserIes.ExecuterAsync(c.AgentId, ticket.Id));

        // 3. Achèvement branche Contrôle Documentaire
        await c.InstruireBranche.ExecuterAsync(c.AgentId, ticket.Id, BrancheIes.ControleDocumentaire);

        // 4. Les 2 branches sont achevées -> Jointure constatée et clôture
        await c.FinaliserIes.ExecuterAsync(c.AgentId, ticket.Id);

        var ticketCloture = await new TicketRepository(c.DbContext).ObtenirParIdAsync(ticket.Id);
        Assert.NotNull(ticketCloture);
        Assert.Equal(StatutTicket.Cloture, ticketCloture.Statut);
    }
}
