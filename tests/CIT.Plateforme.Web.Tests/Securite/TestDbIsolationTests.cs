using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Securite;

/// <summary>
/// [TEST-SPEC] INC-05 — étanchéité du harnais de test.
///
/// INCIDENT : les fabriques fixaient leur base par <c>ConfigureAppConfiguration</c>. En
/// environnement <c>Development</c>, les user-secrets chargés par <c>WebApplication.CreateBuilder</c>
/// priment sur cette valeur. Les tests Web se sont donc exécutés contre <c>DbBillingOffice</c>, la
/// base de développement réelle, et l'ont supprimée puis recréée.
///
/// Ces tests vérifient que le cas ne peut plus se reproduire silencieusement.
/// </summary>
[Collection("PlateformeWebCollection")]
public sealed class TestDbIsolationTests(PlateformeWebApplicationFactory fabrique)
{
    // ================================================================================
    // TEST-DB-01 — l'hôte de test n'utilise jamais la base de développement
    // ================================================================================

    [Fact]
    public void TestDb01_L_hote_de_test_n_utilise_pas_la_base_de_developpement()
    {
        using var scope = fabrique.Services.CreateScope();
        var contexte = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();

        var baseUtilisee = contexte.Database.GetDbConnection().Database;

        Assert.StartsWith(GardeBaseDeTest.PrefixeAutorise, baseUtilisee, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual("DbBillingOffice", baseUtilisee, StringComparer.OrdinalIgnoreCase);
    }

    // ================================================================================
    // TEST-DB-02 — une chaîne non autorisée est refusée, pas subie
    // ================================================================================

    [Theory]
    [InlineData("Server=localhost;Initial Catalog=DbBillingOffice;Integrated Security=True;")]
    [InlineData("Server=localhost;Database=DbBillingOffice;Integrated Security=True;")]
    [InlineData("Server=prod.exemple;Initial Catalog=CitProduction;User Id=sa;Password=peu-importe;")]
    public void TestDb02_Une_base_hors_perimetre_de_test_est_refusee(string chaineInterdite)
    {
        var erreur = Assert.Throws<InvalidOperationException>(() => GardeBaseDeTest.Verifier(chaineInterdite));

        Assert.Contains("Isolation des tests", erreur.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TestDb02_Une_chaine_sans_base_identifiable_est_refusee()
    {
        Assert.Throws<InvalidOperationException>(
            () => GardeBaseDeTest.Verifier("Server=localhost;Integrated Security=True;"));
    }

    [Fact]
    public void TestDb02_La_chaine_du_harnais_est_acceptee()
    {
        // Contrôle de non-régression du garde lui-même : un garde qui refuserait tout serait
        // inutile, et un garde qui accepterait tout serait pire.
        var accepte = GardeBaseDeTest.Verifier(PlateformeWebApplicationFactory.ChaineConnexion);

        Assert.Equal(PlateformeWebApplicationFactory.ChaineConnexion, accepte);
    }

    // ================================================================================
    // TEST-DB-03 — le contexte résolu est contrôlé, pas seulement la chaîne déclarée
    // ================================================================================

    [Fact]
    public void TestDb03_Le_controle_porte_sur_la_base_reellement_resolue()
    {
        using var scope = fabrique.Services.CreateScope();
        var contexte = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();

        // Ne lève pas : c'est bien une base de test. Le contrôle sur le contexte résolu est le
        // seul qu'une source de configuration plus prioritaire ne peut pas contourner — vérifier
        // uniquement la chaîne déclarée aurait laissé passer l'incident.
        GardeBaseDeTest.VerifierContexte(contexte);
    }

    // ================================================================================
    // TEST-DB-05 — un amorçage destructif ne peut pas viser la base de développement
    // ================================================================================

    [Fact]
    public void TestDb05_L_amorcage_destructif_est_refuse_sur_une_base_hors_demonstration()
    {
        // Deuxième barrière, indépendante du harnais : même si un hôte atteignait la base de
        // développement, la politique d'amorçage refuserait la purge.
        var decision = PolitiqueAmorcageDemo.Decider(
            environnement: "Development",
            motDePasseComptesDemo: "SecretDeTestSynthetique123!",
            optInPurge: "true",
            baseDemoAttendue: "CitPlateformeDemo",
            baseCible: "DbBillingOffice");

        Assert.False(decision.PurgeAutorisee);
    }

    // ================================================================================
    // TEST-DB-04 — aucun test ne dépend implicitement des comptes de l'amorçage
    // ================================================================================

    /// <summary>
    /// Cinq scénarios ont échoué le jour où l'amorçage a cessé de créer les comptes de
    /// démonstration : ils s'en servaient de fixture sans le dire. Un test doit créer ce dont il a
    /// besoin — sans quoi une décision produit légitime le casse à distance, et le défaut
    /// n'apparaît nulle part dans son code.
    ///
    /// Seul <c>DevelopmentSeederTests</c> échappe à la règle : l'amorçage y est le sujet, pas la
    /// fixture.
    /// </summary>
    [Fact]
    public void TestDb04_Aucun_test_ne_lit_une_identite_de_demonstration_sans_l_avoir_creee()
    {
        var racine = new DirectoryInfo(AppContext.BaseDirectory);
        while (racine is not null && !Directory.Exists(Path.Combine(racine.FullName, "tests")))
        {
            racine = racine.Parent;
        }
        Assert.NotNull(racine);

        var projetTests = Path.Combine(racine!.FullName, "tests", "CIT.Plateforme.Web.Tests");

        var fautes = Directory
            .EnumerateFiles(projetTests, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(f => !f.EndsWith("DevelopmentSeederTests.cs", StringComparison.Ordinal))
            .SelectMany(f => File.ReadAllLines(f)
                .Select((ligne, index) => (Fichier: Path.GetFileName(f), Numero: index + 1, Ligne: ligne)))
            // Une identité littérale résolue par recherche : le compte est supposé exister,
            // donc produit par quelqu'un d'autre.
            .Where(x => x.Ligne.Contains("FindByEmailAsync(\"", StringComparison.Ordinal))
            .Select(x => $"{x.Fichier}:{x.Numero}")
            .ToList();

        Assert.True(
            fautes.Count == 0,
            "Ces tests lisent une identité fixe qu'ils ne créent pas — utiliser FixtureComptesDeTest : "
            + string.Join(", ", fautes));
    }

    [Fact]
    public void TestDb05_Sans_consentement_explicite_aucune_purge_n_est_possible()
    {
        var decision = PolitiqueAmorcageDemo.Decider(
            environnement: "Development",
            motDePasseComptesDemo: "SecretDeTestSynthetique123!",
            optInPurge: null,
            baseDemoAttendue: "DbBillingOffice",
            baseCible: "DbBillingOffice");

        Assert.False(decision.PurgeAutorisee);
    }
}
