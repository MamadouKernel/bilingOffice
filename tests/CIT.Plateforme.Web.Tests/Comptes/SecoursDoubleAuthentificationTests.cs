using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Comptes;

/// <summary>
/// SORTIES DE SECOURS DE LA 2FA OBLIGATOIRE, sur la vraie pile Identity : code de secours à usage
/// unique, et réinitialisation administrateur (2FA désactivée, clé et codes invalidés, sessions
/// révoquées par renouvellement du jeton de sécurité).
/// </summary>
public sealed class SecoursDoubleAuthentificationTests(PlateformeWebApplicationFactory factory) : IClassFixture<PlateformeWebApplicationFactory>
{
    private static async Task<ApplicationUser> CompteAvec2FaAsync(UserManager<ApplicationUser> userManager)
    {
        var compte = new ApplicationUser
        {
            UserName = $"{Guid.NewGuid()}@exemple.test",
            Email = $"{Guid.NewGuid()}@exemple.test",
            NomAffiche = "Agent 2FA",
            Nature = NatureUtilisateur.Agent,
        };
        Assert.True((await userManager.CreateAsync(compte, "MotDePasseValide123!")).Succeeded);
        await userManager.ResetAuthenticatorKeyAsync(compte);
        await userManager.SetTwoFactorEnabledAsync(compte, true);
        return compte;
    }

    [Fact]
    public async Task Un_code_de_secours_ne_sert_qu_une_seule_fois()
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var compte = await CompteAvec2FaAsync(userManager);

        var codes = (await userManager.GenerateNewTwoFactorRecoveryCodesAsync(compte, 10))!.ToList();

        Assert.Equal(10, codes.Count);
        Assert.True((await userManager.RedeemTwoFactorRecoveryCodeAsync(compte, codes[0])).Succeeded);
        Assert.False((await userManager.RedeemTwoFactorRecoveryCodeAsync(compte, codes[0])).Succeeded);
        Assert.Equal(9, await userManager.CountRecoveryCodesAsync(compte));
    }

    [Fact]
    public async Task La_reinitialisation_desactive_la_2fa_invalide_les_codes_et_revoque_les_sessions()
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var compte = await CompteAvec2FaAsync(userManager);
        await userManager.GenerateNewTwoFactorRecoveryCodesAsync(compte, 10);
        var cleAvant = await userManager.GetAuthenticatorKeyAsync(compte);
        var jetonAvant = await userManager.GetSecurityStampAsync(compte);

        var ok = await scope.ServiceProvider.GetRequiredService<IDoubleAuthentificationAdministration>()
            .ReinitialiserAsync(compte.Id);

        var apres = (await userManager.FindByIdAsync(compte.Id.ToString()))!;
        Assert.True(ok);
        Assert.False(await userManager.GetTwoFactorEnabledAsync(apres));
        Assert.NotEqual(cleAvant, await userManager.GetAuthenticatorKeyAsync(apres));
        Assert.Equal(0, await userManager.CountRecoveryCodesAsync(apres));
        Assert.NotEqual(jetonAvant, await userManager.GetSecurityStampAsync(apres));
    }
}
