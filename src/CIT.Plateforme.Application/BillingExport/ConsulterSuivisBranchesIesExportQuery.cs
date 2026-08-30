using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingExport;

/// <summary>
/// Consultation autorisée des suivis de branches IES Export pour un ticket.
/// </summary>
public sealed class ConsulterSuivisBranchesIesExportQuery(
    ITicketRepository tickets,
    ISuiviBrancheIesRepository suivisBranchesIes,
    IAutorisationService autorisation)
{
    public async Task<IReadOnlyList<SuiviBrancheIes>> ExecuterAsync(Guid agentId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        var autoriseBO = await autorisation.EstAutoriseAsync(
            agentId, Permission.BillingExportIesBackOfficeInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false);
        var autoriseDoc = await autorisation.EstAutoriseAsync(
            agentId, Permission.BillingExportIesControleDocumentaireInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false);

        if (!autoriseBO && !autoriseDoc)
        {
            throw new AccesRefuseException(
                $"L'utilisateur {agentId} ne détient aucune des permissions de branche IES requises pour consulter le ticket {ticketId}.");
        }

        return await suivisBranchesIes.ListerParTicketAsync(ticketId, ct).ConfigureAwait(false);
    }
}
