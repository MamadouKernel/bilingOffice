using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.BillingImport;

/// <summary>
/// [TEST-SPEC] Exigence CDC: IMP-15 → US9 → critère d'acceptation 1 (FR-027) → Test. Contrat C3-19 —
/// research.md D12 : aucun mécanisme de RDV/guichet, seule la déclaration/trace est portée par
/// Feature 003 (point d'extension pour la future Feature 009).
/// </summary>
public sealed class DeclarationPresentielleTests
{
    private sealed record Contexte(
        PlateformeDbContext DbContext,
        SoumettreDemandeImportCommand Soumettre,
        ValiderTicketImportCommand Valider,
        DeclarerFinalisationPresentielleCommand Declarer,
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
        var cloisonnement = new CloisonnementService(ticketRepository, CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.ControleAcces(dbContext));

        dbContext.Files.AddRange(
            FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, SoumettreDemandeImportCommand.FilePiloteManifeste),
            FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, NomsFiles.Facturation),
            FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, NomsFiles.FileIes));

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
        var declarer = new DeclarerFinalisationPresentielleCommand(
            ticketRepository, fileRepository, cloisonnement, journalisation, uniteDeTravail);

        return new Contexte(dbContext, soumettre, valider, declarer, agentId, societe.Id, auteurId);
    }

    [Fact]
    public async Task Client_declare_vouloir_finaliser_physiquement_apres_validation_manifeste()
    {
        var c = await ConstruireContexteAsync();
        var soumission = await c.Soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(c.SocieteId, c.AuteurId, ["BL-PR1"]));
        var ticketId = soumission.TicketsCrees[0].Id;
        await c.Valider.ExecuterAsync(c.AgentId, ticketId);

        await c.Declarer.ExecuterAsync(c.AuteurId, ticketId);

        var ticket = await c.DbContext.Tickets.FindAsync(ticketId);
        Assert.True(ticket!.DeclarationPresentielle);
        Assert.Contains(c.DbContext.HistoriqueTickets, e => e.TicketId == ticketId && e.Evenement == "FinalisationPresentielleDeclaree");
    }

    [Fact]
    public async Task Declaration_tentee_avant_validation_manifeste_est_refusee()
    {
        var c = await ConstruireContexteAsync();
        var soumission = await c.Soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(c.SocieteId, c.AuteurId, ["BL-PR2"]));
        var ticketId = soumission.TicketsCrees[0].Id;

        await Assert.ThrowsAsync<InvalidOperationException>(() => c.Declarer.ExecuterAsync(c.AuteurId, ticketId));

        var ticket = await c.DbContext.Tickets.FindAsync(ticketId);
        Assert.False(ticket!.DeclarationPresentielle);
    }

    [Fact]
    public async Task Un_tiers_non_proprietaire_ne_peut_pas_declarer()
    {
        var c = await ConstruireContexteAsync();
        var soumission = await c.Soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(c.SocieteId, c.AuteurId, ["BL-PR3"]));
        var ticketId = soumission.TicketsCrees[0].Id;
        await c.Valider.ExecuterAsync(c.AgentId, ticketId);

        var tiersId = Guid.NewGuid();
        await Assert.ThrowsAsync<AccesRefuseException>(() => c.Declarer.ExecuterAsync(tiersId, ticketId));
    }
}
