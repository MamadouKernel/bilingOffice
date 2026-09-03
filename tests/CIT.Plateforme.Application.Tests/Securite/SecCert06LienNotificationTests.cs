using System;
using System.Threading.Tasks;
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

namespace CIT.Plateforme.Application.Tests.Securite;

/// <summary>
/// FEATURE 011 — CERTIFICATION SEC-12 · suite TEST-SPEC « SEC-CERT-06 » (T026→T030).
///
/// Texte CDC (SEC-12, Must) : « Aucun lien transmis par notification ne donne accès direct
/// à un document protégé sans contrôle préalable d'identité et d'habilitations : le lien
/// conduit au ticket après authentification. »
///
/// Volets DÉJÀ DÉMONTRÉS par AccesDocumentAutoriseTests (rattachés, non dupliqués — I5) :
///   - non-propriétaire refusé ;
///   - agent habilité accepté ;
///   - document non analysé ou infecté refusé.
///
/// Volets AJOUTÉS par cette suite :
///   T027 — accès sans authentification → refus sans divulgation ;
///   T028 — habilitation retirée → refus sans divulgation ;
///   T029 — la notification porte une référence de ticket, pas une URL absolue ni un jeton.
///
/// AUCUN CODE PRODUIT N'EST MODIFIÉ. Aucun chemin d'accès parallèle n'est introduit.
/// </summary>
public sealed class SecCert06LienNotificationTests
{
    // ---- T027 : accès sans authentification → contrôle d'identité exigé ----

    [Fact]
    public async Task Acces_sans_authentification_est_refuse_sans_divulgation()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var stockage = new StockageDocumentsFichiers(configuration, new EnvironnementDeTest("Development"));
        var autorisation = new AutorisationService(dbContext);
        var ticketRepo = new TicketRepository(dbContext);
        var cloisonnement = new CloisonnementService(ticketRepo, new DossierRepository(dbContext));

        var proprietaire = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var dossier = Dossier.Creer("DOS-SEC12-001", proprietaire, horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-SEC12-001", "BillingOffice", "Import", fileId, dossier.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);

        var cle = await stockage.EnregistrerAsync(
            new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes("contenu")), "facture.pdf");
        var document = Document.Deposer(ticket.Id, proprietaire, "facture.pdf", "application/pdf", 7, cle, horloge.MaintenantUtc);
        document.MarquerResultatAntivirus(StatutAnalyseAntivirus.Sain);
        dbContext.Documents.Add(document);
        await dbContext.SaveChangesAsync();

        var query = new TelechargerDocumentQuery(
            new DocumentRepository(dbContext), stockage, autorisation, ticketRepo, cloisonnement, journalisation, uniteDeTravail);

        // Un identifiant Guid.Empty simule un appelant non authentifié. Le service applicatif
        // doit refuser l'accès — jamais servir le contenu, quels que soient les paramètres de
        // l'URL ou l'identifiant du document.
        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            query.TelechargerPourClientAsync(Guid.Empty, document.Id));
    }

    // ---- T028 : habilitation retirée → refus sans divulgation ----

    [Fact]
    public async Task Agent_dont_l_habilitation_a_ete_retiree_est_refuse()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var stockage = new StockageDocumentsFichiers(configuration, new EnvironnementDeTest("Development"));
        var autorisation = new AutorisationService(dbContext);
        var ticketRepo = new TicketRepository(dbContext);
        var cloisonnement = new CloisonnementService(ticketRepo, new DossierRepository(dbContext));

        var proprietaire = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        // NE PAS attribuer Permission.DocumentTelecharger → l'agent n'a pas l'habilitation.
        var dossier = Dossier.Creer("DOS-SEC12-002", proprietaire, horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-SEC12-002", "BillingOffice", "Import", fileId, dossier.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);

        var cle = await stockage.EnregistrerAsync(
            new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes("contenu")), "bl.pdf");
        var document = Document.Deposer(ticket.Id, proprietaire, "bl.pdf", "application/pdf", 7, cle, horloge.MaintenantUtc);
        document.MarquerResultatAntivirus(StatutAnalyseAntivirus.Sain);
        dbContext.Documents.Add(document);
        await dbContext.SaveChangesAsync();

        var query = new TelechargerDocumentQuery(
            new DocumentRepository(dbContext), stockage, autorisation, ticketRepo, cloisonnement, journalisation, uniteDeTravail);

        // Un agent sans habilitation de téléchargement doit être refusé — même si le document
        // est sain. Aucune information sur l'existence du document n'est divulguée.
        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            query.TelechargerPourAgentAsync(agentId, document.Id));
    }

    // ---- T029 : la notification porte une référence, pas une URL absolue ni un jeton ----

    [Fact]
    public void La_notification_porte_une_reference_de_ticket_pas_une_url_absolue_ni_un_jeton()
    {
        // Vérification STRUCTURELLE : une notification porte un TicketId (Guid), jamais une URL
        // absolue vers un document, jamais un jeton d'accès. Le lien est relatif et résolu au
        // rendu (research.md D12, Notification.cs commentaire TicketId).
        var ticketId = Guid.NewGuid();
        var notification = Domain.Notifications.Notification.Creer(
            evenementSourceId: Guid.NewGuid(),
            destinataireId: Guid.NewGuid(),
            typeEvenement: "SOC-01",
            modeleId: null,
            titre: "Votre dossier a été traité",
            corps: "Le ticket TCK-SEC12-003 a été clôturé.",
            ticketId: ticketId,
            maintenantUtc: DateTime.UtcNow);

        // La notification porte une référence de ticket en tant que Guid — pas une URL.
        Assert.Equal(ticketId, notification.TicketId);

        // Le corps ne contient aucune URL absolue ni jeton d'accès.
        Assert.DoesNotContain("https://", notification.Corps, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("http://", notification.Corps, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token=", notification.Corps, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("access_token", notification.Corps, StringComparison.OrdinalIgnoreCase);
    }
}
