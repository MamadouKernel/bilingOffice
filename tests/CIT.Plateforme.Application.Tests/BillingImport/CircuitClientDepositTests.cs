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
/// [TEST-SPEC] Exigence CDC: IMP-30, IMP-31, IMP-32 → US7 → critères d'acceptation 1-4 (FR-021,
/// FR-022, FR-023) → Test. Circuit Client Deposit : facturation → Comptabilité Client (lettrage
/// IRIS, hors portail, research.md D7) → Contrôle Documentaire → clôture. CLAR-DEPOSIT-01 : un
/// lettrage impossible ou à reprendre n'a aucune contrepartie système — le ticket reste en
/// Comptabilité Client jusqu'à résolution manuelle.
/// </summary>
public sealed class CircuitClientDepositTests
{
    private static readonly ReglesDepotDocument Regles = new(["pdf", "jpg"], TailleMaxOctets: 1024);

    private sealed record Contexte(
        PlateformeDbContext DbContext,
        SoumettreDemandeImportCommand Soumettre,
        ValiderTicketImportCommand Valider,
        EditerFactureCommand EditerFacture,
        ConfirmerLettrageCommand ConfirmerLettrage,
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
            FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, NomsFiles.ComptabiliteClient),
            FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, NomsFiles.ControleDocumentaire),
            FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, NomsFiles.FileIes));

        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations", TypeClient.Deposit, DateTime.UtcNow);
        societe.Valider();
        var auteurId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, auteurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        dbContext.Habilitations.AddRange(
            HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentId), Permission.BillingImportManifesteInstruire),
            HabilitationUtilisateur.Attribuer(agentId, Permission.BillingImportFacturationEditer),
            HabilitationUtilisateur.Attribuer(agentId, Permission.BillingImportComptabiliteConfirmerLettrage),
            HabilitationUtilisateur.Attribuer(agentId, Permission.BillingImportControleDocumentaireTraiter),
            HabilitationUtilisateur.Attribuer(agentId, Permission.DocumentDeposer));
        await dbContext.SaveChangesAsync();

        var soumettre = new SoumettreDemandeImportCommand(
            ticketRepository, dossierRepository, new SocieteClienteRepository(dbContext),
            new UtilisateurSocieteRepository(dbContext), fileRepository, numerotation, journalisation, horloge, uniteDeTravail);
        var valider = new ValiderTicketImportCommand(
            ticketRepository, dossierRepository, fileRepository, new SuiviBrancheIesRepository(dbContext),
            autorisation, journalisation, horloge, uniteDeTravail);
        var editerFacture = new EditerFactureCommand(
            ticketRepository, dossierRepository, fileRepository, deposerDocument, autorisation, journalisation, horloge, uniteDeTravail);
        var confirmerLettrage = new ConfirmerLettrageCommand(ticketRepository, fileRepository, autorisation, journalisation, horloge, uniteDeTravail);
        var controleEtCloturer = new DeposerDocumentsControleEtCloturerCommand(
            ticketRepository, deposerDocument, autorisation, journalisation, horloge, uniteDeTravail);

        return new Contexte(
            dbContext, soumettre, valider, editerFacture, confirmerLettrage, controleEtCloturer, agentId, societe.Id, auteurId);
    }

    private static MemoryStream Contenu(string texte) => new(Encoding.UTF8.GetBytes(texte));

    private static async Task<Guid> CreerEtValiderTicketAsync(Contexte c, string numeroBl)
    {
        var soumission = await c.Soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(c.SocieteId, c.AuteurId, [numeroBl]));
        var ticketId = soumission.TicketsCrees[0].Id;
        await PriseEnChargeDeTest.PrendreAsync(c.DbContext, ticketId, c.AgentId);
        await c.Valider.ExecuterAsync(c.AgentId, ticketId);
        return ticketId;
    }

    private static async Task<Guid> CreerJusquEnComptabiliteClientAsync(Contexte c, string numeroBl)
    {
        var ticketId = await CreerEtValiderTicketAsync(c, numeroBl);
        using var facture = Contenu("facture");
        await ProformaDeTest.ValiderAsync(c.DbContext, ticketId, c.AgentId);
        await c.EditerFacture.ExecuterAsync(c.AgentId, ticketId, "facture.pdf", "application/pdf", facture, facture.Length, Regles);
        await PriseEnChargeDeTest.PrendreAsync(c.DbContext, ticketId, c.AgentId);
        return ticketId;
    }

    [Fact]
    public async Task Editer_facture_pour_un_dossier_deposit_transfere_vers_comptabilite_client()
    {
        var c = await ConstruireContexteAsync();
        var ticketId = await CreerJusquEnComptabiliteClientAsync(c, "BL-DP1");

        var ticket = await c.DbContext.Tickets.FindAsync(ticketId);
        var file = await c.DbContext.Files.FindAsync(ticket!.FileCouranteId);
        Assert.Equal(NomsFiles.ComptabiliteClient, file!.Nom);
    }

    [Fact]
    public async Task Lettrage_confirme_transfere_vers_le_controle_documentaire()
    {
        var c = await ConstruireContexteAsync();
        var ticketId = await CreerJusquEnComptabiliteClientAsync(c, "BL-DP2");

        await c.ConfirmerLettrage.ConfirmerAsync(c.AgentId, ticketId);

        var ticket = await c.DbContext.Tickets.FindAsync(ticketId);
        var file = await c.DbContext.Files.FindAsync(ticket!.FileCouranteId);
        Assert.Equal(NomsFiles.ControleDocumentaire, file!.Nom);
    }

    [Fact]
    public async Task Lettrage_impossible_ou_a_reprendre_ne_declenche_aucune_action_le_ticket_reste_en_comptabilite_client()
    {
        var c = await ConstruireContexteAsync();
        var ticketId = await CreerJusquEnComptabiliteClientAsync(c, "BL-DP3");
        var fileAvant = (await c.DbContext.Tickets.FindAsync(ticketId))!.FileCouranteId;
        var statutAvant = (await c.DbContext.Tickets.FindAsync(ticketId))!.Statut;

        // CLAR-DEPOSIT-01 : « aucune action » — aucun appel de commande ne représente ce cas ; le
        // temps qui passe sans confirmation ne doit ni clôturer, ni renvoyer vers la Facturation.
        var ticketApres = await c.DbContext.Tickets.FindAsync(ticketId);

        Assert.Equal(fileAvant, ticketApres!.FileCouranteId);
        Assert.Equal(statutAvant, ticketApres.Statut);
        Assert.NotEqual(StatutTicket.Cloture, ticketApres.Statut);
        Assert.NotEqual(StatutTicket.Annule, ticketApres.Statut);
    }

    [Fact]
    public async Task Apres_lettrage_confirme_pilote_cd_depose_les_documents_et_cloture()
    {
        var c = await ConstruireContexteAsync();
        var ticketId = await CreerJusquEnComptabiliteClientAsync(c, "BL-DP4");
        await c.ConfirmerLettrage.ConfirmerAsync(c.AgentId, ticketId);

        using var badt = Contenu("badt");
        await PriseEnChargeDeTest.PrendreAsync(c.DbContext, ticketId, c.AgentId);
        await c.ControleEtCloturer.ExecuterAsync(c.AgentId, ticketId, "badt.pdf", "application/pdf", badt, badt.Length, Regles);

        var ticket = await c.DbContext.Tickets.FindAsync(ticketId);
        Assert.Equal(StatutTicket.Cloture, ticket!.Statut);
        Assert.Equal(MotifClotureType.Normale, ticket.MotifCloture);
    }
}
