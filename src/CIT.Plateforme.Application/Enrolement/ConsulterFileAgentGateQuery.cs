using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Enrolement;

/// <summary>
/// Glue UI (C8-10) — résout la file "Agent Gate" par nom (jamais un Guid en dur) puis délègue au
/// contrôle d'autorisation générique, mirroring ConsulterFileReclamationsQuery (F006).
/// </summary>
public sealed class ConsulterFileAgentGateQuery(
    ITicketRepository ticketRepository,
    IFileDeTraitementRepository fileRepository,
    IAutorisationService autorisationService)
{
    public async Task<IReadOnlyList<Ticket>> ExecuterAsync(Guid agentId, CancellationToken ct = default)
    {
        var files = await fileRepository.ListerAsync(ct).ConfigureAwait(false);
        var fileAgentGate = files.FirstOrDefault(f => f.Module == "Enrolement" && f.Nom == "Agent Gate")
            ?? throw new InvalidOperationException("La file « Agent Gate » n'est pas configurée.");

        var peutStandard = await autorisationService.EstAutoriseAsync(agentId, Permission.TicketPrendreEnCharge, fileAgentGate.Id, ct).ConfigureAwait(false);
        var peutEnrolement = await autorisationService.EstAutoriseAsync(agentId, Permission.EnrolementInstruire, fileAgentGate.Id, ct).ConfigureAwait(false);

        if (!peutStandard && !peutEnrolement)
        {
            throw new AccesRefuseException($"L'agent {agentId} n'a pas l'autorisation d'accéder à la file Agent Gate.");
        }

        var tickets = await ticketRepository.ListerParFileAsync(fileAgentGate.Id, ct).ConfigureAwait(false);
        return [.. tickets
            .Where(t => t.Statut == StatutTicket.EnAttenteDePriseEnCharge || t.Statut == StatutTicket.EnCours || t.Statut == StatutTicket.EnAttenteClient)
            .OrderBy(t => t.DateArriveeEnFileUtc)];
    }
}
