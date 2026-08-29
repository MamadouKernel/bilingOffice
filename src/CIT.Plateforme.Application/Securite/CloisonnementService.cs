using CIT.Plateforme.Application.Abstractions;

namespace CIT.Plateforme.Application.Securite;

/// <summary>
/// Cloisonnement client/agent (T015, SEC-01, SEC-02, SEC-03 ; CPT-04 cité — hors périmètre de ce
/// socle mais respecté ici en frontière). Un client n'accède qu'à ses propres tickets et
/// documents ; un agent n'accède qu'aux files et actions correspondant à ses habilitations
/// (vérifiées séparément par <see cref="IAutorisationService"/>).
/// </summary>
public sealed class CloisonnementService(ITicketRepository tickets, IDossierRepository dossiers)
{
    /// <summary>Vérifie qu'un client est bien propriétaire (via son dossier) du ticket consulté.</summary>
    public async Task<bool> ClientEstProprietaireDuTicketAsync(Guid clientId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false);
        if (ticket is null)
        {
            return false;
        }

        var dossier = await dossiers.ObtenirParIdAsync(ticket.DossierId, ct).ConfigureAwait(false);
        return dossier is not null && dossier.ClientId == clientId;
    }

    public async Task ExigerProprieteClientAsync(Guid clientId, Guid ticketId, CancellationToken ct = default)
    {
        var proprietaire = await ClientEstProprietaireDuTicketAsync(clientId, ticketId, ct).ConfigureAwait(false);
        if (!proprietaire)
        {
            throw new AccesRefuseException($"Le client {clientId} n'est pas propriétaire du ticket {ticketId} (SEC-01).");
        }
    }
}
