using CIT.Plateforme.Infrastructure.Documents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CIT.Plateforme.Infrastructure.HealthChecks;

/// <summary>
/// SEC-07 — état RÉEL du moteur antivirus. L'ancienne version répondait « opérationnel » dès que
/// le service était injecté, même sans Defender sur le serveur : les dépôts y étaient alors tous
/// refusés (fail-closed) sans que l'exploitation le voie. Le contrôle vérifie désormais que
/// MpCmdRun.exe est réellement présent, avec la même résolution que le scanner.
/// </summary>
public sealed class AntivirusHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var chemin = ScannerAntivirusDefender.ResoudreCheminMpCmdRun(configuration["Securite:Antivirus:CheminExecutable"]);
        return Task.FromResult(chemin is null
            ? HealthCheckResult.Unhealthy("Microsoft Defender (MpCmdRun.exe) introuvable : tout dépôt de document est refusé.")
            : HealthCheckResult.Healthy($"Microsoft Defender disponible : {chemin}"));
    }
}
