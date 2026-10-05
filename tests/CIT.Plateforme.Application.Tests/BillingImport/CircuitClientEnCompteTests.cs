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
/// [TEST-SPEC] Exigence CDC: IMP-20, IMP-21, IMP-22 → US6 → critères d'acceptation 1-2 (FR-018,
/// FR-019, FR-020) → Test. Circuit Client En Compte : facturation → Contrôle Documentaire direct
/// (research.md D6) — aucune étape de paiement, contrairement au Client Normal.
/// </summary>
public sealed class CircuitClientEnCompteTests
{
    private static readonly ReglesDepotDocument Regles = new(["pdf", "jpg"], TailleMaxOctets: 1024);

    private sealed record Contexte(
        PlateformeDbContext DbContext,
        SoumettreDemandeImportCommand Soumettre,
        ValiderTicketImportCommand Valider,
        EditerFactureCommand EditerFacture,
        DeposerDocumentsControleEtCloturerCommand ControleEtCloturer,
        ConsulterDocumentsTicketClotureQuery ConsulterDocuments,
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

        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations", TypeClient.EnCompte, DateTime.UtcNow);
        societe.Valider();
        var auteurId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, auteurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        dbContext.Habilitations.AddRange(
            HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentId), Permission.BillingImportManifesteInstruire),
            HabilitationUtilisateur.Attribuer(agentId, Permission.BillingImportFacturationEditer),
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
        var controleEtCloturer = new DeposerDocumentsControleEtCloturerCommand(
            ticketRepository, deposerDocument, autorisation, journalisation, horloge, uniteDeTravail);
        var consulterDocuments = new ConsulterDocumentsTicketClotureQuery(new DocumentRepository(dbContext), cloisonnement);

        return new Contexte(
            dbContext, soumettre, valider, editerFacture, controleEtCloturer, consulterDocuments, agentId, societe.Id, auteurId);
    }

    private static MemoryStream Contenu(string texte) => new(Encoding.UTF8.GetBytes(texte));

    private static async Task<Guid> CreerEtValiderTicketAsync(Contexte c, string numeroBl)
    {
        var soumission = await c.Soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(c.SocieteId, c.AuteurId, [numeroBl]));
        var ticketId = soumission.TicketsCrees[0].Id;
        await c.Valider.ExecuterAsync(c.AgentId, ticketId);
        return ticketId;
    }

    [Fact]
    public async Task Editer_facture_pour_un_dossier_en_compte_transfere_directement_au_controle_documentaire()
    {
        var c = await ConstruireContexteAsync();
        var ticketId = await CreerEtValiderTicketAsync(c, "BL-EC1");
        using var facture = Contenu("facture");

        await ProformaDeTest.ValiderAsync(c.DbContext, ticketId, c.AgentId);
        await c.EditerFacture.ExecuterAsync(c.AgentId, ticketId, "facture.pdf", "application/pdf", facture, facture.Length, Regles);

        var ticket = await c.DbContext.Tickets.FindAsync(ticketId);
        var file = await c.DbContext.Files.FindAsync(ticket!.FileCouranteId);
        Assert.Equal(NomsFiles.ControleDocumentaire, file!.Nom);
        // Jamais de passage par la file Validation Paiement (D6) — aucune étape de paiement En Compte.
        Assert.NotEqual(NomsFiles.ValidationPaiement, file.Nom);
    }

    [Fact]
    public async Task Ticket_en_compte_cloture_permet_au_client_de_recuperer_facture_et_pieces_de_controle()
    {
        var c = await ConstruireContexteAsync();
        var ticketId = await CreerEtValiderTicketAsync(c, "BL-EC2");
        using (var facture = Contenu("facture"))
        {
            await ProformaDeTest.ValiderAsync(c.DbContext, ticketId, c.AgentId);
            await c.EditerFacture.ExecuterAsync(c.AgentId, ticketId, "facture.pdf", "application/pdf", facture, facture.Length, Regles);
        }
        using (var badt = Contenu("badt"))
        {
            await c.ControleEtCloturer.ExecuterAsync(c.AgentId, ticketId, "badt.pdf", "application/pdf", badt, badt.Length, Regles);
        }

        var ticket = await c.DbContext.Tickets.FindAsync(ticketId);
        Assert.Equal(StatutTicket.Cloture, ticket!.Statut);

        var documents = await c.ConsulterDocuments.ExecuterAsync(c.AuteurId, ticketId);

        Assert.Equal(2, documents.Count);
        Assert.Contains(documents, d => d.NomFichier == "facture.pdf");
        Assert.Contains(documents, d => d.NomFichier == "badt.pdf");
    }

    [Fact]
    public async Task Un_tiers_non_proprietaire_ne_peut_pas_consulter_les_documents()
    {
        var c = await ConstruireContexteAsync();
        var ticketId = await CreerEtValiderTicketAsync(c, "BL-EC3");
        using (var facture = Contenu("facture"))
        {
            await ProformaDeTest.ValiderAsync(c.DbContext, ticketId, c.AgentId);
            await c.EditerFacture.ExecuterAsync(c.AgentId, ticketId, "facture.pdf", "application/pdf", facture, facture.Length, Regles);
        }
        using (var badt = Contenu("badt"))
        {
            await c.ControleEtCloturer.ExecuterAsync(c.AgentId, ticketId, "badt.pdf", "application/pdf", badt, badt.Length, Regles);
        }

        var tiersId = Guid.NewGuid();
        await Assert.ThrowsAsync<AccesRefuseException>(() => c.ConsulterDocuments.ExecuterAsync(tiersId, ticketId));
    }
}
