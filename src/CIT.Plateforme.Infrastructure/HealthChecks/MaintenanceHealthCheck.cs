using CIT.Plateforme.Application.Supervision;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CIT.Plateforme.Infrastructure.HealthChecks;

public class MaintenanceHealthCheck : IHealthCheck
{
    private readonly GestionnaireMaintenanceService _gestionnaireMaintenance;

    public MaintenanceHealthCheck(GestionnaireMaintenanceService gestionnaireMaintenance)
    {
        _gestionnaireMaintenance = gestionnaireMaintenance;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (_gestionnaireMaintenance.EstEnMaintenance)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy($"Plateforme en mode maintenance planifiée : {_gestionnaireMaintenance.Motif}"));
        }

        return Task.FromResult(HealthCheckResult.Healthy("Aucune maintenance planifiée en cours."));
    }
}
