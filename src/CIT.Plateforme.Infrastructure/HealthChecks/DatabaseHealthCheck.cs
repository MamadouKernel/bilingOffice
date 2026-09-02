using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CIT.Plateforme.Infrastructure.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly PlateformeDbContext _dbContext;

    public DatabaseHealthCheck(PlateformeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Connectivité base de données opérationnelle.")
                : HealthCheckResult.Unhealthy("Impossible de se connecter à la base de données SQL.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Erreur de connexion à la base de données SQL.", ex);
        }
    }
}
