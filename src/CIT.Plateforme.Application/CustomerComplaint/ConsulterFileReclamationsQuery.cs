using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.CustomerComplaint;

public sealed class ConsulterFileReclamationsQuery(
    ITicketRepository ticketRepository,
    IFileDeTraitementRepository fileRepository,
    IAutorisationService autorisationService)
{
    public async Task<IReadOnlyList<Ticket>> ExecuterAsync(Guid agentId, CancellationToken ct = default)
    {
        var files = await fileRepository.ListerAsync(ct).ConfigureAwait(false);
        var fileSupport = files.FirstOrDefault(f => f.Nom == "Support Opérationnel");
        if (fileSupport is null)
        {
            throw new InvalidOperationException("La file « Support Opérationnel » n'est pas configurée.");
        }

        var peutStandard = await autorisationService.EstAutoriseAsync(agentId, Permission.TicketPrendreEnCharge, fileSupport.Id, ct).ConfigureAwait(false);
        var peutComplaint = await autorisationService.EstAutoriseAsync(agentId, Permission.ComplaintInstruire, fileSupport.Id, ct).ConfigureAwait(false);

        if (!peutStandard && !peutComplaint)
        {
            throw new AccesRefuseException($"L'agent {agentId} n'a pas l'autorisation d'accéder à la file Support Opérationnel.");
        }

        var tickets = await ticketRepository.ListerParFileAsync(fileSupport.Id, ct).ConfigureAwait(false);
        return tickets
            .Where(t => t.Statut == StatutTicket.EnAttenteDePriseEnCharge || t.Statut == StatutTicket.EnCours)
            .OrderBy(t => t.DateArriveeEnFileUtc)
            .ToList();
    }
}
