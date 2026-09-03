using System.Text;
using CIT.Plateforme.Application.Documents;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Documents;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Enrolement;

// FEATURE 008 (Enrôlement Transporteurs) — T022. C8-06 : preuve que DeposerDocumentCommand
// (F001, générique) fonctionne SANS AUCUNE MODIFICATION pour un ticket Module="Enrolement",
// exactement comme pour tout autre module (research.md D7).
public sealed class DepotPieceEnrolementIntegrationTests
{
    private static readonly ReglesDepotDocument Regles = new(["pdf"], TailleMaxOctets: 1024);

    private static MemoryStream ContenuDeTest() => new(Encoding.UTF8.GetBytes("piece-justificative-simulee"));

    [Fact]
    public async Task Transporteur_proprietaire_depose_une_piece_sur_son_ticket_enrolement()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var stockage = new StockageDocumentsFichiers(configuration, new EnvironnementDeTest("Development"));
        var antivirus = new AucuneAnalyseAntivirusConfiguree();
        var ticketRepo = new TicketRepository(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var cloisonnement = new CloisonnementService(ticketRepo, new DossierRepository(dbContext));

        var transporteurId = Guid.NewGuid();
        var dossier = Dossier.Creer("DOS-ENR-000001", transporteurId, horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-ENR-000001", "Enrolement", "Enrolement", Guid.NewGuid(), dossier.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();

        var commande = new DeposerDocumentCommand(
            new DocumentRepository(dbContext), ticketRepo, stockage, antivirus, autorisation, cloisonnement, journalisation, horloge, uniteDeTravail);

        using var contenu = ContenuDeTest();
        var document = await commande.DeposerPourClientAsync(transporteurId, ticket.Id, "permis.pdf", "application/pdf", contenu, contenu.Length, Regles);

        Assert.Equal("permis.pdf", document.NomFichier);

        var documentRepo = new DocumentRepository(dbContext);
        var piecesDuTicket = await documentRepo.ListerParTicketAsync(ticket.Id);
        Assert.Single(piecesDuTicket);
    }

    [Fact]
    public async Task Transporteur_non_proprietaire_est_refuse()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var stockage = new StockageDocumentsFichiers(configuration, new EnvironnementDeTest("Development"));
        var antivirus = new AucuneAnalyseAntivirusConfiguree();
        var ticketRepo = new TicketRepository(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var cloisonnement = new CloisonnementService(ticketRepo, new DossierRepository(dbContext));

        var transporteurProprietaire = Guid.NewGuid();
        var transporteurIntrus = Guid.NewGuid();
        var dossier = Dossier.Creer("DOS-ENR-000002", transporteurProprietaire, horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-ENR-000002", "Enrolement", "Enrolement", Guid.NewGuid(), dossier.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();

        var commande = new DeposerDocumentCommand(
            new DocumentRepository(dbContext), ticketRepo, stockage, antivirus, autorisation, cloisonnement, journalisation, horloge, uniteDeTravail);

        using var contenu = ContenuDeTest();
        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            commande.DeposerPourClientAsync(transporteurIntrus, ticket.Id, "permis.pdf", "application/pdf", contenu, contenu.Length, Regles));
    }
}
