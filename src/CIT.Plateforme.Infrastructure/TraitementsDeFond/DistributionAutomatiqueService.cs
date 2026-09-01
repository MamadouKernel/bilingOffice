using CIT.Plateforme.Application.Affectation;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Infrastructure.TraitementsDeFond;

/// <summary>Enveloppe d'hébergement (D5) — aucune règle métier (règle 12) ; toute la logique vit
/// dans <see cref="DistributionAutomatiqueUseCase"/>.</summary>
public sealed class DistributionAutomatiqueService(IServiceScopeFactory scopeFactory, ILogger<DistributionAutomatiqueService> logger) : BackgroundService
{
    public static readonly TimeSpan IntervalleParDefaut = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var minuteur = new PeriodicTimer(IntervalleParDefaut);
        try
        {
            while (await minuteur.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                try
                {
                    await TraiterUneFoisAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Échec de la distribution automatique.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Arrêt demandé par l'hôte.
        }
    }

    public async Task TraiterUneFoisAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();
        var useCase = scope.ServiceProvider.GetRequiredService<DistributionAutomatiqueUseCase>();

        var ticketsEnAttente = await dbContext.Tickets
            .Where(t => t.Statut == StatutTicket.EnAttenteDePriseEnCharge)
            .Select(t => t.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var ticketId in ticketsEnAttente)
        {
            await useCase.TraiterTicketAsync(ticketId, ct).ConfigureAwait(false);
        }

        logger.LogInformation("Distribution automatique : {NombreTickets} ticket(s) en attente examiné(s).", ticketsEnAttente.Count);
    }
}
