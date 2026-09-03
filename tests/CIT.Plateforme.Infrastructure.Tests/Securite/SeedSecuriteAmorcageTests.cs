using System.Text.RegularExpressions;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Securite;

/// <summary>
/// [TEST-SPEC] INC-01 / INC-02 — amorçage de démonstration.
///
/// INCIDENT : <c>DevelopmentSeeder</c> repliait sur un mot de passe écrit en dur quand
/// <c>CIT_TEST_USERS_PASSWORD</c> était absente. Ce repli avait deux effets : un secret réel
/// publié dans le dépôt, et une purge devenue inconditionnelle — laquelle s'est appliquée à la
/// base de développement réelle, jusqu'à sa suppression et sa recréation.
///
/// RÈGLE VÉRIFIÉE ICI : une absence de configuration EMPÊCHE la purge, elle ne l'autorise pas.
/// </summary>
public sealed class SeedSecuriteAmorcageTests
{
    private const string SecretDeTest = "SecretDeTestSynthetique123!";
    private const string BaseDemo = "CitPlateformeDemo";

    // ================================================================================
    // SEED-SEC-01 — aucun mot de passe en dur dans le code source
    // ================================================================================

    [Fact]
    public void SeedSec01_Aucun_mot_de_passe_code_en_dur_dans_les_sources()
    {
        var racine = LocaliserRacineDepot();
        var sources = Directory
            .EnumerateFiles(Path.Combine(racine, "src"), "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .ToList();

        Assert.NotEmpty(sources);

        // Une affectation littérale à une variable dont le nom évoque un mot de passe. La détection
        // vise la FORME fautive, pas une valeur particulière : inscrire une autre valeur en dur
        // serait exactement la même faute.
        var motif = new Regex(
            @"(?i)\b(password|motdepasse|mot_de_passe|pwd|secret)\w*\s*=\s*""[^""]{6,}""",
            RegexOptions.Compiled);

        var fautes = new List<string>();
        foreach (var fichier in sources)
        {
            foreach (var (ligne, index) in File.ReadAllLines(fichier).Select((l, i) => (l, i + 1)))
            {
                // Les clés de configuration ne sont pas des secrets : ce sont des noms.
                if (ligne.Contains("CIT_TEST_USERS_PASSWORD", StringComparison.Ordinal)
                    || ligne.Contains("Administration:AdministrateurInitial", StringComparison.Ordinal)
                    || ligne.Contains("ConnectionStrings", StringComparison.Ordinal))
                {
                    continue;
                }

                if (motif.IsMatch(ligne))
                {
                    fautes.Add($"{Path.GetRelativePath(racine, fichier)}:{index}");
                }
            }
        }

        Assert.True(
            fautes.Count == 0,
            "Un secret semble codé en dur : " + string.Join(", ", fautes));
    }

    // ================================================================================
    // SEED-SEC-02 / SEED-SAFE-01 / SEED-SAFE-02 — hors développement, rien ne s'exécute
    // ================================================================================

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData(null)]
    [InlineData("")]
    public void SeedSec02_Hors_Development_aucun_amorcage_ni_purge(string? environnement)
    {
        // Même avec TOUTE la configuration destructive réunie, l'environnement seul suffit à
        // tout refuser.
        var decision = PolitiqueAmorcageDemo.Decider(
            environnement, SecretDeTest, "true", BaseDemo, BaseDemo);

        Assert.False(decision.AmorcageAutorise);
        Assert.False(decision.PurgeAutorisee);
    }

    // ================================================================================
    // SEED-SEC-04 — un secret absent ne se remplace jamais
    // ================================================================================

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SeedSec04_Secret_absent_n_est_jamais_remplace_par_un_repli(string? secret)
    {
        var decision = PolitiqueAmorcageDemo.Decider(
            "Development", secret, "true", BaseDemo, BaseDemo);

        Assert.False(decision.AmorcageAutorise);
        Assert.False(decision.PurgeAutorisee);
        Assert.Contains(PolitiqueAmorcageDemo.CleMotDePasse, decision.Motif, StringComparison.Ordinal);
    }

