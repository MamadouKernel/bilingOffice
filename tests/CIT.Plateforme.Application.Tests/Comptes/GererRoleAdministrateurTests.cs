using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Comptes;

/// <summary>
/// [TEST-SPEC — DOIT ÊTRE ÉCRIT ET ÉCHOUER AVANT L'IMPLÉMENTATION DE
/// <see cref="GererRoleAdministrateurCommand"/>] Exigence CDC: ENT-02 → US3 → critère
/// d'acceptation 6 de US3 (FR-009) → Test — CLAR-03.
/// </summary>
public sealed class GererRoleAdministrateurTests
{
    private static (PlateformeDbContext DbContext, GererRoleAdministrateurCommand Commande, SocieteCliente Societe) ConstruireContexte()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations déclarées", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        dbContext.Societes.Add(societe);

        var horloge = HorlogeDeTest.Defaut();
        var commande = new GererRoleAdministrateurCommand(
            new UtilisateurSocieteRepository(dbContext), new SocieteClienteRepository(dbContext),
            new JournalisationService(dbContext, horloge), new UniteDeTravail(dbContext));

        return (dbContext, commande, societe);
    }

    [Fact]
    public async Task Retirer_le_role_du_dernier_administrateur_actif_est_refuse_meme_en_auto_retrait()
    {
        var (dbContext, commande, societe) = ConstruireContexte();
        var seulAdmin = UtilisateurSociete.Rattacher(Guid.NewGuid(), societe.Id, DateTime.UtcNow, estAdministrateur: true);
        societe.EnregistrerAdministrateurActif();
        dbContext.UtilisateursSociete.Add(seulAdmin);
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<DernierAdministrateurActifException>(() =>
            commande.RetirerRoleAsync(seulAdmin.UtilisateurId, seulAdmin.Id));

        var inchange = dbContext.UtilisateursSociete.Single(u => u.Id == seulAdmin.Id);
        Assert.True(inchange.EstAdministrateur);
    }

    [Fact]
    public async Task Attribuer_le_role_a_un_nouvel_utilisateur_reussit_sans_limite_de_nombre()
    {
        var (dbContext, commande, societe) = ConstruireContexte();
        var adminExistant = UtilisateurSociete.Rattacher(Guid.NewGuid(), societe.Id, DateTime.UtcNow, estAdministrateur: true);
        var futurAdmin = UtilisateurSociete.Rattacher(Guid.NewGuid(), societe.Id, DateTime.UtcNow);
        societe.EnregistrerAdministrateurActif();
        dbContext.UtilisateursSociete.AddRange(adminExistant, futurAdmin);
        await dbContext.SaveChangesAsync();

        await commande.AttribuerRoleAsync(adminExistant.UtilisateurId, futurAdmin.Id);

        var miseAJour = dbContext.UtilisateursSociete.Single(u => u.Id == futurAdmin.Id);
        Assert.True(miseAJour.EstAdministrateur);
        var societeMiseAJour = dbContext.Societes.Single(s => s.Id == societe.Id);
        Assert.Equal(2, societeMiseAJour.NombreAdministrateursActifs);
    }
}
