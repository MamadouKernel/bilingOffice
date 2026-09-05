using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Securite;

/// <summary>
/// [SÉCURITÉ — P0-ter] L'ÉTAT DU COMPTE EST UNE RÈGLE D'AUTORISATION, PAS UNE DONNÉE D'AFFICHAGE.
///
/// CE QUE CETTE SUITE DÉMONTRE. Avant correction, désactiver un compte interne n'avait aucun effet
/// sur ses permissions : <c>AutorisationService</c> ne consultait que la table des habilitations,
/// sans jamais demander si le compte existait encore ni s'il était actif. La possession
/// <i>historique</i> d'une habilitation valait donc autorisation présente. Un administrateur
/// désactivé conservant <c>administration.comptes</c> pouvait se réactiver lui-même.
///
/// POURQUOI AUCUN TEST NE L'AVAIT VU. La certification SEC-11 reposait sur une assertion
/// tautologique : elle écrivait <c>EstActif = false</c> puis vérifiait <c>Assert.False(EstActif)</c>,
/// sans exercer le moindre chemin d'accès. Le seul test comportemental existant éprouvait
/// <c>ObtenirContexteUtilisateurQuery</c> — c'est-à-dire le seul maillon de la chaîne qui
/// fonctionnait déjà.
///
/// Chaque test ci-dessous exerce une DÉCISION SERVEUR réelle. Aucun ne se contente de relire un
/// indicateur qu'il vient d'écrire, et aucun ne passe par l'interface : une garde d'interface ne
/// constitue jamais la frontière de sécurité.
/// </summary>
public sealed class EtatCompteEtAutorisationTests
{
    // ---------------------------------------------------------------------
    // Fixture
    // ---------------------------------------------------------------------

