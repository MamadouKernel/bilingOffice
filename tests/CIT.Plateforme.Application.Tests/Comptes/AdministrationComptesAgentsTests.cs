using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Comptes;

/// <summary>
/// CDC § 17.3 — « Administration : gestion des comptes clients (validation, typage) et des
/// comptes agents (RÔLES) », et SEC-11 (Must) — « Les comptes agents peuvent être désactivés
/// immédiatement, sans suppression de leur historique. »
///
/// Ces deux exigences n'étaient portées par aucun écran ni aucune commande : les comptes internes
/// n'existaient que par le jeu de données de développement, jamais exécuté en production.
/// </summary>
public class AdministrationComptesAgentsTests
{
    /// <summary>
    /// Substitut du port d'administration, adossé au MÊME contexte que les dépôts réels : ce que
    /// la commande écrit ici est relu ensuite par <see cref="UtilisateurInfoRepository"/>. Le
    /// scénario traverse donc réellement la couche applicative, au lieu de se vérifier lui-même.
    /// </summary>
    private sealed class PortComptesEnMemoire(PlateformeDbContext dbContext) : IAdministrationComptesRepository
    {
        public Task<IReadOnlyList<UtilisateurInfo>> ListerAgentsAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<UtilisateurInfo>>(
                dbContext.Users
                    .Where(u => u.Nature == NatureUtilisateur.Agent)
                    .Select(u => new UtilisateurInfo(u.Id, u.UserName ?? string.Empty, u.Email ?? string.Empty, u.NomAffiche, u.EstActif, u.Nature))
                    .ToList());

        public async Task<ResultatCreationCompte> CreerAgentAsync(string email, string nomAffiche, string motDePasseInitial, CancellationToken ct = default)
        {
            if (dbContext.Users.Any(u => u.Email == email))
            {
                return new ResultatCreationCompte(false, Guid.Empty, ["Cette adresse est déjà utilisée."]);
            }

            var compte = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                NomAffiche = nomAffiche,
                Nature = NatureUtilisateur.Agent,
                EmailConfirmed = true,
                EstActif = true,
            };

            dbContext.Users.Add(compte);
            await dbContext.SaveChangesAsync(ct);
            return new ResultatCreationCompte(true, compte.Id, Array.Empty<string>());
        }

        public async Task DefinirActivationAsync(Guid utilisateurId, bool actif, CancellationToken ct = default)
        {
            var compte = dbContext.Users.Single(u => u.Id == utilisateurId);
            compte.EstActif = actif;
            await dbContext.SaveChangesAsync(ct);
        }

