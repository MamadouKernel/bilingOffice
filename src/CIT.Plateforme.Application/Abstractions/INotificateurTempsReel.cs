namespace CIT.Plateforme.Application.Abstractions;

/// <summary>
/// SOC-08 / SUP-01 / SUP-02 — diffusion temps réel vers le tableau de bord de supervision.
/// Purement un signal de rafraîchissement : aucun état métier ne transite ici, la donnée reste
/// exclusivement lue via les cas d'usage Application (VueFilesQuery/VueChargeQuery), jamais
/// portée par ce canal.
/// </summary>
public interface INotificateurTempsReel
{
    Task NotifierChangementAsync(CancellationToken ct = default);
}
