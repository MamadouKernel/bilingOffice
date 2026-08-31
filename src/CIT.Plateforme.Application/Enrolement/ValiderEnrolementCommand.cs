using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Enrolement;

/// <summary>
/// Contrat C8-05 — Valider un dossier d'enrôlement. CDC : ENR-02.
/// <para>
/// research.md D5/D6 : la validation n'est qu'un jalon d'historique — le <see cref="StatutTicket"/>
/// reste <c>EnCours</c> (pas de nouvel état, la clôture n'intervient qu'à la remise du badge,
/// <see cref="ConstaterRemiseBadgeCommand"/>). Réutilise <see cref="Permission.EnrolementInstruire"/>
/// plutôt que le générique <c>ActionsWorkflowStandardCommand</c> (research.md D5 : ce dernier n'est
/// pas modulé par module).
/// </para>
/// </summary>
public sealed class ValiderEnrolementCommand(
    ITicketRepository tickets,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid agentId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        if (ticket.Module != "Enrolement")
        {
            throw new InvalidOperationException("Seuls les tickets Enrolement peuvent être validés via ce contrat.");
        }

        await autorisation.ExigerAsync(agentId, Permission.EnrolementInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "EnrolementValide", null, agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "EnrolementValide", nameof(Ticket), ticket.Id, null, null, ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("EnrolementValide", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
