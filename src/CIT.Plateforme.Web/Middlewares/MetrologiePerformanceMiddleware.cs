using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Web.Middlewares;

/// <summary>
/// Métrologie de performance (NFR-04) : mesure la durée de chaque requête, l'expose au client par
/// l'en-tête <c>Server-Timing</c>, et la journalise côté serveur.
///
/// G-03 — POURQUOI LA JOURNALISATION A ÉTÉ AJOUTÉE : ce middleware injectait un
/// <see cref="ILogger"/> qu'il n'utilisait jamais. La durée n'était donc observable que par un
/// client lisant l'en-tête au moment de l'appel, et aucune trace n'en subsistait. Une lenteur
/// signalée après coup — « c'était lent hier vers 15 h » — restait inanalysable : ni mesure, ni
/// route, ni horodatage. Une exigence de temps de réponse qu'on ne peut pas constater a posteriori
/// n'est pas exploitable en exploitation.
/// </summary>
public class MetrologiePerformanceMiddleware
{
    /// <summary>
    /// Au-delà de ce seuil, la requête est journalisée en avertissement. En deçà, elle l'est en
    /// trace de diagnostic, donc filtrable par configuration.
    ///
    /// La valeur découle du CDC : NFR-04 fixe un temps de réponse cible inférieur à 3 secondes.
    /// Ce n'est pas un paramètre métier inventé — c'est le seuil contractuel lui-même, utilisé
    /// pour distinguer ce qui mérite l'attention de ce qui est nominal.
    /// </summary>
    private const double SeuilAvertissementMs = 3000;

    private readonly RequestDelegate _next;
    private readonly ILogger<MetrologiePerformanceMiddleware> _logger;

    public MetrologiePerformanceMiddleware(RequestDelegate next, ILogger<MetrologiePerformanceMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var debut = Stopwatch.GetTimestamp();

        context.Response.OnStarting(() =>
        {
            var dureeMs = Stopwatch.GetElapsedTime(debut).TotalMilliseconds;

            if (!context.Response.Headers.ContainsKey("Server-Timing"))
            {
                context.Response.Headers.Append("Server-Timing", $"total;dur={dureeMs:F2}");
            }

            return Task.CompletedTask;
        });

        try
        {
            await _next(context);
        }
        finally
        {
            var dureeMs = Stopwatch.GetElapsedTime(debut).TotalMilliseconds;
            Journaliser(context, dureeMs);
        }
    }

    /// <summary>
    /// Journalise la mesure.
    ///
    /// CE QUI EST TRACÉ : le gabarit de route (ou le chemin), la méthode, le code de statut et la
    /// durée. Rien d'autre.
    ///
    /// CE QUI NE L'EST JAMAIS : la chaîne de requête, le corps, les en-têtes, l'identité de
    /// l'appelant. Une URL porte des identifiants de dossier et de ticket ; une chaîne de requête
    /// peut porter un jeton. Un journal de performance qui les recopie devient un journal de
    /// données personnelles, conservé plus longtemps et lu par plus de monde.
    ///
    /// Le gabarit de route est préféré au chemin quand il est disponible : « /tickets/{numero} »
    /// agrège les mesures et ne divulgue aucun numéro réel.
    /// </summary>
    private void Journaliser(HttpContext context, double dureeMs)
    {
        var route = context.GetEndpoint() is Microsoft.AspNetCore.Routing.RouteEndpoint point
            ? point.RoutePattern.RawText
            : context.Request.Path.Value;

        if (dureeMs >= SeuilAvertissementMs)
        {
            _logger.LogWarning(
                "Requête lente : {Methode} {Route} → {Statut} en {DureeMs:F0} ms (seuil NFR-04 : {SeuilMs} ms).",
                context.Request.Method, route, context.Response.StatusCode, dureeMs, SeuilAvertissementMs);
            return;
        }

        // En deçà du seuil, la mesure reste disponible pour l'analyse sans peser sur le volume
        // courant : le niveau Debug est filtré par défaut en dehors du développement.
        _logger.LogDebug(
            "{Methode} {Route} → {Statut} en {DureeMs:F0} ms.",
            context.Request.Method, route, context.Response.StatusCode, dureeMs);
    }
}

public static class MetrologiePerformanceMiddlewareExtensions
{
    public static IApplicationBuilder UseMetrologiePerformance(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<MetrologiePerformanceMiddleware>();
    }
}
