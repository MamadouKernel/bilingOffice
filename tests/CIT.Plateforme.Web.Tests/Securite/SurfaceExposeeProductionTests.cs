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
/// [TEST-SPEC] SEC — surface exposée hors développement.
///
/// DÉFAUT CORRIGÉ : <c>app.MapOpenApi("/swagger/v1/swagger.json")</c> était appelé
/// inconditionnellement. La description complète de l'API — routes, paramètres, formes de réponse
/// — était donc servie à un visiteur anonyme en Staging comme en Production. Ce n'est pas une
/// faille en soi, les endpoints restant protégés ; c'est de la reconnaissance offerte, et le
/// premier réflexe d'un test d'intrusion.
/// </summary>
[Collection("PlateformeWebCollection")]
public sealed class SurfaceExposeeProductionTests
{
    private sealed class FabriqueEnvironnement(string environnement) : WebApplicationFactory<Program>
    {
        private const string ChaineConnexionDediee =
            "Server=(localdb)\\MSSQLLocalDB;Database=CitPlateformeTestsSurface;Trusted_Connection=True;TrustServerCertificate=True;";

        private static readonly Lock VerrouCreation = new();
        private static bool baseDejaCreee;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environnement);

            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("ConnectionStrings:PlateformeDb", ChaineConnexionDediee),
                new KeyValuePair<string, string?>("Notifications:Reessai:NombreTentativesMax", "3"),
                new KeyValuePair<string, string?>("Notifications:Reessai:DelaiEntreTentatives", "00:01:00"),
                // Hors développement, le stockage documentaire exige désormais une racine
                // explicite : sans elle, l'hôte refuserait de servir (comportement fail-safe voulu).
                new KeyValuePair<string, string?>(
                    "Stockage:RepertoireDocuments",
                    Path.Combine(Path.GetTempPath(), "cit-tests-surface-documents")),
            ]));

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<PlateformeDbContext>>();
                services.RemoveAll<PlateformeDbContext>();
                services.AddDbContext<PlateformeDbContext>(options =>
                    options.UseSqlServer(GardeBaseDeTest.Verifier(ChaineConnexionDediee)));

                // La base de test est persistante d'une exécution à l'autre : son schéma reste
                // figé à ce qu'il était lors de sa création, et toute entité ajoutée depuis
                // manque à l'appel. D'où la suppression puis la recréation.
                //
                // Le schéma est créé par `Migrate`, non par `EnsureCreated` : le démarrage de
                // l'application applique lui-même les migrations, et une base créée sans
                // `__EFMigrationsHistory` le ferait échouer en rejouant la première migration
                // sur des objets déjà présents. Même motif dans les deux autres fabriques.
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

                            Microsoft.Data.SqlClient.SqlConnection.ClearAllPools();
                            db.Database.EnsureDeleted();
                            db.Database.Migrate();
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

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task La_description_openapi_n_est_pas_servie_hors_developpement(string environnement)
    {
        using var fabrique = new FabriqueEnvironnement(environnement);
        using var client = ClientAnonyme(fabrique);

        var reponse = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }

    [Fact]
    public async Task En_developpement_la_description_openapi_reste_disponible()
    {
        // Elle garde son utilité pour l'intégration locale : c'est son exposition en dehors du
        // poste de développement qui était fautive.
        using var fabrique = new FabriqueEnvironnement("Development");
        using var client = ClientAnonyme(fabrique);

        var reponse = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
    }

    [Fact]
    public async Task L_api_reste_fermee_a_un_appelant_non_authentifie()
    {
        using var fabrique = new FabriqueEnvironnement("Production");
        using var client = ClientAnonyme(fabrique);

        var reponse = await client.GetAsync("/api/v1/tickets/INEXISTANT");

        // 401 attendu, jamais 200 ni 404 : un 404 renseignerait déjà sur l'existence de la route.
        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
    }
}
