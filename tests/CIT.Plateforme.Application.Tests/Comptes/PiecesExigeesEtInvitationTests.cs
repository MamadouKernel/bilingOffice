using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Comptes;

/// <summary>
/// CPT-01 (Must) — « Création de compte client soumise à validation par CIT avant activation ;
/// les informations et justificatifs exigés sont PARAMÉTRABLES. »
///
/// ENT-01 (Must) — « Une société cliente peut rattacher plusieurs utilisateurs à son compte. »
///
/// Ces deux exigences étaient portées à vide : la demande se réduisait à un champ de texte libre
/// sans contrôle possible, et le rattachement supposait un compte préexistant qu'aucun parcours
/// ne permettait d'obtenir sans ouvrir une société parasite.
/// </summary>
public sealed class PiecesExigeesEtInvitationTests
{
    private sealed record Contexte(
        PlateformeDbContext DbContext,
        HorlogeDeTest Horloge,
        SoumettreDemandeCompteCommand Soumettre,
        GererPiecesExigeesCommand GererPieces,
        InviterCollaborateurCommand Inviter,
        AccepterInvitationCommand Accepter,
        ConsulterInvitationQuery Consulter,
        RevoquerInvitationCommand Revoquer);

    private static Contexte Construire()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journal = new JournalisationService(dbContext, horloge);
        var uow = new UniteDeTravail(dbContext);

        var pieces = new PieceExigeeCompteRepository(dbContext);
        var justificatifs = new JustificatifDemandeCompteRepository(dbContext);
        var societes = new SocieteClienteRepository(dbContext);
        var rattachements = new UtilisateurSocieteRepository(dbContext);
        var invitations = new InvitationCollaborateurRepository(dbContext);
        var utilisateurs = new UtilisateurInfoRepository(dbContext);
        var autorisation = new AutorisationService(dbContext);

