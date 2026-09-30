using CIT.Plateforme.Application.Abstractions;

namespace CIT.Plateforme.Web.Services;

/// <summary>
/// Abstraction du signal temps réel de supervision, côté ABONNEMENT. Elle permet aux tableaux de bord
/// de s'abonner (<see cref="Change"/>) sans dépendre d'une implémentation concrète — mono-nœud
/// (<see cref="SignalSupervision"/>, événement en mémoire) ou multi-nœuds
/// (<see cref="SignalSupervisionRedis"/>, diffusion pub/sub Redis). Le côté ÉMISSION reste
/// <see cref="INotificateurTempsReel"/> (utilisé par la couche Application).
/// </summary>
public interface ISignalSupervision : INotificateurTempsReel
{
    /// <summary>Déclenché après chaque changement ; les abonnés se désabonnent à leur libération.</summary>
    event Func<Task>? Change;
}
