using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

/// <summary>
/// Option B (décision du 05/10/2026) — prise en charge explicite d'un ticket Billing sur l'étape
/// courante, préalable à tout traitement. Contrairement à <see cref="PrendreEnChargeCommand"/> (qui
/// mappe une permission fixe par module), l'habilitation dépend ici de la file : l'agent doit
/// détenir AU MOINS UNE des permissions de l'étape, évaluées sur <see cref="Ticket.FileCouranteId"/>
/// — exactement la même règle que <c>ConsulterFileBillingImportQuery</c> pour afficher la file.
/// Une seule commande couvre import et export : elle ne connaît que le ticket et ses permissions.
/// </summary>
public sealed class PrendreEnChargeEtapeBillingCommand(
    ITicketRepository tickets,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(
        Guid agentId,
        Guid ticketId,
        IReadOnlyList<string> permissionsEtape,
        CancellationToken ct = default)
    {
        if (permissionsEtape.Count == 0)
        {
            throw new ArgumentException("Au moins une permission d'étape doit être fournie.", nameof(permissionsEtape));
        }

        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        var autorise = false;
        foreach (var permission in permissionsEtape)
        {
            if (await autorisation.EstAutoriseAsync(agentId, permission, ticket.FileCouranteId, ct).ConfigureAwait(false))
            {
                autorise = true;
                break;
            }
        }
        if (!autorise)
        {
            throw new AccesRefuseException(
                $"L'utilisateur {agentId} ne détient aucune des permissions requises pour prendre en charge le ticket {ticket.Numero}.");
        }

        ticket.PrendreEnCharge(agentId, horloge.MaintenantUtc);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "TicketPrisEnCharge", $"operateur={agentId}", agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "TicketPrisEnCharge", nameof(Ticket), ticket.Id, StatutTicket.EnAttenteDePriseEnCharge.ToString(), StatutTicket.EnCours.ToString(), ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("TicketPrisEnCharge", $"{{\"ticketId\":\"{ticket.Id}\",\"operateurId\":\"{agentId}\"}}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
