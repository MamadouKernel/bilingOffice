using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Comptes;

/// <summary>
/// [TEST-SPEC] Remédiation post-audit (`post-implementation-audit.md` §6, ENT-02 — dérive du
/// compteur). Remplace <c>DeriveCompteurAdministrateurTests</c> (qui démontrait l'écart avant
/// correction — ses assertions décriraient désormais un comportement incorrect si conservées
/// telles quelles). Vérifie, pour CHAQUE transition possible de
/// <c>(EstAdministrateur, EstActif)</c>, l'invariant :
/// <c>SocieteCliente.NombreAdministrateursActifs == COUNT(UtilisateurSociete WHERE
/// EstAdministrateur = true AND EstActif = true)</c>.
/// Exigence CDC: ENT-02 → US3 → critère d'acceptation 6 de US3 (FR-009) → Test — CLAR-03.
/// </summary>
public sealed class InvariantCompteurAdministrateurTests
{
    private static (PlateformeDbContext DbContext, SocieteCliente Societe, RattacherUtilisateurCommand Rattacher,
        ActiverDesactiverUtilisateurCommand ActiverDesactiver, GererRoleAdministrateurCommand GererRole, Guid PremierAdminId)
        ConstruireContexteAvecUnAdmin()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations déclarées", TypeClient.Normal, horloge.MaintenantUtc);
        societe.Valider();
        var premierAdmin = UtilisateurSociete.Rattacher(Guid.NewGuid(), societe.Id, horloge.MaintenantUtc, estAdministrateur: true);
        societe.EnregistrerAdministrateurActif();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(premierAdmin);
        dbContext.SaveChanges();

        var rattacher = new RattacherUtilisateurCommand(
            new UtilisateurSocieteRepository(dbContext), new JournalisationService(dbContext, horloge), horloge, new UniteDeTravail(dbContext));
        var activerDesactiver = new ActiverDesactiverUtilisateurCommand(
            new UtilisateurSocieteRepository(dbContext), new SocieteClienteRepository(dbContext),
            new JournalisationService(dbContext, horloge), new UniteDeTravail(dbContext));
        var gererRole = new GererRoleAdministrateurCommand(
            new UtilisateurSocieteRepository(dbContext), new SocieteClienteRepository(dbContext),
            new JournalisationService(dbContext, horloge), new UniteDeTravail(dbContext));

