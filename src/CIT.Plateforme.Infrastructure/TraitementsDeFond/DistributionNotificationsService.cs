using CIT.Plateforme.Application.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Infrastructure.TraitementsDeFond;

/// <summary>
/// FEATURE 009 (T135, research.md D8) — enveloppe d'hébergement du moteur de distribution.
///
/// Patron établi par <see cref="RelancesAbsenceReponseService"/> (F001) et repris à l'identique :
/// la BackgroundService ne porte AUCUNE règle métier ; toute la logique testable vit dans
/// <see cref="DistribuerNotificationsUseCase"/> (couche Application), et
/// <see cref="TraiterUneFoisAsync"/> est exposée séparément de la boucle temporelle pour rester
/// appelable depuis un test d'intégration sans attendre le minuteur.
///
/// F009 réutilise le PATRON des traitements de fond du socle, jamais leurs capacités :
/// RelancesAbsenceReponseService, SurveillanceSlaService et DistributionAutomatiqueService ne
/// sont ni modifiés, ni refondus (spec.md §17, tasks.md T137).
/// </summary>
public sealed class DistributionNotificationsService(
    IServiceScopeFactory scopeFactory,
    ILogger<DistributionNotificationsService> logger)
    : BackgroundService
{
    /// <summary>Intervalle de scrutation — VALEUR TECHNIQUE, pas une règle métier. Le CDC ne fixe
    /// aucune latence de notification (PC-02 ne couvre que la supervision temps réel).</summary>
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
                    logger.LogError(ex, "Échec du tour de distribution des notifications.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Arrêt demandé par l'hôte.
        }
    }

    public async Task<int> TraiterUneFoisAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<DistribuerNotificationsUseCase>();

        var crees = await useCase.DistribuerAsync(ct: ct).ConfigureAwait(false);

        if (crees > 0)
        {
            logger.LogInformation("Distribution des notifications : {Nombre} notification(s) créée(s).", crees);
        }

        return crees;
    }
}

/// <summary>
/// FEATURE 009 (T136, research.md D8/D9) — enveloppe d'hébergement de la reprise ERR-02.
///
/// AUCUNE valeur de reprise n'est écrite ici : nombre de tentatives et cadence proviennent
/// exclusivement de la configuration, validée au démarrage (CLAR-NOT-02). Seul l'intervalle de
/// scrutation du service est une valeur technique.
/// </summary>
public sealed class ReessaiEnvoisNotificationService(
    IServiceScopeFactory scopeFactory,
    ParametresReessaiNotification parametres,
    ILogger<ReessaiEnvoisNotificationService> logger)
    : BackgroundService
{
    public ParametresReessaiNotification Parametres { get; } = parametres ?? throw new ArgumentNullException(nameof(parametres));

    /// <summary>Intervalle de scrutation — valeur technique. NE FIXE NI le délai entre
    /// tentatives, NI le nombre de tentatives : ces deux valeurs sont métier et configurées.</summary>
    public static readonly TimeSpan IntervalleParDefaut = TimeSpan.FromMinutes(1);

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
                    logger.LogError(ex, "Échec du tour de reprise des envois de notification.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Arrêt demandé par l'hôte.
        }
    }

    public async Task<int> TraiterUneFoisAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<ReessayerEnvoisNotificationUseCase>();

        var traites = await useCase.ReessayerAsync(ct).ConfigureAwait(false);

        if (traites > 0)
        {
            logger.LogInformation("Reprise des envois de notification : {Nombre} envoi(s) traité(s).", traites);
        }

        return traites;
    }
}