        return new Contexte(
            dbContext,
            horloge,
            new SoumettreDemandeCompteCommand(societes, pieces, justificatifs, journal, horloge, uow),
            new GererPiecesExigeesCommand(pieces, autorisation, journal, uow),
            new InviterCollaborateurCommand(invitations, rattachements, utilisateurs, journal, horloge, uow),
            new AccepterInvitationCommand(invitations, rattachements, societes, journal, horloge, uow),
            new ConsulterInvitationQuery(invitations, societes, horloge),
            new RevoquerInvitationCommand(invitations, rattachements, journal, uow));
    }

    private static Guid AjouterAdministrateurCit(PlateformeDbContext dbContext)
    {
        var id = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, id), Permission.AdministrationComptes));
        dbContext.SaveChanges();
        return id;
    }

    private static JustificatifDepose Depot(string pieceCode) =>
        new(pieceCode, $"{pieceCode}.pdf", "application/pdf", 1024, $"cle/{pieceCode}");

    // ═══════════════════ CPT-01 — justificatifs paramétrables ═══════════════════

    /// <summary>
    /// État initial du produit : aucune pièce n'est livrée par défaut, le CDC n'en nommant aucune.
    /// Le contrôle ne doit donc rien bloquer tant que le métier n'a rien paramétré.
    /// </summary>
    [Fact]
    public async Task Sans_piece_parametree_la_demande_reste_recevable()
    {
        var c = Construire();

        var societe = await c.Soumettre.ExecuterAsync(
            new SoumettreDemandeCompteRequete(Guid.NewGuid(), "Informations", TypeClient.Normal));

        Assert.Equal(EtatSociete.EnAttenteValidation, societe.Etat);
    }

    [Fact]
    public async Task Une_piece_obligatoire_absente_rend_le_dossier_incomplet()
    {
        var c = Construire();
        var adminId = AjouterAdministrateurCit(c.DbContext);
        await c.GererPieces.DefinirAsync(adminId, "rccm", "Registre de Commerce", obligatoire: true, ordre: 1);

        var exception = await Assert.ThrowsAsync<DossierIncompletException>(
            () => c.Soumettre.ExecuterAsync(
                new SoumettreDemandeCompteRequete(Guid.NewGuid(), "Informations", TypeClient.Normal)));

        // Le refus nomme ce qui manque : un message opaque laisserait le demandeur sans recours.
        Assert.Contains("Registre de Commerce", exception.PiecesManquantes);
        Assert.Empty(c.DbContext.Societes);
    }

    [Fact]
    public async Task Une_piece_obligatoire_fournie_rend_le_dossier_recevable_et_conserve_le_depot()
    {
        var c = Construire();
        var adminId = AjouterAdministrateurCit(c.DbContext);
        await c.GererPieces.DefinirAsync(adminId, "rccm", "Registre de Commerce", obligatoire: true, ordre: 1);

        var societe = await c.Soumettre.ExecuterAsync(new SoumettreDemandeCompteRequete(
            Guid.NewGuid(), "Informations", TypeClient.Normal, [Depot("rccm")]));

        Assert.Equal(EtatSociete.EnAttenteValidation, societe.Etat);
        var depose = Assert.Single(c.DbContext.JustificatifsDemandeCompte.Where(j => j.SocieteId == societe.Id));
        Assert.Equal("rccm", depose.PieceCode);
    }

    [Fact]
    public async Task Une_piece_facultative_absente_ne_bloque_pas()
    {
        var c = Construire();
        var adminId = AjouterAdministrateurCit(c.DbContext);
        await c.GererPieces.DefinirAsync(adminId, "logo", "Logo de l'entreprise", obligatoire: false, ordre: 1);

        var societe = await c.Soumettre.ExecuterAsync(
            new SoumettreDemandeCompteRequete(Guid.NewGuid(), "Informations", TypeClient.Normal));

        Assert.Equal(EtatSociete.EnAttenteValidation, societe.Etat);
    }

    /// <summary>
    /// Une pièce retirée du parcours est désactivée, jamais supprimée — les dossiers déjà déposés
    /// la référencent. Elle ne doit plus peser sur les nouvelles demandes pour autant.
    /// </summary>
    [Fact]
    public async Task Une_piece_desactivee_sort_du_controle_de_completude()
    {
        var c = Construire();
        var adminId = AjouterAdministrateurCit(c.DbContext);
        var pieceId = await c.GererPieces.DefinirAsync(adminId, "ancienne", "Pièce abandonnée", obligatoire: true, ordre: 1);

        await c.GererPieces.DefinirActivationAsync(adminId, pieceId, actif: false);

        var societe = await c.Soumettre.ExecuterAsync(
            new SoumettreDemandeCompteRequete(Guid.NewGuid(), "Informations", TypeClient.Normal));

        Assert.Equal(EtatSociete.EnAttenteValidation, societe.Etat);
    }

    [Fact]
    public async Task Le_parametrage_des_pieces_est_reserve_a_l_administration()
    {
        var c = Construire();

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => c.GererPieces.DefinirAsync(Guid.NewGuid(), "rccm", "Registre", true, 1));
    }

    [Fact]
    public async Task Un_code_de_piece_en_doublon_est_refuse()
    {
        var c = Construire();
        var adminId = AjouterAdministrateurCit(c.DbContext);
        await c.GererPieces.DefinirAsync(adminId, "rccm", "Registre de Commerce", true, 1);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => c.GererPieces.DefinirAsync(adminId, "rccm", "Autre libellé", true, 2));
    }

    /// <summary>
    /// MCT opération 2 / MOT étape 3 — « confirmation ou CORRECTION du profil déclaré » (CPT-02).
    ///
    /// La correction fait partie de l'INSTRUCTION : elle doit donc être possible tant que la
    /// demande est en attente. Elle ne l'était pas en pratique — l'écran de validation affichait
    /// le profil sans permettre de le changer, et l'écran de modification du type ne proposait
    /// que les sociétés déjà actives. Corriger ne vaut pas décider : l'état reste inchangé.
    /// </summary>
    [Fact]
    public async Task Le_profil_declare_est_corrigible_pendant_l_instruction()
    {
        var c = Construire();
        var adminId = Guid.NewGuid();
        c.DbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(c.DbContext, adminId), Permission.CompteTypeModifier));
        await c.DbContext.SaveChangesAsync();

        var societe = await c.Soumettre.ExecuterAsync(
            new SoumettreDemandeCompteRequete(Guid.NewGuid(), "ACME", TypeClient.Normal));

        var modifier = new ModifierTypeClientCommand(
            new SocieteClienteRepository(c.DbContext),
            new AutorisationService(c.DbContext),
            new JournalisationService(c.DbContext, c.Horloge),
            new UniteDeTravail(c.DbContext));

        await modifier.ExecuterAsync(adminId, societe.Id, TypeClient.IES);

        var relue = c.DbContext.Societes.Single(x => x.Id == societe.Id);
        Assert.Equal(TypeClient.IES, relue.Profil);
        Assert.Equal(EtatSociete.EnAttenteValidation, relue.Etat);
    }

    // ═══════════════════ ENT-01 — parcours d'invitation ═══════════════════

    private static (Guid SocieteId, Guid AdminId) SocieteActiveAvecAdministrateur(PlateformeDbContext dbContext)
    {
        var adminId = Guid.NewGuid();
        var societe = SocieteCliente.Soumettre(adminId, "ACME Transit", TypeClient.Normal, new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc));
        societe.Valider();
        societe.EnregistrerAdministrateurActif();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, adminId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc), estAdministrateur: true));
        dbContext.SaveChanges();
        return (societe.Id, adminId);
    }

    [Fact]
    public async Task Seul_un_administrateur_de_la_societe_peut_inviter()
    {
        var c = Construire();
        var (societeId, _) = SocieteActiveAvecAdministrateur(c.DbContext);

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => c.Inviter.ExecuterAsync(Guid.NewGuid(), societeId, "invite@acme.test", TimeSpan.FromDays(7)));
    }

    /// <summary>
    /// LE CAS DÉCISIF : l'invité obtient un compte ET son rattachement, sans qu'aucune société
    /// supplémentaire ne soit créée. C'est exactement ce que l'auto-inscription ne savait pas faire.
    /// </summary>
    [Fact]
    public async Task L_acceptation_rattache_a_la_societe_sans_creer_de_societe_supplementaire()
    {
        var c = Construire();
        var (societeId, adminId) = SocieteActiveAvecAdministrateur(c.DbContext);
        var societesAvant = c.DbContext.Societes.Count();

        var invitation = await c.Inviter.ExecuterAsync(adminId, societeId, "invite@acme.test", TimeSpan.FromDays(7));

        var vue = await c.Consulter.ExecuterAsync(invitation.Jeton);
        Assert.NotNull(vue);
        Assert.Equal("invite@acme.test", vue!.Email);

        var invitéId = Guid.NewGuid();
        var rattachement = await c.Accepter.ExecuterAsync(invitation.Jeton, invitéId, "invite@acme.test");

        Assert.Equal(societeId, rattachement.SocieteId);
        Assert.False(rattachement.EstAdministrateur);
        Assert.Equal(societesAvant, c.DbContext.Societes.Count());
    }

    [Fact]
    public async Task Une_invitation_ne_vaut_que_pour_l_adresse_a_laquelle_elle_est_destinee()
    {
        var c = Construire();
        var (societeId, adminId) = SocieteActiveAvecAdministrateur(c.DbContext);
        var invitation = await c.Inviter.ExecuterAsync(adminId, societeId, "invite@acme.test", TimeSpan.FromDays(7));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => c.Accepter.ExecuterAsync(invitation.Jeton, Guid.NewGuid(), "intrus@ailleurs.test"));
    }

    [Fact]
    public async Task Une_invitation_ne_sert_qu_une_fois()
    {
        var c = Construire();
        var (societeId, adminId) = SocieteActiveAvecAdministrateur(c.DbContext);
        var invitation = await c.Inviter.ExecuterAsync(adminId, societeId, "invite@acme.test", TimeSpan.FromDays(7));

        await c.Accepter.ExecuterAsync(invitation.Jeton, Guid.NewGuid(), "invite@acme.test");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => c.Accepter.ExecuterAsync(invitation.Jeton, Guid.NewGuid(), "invite@acme.test"));
    }

    [Fact]
    public async Task Une_invitation_expiree_n_est_plus_utilisable()
    {
        var c = Construire();
        var (societeId, adminId) = SocieteActiveAvecAdministrateur(c.DbContext);
        var invitation = await c.Inviter.ExecuterAsync(adminId, societeId, "invite@acme.test", TimeSpan.FromDays(7));

        c.Horloge.MaintenantUtc = c.Horloge.MaintenantUtc.AddDays(8);

        Assert.Null(await c.Consulter.ExecuterAsync(invitation.Jeton));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => c.Accepter.ExecuterAsync(invitation.Jeton, Guid.NewGuid(), "invite@acme.test"));
    }

    [Fact]
    public async Task Une_invitation_revoquee_n_est_plus_utilisable()
    {
        var c = Construire();
        var (societeId, adminId) = SocieteActiveAvecAdministrateur(c.DbContext);
        var invitation = await c.Inviter.ExecuterAsync(adminId, societeId, "invite@acme.test", TimeSpan.FromDays(7));

        await c.Revoquer.ExecuterAsync(adminId, invitation.Id);

        Assert.Null(await c.Consulter.ExecuterAsync(invitation.Jeton));
    }

    [Fact]
    public async Task Un_jeton_inconnu_ne_revele_rien()
    {
        var c = Construire();
        SocieteActiveAvecAdministrateur(c.DbContext);

        Assert.Null(await c.Consulter.ExecuterAsync("jeton-qui-n-existe-pas"));
    }

    /// <summary>
    /// Inviter quelqu'un qui possède déjà un compte produirait un second compte pour la même
    /// personne : le rattachement direct est la voie correcte dans ce cas.
    /// </summary>
    [Fact]
    public async Task Inviter_une_adresse_deja_titulaire_d_un_compte_est_refuse()
    {
        var c = Construire();
        var (societeId, adminId) = SocieteActiveAvecAdministrateur(c.DbContext);
        c.DbContext.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "deja@acme.test",
            Email = "deja@acme.test",
            NomAffiche = "Déjà inscrit",
            Nature = NatureUtilisateur.Client,
        });
        await c.DbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => c.Inviter.ExecuterAsync(adminId, societeId, "deja@acme.test", TimeSpan.FromDays(7)));
    }

    /// <summary>Le jeton est un secret porteur : deux invitations n'en partagent jamais un.</summary>
    [Fact]
    public async Task Deux_invitations_portent_des_jetons_distincts()
    {
        var c = Construire();
        var (societeId, adminId) = SocieteActiveAvecAdministrateur(c.DbContext);

        var premiere = await c.Inviter.ExecuterAsync(adminId, societeId, "un@acme.test", TimeSpan.FromDays(7));
        var seconde = await c.Inviter.ExecuterAsync(adminId, societeId, "deux@acme.test", TimeSpan.FromDays(7));

        Assert.NotEqual(premiere.Jeton, seconde.Jeton);
        Assert.True(premiere.Jeton.Length >= 40);
    }
}
