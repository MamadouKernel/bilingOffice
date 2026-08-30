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
/// [TEST-SPEC] REMÉDIATION AUDIT (post-implementation-audit.md §8.2) — Contrat C3-18, IMP-41/IMP-42.
/// <see cref="ConstaterJointureIesEtFinaliserCommand"/> n'exigeait auparavant aucune habilitation ;
/// remplace <c>AuditAutorisationIesFinalisationTests</c> (qui démontrait l'écart, désormais fermé)
/// par la suite de régression permanente couvrant les 6 cas demandés par la remédiation (A→F).
/// </summary>
public sealed class ConstaterJointureIesAutorisationTests
{
    private sealed record Contexte(
        PlateformeDbContext DbContext,
        SoumettreDemandeImportCommand Soumettre,
        ValiderTicketImportCommand Valider,
        InstruireBrancheIesCommand Instruire,
        ConstaterJointureIesEtFinaliserCommand ConstaterJointure,
        Guid SocieteId,
        Guid AuteurId,
        Guid AgentManifesteId);

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
        var suivisRepository = new SuiviBrancheIesRepository(dbContext);
        var autorisation = new AutorisationService(dbContext);

        dbContext.Files.AddRange(
            FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, SoumettreDemandeImportCommand.FilePiloteManifeste),
            FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, NomsFiles.FileIes));
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations", TypeClient.IES, DateTime.UtcNow);
        societe.Valider();
        var auteurId = Guid.NewGuid();
        var agentManifesteId = Guid.NewGuid();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(auteurId, societe.Id, DateTime.UtcNow, droitSoumission: true));
        dbContext.Habilitations.AddRange(
            HabilitationUtilisateur.Attribuer(agentManifesteId, Permission.BillingImportManifesteInstruire),
            HabilitationUtilisateur.Attribuer(agentManifesteId, Permission.BillingImportIesManifesteInstruire));
        await dbContext.SaveChangesAsync();

        var soumettre = new SoumettreDemandeImportCommand(
            ticketRepository, dossierRepository, new SocieteClienteRepository(dbContext),
            new UtilisateurSocieteRepository(dbContext), fileRepository, numerotation, journalisation, horloge, uniteDeTravail);
        var valider = new ValiderTicketImportCommand(
            ticketRepository, dossierRepository, fileRepository, suivisRepository, autorisation, journalisation, horloge, uniteDeTravail);
        var instruire = new InstruireBrancheIesCommand(ticketRepository, suivisRepository, autorisation, journalisation, horloge, uniteDeTravail);
        var constaterJointure = new ConstaterJointureIesEtFinaliserCommand(
            ticketRepository, suivisRepository, autorisation, journalisation, horloge, uniteDeTravail);

        return new Contexte(dbContext, soumettre, valider, instruire, constaterJointure, societe.Id, auteurId, agentManifesteId);
    }

    private static async Task<Guid> CreerTicketIesAvecBranchesAcheveesAsync(Contexte c, string numeroBl, bool acheverManifeste, bool acheverCd, Guid agentCdId)
    {
        var soumission = await c.Soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(c.SocieteId, c.AuteurId, [numeroBl]));
        var ticketId = soumission.TicketsCrees[0].Id;
        await c.Valider.ExecuterAsync(c.AgentManifesteId, ticketId);
        if (acheverManifeste)
        {
            await c.Instruire.ExecuterAsync(c.AgentManifesteId, ticketId, BrancheIes.Manifeste);
        }
        if (acheverCd)
        {
            await c.Instruire.ExecuterAsync(agentCdId, ticketId, BrancheIes.ControleDocumentaire);
        }
        return ticketId;
    }

    [Fact]
    public async Task A_Utilisateur_habilite_branche_manifeste_deux_branches_achevees_finalisation_autorisee()
    {
        var c = await ConstruireContexteAsync();
        var agentCdId = Guid.NewGuid();
        c.DbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentCdId, Permission.BillingImportIesControleDocumentaireInstruire));
        await c.DbContext.SaveChangesAsync();
        var ticketId = await CreerTicketIesAvecBranchesAcheveesAsync(c, "BL-AUTIES-A", acheverManifeste: true, acheverCd: true, agentCdId);

        await c.ConstaterJointure.ExecuterAsync(c.AgentManifesteId, ticketId);

        var ticket = await c.DbContext.Tickets.FindAsync(ticketId);
        Assert.Equal(StatutTicket.Cloture, ticket!.Statut);
    }

    [Fact]
    public async Task B_Utilisateur_sans_permission_ies_est_refuse()
    {
        var c = await ConstruireContexteAsync();
        var agentCdId = Guid.NewGuid();
        c.DbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentCdId, Permission.BillingImportIesControleDocumentaireInstruire));
        await c.DbContext.SaveChangesAsync();
        var ticketId = await CreerTicketIesAvecBranchesAcheveesAsync(c, "BL-AUTIES-B", acheverManifeste: true, acheverCd: true, agentCdId);

        // Aucune habilitation billing_import.* d'aucune sorte.
        var intrus = Guid.NewGuid();
        await Assert.ThrowsAsync<AccesRefuseException>(() => c.ConstaterJointure.ExecuterAsync(intrus, ticketId));

        var ticket = await c.DbContext.Tickets.FindAsync(ticketId);
        Assert.NotEqual(StatutTicket.Cloture, ticket!.Statut);
    }

    [Fact]
    public async Task C_Utilisateur_habilite_sur_une_autre_file_est_refuse()
    {
        var c = await ConstruireContexteAsync();
        var agentCdId = Guid.NewGuid();
        c.DbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentCdId, Permission.BillingImportIesControleDocumentaireInstruire));
        await c.DbContext.SaveChangesAsync();
        var ticketId = await CreerTicketIesAvecBranchesAcheveesAsync(c, "BL-AUTIES-C", acheverManifeste: true, acheverCd: true, agentCdId);

        // Habilitation IES réelle, mais bornée à une AUTRE file que la File IES du ticket — un
        // rattachement de périmètre différent (SEC-11) ne doit jamais couvrir ce ticket.
        var agentAutreFile = Guid.NewGuid();
        c.DbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentAutreFile, Permission.BillingImportIesManifesteInstruire, Guid.NewGuid()));
        await c.DbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<AccesRefuseException>(() => c.ConstaterJointure.ExecuterAsync(agentAutreFile, ticketId));
    }

    [Fact]
    public async Task D_Identite_vide_est_refusee_aucun_contournement_anonyme()
    {
        var c = await ConstruireContexteAsync();
        var agentCdId = Guid.NewGuid();
        c.DbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentCdId, Permission.BillingImportIesControleDocumentaireInstruire));
        await c.DbContext.SaveChangesAsync();
        var ticketId = await CreerTicketIesAvecBranchesAcheveesAsync(c, "BL-AUTIES-D", acheverManifeste: true, acheverCd: true, agentCdId);

        await Assert.ThrowsAsync<AccesRefuseException>(() => c.ConstaterJointure.ExecuterAsync(Guid.Empty, ticketId));
    }

    [Fact]
    public async Task E_Une_seule_branche_achevee_et_utilisateur_autorise_reste_JointureIesIncompleteException()
    {
        var c = await ConstruireContexteAsync();
        var ticketId = await CreerTicketIesAvecBranchesAcheveesAsync(c, "BL-AUTIES-E", acheverManifeste: true, acheverCd: false, Guid.NewGuid());

        // L'agent EST habilité (branche Manifeste) — l'autorisation passe, mais la jointure reste
        // incomplète : la précondition métier prime, elle n'est jamais contournée par le fait
        // d'être autorisé à l'action.
        await Assert.ThrowsAsync<JointureIesIncompleteException>(() => c.ConstaterJointure.ExecuterAsync(c.AgentManifesteId, ticketId));
    }

    [Fact]
    public async Task F_Deux_branches_achevees_et_utilisateur_habilite_uniquement_branche_controle_documentaire_reussit()
    {
        var c = await ConstruireContexteAsync();
        var agentCdId = Guid.NewGuid();
        c.DbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentCdId, Permission.BillingImportIesControleDocumentaireInstruire));
        await c.DbContext.SaveChangesAsync();
        var ticketId = await CreerTicketIesAvecBranchesAcheveesAsync(c, "BL-AUTIES-F", acheverManifeste: true, acheverCd: true, agentCdId);

        // Symétrique de A : le Pilote Contrôle Documentaire (l'autre branche) peut également
        // finaliser — aucune des deux permissions n'est privilégiée par rapport à l'autre (D10).
        await c.ConstaterJointure.ExecuterAsync(agentCdId, ticketId);

        var ticket = await c.DbContext.Tickets.FindAsync(ticketId);
        Assert.Equal(StatutTicket.Cloture, ticket!.Statut);
    }
}
