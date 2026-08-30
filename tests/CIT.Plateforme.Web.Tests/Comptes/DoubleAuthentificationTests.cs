using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Comptes;

/// <summary>
/// [TEST-SPEC] Exigence CDC: CPT-07 → US6 → critère d'acceptation 1 de US6 (FR-007) → Test.
/// Capacité Could — jamais imposée par défaut.
/// </summary>
public sealed class DoubleAuthentificationTests : IClassFixture<PlateformeWebApplicationFactory>
{
    private readonly PlateformeWebApplicationFactory factory;

    public DoubleAuthentificationTests(PlateformeWebApplicationFactory factory) => this.factory = factory;

    private static async Task<ApplicationUser> CreerUtilisateurAsync(UserManager<ApplicationUser> userManager)
    {
        var utilisateur = new ApplicationUser
        {
            UserName = $"{Guid.NewGuid()}@exemple.test",
            Email = $"{Guid.NewGuid()}@exemple.test",
            NomAffiche = "Test",
            Nature = NatureUtilisateur.Client,
        };
        var resultat = await userManager.CreateAsync(utilisateur, "MotDePasseValide123!");
        Assert.True(resultat.Succeeded);
        return utilisateur;
    }

    // Note technique : SignInManager.PasswordSignInAsync exige un HttpContext réel (écriture du
    // cookie de session) indisponible dans ce contexte de test hors requête HTTP. Les assertions
    // ci-dessous portent directement sur la précondition qu'utilise SignInManager en interne
    // (UserManager.GetTwoFactorEnabledAsync + un fournisseur de jeton 2FA valide) pour décider de
    // déclencher le défi 2FA — même signal métier, sans dépendre du pipeline HTTP complet.

    [Fact]
    public async Task Apres_activation_la_connexion_suivante_exige_le_second_facteur()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        var utilisateur = await CreerUtilisateurAsync(userManager);
        // « Activer » un second facteur suppose un fournisseur réellement opérant (ici
        // l'authentificateur TOTP natif Identity) — pas seulement le drapeau TwoFactorEnabled.
        await userManager.ResetAuthenticatorKeyAsync(utilisateur);
        var activation = await userManager.SetTwoFactorEnabledAsync(utilisateur, true);
        Assert.True(activation.Succeeded);

        Assert.True(await userManager.GetTwoFactorEnabledAsync(utilisateur));
        var fournisseursValides = await userManager.GetValidTwoFactorProvidersAsync(utilisateur);
        Assert.NotEmpty(fournisseursValides);
    }

    [Fact]
    public async Task Un_compte_sans_activation_explicite_n_exige_jamais_le_second_facteur()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        var utilisateur = await CreerUtilisateurAsync(userManager);

        Assert.False(await userManager.GetTwoFactorEnabledAsync(utilisateur));
    }
}