        return (dbContext, societe, rattacher, activerDesactiver, gererRole, premierAdmin.UtilisateurId);
    }

    /// <summary>Vérifie l'invariant directement contre la base : le compteur dénormalisé doit
    /// toujours être égal au COUNT réel des administrateurs actifs persistés.</summary>
    private static void VerifierInvariant(PlateformeDbContext dbContext, Guid societeId)
    {
        var societe = dbContext.Societes.Single(s => s.Id == societeId);
        var compteReel = dbContext.UtilisateursSociete.Count(u => u.SocieteId == societeId && u.EstAdministrateur && u.EstActif);
        Assert.Equal(compteReel, societe.NombreAdministrateursActifs);
    }

    [Fact]
    public async Task Attribution_du_role_a_un_utilisateur_actif_incremente_le_compteur()
    {
        var (dbContext, societe, rattacher, _, gererRole, premierAdminId) = ConstruireContexteAvecUnAdmin();
        var nouvelUtilisateur = await rattacher.ExecuterAsync(premierAdminId, societe.Id, Guid.NewGuid());

        await gererRole.AttribuerRoleAsync(premierAdminId, nouvelUtilisateur.Id);

        Assert.Equal(2, dbContext.Societes.Single(s => s.Id == societe.Id).NombreAdministrateursActifs);
        VerifierInvariant(dbContext, societe.Id);
    }

    [Fact]
    public async Task ECART_CORRIGE_Attribution_du_role_a_un_utilisateur_desactive_n_incremente_pas_le_compteur()
    {
        var (dbContext, societe, rattacher, activerDesactiver, gererRole, premierAdminId) = ConstruireContexteAvecUnAdmin();
        var collaborateur = await rattacher.ExecuterAsync(premierAdminId, societe.Id, Guid.NewGuid());
        await activerDesactiver.DesactiverAsync(premierAdminId, collaborateur.Id);

        // Avant correction (§6 de l'audit) : ce compteur passait à 2 malgré une cible désactivée.
        await gererRole.AttribuerRoleAsync(premierAdminId, collaborateur.Id);

        Assert.Equal(1, dbContext.Societes.Single(s => s.Id == societe.Id).NombreAdministrateursActifs);
        VerifierInvariant(dbContext, societe.Id);

        // Conséquence vérifiée : le seul administrateur RÉELLEMENT actif (premierAdminId) reste
        // protégé par CLAR-03 — il ne peut toujours pas se désactiver lui-même.
        await Assert.ThrowsAsync<DernierAdministrateurActifException>(() =>
            activerDesactiver.DesactiverAsync(premierAdminId, dbContext.UtilisateursSociete.Single(u => u.UtilisateurId == premierAdminId).Id));
    }

    [Fact]
    public async Task Retrait_du_role_d_un_administrateur_desactive_ne_decremente_pas_le_compteur()
    {
        var (dbContext, societe, rattacher, activerDesactiver, gererRole, premierAdminId) = ConstruireContexteAvecUnAdmin();
        var collaborateur = await rattacher.ExecuterAsync(premierAdminId, societe.Id, Guid.NewGuid());
        await gererRole.AttribuerRoleAsync(premierAdminId, collaborateur.Id); // actif → compteur = 2
        await activerDesactiver.DesactiverAsync(premierAdminId, collaborateur.Id); // désactivé → compteur = 1 (décrémenté ici)

        // Retirer le rôle d'un administrateur déjà désactivé ne doit PAS décrémenter à nouveau.
        await gererRole.RetirerRoleAsync(premierAdminId, collaborateur.Id);

        Assert.Equal(1, dbContext.Societes.Single(s => s.Id == societe.Id).NombreAdministrateursActifs);
        VerifierInvariant(dbContext, societe.Id);
    }

    [Fact]
    public async Task Activation_d_un_administrateur_inactif_incremente_le_compteur()
    {
        var (dbContext, societe, rattacher, activerDesactiver, gererRole, premierAdminId) = ConstruireContexteAvecUnAdmin();
        var collaborateur = await rattacher.ExecuterAsync(premierAdminId, societe.Id, Guid.NewGuid());
        await gererRole.AttribuerRoleAsync(premierAdminId, collaborateur.Id); // compteur = 2
        await activerDesactiver.DesactiverAsync(premierAdminId, collaborateur.Id); // compteur = 1
        VerifierInvariant(dbContext, societe.Id);

        await activerDesactiver.ActiverAsync(premierAdminId, collaborateur.Id); // réactivation → compteur = 2

        Assert.Equal(2, dbContext.Societes.Single(s => s.Id == societe.Id).NombreAdministrateursActifs);
        VerifierInvariant(dbContext, societe.Id);
    }

    [Fact]
    public async Task Desactivation_d_un_administrateur_actif_decremente_le_compteur()
    {
        var (dbContext, societe, rattacher, activerDesactiver, gererRole, premierAdminId) = ConstruireContexteAvecUnAdmin();
        var collaborateur = await rattacher.ExecuterAsync(premierAdminId, societe.Id, Guid.NewGuid());
        await gererRole.AttribuerRoleAsync(premierAdminId, collaborateur.Id); // compteur = 2

        await activerDesactiver.DesactiverAsync(premierAdminId, collaborateur.Id);

        Assert.Equal(1, dbContext.Societes.Single(s => s.Id == societe.Id).NombreAdministrateursActifs);
        VerifierInvariant(dbContext, societe.Id);
    }

    [Fact]
    public async Task Rattachement_d_un_non_administrateur_ne_modifie_jamais_le_compteur()
    {
        var (dbContext, societe, rattacher, activerDesactiver, _, premierAdminId) = ConstruireContexteAvecUnAdmin();

        var collaborateur = await rattacher.ExecuterAsync(premierAdminId, societe.Id, Guid.NewGuid());
        Assert.Equal(1, dbContext.Societes.Single(s => s.Id == societe.Id).NombreAdministrateursActifs);

        await activerDesactiver.DesactiverAsync(premierAdminId, collaborateur.Id);
        Assert.Equal(1, dbContext.Societes.Single(s => s.Id == societe.Id).NombreAdministrateursActifs);

        await activerDesactiver.ActiverAsync(premierAdminId, collaborateur.Id);
        Assert.Equal(1, dbContext.Societes.Single(s => s.Id == societe.Id).NombreAdministrateursActifs);

        VerifierInvariant(dbContext, societe.Id);
    }
}
