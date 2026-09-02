using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Comptes;

/// <summary>
/// Amorçage du premier administrateur — comble un silence du CDC.
///
/// L'écran d'administration des comptes (§ 17.3) exige une permission que personne ne détient au
/// premier démarrage d'une production, et qu'aucun chemin applicatif ne permet d'accorder. La
/// plateforme serait livrée verrouillée de l'extérieur.
///
/// CE QUI EST VÉRIFIÉ ICI EST LA PROPRIÉTÉ DE SÛRETÉ, pas le confort : ce mécanisme ne doit
/// jamais devenir une porte dérobée. Il est donc INERTE dès qu'un administrateur existe — il ne
/// peut ni recréer un compte supprimé, ni réhabiliter un compte désactivé, ni s'exécuter deux
/// fois. Le substitut de <see cref="UserManager{TUser}"/> est volontairement strict : toute
/// tentative d'écriture le ferait échouer, ce qui rend l'inertie démontrable plutôt que déclarée.
/// </summary>
public sealed class ProvisionnementAdministrateurInitialTests
{
    /// <summary>Substitut strict : aucune méthode n'est configurée, donc toute écriture échoue.</summary>
    private static UserManager<ApplicationUser> UserManagerIntouchable() =>
        new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!)
        { CallBase = false }.Object;

    private static IConfiguration Configuration(params (string Cle, string Valeur)[] valeurs) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(valeurs.Select(v => new KeyValuePair<string, string?>(v.Cle, v.Valeur)))
            .Build();

    private static ProvisionnementAdministrateurInitial Construire(
        PlateformeDbContext dbContext, IConfiguration configuration) =>
        new(dbContext, UserManagerIntouchable(), configuration,
            NullLogger<ProvisionnementAdministrateurInitial>.Instance);

    /// <summary>
    /// LA PROPRIÉTÉ DÉCISIVE : dès qu'un compte détient la permission, l'amorçage ne fait plus
    /// rien — même si la configuration désigne quelqu'un d'autre. Sans cela, un secret laissé
    /// dans la configuration rouvrirait un accès à chaque redémarrage.
    /// </summary>
    [Fact]
    public async Task Des_qu_un_administrateur_existe_l_amorcage_est_inerte()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var administrateurEnPlace = Guid.NewGuid();
        dbContext.Habilitations.Add(
            HabilitationUtilisateur.Attribuer(administrateurEnPlace, Permission.AdministrationComptes));
        await dbContext.SaveChangesAsync();

        var provisionnement = Construire(dbContext, Configuration(
            ("Administration:AdministrateurInitial:Email", "intrus@ailleurs.test"),
            ("Administration:AdministrateurInitial:MotDePasse", "MotDePasse123!")));

        // Ne lève pas : le substitut strict n'est jamais sollicité.
        await provisionnement.ExecuterAsync();

        var habilitation = Assert.Single(
            dbContext.Habilitations.Where(h => h.PermissionCode == Permission.AdministrationComptes));
        Assert.Equal(administrateurEnPlace, habilitation.UtilisateurId);
    }

    /// <summary>
    /// Une configuration absente ne doit pas empêcher le démarrage : une plateforme qui refuse de
    /// démarrer est plus difficile à diagnostiquer qu'une plateforme qui signale ce qui lui manque.
    /// </summary>
    [Fact]
    public async Task Sans_configuration_l_amorcage_n_echoue_pas_et_n_habilite_personne()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();

        var provisionnement = Construire(dbContext, Configuration());

        await provisionnement.ExecuterAsync();

        Assert.Empty(dbContext.Habilitations.Where(h => h.PermissionCode == Permission.AdministrationComptes));
    }

    /// <summary>Une adresse sans mot de passe est une configuration incomplète, pas une intention.</summary>
    [Fact]
    public async Task Une_configuration_incomplete_n_habilite_personne()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();

        var provisionnement = Construire(dbContext, Configuration(
            ("Administration:AdministrateurInitial:Email", "admin@cit.test")));

        await provisionnement.ExecuterAsync();

        Assert.Empty(dbContext.Habilitations.Where(h => h.PermissionCode == Permission.AdministrationComptes));
    }
}
