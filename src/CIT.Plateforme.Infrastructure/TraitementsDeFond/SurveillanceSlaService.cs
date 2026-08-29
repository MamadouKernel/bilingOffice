using CIT.Plateforme.Application.Supervision;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Infrastructure.TraitementsDeFond;

/// <summary>Enveloppe d'hébergement (D5) — aucune règle métier (règle 12) ; toute la logique vit
/// dans <see cref="SurveillerEcheancesUseCase"/>, appelée ici à intervalle configurable.</summary>
public sealed class SurveillanceSlaService(IServiceScopeFactory scopeFactory, ILogger<SurveillanceSlaService> logger) : BackgroundService
{
    public static readonly TimeSpan IntervalleParDefaut = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var minuteur = new PeriodicTimer(IntervalleParDefaut);
        while (!stoppingToken.IsCancellationRequested && await minuteur.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                await TraiterUneFoisAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Échec de la surveillance des échéances SLA.");
            }
        }
    }

    public async Task TraiterUneFoisAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();
        var useCase = scope.ServiceProvider.GetRequiredService<SurveillerEcheancesUseCase>();

        var ticketsActifs = await dbContext.Tickets
            .Where(t => t.Statut != StatutTicket.Cloture && t.Statut != StatutTicket.Annule)
            .Select(t => t.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var ticketId in ticketsActifs)
        {
            await useCase.TraiterTicketAsync(ticketId, ct).ConfigureAwait(false);
        }

        // T104 — Observabilité (Principe XIV) : volumétrie du passage, jamais de donnée sensible
        // (aucun contenu métier, aucun identifiant nominatif d'utilisateur en clair).
        logger.LogInformation("Surveillance SLA : {NombreTickets} ticket(s) actif(s) examiné(s).", ticketsActifs.Count);
    }
}
