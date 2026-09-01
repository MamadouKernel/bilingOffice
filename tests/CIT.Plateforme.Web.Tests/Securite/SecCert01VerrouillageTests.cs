using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
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

namespace CIT.Plateforme.Web.Tests.Securite;

/// <summary>
/// FEATURE 011 — CERTIFICATION SEC-01 · suite TEST-SPEC « SEC-CERT-01 » (T007→T011).
///
/// Texte CDC (SEC-01, Must) : « Authentification obligatoire pour tout accès ; politique de
/// mots de passe robuste ; verrouillage après échecs répétés. »
///
/// POURQUOI CETTE SUITE EXISTE : l'audit F011 (research.md D1) a constaté que quatre suites
/// nomment SEC-01, mais qu'elles couvrent l'AUTORISATION, jamais le VERROUILLAGE. La recherche
/// de MaxFailedAccessAttempts / LockoutEnd / AccessFailedCount / IsLockedOut dans tests/
/// retournait ZÉRO occurrence. Le troisième volet du CDC n'était donc démontré par aucun test.
///
/// AUCUN CODE PRODUIT N'EST MODIFIÉ : la politique de verrouillage est déjà configurée. Cette
/// suite la CERTIFIE, elle ne la crée pas.
/// </summary>
public sealed class SecCert01VerrouillageTests : IClassFixture<PlateformeWebApplicationFactory>
{
    private const string MotDePasse = "MotDePasseValide123!";
    private const string MauvaisMotDePasse = "MauvaisMotDePasse999!";

    private readonly PlateformeWebApplicationFactory factory;

    public SecCert01VerrouillageTests(PlateformeWebApplicationFactory factory) => this.factory = factory;

    // BaseAddress https:// — le cookie Identity est marqué Secure ; sans cela le CookieContainer
    // ne le renverrait jamais (même patron que les suites de connexion existantes).
    private HttpClient CreerClient() => factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true,
        BaseAddress = new Uri("https://localhost"),
    });

    private static string ExtraireJetonAntiforgery(string html) =>
        Regex.Match(html, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"").Groups[1].Value;

    private async Task<ApplicationUser> CreerUtilisateurActifAsync()
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();

        var email = $"{Guid.NewGuid()}@exemple.test";
        var utilisateur = new ApplicationUser
        {
            UserName = email,
            Email = email,
            NomAffiche = "Certification SEC-01",
            Nature = NatureUtilisateur.Client,
            EmailConfirmed = true,
            EstActif = true,
        };

        var creation = await userManager.CreateAsync(utilisateur, MotDePasse);
        Assert.True(creation.Succeeded, string.Join(" ", creation.Errors.Select(e => e.Description)));

        var societe = SocieteCliente.Soumettre(utilisateur.Id, "Certification SEC-01", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        societe.EnregistrerAdministrateurActif();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(
            UtilisateurSociete.Rattacher(utilisateur.Id, societe.Id, DateTime.UtcNow, estAdministrateur: true));
        await dbContext.SaveChangesAsync();

        return utilisateur;
    }

    private async Task<HttpResponseMessage> TenterConnexionAsync(HttpClient client, string email, string motDePasse)
    {
        var page = await client.GetStringAsync("/comptes/connexion");
        var jeton = ExtraireJetonAntiforgery(page);

        return await client.PostAsync("/comptes/connexion", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_handler"] = "connexion",
            ["__RequestVerificationToken"] = jeton,
            ["Modele.Email"] = email,
            ["Modele.MotDePasse"] = motDePasse,
        }));
    }

    private async Task<bool> EstConnecteAsync(HttpClient client)
    {
        var reponse = await client.GetAsync("/client/demandes");
        return reponse.StatusCode == HttpStatusCode.OK;
    }

    private async Task<ApplicationUser> RelireAsync(Guid utilisateurId)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return (await userManager.FindByIdAsync(utilisateurId.ToString()))!;
    }

    // ---- T007 : nominal — l'authentification fonctionne ----

    [Fact]
    public async Task Identifiants_valides_donnent_acces()
    {
        var utilisateur = await CreerUtilisateurActifAsync();
        var client = CreerClient();

        await TenterConnexionAsync(client, utilisateur.Email!, MotDePasse);

        Assert.True(await EstConnecteAsync(client), "Des identifiants valides doivent donner accès (SEC-01).");
    }

    // ---- T010 : authentification obligatoire pour tout accès ----

    [Fact]
    public async Task Sans_session_une_ressource_protegee_renvoie_au_controle_d_identite()
    {
        var client = CreerClient();

        var reponse = await client.GetAsync("/comptes/double-authentification");

        // Le serveur refuse : soit redirection vers la page de connexion, soit refus direct.
        // Dans aucun cas la ressource protégée n'est servie.
        Assert.NotEqual(HttpStatusCode.OK, reponse.StatusCode);
    }

    // ---- T008 : verrouillage après échecs répétés ----

    [Fact]
    public async Task Echecs_repetes_au_dela_du_plafond_verrouillent_le_compte()
    {
        var utilisateur = await CreerUtilisateurActifAsync();
        var client = CreerClient();

        // Le plafond est une valeur de CONFIGURATION du produit (research.md D1) : le test ne le
        // fige pas, il le lit pour rester valide si la politique évolue.
        int plafond;
        using (var scope = factory.Services.CreateScope())
        {
            plafond = scope.ServiceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<IdentityOptions>>()
                .Value.Lockout.MaxFailedAccessAttempts;
        }

        for (var tentative = 0; tentative < plafond; tentative++)
        {
            await TenterConnexionAsync(client, utilisateur.Email!, MauvaisMotDePasse);
        }

        var apres = await RelireAsync(utilisateur.Id);
        Assert.True(
            apres.LockoutEnd is not null && apres.LockoutEnd > DateTimeOffset.UtcNow,
            $"Après {plafond} échecs consécutifs, le compte doit être verrouillé (SEC-01).");
    }

    // ---- T009 : CAS DÉCISIF — le verrouillage prime sur la validité du mot de passe ----

    [Fact]
    public async Task Compte_verrouille_refuse_l_acces_meme_avec_le_bon_mot_de_passe()
    {
        var utilisateur = await CreerUtilisateurActifAsync();
        var client = CreerClient();

        int plafond;
        using (var scope = factory.Services.CreateScope())
        {
            plafond = scope.ServiceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<IdentityOptions>>()
                .Value.Lockout.MaxFailedAccessAttempts;
        }

        for (var tentative = 0; tentative < plafond; tentative++)
        {
            await TenterConnexionAsync(client, utilisateur.Email!, MauvaisMotDePasse);
        }

        // Le BON mot de passe, pendant le verrouillage.
        await TenterConnexionAsync(client, utilisateur.Email!, MotDePasse);

        Assert.False(
            await EstConnecteAsync(client),
            "Pendant le verrouillage, même le bon mot de passe doit être refusé — sans quoi le "
            + "verrouillage n'offrirait aucune protection contre le forçage par essais répétés (SEC-01).");
    }
}
