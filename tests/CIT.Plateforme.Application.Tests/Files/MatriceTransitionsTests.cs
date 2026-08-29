using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Files;

/// <summary>
/// [TEST-TECH] Decision: CLAR-04 (spec.md § Clarifications), TRF-06.
/// Règle métier critique « transitions » : toute transition hors matrice est mécaniquement
/// refusée, quelle que soit l'habilitation de l'initiateur — vérifié en appelant directement le
/// cas d'usage, sans dépendre d'aucune interface.
/// <para>
/// CORRECTION DE PLACEMENT (voir checkpoint) : `tasks.md` assignait ce test à Domain.Tests ; la
/// vérification porte sur l'orchestration applicative (absence d'entrée dans le référentiel de
/// transitions), pas sur une règle de domaine pure — il vit donc dans Application.Tests.
/// </para>
/// </summary>
public sealed class MatriceTransitionsTests
{
    [Fact]
    public async Task Transfert_vers_une_file_absente_de_la_matrice_est_toujours_refuse_meme_habilite()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var ticketRepo = new TicketRepository(dbContext);

        var agentId = Guid.NewGuid();
        var fileSource = Guid.NewGuid();
        var fileCible = Guid.NewGuid();

        // L'agent est pleinement habilité à transférer — mais AUCUNE entrée n'existe dans la
        // matrice pour (fileSource -> fileCible).
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentId, Permission.TicketTransferer, fileSource));
        var dossier = Domain.Tickets.Dossier.Creer("DOS-MTX-000001", Guid.NewGuid(), horloge.MaintenantUtc);
        var ticket = Domain.Tickets.Ticket.Creer("TCK-MTX-000001", "BillingOffice", "Import", fileSource, dossier.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();

        var transferer = new TransfererTicketCommand(
            ticketRepo, new TransitionAutoriseeRepository(dbContext), autorisation, journalisation, horloge, uniteDeTravail);

        await Assert.ThrowsAsync<TransitionNonAutoriseeException>(() =>
            transferer.ExecuterAsync(agentId, ticket.Id, fileCible, "Motif quelconque"));
    }

    [Fact]
    public async Task Transfert_avec_motif_non_liste_pour_une_transition_existante_est_refuse()
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
        dbContext.TransitionsAutorisees.Add(TransitionAutorisee.Definir(fileSource, fileCible, ["Motif autorisé"]));
        var dossier = Domain.Tickets.Dossier.Creer("DOS-MTX-000002", Guid.NewGuid(), horloge.MaintenantUtc);
        var ticket = Domain.Tickets.Ticket.Creer("TCK-MTX-000002", "BillingOffice", "Import", fileSource, dossier.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();

        var transferer = new TransfererTicketCommand(
            new TicketRepository(dbContext), new TransitionAutoriseeRepository(dbContext), autorisation, journalisation, horloge, uniteDeTravail);

        await Assert.ThrowsAsync<TransitionNonAutoriseeException>(() =>
            transferer.ExecuterAsync(agentId, ticket.Id, fileCible, "Motif non listé"));
    }
}
