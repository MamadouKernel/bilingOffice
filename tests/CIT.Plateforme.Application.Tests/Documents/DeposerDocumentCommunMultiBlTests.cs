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
/// Écart 06/10 #1 — « document commun à plusieurs BL/bookings » : le client dépose UNE fois une
/// pièce et la rattache à plusieurs tickets de sa demande. Un seul upload physique (stockage +
/// antivirus une fois), autant de documents que de tickets sélectionnés, partageant la clé.
/// </summary>
public sealed class DeposerDocumentCommunMultiBlTests
{
    private sealed class FakeAntivirus(StatutAnalyseAntivirus statut) : IAnalyseAntivirus
    {
        public int Appels { get; private set; }
        public Task<StatutAnalyseAntivirus> AnalyserAsync(Stream contenu, CancellationToken ct = default)
        {
            Appels++;
            return Task.FromResult(statut);
        }
    }

    private static readonly ReglesDepotDocument Regles = new(["pdf"], TailleMaxOctets: 10 * 1024 * 1024);

    [Fact]
    public async Task Document_commun_est_rattache_a_tous_les_tickets_avec_un_seul_upload()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var uow = new UniteDeTravail(db);
        var journal = new JournalisationService(db, horloge);
        var docsRepo = new DocumentRepository(db);
        var ticketsRepo = new TicketRepository(db);
        var autorisation = new AutorisationService(db);
        var cloisonnement = new CloisonnementService(ticketsRepo, FabriqueRequetes.ControleAcces(db));

        var stockage = new Mock<IStockageDocuments>();
        stockage.Setup(s => s.EnregistrerAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("cle-commune");
        var antivirus = new FakeAntivirus(StatutAnalyseAntivirus.Sain);

        var clientId = Guid.NewGuid();
        var dossier = Dossier.Creer("DOS-COMMUN-0001",
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, clientId, NatureUtilisateur.Client), horloge.MaintenantUtc);
        var fileId = Guid.NewGuid();
        var ticketA = Ticket.Creer("TCK-COMMUN-A", "BillingImport", "Import", fileId, dossier.Id, horloge.MaintenantUtc, "BL-A");
        var ticketB = Ticket.Creer("TCK-COMMUN-B", "BillingImport", "Import", fileId, dossier.Id, horloge.MaintenantUtc, "BL-B");
        db.Dossiers.Add(dossier);
        db.Tickets.AddRange(ticketA, ticketB);
        await db.SaveChangesAsync();

        var cmd = new DeposerDocumentCommand(
            docsRepo, ticketsRepo, stockage.Object, antivirus, autorisation, cloisonnement, journal, horloge, uow);

        var contenu = new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.4\nDeclaration douane commune"));

        var docs = await cmd.DeposerDocumentCommunPourClientAsync(
            clientId, [ticketA.Id, ticketB.Id], "declaration.pdf", "application/pdf", contenu, contenu.Length,
            Regles, CategorieDocumentBillingImport.DeclarationDouane);

        Assert.Equal(2, docs.Count);
        Assert.Equal([ticketA.Id, ticketB.Id], docs.Select(d => d.TicketId).ToArray());
        Assert.All(docs, d => Assert.Equal("cle-commune", d.CleStockage));
        Assert.All(docs, d => Assert.Equal(StatutAnalyseAntivirus.Sain, d.StatutAntivirus));
        // Un seul upload physique, une seule analyse — malgré deux tickets.
        stockage.Verify(s => s.EnregistrerAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(1, antivirus.Appels);
    }

    [Fact]
    public async Task Refuse_si_un_ticket_n_appartient_pas_au_client()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var uow = new UniteDeTravail(db);
        var journal = new JournalisationService(db, horloge);
        var docsRepo = new DocumentRepository(db);
        var ticketsRepo = new TicketRepository(db);
        var autorisation = new AutorisationService(db);
        var cloisonnement = new CloisonnementService(ticketsRepo, FabriqueRequetes.ControleAcces(db));

        var stockage = new Mock<IStockageDocuments>();
        stockage.Setup(s => s.EnregistrerAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("cle-commune");

        var clientId = Guid.NewGuid();
        var autreClient = Guid.NewGuid();
        var dossierClient = Dossier.Creer("DOS-COMMUN-0002",
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, clientId, NatureUtilisateur.Client), horloge.MaintenantUtc);
        var dossierAutre = Dossier.Creer("DOS-COMMUN-0003",
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, autreClient, NatureUtilisateur.Client), horloge.MaintenantUtc);
        var fileId = Guid.NewGuid();
        var ticketMien = Ticket.Creer("TCK-MIEN", "BillingImport", "Import", fileId, dossierClient.Id, horloge.MaintenantUtc, "BL-M");
        var ticketAutrui = Ticket.Creer("TCK-AUTRUI", "BillingImport", "Import", fileId, dossierAutre.Id, horloge.MaintenantUtc, "BL-X");
        db.Dossiers.AddRange(dossierClient, dossierAutre);
        db.Tickets.AddRange(ticketMien, ticketAutrui);
        await db.SaveChangesAsync();

        var cmd = new DeposerDocumentCommand(
            docsRepo, ticketsRepo, stockage.Object, new FakeAntivirus(StatutAnalyseAntivirus.Sain),
            autorisation, cloisonnement, journal, horloge, uow);

        var contenu = new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.4\nFraude"));

        await Assert.ThrowsAnyAsync<Exception>(() => cmd.DeposerDocumentCommunPourClientAsync(
            clientId, [ticketMien.Id, ticketAutrui.Id], "x.pdf", "application/pdf", contenu, contenu.Length, Regles));

        // Cloisonnement d'abord : aucun upload ne doit partir si un ticket n'est pas au client.
        stockage.Verify(s => s.EnregistrerAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
