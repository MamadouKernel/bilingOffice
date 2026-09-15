using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Audit;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Securite;

/// <summary>
/// REMÉDIATION UI/UX (D-03) — LE JOURNAL D'AUDIT DOIT IMPUTER SES ENTRÉES.
///
/// Les 111 entrées de la recette affichaient toutes le libellé « Utilisateur » : un registre
/// d'imputabilité qui n'impute personne. La donnée existait (<c>AuteurId</c>) mais n'était jamais
/// résolue. Ces tests fixent la résolution — et sa règle cardinale : l'identifiant reste la clé
/// stable, jamais remplacée par un nom qui peut changer.
/// </summary>
public sealed class ResolutionAuteurJournalAuditTests
{
    private static JournalAuditLecture Lecteur(PlateformeDbContext db) =>
        new(db, new UtilisateurInfoRepository(db));

    private static ApplicationUser Utilisateur(
        Guid id, string nom, string email, NatureUtilisateur nature, bool actif = true) => new()
    {
        Id = id,
        UserName = email,
        Email = email,
        NomAffiche = nom,
        Nature = nature,
        EstActif = actif,
    };

    private static void Journaliser(PlateformeDbContext db, Guid? auteurId, string action) =>
        db.JournalAudit.Add(EntreeJournalAudit.Creer(
            auteurId, action, "Ticket", Guid.NewGuid(), null, null, DateTime.UtcNow));

    // ===================================================================================
    // Résolution des trois natures d'auteur
    // ===================================================================================

    [Fact]
    public async Task Une_action_de_client_est_imputee_au_client_nomme()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var clientId = Guid.NewGuid();
        db.Users.Add(Utilisateur(clientId, "Awa Koné", "awa.kone@societe.test", NatureUtilisateur.Client));
        Journaliser(db, clientId, "TicketCree");
        await db.SaveChangesAsync();

        var entree = Assert.Single(await Lecteur(db).ListerAsync(null));

