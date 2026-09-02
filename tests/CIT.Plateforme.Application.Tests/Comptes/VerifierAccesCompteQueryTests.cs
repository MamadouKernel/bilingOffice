using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Comptes;

/// <summary>
/// CPT-01 / CPT-03 — contrôle métier d'accès au compte, appliqué APRÈS l'authentification.
///
/// La règle exigeait un rattachement à une société pour tout compte de nature Client. Elle
/// confondait deux situations distinctes : le demandeur dont le dossier n'est pas tranché, qui
/// doit être refusé, et la personne physique qui n'a jamais déposé de demande — le transporteur
/// du module Enrôlement — pour qui cette condition n'a aucun sens. Le module était de fait
/// inaccessible à ses propres usagers.
///
/// L'enjeu de cette suite n'est pas d'ouvrir l'accès : c'est de démontrer que l'ouverture
/// N'AFFAIBLIT PAS CPT-01. Les cas de refus sont donc traités en premier et couverts un par un.
/// </summary>
public sealed class VerifierAccesCompteQueryTests
{
    private static VerifierAccesCompteQuery Construire(PlateformeDbContext dbContext) =>
        new(new UtilisateurSocieteRepository(dbContext),
            new SocieteClienteRepository(dbContext),
            new UtilisateurInfoRepository(dbContext));

    /// <summary>Le compte doit exister : un identifiant inconnu est refuse quoi qu'il arrive.</summary>
    private static Guid CreerCompte(PlateformeDbContext dbContext, bool actif = true)
    {
        var compte = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"{Guid.NewGuid():N}@test.local",
            Email = $"{Guid.NewGuid():N}@test.local",
            NomAffiche = "Compte de test",
            Nature = NatureUtilisateur.Client,
            EstActif = actif,
        };
        dbContext.Users.Add(compte);
        dbContext.SaveChanges();
        return compte.Id;
    }

    private static readonly DateTime Maintenant = new(2026, 9, 2, 8, 0, 0, DateTimeKind.Utc);

    private static SocieteCliente AjouterDemande(PlateformeDbContext dbContext, Guid demandeurId)
    {
        var societe = SocieteCliente.Soumettre(demandeurId, "Entreprise", TypeClient.Normal, Maintenant);
        dbContext.Societes.Add(societe);
        dbContext.SaveChanges();
        return societe;
    }

    // ═══════════ CPT-01 — les refus, qui ne doivent pas bouger ═══════════

    /// <summary>
    /// LE CAS DÉCISIF : « soumise à validation par CIT AVANT activation ». Le compte existe dès
    /// la soumission ; il ne doit pas pour autant donner accès à quoi que ce soit.
    /// </summary>
    [Fact]
    public async Task Un_demandeur_en_attente_de_validation_reste_refuse()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var demandeurId = CreerCompte(dbContext);
        var societe = AjouterDemande(dbContext, demandeurId);

        Assert.Equal(EtatSociete.EnAttenteValidation, societe.Etat);
        Assert.False(await Construire(dbContext).AccesAutoriseAsync(demandeurId));
    }

    [Fact]
    public async Task Un_demandeur_dont_la_demande_a_ete_refusee_reste_refuse()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var demandeurId = CreerCompte(dbContext);
        var societe = AjouterDemande(dbContext, demandeurId);
        societe.Rejeter();
        await dbContext.SaveChangesAsync();

        Assert.False(await Construire(dbContext).AccesAutoriseAsync(demandeurId));
    }

    /// <summary>SEC-11 et ENT-02 : un collaborateur désactivé perd l'accès, société active ou non.</summary>
    [Fact]
    public async Task Un_collaborateur_desactive_reste_refuse()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var utilisateurId = CreerCompte(dbContext);
        var societe = AjouterDemande(dbContext, Guid.NewGuid());
        societe.Valider();

        var rattachement = UtilisateurSociete.Rattacher(utilisateurId, societe.Id, Maintenant);
        rattachement.Desactiver();
        dbContext.UtilisateursSociete.Add(rattachement);
        await dbContext.SaveChangesAsync();

        Assert.False(await Construire(dbContext).AccesAutoriseAsync(utilisateurId));
    }

    [Fact]
    public async Task Un_collaborateur_actif_d_une_societe_non_validee_reste_refuse()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var utilisateurId = CreerCompte(dbContext);
        var societe = AjouterDemande(dbContext, Guid.NewGuid());

        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(utilisateurId, societe.Id, Maintenant));
        await dbContext.SaveChangesAsync();

        Assert.False(await Construire(dbContext).AccesAutoriseAsync(utilisateurId));
    }

    // ═══════════ Les accès légitimes ═══════════

    [Fact]
    public async Task Un_collaborateur_actif_d_une_societe_validee_accede()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var utilisateurId = CreerCompte(dbContext);
        var societe = AjouterDemande(dbContext, Guid.NewGuid());
        societe.Valider();

        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(utilisateurId, societe.Id, Maintenant));
        await dbContext.SaveChangesAsync();

        Assert.True(await Construire(dbContext).AccesAutoriseAsync(utilisateurId));
    }

    /// <summary>
    /// LE DÉFAUT CORRIGÉ. Transporteur ou chauffeur : personne physique unique, sans société
    /// rattachée par conception (CLAR-ENR-01, et § 6 du CDC qui en fait un profil distinct).
    /// Il n'a jamais déposé de demande de compte société — la condition de CPT-01 ne le concerne
    /// donc pas. Auparavant, il ne pouvait pas se connecter, ce qui rendait tout le module
    /// Enrôlement inaccessible à ses usagers.
    /// </summary>
    [Fact]
    public async Task Une_personne_physique_sans_societe_accede()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();

        var transporteur = CreerCompte(dbContext);

        Assert.True(await Construire(dbContext).AccesAutoriseAsync(transporteur));
    }

    /// <summary>
    /// SEC-11 — un compte désactivé est refusé dès la connexion, sans attendre la résolution du
    /// contexte. Ce contrôle est venu avec le correctif : il n'existait pas auparavant à ce niveau.
    /// </summary>
    [Fact]
    public async Task Un_compte_desactive_est_refuse_des_la_connexion()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var compteInactif = CreerCompte(dbContext, actif: false);

        Assert.False(await Construire(dbContext).AccesAutoriseAsync(compteInactif));
    }

    /// <summary>
    /// Un identifiant inconnu n'est jamais autorisé : l'absence de demande ne doit pas devenir
    /// une permissivité par défaut.
    /// </summary>
    [Fact]
    public async Task Un_identifiant_inconnu_est_refuse()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();

        Assert.False(await Construire(dbContext).AccesAutoriseAsync(Guid.NewGuid()));
    }

    /// <summary>
    /// Garde anti-régression : l'ouverture doit dépendre de l'absence de demande PROPRE à cet
    /// utilisateur, pas de l'absence de demandes en général. Une base peuplée ne doit pas
    /// refermer l'accès du transporteur, ni l'ouvrir à un demandeur en attente.
    /// </summary>
    [Fact]
    public async Task L_acces_se_juge_utilisateur_par_utilisateur_et_non_sur_l_etat_global()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var demandeurEnAttente = CreerCompte(dbContext);
        AjouterDemande(dbContext, demandeurEnAttente);

        var transporteur = CreerCompte(dbContext);

        var query = Construire(dbContext);
        Assert.False(await query.AccesAutoriseAsync(demandeurEnAttente));
        Assert.True(await query.AccesAutoriseAsync(transporteur));
    }
}
