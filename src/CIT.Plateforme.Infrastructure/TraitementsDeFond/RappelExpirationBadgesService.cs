using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Enrolement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Infrastructure.TraitementsDeFond;

/// <summary>
/// Enveloppe d'hébergement (D5) — aucune règle métier (règle 12) ; toute la logique vit dans
/// <see cref="RappelerExpirationBadgesCommand"/>.
///
/// DÉFAUT CORRIGÉ (audit du 2026-09-04, <c>docs/89</c>) : ENR-05 exige la « détection proactive »
/// de l'expiration des badges. La commande était écrite et testée, et <b>rien ne l'appelait</b> —
/// ni écran, ni ordonnancement. Une détection proactive que personne ne déclenche n'a jamais lieu.
///
/// Elle est ordonnancée ici, et non exposée dans un écran : sa nature est d'être périodique et
/// sans opérateur.
/// </summary>
public sealed class RappelExpirationBadgesService(
    IServiceScopeFactory scopeFactory,
    ILogger<RappelExpirationBadgesService> logger) : BackgroundService
{
    /// <summary>
    /// Cadence <b>technique</b> de réveil, pas une valeur métier : les seules valeurs métier —
    /// durée de validité d'un badge et délai de préavis — restent dans la configuration
    /// (<c>Enrolement:DureeValiditeBadgeJours</c>, <c>Enrolement:DelaiRappelExpirationJours</c>)
    /// et ne sont pas inventées par le code (PC-12).
    /// </summary>
    public static readonly TimeSpan IntervalleParDefaut = TimeSpan.FromHours(12);

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
                    logger.LogError(ex, "Échec du rappel d'expiration des badges.");
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

        // Échéances des documents de flotte (03/10/2026) : indépendantes du paramétrage des badges.
        var echeances = await scope.ServiceProvider.GetRequiredService<RappelerEcheancesDocumentsCommand>()
            .ExecuterAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Rappel d'échéance des documents de flotte : {NombreRappels} rappel(s) émis.", echeances);

        // Rappels de date d'enlèvement (05/10/2026) : veille et dépassement, indépendants du paramétrage des badges.
        var enlevements = await scope.ServiceProvider.GetRequiredService<CIT.Plateforme.Application.BillingImport.RappelerDatesEnlevementCommand>()
            .ExecuterAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Rappel de date d'enlèvement : {NombreRappels} rappel(s) émis.", enlevements);

        // Tant que les deux paramètres ne sont pas administrés, il n'y a rien à faire : ce n'est
        // pas une panne, et cela ne doit pas remplir le journal d'erreurs à chaque réveil.
        var parametres = scope.ServiceProvider.GetRequiredService<IParametresEnrolementProvider>();
        if (parametres.ObtenirDureeValiditeBadgeJours() is null
            || parametres.ObtenirDelaiRappelExpirationJours() is null)
        {
            logger.LogInformation(
                "Rappel d'expiration des badges : durée de validité ou délai de préavis non configuré, passe ignorée.");
            return;
        }

        var commande = scope.ServiceProvider.GetRequiredService<RappelerExpirationBadgesCommand>();
        var nombre = await commande.ExecuterAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Rappel d'expiration des badges : {NombreRappels} rappel(s) émis.", nombre);
    }
}
