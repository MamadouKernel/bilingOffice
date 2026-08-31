using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

/// <summary>SOC-04 : chaque agent voit uniquement les tickets en attente de son rôle (sa file),
/// avec tri et filtres. L'habilitation sur la file est vérifiée côté serveur avant toute lecture
/// (Principe VII).</summary>
public sealed class ConsulterFileQuery(ITicketRepository tickets, IAutorisationService autorisation)
{
    public async Task<IReadOnlyList<Ticket>> ExecuterAsync(Guid agentId, Guid fileId, StatutTicket? filtreStatut = null, CancellationToken ct = default)
    {
        bool peutStandard = await autorisation.EstAutoriseAsync(agentId, Permission.TicketPrendreEnCharge, fileId, ct).ConfigureAwait(false);
        bool peutComplaint = await autorisation.EstAutoriseAsync(agentId, Permission.ComplaintInstruire, fileId, ct).ConfigureAwait(false);
        
        if (!peutStandard && !peutComplaint)
        {
            throw new AccesRefuseException($"L'agent {agentId} n'a pas les autorisations nécessaires pour consulter la file {fileId}.");
        }

        var ticketsDeLaFile = await tickets.ListerParFileAsync(fileId, ct).ConfigureAwait(false);

        return filtreStatut is StatutTicket statut
            ? [.. ticketsDeLaFile.Where(t => t.Statut == statut)]
            : ticketsDeLaFile;
    }
}
