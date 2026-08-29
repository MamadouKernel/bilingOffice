using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Tickets;

/// <summary>
/// [TEST-CDC] (portion) REC-SCE-12 — attente client suspend le temps interne CIT, reprise après
/// réponse relance le décompte. CDC : TPS-02, TPS-04.
/// </summary>
public sealed class AttenteClientTests
{
    [Fact]
    public async Task Mise_en_attente_puis_reprise_transitionne_correctement_le_statut()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var ticketRepo = new TicketRepository(dbContext);

        var agentId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentId, Permission.TicketPrendreEnCharge, fileId));
        var dossier = Dossier.Creer("DOS-ATT-000001", Guid.NewGuid(), horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-ATT-000001", "BillingOffice", "Import", fileId, dossier.Id, horloge.MaintenantUtc);
        ticket.PrendreEnCharge(agentId, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();

        var periodesRepo = new PeriodeAttenteClientRepository(dbContext);
        var commande = new AttenteClientCommand(ticketRepo, periodesRepo, autorisation, journalisation, horloge, uniteDeTravail);

        await commande.MettreEnAttenteAsync(agentId, ticket.Id, "Document manquant");
        var apresAttente = await dbContext.Tickets.FindAsync(ticket.Id);
        Assert.Equal(StatutTicket.EnAttenteClient, apresAttente!.Statut);
        Assert.NotNull(apresAttente.DateDebutAttenteClienteUtc);

        await commande.RependreApresReponseAsync(ticket.Id);
        var apresReprise = await dbContext.Tickets.FindAsync(ticket.Id);
        Assert.Equal(StatutTicket.EnCours, apresReprise!.Statut);
        Assert.Null(apresReprise.DateDebutAttenteClienteUtc);
    }
}
