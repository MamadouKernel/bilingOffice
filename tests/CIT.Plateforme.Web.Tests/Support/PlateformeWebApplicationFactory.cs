using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CIT.Plateforme.Web.Tests.Support;

/// <summary>
/// Démarre réellement l'application composée (DI, middlewares, Identity, EF Core — la même
/// composition qu'en production) pour les scénarios de recette REC-SCE-09→14 (seuls scénarios CDC
/// §22 applicables à ce socle — `recipe-scope-audit.md`). La chaîne de connexion est remplacée
/// par une base de test dédiée ; le choix de l'environnement SQL de test pour la CI reste ouvert
/// (T111, CLARIFICATION REQUIRED) — LocalDB est utilisé ici comme commodité de développement
/// local pour obtenir des résultats réels dans cette session.
/// </summary>
public sealed class PlateformeWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string ChaineConnexion =
        "Server=(localdb)\\MSSQLLocalDB;Database=CitPlateformeTestsWeb;Trusted_Connection=True;TrustServerCertificate=True;";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("ConnectionStrings:PlateformeDb", ChaineConnexion),
                // FEATURE 011 — configuration de reprise technique de test (T032, T033).
                // Valeur technique de démonstration, explicitement non contractuelle (I4, CLAR-NOT-02 reste ouverte).
                new KeyValuePair<string, string?>("Notifications:Reessai:NombreTentativesMax", "3"),
                new KeyValuePair<string, string?>("Notifications:Reessai:DelaiEntreTentatives", "00:01:00"),
            ]);
        });

        builder.ConfigureServices(services =>
        {
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();

            // Voir CIT.Plateforme.Infrastructure.Tests.Support.BaseDeTestSql pour la même
            // justification : une base de test persistante peut porter un schéma périmé.
            if (!baseDejaCreee)
            {
                lock (VerrouCreation)
                {
                    if (!baseDejaCreee)
                    {
                        Microsoft.Data.SqlClient.SqlConnection.ClearAllPools();
                        dbContext.Database.EnsureDeleted();
                        dbContext.Database.EnsureCreated();
                        baseDejaCreee = true;
                    }
                }
            }
        });
    }

    private static readonly Lock VerrouCreation = new();
    private static bool baseDejaCreee;
}

[Xunit.CollectionDefinition("PlateformeWebCollection", DisableParallelization = true)]
public class PlateformeWebCollection : Xunit.ICollectionFixture<PlateformeWebApplicationFactory>
{
}
