namespace CIT.Plateforme.Application.Abstractions;

/// <summary>
/// FEATURE 009 — frontière transactionnelle explicite du moteur de notification.
///
/// POURQUOI CE PORT EXISTE (tasks.md §6, scénarios C et D) : la distribution d'un événement doit
/// être atomique de bout en bout — prise de l'événement, création des notifications, marquage du
/// curseur. Sans transaction explicite :
///   - un échec après la prise laisserait l'événement marqué distribué sans notification
///     (perte silencieuse — scénario C) ;
///   - deux workers concurrents pourraient franchir la prise simultanément (scénario D).
///
/// Le marquage <c>DistribueLeUtc</c> ne doit JAMAIS être committé avant la persistance durable
/// des notifications. Garantie visée : at-least-once au traitement, effectively-once à l'effet
/// observable (tasks.md I9). Aucune garantie de livraison unique stricte n'est revendiquée : elle ne serait pas démontrable ici.
///
/// L'Application ne connaît ni EF Core, ni SQL Server : ce port est implémenté par
/// l'Infrastructure (Constitution Principe IV).
/// </summary>
public interface IExecuteurTransactionnel
{
    /// <summary>
    /// Exécute <paramref name="operation"/> dans une transaction. En cas d'exception, la
    /// transaction est annulée et l'exception propagée à l'appelant, qui décide de poursuivre
    /// avec les événements suivants (isolation — un événement en échec n'interrompt pas la boucle).
    /// </summary>
    Task<T> ExecuterAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default);
}
