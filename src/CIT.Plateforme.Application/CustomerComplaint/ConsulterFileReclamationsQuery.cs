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
        // Plusieurs files peuvent porter ce nom (file héritée d'un ancien module + file du module
        // réclamations) : on agrège toutes celles où l'agent est habilité, pas la première trouvée.
        var files = await fileRepository.ListerAsync(ct).ConfigureAwait(false);
        var filesSupport = files.Where(f => f.Nom == "Support Opérationnel").ToList();
        if (filesSupport.Count == 0)
        {
            throw new InvalidOperationException("La file « Support Opérationnel » n'est pas configurée.");
        }

        var tickets = new List<Ticket>();
        var autoriseSurUneFile = false;
        foreach (var fileSupport in filesSupport)
        {
            var autorise = await autorisationService.EstAutoriseAsync(agentId, Permission.TicketPrendreEnCharge, fileSupport.Id, ct).ConfigureAwait(false)
                || await autorisationService.EstAutoriseAsync(agentId, Permission.ComplaintInstruire, fileSupport.Id, ct).ConfigureAwait(false);
            if (autorise)
            {
                autoriseSurUneFile = true;
                tickets.AddRange(await ticketRepository.ListerParFileAsync(fileSupport.Id, ct).ConfigureAwait(false));
            }
        }

        if (!autoriseSurUneFile)
        {
            throw new AccesRefuseException($"L'agent {agentId} n'a pas l'autorisation d'accéder à la file Support Opérationnel.");
        }

        return tickets
            .Where(t => t.Statut == StatutTicket.EnAttenteDePriseEnCharge || t.Statut == StatutTicket.EnCours)
            .OrderBy(t => t.DateArriveeEnFileUtc)
            .ToList();
    }
}
