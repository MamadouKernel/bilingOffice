using System.Net;
using System.Text.RegularExpressions;
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

/// <summary>
/// [TEST-SPEC — Impersonation] Parcours réel HTTP de l'emprunt d'identité « se connecter en tant que »
/// (<see cref="PlateformeWebApplicationFactory"/>, cookies + antiforgery authentiques). Éprouve le
/// cœur sensible de la feature : un administrateur habilité emprunte un compte ordinaire, le bandeau
/// de retour apparaît, le retour ramène à l'administrateur ; et un compte lui-même habilité à
/// l'emprunt ne PEUT PAS être emprunté (pas d'escalade latérale).
/// </summary>
[Collection("PlateformeWebCollection")]
public sealed class ParcoursImpersonationTests : IClassFixture<PlateformeWebApplicationFactory>
{
    private const string MotDePasse = "MotDePasseValide123!";
    private const string ActionRetour = "/administration/impersonation/revenir";

    private readonly PlateformeWebApplicationFactory factory;

    public ParcoursImpersonationTests(PlateformeWebApplicationFactory factory) => this.factory = factory;

    private static HttpClient CreerClient(PlateformeWebApplicationFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost"),
        });

    private static string ExtraireJeton(string html) =>
        Regex.Match(html, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"").Groups[1].Value;

    /// <summary>Crée un compte agent actif, éventuellement habilité à l'emprunt d'identité.</summary>
    private async Task<ApplicationUser> CreerAgentAsync(bool avecDroitImpersonation)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();

        var email = $"{Guid.NewGuid()}@exemple.test";
        var compte = new ApplicationUser { UserName = email, Email = email, NomAffiche = "Agent Test", Nature = NatureUtilisateur.Agent };
        var creation = await userManager.CreateAsync(compte, MotDePasse);
        Assert.True(creation.Succeeded, string.Join(" ", creation.Errors.Select(e => e.Description)));

        if (avecDroitImpersonation)
        {
            db.Habilitations.Add(HabilitationUtilisateur.Attribuer(compte.Id, Permission.ComptesImpersonation));
            await db.SaveChangesAsync();
        }

        return compte;
    }

    private async Task ConnecterAsync(HttpClient client, ApplicationUser compte)
    {
        var page = await client.GetStringAsync("/comptes/connexion");
        var jeton = ExtraireJeton(page);
        var reponse = await client.PostAsync("/comptes/connexion", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_handler"] = "connexion",
            ["__RequestVerificationToken"] = jeton,
            ["Modele.Email"] = compte.Email!,
            ["Modele.MotDePasse"] = MotDePasse,
        }));
        Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);

        // Sonde d'authentification : la page d'emprunt doit répondre 200 (et non une redirection login).
        var sonde = await client.GetAsync("/administration/impersonation");
        Assert.True(sonde.StatusCode == HttpStatusCode.OK,
            $"Attendu 200 sur la page d'emprunt, obtenu {(int)sonde.StatusCode} → {sonde.Headers.Location?.OriginalString}");
    }

    [Fact]
    public async Task Admin_emprunte_un_compte_puis_revient()
    {
        var client = CreerClient(factory);
        var admin = await CreerAgentAsync(avecDroitImpersonation: true);
        var cible = await CreerAgentAsync(avecDroitImpersonation: false);
        await ConnecterAsync(client, admin);

        // La page d'emprunt fournit le formulaire + le jeton antiforgery.
        var pageEmprunt = await client.GetStringAsync("/administration/impersonation");
        var jeton = ExtraireJeton(pageEmprunt);

        var demarrage = await client.PostAsync("/administration/impersonation/demarrer", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = jeton,
            ["cibleId"] = cible.Id.ToString(),
            ["motDePasse"] = MotDePasse,
        }));
        Assert.Equal(HttpStatusCode.Redirect, demarrage.StatusCode);
        Assert.Equal("/", demarrage.Headers.Location!.OriginalString);

        // La session est désormais empruntée : le bandeau de retour est présent sur toute page.
        var accueilEmprunte = await client.GetStringAsync("/comptes/mon-profil");
        Assert.Contains(ActionRetour, accueilEmprunte);

        // Retour : la session revient à l'administrateur, le bandeau disparaît.
        var retour = await client.PostAsync(ActionRetour, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtraireJeton(accueilEmprunte),
        }));
        Assert.Equal(HttpStatusCode.Redirect, retour.StatusCode);

        var accueilRevenu = await client.GetStringAsync("/comptes/mon-profil");
        Assert.DoesNotContain(ActionRetour, accueilRevenu);
    }

    [Fact]
    public async Task Un_compte_habilite_a_l_emprunt_ne_peut_pas_etre_emprunte()
    {
        var client = CreerClient(factory);
        var admin = await CreerAgentAsync(avecDroitImpersonation: true);
        var autreAdmin = await CreerAgentAsync(avecDroitImpersonation: true);
        await ConnecterAsync(client, admin);

        var pageEmprunt = await client.GetStringAsync("/administration/impersonation");
        var jeton = ExtraireJeton(pageEmprunt);

        var demarrage = await client.PostAsync("/administration/impersonation/demarrer", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = jeton,
            ["cibleId"] = autreAdmin.Id.ToString(),
            ["motDePasse"] = MotDePasse,
        }));

        // Refus tracé par le verdict CibleProtegee ; la session n'est PAS empruntée.
        Assert.Equal(HttpStatusCode.Redirect, demarrage.StatusCode);
        Assert.Contains("CibleProtegee", demarrage.Headers.Location!.OriginalString);

        var accueil = await client.GetStringAsync("/comptes/mon-profil");
        Assert.DoesNotContain(ActionRetour, accueil);
    }
}
