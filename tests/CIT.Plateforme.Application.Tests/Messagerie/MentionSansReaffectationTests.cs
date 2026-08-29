using CIT.Plateforme.Application.Messagerie;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Messagerie;

/// <summary>[TEST-SPEC] Exigence CDC: MSG-05 → US5 → scénario d'acceptation 4 de US5 → Test.</summary>
public sealed class MentionSansReaffectationTests
{
    [Fact]
    public async Task Mentionner_un_collaborateur_ne_change_jamais_l_operateur_assigne()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var ticketRepo = new TicketRepository(dbContext);

        var operateurCourant = Guid.NewGuid();
        var agentQuiMentionne = Guid.NewGuid();
        var collaborateurMentionne = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentQuiMentionne, Permission.MessageNoteInterne, fileId));

        var dossier = Dossier.Creer("DOS-MENT-000001", Guid.NewGuid(), horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-MENT-000001", "BillingOffice", "Import", fileId, dossier.Id, horloge.MaintenantUtc);
        ticket.PrendreEnCharge(operateurCourant, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();

        var commande = new MentionnerCollaborateurCommand(
            new MessageRepository(dbContext), ticketRepo, autorisation, journalisation, horloge, uniteDeTravail);

        await commande.MentionnerAsync(agentQuiMentionne, ticket.Id, collaborateurMentionne, "@Nom peux-tu regarder ce dossier ?");

        var ticketApres = await dbContext.Tickets.FindAsync(ticket.Id);
        Assert.Equal(operateurCourant, ticketApres!.OperateurAssigneId); // MSG-05

        var message = dbContext.Messages.Single(m => m.TicketId == ticket.Id);
        Assert.Equal(collaborateurMentionne, message.CollaborateurMentionneId);
    }
}
