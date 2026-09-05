using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Tickets;

/// <summary>
/// [P1-01 — RG-TCK-07 / CYC-01, CYC-02] Fermeture de l'annulation libre.
///
/// DÉFAUT REPRODUIT ICI (`docs/audit-conformite-merise.md` § 27, P1-01, complété à la remédiation).
/// La règle exige que l'annulation directe soit fermée « tant qu'aucun traitement métier engageant
/// ni document officiel n'a été produit ; au-delà […] selon un seuil paramétrable par workflow ».
///
/// Trois défauts se cumulaient, et leur effet combiné était l'<b>absence totale de protection</b> :
/// <list type="number">
///   <item>la décision n'était pas prise par la commande mais <b>reçue en paramètre</b> ;</item>
///   <item>l'unique appelant — <c>Tickets/DetailTicket.razor</c> — passait <c>false</c> en dur ;</item>
///   <item><c>Ticket.MarquerEtatEngageant()</c> n'avait <b>aucun appelant en production</b>, si
///   bien que <c>EstEnEtatEngageant</c> valait toujours <c>false</c> — ce qui rendait également
///   inerte la garde CYC-05 sur la suggestion de clôture (RG-TCK-11).</item>
/// </list>
/// Aucun seuil paramétrable n'existait par ailleurs.
/// </summary>
public sealed class AnnulationSeuilBloquantTests
{
    private static AnnulerTicketCommand ConstruireCommande(
        out PlateformeDbContext dbContext, out Ticket ticket, out Guid clientId)
    {
        dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new CIT.Plateforme.Infrastructure.Identity.AutorisationService(dbContext);
        var ticketRepo = new TicketRepository(dbContext);
        var cloisonnement = new CloisonnementService(ticketRepo, CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.ControleAcces(dbContext));

        clientId = Guid.NewGuid();
        var dossier = Dossier.Creer("DOS-SEUIL-000001", CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, clientId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), horloge.MaintenantUtc);
        ticket = Ticket.Creer("BIL-IMP-SEUIL1", "BillingImport", "Import", Guid.NewGuid(), dossier.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);
        dbContext.SaveChanges();

        return new AnnulerTicketCommand(ticketRepo, autorisation, journalisation, cloisonnement,
            new EtapeBloquanteAnnulationRepository(dbContext), new PassageEnFileRepository(dbContext),
            horloge, uniteDeTravail);
    }

    /// <summary>
    /// REPRODUCTION DU DÉFAUT. Le ticket a atteint un état engageant — une facture a été éditée.
    /// L'appelant demande néanmoins l'annulation libre exactement comme le fait l'interface.
    /// La commande doit refuser d'elle-même, sans qu'on ait à le lui dire.
    /// </summary>
    [Fact]
    public async Task CYC_01_annulation_libre_refusee_lorsque_le_ticket_est_en_etat_engageant()
    {
        var commande = ConstruireCommande(out var dbContext, out var ticket, out var clientId);

        ticket.MarquerEtatEngageant();
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<AnnulationLibreFermeeException>(() =>
            commande.AnnulerLibrementAsync(clientId, ticket.Id, "Je change d'avis"));

        var apres = await dbContext.Tickets.FindAsync(ticket.Id);
        Assert.NotEqual(StatutTicket.Annule, apres!.Statut);
    }

    /// <summary>Cas nominal : rien d'engageant ne s'est produit, l'annulation reste libre.</summary>
    [Fact]
    public async Task CYC_01_annulation_libre_acceptee_tant_qu_aucun_traitement_engageant()
    {
        var commande = ConstruireCommande(out var dbContext, out var ticket, out var clientId);

        await commande.AnnulerLibrementAsync(clientId, ticket.Id, "Je n'en ai plus besoin");

        var apres = await dbContext.Tickets.FindAsync(ticket.Id);
        Assert.Equal(StatutTicket.Annule, apres!.Statut);
    }

    /// <summary>
    /// CYC-02 — au-delà du seuil, la demande d'annulation reste ouverte. Le client n'est jamais
    /// bloqué : c'est l'annulation <b>directe</b> qui se ferme, pas le droit de demander.
    /// </summary>
    [Fact]
    public async Task CYC_02_la_demande_d_annulation_reste_possible_au_dela_du_seuil()
    {
        var commande = ConstruireCommande(out var dbContext, out var ticket, out var clientId);

        ticket.MarquerEtatEngageant();
        await dbContext.SaveChangesAsync();

        await commande.DemanderAnnulationAsync(clientId, ticket.Id, "Erreur de saisie");

        var historique = dbContext.HistoriqueTickets.Where(h => h.TicketId == ticket.Id).ToList();
        Assert.Contains(historique, h => h.Evenement == "DemandeAnnulationSoumise");
    }
}
