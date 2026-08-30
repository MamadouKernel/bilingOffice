using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Comptes;

/// <summary>
/// [TEST-SPEC] Exigence CDC: ENT-01, ENT-02, CPT-05 (renvoi) → US3 → critères d'acceptation 1, 3,
/// 4 de US3 (FR-005, FR-008, FR-009) → Test.
/// </summary>
public sealed class RattacherUtilisateurTests
{
    private static PlateformeDbContext Contexte() => ContexteDeTest.CreerNouveauContexte();

    private static SocieteCliente AjouterSocieteActive(PlateformeDbContext dbContext)
    {
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations déclarées", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        dbContext.Societes.Add(societe);
        return societe;
    }

    private static UtilisateurSociete AjouterAdministrateur(PlateformeDbContext dbContext, Guid societeId)
    {
        var admin = UtilisateurSociete.Rattacher(Guid.NewGuid(), societeId, DateTime.UtcNow, estAdministrateur: true);
        dbContext.UtilisateursSociete.Add(admin);
        return admin;
    }

    [Fact]
    public async Task Rattachement_reussi_avec_droits_initiaux_par_defaut()
    {
        var dbContext = Contexte();
        var societe = AjouterSocieteActive(dbContext);
        var admin = AjouterAdministrateur(dbContext, societe.Id);
        await dbContext.SaveChangesAsync();

        var horloge = HorlogeDeTest.Defaut();
        var commande = new RattacherUtilisateurCommand(
            new UtilisateurSocieteRepository(dbContext), new JournalisationService(dbContext, horloge), horloge, new UniteDeTravail(dbContext));

        var nouvelUtilisateurId = Guid.NewGuid();
        var rattachement = await commande.ExecuterAsync(admin.UtilisateurId, societe.Id, nouvelUtilisateurId);

        Assert.Equal(DroitConsultation.PropresDemandesUniquement, rattachement.DroitConsultation);
        Assert.True(rattachement.DroitSoumission);
        Assert.True(rattachement.EstActif);
    }

    [Fact]
    public async Task Un_administrateur_ne_peut_rattacher_un_utilisateur_qu_a_sa_propre_societe()
    {
        var dbContext = Contexte();
        var societeA = AjouterSocieteActive(dbContext);
        var societeB = AjouterSocieteActive(dbContext);
        var adminSocieteA = AjouterAdministrateur(dbContext, societeA.Id);
        await dbContext.SaveChangesAsync();

        var horloge = HorlogeDeTest.Defaut();
        var commande = new RattacherUtilisateurCommand(
            new UtilisateurSocieteRepository(dbContext), new JournalisationService(dbContext, horloge), horloge, new UniteDeTravail(dbContext));

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            commande.ExecuterAsync(adminSocieteA.UtilisateurId, societeB.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task Desactivation_d_un_utilisateur_non_administrateur_reussit_sans_condition()
    {
        var dbContext = Contexte();
        var societe = AjouterSocieteActive(dbContext);
        var admin = AjouterAdministrateur(dbContext, societe.Id);
        var collaborateur = UtilisateurSociete.Rattacher(Guid.NewGuid(), societe.Id, DateTime.UtcNow);
        dbContext.UtilisateursSociete.Add(collaborateur);
        await dbContext.SaveChangesAsync();

        var horloge = HorlogeDeTest.Defaut();
        var commande = new ActiverDesactiverUtilisateurCommand(
            new UtilisateurSocieteRepository(dbContext), new SocieteClienteRepository(dbContext),
            new JournalisationService(dbContext, horloge), new UniteDeTravail(dbContext));

        await commande.DesactiverAsync(admin.UtilisateurId, collaborateur.Id);

        var miseAJour = dbContext.UtilisateursSociete.Single(u => u.Id == collaborateur.Id);
        Assert.False(miseAJour.EstActif);
    }

    [Fact]
    public async Task Un_appelant_non_administrateur_ne_peut_pas_desactiver_un_collaborateur()
    {
        var dbContext = Contexte();
        var societe = AjouterSocieteActive(dbContext);
        var collaborateurSansDroit = UtilisateurSociete.Rattacher(Guid.NewGuid(), societe.Id, DateTime.UtcNow);
        var cible = UtilisateurSociete.Rattacher(Guid.NewGuid(), societe.Id, DateTime.UtcNow);
        dbContext.UtilisateursSociete.AddRange(collaborateurSansDroit, cible);
        await dbContext.SaveChangesAsync();

        var horloge = HorlogeDeTest.Defaut();
        var commande = new ActiverDesactiverUtilisateurCommand(
            new UtilisateurSocieteRepository(dbContext), new SocieteClienteRepository(dbContext),
            new JournalisationService(dbContext, horloge), new UniteDeTravail(dbContext));

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            commande.DesactiverAsync(collaborateurSansDroit.UtilisateurId, cible.Id));
    }
}
