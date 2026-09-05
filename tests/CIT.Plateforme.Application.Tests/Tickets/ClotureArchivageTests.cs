using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Tickets;

/// <summary>[TEST-SPEC] Exigence CDC: SOC-09 → US4 → scénario d'acceptation 3 de US4 → Test.</summary>
public sealed class ClotureArchivageTests
{
    [Fact]
    public async Task La_cloture_archive_automatiquement_et_le_ticket_reste_consultable()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var ticketRepo = new TicketRepository(dbContext);

        var agentId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentId), Permission.TicketCloturer, fileId));
        var dossier = Dossier.Creer("DOS-CLO-000001", Guid.NewGuid(), horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-CLO-000001", "BillingOffice", "Import", fileId, dossier.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();

        var commande = new CloturerTicketCommand(ticketRepo, autorisation, journalisation, horloge, uniteDeTravail);
        await commande.ExecuterAsync(agentId, ticket.Id);

        // "Archivage automatique" = aucune suppression, le ticket demeure consultable avec sa
        // date de clôture renseignée (SOC-09) — jamais supprimé physiquement (invariant #9 de
        // data-model.md).
        var apres = await dbContext.Tickets.FindAsync(ticket.Id);
        Assert.NotNull(apres);
        Assert.Equal(StatutTicket.Cloture, apres!.Statut);
        Assert.NotNull(apres.DateClotureUtc);
        Assert.Equal(MotifClotureType.Normale, apres.MotifCloture);
    }
}
