using CIT.Plateforme.Application.Exploitation;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CIT.Plateforme.Web.Services;

/// <summary>
/// NFR-03 / NFR-10 — Sonde interne : à intervalle régulier (paramétrable), vérifie les dépendances
/// critiques (sondes « ready » : base, stockage, chiffrement…) et enregistre un battement. Un trou dans
/// les battements = la plateforme ne tournait pas ; le rapport de disponibilité s'en déduit.
/// </summary>
public sealed class SondeDisponibiliteService(
    IServiceScopeFactory portees,
    HealthCheckService sondes,
    ILogger<SondeDisponibiliteService> journal) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Laisse le démarrage (migrations, provisionnement) se terminer avant la première mesure.
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken).ConfigureAwait(false);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var rapport = await sondes.CheckHealthAsync(s => s.Tags.Contains("ready"), stoppingToken).ConfigureAwait(false);
                using var portee = portees.CreateScope();
                await portee.ServiceProvider.GetRequiredService<EnregistrerPulsationDisponibilite>()
                    .ExecuterAsync(rapport.Status != HealthStatus.Unhealthy, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Base injoignable : aucun battement ne peut être écrit, le trou sera compté comme indisponibilité.
                journal.LogWarning(ex, "Sonde de disponibilité : mesure non enregistrée.");
            }
            await Task.Delay(ParametresCourants.IntervalleSonde, stoppingToken).ConfigureAwait(false);
        }
    }
}
