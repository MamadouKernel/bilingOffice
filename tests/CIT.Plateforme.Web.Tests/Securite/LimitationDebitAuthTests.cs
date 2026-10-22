using System.Net;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Securite;

/// <summary>
/// [TEST-SPEC] SEC — limitation de débit resserrée des points d'authentification.
///
/// Au-delà du seuil par IP, un POST vers <c>/comptes/connexion</c> est refusé par le limiteur.
/// Points vérifiés par ce test, et raisons des codes HTTP observés :
/// <list type="bullet">
/// <item>Le limiteur est placé AVANT l'antiforgery : une tentative qui obtient un permis atteint
/// l'antiforgery et reçoit 400 (formulaire posté sans jeton — pas besoin d'un compte réel).</item>
/// <item>La tentative au-delà du seuil est refusée (429). Comme <c>UseStatusCodePagesWithReExecute</c>
/// enveloppe le limiteur, ce 429 est ré-exécuté vers la page « /not-found » et ressort en 404 côté
/// client. Le passage de 400 (permis accordé) à un code de refus matérialise donc le plafond.</item>
/// <item>Sous TestServer, <c>RemoteIpAddress</c> est nul : toutes les requêtes partagent la
/// partition « inconnu » — exactement une même IP qui martèle la connexion.</item>
/// </list>
/// Le seuil effectif ici est le DÉFAUT du code (10) : la surcharge de configuration in-memory d'une
/// WebApplicationFactory n'atteint pas la valeur lue au tout début de Program. On vérifie donc que
/// le refus tombe à un plafond serré (≤ 10), très en-dessous du quota global de 240/min.
/// </summary>
public sealed class LimitationDebitAuthTests
{
    // Bornes du plafond d'auth dans le code : minimum 5 (Math.Max), défaut 10.
    private const int SeuilMin = 5;
    private const int SeuilMax = 10;

    private static readonly HttpStatusCode[] CodesDeRefus =
        [HttpStatusCode.TooManyRequests, HttpStatusCode.NotFound];

    private sealed class FabriqueDebit : WebApplicationFactory<Program>
    {
        private const string ChaineConnexionDediee =
            "Server=(localdb)\\MSSQLLocalDB;Database=CitPlateformeTestsDebitAuth;Trusted_Connection=True;TrustServerCertificate=True;";

        private static readonly Lock VerrouCreation = new();
        private static bool baseDejaCreee;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");

            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("ConnectionStrings:PlateformeDb", ChaineConnexionDediee),
                new KeyValuePair<string, string?>("Notifications:Reessai:NombreTentativesMax", "3"),
                new KeyValuePair<string, string?>("Notifications:Reessai:DelaiEntreTentatives", "00:01:00"),
                new KeyValuePair<string, string?>(
                    "Stockage:RepertoireDocuments",
                    Path.Combine(Path.GetTempPath(), "cit-tests-debit-documents")),
            ]));

            builder.ConfigureServices(services =>
            {
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
                            GardeBaseDeTest.VerifierContexte(db);

                            SchemaBaseDeTest.Reconstruire(db.Database, ChaineConnexionDediee);
                            baseDejaCreee = true;
                        }
                    }
                }
            });
        }
    }

    private static HttpClient ClientAnonyme(WebApplicationFactory<Program> fabrique)
        => fabrique.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });

    [Fact]
    public async Task Les_POST_de_connexion_au_dela_du_seuil_sont_refuses()
    {
        using var fabrique = new FabriqueDebit();
        using var client = ClientAnonyme(fabrique);

        var statuts = new List<HttpStatusCode>();
        for (var i = 0; i < SeuilMax + 1; i++)
        {
            using var contenu = new FormUrlEncodedContent([]);
            var reponse = await client.PostAsync("/comptes/connexion", contenu);
            statuts.Add(reponse.StatusCode);
        }

        // Première tentative qui n'obtient PLUS de permis : elle cesse d'être le 400 de l'antiforgery.
        var premierRefus = statuts.FindIndex(s => s != HttpStatusCode.BadRequest);

        Assert.True(premierRefus >= 0,
            "Aucun refus : le plafond d'auth ne s'est pas déclenché. Statuts: "
            + string.Join(",", statuts.Select(s => (int)s)));

        // Le refus tombe à un plafond serré (défaut 10), bien sous le quota global de 240/min.
        Assert.InRange(premierRefus, SeuilMin, SeuilMax);
        Assert.Contains(statuts[premierRefus], CodesDeRefus);
    }

    [Fact]
    public async Task Le_GET_du_formulaire_de_connexion_n_est_pas_plafonne()
    {
        // Le GET n'affiche que le formulaire : le marteler ne doit jamais verrouiller l'accès.
        // Il relève du quota global (240/min), pas du compartiment d'auth resserré.
        using var fabrique = new FabriqueDebit();
        using var client = ClientAnonyme(fabrique);

        for (var i = 0; i < SeuilMax + 2; i++)
        {
            var reponse = await client.GetAsync("/comptes/connexion");
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        }
    }
}
