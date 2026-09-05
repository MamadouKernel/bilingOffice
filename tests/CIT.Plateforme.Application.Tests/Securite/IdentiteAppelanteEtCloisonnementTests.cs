using CIT.Plateforme.Application.Messagerie;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Messagerie;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Securite;

/// <summary>
/// [SÉCURITÉ — CONSOLIDATION] L'IDENTITÉ QUI DÉCIDE VIENT DU SERVEUR, JAMAIS DE L'APPELANT.
///
/// CE QUE CETTE SUITE DÉMONTRE. <c>CloisonnementService</c> décidait seul de la propriété d'un
/// ticket avec la règle <c>dossier.ClientId == clientId</c>. Or <c>Dossier.ClientId</c> porte la
/// SOCIÉTÉ pour les parcours Billing, Réclamation et Avoir, et l'UTILISATEUR pour l'Enrôlement.
/// Comparer un identifiant d'utilisateur à une société échouait donc systématiquement sur les
/// parcours société — et l'interface a contourné l'obstacle en passant <c>_fiche.SocieteId</c> à la
/// place de l'utilisateur (<c>FicheTicket.razor</c>).
///
/// DEUX CONSÉQUENCES, TOUTES DEUX ÉPROUVÉES ICI :
/// <list type="number">
///   <item>Le contrôle devient TAUTOLOGIQUE — la valeur comparée provient de la ressource
///   elle-même. Fournir la société d'un dossier suffit à en obtenir l'accès.</item>
///   <item>L'AUTEUR du message devient la société, non la personne. La traçabilité nominative
///   exigée par RG-SEC-03 est perdue, et ENT-03 (RG-CPT-07) devient inapplicable : aucun
///   filtre par auteur ne peut s'appliquer à un auteur qui est une société.</item>
/// </list>
///
/// Aucun test ci-dessous ne passe par l'interface : une garde d'interface n'est pas une frontière.
/// </summary>
public sealed class IdentiteAppelanteEtCloisonnementTests
{
    // ---------------------------------------------------------------------
    // Fixture — deux sociétés, trois collaborateurs, deux tickets
    // ---------------------------------------------------------------------

    private sealed record Contexte(
        PlateformeDbContext Db,
        Guid SocieteA,
        Guid SocieteB,
        Guid ClientA,          // société A, voit toute la société
        Guid CollaborateurA2,  // société A, restreint à ses propres demandes
        Guid ClientB,          // société B
        Ticket TicketA,        // dossier société A, déposé par ClientA
        Ticket TicketB);       // dossier société B

