using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Audit;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Messagerie;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Tests.Partages;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Tickets;

/// <summary>
/// [TEST-SPEC — RG-TCK-06] LA CLÔTURE ARCHIVE, ET L'ARCHIVE RESTE LISIBLE.
///
/// DÉFAUT CORRIGÉ (audit final, <c>docs/audit-final-conformite-98RG.md</c>) : l'archivage n'était
/// pas matérialisé. Le code affirmait en commentaire qu'il était « représenté par la simple
/// présence de DateClotureUtc », et <c>DateArchivageUtc</c> n'était alimentée que par la purge de
/// rétention — ce qui confondait <b>archiver</b> et <b>anonymiser</b>, deux opérations opposées :
/// l'une préserve la lisibilité, l'autre la retire.
///
/// CLÔTURE et ARCHIVAGE sont désormais distincts : la première est la décision d'un agent habilité,
/// le second sa conséquence automatique. Ces tests éprouvent les deux, et surtout ce que le CDC
/// exige de l'archive — qu'elle reste <b>consultable, complète, et soumise aux mêmes droits</b>.
/// </summary>
public sealed class ArchivageAutomatiqueTests
{
    private sealed record Contexte(
        PlateformeDbContext Db,
        CloturerTicketCommand Cloturer,
        ObtenirFicheTicketQuery Fiche,
        Ticket Ticket,
        Guid AgentId,
        Guid ClientId,
        Guid EtrangerId);

    private static async Task<Contexte> ConstruireAsync()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();

        var file = FileDeTraitement.Creer("BillingOffice", "Facturation");
        db.Files.Add(file);

        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Societe Archive", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        db.Societes.Add(societe);

