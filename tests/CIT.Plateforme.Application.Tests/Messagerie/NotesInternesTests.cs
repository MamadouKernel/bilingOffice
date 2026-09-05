using CIT.Plateforme.Application.Messagerie;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Messagerie;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Messagerie;

/// <summary>[TEST-SPEC] Exigence CDC: MSG-02, MSG-03 → US5 → scénarios d'acceptation 2, 3 de US5
/// → Test.</summary>
public sealed class NotesInternesTests
{
    [Fact]
    public async Task Une_note_interne_n_est_jamais_retournee_par_la_lecture_client()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var ticketRepo = new TicketRepository(dbContext);
        var cloisonnement = new CloisonnementService(ticketRepo, CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.ControleAcces(dbContext));

        var clientId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentId), Permission.MessageNoteInterne, fileId));
        var dossier = Dossier.Creer("DOS-NOTE-000001", CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, clientId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-NOTE-000001", "BillingOffice", "Import", fileId, dossier.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();

        var messageRepo = new MessageRepository(dbContext);
        var commande = new PublierMessageCommand(messageRepo, ticketRepo, autorisation, cloisonnement, journalisation, horloge, uniteDeTravail);

        await commande.PublierNoteInterneAsync(agentId, ticket.Id, "Client difficile, vérifier antécédents");

        // MSG-02 : jamais visible du client — la requête dédiée au client filtre systématiquement.
        var visiblesClient = await messageRepo.ListerVisiblesParClientAsync(ticket.Id);
        Assert.Empty(visiblesClient);

        // MSG-03 : la note existe bien (visible des agents habilités), avec un type distinct
        // permettant la distinction visuelle obligatoire.
        var toutLeFil = await messageRepo.ListerParTicketAsync(ticket.Id);
        Assert.Single(toutLeFil);
        Assert.Equal(TypeMessage.NoteInterne, toutLeFil[0].Type);
    }

    [Fact]
    public async Task Un_agent_sans_habilitation_note_interne_ne_peut_pas_en_publier()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var ticketRepo = new TicketRepository(dbContext);
        var cloisonnement = new CloisonnementService(ticketRepo, CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.ControleAcces(dbContext));

        var agentSansDroit = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var dossier = Dossier.Creer("DOS-NOTE-000002", Guid.NewGuid(), horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-NOTE-000002", "BillingOffice", "Import", fileId, dossier.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();

        var commande = new PublierMessageCommand(
            new MessageRepository(dbContext), ticketRepo, autorisation, cloisonnement, journalisation, horloge, uniteDeTravail);

        await Assert.ThrowsAsync<CIT.Plateforme.Application.Securite.AccesRefuseException>(() =>
            commande.PublierNoteInterneAsync(agentSansDroit, ticket.Id, "Tentative non habilitée"));
    }
}
