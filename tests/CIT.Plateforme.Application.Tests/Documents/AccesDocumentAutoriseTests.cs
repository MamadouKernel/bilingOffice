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

/// <summary>[TEST-TECH] Decision: D9 (port de stockage, contrôle à chaque accès), SEC-12. Règle
/// métier critique « autorisations » : un document non validé n'est jamais accessible, et un
/// utilisateur hors périmètre est systématiquement refusé, quel que soit le point d'entrée.</summary>
public sealed class AccesDocumentAutoriseTests
{
    private static async Task<(PlateformeDbContext DbContext, TelechargerDocumentQuery Query, Document Document, Guid ClientProprietaire, Guid AgentHabilite)>
        ConstruireContexteAvecDocumentAsync(StatutAnalyseAntivirus statutAntivirus)
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var stockage = new StockageDocumentsFichiers(configuration);
        var autorisation = new AutorisationService(dbContext);
        var ticketRepo = new TicketRepository(dbContext);
        var cloisonnement = new CloisonnementService(ticketRepo, new DossierRepository(dbContext));

        var clientProprietaire = Guid.NewGuid();
        var agentHabilite = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentHabilite, Permission.DocumentTelecharger, fileId));

        var dossier = Dossier.Creer("DOS-ACC-000001", clientProprietaire, horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-ACC-000001", "BillingOffice", "Import", fileId, dossier.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);

        var cle = await stockage.EnregistrerAsync(new MemoryStream(Encoding.UTF8.GetBytes("contenu")), "facture.pdf");
        var document = Document.Deposer(ticket.Id, clientProprietaire, "facture.pdf", "application/pdf", 7, cle, horloge.MaintenantUtc);
        document.MarquerResultatAntivirus(statutAntivirus);
        dbContext.Documents.Add(document);
        await dbContext.SaveChangesAsync();

        var query = new TelechargerDocumentQuery(
            new DocumentRepository(dbContext), stockage, autorisation, ticketRepo, cloisonnement, journalisation, uniteDeTravail);

        return (dbContext, query, document, clientProprietaire, agentHabilite);
    }

    [Fact]
    public async Task Un_document_non_valide_par_l_antivirus_n_est_jamais_accessible()
    {
        var (_, query, document, clientProprietaire, _) = await ConstruireContexteAvecDocumentAsync(StatutAnalyseAntivirus.NonAnalyse);

        await Assert.ThrowsAsync<DocumentNonAccessibleException>(() =>
            query.TelechargerPourClientAsync(clientProprietaire, document.Id));
    }

    [Fact]
    public async Task Un_document_infecte_n_est_jamais_accessible_meme_au_proprietaire()
    {
        var (_, query, document, clientProprietaire, _) = await ConstruireContexteAvecDocumentAsync(StatutAnalyseAntivirus.Infecte);

        await Assert.ThrowsAsync<DocumentNonAccessibleException>(() =>
            query.TelechargerPourClientAsync(clientProprietaire, document.Id));
    }

    [Fact]
    public async Task Un_client_qui_n_est_pas_proprietaire_est_refuse_meme_si_le_document_est_sain()
    {
        var (_, query, document, _, _) = await ConstruireContexteAvecDocumentAsync(StatutAnalyseAntivirus.Sain);
        var intrus = Guid.NewGuid();

        await Assert.ThrowsAsync<AccesRefuseException>(() => query.TelechargerPourClientAsync(intrus, document.Id));
    }

    [Fact]
    public async Task Un_agent_habilite_accede_a_un_document_sain()
    {
        var (_, query, document, _, agentHabilite) = await ConstruireContexteAvecDocumentAsync(StatutAnalyseAntivirus.Sain);

        await using var flux = await query.TelechargerPourAgentAsync(agentHabilite, document.Id);
        Assert.NotNull(flux);
    }
}
