using System.Text;
using CIT.Plateforme.Application.Documents;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Documents;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Documents;

/// <summary>
/// REMÉDIATION AUDIT (post-implementation-audit.md §6 — AUTORISATIONS MANQUANTES, point 3 de la
/// demande de remédiation) : avant correction, <c>DeposerDocumentCommand</c> n'effectuait aucun
/// contrôle d'autorisation ni de cloisonnement — n'importe quel appelant pouvait déposer un
/// document sur n'importe quel ticket. Ces tests appellent directement la couche applicative
/// (sans passer par aucune UI), conformément à la convention déjà établie par
/// <c>AutorisationServeurTests</c>/<c>AccesDocumentAutoriseTests</c>.
/// </summary>
public sealed class DepotDocumentAutorisationTests
{
    private static (PlateformeDbContext DbContext, DeposerDocumentCommand Commande, Ticket Ticket, Guid FileId, Guid ClientProprietaire)
        ConstruireContexte()
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
        var cloisonnement = new CloisonnementService(ticketRepo, CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.ControleAcces(dbContext));

        var fileId = Guid.NewGuid();
        var clientProprietaire = Guid.NewGuid();
        var dossier = Dossier.Creer("DOS-DEPOT-AUTZ-000001", CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, clientProprietaire, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-DEPOT-AUTZ-000001", "BillingOffice", "Import", fileId, dossier.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);
        dbContext.SaveChanges();

        var commande = new DeposerDocumentCommand(
            new DocumentRepository(dbContext), ticketRepo, stockage, antivirus, autorisation, cloisonnement, journalisation, horloge, uniteDeTravail);

        return (dbContext, commande, ticket, fileId, clientProprietaire);
    }

    private static readonly ReglesDepotDocument Regles = new(["pdf"], TailleMaxOctets: 1024);

    private static MemoryStream ContenuDeTest() => new(Encoding.UTF8.GetBytes("contenu-pdf-simule"));

    [Fact]
    public async Task Agent_habilite_sur_la_file_est_accepte()
    {
        var (dbContext, commande, ticket, fileId, _) = ConstruireContexte();
        var agentHabilite = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentHabilite), Permission.DocumentDeposer, fileId));
        await dbContext.SaveChangesAsync();

        using var contenu = ContenuDeTest();
        var document = await commande.DeposerPourAgentAsync(agentHabilite, ticket.Id, "bl.pdf", "application/pdf", contenu, contenu.Length, Regles);

        Assert.Equal("bl.pdf", document.NomFichier);
    }

    [Fact]
    public async Task Agent_non_habilite_est_refuse()
    {
        var (_, commande, ticket, _, _) = ConstruireContexte();
        var agentNonHabilite = Guid.NewGuid(); // aucune habilitation DocumentDeposer attribuée

        using var contenu = ContenuDeTest();
        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            commande.DeposerPourAgentAsync(agentNonHabilite, ticket.Id, "bl.pdf", "application/pdf", contenu, contenu.Length, Regles));
    }

    [Fact]
    public async Task Client_proprietaire_du_ticket_est_accepte()
    {
        var (_, commande, ticket, _, clientProprietaire) = ConstruireContexte();

        using var contenu = ContenuDeTest();
        var document = await commande.DeposerPourClientAsync(clientProprietaire, ticket.Id, "bl.pdf", "application/pdf", contenu, contenu.Length, Regles);

        Assert.Equal("bl.pdf", document.NomFichier);
    }

    [Fact]
    public async Task Client_non_proprietaire_du_ticket_est_refuse()
    {
        var (_, commande, ticket, _, _) = ConstruireContexte();
        var clientIntrus = Guid.NewGuid();

        using var contenu = ContenuDeTest();
        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            commande.DeposerPourClientAsync(clientIntrus, ticket.Id, "bl.pdf", "application/pdf", contenu, contenu.Length, Regles));
    }
}
