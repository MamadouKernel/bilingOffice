using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Comptes;

public sealed class ConnexionPremierClicTests : IClassFixture<PlateformeWebApplicationFactory>
{
    private const string MotDePasse = "MotDePasseValide123!";
    private readonly PlateformeWebApplicationFactory _factory;

    public ConnexionPremierClicTests(PlateformeWebApplicationFactory factory) => _factory = factory;

    private static HttpClient CreerClient(PlateformeWebApplicationFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost"),
        });

    private static string ExtraireJetonAntiforgery(string html) =>
        Regex.Match(html, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"").Groups[1].Value;

    private async Task<ApplicationUser> CreerUtilisateurActifAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = services.GetRequiredService<PlateformeDbContext>();

        var email = $"{Guid.NewGuid()}@premierclic.test";
        var utilisateur = new ApplicationUser { UserName = email, Email = email, NomAffiche = "Premier Clic User", Nature = NatureUtilisateur.Client };
        var creation = await userManager.CreateAsync(utilisateur, MotDePasse);
        Assert.True(creation.Succeeded, string.Join(" ", creation.Errors.Select(e => e.Description)));

        var societe = SocieteCliente.Soumettre(utilisateur.Id, "Entreprise Premier Clic", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        var rattachement = UtilisateurSociete.Rattacher(utilisateur.Id, societe.Id, DateTime.UtcNow, estAdministrateur: true);
        societe.EnregistrerAdministrateurActif();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(rattachement);
        await dbContext.SaveChangesAsync();

        return utilisateur;
    }

    [Fact]
    public async Task PremierClic_UneSeuleSoumission_AuthentifieEtRedirigeImmediatement_SansErreurAntiforgery()
    {
        // Arrange
        var client = CreerClient(_factory);
        var utilisateur = await CreerUtilisateurActifAsync();

        // 1. Chargement de la page de connexion
        var pageConnexionResponse = await client.GetAsync("/comptes/connexion");
        Assert.Equal(HttpStatusCode.OK, pageConnexionResponse.StatusCode);
        var html = await pageConnexionResponse.Content.ReadAsStringAsync();
        var jeton = ExtraireJetonAntiforgery(html);
        Assert.False(string.IsNullOrWhiteSpace(jeton), "Le jeton __RequestVerificationToken doit être présent dans le formulaire.");

        // 2. Première et unique soumission POST
        var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_handler"] = "connexion",
            ["__RequestVerificationToken"] = jeton,
            ["Modele.Email"] = utilisateur.Email!,
            ["Modele.MotDePasse"] = MotDePasse,
        });

        var postResponse = await client.PostAsync("/comptes/connexion", formContent);

        // Assert : La réponse ne doit JAMAIS être un 400 Bad Request (Antiforgery failure)
        Assert.NotEqual(HttpStatusCode.BadRequest, postResponse.StatusCode);

        // Assert : La réponse doit être une redirection (302) vers la destination d'accueil / dashboard
        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        var location = postResponse.Headers.Location?.ToString();
        Assert.NotNull(location);
        Assert.True(location.EndsWith("/") || location.Contains("dashboard"), $"La redirection doit pointer vers / ou un dashboard, actuel: {location}");

        // 3. Suivi de la redirection : L'utilisateur doit pouvoir accéder à une page protégée du premier coup
        var protectedResponse = await client.GetAsync("/client/demandes");
        Assert.Equal(HttpStatusCode.OK, protectedResponse.StatusCode);
    }

    [Fact]
    public async Task PremierClic_AvecReturnUrl_RedirigeVersReturnUrl_SansErreurAntiforgery()
    {
        // Arrange
        var client = CreerClient(_factory);
        var utilisateur = await CreerUtilisateurActifAsync();

        // 1. Accès via ReturnUrl
        var pageConnexionResponse = await client.GetAsync("/comptes/connexion?ReturnUrl=%2Fclient%2Fdemandes");
        Assert.Equal(HttpStatusCode.OK, pageConnexionResponse.StatusCode);
        var html = await pageConnexionResponse.Content.ReadAsStringAsync();
        var jeton = ExtraireJetonAntiforgery(html);

        // 2. Soumission
        var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_handler"] = "connexion",
            ["__RequestVerificationToken"] = jeton,
            ["Modele.Email"] = utilisateur.Email!,
            ["Modele.MotDePasse"] = MotDePasse,
        });

        var postResponse = await client.PostAsync("/comptes/connexion?ReturnUrl=%2Fclient%2Fdemandes", formContent);

        // Assert
        Assert.NotEqual(HttpStatusCode.BadRequest, postResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        var location = postResponse.Headers.Location?.ToString();
        Assert.NotNull(location);
        Assert.Contains("/client/demandes", location);
    }

    [Fact]
    public async Task PremierClic_CompteAvec2FA_RedirigeVersDefi2FA_SansErreurAntiforgery()
    {
        // Arrange
        var client = CreerClient(_factory);
        var utilisateur = await CreerUtilisateurActifAsync();

        // Activer la 2FA pour cet utilisateur
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(utilisateur.Id.ToString());
            await userManager.ResetAuthenticatorKeyAsync(user!);
            await userManager.SetTwoFactorEnabledAsync(user!, true);
        }

        // 1. Chargement de la page de connexion
        var pageConnexionResponse = await client.GetAsync("/comptes/connexion");
        Assert.Equal(HttpStatusCode.OK, pageConnexionResponse.StatusCode);
        var html = await pageConnexionResponse.Content.ReadAsStringAsync();
        var jeton = ExtraireJetonAntiforgery(html);

        // 2. Première soumission POST
        var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_handler"] = "connexion",
            ["__RequestVerificationToken"] = jeton,
            ["Modele.Email"] = utilisateur.Email!,
            ["Modele.MotDePasse"] = MotDePasse,
        });

        var postResponse = await client.PostAsync("/comptes/connexion", formContent);

        // Assert : Redirection 302 vers le défi 2FA sans erreur 400 Antiforgery
        Assert.NotEqual(HttpStatusCode.BadRequest, postResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        var location = postResponse.Headers.Location?.ToString();
        Assert.NotNull(location);
        Assert.Contains("verifier-code", location);
    }
}
