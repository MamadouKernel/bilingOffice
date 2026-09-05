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
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentQuiMentionne), Permission.MessageNoteInterne, fileId));

        var dossier = Dossier.Creer("DOS-MENT-000001", Guid.NewGuid(), horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-MENT-000001", "BillingOffice", "Import", fileId, dossier.Id, horloge.MaintenantUtc);
        ticket.PrendreEnCharge(operateurCourant, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();

        var commande = new MentionnerCollaborateurCommand(
            new MessageRepository(dbContext), ticketRepo, new UtilisateurInfoRepository(dbContext),
            autorisation, journalisation, horloge, uniteDeTravail);

        await commande.MentionnerAsync(agentQuiMentionne, ticket.Id, collaborateurMentionne, "@Nom peux-tu regarder ce dossier ?");

        var ticketApres = await dbContext.Tickets.FindAsync(ticket.Id);
        Assert.Equal(operateurCourant, ticketApres!.OperateurAssigneId); // MSG-05

        var message = dbContext.Messages.Single(m => m.TicketId == ticket.Id);
        Assert.Equal(collaborateurMentionne, message.CollaborateurMentionneId);
    }

    /// <summary>
    /// MSG-04 depuis une fiche : l'agent désigne son collaborateur par son adresse
    /// professionnelle. Aucune interface ne peut demander un GUID à un utilisateur.
    /// </summary>
    [Fact]
    public async Task Mentionner_par_adresse_professionnelle_resout_le_collaborateur()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var ticketRepo = new TicketRepository(dbContext);

        var agentQuiMentionne = Guid.NewGuid();
        var collaborateurId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentQuiMentionne), Permission.MessageNoteInterne, fileId));

        dbContext.Users.Add(new ApplicationUser
        {
            Id = collaborateurId,
            UserName = "collegue@cit.ci",
            Email = "collegue@cit.ci",
            NomAffiche = "Collègue CIT",
            Nature = NatureUtilisateur.Agent,
            EstActif = true,
        });

        var dossier = Dossier.Creer("DOS-MEN-000001", Guid.NewGuid(), horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-MEN-000001", "BillingOffice", "Import", fileId, dossier.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();

        var commande = new MentionnerCollaborateurCommand(
            new MessageRepository(dbContext), ticketRepo, new UtilisateurInfoRepository(dbContext),
            autorisation, journalisation, horloge, uniteDeTravail);

        var message = await commande.MentionnerParEmailAsync(
            agentQuiMentionne, ticket.Id, " collegue@cit.ci ", "Peux-tu regarder ce dossier ?");

        Assert.Equal(collaborateurId, message.CollaborateurMentionneId);
    }

    /// <summary>
    /// MSG-02 : une note interne n'est JAMAIS visible d'un client. Y mentionner un compte client
    /// lui adresserait une notification sur un contenu qu'il ne peut pas lire.
    /// </summary>
    [Fact]
    public async Task Mentionner_un_compte_client_dans_une_note_interne_est_refuse()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var ticketRepo = new TicketRepository(dbContext);

        var agentQuiMentionne = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentQuiMentionne), Permission.MessageNoteInterne, fileId));

        dbContext.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "client@transitaire.ci",
            Email = "client@transitaire.ci",
            NomAffiche = "Client Transitaire",
            Nature = NatureUtilisateur.Client,
            EstActif = true,
        });

        var dossier = Dossier.Creer("DOS-MEN-000002", Guid.NewGuid(), horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-MEN-000002", "BillingOffice", "Import", fileId, dossier.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();

        var commande = new MentionnerCollaborateurCommand(
            new MessageRepository(dbContext), ticketRepo, new UtilisateurInfoRepository(dbContext),
            autorisation, journalisation, horloge, uniteDeTravail);

        await Assert.ThrowsAsync<InvalidOperationException>(() => commande.MentionnerParEmailAsync(
            agentQuiMentionne, ticket.Id, "client@transitaire.ci", "Note interne"));
    }
}
