using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Comptes;

/// <summary>
/// [TEST-SPEC — DOIT ÊTRE ÉCRIT ET ÉCHOUER AVANT L'IMPLÉMENTATION DE
/// <see cref="ActiverDesactiverUtilisateurCommand"/>] Règle métier critique — invariant du dernier
/// administrateur actif. Exigence CDC: ENT-02 → US3 → critère d'acceptation 6 de US3 (FR-009) →
/// Test — CLAR-03.
/// </summary>
public sealed class DesactivationDernierAdministrateurTests
{
    private static (PlateformeDbContext DbContext, ActiverDesactiverUtilisateurCommand Commande, SocieteCliente Societe) ConstruireContexte()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations déclarées", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        dbContext.Societes.Add(societe);

        var horloge = HorlogeDeTest.Defaut();
        var commande = new ActiverDesactiverUtilisateurCommand(
            new UtilisateurSocieteRepository(dbContext), new SocieteClienteRepository(dbContext),
            new JournalisationService(dbContext, horloge), new UniteDeTravail(dbContext));

        return (dbContext, commande, societe);
    }

    [Fact]
    public async Task Desactiver_le_dernier_administrateur_actif_est_refuse()
    {
        var (dbContext, commande, societe) = ConstruireContexte();
        var seulAdmin = UtilisateurSociete.Rattacher(Guid.NewGuid(), societe.Id, DateTime.UtcNow, estAdministrateur: true);
        societe.EnregistrerAdministrateurActif();
        dbContext.UtilisateursSociete.Add(seulAdmin);
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<DernierAdministrateurActifException>(() =>
            commande.DesactiverAsync(seulAdmin.UtilisateurId, seulAdmin.Id));

        var rattachementInchange = dbContext.UtilisateursSociete.Single(u => u.Id == seulAdmin.Id);
        Assert.True(rattachementInchange.EstActif);
    }

    [Fact]
    public async Task Desactiver_un_administrateur_alors_qu_un_autre_reste_actif_reussit()
    {
        var (dbContext, commande, societe) = ConstruireContexte();
        var admin1 = UtilisateurSociete.Rattacher(Guid.NewGuid(), societe.Id, DateTime.UtcNow, estAdministrateur: true);
        var admin2 = UtilisateurSociete.Rattacher(Guid.NewGuid(), societe.Id, DateTime.UtcNow, estAdministrateur: true);
        societe.EnregistrerAdministrateurActif();
        societe.EnregistrerAdministrateurActif();
        dbContext.UtilisateursSociete.AddRange(admin1, admin2);
        await dbContext.SaveChangesAsync();

        await commande.DesactiverAsync(admin1.UtilisateurId, admin2.Id);

        var admin2MisAJour = dbContext.UtilisateursSociete.Single(u => u.Id == admin2.Id);
        Assert.False(admin2MisAJour.EstActif);
        var societeMiseAJour = dbContext.Societes.Single(s => s.Id == societe.Id);
        Assert.Equal(1, societeMiseAJour.NombreAdministrateursActifs);
    }
}
