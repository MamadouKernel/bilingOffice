using CIT.Plateforme.Application.Administration;
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
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentId), Permission.TicketTransferer, fileSource));
        var dossier = Domain.Tickets.Dossier.Creer("DOS-MTX-000001", Guid.NewGuid(), horloge.MaintenantUtc);
        var ticket = Domain.Tickets.Ticket.Creer("TCK-MTX-000001", "BillingOffice", "Import", fileSource, dossier.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();

        var transferer = new TransfererTicketCommand(
            ticketRepo, new TransitionAutoriseeRepository(dbContext), new MotifTransfertRepository(dbContext),
            autorisation, journalisation, horloge, uniteDeTravail);

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

        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentId), Permission.TicketTransferer, fileSource));
        dbContext.TransitionsAutorisees.Add(TransitionAutorisee.Definir(fileSource, fileCible, ["Motif autorisé"]));
        var dossier = Domain.Tickets.Dossier.Creer("DOS-MTX-000002", Guid.NewGuid(), horloge.MaintenantUtc);
        var ticket = Domain.Tickets.Ticket.Creer("TCK-MTX-000002", "BillingOffice", "Import", fileSource, dossier.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();

        var transferer = new TransfererTicketCommand(
            new TicketRepository(dbContext), new TransitionAutoriseeRepository(dbContext),
            new MotifTransfertRepository(dbContext), autorisation, journalisation, horloge, uniteDeTravail);

        await Assert.ThrowsAsync<TransitionNonAutoriseeException>(() =>
            transferer.ExecuterAsync(agentId, ticket.Id, fileCible, "Motif non listé"));
    }

    /// <summary>
    /// TRF-06 — écart de re-vérification d'audit : le respect de la matrice était bien testé
    /// (refus si absente), mais aucun test ne bouclait l'exigence « paramétrable SANS
    /// DÉVELOPPEMENT » : qu'une transition ajoutée par un administrateur via
    /// <see cref="GererMatriceTransitionsCommand"/> est immédiatement honorée par
    /// <see cref="TransfererTicketCommand"/>, sans redéploiement ni code en dur.
    /// </summary>
    [Fact]
    public async Task Une_transition_ajoutee_par_lAdministration_est_immediatement_utilisable_pour_transferer()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var ticketRepo = new TicketRepository(dbContext);
        var transitionsRepo = new TransitionAutoriseeRepository(dbContext);
        var filesRepo = new FileDeTraitementRepository(dbContext);

        var administrateurId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var fileSource = FileDeTraitement.Creer("BillingImport", "Contrôle BL");
        var fileCible = FileDeTraitement.Creer("BillingImport", "Facturation");
        dbContext.Files.Add(fileSource);
        dbContext.Files.Add(fileCible);
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, administrateurId), Permission.AdministrationFiles));
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentId), Permission.TicketTransferer, fileSource.Id));
        var dossier = Domain.Tickets.Dossier.Creer("DOS-MTX-000003", Guid.NewGuid(), horloge.MaintenantUtc);
        var ticket = Domain.Tickets.Ticket.Creer("TCK-MTX-000003", "BillingOffice", "Import", fileSource.Id, dossier.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();

        var transferer = new TransfererTicketCommand(
            ticketRepo, transitionsRepo, new MotifTransfertRepository(dbContext),
            autorisation, journalisation, horloge, uniteDeTravail);

        // Avant paramétrage : refusé, comme n'importe quelle transition non déclarée.
        await Assert.ThrowsAsync<TransitionNonAutoriseeException>(() =>
            transferer.ExecuterAsync(agentId, ticket.Id, fileCible.Id, "Mal orienté"));

        // Un administrateur fonctionnel — sans toucher au code — déclare la transition.
        var gererMatrice = new GererMatriceTransitionsCommand(transitionsRepo, filesRepo, autorisation, journalisation, uniteDeTravail);
        await gererMatrice.DefinirTransitionAsync(administrateurId, fileSource.Id, fileCible.Id, ["Mal orienté"]);

        // Le même transfert, désormais accepté sans aucun changement de code.
        await transferer.ExecuterAsync(agentId, ticket.Id, fileCible.Id, "Mal orienté");

        var apres = await ticketRepo.ObtenirParIdAsync(ticket.Id);
        Assert.Equal(fileCible.Id, apres!.FileCouranteId);
    }
}
