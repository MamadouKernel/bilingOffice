using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Comptes;

/// <summary>
/// INVITATION D'AGENT — un compte agent créé sans mot de passe initial n'a AUCUN secret : ni
/// l'administrateur ni personne d'autre ne le connaît tant que l'agent ne l'a pas défini lui-même
/// par le lien reçu par e-mail.
/// </summary>
public sealed class InvitationAgentTests(PlateformeWebApplicationFactory factory) : IClassFixture<PlateformeWebApplicationFactory>
{
    [Fact]
    public void Les_liens_de_definition_du_mot_de_passe_sont_valables_deux_heures()
    {
        var options = factory.Services.GetRequiredService<IOptions<DataProtectionTokenProviderOptions>>().Value;

        Assert.Equal(TimeSpan.FromHours(2), options.TokenLifespan);
    }

    [Fact]
    public async Task Un_agent_cree_par_invitation_n_a_aucun_mot_de_passe()
    {
        using var scope = factory.Services.CreateScope();
        var comptes = scope.ServiceProvider.GetRequiredService<IAdministrationComptesRepository>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var resultat = await comptes.CreerAgentAsync($"{Guid.NewGuid()}@exemple.test", "Agent invité", motDePasseInitial: string.Empty);

        Assert.True(resultat.Succes);
        var compte = await userManager.FindByIdAsync(resultat.UtilisateurId.ToString());
        Assert.NotNull(compte);
        Assert.False(await userManager.HasPasswordAsync(compte!));
    }

    [Fact]
    public async Task L_agent_invite_definit_lui_meme_son_mot_de_passe_avec_le_jeton()
    {
        using var scope = factory.Services.CreateScope();
        var comptes = scope.ServiceProvider.GetRequiredService<IAdministrationComptesRepository>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var resultat = await comptes.CreerAgentAsync($"{Guid.NewGuid()}@exemple.test", "Agent invité", motDePasseInitial: string.Empty);
        var compte = (await userManager.FindByIdAsync(resultat.UtilisateurId.ToString()))!;

        // Même jeton que celui porté par le lien d'invitation (EnvoiLienMotDePasse).
        var jeton = await userManager.GeneratePasswordResetTokenAsync(compte);
        var definition = await userManager.ResetPasswordAsync(compte, jeton, "MotDePasseValide123!");

        Assert.True(definition.Succeeded);
        Assert.True(await userManager.HasPasswordAsync(compte));
    }
}
