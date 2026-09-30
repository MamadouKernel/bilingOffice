using CIT.Plateforme.Application.Abstractions;

namespace CIT.Plateforme.Web.Services;

/// <summary>
/// Signal temps réel de la supervision (SOC-08, SUP-01..03) : « quelque chose a changé », sans
/// aucun état métier — les tableaux de bord relisent toujours leurs vues via les cas d'usage.
///
/// Blazor Server exécute déjà les composants sur le serveur : un événement en mémoire (singleton)
/// suffit à prévenir tous les circuits ouverts. Il remplace un hub SignalR auquel le serveur se
/// connectait lui-même (aller-retour réseau, dépendance cliente, route exposée).
/// </summary>
public sealed class SignalSupervision : ISignalSupervision
{
    /// <summary>Déclenché après chaque enregistrement ; les abonnés doivent se désabonner à leur libération.</summary>
    public event Func<Task>? Change;

    public async Task NotifierChangementAsync(CancellationToken ct = default)
    {
        foreach (var abonne in Change?.GetInvocationList().Cast<Func<Task>>() ?? [])
        {
            try
            {
                await abonne().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Un tableau de bord en erreur ou déjà fermé ne doit ni bloquer les autres, ni
                // faire échouer l'enregistrement métier qui a déclenché le signal.
            }
        }
    }
}
