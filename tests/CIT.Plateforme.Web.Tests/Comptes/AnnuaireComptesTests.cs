using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Comptes;

/// <summary>
/// ANNUAIRE DES COMPTES — tous les comptes sont visibles et correctement classés (agent,
/// transporteur, client sans société) ; la lecture est réservée aux administrateurs des comptes.
/// </summary>
public sealed class AnnuaireComptesTests(PlateformeWebApplicationFactory factory) : IClassFixture<PlateformeWebApplicationFactory>
{
    private static async Task<ApplicationUser> CreerAsync(UserManager<ApplicationUser> userManager, NatureUtilisateur nature)
    {
        var compte = new ApplicationUser
        {
            UserName = $"{Guid.NewGuid()}@exemple.test",
            Email = $"{Guid.NewGuid()}@exemple.test",
            NomAffiche = $"Compte {nature}",
            Nature = nature,
        };
        Assert.True((await userManager.CreateAsync(compte, "MotDePasseValide123!")).Succeeded);
        return compte;
    }

    [Fact]
    public async Task L_annuaire_classe_agents_transporteurs_et_clients_sans_societe()
    {
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        var agent = await CreerAsync(userManager, NatureUtilisateur.Agent);
        var transporteur = await CreerAsync(userManager, NatureUtilisateur.Client);
        var clientSansSociete = await CreerAsync(userManager, NatureUtilisateur.Client);

        var habilitations = sp.GetRequiredService<IHabilitationRepository>();
        await habilitations.AjouterAsync(HabilitationUtilisateur.Attribuer(transporteur.Id, Permission.EnrolementSoumettre));
        await habilitations.AjouterAsync(HabilitationUtilisateur.Attribuer(agent.Id, Permission.AdministrationComptes));
        await sp.GetRequiredService<IUniteDeTravail>().EnregistrerAsync();

        var comptes = await sp.GetRequiredService<ListerComptesUtilisateursQuery>().ExecuterAsync(agent.Id);

        Assert.Equal(CategorieCompte.AgentCit, comptes.Single(c => c.Id == agent.Id).Categorie);
        Assert.Equal(CategorieCompte.Transporteur, comptes.Single(c => c.Id == transporteur.Id).Categorie);
        Assert.Equal(CategorieCompte.ClientSansSociete, comptes.Single(c => c.Id == clientSansSociete.Id).Categorie);
    }

    [Fact]
    public async Task L_annuaire_est_refuse_sans_habilitation()
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var sansDroit = await CreerAsync(userManager, NatureUtilisateur.Agent);

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            scope.ServiceProvider.GetRequiredService<ListerComptesUtilisateursQuery>().ExecuterAsync(sansDroit.Id));
    }
}
