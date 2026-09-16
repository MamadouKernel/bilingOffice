using CIT.Plateforme.Application.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace CIT.Plateforme.Web.Hubs;

/// <summary>
/// SOC-08 / SUP-01 / SUP-02 — implémentation Web de <see cref="INotificateurTempsReel"/> :
/// pousse un simple signal de rafraîchissement à tous les clients connectés au hub de
/// supervision. Aucune donnée métier ne transite dans le message.
/// </summary>
public sealed class SupervisionSignalRNotifier(IHubContext<SupervisionHub> hub) : INotificateurTempsReel
{
    public const string EvenementRafraichir = "RafraichirSupervision";

    public Task NotifierChangementAsync(CancellationToken ct = default) =>
        hub.Clients.All.SendAsync(EvenementRafraichir, ct);
}
