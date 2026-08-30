using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Comptes;

/// <summary>
/// [TEST-SPEC] Exigence CDC: ENT-02 → US3 → critère d'acceptation 2 de US3 (FR-009) → Test.
/// </summary>
public sealed class ModifierDroitsUtilisateurTests
{
    [Fact]
    public async Task Modification_des_droits_appliquee_immediatement()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var societeId = Guid.NewGuid();
        var admin = UtilisateurSociete.Rattacher(Guid.NewGuid(), societeId, DateTime.UtcNow, estAdministrateur: true);
        var collaborateur = UtilisateurSociete.Rattacher(Guid.NewGuid(), societeId, DateTime.UtcNow);
        dbContext.UtilisateursSociete.AddRange(admin, collaborateur);
        await dbContext.SaveChangesAsync();

        var horloge = HorlogeDeTest.Defaut();
        var commande = new ModifierDroitsUtilisateurCommand(
            new UtilisateurSocieteRepository(dbContext), new JournalisationService(dbContext, horloge), new UniteDeTravail(dbContext));

        await commande.ExecuterAsync(admin.UtilisateurId, collaborateur.Id, DroitConsultation.ToutesDemandesSociete, droitSoumission: false);

        var miseAJour = dbContext.UtilisateursSociete.Single(u => u.Id == collaborateur.Id);
        Assert.Equal(DroitConsultation.ToutesDemandesSociete, miseAJour.DroitConsultation);
        Assert.False(miseAJour.DroitSoumission);
    }
}
