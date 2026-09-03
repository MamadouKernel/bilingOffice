using CIT.Plateforme.Web.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Supervision;

/// <summary>
/// [TEST-SPEC] G-03 — journalisation de la latence côté serveur.
///
/// DÉFAUT CORRIGÉ : <c>MetrologiePerformanceMiddleware</c> injectait un <c>ILogger</c> qu'il
/// n'utilisait jamais. La durée n'existait que dans l'en-tête <c>Server-Timing</c>, lue au vol par
/// le client ; aucune trace n'en subsistait. Une lenteur signalée après coup restait
/// inanalysable.
///
/// EXIGENCE OPPOSÉE, TOUT AUSSI IMPORTANTE : ce journal ne doit pas devenir un journal de données
/// personnelles. Les URL portent des numéros de dossier et de ticket, les chaînes de requête
/// peuvent porter un jeton.
/// </summary>
public sealed class JournalisationLatenceTests
{
    private sealed class JournalCapture : ILogger<MetrologiePerformanceMiddleware>
    {
        public List<(LogLevel Niveau, string Message)> Entrees { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entrees.Add((logLevel, formatter(state, exception)));
    }

    private static async Task<JournalCapture> ExecuterAsync(string chemin, string requete, Func<Task>? traitement = null)
    {
        var journal = new JournalCapture();
        var contexte = new DefaultHttpContext();
        contexte.Request.Method = "GET";
        contexte.Request.Path = chemin;
        contexte.Request.QueryString = new QueryString(requete);
        contexte.Response.Body = new MemoryStream();

        var middleware = new MetrologiePerformanceMiddleware(
            _ => traitement?.Invoke() ?? Task.CompletedTask,
            journal);

        await middleware.InvokeAsync(contexte);
        return journal;
    }

    [Fact]
    public async Task La_duree_de_chaque_requete_est_journalisee()
    {
        var journal = await ExecuterAsync("/comptes/connexion", string.Empty);

        Assert.NotEmpty(journal.Entrees);
        Assert.Contains(journal.Entrees, e => e.Message.Contains(" ms", StringComparison.Ordinal));
    }

    [Fact]
    public async Task La_route_et_le_statut_figurent_dans_la_trace()
    {
        var journal = await ExecuterAsync("/comptes/connexion", string.Empty);

        var trace = journal.Entrees[0].Message;
        Assert.Contains("/comptes/connexion", trace, StringComparison.Ordinal);
        Assert.Contains("GET", trace, StringComparison.Ordinal);
        Assert.Contains("200", trace, StringComparison.Ordinal);
    }

    [Fact]
    public async Task La_chaine_de_requete_n_est_jamais_journalisee()
    {
        // Une chaîne de requête peut porter un jeton de réinitialisation ou un identifiant de
        // dossier. La recopier dans le journal de performance en ferait une donnée conservée
        // plus longtemps et lue par plus de monde que la requête elle-même.
        var journal = await ExecuterAsync(
            "/comptes/reinitialiser-mot-de-passe",
            "?jeton=VALEUR_SENSIBLE_A_NE_PAS_TRACER&utilisateurId=42");

        foreach (var (_, message) in journal.Entrees)
        {
            Assert.DoesNotContain("VALEUR_SENSIBLE_A_NE_PAS_TRACER", message, StringComparison.Ordinal);
            Assert.DoesNotContain("jeton=", message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("utilisateurId=", message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Une_requete_nominale_reste_au_niveau_diagnostic()
    {
        // Le niveau Debug est filtré par défaut hors développement : la mesure reste disponible
        // sans gonfler le volume courant.
        var journal = await ExecuterAsync("/comptes/connexion", string.Empty);

        Assert.All(journal.Entrees, e => Assert.Equal(LogLevel.Debug, e.Niveau));
    }

    [Fact]
    public async Task Une_requete_depassant_le_seuil_NFR_04_est_signalee_en_avertissement()
    {
        // 3 secondes est le seuil contractuel de NFR-04, pas une valeur inventée.
        var journal = await ExecuterAsync("/lent", string.Empty, async () => await Task.Delay(3100));

        Assert.Contains(journal.Entrees, e => e.Niveau == LogLevel.Warning);
        Assert.Contains(journal.Entrees, e => e.Message.Contains("lente", StringComparison.OrdinalIgnoreCase));
    }

    // L'émission de l'en-tête Server-Timing n'est PAS vérifiée ici : elle passe par un rappel
    // OnStarting, que DefaultHttpContext ne déclenche pas. Un test unitaire l'affirmerait donc
    // sans rien prouver. Cette garantie est couverte par NfrCert04PerformanceTests, qui interroge
    // l'application par une vraie requête HTTP.
}
