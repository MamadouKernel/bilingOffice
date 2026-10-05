using System.Text;
using CIT.Plateforme.Application.BillingExport;
using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Application.Documents;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Documents;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Tests.Partages;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CIT.Plateforme.Application.Tests.BillingExport;

/// <summary>
/// [TEST-SPEC — RG-EXP-03] LE DRAFT IN PRÉCÈDE LA FACTURE EXPORT.
///
/// DÉFAUT CORRIGÉ (audit final, <c>docs/audit-final-conformite-98RG.md</c>) : la règle énonce deux
/// adaptations de l'export par rapport à l'import — le BEE comme document final, <b>et le Draft IN
/// créé par le Pilote Facturation avant l'édition de la facture</b>. Le BEE existait ; le Draft IN
/// n'existait <b>nulle part</b> : ni entité, ni commande, ni étape. Aucun test ne le réclamait,
/// et le parcours passait de la codification à la facture définitive.
/// </summary>
public sealed class DraftInExportTests
{
    private static readonly ReglesDepotDocument Regles = new(["pdf", "jpg"], TailleMaxOctets: 1024);

    private sealed record Contexte(
        PlateformeDbContext Db,
        SoumettreDemandeExportCommand Soumettre,
        ValiderTicketExportCommand Valider,
        EmettreDraftInExportCommand EmettreDraftIn,
        EditerFactureExportCommand EditerFacture,
        Guid AgentId,
        Guid AgentSansDroitId,
        Guid SocieteId,
        Guid AuteurId);

    private static async Task<Contexte> ConstruireAsync(TypeClient profil = TypeClient.EnCompte)
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(db, horloge);
        var uniteDeTravail = new UniteDeTravail(db);
        var numerotation = new NumerotationEnMemoirePourTests();
        var ticketRepository = new TicketRepository(db);
        var dossierRepository = new DossierRepository(db);
        var fileRepository = new FileDeTraitementRepository(db);
        var autorisation = new AutorisationService(db);
        var cloisonnement = new CloisonnementService(ticketRepository, FabriqueRequetes.ControleAcces(db));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var deposerDocument = new DeposerDocumentCommand(
            new DocumentRepository(db), ticketRepository,
            new StockageDocumentsFichiers(configuration, new EnvironnementDeTest("Development")),
            new AucuneAnalyseAntivirusConfiguree(), autorisation, cloisonnement, journalisation, horloge, uniteDeTravail);

