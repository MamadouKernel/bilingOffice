using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Supervision;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Supervision;

/// <summary>[TEST-SPEC] Exigence CDC: MAS-02 → US10 → scénario d'acceptation 4 de US10 → Test.
/// Aucun scénario CDC direct dans ce périmètre (conditionnée à MAS-01, Should).</summary>
public sealed class ActionsDeMasseTests
{
    [Fact]
    public async Task Chaque_ticket_de_la_selection_est_trace_individuellement_meme_en_cas_d_echec_partiel()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);

        var superviseurId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(superviseurId, Permission.ActionDeMasse));

        var dossier = Dossier.Creer("DOS-MAS-000001", Guid.NewGuid(), horloge.MaintenantUtc);
        var ticket1 = Ticket.Creer("TCK-MAS-000001", "BillingOffice", "Import", Guid.NewGuid(), dossier.Id, horloge.MaintenantUtc);
        var ticket2 = Ticket.Creer("TCK-MAS-000002", "BillingOffice", "Import", Guid.NewGuid(), dossier.Id, horloge.MaintenantUtc);
        ticket2.Cloturer(MotifClotureType.Normale, horloge.MaintenantUtc); // déjà clôturé -> échouera à l'annulation
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.AddRange(ticket1, ticket2);
        await dbContext.SaveChangesAsync();

        var commande = new ActionDeMasseCommand(autorisation, journalisation, uniteDeTravail);

        var resultats = await commande.ExecuterAsync(
            superviseurId, [ticket1.Id, ticket2.Id], TypeActionDeMasse.ModifierPriorite,
            async (ticketId, ct) =>
            {
                var t = await dbContext.Tickets.FindAsync([ticketId], ct);
                t!.Annuler(horloge.MaintenantUtc); // lève si déjà clôturé (ticket2)
            });

        Assert.Equal(2, resultats.Count);
        Assert.True(resultats.Single(r => r.TicketId == ticket1.Id).Succes);
        Assert.False(resultats.Single(r => r.TicketId == ticket2.Id).Succes);

        // MAS-02 : traçage individuel — une entrée d'audit PAR ticket de la sélection, y compris
        // pour l'échec.
        var entreesAudit = dbContext.JournalAudit.Where(a => a.Action.StartsWith("ActionDeMasse:")).ToList();
        Assert.Equal(2, entreesAudit.Count);
        Assert.Contains(entreesAudit, a => a.EntiteId == ticket1.Id && a.DetailApres == "succès");
        Assert.Contains(entreesAudit, a => a.EntiteId == ticket2.Id && a.DetailApres!.StartsWith("échec"));
    }
}
