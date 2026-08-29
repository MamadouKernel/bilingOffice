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

/// <summary>[TEST-SPEC] Exigence CDC: SOC-07 → US6 → scénario d'acceptation 2 de US6 → Test.
/// Aucun scénario CDC direct dans ce périmètre.
/// <para>
/// REMÉDIATION AUDIT (post-implementation-audit.md §6) : ces tests exercent désormais
/// <c>DeposerPourAgentAsync</c> avec un agent réellement habilité (<see cref="Permission.DocumentDeposer"/>)
/// — le contrôle d'autorisation lui-même (positif/négatif) est couvert séparément par
/// <c>DepotDocumentAutorisationTests</c>.
/// </para>
/// </summary>
public sealed class ControleDepotTests
{
    private static DeposerDocumentCommand ConstruireCommande(out PlateformeDbContext dbContext, out Ticket ticket, out Guid agentId)
    {
        dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var stockage = new StockageDocumentsFichiers(configuration);
        var antivirus = new AucuneAnalyseAntivirusConfiguree();
        var ticketRepo = new TicketRepository(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var cloisonnement = new CloisonnementService(ticketRepo, new DossierRepository(dbContext));

        var fileId = Guid.NewGuid();
        agentId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentId, Permission.DocumentDeposer, fileId));

        var dossier = Dossier.Creer("DOS-DOC-000001", Guid.NewGuid(), horloge.MaintenantUtc);
        ticket = Ticket.Creer("TCK-DOC-000001", "BillingOffice", "Import", fileId, dossier.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);
        dbContext.SaveChanges();

        return new DeposerDocumentCommand(
            new DocumentRepository(dbContext), ticketRepo, stockage, antivirus, autorisation, cloisonnement, journalisation, horloge, uniteDeTravail);
    }

    private static readonly ReglesDepotDocument ReglesEtape = new(["pdf", "jpg"], TailleMaxOctets: 1024);

    [Fact]
    public async Task Depot_conforme_est_accepte()
    {
        var commande = ConstruireCommande(out _, out var ticket, out var agentId);
        using var contenu = new MemoryStream(Encoding.UTF8.GetBytes("contenu-pdf-simule"));

        var document = await commande.DeposerPourAgentAsync(agentId, ticket.Id, "bl.pdf", "application/pdf", contenu, contenu.Length, ReglesEtape);

        Assert.Equal("bl.pdf", document.NomFichier);
        Assert.Equal(StatutAnalyseAntivirus.Sain, document.StatutAntivirus);
    }

    [Fact]
    public async Task Depot_avec_type_non_autorise_est_refuse_avec_message_explicite()
    {
        var commande = ConstruireCommande(out _, out var ticket, out var agentId);
        using var contenu = new MemoryStream(Encoding.UTF8.GetBytes("exe"));

        var exception = await Assert.ThrowsAsync<DepotDocumentRefuseException>(() =>
            commande.DeposerPourAgentAsync(agentId, ticket.Id, "virus.exe", "application/octet-stream", contenu, contenu.Length, ReglesEtape));

        Assert.Contains("exe", exception.Message);
    }

    [Fact]
    public async Task Depot_depassant_la_taille_maximale_est_refuse_avec_message_explicite()
    {
        var commande = ConstruireCommande(out _, out var ticket, out var agentId);
        using var contenu = new MemoryStream(new byte[2048]);

        var exception = await Assert.ThrowsAsync<DepotDocumentRefuseException>(() =>
            commande.DeposerPourAgentAsync(agentId, ticket.Id, "gros.pdf", "application/pdf", contenu, contenu.Length, ReglesEtape));

        Assert.Contains("taille", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