        public Task<bool> VerifierMotDePasseAsync(Guid utilisateurId, string motDePasse, CancellationToken ct = default) =>
            Task.FromResult(true);
    }

    private static ApplicationUser AjouterAgent(PlateformeDbContext dbContext, string email, bool actif = true)
    {
        var compte = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            NomAffiche = email,
            Nature = NatureUtilisateur.Agent,
            EstActif = actif,
        };
        dbContext.Users.Add(compte);
        dbContext.SaveChanges();
        return compte;
    }

    private static Guid AjouterAdministrateur(PlateformeDbContext dbContext)
    {
        var admin = AjouterAgent(dbContext, $"admin-{Guid.NewGuid():N}@cit.test");
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, admin.Id), Permission.AdministrationComptes));
        dbContext.SaveChanges();
        return admin.Id;
    }

    private static (CreerCompteAgentCommand Creer,
                    DefinirActivationCompteCommand Activation,
                    GererHabilitationAgentCommand Habilitation,
                    ListerComptesAgentsQuery Lister)
        Commandes(PlateformeDbContext dbContext)
    {
        var port = new PortComptesEnMemoire(dbContext);
        var utilisateurs = new UtilisateurInfoRepository(dbContext);
        var habilitations = new HabilitationRepository(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var journal = new Mock<IJournalisationService>().Object;
        // L'unite de travail REELLE, pas un simulacre : les depots n'appellent jamais
        // SaveChanges eux-memes, et un substitut a vide laisserait les ecritures dans le
        // traqueur sans jamais les persister — le test passerait a cote de ce qu'il verifie.
        var uow = new UniteDeTravail(dbContext);

        return (
            new CreerCompteAgentCommand(port, autorisation, journal, uow),
            new DefinirActivationCompteCommand(port, utilisateurs, autorisation, journal, uow),
            new GererHabilitationAgentCommand(habilitations, utilisateurs, autorisation, journal, uow),
            new ListerComptesAgentsQuery(port, habilitations, autorisation));
    }

    // ---- Autorisation : § 17.3 réserve ces opérations, elles ne sont pas ouvertes ----

    [Fact]
    public async Task Un_agent_sans_la_permission_ne_peut_creer_aucun_compte()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var quidam = AjouterAgent(dbContext, "quidam@cit.test");
        var (creer, _, _, _) = Commandes(dbContext);

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => creer.ExecuterAsync(quidam.Id, "nouveau@cit.test", "Nouveau", "MotDePasse123!"));
    }

    [Fact]
    public async Task Un_agent_sans_la_permission_ne_peut_ni_habiliter_ni_desactiver()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var quidam = AjouterAgent(dbContext, "quidam@cit.test");
        var cible = AjouterAgent(dbContext, "cible@cit.test");
        var (_, activation, habilitation, lister) = Commandes(dbContext);

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => habilitation.AttribuerAsync(quidam.Id, cible.Id, Permission.TicketCloturer));
        await Assert.ThrowsAsync<AccesRefuseException>(
            () => activation.ExecuterAsync(quidam.Id, cible.Id, false));
        await Assert.ThrowsAsync<AccesRefuseException>(
            () => lister.ExecuterAsync(quidam.Id));
    }

    // ---- § 17.3 : création et attribution de rôles ----

    [Fact]
    public async Task L_administrateur_cree_un_compte_agent_puis_le_retrouve_dans_la_liste()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var adminId = AjouterAdministrateur(dbContext);
        var (creer, _, _, lister) = Commandes(dbContext);

        var resultat = await creer.ExecuterAsync(adminId, "nouvel.agent@cit.test", "Nouvel Agent", "MotDePasse123!");

        Assert.True(resultat.Succes);
        var comptes = await lister.ExecuterAsync(adminId);
        Assert.Contains(comptes, c => c.Email == "nouvel.agent@cit.test" && c.EstActif);
    }

    [Fact]
    public async Task Un_role_attribue_apparait_puis_disparait_apres_retrait()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var adminId = AjouterAdministrateur(dbContext);
        var cible = AjouterAgent(dbContext, "cible@cit.test");
        var (_, _, habilitation, lister) = Commandes(dbContext);

        await habilitation.AttribuerAsync(adminId, cible.Id, Permission.TicketCloturer);
        var apresAttribution = await lister.ExecuterAsync(adminId);
        Assert.Contains(Permission.TicketCloturer, apresAttribution.Single(c => c.Id == cible.Id).Permissions);

        await habilitation.RetirerAsync(adminId, cible.Id, Permission.TicketCloturer);
        var apresRetrait = await lister.ExecuterAsync(adminId);
        Assert.DoesNotContain(Permission.TicketCloturer, apresRetrait.Single(c => c.Id == cible.Id).Permissions);
    }

    [Fact]
    public async Task Reattribuer_un_role_deja_accorde_ne_cree_pas_de_doublon()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var adminId = AjouterAdministrateur(dbContext);
        var cible = AjouterAgent(dbContext, "cible@cit.test");
        var (_, _, habilitation, _) = Commandes(dbContext);

        await habilitation.AttribuerAsync(adminId, cible.Id, Permission.TicketCloturer);
        await habilitation.AttribuerAsync(adminId, cible.Id, Permission.TicketCloturer);

        Assert.Single(dbContext.Habilitations.Where(
            h => h.UtilisateurId == cible.Id && h.PermissionCode == Permission.TicketCloturer));
    }

    [Fact]
    public async Task Une_permission_inconnue_est_refusee_plutot_qu_accordee_en_silence()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var adminId = AjouterAdministrateur(dbContext);
        var cible = AjouterAgent(dbContext, "cible@cit.test");
        var (_, _, habilitation, _) = Commandes(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(
            () => habilitation.AttribuerAsync(adminId, cible.Id, "permission.qui.n.existe.pas"));
    }

    // ---- Garde-fous : ne pas se verrouiller hors de l'administration ----

    [Fact]
    public async Task Un_administrateur_ne_peut_pas_se_desactiver_lui_meme()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var adminId = AjouterAdministrateur(dbContext);
        var (_, activation, _, _) = Commandes(dbContext);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => activation.ExecuterAsync(adminId, adminId, false));
    }

    [Fact]
    public async Task Un_administrateur_ne_peut_pas_se_retirer_le_droit_d_administrer()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var adminId = AjouterAdministrateur(dbContext);
        var (_, _, habilitation, _) = Commandes(dbContext);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => habilitation.RetirerAsync(adminId, adminId, Permission.AdministrationComptes));
    }

    // ---- SEC-11 : la désactivation produit un EFFET D'ACCÈS RÉEL ----

    /// <summary>
    /// Ce cas est le pendant manquant de la suite de certification SEC-11 : celle-ci démontrait
    /// la conservation de l'historique, mais son volet « accès » se contentait de vérifier que
    /// l'indicateur qu'elle venait d'écrire valait bien ce qu'elle y avait mis.
    ///
    /// Ici, la désactivation passe par la commande, puis l'accès est réévalué par le chemin de
    /// production — <see cref="ObtenirContexteUtilisateurQuery"/>, qui alimente la navigation et
    /// les vérifications d'autorisation. Un compte désactivé n'est plus authentifié et ne porte
    /// plus aucune permission, quand bien même ses habilitations subsistent en base.
    /// </summary>
    [Fact]
    public async Task Un_compte_desactive_perd_effectivement_tout_acces()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var adminId = AjouterAdministrateur(dbContext);
        var cible = AjouterAgent(dbContext, "agent.actif@cit.test");
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, cible.Id), Permission.TicketCloturer));
        await dbContext.SaveChangesAsync();

        var contexteQuery = new ObtenirContexteUtilisateurQuery(
            new UtilisateurInfoRepository(dbContext),
            new UtilisateurSocieteRepository(dbContext),
            new SocieteClienteRepository(dbContext),
            new HabilitationRepository(dbContext),
            new PerimetreSupervisionRepository(dbContext));

        var avant = await contexteQuery.ExecuterAsync(cible.Id);
        Assert.True(avant.EstAuthentifie);
        Assert.Contains(Permission.TicketCloturer, avant.Permissions);

        var (_, activation, _, _) = Commandes(dbContext);
        await activation.ExecuterAsync(adminId, cible.Id, actif: false);

        var apres = await contexteQuery.ExecuterAsync(cible.Id);
        Assert.False(apres.EstAuthentifie);
        Assert.Empty(apres.Permissions);

        // SEC-11, second volet : la désactivation ne supprime pas. Le compte et ses habilitations
        // restent en base, ce qui garde son historique attribuable nominativement.
        Assert.NotNull(dbContext.Users.SingleOrDefault(u => u.Id == cible.Id));
        Assert.NotEmpty(dbContext.Habilitations.Where(h => h.UtilisateurId == cible.Id));
    }

    [Fact]
    public async Task Un_compte_reactive_retrouve_ses_permissions()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var adminId = AjouterAdministrateur(dbContext);
        var cible = AjouterAgent(dbContext, "agent@cit.test", actif: false);
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, cible.Id), Permission.TicketCloturer));
        await dbContext.SaveChangesAsync();

        var (_, activation, _, _) = Commandes(dbContext);
        await activation.ExecuterAsync(adminId, cible.Id, actif: true);

        var contexteQuery = new ObtenirContexteUtilisateurQuery(
            new UtilisateurInfoRepository(dbContext),
            new UtilisateurSocieteRepository(dbContext),
            new SocieteClienteRepository(dbContext),
            new HabilitationRepository(dbContext),
            new PerimetreSupervisionRepository(dbContext));

        var contexte = await contexteQuery.ExecuterAsync(cible.Id);
        Assert.True(contexte.EstAuthentifie);
        Assert.Contains(Permission.TicketCloturer, contexte.Permissions);
    }
}
