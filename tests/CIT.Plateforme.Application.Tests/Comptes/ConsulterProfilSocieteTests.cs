using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Comptes;

/// <summary>
/// [TEST-SPEC] Exigence CDC: CPT-04, ENT-01, ENT-03 → US4 → critère d'acceptation 1 de US4
/// (FR-004) → Test.
/// </summary>
public sealed class ConsulterProfilSocieteTests
{
    private static (PlateformeDbContext DbContext, ConsulterProfilSocieteQuery Query) Construire()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var query = new ConsulterProfilSocieteQuery(
            new SocieteClienteRepository(dbContext), new UtilisateurSocieteRepository(dbContext), new AutorisationService(dbContext));
        return (dbContext, query);
    }

    [Fact]
    public async Task Un_utilisateur_de_la_societe_peut_consulter_le_profil()
    {
        var (dbContext, query) = Construire();
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations déclarées", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        var utilisateur = UtilisateurSociete.Rattacher(Guid.NewGuid(), societe.Id, DateTime.UtcNow);
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(utilisateur);
        await dbContext.SaveChangesAsync();

        var profil = await query.ExecuterAsync(utilisateur.UtilisateurId, societe.Id);

        Assert.Equal(societe.Id, profil.Societe.Id);
        Assert.Single(profil.UtilisateursRattaches);
    }

    [Fact]
    public async Task Un_utilisateur_d_une_autre_societe_ne_peut_pas_consulter_le_profil()
    {
        var (dbContext, query) = Construire();
        var societeA = SocieteCliente.Soumettre(Guid.NewGuid(), "Société A", TypeClient.Normal, DateTime.UtcNow);
        societeA.Valider();
        var societeB = SocieteCliente.Soumettre(Guid.NewGuid(), "Société B", TypeClient.Normal, DateTime.UtcNow);
        societeB.Valider();
        var utilisateurB = UtilisateurSociete.Rattacher(Guid.NewGuid(), societeB.Id, DateTime.UtcNow);
        dbContext.Societes.AddRange(societeA, societeB);
        dbContext.UtilisateursSociete.Add(utilisateurB);
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<AccesRefuseException>(() => query.ExecuterAsync(utilisateurB.UtilisateurId, societeA.Id));
    }
}
