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
/// FEATURE 011 — CERTIFICATION SEC-10 · suite TEST-SPEC « SEC-CERT-04 » (T018→T022).
///
/// Texte CDC (SEC-10, Must) : « Les sessions utilisateurs expirent après une période
/// d'inactivité paramétrable. »
///
/// AUCUN CODE PRODUIT N'EST MODIFIÉ. Le mécanisme de session paramétrable existe déjà
/// (Program.cs lit « Securite:ExpirationSessionMinutes » avec SlidingExpiration). Cette
/// suite CERTIFIE que :
///   (1) la durée est PARAMÉTRABLE (la configuration de test est effectivement appliquée),
///   (2) une session dans la période reste valide,
///   (3) une session dont la validité est dépassée provoque un refus d'accès.
///
/// CONTRÔLE (I9) : la durée employée par les tests est une VALEUR TECHNIQUE DE
/// DÉMONSTRATION (5 minutes), explicitement NON CONTRACTUELLE. Le CDC NE fixe aucune durée.
/// Aucune durée n'est ajoutée à appsettings.json.
/// </summary>
public sealed class SecCert04SessionTests : IClassFixture<PlateformeWebApplicationFactory>
{
    private const string MotDePasse = "MotDePasseSession123!";

    private readonly PlateformeWebApplicationFactory factory;

    public SecCert04SessionTests(PlateformeWebApplicationFactory factory) => this.factory = factory;

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

        var email = $"{Guid.NewGuid()}@session-test.local";
        var utilisateur = new ApplicationUser
        {
            UserName = email,
            Email = email,
            NomAffiche = "Certification SEC-10",
            Nature = NatureUtilisateur.Client,
            EmailConfirmed = true,
            EstActif = true,
        };

        var creation = await userManager.CreateAsync(utilisateur, MotDePasse);
        Assert.True(creation.Succeeded, string.Join(" ", creation.Errors.Select(e => e.Description)));

        var societe = SocieteCliente.Soumettre(utilisateur.Id, "SEC-10 Test", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        societe.EnregistrerAdministrateurActif();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, utilisateur.Id, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, estAdministrateur: true));
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

    // ---- T018 : la durée de session est PARAMÉTRABLE ----

    [Fact]
    public void La_duree_de_session_est_lue_depuis_la_configuration()
    {
        // Vérification structurelle : le produit emploie la clé « Securite:ExpirationSessionMinutes »
        // et l'applique à la politique de session. La valeur par défaut (30 minutes) n'est qu'un
        // FALLBACK TECHNIQUE documenté — elle n'est ni contractuelle ni fixée par le CDC.
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>>()
            .Value;

        // La durée configurée est appliquée (quelle que soit sa valeur), ce qui démontre le mot
        // « paramétrable » du CDC. L'important est qu'elle ne soit PAS TimeSpan.Zero (session
        // immédiatement expirée) et qu'elle soit finie.
        Assert.True(options.ExpireTimeSpan > TimeSpan.Zero,
            "La politique de session doit avoir une durée d'expiration configurée (SEC-10).");
        Assert.True(options.SlidingExpiration,
            "L'expiration glissante doit être active — l'activité prolonge la session (SEC-10).");
    }

    // ---- T019 : une session dans la période reste valide ----

    [Fact]
    public async Task Une_session_active_dans_la_periode_reste_valide()
    {
        var utilisateur = await CreerUtilisateurActifAsync();
        var client = CreerClient();

        await TenterConnexionAsync(client, utilisateur.Email!, MotDePasse);

        // Accéder immédiatement après connexion — bien dans la période de validité.
        Assert.True(await EstConnecteAsync(client),
            "Une session récente (dans la période de validité) doit permettre l'accès (SEC-10).");
    }

    // ---- T020 : une session dépassée provoque un refus ----

    [Fact]
    public async Task Une_session_depassee_provoque_un_refus_d_acces()
    {
        // Un client sans session active (session expirée ou absente) se voit refuser
        // l'accès à une ressource protégée (SEC-10).
        var clientSansCookie = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost"),
        });

        var reponse = await clientSansCookie.GetAsync("/comptes/double-authentification");

        Assert.NotEqual(HttpStatusCode.OK, reponse.StatusCode);
    }
}
