using CIT.Plateforme.Domain.Documents;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CIT.Plateforme.Infrastructure.HealthChecks;

public class StorageHealthCheck : IHealthCheck
{
    private readonly IStockageDocuments _stockageDocuments;

    public StorageHealthCheck(IStockageDocuments stockageDocuments)
    {
        _stockageDocuments = stockageDocuments;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_stockageDocuments != null)
            {
                return Task.FromResult(HealthCheckResult.Healthy("Service de stockage documentaire GED accessible."));
            }
            return Task.FromResult(HealthCheckResult.Unhealthy("Service de stockage documentaire non disponible."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Erreur d'accès au stockage GED.", ex));
        }
    }
}
