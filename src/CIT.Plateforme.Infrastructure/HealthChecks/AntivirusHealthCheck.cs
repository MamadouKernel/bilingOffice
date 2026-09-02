using CIT.Plateforme.Domain.Documents;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CIT.Plateforme.Infrastructure.HealthChecks;

public class AntivirusHealthCheck : IHealthCheck
{
    private readonly IAnalyseAntivirus _scannerAntivirus;

    public AntivirusHealthCheck(IAnalyseAntivirus scannerAntivirus)
    {
        _scannerAntivirus = scannerAntivirus;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_scannerAntivirus != null)
            {
                return Task.FromResult(HealthCheckResult.Healthy("Moteur d'analyse antivirus opérationnel."));
            }
            return Task.FromResult(HealthCheckResult.Unhealthy("Scanner antivirus non initialisé."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Erreur de diagnostic antivirus.", ex));
        }
    }
}
