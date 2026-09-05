using CIT.Plateforme.Application.Dossiers;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Tickets;
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
/// [SÉCURITÉ — P0-bis] Cloisonnement de l'acteur AGENT sur les fiches ticket et dossier.
///
/// DÉFAUT REPRODUIT ICI (`docs/contre-audit-P0-P1-3FN.md` § 3). <c>ObtenirFicheTicketQuery</c> et
/// <c>ObtenirFicheDossierQuery</c> enveloppaient l'intégralité de leur garde dans
/// <c>if (estClient)</c>. La branche agent n'existait pas : tout appelant qui n'était pas de nature
/// <c>Client</c> — y compris un compte agent <b>sans aucune habilitation</b>, y compris un
/// identifiant d'utilisateur inconnu — obtenait la fiche complète du ticket désigné, <b>notes
/// internes CIT comprises</b>.
///
/// Le raisonnement fautif tenait en une phrase : « ce n'est pas un client, donc on laisse passer ».
/// C'est exactement ce que la règle de refus par défaut interdit.
///
/// Le comportement de référence existait déjà dans <c>ObtenirFicheEnrolementQuery</c>, qui garde
/// correctement le cas agent — preuve que l'omission était un défaut et non un choix.
///
/// Règles : RG-CPT-04, RG-SEC-01, RG-SEC-02, RG-MSG-01. CDC : CPT-04, SEC-01, SEC-03.
/// </summary>
public sealed class CloisonnementAgentFichesTests
{
    private sealed record Contexte(
        PlateformeDbContext Db,
        ObtenirFicheTicketQuery FicheTicket,
        ObtenirFicheDossierQuery FicheDossier,
        Guid TicketFileA,
        Guid DossierSociete,
        Guid ClientProprietaire,
        Guid ClientTiers,
        Guid AgentHabiliteFileA,
        Guid AgentHabiliteFileB,
        Guid AgentSansHabilitation,
        Guid AgentDesactive,
        Guid Transporteur,
        Guid TicketTransporteur,
        Guid DossierTransporteur);

