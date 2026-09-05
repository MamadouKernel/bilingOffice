using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Tests.Partages;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Comptes;

/// <summary>
/// [P3] LA RÈGLE D'ACCÈS À LA CONNEXION, ÉPROUVÉE DIRECTEMENT.
///
/// POURQUOI CETTE SUITE EXISTE. La règle vivait en DEUX exemplaires identiques, dans
/// <c>Connexion.razor</c> et <c>VerifierCode.razor</c>. Elle n'était donc couverte que par des
/// tests de câblage lisant les sources — c'est le risque résiduel R1 de la consolidation : on
/// vérifiait que l'appel existait, jamais qu'il décidait juste.
///
/// Deux copies d'une règle de sécurité finissent par diverger. C'est exactement ce qui s'est
/// produit au P0-ter : <c>Connexion.razor</c> portait le contrôle d'activité, <c>VerifierCode.razor</c>
/// ne l'avait jamais eu, et personne ne s'en était aperçu parce que rien ne les comparait.
///
/// La règle est désormais unique et appelée par les deux pages. Ces tests l'éprouvent sur des
/// comptes réels, sans passer par aucune interface.
/// </summary>
public sealed class RegleAccesConnexionTests
{
    private static (PlateformeDbContext Db, RegleAccesConnexion Regle) Construire()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var verifier = new VerifierAccesCompteQuery(
            new UtilisateurSocieteRepository(db),
            new SocieteClienteRepository(db),
            new UtilisateurInfoRepository(db));
        return (db, new RegleAccesConnexion(verifier));
    }

    private static ApplicationUser Compte(PlateformeDbContext db, NatureUtilisateur nature, bool actif)
    {
        var compte = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"{Guid.NewGuid():N}@connexion.test",
            Email = $"{Guid.NewGuid():N}@connexion.test",
            NomAffiche = "Utilisateur",
            Nature = nature,
            EstActif = actif,
        };
        db.Users.Add(compte);
        db.SaveChanges();
        return compte;
    }

    // ---------------------------------------------------------------------
    // La règle d'ACTIVATION — applicable à tous les comptes
    // ---------------------------------------------------------------------

    /// <summary>Un agent actif entre. Témoin : sans lui, une règle qui refuserait tout passerait
    /// les cas de refus.</summary>
    [Fact]
    public async Task Un_agent_actif_est_autorise()
    {
        var (db, regle) = Construire();
        using var _ = db;
        var agent = Compte(db, NatureUtilisateur.Agent, actif: true);

        Assert.True(await regle.AccesAutoriseAsync(agent.Id, agent.Nature, agent.EstActif));
    }

    /// <summary>
    /// UN AGENT DÉSACTIVÉ EST REFUSÉ. C'est le cas que la voie 2FA ne vérifiait pas : la règle
    /// n'y était pas, et le compte ouvrait une session.
    /// </summary>
    [Fact]
    public async Task Un_agent_desactive_est_refuse()
    {
        var (db, regle) = Construire();
        using var _ = db;
        var agent = Compte(db, NatureUtilisateur.Agent, actif: false);

        Assert.False(await regle.AccesAutoriseAsync(agent.Id, agent.Nature, agent.EstActif));
    }

    /// <summary>Un client désactivé est refusé, comme l'agent : la règle ne distingue pas.</summary>
    [Fact]
    public async Task Un_client_desactive_est_refuse()
    {
        var (db, regle) = Construire();
        using var _ = db;
        var client = Compte(db, NatureUtilisateur.Client, actif: false);

        Assert.False(await regle.AccesAutoriseAsync(client.Id, client.Nature, client.EstActif));
    }

    // ---------------------------------------------------------------------
    // La règle SOCIÉTÉ — propre aux comptes Client (CPT-03)
    // ---------------------------------------------------------------------

    /// <summary>
    /// Un client actif rattaché à une société ACTIVE entre. C'est la règle CPT-03, et elle ne
    /// s'applique qu'à lui.
    /// </summary>
    [Fact]
    public async Task Un_client_actif_rattache_a_une_societe_active_est_autorise()
    {
        var (db, regle) = Construire();
        using var _ = db;
        var client = Compte(db, NatureUtilisateur.Client, actif: true);

        var societe = SocieteCliente.Soumettre(client.Id, "SOCIETE", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        db.Societes.Add(societe);
        db.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(client.Id, societe.Id, DateTime.UtcNow));
        db.SaveChanges();

        Assert.True(await regle.AccesAutoriseAsync(client.Id, client.Nature, client.EstActif));
    }

    /// <summary>
    /// Un client actif dont la demande de société n'est pas encore tranchée est refusé (CPT-01 —
    /// « soumise à validation par CIT avant activation »).
    /// </summary>
    [Fact]
    public async Task Un_client_dont_la_demande_de_societe_n_est_pas_tranchee_est_refuse()
    {
        var (db, regle) = Construire();
        using var _ = db;
        var client = Compte(db, NatureUtilisateur.Client, actif: true);

        // Demande déposée, jamais validée : aucun rattachement n'existe.
        db.Societes.Add(SocieteCliente.Soumettre(client.Id, "SOCIETE-EN-ATTENTE", TypeClient.Normal, DateTime.UtcNow));
        db.SaveChanges();

        Assert.False(await regle.AccesAutoriseAsync(client.Id, client.Nature, client.EstActif));
    }

    /// <summary>
    /// Un AGENT actif n'est pas soumis à la règle société : il n'est jamais rattaché à une
    /// société, par conception. C'est le témoin qui empêche de réintroduire le blocage corrigé
    /// le 2026-08-30.
    /// </summary>
    [Fact]
    public async Task Un_agent_actif_n_est_pas_soumis_a_la_regle_societe()
    {
        var (db, regle) = Construire();
        using var _ = db;
        var agent = Compte(db, NatureUtilisateur.Agent, actif: true);

        // Aucune société, aucun rattachement — et pourtant autorisé.
        Assert.True(await regle.AccesAutoriseAsync(agent.Id, agent.Nature, agent.EstActif));
    }

    // ---------------------------------------------------------------------
    // Le scénario du § 5 de la mission
    // ---------------------------------------------------------------------

    /// <summary>
    /// LE SCÉNARIO 2FA. Compte actif au moment de la saisie du mot de passe, désactivé pendant que
    /// l'utilisateur cherche son code, puis validation : la règle doit refuser.
    ///
    /// Éprouvé sur la RÈGLE et non sur la page : piloter <c>SignInManager</c> et son cookie
    /// <c>TwoFactorUserId</c> hors d'un navigateur demanderait une infrastructure sans rapport avec
    /// ce qui est vérifié ici. Le câblage de la page reste couvert par
    /// <c>CablageControleAccesTests.La_voie_2fa_verifie_l_activite_du_compte</c>.
    /// </summary>
    [Fact]
    public async Task Un_compte_desactive_entre_les_deux_etapes_de_la_2fa_est_refuse()
    {
        var (db, regle) = Construire();
        using var _ = db;
        var agent = Compte(db, NatureUtilisateur.Agent, actif: true);

        // Étape 1 — mot de passe validé, le compte est encore actif.
        Assert.True(await regle.AccesAutoriseAsync(agent.Id, agent.Nature, agent.EstActif));

        // Un administrateur désactive le compte pendant que l'utilisateur cherche son code.
        agent.EstActif = false;
        await db.SaveChangesAsync();

        // Étape 2 — le code est bon, mais le compte ne l'est plus.
        Assert.False(await regle.AccesAutoriseAsync(agent.Id, agent.Nature, agent.EstActif));
    }

    /// <summary>Un compte inconnu est refusé, quelle que soit la nature annoncée.</summary>
    [Fact]
    public async Task Un_compte_inconnu_est_refuse()
    {
        var (db, regle) = Construire();
        using var _ = db;

        Assert.False(await regle.AccesAutoriseAsync(Guid.NewGuid(), NatureUtilisateur.Client, estActif: true));
    }
}
