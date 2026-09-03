using System.Text.RegularExpressions;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Securite;

/// <summary>
/// [TEST-SPEC] G-04 — divulgation du jeton de réinitialisation (CPT-03).
///
/// DÉFAUT CORRIGÉ : <c>/comptes/mot-de-passe-oublie</c> est une page publique (aucun
/// <c>[Authorize]</c>, aucune politique de repli dans <c>Program.cs</c>). Elle rendait le lien de
/// réinitialisation — jeton compris — directement dans la réponse HTTP, sans aucune garde
/// d'environnement. Le libellé disait « mode démo » ; le code ne testait rien.
///
/// EXPLOITATION : un anonyme saisissait une adresse, lisait le lien à l'écran, le suivait et
/// prenait le compte. Appliqué au compte portant toutes les habilitations, cela donnait
/// l'administration complète de la plateforme.
///
/// PRINCIPE RETENU (fail-safe) : tant qu'aucun canal de distribution approuvé n'est configuré
/// (PC-16 non résolu), la plateforme préfère un RESET NON DISTRIBUABLE à un RESET PUBLIC.
/// </summary>
[Collection("PlateformeWebCollection")]
public sealed class SecResetDivulgationJetonTests
{
    private const string MotDePasseSynthetique = "MotDePasseValide123!";

    /// <summary>
    /// Fabrique permettant de forcer l'environnement d'exécution : c'est précisément la
    /// dimension que le défaut ignorait, elle doit donc être testable explicitement.
    /// </summary>
    private sealed class FabriqueEnvironnement(string environnement) : WebApplicationFactory<Program>
    {
        /// <summary>
        /// Base DÉDIÉE, distincte de celle des autres tests Web. <c>PlateformeWebApplicationFactory</c>
        /// supprime et recrée la sienne une fois par processus ; partager cette base ferait
        /// disparaître, en cours d'exécution, les données créées par les tests voisins.
        /// </summary>
        private const string ChaineConnexionDediee =
            "Server=(localdb)\\MSSQLLocalDB;Database=CitPlateformeTestsSecReset;Trusted_Connection=True;TrustServerCertificate=True;";

        private static readonly Lock VerrouCreation = new();
        private static bool baseDejaCreee;