    private static Guid AjouterCompte(PlateformeDbContext db, string cle, NatureUtilisateur nature = NatureUtilisateur.Client)
    {
        var compte = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"{cle}@consolidation.test",
            Email = $"{cle}@consolidation.test",
            NomAffiche = cle,
            Nature = nature,
            EstActif = true,
        };
        db.Users.Add(compte);
        db.SaveChanges();
        return compte.Id;
    }

    private static Contexte Preparer()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var societeA = Guid.NewGuid();
        var societeB = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        db.Files.Add(FileDeTraitement.Creer("IMPORT", "Pilote Manifeste"));

        var clientA = AjouterCompte(db, "client-a");
        var collaborateurA2 = AjouterCompte(db, "collaborateur-a2");
        var clientB = AjouterCompte(db, "client-b");

        // ClientA voit toute la société ; CollaborateurA2 est restreint à ses propres demandes.
        var rattachA = UtilisateurSociete.Rattacher(clientA, societeA, DateTime.UtcNow, false, true);
        rattachA.ModifierDroits(DroitConsultation.ToutesDemandesSociete, droitSoumission: true);
        db.UtilisateursSociete.Add(rattachA);
        db.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(collaborateurA2, societeA, DateTime.UtcNow, false, true));
        db.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(clientB, societeB, DateTime.UtcNow, false, true));

        // Parcours Billing : Dossier.ClientId porte la SOCIÉTÉ, l'auteur est la personne.
        var dossierA = Dossier.Creer("DOS-CONSO-0001", societeA, DateTime.UtcNow, clientA);
        var ticketA = Ticket.Creer("TCK-CONSO-0001", "IMPORT", "STANDARD", fileId, dossierA.Id, DateTime.UtcNow, "BL-A");
        var dossierB = Dossier.Creer("DOS-CONSO-0002", societeB, DateTime.UtcNow, clientB);
        var ticketB = Ticket.Creer("TCK-CONSO-0002", "IMPORT", "STANDARD", fileId, dossierB.Id, DateTime.UtcNow, "BL-B");
        db.Dossiers.AddRange(dossierA, dossierB);
        db.Tickets.AddRange(ticketA, ticketB);
        db.SaveChanges();

        return new Contexte(db, societeA, societeB, clientA, collaborateurA2, clientB, ticketA, ticketB);
    }

    private static PublierMessageCommand Publier(PlateformeDbContext db) => new(
        new MessageRepository(db),
        new TicketRepository(db),
        new AutorisationService(db),
        new CloisonnementService(new TicketRepository(db), CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.ControleAcces(db)),
        new JournalisationService(db, new HorlogeSysteme()),
        new HorlogeSysteme(),
        new UniteDeTravail(db));

    // ---------------------------------------------------------------------
    // Cas 1 & 2 — l'appelant ne choisit pas l'identité qui l'autorise
    // ---------------------------------------------------------------------

    /// <summary>
    /// Cas 1 — le client A agit sur le ticket B en fournissant l'identifiant de B. Refus attendu.
    /// </summary>
    [Fact]
    public async Task Cas01_Client_agissant_sur_le_ticket_d_un_tiers_est_refuse()
    {
        var c = Preparer();
        using var db = c.Db;

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => Publier(db).PublierMessageClientAsync(c.ClientA, c.TicketB.Id, "tentative"));
    }

    /// <summary>
    /// Cas 2 — LE CAS DÉCISIF. Le client A fournit la SOCIÉTÉ B comme identité appelante. C'est la
    /// forme exacte du contournement présent dans l'interface : l'appelant produit la valeur à
    /// laquelle le contrôle se compare, et le contrôle ne peut alors que l'accepter.
    /// </summary>
    [Fact]
    public async Task Cas02_Une_societe_fournie_comme_identite_n_autorise_rien()
    {
        var c = Preparer();
        using var db = c.Db;

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => Publier(db).PublierMessageClientAsync(c.SocieteB, c.TicketB.Id, "tentative"));

        // Y compris sur sa propre société : une société n'est pas un compte, elle n'agit pas.
        await Assert.ThrowsAsync<AccesRefuseException>(
            () => Publier(db).PublierMessageClientAsync(c.SocieteA, c.TicketA.Id, "tentative"));
    }

    // ---------------------------------------------------------------------
    // Cas 3 & 4 — ENT-03
    // ---------------------------------------------------------------------

    /// <summary>
    /// Cas 3 — ENT-03 (RG-CPT-07). Un collaborateur restreint à ses propres demandes ne peut pas
    /// agir sur le ticket d'un collègue, bien qu'ils partagent la même société.
    /// </summary>
    [Fact]
    public async Task Cas03_Collaborateur_restreint_ne_peut_pas_agir_sur_le_ticket_d_un_collegue()
    {
        var c = Preparer();
        using var db = c.Db;

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => Publier(db).PublierMessageClientAsync(c.CollaborateurA2, c.TicketA.Id, "tentative"));
    }

    /// <summary>
    /// Cas 4 — le pendant du cas 3 : un collaborateur autorisé sur toute la société agit bien.
    /// Sans ce témoin, une correction qui refuserait tout passerait le cas 3.
    /// </summary>
    [Fact]
    public async Task Cas04_Collaborateur_autorise_sur_la_societe_peut_agir()
    {
        var c = Preparer();
        using var db = c.Db;

        var message = await Publier(db).PublierMessageClientAsync(c.ClientA, c.TicketA.Id, "bonjour");

        Assert.NotNull(message);
    }

    // ---------------------------------------------------------------------
    // Cas 5 & 6 — l'auteur du message
    // ---------------------------------------------------------------------

    /// <summary>
    /// Cas 5 — l'auteur enregistré est l'UTILISATEUR RÉEL. RG-SEC-03 exige que toute action
    /// significative soit tracée ; une trace attribuée à une société ne désigne personne.
    /// </summary>
    [Fact]
    public async Task Cas05_L_auteur_du_message_est_l_utilisateur_reel()
    {
        var c = Preparer();
        using var db = c.Db;

        var message = await Publier(db).PublierMessageClientAsync(c.ClientA, c.TicketA.Id, "bonjour");

        Assert.Equal(c.ClientA, message.AuteurId);
        Assert.NotEqual(c.SocieteA, message.AuteurId);
    }

    /// <summary>
    /// Cas 6 — la société du dossier ne remplace jamais l'identité de l'auteur. Vérifié sur la
    /// trace persistée, non sur la valeur retournée.
    /// </summary>
    [Fact]
    public async Task Cas06_La_societe_du_dossier_ne_devient_jamais_l_auteur()
    {
        var c = Preparer();
        using var db = c.Db;

        await Publier(db).PublierMessageClientAsync(c.ClientA, c.TicketA.Id, "bonjour");

        var messages = db.Messages.Where(m => m.TicketId == c.TicketA.Id).ToList();
        Assert.Single(messages);
        Assert.Equal(c.ClientA, messages[0].AuteurId);
        Assert.DoesNotContain(messages, m => m.AuteurId == c.SocieteA);

        var historique = db.HistoriqueTickets.Where(h => h.TicketId == c.TicketA.Id).ToList();
        Assert.All(historique, h => Assert.NotEqual(c.SocieteA, h.AuteurId));
    }

    // ---------------------------------------------------------------------
    // Cas 7 & 8 — passage obligé par la garde centrale
    // ---------------------------------------------------------------------

    /// <summary>
    /// Cas 7 — une commande passant par <c>CloisonnementService</c> traverse la garde centrale.
    /// Preuve par l'effet : un compte DÉSACTIVÉ est refusé, ce que l'ancienne règle de propriété
    /// ne regardait pas.
    /// </summary>
    [Fact]
    public async Task Cas07_Un_compte_desactive_ne_peut_plus_publier_de_message_client()
    {
        var c = Preparer();
        using var db = c.Db;

        db.Users.Single(u => u.Id == c.ClientA).EstActif = false;
        db.SaveChanges();

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => Publier(db).PublierMessageClientAsync(c.ClientA, c.TicketA.Id, "tentative"));
    }

    /// <summary>
    /// Cas 8 — un identifiant inconnu, qui n'est ni un compte ni une société, est refusé. Ferme la
    /// dernière porte du contrôle par simple égalité.
    /// </summary>
    [Fact]
    public async Task Cas08_Un_identifiant_inconnu_est_refuse()
    {
        var c = Preparer();
        using var db = c.Db;

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => Publier(db).PublierMessageClientAsync(Guid.NewGuid(), c.TicketA.Id, "tentative"));
    }

    /// <summary>
    /// Cas 9 — le transporteur conserve l'accès à son dossier PERSONNEL d'Enrôlement, où
    /// <c>Dossier.ClientId</c> porte l'utilisateur. La centralisation ne doit pas fermer cette voie.
    /// </summary>
    [Fact]
    public async Task Cas09_Le_transporteur_agit_sur_son_dossier_personnel()
    {
        var c = Preparer();
        using var db = c.Db;

        var transporteur = AjouterCompte(db, "transporteur");
        var fileId = db.Files.First().Id;
        var dossier = Dossier.Creer("DOS-CONSO-0003", transporteur, DateTime.UtcNow, transporteur);
        var ticket = Ticket.Creer("TCK-CONSO-0003", "Enrolement", "Enrolement", fileId, dossier.Id, DateTime.UtcNow, null);
        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);
        db.SaveChanges();

        var message = await Publier(db).PublierMessageClientAsync(transporteur, ticket.Id, "ma piece");

        Assert.Equal(transporteur, message.AuteurId);
    }
}
