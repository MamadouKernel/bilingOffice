using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Tickets;

/// <summary>
/// [TEST-SPEC] Exigence CDC: TRF-01, TRF-03, PEC-01 → US2 → scénario d'acceptation 7 de US2
/// (CLAR-03) → Test.
/// </summary>
public sealed class TransfertSansAffectationTests
{
    [Fact]
    public async Task Transfert_sans_destinataire_designe_repart_en_attente_de_pec()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var agentId = Guid.NewGuid();
        var fileSource = Guid.NewGuid();
        var fileCible = Guid.NewGuid();

        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentId, Permission.TicketTransferer, fileSource));
        dbContext.TransitionsAutorisees.Add(TransitionAutorisee.Definir(fileSource, fileCible, []));
        var dossier = Dossier.Creer("DOS-C03-000001", Guid.NewGuid(), horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-C03-000001", "BillingOffice", "Import", fileSource, dossier.Id, horloge.MaintenantUtc);
        ticket.PrendreEnCharge(Guid.NewGuid(), horloge.MaintenantUtc); // ticket était affecté avant transfert
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();

        var transferer = new TransfererTicketCommand(
            new TicketRepository(dbContext), new TransitionAutoriseeRepository(dbContext), autorisation, journalisation, horloge, uniteDeTravail);

        await transferer.ExecuterAsync(agentId, ticket.Id, fileCible, "Motif", destinataireId: null);

        var ticketApres = await dbContext.Tickets.FindAsync(ticket.Id);
        Assert.Equal(StatutTicket.EnAttenteDePriseEnCharge, ticketApres!.Statut);
        Assert.Null(ticketApres.OperateurAssigneId);
        Assert.Null(ticketApres.DatePriseEnChargeUtc);
    }

    [Fact]
    public async Task Transfert_avec_destinataire_designe_affecte_directement()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var agentId = Guid.NewGuid();
        var destinataireId = Guid.NewGuid();
        var fileSource = Guid.NewGuid();
        var fileCible = Guid.NewGuid();

        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentId, Permission.TicketTransferer, fileSource));
        dbContext.TransitionsAutorisees.Add(TransitionAutorisee.Definir(fileSource, fileCible, []));
        var dossier = Dossier.Creer("DOS-C03-000002", Guid.NewGuid(), horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-C03-000002", "BillingOffice", "Import", fileSource, dossier.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();

        var transferer = new TransfererTicketCommand(
            new TicketRepository(dbContext), new TransitionAutoriseeRepository(dbContext), autorisation, journalisation, horloge, uniteDeTravail);

        await transferer.ExecuterAsync(agentId, ticket.Id, fileCible, "Motif", destinataireId);

        var ticketApres = await dbContext.Tickets.FindAsync(ticket.Id);
        Assert.Equal(StatutTicket.EnCours, ticketApres!.Statut);
        Assert.Equal(destinataireId, ticketApres.OperateurAssigneId);
    }
}
