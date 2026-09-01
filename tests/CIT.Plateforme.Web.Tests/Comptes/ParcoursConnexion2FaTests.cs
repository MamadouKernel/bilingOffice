using System.Net;
using System.Security.Cryptography;
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
/// [TEST-SPEC] Remédiation post-audit (`post-implementation-audit.md` §11, CPT-07, SEC-08). Parcours de
/// connexion réel via HTTP (<see cref="PlateformeWebApplicationFactory"/>, requêtes GET/POST
/// authentiques avec jeton d'anti-falsification et cookies réels), pas une simple vérification des
/// préconditions Identity — exactement la remédiation demandée : « adapter le test au niveau
/// WebApplicationFactory plutôt que réduire la preuve à une précondition ».
/// Exigence CDC: CPT-07, SEC-08 → US5 (F012).
/// </summary>
[Collection("PlateformeWebCollection")]
public sealed class ParcoursConnexion2FaTests : IClassFixture<PlateformeWebApplicationFactory>
{
    private const string MotDePasse = "MotDePasseValide123!";

    private readonly PlateformeWebApplicationFactory factory;

    public ParcoursConnexion2FaTests(PlateformeWebApplicationFactory factory) => this.factory = factory;

    private static HttpClient CreerClient(PlateformeWebApplicationFactory factory) =>
        // BaseAddress = https:// (pas le http://localhost par défaut) : le cookie d'authentification
        // Identity est marqué Secure (Program.cs, CookieSecurePolicy.Always) — CookieContainer ne le
        // renverrait jamais sur des requêtes suivantes perçues comme non-HTTPS, même contre le
        // TestServer en mémoire qui n'applique pas TLS lui-même.
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost"),
        });

    private static string ExtraireJetonAntiforgery(string html) =>
        Regex.Match(html, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"").Groups[1].Value;

    private async Task<ApplicationUser> CreerUtilisateurActifAsync(bool avecDeuxFacteurs)
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = services.GetRequiredService<PlateformeDbContext>();

        var email = $"{Guid.NewGuid()}@exemple.test";
        var utilisateur = new ApplicationUser { UserName = email, Email = email, NomAffiche = "Test", Nature = NatureUtilisateur.Client };
        var creation = await userManager.CreateAsync(utilisateur, MotDePasse);
        Assert.True(creation.Succeeded, string.Join(" ", creation.Errors.Select(e => e.Description)));

        var societe = SocieteCliente.Soumettre(utilisateur.Id, "Informations déclarées", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        var rattachement = UtilisateurSociete.Rattacher(utilisateur.Id, societe.Id, DateTime.UtcNow, estAdministrateur: true);
        societe.EnregistrerAdministrateurActif();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(rattachement);
        await dbContext.SaveChangesAsync();

        if (avecDeuxFacteurs)
        {
            await userManager.ResetAuthenticatorKeyAsync(utilisateur);
            await userManager.SetTwoFactorEnabledAsync(utilisateur, true);
        }

        return utilisateur;
    }

    private async Task DesactiverRattachementAsync(Guid utilisateurId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();
        var rattachement = dbContext.UtilisateursSociete.Single(u => u.UtilisateurId == utilisateurId);
        rattachement.Desactiver();
        await dbContext.SaveChangesAsync();
    }

    private async Task<string> ObtenirCleAuthentificateurAsync(Guid utilisateurId)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var utilisateur = await userManager.FindByIdAsync(utilisateurId.ToString());
        return (await userManager.GetAuthenticatorKeyAsync(utilisateur!))!;
    }

    /// <summary>Même algorithme que <c>Rfc6238AuthenticationService</c>/<c>AuthenticatorTokenProvider</c>
    /// d'ASP.NET Core Identity (TOTP, RFC 6238 : HMAC-SHA1, pas de 30s, 6 chiffres) — nécessaire
    /// pour générer, côté test, un code valide correspondant à la clé réelle stockée par Identity.</summary>
    private static string GenererCodeTotp(string base32Key, DateTimeOffset? instant = null)
    {
        var cle = Base32Decode(base32Key);
        var pasDeTemps = (instant ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds() / 30L;
        var pasBytes = BitConverter.GetBytes(pasDeTemps);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(pasBytes);
        }

        using var hmac = new HMACSHA1(cle);
        var hachage = hmac.ComputeHash(pasBytes);
        var decalage = hachage[^1] & 0xf;
        var code = ((hachage[decalage] & 0x7f) << 24)
                 | ((hachage[decalage + 1] & 0xff) << 16)
                 | ((hachage[decalage + 2] & 0xff) << 8)
                 | (hachage[decalage + 3] & 0xff);
        return (code % 1_000_000).ToString("D6");
    }

    private static byte[] Base32Decode(string base32)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        base32 = base32.TrimEnd('=').ToUpperInvariant();
        var octets = new List<byte>();
        int tampon = 0, bitsRestants = 0;
        foreach (var c in base32)
        {
            var valeur = alphabet.IndexOf(c);
            if (valeur < 0)
            {
                continue;
            }

            tampon = (tampon << 5) | valeur;
            bitsRestants += 5;
            if (bitsRestants >= 8)
            {
                octets.Add((byte)((tampon >> (bitsRestants - 8)) & 0xFF));
                bitsRestants -= 8;
            }
        }

        return [.. octets];
    }

    /// <summary>
    /// Vérifie l'authentification par une sonde réaliste (accès à une page protégée
    /// <c>[Authorize]</c>, <c>/comptes/double-authentification</c>) plutôt que par inspection brute
    /// des en-têtes <c>Set-Cookie</c> : une réponse de connexion peut contenir à la fois le cookie
    /// posé par <c>SignInAsync</c> ET son annulation par un <c>SignOutAsync</c> ultérieur dans la
    /// même réponse (cas E) — seule une requête de suivi avec le même <see cref="HttpClient"/>
    /// (même <c>CookieContainer</c>, qui applique correctement les deux en-têtes dans l'ordre)
    /// révèle l'état réel.
    /// </summary>
    private static async Task<bool> EstConnecteAsync(HttpClient client)
    {
        var reponse = await client.GetAsync("/client/demandes");
        return reponse.StatusCode == HttpStatusCode.OK;
    }

    private static async Task<(bool Connecte, HttpStatusCode Statut, string? Location, string Corps)> VerifierConnexionAsync(HttpClient client)
    {
        var reponse = await client.GetAsync("/client/demandes");
        var corps = await reponse.Content.ReadAsStringAsync();
        return (reponse.StatusCode == HttpStatusCode.OK, reponse.StatusCode, reponse.Headers.Location?.OriginalString, corps);
    }

    [Fact]
    public async Task A_Utilisateur_sans_2FA_connexion_reussie_avec_mot_de_passe_seul()
    {
        var client = CreerClient(factory);
        var utilisateur = await CreerUtilisateurActifAsync(avecDeuxFacteurs: false);

        var pageConnexion = await client.GetStringAsync("/comptes/connexion");
        var jeton = ExtraireJetonAntiforgery(pageConnexion);

        var reponse = await client.PostAsync("/comptes/connexion", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_handler"] = "connexion",
            ["__RequestVerificationToken"] = jeton,
            ["Modele.Email"] = utilisateur.Email!,
            ["Modele.MotDePasse"] = MotDePasse,
        }));

        _ = reponse;
        Assert.True(await EstConnecteAsync(client), "L'utilisateur doit être connecté — connexion directe réussie sans 2FA.");
    }

    [Fact]
    public async Task B_Utilisateur_avec_2FA_mot_de_passe_seul_est_insuffisant()
    {
        var client = CreerClient(factory);
        var utilisateur = await CreerUtilisateurActifAsync(avecDeuxFacteurs: true);

        var pageConnexion = await client.GetStringAsync("/comptes/connexion");
        var jeton = ExtraireJetonAntiforgery(pageConnexion);

        var reponse = await client.PostAsync("/comptes/connexion", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_handler"] = "connexion",
            ["__RequestVerificationToken"] = jeton,
            ["Modele.Email"] = utilisateur.Email!,
            ["Modele.MotDePasse"] = MotDePasse,
        }));

        // Le mot de passe seul déclenche le défi 2FA (redirection), jamais une connexion directe.
        Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
        Assert.Contains("verifier-code", reponse.Headers.Location!.OriginalString);
        Assert.False(await EstConnecteAsync(client),
            "Le mot de passe seul ne doit jamais suffire à connecter un compte avec 2FA activée.");
    }

    [Fact]
    public async Task C_Utilisateur_avec_2FA_et_code_valide_connexion_reussie()
    {
        var client = CreerClient(factory);
        var utilisateur = await CreerUtilisateurActifAsync(avecDeuxFacteurs: true);
        var cleAuthentificateur = await ObtenirCleAuthentificateurAsync(utilisateur.Id);

        var pageConnexion = await client.GetStringAsync("/comptes/connexion");
        var jetonConnexion = ExtraireJetonAntiforgery(pageConnexion);

        var reponseMotDePasse = await client.PostAsync("/comptes/connexion", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_handler"] = "connexion",
            ["__RequestVerificationToken"] = jetonConnexion,
            ["Modele.Email"] = utilisateur.Email!,
            ["Modele.MotDePasse"] = MotDePasse,
        }));

        Assert.Equal(HttpStatusCode.Redirect, reponseMotDePasse.StatusCode);
        var emplacementDefi = reponseMotDePasse.Headers.Location!.OriginalString;
        Assert.Contains("verifier-code", emplacementDefi);

        var pageCode = await client.GetStringAsync(emplacementDefi);
        var jetonCode = ExtraireJetonAntiforgery(pageCode);
        var code = GenererCodeTotp(cleAuthentificateur);

        var reponseCode = await client.PostAsync("/comptes/verifier-code", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_handler"] = "verifier-code",
            ["__RequestVerificationToken"] = jetonCode,
            ["Modele.Code"] = code,
        }));

        var (connecte, statut, location, corps) = await VerifierConnexionAsync(client);
        Assert.True(connecte, $"Un code TOTP valide doit permettre l'accès. Statut reçu: {statut}, Redirection: {location}, Corps: {corps}");
    }

    [Fact]
    public async Task D_Code_invalide_est_refuse()
    {
        var client = CreerClient(factory);
        var utilisateur = await CreerUtilisateurActifAsync(avecDeuxFacteurs: true);

        var pageConnexion = await client.GetStringAsync("/comptes/connexion");
        var jetonConnexion = ExtraireJetonAntiforgery(pageConnexion);

        var reponseMotDePasse = await client.PostAsync("/comptes/connexion", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_handler"] = "connexion",
            ["__RequestVerificationToken"] = jetonConnexion,
            ["Modele.Email"] = utilisateur.Email!,
            ["Modele.MotDePasse"] = MotDePasse,
        }));

        var emplacementDefi = reponseMotDePasse.Headers.Location!.OriginalString;
        var pageCode = await client.GetStringAsync(emplacementDefi);
        var jetonCode = ExtraireJetonAntiforgery(pageCode);

        var reponseCode = await client.PostAsync("/comptes/verifier-code", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_handler"] = "verifier-code",
            ["__RequestVerificationToken"] = jetonCode,
            ["Modele.Code"] = "000000",
        }));

        _ = reponseCode;
        Assert.False(await EstConnecteAsync(client), "Un code TOTP invalide ne doit jamais connecter l'utilisateur.");
    }

    [Fact]
    public async Task E_Utilisateur_desactive_est_refuse()
    {
        var client = CreerClient(factory);
        var utilisateur = await CreerUtilisateurActifAsync(avecDeuxFacteurs: false);
        await DesactiverRattachementAsync(utilisateur.Id);

        var pageConnexion = await client.GetStringAsync("/comptes/connexion");
        var jeton = ExtraireJetonAntiforgery(pageConnexion);

        var reponse = await client.PostAsync("/comptes/connexion", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_handler"] = "connexion",
            ["__RequestVerificationToken"] = jeton,
            ["Modele.Email"] = utilisateur.Email!,
            ["Modele.MotDePasse"] = MotDePasse,
        }));

        _ = reponse;
        Assert.False(await EstConnecteAsync(client), "Un utilisateur désactivé (CPT-03, US2 scénario 2) ne doit jamais être connecté.");
    }
}
