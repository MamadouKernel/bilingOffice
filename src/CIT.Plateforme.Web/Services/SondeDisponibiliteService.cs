using CIT.Plateforme.Application.Exploitation;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CIT.Plateforme.Web.Services;

/// <summary>
/// NFR-03 / NFR-10 — Sonde interne : à intervalle fixe (paramétrable), vérifie que le portail répond
/// (base, stockage, chiffrement) et enregistre un battement. Un trou dans les battements = la
/// plateforme ne tournait pas ; le rapport de disponibilité s'en déduit.
/// </summary>
public sealed class SondeDisponibiliteService(
    IServiceScopeFactory portees,
    HealthCheckService sondes,
    ILogger<SondeDisponibiliteService> journal) : BackgroundService
{
    /// <summary>Au-delà, la plateforme est comptée indisponible pour cette mesure (au lieu de laisser un trou).</summary>
    private static readonly TimeSpan DelaiMaxMesure = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Laisse le démarrage (migrations, provisionnement) se terminer avant la première mesure.
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken).ConfigureAwait(false);
        while (!stoppingToken.IsCancellationRequested)
        {
            var prochaine = DateTime.UtcNow + ParametresCourants.IntervalleSonde;
            await MesurerAsync(stoppingToken).ConfigureAwait(false);
            // Cadence fixe : une mesure lente ne décale pas les suivantes.
            var attente = prochaine - DateTime.UtcNow;
            if (attente > TimeSpan.Zero)
            {
                await Task.Delay(attente, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private async Task MesurerAsync(CancellationToken stoppingToken)
    {
        bool saine;
        try
        {
            using var limite = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            limite.CancelAfter(DelaiMaxMesure);
            // L'antivirus n'empêche pas l'accès au portail (seul le dépôt de pièces en dépend) : il est
            // suivi par /ready, pas compté dans la disponibilité.
            var verification = sondes.CheckHealthAsync(s => s.Tags.Contains("ready") && s.Name != "antivirus", limite.Token);
            // On n'attend pas plus que le délai, même si une vérification ignore l'annulation.
            if (await Task.WhenAny(verification, Task.Delay(DelaiMaxMesure, stoppingToken)).ConfigureAwait(false) != verification)
            {
                throw new OperationCanceledException();
            }
            saine = (await verification.ConfigureAwait(false)).Status != HealthStatus.Unhealthy;
        }
        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
        {
            journal.LogWarning("Sonde de disponibilité : aucune réponse en {Delai} s, mesure comptée indisponible.", DelaiMaxMesure.TotalSeconds);
            saine = false;
        }

        try
        {
            using var portee = portees.CreateScope();
            await portee.ServiceProvider.GetRequiredService<EnregistrerPulsationDisponibilite>()
                .ExecuterAsync(saine, stoppingToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Base injoignable : aucun battement ne peut être écrit, le trou sera compté comme indisponibilité.
            journal.LogWarning(ex, "Sonde de disponibilité : mesure non enregistrée.");
        }
    }
}