        db.Files.AddRange(
            FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, NomsFilesExport.BackOfficeExport),
            FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, NomsFilesExport.Facturation),
            FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, NomsFilesExport.ValidationPaiement),
            FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, NomsFilesExport.ComptabiliteClient),
            FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, NomsFilesExport.ControleDocumentaire),
            FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, NomsFilesExport.FileIes));

        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "SocieteExportDraftIn", profil, DateTime.UtcNow);
        societe.Valider();
        var auteurId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var agentSansDroitId = Guid.NewGuid();

        db.Societes.Add(societe);
        db.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(
            ComptesDeTest.CompteActif(db, auteurId, NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        db.Habilitations.AddRange(
            HabilitationUtilisateur.Attribuer(ComptesDeTest.CompteActif(db, agentId), Permission.BillingExportBackOfficeInstruire),
            HabilitationUtilisateur.Attribuer(agentId, Permission.BillingExportFacturationEditer),
            HabilitationUtilisateur.Attribuer(agentId, Permission.DocumentDeposer),
            // Cet agent instruit le Back Office mais ne facture PAS : le Draft IN doit lui être refusé.
            HabilitationUtilisateur.Attribuer(ComptesDeTest.CompteActif(db, agentSansDroitId), Permission.BillingExportBackOfficeInstruire),
            HabilitationUtilisateur.Attribuer(agentSansDroitId, Permission.DocumentDeposer));
        await db.SaveChangesAsync();

        var soumettre = new SoumettreDemandeExportCommand(
            ticketRepository, dossierRepository, new SocieteClienteRepository(db),
            new UtilisateurSocieteRepository(db), fileRepository, numerotation, journalisation, horloge, uniteDeTravail);
        var valider = new ValiderTicketExportCommand(
            ticketRepository, dossierRepository, fileRepository, new SuiviBrancheIesRepository(db),
            autorisation, journalisation, horloge, uniteDeTravail);
        var emettreDraftIn = new EmettreDraftInExportCommand(
            ticketRepository, deposerDocument, autorisation, journalisation, horloge, uniteDeTravail);
        var editerFacture = new EditerFactureExportCommand(
            ticketRepository, dossierRepository, fileRepository, deposerDocument, autorisation, journalisation, horloge, uniteDeTravail);

        return new Contexte(db, soumettre, valider, emettreDraftIn, editerFacture, agentId, agentSansDroitId, societe.Id, auteurId);
    }

    private static MemoryStream Fichier(string contenu) => new(Encoding.UTF8.GetBytes(contenu));

    private static async Task<Ticket> TicketEnFacturationAsync(Contexte c)
    {
        var soumission = await c.Soumettre.ExecuterAsync(
            new SoumettreDemandeExportRequete(c.SocieteId, c.AuteurId, ["BKG-DRAFT-01"]));
        var ticket = soumission.TicketsCrees[0];
        await c.Valider.ExecuterAsync(c.AgentId, ticket.Id);
        return ticket;
    }

    /// <summary>Parcours nominal : codification → Draft IN → facture.</summary>
    [Fact]
    public async Task Export_normal_le_draft_in_precede_la_facture()
    {
        var c = await ConstruireAsync(TypeClient.Normal);
        using var _ = c.Db;
        var ticket = await TicketEnFacturationAsync(c);

        Assert.False(ticket.DraftInExportEmis);

        await c.EmettreDraftIn.ExecuterAsync(
            c.AgentId, ticket.Id, "DraftIN-001.pdf", "application/pdf", Fichier("draft"), 5, Regles);

        Assert.True(ticket.DraftInExportEmis);
        Assert.NotNull(ticket.DraftInExportEmisLeUtc);

        await ProformaDeTest.ValiderAsync(c.Db, ticket.Id, c.AgentId);
        await c.EditerFacture.ExecuterAsync(
            c.AgentId, ticket.Id, "Facture-001.pdf", "application/pdf", Fichier("facture"), 7, Regles);

        Assert.True(ticket.EstEnEtatEngageant);
    }

    /// <summary>
    /// SANS Draft IN, la facture est refusée. C'est l'invariant que la règle porte, et il ne tenait
    /// à rien avant cette remédiation.
    /// </summary>
    [Fact]
    public async Task Facture_refusee_si_le_draft_in_n_a_pas_ete_emis()
    {
        var c = await ConstruireAsync();
        using var _ = c.Db;
        var ticket = await TicketEnFacturationAsync(c);

        await Assert.ThrowsAsync<DraftInRequisException>(() => c.EditerFacture.ExecuterAsync(
            c.AgentId, ticket.Id, "Facture-001.pdf", "application/pdf", Fichier("facture"), 7, Regles));

        // Le refus ne laisse rien derrière lui : ni document, ni transfert, ni engagement.
        Assert.Empty(c.Db.Documents.Where(d => d.TicketId == ticket.Id));
        Assert.False(ticket.EstEnEtatEngageant);
    }

    /// <summary>Le Draft IN est historisé et audité, comme toute action significative (RG-SEC-03).</summary>
    [Fact]
    public async Task Draft_in_est_historise_et_audite()
    {
        var c = await ConstruireAsync();
        using var _ = c.Db;
        var ticket = await TicketEnFacturationAsync(c);

        await c.EmettreDraftIn.ExecuterAsync(
            c.AgentId, ticket.Id, "DraftIN-001.pdf", "application/pdf", Fichier("draft"), 5, Regles);

        Assert.Contains(c.Db.HistoriqueTickets, h => h.TicketId == ticket.Id && h.Evenement == "DraftInExportEmis");
        Assert.Contains(c.Db.JournalAudit, a => a.Action == "DraftInExportEmis" && a.EntiteId == ticket.Id);

        // Le document est bien joint au ticket.
        Assert.Contains(c.Db.Documents, d => d.TicketId == ticket.Id && d.NomFichier == "DraftIN-001.pdf");
    }

    /// <summary>Le Draft IN n'est pas un document officiel : il ne rend pas le ticket engageant.</summary>
    [Fact]
    public async Task Draft_in_ne_rend_pas_le_ticket_engageant()
    {
        var c = await ConstruireAsync();
        using var _ = c.Db;
        var ticket = await TicketEnFacturationAsync(c);

        await c.EmettreDraftIn.ExecuterAsync(
            c.AgentId, ticket.Id, "DraftIN-001.pdf", "application/pdf", Fichier("draft"), 5, Regles);

        Assert.False(ticket.EstEnEtatEngageant);
    }

    /// <summary>Un agent sans l'habilitation de facturation ne peut pas émettre le Draft IN.</summary>
    [Fact]
    public async Task Mauvais_role_ne_peut_pas_emettre_le_draft_in()
    {
        var c = await ConstruireAsync();
        using var _ = c.Db;
        var ticket = await TicketEnFacturationAsync(c);

        await Assert.ThrowsAsync<AccesRefuseException>(() => c.EmettreDraftIn.ExecuterAsync(
            c.AgentSansDroitId, ticket.Id, "DraftIN-001.pdf", "application/pdf", Fichier("draft"), 5, Regles));

        Assert.False(ticket.DraftInExportEmis);
    }

    /// <summary>
    /// L'IMPORT ne connaît pas le Draft IN : la commande le refuse, et l'édition de la facture
    /// import n'en dépend en rien.
    /// </summary>
    [Fact]
    public async Task Import_n_a_aucun_draft_in()
    {
        var c = await ConstruireAsync();
        using var _ = c.Db;

        var dossier = Dossier.Creer("DOS-IMP-DRAFT", c.SocieteId, DateTime.UtcNow, c.AuteurId, TypeClient.Normal);
        c.Db.Dossiers.Add(dossier);
        var fileImport = FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, NomsFiles.Facturation);
        c.Db.Files.Add(fileImport);
        var ticketImport = Ticket.Creer(
            "TCK-IMP-DRAFT", SoumettreDemandeImportCommand.Module, "STANDARD",
            fileImport.Id, dossier.Id, DateTime.UtcNow, "BL-DRAFT-1");
        c.Db.Tickets.Add(ticketImport);
        c.Db.Habilitations.Add(HabilitationUtilisateur.Attribuer(c.AgentId, Permission.BillingExportFacturationEditer, fileImport.Id));
        c.Db.SaveChanges();

        await Assert.ThrowsAsync<InvalidOperationException>(() => c.EmettreDraftIn.ExecuterAsync(
            c.AgentId, ticketImport.Id, "DraftIN-001.pdf", "application/pdf", Fichier("draft"), 5, Regles));

        Assert.False(ticketImport.DraftInExportEmis);
    }

    /// <summary>
    /// RG-EXP-04 — le parcours IES ne passe pas par l'édition de facture de la plateforme : le
    /// Draft IN ne lui est donc jamais opposé. Le comportement est inchangé.
    /// </summary>
    [Fact]
    public async Task Parcours_ies_export_reste_conforme_a_RG_EXP_04()
    {
        var c = await ConstruireAsync(TypeClient.IES);
        using var _ = c.Db;
        var ticket = await TicketEnFacturationAsync(c);

        // Le ticket IES part en File IES, jamais en Facturation — la facture est produite hors
        // plateforme (RG-EXP-04). L'édition de facture reste refusée pour une raison de PROFIL,
        // pas pour absence de Draft IN.
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => c.EditerFacture.ExecuterAsync(
            c.AgentId, ticket.Id, "Facture-001.pdf", "application/pdf", Fichier("facture"), 7, Regles));

        Assert.IsNotType<DraftInRequisException>(exception);
        Assert.Contains("IES", exception.Message, StringComparison.Ordinal);
    }
}
