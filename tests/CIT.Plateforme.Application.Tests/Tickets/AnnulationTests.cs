using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Tickets;

/// <summary>
/// [TEST-SPEC] Exigence CDC: CYC-01, CYC-02, CYC-03 → US4 → scénarios d'acceptation 1, 2 de US4
/// → Test. Aucun scénario CDC direct dans ce périmètre. Règle métier critique « cycle de vie ».
/// </summary>
public sealed class AnnulationTests
{
    private static Guid _fileCourante;

    private static AnnulerTicketCommand ConstruireCommande(out PlateformeDbContext dbContext, out Ticket ticket, out Guid clientId)
    {
        dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new CIT.Plateforme.Infrastructure.Identity.AutorisationService(dbContext);
        var ticketRepo = new TicketRepository(dbContext);
        var cloisonnement = new CloisonnementService(ticketRepo, CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.ControleAcces(dbContext));

        clientId = Guid.NewGuid();
        var dossier = Dossier.Creer("DOS-ANN-000001", CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, clientId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), horloge.MaintenantUtc);
        var file = FileDeTraitement.Creer("BillingOffice", "File d'annulation");
        dbContext.Files.Add(file);
        _fileCourante = file.Id;
        ticket = Ticket.Creer("TCK-ANN-000001", "BillingOffice", "Import", file.Id, dossier.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);
        dbContext.SaveChanges();

        return new AnnulerTicketCommand(ticketRepo, autorisation, journalisation, cloisonnement,
            new EtapeBloquanteAnnulationRepository(dbContext), new PassageEnFileRepository(dbContext),
            horloge, uniteDeTravail);
    }

    [Fact]
    public async Task CYC_01_annulation_libre_acceptee_avant_l_etape_bloquante()
    {
        var commande = ConstruireCommande(out var dbContext, out var ticket, out var clientId);

        await commande.AnnulerLibrementAsync(clientId, ticket.Id, "Je n'en ai plus besoin");

        var apres = await dbContext.Tickets.FindAsync(ticket.Id);
        Assert.Equal(StatutTicket.Annule, apres!.Statut);
    }

    /// <summary>
    /// OBJECTIF MÉTIER INCHANGÉ : au-delà de l'étape bloquante, l'annulation libre est refusée.
    /// Seule son EXPRESSION change (remédiation P1-01) : le test pilotait un booléen passé à la
    /// commande — ce que faisait aussi l'interface, avec la valeur `false`, ce qui privait la règle
    /// de tout effet. Le seuil est désormais une donnée de paramétrage, et c'est elle qu'on pose.
    /// La couverture s'en trouve renforcée : on éprouve la règle réelle, plus un argument.
    /// </summary>
    [Fact]
    public async Task CYC_02_annulation_libre_refusee_au_dela_de_l_etape_bloquante()
    {
        var commande = ConstruireCommande(out var dbContext, out var ticket, out var clientId);

        dbContext.EtapesBloquantesAnnulation.Add(EtapeBloquanteAnnulation.Definir(
            "BillingOffice", "Import", _fileCourante, "la facturation a été engagée"));
        await dbContext.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<AnnulationLibreFermeeException>(() =>
            commande.AnnulerLibrementAsync(clientId, ticket.Id, "Motif"));

        Assert.Contains("la facturation a été engagée", exception.Message);
    }

    [Fact]
    public async Task CYC_02_demande_d_annulation_est_transmise_sans_annuler_automatiquement()
    {
        var commande = ConstruireCommande(out var dbContext, out var ticket, out var clientId);

        await commande.DemanderAnnulationAsync(clientId, ticket.Id, "Erreur de saisie");

        var apres = await dbContext.Tickets.FindAsync(ticket.Id);
        Assert.NotEqual(StatutTicket.Annule, apres!.Statut); // transmise pour décision, pas appliquée automatiquement (CYC-02)

        var historique = dbContext.HistoriqueTickets.Where(h => h.TicketId == ticket.Id).ToList();
        Assert.Contains(historique, h => h.Evenement == "DemandeAnnulationSoumise");
    }

    [Fact]
    public async Task CYC_03_toute_annulation_est_motivee_et_tracee()
    {
        var commande = ConstruireCommande(out _, out var ticket, out var clientId);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            commande.AnnulerLibrementAsync(clientId, ticket.Id, motif: ""));
    }
}