    private static Guid Compte(PlateformeDbContext db, string cle, NatureUtilisateur nature, bool actif = true)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"{cle}@p0bis.test",
            NomAffiche = cle,
            Nature = nature,
            EstActif = actif,
        };
        db.Users.Add(user);
        return user.Id;
    }

    private static Contexte Construire()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();

        var fileA = FileDeTraitement.Creer("BillingImport", "File A");
        var fileB = FileDeTraitement.Creer("BillingImport", "File B");
        db.Files.AddRange(fileA, fileB);

        var clientProprietaire = Compte(db, "client-proprio", NatureUtilisateur.Client);
        var clientTiers = Compte(db, "client-tiers", NatureUtilisateur.Client);
        var agentA = Compte(db, "agent-file-a", NatureUtilisateur.Agent);
        var agentB = Compte(db, "agent-file-b", NatureUtilisateur.Agent);
        var agentSans = Compte(db, "agent-sans-habilitation", NatureUtilisateur.Agent);
        var agentDesactive = Compte(db, "agent-desactive", NatureUtilisateur.Agent, actif: false);
        var transporteur = Compte(db, "transporteur", NatureUtilisateur.Client);

        var societe = SocieteCliente.Soumettre(clientProprietaire, "Société P0-bis", TypeClient.Normal, horloge.MaintenantUtc);
        societe.Valider();
        var societeTiers = SocieteCliente.Soumettre(clientTiers, "Société tierce", TypeClient.Normal, horloge.MaintenantUtc);
        societeTiers.Valider();
        db.Societes.AddRange(societe, societeTiers);

        var rattachement = UtilisateurSociete.Rattacher(clientProprietaire, societe.Id, horloge.MaintenantUtc, estAdministrateur: true);
        var rattachementTiers = UtilisateurSociete.Rattacher(clientTiers, societeTiers.Id, horloge.MaintenantUtc, estAdministrateur: true);
        db.UtilisateursSociete.AddRange(rattachement, rattachementTiers);

        db.Habilitations.AddRange(
            HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, agentA), Permission.TicketPrendreEnCharge, fileA.Id),
            HabilitationUtilisateur.Attribuer(agentB, Permission.TicketPrendreEnCharge, fileB.Id),
            // L'agent désactivé est PLEINEMENT habilité : seule la désactivation du compte doit
            // le bloquer (SEC-11), rien d'autre.
            HabilitationUtilisateur.Attribuer(agentDesactive, Permission.TicketPrendreEnCharge, fileA.Id));

        var dossier = Dossier.Creer("DOS-P0BIS-0001", societe.Id, horloge.MaintenantUtc, clientProprietaire, TypeClient.Normal);
        var ticket = Ticket.Creer("BIL-IMP-P0B001", "BillingImport", "Import", fileA.Id, dossier.Id, horloge.MaintenantUtc);
        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);

        // Note interne CIT — jamais visible du client (RG-MSG-01), et qui ne doit pas non plus
        // parvenir à un agent non habilité sur la ressource.
        db.Messages.Add(Message.Publier(ticket.Id, agentA, TypeMessage.NoteInterne, "Note interne confidentielle", horloge.MaintenantUtc));
        db.Messages.Add(Message.Publier(ticket.Id, clientProprietaire, TypeMessage.Externe, "Message visible du client", horloge.MaintenantUtc));

        // Parcours transporteur : dossier personnel, ClientId = utilisateur (aucune société).
        var dossierTransporteur = Dossier.Creer("DOS-P0BIS-0002", transporteur, horloge.MaintenantUtc);
        var ticketTransporteur = Ticket.Creer("ENR-ENR-P0B002", "Enrolement", "Enrolement", fileB.Id, dossierTransporteur.Id, horloge.MaintenantUtc);
        db.Dossiers.Add(dossierTransporteur);
        db.Tickets.Add(ticketTransporteur);

        db.SaveChanges();

        return new Contexte(
            db,
            FabriqueRequetes.FicheTicket(db),
            FabriqueRequetes.FicheDossier(db),
            ticket.Id, dossier.Id,
            clientProprietaire, clientTiers,
            agentA, agentB, agentSans, agentDesactive,
            transporteur, ticketTransporteur.Id, dossierTransporteur.Id);
    }

    // =====================================================================
    // 1 & 2 — LE DÉFAUT : agent sans habilitation
    // =====================================================================

    [Fact]
    public async Task Cas01_Agent_sans_habilitation_ne_lit_aucun_ticket()
    {
        var c = Construire();

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => c.FicheTicket.ExecuterAsync(c.AgentSansHabilitation, c.TicketFileA));
    }

    [Fact]
    public async Task Cas02_Agent_sans_habilitation_ne_lit_aucun_dossier()
    {
        var c = Construire();

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => c.FicheDossier.ExecuterAsync(c.AgentSansHabilitation, c.DossierSociete));
    }

    // =====================================================================
    // 3 & 4 — Habilitation bornée à la file
    // =====================================================================

    [Fact]
    public async Task Cas03_Agent_habilite_sur_la_file_du_ticket_accede()
    {
        var c = Construire();

        var fiche = await c.FicheTicket.ExecuterAsync(c.AgentHabiliteFileA, c.TicketFileA);

        Assert.Equal("BIL-IMP-P0B001", fiche.NumeroTicket);
    }

    [Fact]
    public async Task Cas04_Agent_habilite_sur_une_autre_file_est_refuse()
    {
        var c = Construire();

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => c.FicheTicket.ExecuterAsync(c.AgentHabiliteFileB, c.TicketFileA));
    }

    // =====================================================================
    // 5 à 7 — Compte inactif, absence de rôle, absence de rattachement de file
    // =====================================================================

    /// <summary>SEC-11 — la désactivation d'un compte agent est immédiate. Une habilitation
    /// conservée ne vaut rien sur un compte désactivé.</summary>
    [Fact]
    public async Task Cas05_Agent_desactive_est_refuse_malgre_son_habilitation()
    {
        var c = Construire();

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => c.FicheTicket.ExecuterAsync(c.AgentDesactive, c.TicketFileA));
    }

    /// <summary>La nature « Agent » n'est pas un droit : sans aucune permission, aucun accès.</summary>
    [Fact]
    public async Task Cas06_Agent_sans_aucun_role_est_refuse()
    {
        var c = Construire();

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => c.FicheTicket.ExecuterAsync(c.AgentSansHabilitation, c.TicketTransporteur));
    }

    /// <summary>Habilitation existante mais bornée à une file étrangère au ticket : refus.</summary>
    [Fact]
    public async Task Cas07_Agent_avec_role_mais_sans_rattachement_a_la_file_est_refuse()
    {
        var c = Construire();

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => c.FicheTicket.ExecuterAsync(c.AgentHabiliteFileB, c.TicketFileA));
    }

    // =====================================================================
    // 8 — Dossier dont les tickets sont hors des droits de l'agent
    // =====================================================================

    /// <summary>
    /// RÈGLE MÉTIER RETENUE, explicite : un dossier n'appartient à aucune file — ce sont ses
    /// tickets qui en portent une. Un agent accède donc à un dossier si et seulement s'il accède à
    /// <b>au moins un</b> de ses tickets. Un agent dont aucune file ne recoupe le dossier n'a rien
    /// à y instruire ; le lui ouvrir contredirait RG-SEC-02.
    /// </summary>
    [Fact]
    public async Task Cas08_Agent_habilite_sur_une_autre_file_ne_lit_pas_le_dossier()
    {
        var c = Construire();

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => c.FicheDossier.ExecuterAsync(c.AgentHabiliteFileB, c.DossierSociete));
    }

    [Fact]
    public async Task Cas08bis_Agent_habilite_sur_la_file_d_un_ticket_du_dossier_accede()
    {
        var c = Construire();

        var fiche = await c.FicheDossier.ExecuterAsync(c.AgentHabiliteFileA, c.DossierSociete);

        Assert.Equal("DOS-P0BIS-0001", fiche.NumeroDossier);
    }

    // =====================================================================
    // 9 — Notes internes
    // =====================================================================

    /// <summary>
    /// RG-MSG-01 — une note interne n'est jamais visible du client. Elle ne doit pas non plus
    /// parvenir à un agent qui n'a pas accès à la ressource : le refus porte sur la fiche entière,
    /// donc a fortiori sur les échanges internes qu'elle contient.
    /// </summary>
    [Fact]
    public async Task Cas09_Agent_non_habilite_n_obtient_aucune_note_interne()
    {
        var c = Construire();

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => c.FicheTicket.ExecuterAsync(c.AgentSansHabilitation, c.TicketFileA));
    }

    [Fact]
    public async Task Cas09bis_Agent_habilite_obtient_bien_les_notes_internes()
    {
        var c = Construire();

        var fiche = await c.FicheTicket.ExecuterAsync(c.AgentHabiliteFileA, c.TicketFileA);

        Assert.Contains(fiche.Messages, m => m.EstInterne);
    }

    [Fact]
    public async Task Cas09ter_Le_client_proprietaire_ne_voit_jamais_les_notes_internes()
    {
        var c = Construire();

        var fiche = await c.FicheTicket.ExecuterAsync(c.ClientProprietaire, c.TicketFileA);

        Assert.DoesNotContain(fiche.Messages, m => m.EstInterne);
        Assert.Contains(fiche.Messages, m => !m.EstInterne);
    }

    // =====================================================================
    // 10 à 12 — Non-régression client et transporteur
    // =====================================================================

    [Fact]
    public async Task Cas10_Client_proprietaire_accede_toujours()
    {
        var c = Construire();

        var fiche = await c.FicheTicket.ExecuterAsync(c.ClientProprietaire, c.TicketFileA);

        Assert.Equal("BIL-IMP-P0B001", fiche.NumeroTicket);
    }

    [Fact]
    public async Task Cas11_Client_non_proprietaire_est_refuse()
    {
        var c = Construire();

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => c.FicheTicket.ExecuterAsync(c.ClientTiers, c.TicketFileA));
    }

    [Fact]
    public async Task Cas12_Transporteur_ne_lit_pas_le_ticket_d_un_client()
    {
        var c = Construire();

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => c.FicheTicket.ExecuterAsync(c.Transporteur, c.TicketFileA));
    }

    /// <summary>
    /// Réserve R-02 du rapport P0 : le transporteur ne pouvait pas ouvrir son PROPRE ticket
    /// d'enrôlement par cette requête, faute de reconnaître la propriété personnelle du dossier.
    /// Le comportement était fail-closed — sans risque, mais fonctionnellement faux.
    /// </summary>
    [Fact]
    public async Task Cas12bis_Transporteur_accede_a_son_propre_ticket()
    {
        var c = Construire();

        var fiche = await c.FicheTicket.ExecuterAsync(c.Transporteur, c.TicketTransporteur);

        Assert.Equal("ENR-ENR-P0B002", fiche.NumeroTicket);
    }

    /// <summary>Un identifiant d'utilisateur inconnu ne doit rien ouvrir — il n'était pas de nature
    /// Client, donc l'ancien contrôle le laissait passer.</summary>
    [Fact]
    public async Task Cas13_Utilisateur_inconnu_est_refuse()
    {
        var c = Construire();

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => c.FicheTicket.ExecuterAsync(Guid.NewGuid(), c.TicketFileA));
    }
}
