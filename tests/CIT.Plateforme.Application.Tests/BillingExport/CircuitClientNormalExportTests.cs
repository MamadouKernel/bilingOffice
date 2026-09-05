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
/// [TEST-SPEC] EXP-03 / Contrats C5-01, C5-03, C5-05, C5-06, C5-07.
/// Circuit Client Normal Export : Back Office -> Facturation -> Validation Paiement -> Contrôle Documentaire (BEE) -> Clôture.
/// </summary>
public sealed class CircuitClientNormalExportTests
{
    private static readonly ReglesDepotDocument Regles = new(["pdf", "jpg"], TailleMaxOctets: 1024);

    private sealed record Contexte(
        PlateformeDbContext DbContext,
        SoumettreDemandeExportCommand Soumettre,
        ValiderTicketExportCommand Valider,
        EditerFactureExportCommand EditerFacture,
        DeposerPreuvePaiementExportCommand DeposerPreuve,
        ValiderPreuvePaiementExportCommand ValiderPreuve,
        FinaliserControleDocumentaireExportCommand FinaliserControle,
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
            FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, NomsFilesExport.BackOfficeExport),
            FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, NomsFilesExport.Facturation),
            FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, NomsFilesExport.ValidationPaiement),
            FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, NomsFilesExport.ControleDocumentaire));

        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "SocieteExportNormal", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        var auteurId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, auteurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        dbContext.Habilitations.AddRange(
            HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentId), Permission.BillingExportBackOfficeInstruire),
            HabilitationUtilisateur.Attribuer(agentId, Permission.BillingExportFacturationEditer),
            HabilitationUtilisateur.Attribuer(agentId, Permission.BillingExportPaiementValider),
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
        var deposerPreuve = new DeposerPreuvePaiementExportCommand(deposerDocument);
        var validerPreuve = new ValiderPreuvePaiementExportCommand(
            ticketRepository, fileRepository, autorisation, journalisation, horloge, uniteDeTravail);
        var finaliserControle = new FinaliserControleDocumentaireExportCommand(
            ticketRepository, deposerDocument, autorisation, journalisation, horloge, uniteDeTravail);

        return new Contexte(dbContext, soumettre, valider, editerFacture, deposerPreuve, validerPreuve, finaliserControle, agentId, societe.Id, auteurId);
    }

    [Fact]
    public async Task Parcours_complet_client_normal_export_jusqu_a_cloture_avec_bee()
    {
        var c = await ConstruireContexteAsync();
        var soumission = await c.Soumettre.ExecuterAsync(new SoumettreDemandeExportRequete(c.SocieteId, c.AuteurId, ["BKG-NORM-01"]));
        var ticket = soumission.TicketsCrees[0];

        // 1. Back Office valide -> File Facturation
        await c.Valider.ExecuterAsync(c.AgentId, ticket.Id);

        // 2. Facturation émet la facture -> File Validation Paiement
        var factureContenu = Encoding.UTF8.GetBytes("Facture EXPORT FCT-2026-001");
        await c.EditerFacture.ExecuterAsync(c.AgentId, ticket.Id, "Facture-001.pdf", "application/pdf", new MemoryStream(factureContenu), factureContenu.Length, Regles);

        // 3. Client dépose la preuve de paiement
        var preuveContenu = Encoding.UTF8.GetBytes("Preuve Virement");
        await c.DeposerPreuve.ExecuterAsync(c.AuteurId, ticket.Id, "Preuve.pdf", "application/pdf", new MemoryStream(preuveContenu), preuveContenu.Length, Regles);

        // 4. Validation du paiement -> File Contrôle Documentaire
        await c.ValiderPreuve.ValiderAsync(c.AgentId, ticket.Id);

        // 5. Contrôle Documentaire dépose le BEE (Bordereau d'Enlèvement Export) et clôture
        var beeContenu = Encoding.UTF8.GetBytes("Bordereau d'Enlèvement Export");
        await c.FinaliserControle.ExecuterAsync(c.AgentId, ticket.Id, "BEE-2026-001.pdf", "application/pdf", new MemoryStream(beeContenu), beeContenu.Length, Regles);

        var ticketCloture = await new TicketRepository(c.DbContext).ObtenirParIdAsync(ticket.Id);
        Assert.NotNull(ticketCloture);
        Assert.Equal(StatutTicket.Cloture, ticketCloture.Statut);
        Assert.Equal(MotifClotureType.Normale, ticketCloture.MotifCloture);
    }
}
