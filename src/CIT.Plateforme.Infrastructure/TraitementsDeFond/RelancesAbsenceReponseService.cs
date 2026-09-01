using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Infrastructure.TraitementsDeFond;

/// <summary>
/// Enveloppe d'hébergement (D5) — ne porte AUCUNE règle métier (règle 12). Toute la logique
/// testable vit dans <see cref="RelancesAbsenceReponseUseCase"/> (Application), appelée ici à un
/// intervalle configurable (PC-02 : valeur technique par défaut documentée, non fixée).
/// </summary>
public sealed class RelancesAbsenceReponseService(
    IServiceScopeFactory scopeFactory,
    ILogger<RelancesAbsenceReponseService> logger)
    : BackgroundService
{
    /// <summary>Intervalle de scrutation — valeur technique par défaut (D5), modifiable sans
    /// développement via configuration ; PC-02 reste ouvert pour la latence métier attendue.</summary>
    public static readonly TimeSpan IntervalleParDefaut = TimeSpan.FromMinutes(5);

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
                    logger.LogError(ex, "Échec de l'émission des relances ou clôtures automatiques.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Arrêt demandé par l'hôte.
        }
    }

    /// <summary>Exposé séparément de la boucle temporelle pour rester appelable depuis un test
    /// d'intégration sans attendre le minuteur (règle 12).</summary>
    public async Task TraiterUneFoisAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();
        var useCase = scope.ServiceProvider.GetRequiredService<RelancesAbsenceReponseUseCase>();

        var ticketsEnAttente = await dbContext.Tickets
            .Where(t => t.Statut == Domain.Tickets.StatutTicket.EnAttenteClient)
            .Select(t => t.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        // Paramètres non inventés : valeurs de démonstration technique le temps qu'un mécanisme
        // d'administration (contrat C-05/administration) les rende configurables en base — voir
        // dette technique du rapport final.
        var parametres = new ParametresRelance(NombreRelancesMax: 3, DelaiEntreRelances: TimeSpan.FromDays(2));

        foreach (var ticketId in ticketsEnAttente)
        {
            await useCase.TraiterTicketAsync(ticketId, parametres, ct).ConfigureAwait(false);
        }

        logger.LogInformation("Relances d'absence de réponse : {NombreTickets} ticket(s) en attente client examiné(s).", ticketsEnAttente.Count);
    }
}