    // ================================================================================
    // SEED-SEC-03 / SEED-SAFE-03 — sans consentement explicite, pas de purge
    // ================================================================================

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("false")]
    [InlineData("1")]
    [InlineData("oui")]
    public void SeedSafe03_Development_sans_consentement_explicite_ne_purge_pas(string? optIn)
    {
        var decision = PolitiqueAmorcageDemo.Decider(
            "Development", SecretDeTest, optIn, BaseDemo, BaseDemo);

        // L'amorçage non destructif reste possible : c'est la purge qui exige le consentement.
        Assert.True(decision.AmorcageAutorise);
        Assert.False(decision.PurgeAutorisee);
    }

    // ================================================================================
    // SEED-SAFE-04 — identité de base non confirmée : purge refusée
    // ================================================================================

    [Fact]
    public void SeedSafe04_Base_non_declaree_refuse_la_purge()
    {
        var decision = PolitiqueAmorcageDemo.Decider(
            "Development", SecretDeTest, "true", baseDemoAttendue: null, baseCible: "DbBillingOffice");

        Assert.True(decision.AmorcageAutorise);
        Assert.False(decision.PurgeAutorisee);
    }

    [Fact]
    public void SeedSafe04_Base_differente_de_celle_declaree_refuse_la_purge()
    {
        // Le cas exact de l'incident : la configuration désigne une base de démonstration, mais
        // l'application est en réalité connectée à la base de développement.
        var decision = PolitiqueAmorcageDemo.Decider(
            "Development", SecretDeTest, "true", baseDemoAttendue: BaseDemo, baseCible: "DbBillingOffice");

        Assert.True(decision.AmorcageAutorise);
        Assert.False(decision.PurgeAutorisee);
        Assert.Contains("DbBillingOffice", decision.Motif, StringComparison.Ordinal);
    }

    // ================================================================================
    // SEED-SAFE-05 — les quatre barrières réunies autorisent le comportement attendu
    // ================================================================================

    [Fact]
    public void SeedSafe05_Base_de_demonstration_declaree_et_consentement_autorisent_la_purge()
    {
        var decision = PolitiqueAmorcageDemo.Decider(
            "Development", SecretDeTest, "true", BaseDemo, BaseDemo);

        Assert.True(decision.AmorcageAutorise);
        Assert.True(decision.PurgeAutorisee);
    }

    [Fact]
    public void SeedSafe05_La_comparaison_de_base_ignore_la_casse()
    {
        var decision = PolitiqueAmorcageDemo.Decider(
            "development", SecretDeTest, "TRUE", BaseDemo, BaseDemo.ToUpperInvariant());

        Assert.True(decision.PurgeAutorisee);
    }

    // ================================================================================
    // Le motif journalisé ne doit jamais porter le secret
    // ================================================================================

    [Theory]
    [InlineData("Production", "true")]
    [InlineData("Development", null)]
    [InlineData("Development", "true")]
    public void Le_motif_journalise_ne_contient_jamais_le_secret(string environnement, string? optIn)
    {
        var decision = PolitiqueAmorcageDemo.Decider(
            environnement, SecretDeTest, optIn, BaseDemo, "AutreBase");

        Assert.DoesNotContain(SecretDeTest, decision.Motif, StringComparison.Ordinal);
    }

    private static string LocaliserRacineDepot()
    {
        var repertoire = new DirectoryInfo(AppContext.BaseDirectory);
        while (repertoire is not null && !Directory.Exists(Path.Combine(repertoire.FullName, "src")))
        {
            repertoire = repertoire.Parent;
        }

        Assert.NotNull(repertoire);
        return repertoire!.FullName;
    }
}
