using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CIT.Plateforme.Web.Tests.Support;

/// <summary>
/// Démarre réellement l'application composée (DI, middlewares, Identity, EF Core — la même
/// composition qu'en production) pour les scénarios de recette REC-SCE-09→14 (seuls scénarios CDC
/// §22 applicables à ce socle — `recipe-scope-audit.md`). La chaîne de connexion est remplacée
/// par une base de test dédiée ; le choix de l'environnement SQL de test pour la CI reste ouvert
/// (T111, CLARIFICATION REQUIRED) — LocalDB est utilisé ici comme commodité de développement
/// local pour obtenir des résultats réels dans cette session.
/// </summary>
public sealed class PlateformeWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
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
                // Les tests simulent des utilisateurs connectés sans application TOTP : l'obligation
                // de 2FA (activée par défaut en exécution réelle) est coupée pour eux seulement.
                new KeyValuePair<string, string?>("Securite:DoubleAuthentificationObligatoire", "false"),
                // Les tests exercent le verrouillage de compte SEC-01 par POST répétés et une
                // connexion valide : la limite de débit par IP (production) renverrait 429 avant le
                // 5e essai. Neutralisée pour les tests uniquement (voir Program.cs).
                new KeyValuePair<string, string?>("LimitationDebit:Desactivee", "true"),
            ]);
        });

        builder.ConfigureServices(services =>
        {
            // ISOLATION (incident INC-05) : `ConfigureAppConfiguration` ci-dessus ne suffit pas.
            // En environnement Development, les user-secrets sont chargés par
            // `WebApplication.CreateBuilder` et PRIMENT sur la valeur injectée par la fabrique —
            // les tests atteignaient donc `DbBillingOffice`, la base de développement réelle, et
            // l'ont supprimée puis recréée. Refaire l'enregistrement du DbContext ici est le seul
            // point que l'ordre des sources de configuration ne peut pas renverser.
            TraitementsDeFondDeTest.Retirer(services);

            services.RemoveAll<DbContextOptions<PlateformeDbContext>>();
            services.RemoveAll<PlateformeDbContext>();
            services.AddDbContext<PlateformeDbContext>(options =>
                options.UseSqlServer(GardeBaseDeTest.Verifier(ChaineConnexion)));

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();

            // Contrôle final sur la base RÉELLEMENT résolue, avant toute opération de schéma.
            GardeBaseDeTest.VerifierContexte(dbContext);

            // Voir CIT.Plateforme.Infrastructure.Tests.Support.BaseDeTestSql pour la même
            // justification : une base de test persistante peut porter un schéma périmé.
            if (!baseDejaCreee)
            {
                lock (VerrouCreation)
                {
                    if (!baseDejaCreee)
                    {
                        SchemaBaseDeTest.Reconstruire(dbContext.Database, ChaineConnexion);
                        baseDejaCreee = true;
                    }
                }
            }
        });
    }

    /// <summary>
    /// xUnit appelle cette méthode UNE FOIS PAR INSTANCE DE FIXTURE — donc une fois par classe de
    /// test, la fixture de collection mise à part. C'est le seul point qui s'exécute avant la
    /// construction de l'hôte, et donc avant que le démarrage de l'application ne réinsère ses
    /// données de référence : la purge doit précéder cette construction pour que chaque classe
    /// reparte du même état.
    ///
    /// Le contexte est ouvert directement, sans passer par l'hôte : y accéder ici déclencherait
    /// la construction que l'on cherche justement à devancer.
    /// </summary>
    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<PlateformeDbContext>()
            .UseSqlServer(GardeBaseDeTest.Verifier(ChaineConnexion))
            .Options;

        await using var contexte = new PlateformeDbContext(options);

        // Contrôle sur la base réellement résolue, avant toute écriture (incident INC-05).
        GardeBaseDeTest.VerifierContexte(contexte);

        if (!baseDejaCreee)
        {
            lock (VerrouCreation)
            {
                if (!baseDejaCreee)
                {
                    SchemaBaseDeTest.Reconstruire(contexte.Database, ChaineConnexion);
                    baseDejaCreee = true;
                }
            }
        }

        await ReinitialisationDonneesDeTest.ViderAsync(contexte);
    }

    /// <summary>
    /// Implémentation explicite : <c>WebApplicationFactory</c> expose déjà un
    /// <c>DisposeAsync</c> renvoyant <c>ValueTask</c>, que celui d'<c>IAsyncLifetime</c>
    /// masquerait. La libération réelle reste celle de la classe de base.
    /// </summary>
    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

    private static readonly Lock VerrouCreation = new();
    private static bool baseDejaCreee;
}

[Xunit.CollectionDefinition("PlateformeWebCollection", DisableParallelization = true)]
public class PlateformeWebCollection : Xunit.ICollectionFixture<PlateformeWebApplicationFactory>
{
}