        Assert.Equal("Awa Koné", entree.AuteurLibelle);
        Assert.Equal(NatureUtilisateur.Client, entree.AuteurNature);
        Assert.Equal(clientId, entree.AuteurId);
        Assert.False(entree.AuteurIntrouvable);
    }

    [Fact]
    public async Task Une_action_d_agent_est_imputee_a_l_agent_nomme()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var agentId = Guid.NewGuid();
        db.Users.Add(Utilisateur(agentId, "Kouassi N'Guessan", "kouassi@cit.test", NatureUtilisateur.Agent));
        Journaliser(db, agentId, "TicketPrisEnCharge");
        await db.SaveChangesAsync();

        var entree = Assert.Single(await Lecteur(db).ListerAsync(null));

        Assert.Equal("Kouassi N'Guessan", entree.AuteurLibelle);
        Assert.Equal(NatureUtilisateur.Agent, entree.AuteurNature);
    }

    [Fact]
    public async Task Une_action_d_administrateur_est_imputee_a_l_administrateur_nomme()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var adminId = Guid.NewGuid();
        db.Users.Add(Utilisateur(adminId, "Admin CIT", "admin@cit.test", NatureUtilisateur.Agent));
        Journaliser(db, adminId, "HabilitationsAccordeesEnMasse");
        await db.SaveChangesAsync();

        var entree = Assert.Single(await Lecteur(db).ListerAsync(null));

        Assert.Equal("Admin CIT", entree.AuteurLibelle);
    }

    // ===================================================================================
    // Cas limites — le socle, le compte désactivé, l'auteur supprimé, l'opération en masse
    // ===================================================================================

    [Fact]
    public async Task Une_action_sans_auteur_est_imputee_au_socle()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        Journaliser(db, null, "PurgeAutomatique");
        await db.SaveChangesAsync();

        var entree = Assert.Single(await Lecteur(db).ListerAsync(null));

        Assert.Null(entree.AuteurId);
        Assert.Equal("Système CIT", entree.AuteurLibelle);
        Assert.False(entree.AuteurIntrouvable);
    }

    [Fact]
    public async Task Un_compte_desactive_reste_impute_normalement()
    {
        // La désactivation ne fait pas disparaître l'imputabilité passée : le compte existe encore.
        var db = ContexteDeTest.CreerNouveauContexte();
        var desactiveId = Guid.NewGuid();
        db.Users.Add(Utilisateur(desactiveId, "Ex-Collaborateur", "ex@cit.test", NatureUtilisateur.Agent, actif: false));
        Journaliser(db, desactiveId, "TicketCloture");
        await db.SaveChangesAsync();

        var entree = Assert.Single(await Lecteur(db).ListerAsync(null));

        Assert.Equal("Ex-Collaborateur", entree.AuteurLibelle);
        Assert.False(entree.AuteurIntrouvable);
        Assert.Equal(desactiveId, entree.AuteurId);
    }

    [Fact]
    public async Task Un_auteur_dont_le_compte_est_introuvable_garde_son_identifiant()
    {
        // L'imputabilité ne s'efface pas : l'entrée reste attribuée à son identifiant, présenté
        // comme « compte supprimé » — jamais anonymisée.
        var db = ContexteDeTest.CreerNouveauContexte();
        var fantomeId = Guid.NewGuid();
        Journaliser(db, fantomeId, "TicketReouvert"); // aucun utilisateur correspondant
        await db.SaveChangesAsync();

        var entree = Assert.Single(await Lecteur(db).ListerAsync(null));

        Assert.Equal(fantomeId, entree.AuteurId);
        Assert.True(entree.AuteurIntrouvable);
        Assert.Equal("Compte supprimé", entree.AuteurLibelle);
    }

    [Fact]
    public async Task Une_operation_en_masse_impute_toutes_ses_entrees_au_meme_auteur()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var adminId = Guid.NewGuid();
        db.Users.Add(Utilisateur(adminId, "Admin Bulk", "bulk@cit.test", NatureUtilisateur.Agent));
        for (var i = 0; i < 54; i++)
        {
            Journaliser(db, adminId, "HabilitationAccordee");
        }
        Journaliser(db, adminId, "HabilitationsAccordeesEnMasse");
        await db.SaveChangesAsync();

        var entrees = await Lecteur(db).ListerAsync(null);

        Assert.Equal(55, entrees.Count);
        Assert.All(entrees, e =>
        {
            Assert.Equal("Admin Bulk", e.AuteurLibelle);
            Assert.Equal(adminId, e.AuteurId);
        });
    }

    [Fact]
    public async Task La_lecture_paginee_resout_aussi_les_auteurs()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var agentId = Guid.NewGuid();
        db.Users.Add(Utilisateur(agentId, "Paginé", "page@cit.test", NatureUtilisateur.Agent));
        for (var i = 0; i < 12; i++)
        {
            Journaliser(db, agentId, $"Action{i:D2}");
        }
        await db.SaveChangesAsync();

        var (entrees, total) = await Lecteur(db).ListerPageAsync(entiteId: null, page: 1, taillePage: 10);

        Assert.Equal(12, total);
        Assert.Equal(10, entrees.Count);
        Assert.All(entrees, e => Assert.Equal("Paginé", e.AuteurLibelle));
    }

    // ===================================================================================
    // Le libellé — priorité nom, puis e-mail, puis « compte supprimé »
    // ===================================================================================

    [Theory]
    [InlineData("Awa Koné", "awa@test.ci", "Awa Koné")]
    [InlineData("", "awa@test.ci", "awa@test.ci")]
    [InlineData(null, "awa@test.ci", "awa@test.ci")]
    public void Le_libelle_prefere_le_nom_puis_l_email(string? nom, string email, string attendu)
    {
        var vue = new EntreeJournalAuditVue(
            Guid.NewGuid(), "Action", "Ticket", Guid.NewGuid(), DateTime.UtcNow,
            AuteurNomAffiche: nom, AuteurEmail: email, AuteurNature: NatureUtilisateur.Client);

        Assert.Equal(attendu, vue.AuteurLibelle);
    }

    [Fact]
    public void Un_auteur_present_sans_identite_resolue_est_dit_introuvable()
    {
        var vue = new EntreeJournalAuditVue(
            Guid.NewGuid(), "Action", "Ticket", Guid.NewGuid(), DateTime.UtcNow);

        Assert.True(vue.AuteurIntrouvable);
        Assert.Equal("Compte supprimé", vue.AuteurLibelle);
    }
}
