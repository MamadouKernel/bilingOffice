using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

/// <summary>SOC-04 : chaque agent voit uniquement les tickets en attente de son rôle (sa file),
/// avec tri et filtres. L'habilitation sur la file est vérifiée côté serveur avant toute lecture
/// (Principe VII).</summary>
public sealed class ConsulterFileQuery(
    ITicketRepository tickets,
    IAutorisationService autorisation,
    IPrioriteRepository? priorites = null)
{
    public async Task<IReadOnlyList<Ticket>> ExecuterAsync(Guid agentId, Guid fileId, StatutTicket? filtreStatut = null, CancellationToken ct = default)
    {
        bool peutStandard = await autorisation.EstAutoriseAsync(agentId, Permission.TicketPrendreEnCharge, fileId, ct).ConfigureAwait(false);
        bool peutComplaint = await autorisation.EstAutoriseAsync(agentId, Permission.ComplaintInstruire, fileId, ct).ConfigureAwait(false);
        bool peutEnrolement = await autorisation.EstAutoriseAsync(agentId, Permission.EnrolementInstruire, fileId, ct).ConfigureAwait(false);

        if (!peutStandard && !peutComplaint && !peutEnrolement)
        {
            throw new AccesRefuseException($"L'agent {agentId} n'a pas les autorisations nécessaires pour consulter la file {fileId}.");
        }

        var ticketsDeLaFile = await tickets.ListerParFileAsync(fileId, ct).ConfigureAwait(false);

        var ticketsFiltres = filtreStatut is StatutTicket statut
            ? ticketsDeLaFile.Where(t => t.Statut == statut).ToList()
            : ticketsDeLaFile.ToList();

        if (priorites is not null)
        {
            var niveaux = await priorites.ListerNiveauxAsync(ct).ConfigureAwait(false);
            var rangsMap = niveaux.ToDictionary(n => n.Id, n => n.Rang);

            return [.. ticketsFiltres
                .OrderByDescending(t => t.PrioriteId.HasValue && rangsMap.TryGetValue(t.PrioriteId.Value, out var rang) ? rang : 0)
                .ThenBy(t => t.DateCreationUtc)];
        }

        return ticketsFiltres;
    }
}
