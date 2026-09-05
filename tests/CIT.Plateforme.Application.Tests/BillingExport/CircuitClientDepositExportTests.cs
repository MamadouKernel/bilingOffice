using System.Text;
using CIT.Plateforme.Application.BillingExport;
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

namespace CIT.Plateforme.Application.Tests.BillingExport;

/// <summary>
/// [TEST-SPEC] EXP-03 / Circuit Client Deposit : Facturation -> Comptabilité Client (Lettrage) -> Contrôle Documentaire (BEE) -> Clôture.
/// </summary>
public sealed class CircuitClientDepositExportTests
{
    private static readonly ReglesDepotDocument Regles = new(["pdf", "jpg"], TailleMaxOctets: 1024);

    [Fact]
    public async Task Parcours_complet_client_deposit_export_avec_lettrage_comptabilite()
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
            FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, NomsFilesExport.BackOfficeExport),
            FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, NomsFilesExport.Facturation),
            FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, NomsFilesExport.ComptabiliteClient),
            FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, NomsFilesExport.ControleDocumentaire));

        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "SocieteExportDeposit", TypeClient.Deposit, DateTime.UtcNow);
        societe.Valider();
        var auteurId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, auteurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        dbContext.Habilitations.AddRange(
            HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentId), Permission.BillingExportBackOfficeInstruire),
            HabilitationUtilisateur.Attribuer(agentId, Permission.BillingExportFacturationEditer),
            HabilitationUtilisateur.Attribuer(agentId, Permission.BillingExportComptabiliteConfirmerLettrage),
            HabilitationUtilisateur.Attribuer(agentId, Permission.BillingExportControleDocumentaireTraiter),
            HabilitationUtilisateur.Attribuer(agentId, Permission.DocumentDeposer));
        await dbContext.SaveChangesAsync();

        var soumettre = new SoumettreDemandeExportCommand(
            ticketRepository, dossierRepository, new SocieteClienteRepository(dbContext),
            new UtilisateurSocieteRepository(dbContext), fileRepository, numerotation, journalisation, horloge, uniteDeTravail);
        var valider = new ValiderTicketExportCommand(
            ticketRepository, dossierRepository, fileRepository, new SuiviBrancheIesRepository(dbContext),
            autorisation, journalisation, horloge, uniteDeTravail);
        var editerFacture = new EditerFactureExportCommand(
            ticketRepository, dossierRepository, fileRepository, deposerDocument, autorisation, journalisation, horloge, uniteDeTravail);
        var confirmerLettrage = new ConfirmerLettrageExportCommand(
            ticketRepository, fileRepository, autorisation, journalisation, horloge, uniteDeTravail);
        var finaliserControle = new FinaliserControleDocumentaireExportCommand(
            ticketRepository, deposerDocument, autorisation, journalisation, horloge, uniteDeTravail);

        var soumission = await soumettre.ExecuterAsync(new SoumettreDemandeExportRequete(societe.Id, auteurId, ["BKG-DEP-01"]));
        var ticket = soumission.TicketsCrees[0];

        // 1. Back Office -> Facturation
        await valider.ExecuterAsync(agentId, ticket.Id);

        // 2. Facturation -> Comptabilité Client
        var factureContenu = Encoding.UTF8.GetBytes("Facture Export Deposit");
        await editerFacture.ExecuterAsync(agentId, ticket.Id, "Facture-001.pdf", "application/pdf", new MemoryStream(factureContenu), factureContenu.Length, Regles);

        var ticketApresFacture = await ticketRepository.ObtenirParIdAsync(ticket.Id);
        var fileCompta = dbContext.Files.Single(f => f.Nom == NomsFilesExport.ComptabiliteClient);
        Assert.NotNull(ticketApresFacture);
        Assert.Equal(fileCompta.Id, ticketApresFacture.FileCouranteId);

        // 3. Comptabilité confirme le lettrage -> Contrôle Documentaire
        await confirmerLettrage.ExecuterAsync(agentId, ticket.Id);

        var ticketApresCompta = await ticketRepository.ObtenirParIdAsync(ticket.Id);
        var fileControle = dbContext.Files.Single(f => f.Nom == NomsFilesExport.ControleDocumentaire);
        Assert.NotNull(ticketApresCompta);
        Assert.Equal(fileControle.Id, ticketApresCompta.FileCouranteId);

        // 4. Contrôle Documentaire dépose le BEE et clôture
        var beeContenu = Encoding.UTF8.GetBytes("BEE Export Deposit");
        await finaliserControle.ExecuterAsync(agentId, ticket.Id, "BEE-001.pdf", "application/pdf", new MemoryStream(beeContenu), beeContenu.Length, Regles);

        var ticketCloture = await ticketRepository.ObtenirParIdAsync(ticket.Id);
        Assert.NotNull(ticketCloture);
        Assert.Equal(StatutTicket.Cloture, ticketCloture.Statut);
    }
}