    private static ApplicationUser AjouterCompte(
        PlateformeDbContext db, string cle, NatureUtilisateur nature, bool actif)
    {
        var compte = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"{cle}@p0ter.test",
            Email = $"{cle}@p0ter.test",
            NomAffiche = cle,
            Nature = nature,
            EstActif = actif,
        };
        db.Users.Add(compte);
        db.SaveChanges();
        return compte;
    }

    private static void Habiliter(PlateformeDbContext db, Guid utilisateurId, string permission, Guid? fileId = null)
    {
        // VOLONTAIREMENT SANS ComptesDeTest.CompteActif : cette suite décrit elle-même ses comptes,
        // et le cas 10 attribue justement une habilitation à un identifiant SANS compte — matérialiser
        // le compte ici priverait ce cas de tout objet.
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(utilisateurId, permission, fileId));
        db.SaveChanges();
    }

    private static ControleAccesRessourceService ControleAcces(PlateformeDbContext db) =>
        FabriqueRequetes.ControleAcces(db);

    /// <summary>
    /// Port d'administration adossé au MÊME contexte que les dépôts réels : ce que la commande
    /// écrit est relu ensuite par les vrais dépôts. Le scénario traverse donc la couche
    /// applicative au lieu de se vérifier lui-même.
    /// </summary>
    private sealed class PortComptes(PlateformeDbContext db) : IAdministrationComptesRepository
    {
        public Task<IReadOnlyList<UtilisateurInfo>> ListerAgentsAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<UtilisateurInfo>>(
                db.Users.Where(u => u.Nature == NatureUtilisateur.Agent)
                    .Select(u => new UtilisateurInfo(u.Id, u.UserName ?? string.Empty, u.Email ?? string.Empty, u.NomAffiche, u.EstActif, u.Nature))
                    .ToList());

        public Task<ResultatCreationCompte> CreerAgentAsync(string email, string nomAffiche, string motDePasseInitial, CancellationToken ct = default)
        {
            var compte = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                NomAffiche = nomAffiche,
                Nature = NatureUtilisateur.Agent,
                EstActif = true,
            };
            db.Users.Add(compte);
            db.SaveChanges();
            return Task.FromResult(new ResultatCreationCompte(true, compte.Id, Array.Empty<string>()));
        }

        public async Task DefinirActivationAsync(Guid utilisateurId, bool actif, CancellationToken ct = default)
        {
            var compte = db.Users.Single(u => u.Id == utilisateurId);
            compte.EstActif = actif;
            await db.SaveChangesAsync(ct);
        }

        public Task<bool> VerifierMotDePasseAsync(Guid utilisateurId, string motDePasse, CancellationToken ct = default) =>
            Task.FromResult(true);
    }

    private static DefinirActivationCompteCommand CommandeActivation(PlateformeDbContext db) => new(
        new PortComptes(db),
        new UtilisateurInfoRepository(db),
        new AutorisationService(db),
        new JournalisationService(db, new HorlogeSysteme()),
        new UniteDeTravail(db));

    /// <summary>Crée un ticket rattaché à un dossier de société, dans une file donnée.</summary>
    private static (Ticket Ticket, Dossier Dossier) AjouterTicket(
        PlateformeDbContext db, Guid societeId, Guid fileId, Guid auteurId)
    {
        var dossier = Dossier.Creer($"DOS-{Guid.NewGuid():N}"[..16], societeId, DateTime.UtcNow, auteurId);
        var ticket = Ticket.Creer($"TCK-{Guid.NewGuid():N}"[..16], "IMPORT", "STANDARD", fileId, dossier.Id, DateTime.UtcNow, "BL-P0TER");
        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);
        db.SaveChanges();
        return (ticket, dossier);
    }

    // ---------------------------------------------------------------------
    // Cas 1 & 2 — la bascule d'activité, à permission constante
    // ---------------------------------------------------------------------

    /// <summary>Cas 1 — agent actif + permission valide ⇒ action autorisée. Témoin : sans lui, un
    /// service qui refuserait tout passerait les autres tests.</summary>
    [Fact]
    public async Task Cas01_Agent_actif_avec_permission_est_autorise()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var agent = AjouterCompte(db, "agent-actif", NatureUtilisateur.Agent, actif: true);
        Habiliter(db, agent.Id, Permission.TicketCloturer);

        var autorisation = new AutorisationService(db);

        Assert.True(await autorisation.EstAutoriseAsync(agent.Id, Permission.TicketCloturer));
    }

    /// <summary>Cas 2 — agent désactivé + permission valide ⇒ REFUS. La possession historique
    /// d'une habilitation ne survit pas à la désactivation du compte.</summary>
    [Fact]
    public async Task Cas02_Agent_desactive_avec_permission_est_refuse()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var agent = AjouterCompte(db, "agent-desactive", NatureUtilisateur.Agent, actif: false);
        Habiliter(db, agent.Id, Permission.TicketCloturer);

        var autorisation = new AutorisationService(db);

        Assert.False(await autorisation.EstAutoriseAsync(agent.Id, Permission.TicketCloturer));
        await Assert.ThrowsAsync<AccesRefuseException>(
            () => autorisation.ExigerAsync(agent.Id, Permission.TicketCloturer));
    }

    // ---------------------------------------------------------------------
    // Cas 3, 4, 5 — administration
    // ---------------------------------------------------------------------

    /// <summary>Cas 3 — administrateur actif ⇒ administration autorisée.</summary>
    [Fact]
    public async Task Cas03_Administrateur_actif_peut_administrer()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var admin = AjouterCompte(db, "admin-actif", NatureUtilisateur.Agent, actif: true);
        Habiliter(db, admin.Id, Permission.AdministrationComptes);
        var cible = AjouterCompte(db, "cible", NatureUtilisateur.Agent, actif: true);

        await CommandeActivation(db).ExecuterAsync(admin.Id, cible.Id, actif: false);

        Assert.False(db.Users.Single(u => u.Id == cible.Id).EstActif);
    }

    /// <summary>Cas 4 — administrateur désactivé ⇒ administration REFUSÉE. C'est la commande
    /// serveur qui doit refuser, non l'écran qui doit masquer le bouton.</summary>
    [Fact]
    public async Task Cas04_Administrateur_desactive_ne_peut_pas_administrer()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var admin = AjouterCompte(db, "admin-desactive", NatureUtilisateur.Agent, actif: false);
        Habiliter(db, admin.Id, Permission.AdministrationComptes);
        var cible = AjouterCompte(db, "cible", NatureUtilisateur.Agent, actif: true);

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => CommandeActivation(db).ExecuterAsync(admin.Id, cible.Id, actif: false));

        Assert.True(db.Users.Single(u => u.Id == cible.Id).EstActif);
    }

    /// <summary>
    /// Cas 5 — LE CAS DÉCISIF : un administrateur désactivé tente de se réactiver lui-même.
    /// C'est le scénario d'escalade complet de la chaîne P0-ter — il rendait la désactivation
    /// réversible par la personne même qu'elle visait.
    /// </summary>
    [Fact]
    public async Task Cas05_Administrateur_desactive_ne_peut_pas_se_reactiver_lui_meme()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var admin = AjouterCompte(db, "admin-auto-reactivation", NatureUtilisateur.Agent, actif: false);
        Habiliter(db, admin.Id, Permission.AdministrationComptes);

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => CommandeActivation(db).ExecuterAsync(admin.Id, admin.Id, actif: true));

        Assert.False(db.Users.Single(u => u.Id == admin.Id).EstActif);
    }

    // ---------------------------------------------------------------------
    // Cas 6, 7, 8 — session déjà ouverte
    // ---------------------------------------------------------------------

    /// <summary>
    /// Cas 6 — session préexistante. L'utilisateur était actif, s'est connecté, puis a été
    /// désactivé : son cookie reste techniquement valide. La décision serveur doit refuser
    /// indépendamment de l'état du cookie — c'est précisément ce que « le cookie ne suffit
    /// jamais » signifie.
    /// </summary>
    [Fact]
    public async Task Cas06_Session_preexistante_ne_permet_plus_d_agir_apres_desactivation()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var agent = AjouterCompte(db, "agent-session", NatureUtilisateur.Agent, actif: true);
        Habiliter(db, agent.Id, Permission.TicketTransferer);

        var autorisation = new AutorisationService(db);

        // La session s'ouvre alors que le compte est actif.
        Assert.True(await autorisation.EstAutoriseAsync(agent.Id, Permission.TicketTransferer));

        // Un administrateur désactive le compte pendant que la session vit.
        db.Users.Single(u => u.Id == agent.Id).EstActif = false;
        await db.SaveChangesAsync();

        // Requête suivante, même identité, même cookie : refus.
        Assert.False(await autorisation.EstAutoriseAsync(agent.Id, Permission.TicketTransferer));
    }

    /// <summary>Cas 7 — agent désactivé ⇒ lecture protégée refusée.</summary>
    [Fact]
    public async Task Cas07_Agent_desactive_ne_lit_aucune_ressource_protegee()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var fileId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        var agent = AjouterCompte(db, "agent-lecture", NatureUtilisateur.Agent, actif: false);
        Habiliter(db, agent.Id, Permission.TicketPrendreEnCharge, fileId);
        var (ticket, dossier) = AjouterTicket(db, societeId, fileId, Guid.NewGuid());

        Assert.False(await ControleAcces(db).PeutConsulterLeTicketAsync(agent.Id, ticket));
        Assert.False(await ControleAcces(db).PeutConsulterLeDossierAsync(agent.Id, dossier));
    }

    /// <summary>Cas 8 — agent désactivé ⇒ commande métier refusée, au niveau du service et non
    /// de l'écran.</summary>
    [Fact]
    public async Task Cas08_Agent_desactive_ne_peut_executer_aucune_commande_metier()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var fileId = Guid.NewGuid();
        var agent = AjouterCompte(db, "agent-commande", NatureUtilisateur.Agent, actif: false);
        Habiliter(db, agent.Id, Permission.TicketPrendreEnCharge, fileId);

        var autorisation = new AutorisationService(db);

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => autorisation.ExigerAsync(agent.Id, Permission.TicketPrendreEnCharge, fileId));
    }

    // ---------------------------------------------------------------------
    // Cas 9, 10 — absence d'habilitation, absence de compte
    // ---------------------------------------------------------------------

    /// <summary>Cas 9 — agent actif mais sans habilitation ⇒ refus. La nature « Agent » n'a
    /// jamais été un droit ; ce test garantit que la correction ne l'a pas rendue suffisante.</summary>
    [Fact]
    public async Task Cas09_Agent_sans_habilitation_est_refuse()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var agent = AjouterCompte(db, "agent-sans-droit", NatureUtilisateur.Agent, actif: true);

        var autorisation = new AutorisationService(db);

        Assert.False(await autorisation.EstAutoriseAsync(agent.Id, Permission.TicketCloturer));
    }

    /// <summary>
    /// Cas 10 — identité inconnue ou supprimée, dont la session est encore présentée. Aucune ligne
    /// de compte, donc aucune habilitation : le refus doit venir de l'absence de compte, pas de
    /// l'absence de droit. Le test attribue donc explicitement la permission à cet identifiant
    /// orphelin, ce qui reproduit une habilitation résiduelle après suppression du compte.
    /// </summary>
    [Fact]
    public async Task Cas10_Compte_inconnu_avec_habilitation_residuelle_est_refuse()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var fantome = Guid.NewGuid();
        Habiliter(db, fantome, Permission.AdministrationComptes);

        var autorisation = new AutorisationService(db);

        Assert.False(await autorisation.EstAutoriseAsync(fantome, Permission.AdministrationComptes));
        Assert.False(await autorisation.EstAutoriseAsync(Guid.Empty, Permission.AdministrationComptes));
    }

    // ---------------------------------------------------------------------
    // Cas 11, 12 — client et transporteur
    // ---------------------------------------------------------------------

    /// <summary>
    /// Cas 11 — client désactivé. La règle existante (CPT-04) veut qu'un client n'accède qu'à ses
    /// propres dossiers ; la désactivation la renforce sans la contredire : le propriétaire
    /// légitime lui-même n'accède plus.
    /// </summary>
    [Fact]
    public async Task Cas11_Client_desactive_n_accede_plus_a_ses_propres_dossiers()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var societeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var client = AjouterCompte(db, "client-desactive", NatureUtilisateur.Client, actif: false);
        db.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(client.Id, societeId, DateTime.UtcNow, false, true));
        db.SaveChanges();
        var (ticket, dossier) = AjouterTicket(db, societeId, fileId, client.Id);

        Assert.False(await ControleAcces(db).PeutConsulterLeTicketAsync(client.Id, ticket));
        Assert.False(await ControleAcces(db).PeutConsulterLeDossierAsync(client.Id, dossier));
    }

    /// <summary>
    /// Cas 12 — transporteur désactivé. Son dossier d'Enrôlement est PERSONNEL (<c>ClientId</c>
    /// porte l'utilisateur, non une société) : c'est la voie de propriété nominative, qui doit
    /// elle aussi céder devant la désactivation.
    /// </summary>
    [Fact]
    public async Task Cas12_Transporteur_desactive_n_accede_plus_a_son_propre_dossier()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var fileId = Guid.NewGuid();
        var transporteur = AjouterCompte(db, "transporteur-desactive", NatureUtilisateur.Client, actif: false);
        var (ticket, dossier) = AjouterTicket(db, transporteur.Id, fileId, transporteur.Id);

        Assert.False(await ControleAcces(db).PeutConsulterLeTicketAsync(transporteur.Id, ticket));
        Assert.False(await ControleAcces(db).PeutConsulterLeDossierAsync(transporteur.Id, dossier));
    }

    /// <summary>
    /// Témoin du cas 12 : le même transporteur, actif, accède bien à son dossier personnel. Sans
    /// ce témoin, une correction qui refuserait tout dossier personnel passerait le cas 12.
    /// </summary>
    [Fact]
    public async Task Cas12bis_Transporteur_actif_accede_a_son_propre_dossier()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var fileId = Guid.NewGuid();
        var transporteur = AjouterCompte(db, "transporteur-actif", NatureUtilisateur.Client, actif: true);
        var (ticket, dossier) = AjouterTicket(db, transporteur.Id, fileId, transporteur.Id);

        Assert.True(await ControleAcces(db).PeutConsulterLeTicketAsync(transporteur.Id, ticket));
        Assert.True(await ControleAcces(db).PeutConsulterLeDossierAsync(transporteur.Id, dossier));
    }
}
