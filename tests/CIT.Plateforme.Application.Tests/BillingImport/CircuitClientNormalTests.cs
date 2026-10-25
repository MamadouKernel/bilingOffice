using System.Text;
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
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CIT.Plateforme.Application.Tests.BillingImport;

/// <summary>
/// [TEST-SPEC] Exigence CDC: IMP-10..14 → US5 → critères d'acceptation (FR-016..FR-020) → Test.
/// Circuit Client Normal complet : Manifeste → Facturation → Validation Paiement → Contrôle
/// Documentaire → Clôture. CLAR-PAIEMENT-01 : une preuve refusée renvoie le client vers un nouveau
/// dépôt, jamais un rejet définitif.
/// </summary>
public sealed class CircuitClientNormalTests
{
    private static readonly ReglesDepotDocument Regles = new(["pdf", "jpg"], TailleMaxOctets: 1024);

    private sealed record Contexte(
        PlateformeDbContext DbContext,
        SoumettreDemandeImportCommand Soumettre,
        ValiderTicketImportCommand Valider,
        DemanderFactureCommand DemanderFacture,
        EditerFactureCommand EditerFacture,
        DeposerPreuvePaiementCommand DeposerPreuve,
        ValiderPreuvePaiementCommand ValiderPreuve,
        DeposerDocumentsControleEtCloturerCommand ControleEtCloturer,
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
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var deposerDocument = new DeposerDocumentCommand(
            new DocumentRepository(dbContext), ticketRepository, new StockageDocumentsFichiers(configuration, new EnvironnementDeTest("Development")),
            new AucuneAnalyseAntivirusConfiguree(), autorisation, cloisonnement, journalisation, horloge, uniteDeTravail);

        dbContext.Files.AddRange(
            FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, SoumettreDemandeImportCommand.FilePiloteManifeste),
            FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, NomsFiles.Facturation),
            FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, NomsFiles.ValidationPaiement),
            FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, NomsFiles.ControleDocumentaire),
            FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, NomsFiles.FileIes));

        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        var auteurId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, auteurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        dbContext.Habilitations.AddRange(
            HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentId), Permission.BillingImportManifesteInstruire),
            HabilitationUtilisateur.Attribuer(agentId, Permission.BillingImportFacturationEditer),
            HabilitationUtilisateur.Attribuer(agentId, Permission.BillingImportPaiementValider),
            HabilitationUtilisateur.Attribuer(agentId, Permission.BillingImportControleDocumentaireTraiter),
            HabilitationUtilisateur.Attribuer(agentId, Permission.DocumentDeposer));
        await dbContext.SaveChangesAsync();

        var soumettre = new SoumettreDemandeImportCommand(
            ticketRepository, dossierRepository, new SocieteClienteRepository(dbContext),
            new UtilisateurSocieteRepository(dbContext), fileRepository, numerotation, journalisation, horloge, uniteDeTravail);
        var valider = new ValiderTicketImportCommand(
            ticketRepository, dossierRepository, fileRepository, new SuiviBrancheIesRepository(dbContext),
            autorisation, journalisation, horloge, uniteDeTravail);
        var demanderFacture = new DemanderFactureCommand(ticketRepository, cloisonnement, journalisation, uniteDeTravail);
        var editerFacture = new EditerFactureCommand(
            ticketRepository, dossierRepository, fileRepository, deposerDocument, autorisation, journalisation, horloge, uniteDeTravail);
        var deposerPreuve = new DeposerPreuvePaiementCommand(deposerDocument, ticketRepository, journalisation, horloge, uniteDeTravail);
        var validerPreuve = new ValiderPreuvePaiementCommand(ticketRepository, fileRepository, autorisation, journalisation, horloge, uniteDeTravail);
        var controleEtCloturer = new DeposerDocumentsControleEtCloturerCommand(
            ticketRepository, deposerDocument, autorisation, journalisation, horloge, uniteDeTravail);

        return new Contexte(
            dbContext, soumettre, valider, demanderFacture, editerFacture, deposerPreuve, validerPreuve, controleEtCloturer,
            agentId, societe.Id, auteurId);
    }

    private static MemoryStream Contenu(string texte = "contenu-simule") => new(Encoding.UTF8.GetBytes(texte));

    private static async Task<Guid> CreerEtValiderTicketAsync(Contexte c, string numeroBl)
    {
        var soumission = await c.Soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(c.SocieteId, c.AuteurId, [numeroBl]));
        var ticketId = soumission.TicketsCrees[0].Id;
        await PriseEnChargeDeTest.PrendreAsync(c.DbContext, ticketId, c.AgentId);
        await c.Valider.ExecuterAsync(c.AgentId, ticketId);
        return ticketId;
    }

    [Fact]
    public async Task Demander_facture_trace_le_signal_client_sans_changer_de_file()
    {
        var c = await ConstruireContexteAsync();
        var ticketId = await CreerEtValiderTicketAsync(c, "BL-N1");
        var fileAvant = (await c.DbContext.Tickets.FindAsync(ticketId))!.FileCouranteId;

        await c.DemanderFacture.ExecuterAsync(c.AuteurId, ticketId);

        var ticket = await c.DbContext.Tickets.FindAsync(ticketId);
        Assert.Equal(fileAvant, ticket!.FileCouranteId);
        Assert.Contains(c.DbContext.HistoriqueTickets, e => e.TicketId == ticketId && e.Evenement == "FactureDemandee");
    }

    [Fact]
    public async Task Editer_facture_depose_le_document_et_transfere_vers_validation_paiement()
    {
        var c = await ConstruireContexteAsync();
        var ticketId = await CreerEtValiderTicketAsync(c, "BL-N2");
        using var contenu = Contenu();

        await ProformaDeTest.ValiderAsync(c.DbContext, ticketId, c.AgentId);
        await c.EditerFacture.ExecuterAsync(c.AgentId, ticketId, "facture.pdf", "application/pdf", contenu, contenu.Length, Regles);

        var ticket = await c.DbContext.Tickets.FindAsync(ticketId);
        var file = await c.DbContext.Files.FindAsync(ticket!.FileCouranteId);
        Assert.Equal(NomsFiles.ValidationPaiement, file!.Nom);
        Assert.Contains(c.DbContext.Documents, d => d.TicketId == ticketId && d.NomFichier == "facture.pdf");
    }

    [Fact]
    public async Task Deposer_preuve_de_paiement_puis_valider_transfere_vers_controle_documentaire()
    {
        var c = await ConstruireContexteAsync();
        var ticketId = await CreerEtValiderTicketAsync(c, "BL-N3");
        using (var facture = Contenu("facture"))
        {
            await ProformaDeTest.ValiderAsync(c.DbContext, ticketId, c.AgentId);
            await c.EditerFacture.ExecuterAsync(c.AgentId, ticketId, "facture.pdf", "application/pdf", facture, facture.Length, Regles);
        }
        using (var preuve = Contenu("preuve"))
        {
            await c.DeposerPreuve.ExecuterAsync(c.AuteurId, ticketId, "preuve.pdf", "application/pdf", preuve, preuve.Length, Regles);
        }

        await PriseEnChargeDeTest.PrendreAsync(c.DbContext, ticketId, c.AgentId);
        await c.ValiderPreuve.ValiderAsync(c.AgentId, ticketId);

        var ticket = await c.DbContext.Tickets.FindAsync(ticketId);
        var file = await c.DbContext.Files.FindAsync(ticket!.FileCouranteId);
        Assert.Equal(NomsFiles.ControleDocumentaire, file!.Nom);
    }

    [Fact]
    public async Task Preuve_de_paiement_refusee_ramene_le_client_vers_un_nouveau_depot_sans_rejet_definitif()
    {
        var c = await ConstruireContexteAsync();
        var ticketId = await CreerEtValiderTicketAsync(c, "BL-N4");
        using (var facture = Contenu("facture"))
        {
            await ProformaDeTest.ValiderAsync(c.DbContext, ticketId, c.AgentId);
            await c.EditerFacture.ExecuterAsync(c.AgentId, ticketId, "facture.pdf", "application/pdf", facture, facture.Length, Regles);
        }
        using (var preuveInvalide = Contenu("preuve-invalide"))
        {
            await c.DeposerPreuve.ExecuterAsync(c.AuteurId, ticketId, "preuve1.pdf", "application/pdf", preuveInvalide, preuveInvalide.Length, Regles);
        }

        await PriseEnChargeDeTest.PrendreAsync(c.DbContext, ticketId, c.AgentId);
        await c.ValiderPreuve.RefuserAsync(c.AgentId, ticketId, "Montant incorrect");

        var ticketApresRefus = await c.DbContext.Tickets.FindAsync(ticketId);
        Assert.Equal(StatutTicket.EnAttenteClient, ticketApresRefus!.Statut);
        Assert.NotEqual(StatutTicket.Annule, ticketApresRefus.Statut);
        Assert.Contains(c.DbContext.JournalEvenements, e => e.TypeEvenement == "PreuvePaiementRefusee" && e.ChargeUtile.Contains(ticketId.ToString()));

        // Le client peut déposer une nouvelle preuve — jamais bloqué par un rejet définitif.
        using var preuveCorrigee = Contenu("preuve-corrigee");
        await c.DeposerPreuve.ExecuterAsync(c.AuteurId, ticketId, "preuve2.pdf", "application/pdf", preuveCorrigee, preuveCorrigee.Length, Regles);
        Assert.Contains(c.DbContext.Documents, d => d.TicketId == ticketId && d.NomFichier == "preuve2.pdf");
    }

    [Fact]
    public async Task Depot_bs_badt_et_cloture_termine_le_ticket()
    {
        var c = await ConstruireContexteAsync();
        var ticketId = await CreerEtValiderTicketAsync(c, "BL-N5");
        using (var facture = Contenu("facture"))
        {
            await ProformaDeTest.ValiderAsync(c.DbContext, ticketId, c.AgentId);
            await c.EditerFacture.ExecuterAsync(c.AgentId, ticketId, "facture.pdf", "application/pdf", facture, facture.Length, Regles);
        }
        using (var preuve = Contenu("preuve"))
        {
            await c.DeposerPreuve.ExecuterAsync(c.AuteurId, ticketId, "preuve.pdf", "application/pdf", preuve, preuve.Length, Regles);
        }
        await PriseEnChargeDeTest.PrendreAsync(c.DbContext, ticketId, c.AgentId);
        await c.ValiderPreuve.ValiderAsync(c.AgentId, ticketId);

        using var badt = Contenu("badt");
        await PriseEnChargeDeTest.PrendreAsync(c.DbContext, ticketId, c.AgentId);
        await c.ControleEtCloturer.ExecuterAsync(c.AgentId, ticketId, "badt.pdf", "application/pdf", badt, badt.Length, Regles);

        var ticket = await c.DbContext.Tickets.FindAsync(ticketId);
        Assert.Equal(StatutTicket.Cloture, ticket!.Statut);
        Assert.Equal(MotifClotureType.Normale, ticket.MotifCloture);

        // Le client est notifié à CHAQUE étape du parcours (05/10/2026).
        var evenements = c.DbContext.JournalEvenements.Where(e => e.ChargeUtile.Contains(ticketId.ToString())).Select(e => e.TypeEvenement).ToList();
        Assert.All(["TicketCree", "TicketValide", "FactureEditee", "PaiementConfirme", "TicketCloture"], t => Assert.Contains(t, evenements));
    }

    /// <summary>IMP-12 — le client paie hors portail puis joint sa preuve au ticket : la pièce est
    /// rattachée au ticket au nom du client, et le ticket attend la validation de la Caisse (il ne
    /// passe pas seul au Contrôle documentaire).</summary>
    [Fact]
    public async Task IMP12_le_client_joint_sa_preuve_de_paiement_qui_attend_la_validation_caisse()
    {
        var c = await ConstruireContexteAsync();
        var ticketId = await CreerEtValiderTicketAsync(c, "BL-IMP12");
        using (var facture = Contenu("facture"))
        {
            await ProformaDeTest.ValiderAsync(c.DbContext, ticketId, c.AgentId);
            await c.EditerFacture.ExecuterAsync(c.AgentId, ticketId, "facture.pdf", "application/pdf", facture, facture.Length, Regles);
        }

        using var preuve = Contenu("preuve-virement");
        await c.DeposerPreuve.ExecuterAsync(c.AuteurId, ticketId, "preuve-virement.pdf", "application/pdf", preuve, preuve.Length, Regles);

        var piece = Assert.Single(c.DbContext.Documents, d => d.TicketId == ticketId && d.NomFichier == "preuve-virement.pdf");
        Assert.Equal(c.AuteurId, piece.DeposeParId);
        var ticket = await c.DbContext.Tickets.FindAsync(ticketId);
        var file = await c.DbContext.Files.FindAsync(ticket!.FileCouranteId);
        Assert.Equal(NomsFiles.ValidationPaiement, file!.Nom);
        Assert.NotEqual(StatutTicket.Cloture, ticket.Statut);
    }

    /// <summary>IMP-14 — le Pilote Contrôle documentaire dépose les documents de sortie (BS, BADT) :
    /// ils sont rattachés au ticket au nom de l'agent, le ticket est clôturé et le client retrouve
    /// dans son ticket l'ensemble des pièces (facture, preuve, documents de sortie).</summary>
    [Fact]
    public async Task IMP14_le_controle_documentaire_depose_les_documents_de_sortie_et_cloture()
    {
        var c = await ConstruireContexteAsync();
        var ticketId = await CreerEtValiderTicketAsync(c, "BL-IMP14");
        using (var facture = Contenu("facture"))
        {
            await ProformaDeTest.ValiderAsync(c.DbContext, ticketId, c.AgentId);
            await c.EditerFacture.ExecuterAsync(c.AgentId, ticketId, "facture.pdf", "application/pdf", facture, facture.Length, Regles);
        }
        using (var preuve = Contenu("preuve"))
        {
            await c.DeposerPreuve.ExecuterAsync(c.AuteurId, ticketId, "preuve.pdf", "application/pdf", preuve, preuve.Length, Regles);
        }
        await PriseEnChargeDeTest.PrendreAsync(c.DbContext, ticketId, c.AgentId);
        await c.ValiderPreuve.ValiderAsync(c.AgentId, ticketId);

        using var sortie = Contenu("bs-badt");
        await PriseEnChargeDeTest.PrendreAsync(c.DbContext, ticketId, c.AgentId);
        await c.ControleEtCloturer.ExecuterAsync(c.AgentId, ticketId, "bon-de-sortie-badt.pdf", "application/pdf", sortie, sortie.Length, Regles);

        var ticket = await c.DbContext.Tickets.FindAsync(ticketId);
        Assert.Equal(StatutTicket.Cloture, ticket!.Statut);
        var pieceSortie = Assert.Single(c.DbContext.Documents, d => d.TicketId == ticketId && d.NomFichier == "bon-de-sortie-badt.pdf");
        Assert.Equal(c.AgentId, pieceSortie.DeposeParId);
        var pieces = c.DbContext.Documents.Where(d => d.TicketId == ticketId).Select(d => d.NomFichier).ToList();
        Assert.Contains("facture.pdf", pieces);
        Assert.Contains("preuve.pdf", pieces);
    }

    [Fact]
    public async Task Chaine_complete_du_circuit_normal_de_bout_en_bout()
    {
        var c = await ConstruireContexteAsync();
        var ticketId = await CreerEtValiderTicketAsync(c, "BL-N6");

        await c.DemanderFacture.ExecuterAsync(c.AuteurId, ticketId);
        using (var facture = Contenu("facture"))
        {
            await ProformaDeTest.ValiderAsync(c.DbContext, ticketId, c.AgentId);
            await c.EditerFacture.ExecuterAsync(c.AgentId, ticketId, "facture.pdf", "application/pdf", facture, facture.Length, Regles);
        }
        using (var preuveInvalide = Contenu("preuve-invalide"))
        {
            await c.DeposerPreuve.ExecuterAsync(c.AuteurId, ticketId, "preuve1.pdf", "application/pdf", preuveInvalide, preuveInvalide.Length, Regles);
        }
        await PriseEnChargeDeTest.PrendreAsync(c.DbContext, ticketId, c.AgentId);
        await c.ValiderPreuve.RefuserAsync(c.AgentId, ticketId, "Montant incorrect");
        using (var preuveCorrigee = Contenu("preuve-corrigee"))
        {
            await c.DeposerPreuve.ExecuterAsync(c.AuteurId, ticketId, "preuve2.pdf", "application/pdf", preuveCorrigee, preuveCorrigee.Length, Regles);
        }
        await PriseEnChargeDeTest.PrendreAsync(c.DbContext, ticketId, c.AgentId);
        await c.ValiderPreuve.ValiderAsync(c.AgentId, ticketId);
        using var badt = Contenu("badt");
        await PriseEnChargeDeTest.PrendreAsync(c.DbContext, ticketId, c.AgentId);
        await c.ControleEtCloturer.ExecuterAsync(c.AgentId, ticketId, "badt.pdf", "application/pdf", badt, badt.Length, Regles);

        var ticket = await c.DbContext.Tickets.FindAsync(ticketId);
        Assert.Equal(StatutTicket.Cloture, ticket!.Statut);
        // facture + preuve1 (refusée) + preuve2 (corrigée) + BADT.
        Assert.Equal(4, c.DbContext.Documents.Count(d => d.TicketId == ticketId));
    }
}
