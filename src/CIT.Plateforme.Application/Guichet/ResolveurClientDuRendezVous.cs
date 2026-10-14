using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Notifications.Resolveurs;

namespace CIT.Plateforme.Application.Guichet;

/// <summary>
/// FEATURE 010 (Guichet) — résout le destinataire CLIENT des événements de rendez-vous au Billing
/// Office, pour les notifier (confirmation, annulation).
///
/// Ce résolveur vit dans la feature Guichet, PAS dans la couche Notifications : l'invariant I4
/// (spec.md §23) interdit que le moteur de notifications connaisse le vocabulaire du Guichet. Le
/// Guichet, lui, peut s'abonner au port générique <see cref="IResolveurDestinataires"/> et réutilise
/// la résolution client/société existante par délégation — aucune duplication de la logique de
/// cloisonnement inter-sociétés.
/// </summary>
public sealed class ResolveurClientDuRendezVous(ResolveurClientDuDossier client) : IResolveurDestinataires
{
    private static readonly HashSet<string> TypesRendezVous = new(StringComparer.Ordinal)
    {
        "RendezVousReserve",
        "RendezVousAnnule",
    };

    public bool PeutResoudre(string typeEvenement) => TypesRendezVous.Contains(typeEvenement);

    public async Task<IReadOnlyList<Guid>> ResoudreAsync(string typeEvenement, string chargeUtile, CancellationToken ct = default)
    {
        if (!PeutResoudre(typeEvenement))
        {
            return [];
        }

        return await client.ResoudreDestinatairesPourTicketAsync(chargeUtile, ct).ConfigureAwait(false);
    }

    public Task<Guid?> ResoudreTicketAsync(string chargeUtile, CancellationToken ct = default) =>
        client.ResoudreTicketAsync(chargeUtile, ct);
}
