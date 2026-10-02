using CIT.Plateforme.Infrastructure.Documents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CIT.Plateforme.Infrastructure.HealthChecks;

/// <summary>
/// SEC-04 — signale à l'exploitation que les pièces déposées sont écrites EN CLAIR faute de clé
/// <c>Stockage:CleChiffrement</c>. « Dégradé » et non « En panne » : la plateforme reste utilisable.
/// </summary>
public sealed class ChiffrementDocumentsHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.FromResult(StockageDocumentsFichiers.LireCle(configuration) is null
                ? HealthCheckResult.Degraded($"« {StockageDocumentsFichiers.CleChiffrement} » absente : pièces stockées en clair.")
                : HealthCheckResult.Healthy("Pièces chiffrées au repos (AES-256-GCM)."));
        }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Clé de chiffrement des pièces invalide.", ex));
        }
    }
}