        public JournalMemoire Journal { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environnement);

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(
                [
                    new KeyValuePair<string, string?>("ConnectionStrings:PlateformeDb", ChaineConnexionDediee),
                    new KeyValuePair<string, string?>("Notifications:Reessai:NombreTentativesMax", "3"),
                    new KeyValuePair<string, string?>("Notifications:Reessai:DelaiEntreTentatives", "00:01:00"),
                ]);
            });

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ILoggerProvider>(Journal);

                // `appsettings.Production.json` et `appsettings.Staging.json` déclarent
                // `ConnectionStrings:PlateformeDb` à la chaîne vide, et leurs sources priment sur
                // celle ajoutée ici. L'enregistrement du DbContext est donc refait explicitement :
                // c'est le seul point à l'abri de l'ordre des sources de configuration.
                TraitementsDeFondDeTest.Retirer(services);

                services.RemoveAll<DbContextOptions<PlateformeDbContext>>();
                services.RemoveAll<PlateformeDbContext>();
                services.AddDbContext<PlateformeDbContext>(options =>
                    options.UseSqlServer(GardeBaseDeTest.Verifier(ChaineConnexionDediee)));

                if (!baseDejaCreee)
                {
                    lock (VerrouCreation)
                    {
                        if (!baseDejaCreee)
                        {
                            using var provider = services.BuildServiceProvider();
                            using var scope = provider.CreateScope();
                            var db = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();
                            SchemaBaseDeTest.Reconstruire(db.Database, ChaineConnexionDediee);
                            baseDejaCreee = true;
                        }
                    }
                }
            });
        }
    }

    /// <summary>Capture tout ce que l'application journalise, pour vérifier qu'aucun secret n'y passe.</summary>
    private sealed class JournalMemoire : ILoggerProvider
    {
        private readonly List<string> lignes = [];
        private readonly Lock verrou = new();

        public IReadOnlyList<string> Lignes
        {
            get { lock (verrou) { return [.. lignes]; } }
        }

        public ILogger CreateLogger(string categoryName) => new Enregistreur(this);

        public void Dispose() { }

        private void Ajouter(string ligne)
        {
            lock (verrou) { lignes.Add(ligne); }
        }

        private sealed class Enregistreur(JournalMemoire journal) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
                => journal.Ajouter(formatter(state, exception));
        }
    }

    private static HttpClient CreerClientAnonyme(WebApplicationFactory<Program> fabrique)
        => fabrique.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost"),
        });

    private static async Task<ApplicationUser> CreerCompteAsync(WebApplicationFactory<Program> fabrique)
    {
        using var scope = fabrique.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = $"{Guid.NewGuid()}@exemple.test";
        var compte = new ApplicationUser
        {
            UserName = email,
            Email = email,
            NomAffiche = "Compte de test G-04",
            Nature = NatureUtilisateur.Client,
        };
        Assert.True((await userManager.CreateAsync(compte, MotDePasseSynthetique)).Succeeded);
        return compte;
    }

    /// <summary>Soumet le formulaire public et retourne le corps de la réponse.</summary>
    private static async Task<string> DemanderReinitialisationAsync(HttpClient client, string email)
    {
        var page = await client.GetStringAsync("/comptes/mot-de-passe-oublie");
        var antiforgery = Regex.Match(page, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"").Groups[1].Value;

        var reponse = await client.PostAsync("/comptes/mot-de-passe-oublie", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["_handler"] = "mot-de-passe-oublie",
                ["__RequestVerificationToken"] = antiforgery,
                ["Modele.Email"] = email,
            }));

        return await reponse.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Neutralise ce qui varie légitimement d'une réponse à l'autre : le jeton antiforgery, et
    /// l'adresse que le formulaire réémet. Cette adresse est la saisie de l'appelant lui-même —
    /// la lui renvoyer ne lui apprend rien. Tout le reste doit être identique : c'est là que se
    /// logerait un oracle d'énumération.
    /// </summary>
    private static string Normaliser(string corps, string emailSaisi)
    {
        var normalise = Regex.Replace(corps, "value=\"[^\"]{20,}\"", "value=\"__OPAQUE__\"");

        // Masque toute suite base64 longue — jeton antiforgery, état de formulaire Blazor : ces
        // valeurs changent à chaque requête sans rien dire du compte. Le masque ne couvre PAS les
        // GUID (le tiret n'appartient pas à l'alphabet base64), de sorte qu'un identifiant de
        // compte qui fuiterait ferait toujours diverger la comparaison. La divulgation de jeton
        // et d'URL, elle, est vérifiée nommément par SEC-RESET-01, 02, 04 et 05.
        normalise = Regex.Replace(normalise, "[A-Za-z0-9+/]{32,}={0,2}", "__OPAQUE__");

        return normalise.Replace(emailSaisi, "__EMAIL_SAISI__", StringComparison.OrdinalIgnoreCase);
    }

    // ================================================================================
    // SEC-RESET-01 / 02 / 04 — Production ne divulgue ni jeton ni lien
    // ================================================================================

    [Fact]
    public async Task SecReset01_Production_ne_divulgue_aucun_jeton_a_un_anonyme()
    {
        using var fabrique = new FabriqueEnvironnement("Production");
        var compte = await CreerCompteAsync(fabrique);
        using var client = CreerClientAnonyme(fabrique);

        var corps = await DemanderReinitialisationAsync(client, compte.Email!);

        Assert.DoesNotContain("jeton=", corps, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("utilisateurId=", corps, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(compte.Id.ToString(), corps, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SecReset02_Production_ne_divulgue_aucune_url_de_reinitialisation()
    {
        using var fabrique = new FabriqueEnvironnement("Production");
        var compte = await CreerCompteAsync(fabrique);
        using var client = CreerClientAnonyme(fabrique);

        var corps = await DemanderReinitialisationAsync(client, compte.Email!);

        // Le lien vers la page de réinitialisation ne doit apparaître sous aucune forme :
        // ni en clair, ni encodé, ni assorti de paramètres.
        Assert.DoesNotContain("/comptes/reinitialiser-mot-de-passe?", corps, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SecReset04_Production_repond_de_facon_generique_a_un_compte_existant()
    {
        using var fabrique = new FabriqueEnvironnement("Production");
        var compte = await CreerCompteAsync(fabrique);
        using var client = CreerClientAnonyme(fabrique);

        var corps = await DemanderReinitialisationAsync(client, compte.Email!);

        Assert.DoesNotContain("mode démo", corps, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Lien de réinitialisation généré", corps, StringComparison.OrdinalIgnoreCase);
    }

    // ================================================================================
    // SEC-RESET-05 — Staging ne divulgue pas davantage
    // ================================================================================

    [Fact]
    public async Task SecReset05_Staging_ne_divulgue_aucun_jeton_a_un_anonyme()
    {
        using var fabrique = new FabriqueEnvironnement("Staging");
        var compte = await CreerCompteAsync(fabrique);
        using var client = CreerClientAnonyme(fabrique);

        var corps = await DemanderReinitialisationAsync(client, compte.Email!);

        Assert.DoesNotContain("jeton=", corps, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/comptes/reinitialiser-mot-de-passe?", corps, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(compte.Id.ToString(), corps, StringComparison.OrdinalIgnoreCase);
    }

    // ================================================================================
    // SEC-RESET-03 — aucune énumération de comptes
    // ================================================================================

    [Fact]
    public async Task SecReset03_Compte_existant_et_inconnu_donnent_une_reponse_indiscernable()
    {
        using var fabrique = new FabriqueEnvironnement("Production");
        var compte = await CreerCompteAsync(fabrique);
        using var client = CreerClientAnonyme(fabrique);

        var emailInconnu = $"{Guid.NewGuid()}@inconnu.test";

        var corpsExistant = Normaliser(
            await DemanderReinitialisationAsync(client, compte.Email!), compte.Email!);
        var corpsInconnu = Normaliser(
            await DemanderReinitialisationAsync(client, emailInconnu), emailInconnu);

        // Toute différence observable est un oracle d'énumération : elle indique à un anonyme
        // quelles adresses correspondent à un compte réel.
        Assert.Equal(corpsInconnu, corpsExistant);
    }

    // ================================================================================
    // SEC-RESET-06 — le comportement de démonstration est explicitement conditionné
    // ================================================================================

    [Fact]
    public async Task SecReset06_Le_comportement_de_demonstration_est_conditionne_a_Development()
    {
        using var fabriqueDev = new FabriqueEnvironnement("Development");
        var compte = await CreerCompteAsync(fabriqueDev);
        using var clientDev = CreerClientAnonyme(fabriqueDev);

        var corpsDev = await DemanderReinitialisationAsync(clientDev, compte.Email!);

        // En développement, le lien reste affiché : sans canal de distribution, c'est le seul
        // moyen d'exercer le parcours localement. La garde doit être explicite, donc observable.
        Assert.Contains("/comptes/reinitialiser-mot-de-passe?", corpsDev, StringComparison.OrdinalIgnoreCase);

        using var fabriqueProd = new FabriqueEnvironnement("Production");
        using var clientProd = CreerClientAnonyme(fabriqueProd);
        var corpsProd = await DemanderReinitialisationAsync(clientProd, compte.Email!);

        Assert.DoesNotContain("/comptes/reinitialiser-mot-de-passe?", corpsProd, StringComparison.OrdinalIgnoreCase);
    }

    // ================================================================================
    // SEC-RESET-07 — le jeton n'atterrit pas dans les journaux
    // ================================================================================

    [Fact]
    public async Task SecReset07_Aucun_jeton_ni_adresse_n_est_journalise()
    {
        using var fabrique = new FabriqueEnvironnement("Production");
        var compte = await CreerCompteAsync(fabrique);
        using var client = CreerClientAnonyme(fabrique);

        await DemanderReinitialisationAsync(client, compte.Email!);

        foreach (var ligne in fabrique.Journal.Lignes)
        {
            Assert.DoesNotContain("jeton=", ligne, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(compte.Email!, ligne, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(MotDePasseSynthetique, ligne, StringComparison.Ordinal);
        }
    }

    // ================================================================================
    // SEC-RESET-08 — le parcours officiel Identity reste opérationnel
    // ================================================================================

    [Fact]
    public async Task SecReset08_Le_parcours_officiel_de_reinitialisation_reste_fonctionnel()
    {
        using var fabrique = new FabriqueEnvironnement("Production");
        var compte = await CreerCompteAsync(fabrique);

        using var scope = fabrique.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // La correction ferme la divulgation, elle n'affaiblit pas Identity : le mécanisme
        // officiel de jeton doit continuer de fonctionner de bout en bout.
        var recharge = await userManager.FindByIdAsync(compte.Id.ToString());
        Assert.NotNull(recharge);

        var jeton = await userManager.GeneratePasswordResetTokenAsync(recharge);
        Assert.False(string.IsNullOrWhiteSpace(jeton));

        var resultat = await userManager.ResetPasswordAsync(recharge, jeton, "AutreMotDePasseValide456!");
        Assert.True(resultat.Succeeded);
    }
}
