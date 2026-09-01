using CIT.Plateforme.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>
/// FEATURE 009 — implémentation EF Core de la frontière transactionnelle du moteur de
/// notification (tasks.md T192, scénarios C et D).
///
/// La stratégie d'exécution d'EF Core est respectée : si des relances de connexion sont
/// configurées, l'opération complète est rejouée depuis le début, jamais partiellement.
/// </summary>
public sealed class ExecuteurTransactionnel(PlateformeDbContext dbContext) : IExecuteurTransactionnel
{
    public async Task<T> ExecuterAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default)
    {
        // Une transaction déjà ouverte par l'appelant (cas des tests d'intégration) est
        // respectée : ne pas en imbriquer une seconde.
        if (dbContext.Database.CurrentTransaction is not null)
        {
            return await operation(ct).ConfigureAwait(false);
        }

        var strategie = dbContext.Database.CreateExecutionStrategy();

        return await strategie.ExecuteAsync(async token =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(token).ConfigureAwait(false);
            try
            {
                var resultat = await operation(token).ConfigureAwait(false);
                await transaction.CommitAsync(token).ConfigureAwait(false);
                return resultat;
            }
            catch
            {
                // Scénario C : l'événement reste DistribueLeUtc = null et sera relu au tour
                // suivant (garantie at-least-once).
                await transaction.RollbackAsync(token).ConfigureAwait(false);
                throw;
            }
        }, ct).ConfigureAwait(false);
    }
}
