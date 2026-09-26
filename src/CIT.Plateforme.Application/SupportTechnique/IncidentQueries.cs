using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.SupportTechnique;

/// <summary>
/// File d'instruction du Support IT — incidents en attente ou en cours, sur les DEUX files du
/// module (N1 et N2, cette dernière recevant les incidents escaladés). L'accès est autorisé au
/// détenteur de <see cref="Permission.SupportIncidentInstruire"/> (ou du droit standard de prise en
/// charge) sur l'une de ces files ; un incident escaladé reste ainsi visible dans le même plan de
/// travail. Miroir de <c>ConsulterFileReclamationsQuery</c>.
/// </summary>
public sealed class ConsulterFileIncidentsQuery(
    ITicketRepository ticketRepository,
    IFileDeTraitementRepository fileRepository,
    IAutorisationService autorisationService)
{
    public async Task<IReadOnlyList<Ticket>> ExecuterAsync(Guid agentId, CancellationToken ct = default)
    {
        var filesSupport = (await fileRepository.ListerAsync(ct).ConfigureAwait(false))
            .Where(f => f.Module == SupportItConstantes.Module)
            .ToList();

        if (filesSupport.Count == 0)
        {
            throw new InvalidOperationException("Aucune file du module Support IT n'est configurée.");
        }

        if (!await PeutAccederAsync(agentId, filesSupport.Select(f => f.Id), ct).ConfigureAwait(false))
        {
            throw new AccesRefuseException($"L'agent {agentId} n'a pas l'autorisation d'accéder aux files Support IT.");
        }

        var incidents = new List<Ticket>();
        foreach (var file in filesSupport)
        {
            incidents.AddRange(await ticketRepository.ListerParFileAsync(file.Id, ct).ConfigureAwait(false));
        }

        return incidents
            .Where(t => t.Statut == StatutTicket.EnAttenteDePriseEnCharge || t.Statut == StatutTicket.EnCours)
            .OrderBy(t => t.DateArriveeEnFileUtc)
            .ToList();
    }

    private async Task<bool> PeutAccederAsync(Guid agentId, IEnumerable<Guid> fileIds, CancellationToken ct)
    {
        foreach (var fileId in fileIds)
        {
            if (await autorisationService.EstAutoriseAsync(agentId, Permission.SupportIncidentInstruire, fileId, ct).ConfigureAwait(false)
                || await autorisationService.EstAutoriseAsync(agentId, Permission.TicketPrendreEnCharge, fileId, ct).ConfigureAwait(false))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// Incidents techniques déclarés par l'appelant. L'incident appartient à son déclarant (client =
/// auteur = appelant à la création), le suivi filtre donc les dossiers dont il est client puis les
/// tickets du module Support IT.
/// </summary>
public sealed class MesIncidentsQuery(
    ITicketRepository ticketRepository,
    IDossierRepository dossierRepository)
{
    public async Task<IReadOnlyList<Ticket>> ExecuterAsync(Guid appelantId, CancellationToken ct = default)
    {
        var dossiers = await dossierRepository.ListerParClientAsync(appelantId, ct).ConfigureAwait(false);
        if (dossiers.Count == 0)
        {
            return [];
        }

        var tickets = await ticketRepository.ListerParDossiersAsync(dossiers.Select(d => d.Id), ct).ConfigureAwait(false);
        return tickets
            .Where(t => t.Module == SupportItConstantes.Module)
            .OrderByDescending(t => t.DateArriveeEnFileUtc)
            .ToList();
    }
}