        var clientId = ComptesDeTest.CompteActif(db, Guid.NewGuid(), NatureUtilisateur.Client);
        db.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(clientId, societe.Id, DateTime.UtcNow, droitSoumission: true));

        // Un client d'une AUTRE société — le cloisonnement doit tenir sur l'archive comme ailleurs.
        var autreSociete = SocieteCliente.Soumettre(Guid.NewGuid(), "Autre societe", TypeClient.Normal, DateTime.UtcNow);
        autreSociete.Valider();
        db.Societes.Add(autreSociete);
        var etrangerId = ComptesDeTest.CompteActif(db, Guid.NewGuid(), NatureUtilisateur.Client);
        db.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(etrangerId, autreSociete.Id, DateTime.UtcNow, droitSoumission: true));

        var agentId = ComptesDeTest.CompteActif(db, Guid.NewGuid(), NatureUtilisateur.Agent);
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentId, Permission.TicketCloturer, file.Id));

        var dossier = Dossier.Creer("DOS-ARCH-0001", societe.Id, horloge.MaintenantUtc, clientId, TypeClient.Normal);
        db.Dossiers.Add(dossier);

        var ticket = Ticket.Creer("TCK-ARCH-0001", "BillingOffice", "Import", file.Id, dossier.Id, horloge.MaintenantUtc, "BL-ARCH-1");
        db.Tickets.Add(ticket);

        // Contenu que l'archive doit conserver : historique, document, échanges.
        db.HistoriqueTickets.Add(EntreeHistorique.Creer(ticket.Id, "TicketCree", null, clientId, horloge.MaintenantUtc));
        db.Documents.Add(Document.Deposer(
            ticket.Id, clientId, "connaissement.pdf", "application/pdf", 2048, "documents/connaissement.pdf", horloge.MaintenantUtc));
        db.Messages.Add(Message.Publier(ticket.Id, clientId, TypeMessage.Externe, "Bonjour, où en est mon dossier ?", horloge.MaintenantUtc));
        db.Messages.Add(Message.Publier(ticket.Id, agentId, TypeMessage.NoteInterne, "Note interne : à vérifier.", horloge.MaintenantUtc));

        await db.SaveChangesAsync();

        var cloturer = new CloturerTicketCommand(
            new TicketRepository(db), new AutorisationService(db),
            new JournalisationService(db, horloge), horloge, new UniteDeTravail(db));

        return new Contexte(db, cloturer, FabriqueRequetes.FicheTicket(db), ticket, agentId, clientId, etrangerId);
    }

    /// <summary>La clôture archive AUTOMATIQUEMENT — sans geste supplémentaire.</summary>
    [Fact]
    public async Task La_cloture_archive_automatiquement()
    {
        var c = await ConstruireAsync();
        using var _ = c.Db;

        Assert.False(c.Ticket.EstArchive);
        Assert.Null(c.Ticket.DateArchivageUtc);

        await c.Cloturer.ExecuterAsync(c.AgentId, c.Ticket.Id);

        Assert.Equal(StatutTicket.Cloture, c.Ticket.Statut);
        Assert.True(c.Ticket.EstArchive);
        Assert.Equal(c.Ticket.DateClotureUtc, c.Ticket.DateArchivageUtc);
    }

    /// <summary>
    /// CLÔTURE ≠ ARCHIVAGE. L'archivage n'est pas l'anonymisation : le ticket archivé n'est ni
    /// anonymisé, ni vidé.
    /// </summary>
    [Fact]
    public async Task Archivage_n_est_pas_anonymisation()
    {
        var c = await ConstruireAsync();
        using var _ = c.Db;

        await c.Cloturer.ExecuterAsync(c.AgentId, c.Ticket.Id);

        Assert.True(c.Ticket.EstArchive);
        Assert.False(c.Ticket.EstAnonymise);
    }

    /// <summary>Le ticket archivé reste consultable par son client, avec son historique complet.</summary>
    [Fact]
    public async Task Ticket_archive_reste_consultable_avec_historique_complet()
    {
        var c = await ConstruireAsync();
        using var _ = c.Db;
        await c.Cloturer.ExecuterAsync(c.AgentId, c.Ticket.Id);

        var fiche = await c.Fiche.ExecuterAsync(c.ClientId, c.Ticket.Id);

        Assert.True(fiche.EstArchive);
        Assert.NotNull(fiche.DateArchivageUtc);
        Assert.Equal("TCK-ARCH-0001", fiche.NumeroTicket);
        Assert.NotEmpty(fiche.Chronologie);
    }

    /// <summary>Les documents sont conservés et restitués.</summary>
    [Fact]
    public async Task Documents_conserves_apres_archivage()
    {
        var c = await ConstruireAsync();
        using var _ = c.Db;
        await c.Cloturer.ExecuterAsync(c.AgentId, c.Ticket.Id);

        var fiche = await c.Fiche.ExecuterAsync(c.ClientId, c.Ticket.Id);

        Assert.Contains(fiche.Documents, d => d.NomFichier == "connaissement.pdf");
    }

    /// <summary>Les échanges sont conservés — et le filtrage des notes internes tient toujours.</summary>
    [Fact]
    public async Task Echanges_conserves_et_notes_internes_toujours_filtrees()
    {
        var c = await ConstruireAsync();
        using var _ = c.Db;
        await c.Cloturer.ExecuterAsync(c.AgentId, c.Ticket.Id);

        var ficheClient = await c.Fiche.ExecuterAsync(c.ClientId, c.Ticket.Id);
        Assert.Contains(ficheClient.Messages, m => m.Contenu.StartsWith("Bonjour", StringComparison.Ordinal));
        Assert.DoesNotContain(ficheClient.Messages, m => m.EstInterne);

        var ficheAgent = await c.Fiche.ExecuterAsync(c.AgentId, c.Ticket.Id);
        Assert.Contains(ficheAgent.Messages, m => m.EstInterne);
    }

    /// <summary>
    /// L'ARCHIVE N'OUVRE AUCUN DROIT. Un client d'une autre société ne consulte pas davantage un
    /// ticket archivé qu'un ticket actif — l'archivage ne relâche rien.
    /// </summary>
    [Fact]
    public async Task Droits_de_consultation_toujours_appliques_sur_l_archive()
    {
        var c = await ConstruireAsync();
        using var _ = c.Db;
        await c.Cloturer.ExecuterAsync(c.AgentId, c.Ticket.Id);

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => c.Fiche.ExecuterAsync(c.EtrangerId, c.Ticket.Id));

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => c.Fiche.ExecuterAsync(Guid.NewGuid(), c.Ticket.Id));
    }

    /// <summary>
    /// Un compte DÉSACTIVÉ ne consulte pas non plus l'archive : la garde d'identité s'applique
    /// après la clôture comme avant (P0-ter).
    /// </summary>
    [Fact]
    public async Task Compte_desactive_ne_consulte_pas_l_archive()
    {
        var c = await ConstruireAsync();
        using var _ = c.Db;
        await c.Cloturer.ExecuterAsync(c.AgentId, c.Ticket.Id);

        var compte = c.Db.Users.Single(u => u.Id == c.ClientId);
        compte.EstActif = false;
        c.Db.SaveChanges();

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => c.Fiche.ExecuterAsync(c.ClientId, c.Ticket.Id));
    }

    /// <summary>
    /// La date d'archivage ne se réécrit pas : une anonymisation ultérieure (rétention SEC-06) ne
    /// déplace pas la date à laquelle le ticket a été archivé.
    /// </summary>
    [Fact]
    public async Task Anonymisation_ulterieure_ne_deplace_pas_la_date_d_archivage()
    {
        var c = await ConstruireAsync();
        using var _ = c.Db;
        await c.Cloturer.ExecuterAsync(c.AgentId, c.Ticket.Id);
        var dateArchivage = c.Ticket.DateArchivageUtc;

        c.Ticket.MarquerAnonymise(DateTime.UtcNow.AddYears(5));

        Assert.Equal(dateArchivage, c.Ticket.DateArchivageUtc);
        Assert.True(c.Ticket.EstAnonymise);
    }

    /// <summary>
    /// RG-TCK-12 — un ticket rouvert redevient actif : il n'est plus archivé. Le laisser marqué
    /// archivé le décrirait faussement ; l'historique de la clôture reste, lui, dans le journal.
    /// </summary>
    [Fact]
    public async Task Ticket_rouvert_n_est_plus_archive()
    {
        var c = await ConstruireAsync();
        using var _ = c.Db;
        await c.Cloturer.ExecuterAsync(c.AgentId, c.Ticket.Id);
        Assert.True(c.Ticket.EstArchive);

        c.Ticket.Rouvrir(DateTime.UtcNow);

        Assert.False(c.Ticket.EstArchive);
        Assert.Equal(StatutTicket.EnAttenteDePriseEnCharge, c.Ticket.Statut);
    }
}
