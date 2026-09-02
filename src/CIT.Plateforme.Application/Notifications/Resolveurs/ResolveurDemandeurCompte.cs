using CIT.Plateforme.Application.Abstractions;

namespace CIT.Plateforme.Application.Notifications.Resolveurs;

/// <summary>
/// Destinataire d'une décision sur une demande de compte : le DEMANDEUR lui-même.
/// </summary>
public sealed class ResolveurDemandeurCompte : IResolveurDestinataires
{
    private static readonly HashSet<string> TypesDecisionCompte = new(StringComparer.Ordinal)
    {
        "CompteValide",
        "CompteRejete",
    };

    public bool PeutResoudre(string typeEvenement) => TypesDecisionCompte.Contains(typeEvenement);

    public Task<IReadOnlyList<Guid>> ResoudreAsync(string typeEvenement, string chargeUtile, CancellationToken ct = default)
    {
        var demandeurId = ResolveurChargeUtile.LireGuid(chargeUtile, "demandeurId");
        IReadOnlyList<Guid> destinataires = demandeurId is null ? [] : [demandeurId.Value];
        return Task.FromResult(destinataires);
    }

    public Task<Guid?> ResoudreTicketAsync(string chargeUtile, CancellationToken ct = default) =>
        Task.FromResult<Guid?>(null);
}
