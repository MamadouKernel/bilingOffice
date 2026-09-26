using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.SupportTechnique;

/// <summary>
/// File d'instruction du Support IT — tickets d'incident en attente ou en cours dans la file
/// « Support IT ». Miroir de <c>ConsulterFileReclamationsQuery</c> : l'accès est autorisé au
/// détenteur de <see cref="Permission.SupportIncidentInstruire"/> (ou du droit standard de prise en
/// charge borné à cette file).
/// </summary>
public sealed class ConsulterFileIncidentsQuery(
    ITicketRepository ticketRepository,
    IFileDeTraitementRepository fileRepository,
    IAutorisationService autorisationService)
{
    public async Task<IReadOnlyList<Ticket>> ExecuterAsync(Guid agentId, CancellationToken ct = default)
    {
        var files = await fileRepository.ListerAsync(ct).ConfigureAwait(false);
        var fileSupport = files.FirstOrDefault(f => f.Nom == SupportTechnique.NomFileSupportIt && f.Module == SupportTechnique.Module)
            ?? throw new InvalidOperationException("La file « Support IT » n'est pas configurée.");

        var peutStandard = await autorisationService.EstAutoriseAsync(agentId, Permission.TicketPrendreEnCharge, fileSupport.Id, ct).ConfigureAwait(false);
        var peutIncident = await autorisationService.EstAutoriseAsync(agentId, Permission.SupportIncidentInstruire, fileSupport.Id, ct).ConfigureAwait(false);

        if (!peutStandard && !peutIncident)
        {
            throw new AccesRefuseException($"L'agent {agentId} n'a pas l'autorisation d'accéder à la file Support IT.");
        }

        var tickets = await ticketRepository.ListerParFileAsync(fileSupport.Id, ct).ConfigureAwait(false);
        return tickets
            .Where(t => t.Statut == StatutTicket.EnAttenteDePriseEnCharge || t.Statut == StatutTicket.EnCours)
            .OrderBy(t => t.DateArriveeEnFileUtc)
            .ToList();
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
            .Where(t => t.Module == SupportTechnique.Module)
            .OrderByDescending(t => t.DateArriveeEnFileUtc)
            .ToList();
    }
}
