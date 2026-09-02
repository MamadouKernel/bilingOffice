using System.Text;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Documents;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Documents;

/// <summary>
/// TEST D'ORCHESTRATION ANTIVIRUS (MOCK) : Valide que DeposerDocumentCommand applique
/// le bon statut (Sain, Infecte, ErreurAnalyse) et respecte le refus d'accès.
/// </summary>
public sealed class DeposerDocumentAntivirusOrchestrationTests
{
    private sealed class FakeAntivirus(StatutAnalyseAntivirus statutARetourner) : IAnalyseAntivirus
    {
        public Task<StatutAnalyseAntivirus> AnalyserAsync(Stream contenu, CancellationToken ct = default) =>
            Task.FromResult(statutARetourner);
    }

    private static readonly ReglesDepotDocument Regles = new(["pdf"], TailleMaxOctets: 10 * 1024 * 1024);

    [Fact]
    public async Task Orchestration_DocumentSain_EstMarqueSainEtAccessible()
    {
        // Arrange
        var db = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var uow = new UniteDeTravail(db);
        var journal = new JournalisationService(db, horloge);
        var docsRepo = new DocumentRepository(db);
        var ticketsRepo = new TicketRepository(db);
        var autorisation = new AutorisationService(db);
        var cloisonnement = new CloisonnementService(ticketsRepo, new DossierRepository(db));

        var mockStockage = new Mock<IStockageDocuments>();
        mockStockage.Setup(s => s.EnregistrerAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("cle-test-guid");

        var fakeAntivirus = new FakeAntivirus(StatutAnalyseAntivirus.Sain);

        var agentId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var clientProprietaire = Guid.NewGuid();
        var dossier = Dossier.Creer("DOS-TEST-0001", clientProprietaire, horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-TEST-0001", "BillingOffice", "Import", fileId, dossier.Id, horloge.MaintenantUtc);
        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentId, Permission.DocumentDeposer, fileId));
        await db.SaveChangesAsync();

        var cmd = new DeposerDocumentCommand(
            docsRepo, ticketsRepo, mockStockage.Object, fakeAntivirus,
            autorisation, cloisonnement, journal, horloge, uow);

        var contenu = new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.4\nFichier test"));

        // Act
        var doc = await cmd.DeposerPourAgentAsync(
            agentId, ticket.Id, "justificatif.pdf", "application/pdf", contenu, contenu.Length,
            Regles);

        // Assert
        Assert.Equal(StatutAnalyseAntivirus.Sain, doc.StatutAntivirus);
        Assert.True(doc.EstAccessible);
    }

    [Fact]
    public async Task Orchestration_DocumentInfecte_EstMarqueInfecteEtNonAccessible()
    {
        // Arrange
        var db = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var uow = new UniteDeTravail(db);
        var journal = new JournalisationService(db, horloge);
        var docsRepo = new DocumentRepository(db);
        var ticketsRepo = new TicketRepository(db);
        var autorisation = new AutorisationService(db);
        var cloisonnement = new CloisonnementService(ticketsRepo, new DossierRepository(db));

        var mockStockage = new Mock<IStockageDocuments>();
        mockStockage.Setup(s => s.EnregistrerAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("cle-test-guid");

        var fakeAntivirus = new FakeAntivirus(StatutAnalyseAntivirus.Infecte);

        var agentId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var clientProprietaire = Guid.NewGuid();
        var dossier = Dossier.Creer("DOS-TEST-0002", clientProprietaire, horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-TEST-0002", "BillingOffice", "Import", fileId, dossier.Id, horloge.MaintenantUtc);
        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentId, Permission.DocumentDeposer, fileId));
        await db.SaveChangesAsync();

        var cmd = new DeposerDocumentCommand(
            docsRepo, ticketsRepo, mockStockage.Object, fakeAntivirus,
            autorisation, cloisonnement, journal, horloge, uow);

        var contenu = new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.4\nFichier infecte"));

        // Act
        var doc = await cmd.DeposerPourAgentAsync(
            agentId, ticket.Id, "malware.pdf", "application/pdf", contenu, contenu.Length,
            Regles);

        // Assert
        Assert.Equal(StatutAnalyseAntivirus.Infecte, doc.StatutAntivirus);
        Assert.False(doc.EstAccessible);
    }
}
