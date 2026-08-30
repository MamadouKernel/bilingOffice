using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingImport;

/// <summary>
/// REMÉDIATION AUDIT (post-implementation-audit.md §3, contrôle exhaustif des lectures) : la page
/// <c>FileIes.razor</c> injectait <c>ISuiviBrancheIesRepository</c> directement pour afficher l'état
/// des deux branches — même schéma d'autorisation que <see cref="ConsulterFileBillingImportQuery"/>,
/// exigé ici aussi (au moins une des deux permissions de branche IES, scopée à la file du ticket).
/// </summary>
public sealed class ConsulterSuivisBranchesIesQuery(ITicketRepository tickets, ISuiviBrancheIesRepository suivisBranchesIes, IAutorisationService autorisation)
{
    public async Task<IReadOnlyList<SuiviBrancheIes>> ExecuterAsync(Guid agentId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        var autoriseBrancheManifeste = await autorisation.EstAutoriseAsync(
            agentId, Permission.BillingImportIesManifesteInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false);
        var autoriseBrancheControleDocumentaire = await autorisation.EstAutoriseAsync(
            agentId, Permission.BillingImportIesControleDocumentaireInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false);
        if (!autoriseBrancheManifeste && !autoriseBrancheControleDocumentaire)
        {
            throw new AccesRefuseException(
                $"L'utilisateur {agentId} ne détient aucune des permissions de branche IES requises pour consulter le ticket {ticketId}.");
        }

        return await suivisBranchesIes.ListerParTicketAsync(ticketId, ct).ConfigureAwait(false);
    }
}
