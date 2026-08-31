using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

/// <summary>
/// Contrat C-14 — Prendre en charge un ticket. CDC : PEC-01..04, SOC-12.
/// Sécurité applicative de la concurrence assurée par le jeton de concurrence EF Core
/// (règle métier critique « prise en charge concurrente », voir Infrastructure.Tests).
/// </summary>
public sealed class PrendreEnChargeCommand(
    ITicketRepository tickets,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid operateurId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        if (ticket.Module == "CustomerComplaint")
        {
            await autorisation.ExigerAsync(operateurId, Permission.ComplaintInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false);
        }
        else if (ticket.Module == "Enrolement")
        {
            await autorisation.ExigerAsync(operateurId, Permission.EnrolementInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false);
        }
        else
        {
            await autorisation.ExigerAsync(operateurId, Permission.TicketPrendreEnCharge, ticket.FileCouranteId, ct).ConfigureAwait(false);
        }

        ticket.PrendreEnCharge(operateurId, horloge.MaintenantUtc);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "TicketPrisEnCharge", $"operateur={operateurId}", operateurId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(operateurId, "TicketPrisEnCharge", nameof(Ticket), ticket.Id, StatutTicket.EnAttenteDePriseEnCharge.ToString(), StatutTicket.EnCours.ToString(), ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("TicketPrisEnCharge", $"{{\"ticketId\":\"{ticket.Id}\",\"operateurId\":\"{operateurId}\"}}", ct).ConfigureAwait(false);

        // C'est ici que la concurrence réelle est arbitrée : si un autre opérateur a déjà
        // enregistré une modification sur ce même ticket entre le chargement et cet appel,
        // EnregistrerAsync lève ConflitDeConcurrenceException (voir UniteDeTravail).
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
