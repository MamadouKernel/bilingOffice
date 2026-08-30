using System.Text.RegularExpressions;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Comptes;

/// <summary>
/// [TEST-SPEC] Exigence CDC: CPT-03 → US2 → critère d'acceptation 4 de US2 (FR-003) → Test.
/// </summary>
public sealed class RecuperationMotDePasseTests : IClassFixture<PlateformeWebApplicationFactory>
{
    private readonly PlateformeWebApplicationFactory factory;

    public RecuperationMotDePasseTests(PlateformeWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task Nouveau_mot_de_passe_non_conforme_a_la_politique_est_refuse()
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var utilisateur = new ApplicationUser
        {
            UserName = $"{Guid.NewGuid()}@exemple.test",
            Email = $"{Guid.NewGuid()}@exemple.test",
            NomAffiche = "Test",
            Nature = NatureUtilisateur.Client,
        };
        var creation = await userManager.CreateAsync(utilisateur, "MotDePasseValide123!");
        Assert.True(creation.Succeeded);

        var jeton = await userManager.GeneratePasswordResetTokenAsync(utilisateur);

        // Politique de robustesse déjà configurée (Feature 001, RequiredLength = 10) — un mot de
        // passe trop court doit être refusé.
        var resultat = await userManager.ResetPasswordAsync(utilisateur, jeton, "court1!");

        Assert.False(resultat.Succeeded);
    }

    [Fact]
    public async Task Nouveau_mot_de_passe_conforme_est_accepte()
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var utilisateur = new ApplicationUser
        {
            UserName = $"{Guid.NewGuid()}@exemple.test",
            Email = $"{Guid.NewGuid()}@exemple.test",
            NomAffiche = "Test",
            Nature = NatureUtilisateur.Client,
        };
        var creation = await userManager.CreateAsync(utilisateur, "MotDePasseValide123!");
        Assert.True(creation.Succeeded);

        var jeton = await userManager.GeneratePasswordResetTokenAsync(utilisateur);

        var resultat = await userManager.ResetPasswordAsync(utilisateur, jeton, "NouveauMotDePasseValide456!");

        Assert.True(resultat.Succeeded);
    }

    /// <summary>
    /// [TEST-TECH] Remédiation post-audit (2026-08-29) — régression : le formulaire réel de
    /// <c>MotDePasseOublie.razor</c> ne liait auparavant aucune valeur (propriété
    /// <c>[SupplyParameterFromForm]</c> nommée différemment du <c>Model</c> de l'<c>EditForm</c>,
    /// même défaut que celui corrigé pour CPT-07 — voir Connexion.razor). Ce test exerce le
    /// formulaire par une vraie requête HTTP, pas un appel direct à <c>UserManager</c>.
    /// </summary>
    [Fact]
    public async Task Le_formulaire_reel_de_demande_de_reinitialisation_lie_correctement_l_email_saisi()
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = $"{Guid.NewGuid()}@exemple.test";
        var utilisateur = new ApplicationUser
        {
            UserName = email,
            Email = email,
            NomAffiche = "Test",
            Nature = NatureUtilisateur.Client,
        };
        Assert.True((await userManager.CreateAsync(utilisateur, "MotDePasseValide123!")).Succeeded);

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost"),
        });

        var pageDemande = await client.GetStringAsync("/comptes/mot-de-passe-oublie");
        var jeton = Regex.Match(pageDemande, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"").Groups[1].Value;

        var reponse = await client.PostAsync("/comptes/mot-de-passe-oublie", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_handler"] = "mot-de-passe-oublie",
            ["__RequestVerificationToken"] = jeton,
            ["Modele.Email"] = email,
        }));

        var corps = await reponse.Content.ReadAsStringAsync();

        // Avant correction : l'e-mail reçu server-side était toujours vide, `FindByEmailAsync`
        // retournait null, et aucun lien n'était jamais généré (silencieusement, sans erreur
        // visible — un « faux succès » indétectable sans test HTTP réel).
        Assert.Contains("reinitialiser-mot-de-passe", corps);
        Assert.Contains(utilisateur.Id.ToString(), corps);
    }
}
