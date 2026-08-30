using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingExport;

/// <summary>
/// Contrat C5-03 (branche rejet) — Rejet d'un ticket Billing EXPORT par le Back Office (EXP-02, research.md D5bis).
/// </summary>
public sealed class RejeterTicketExportCommand(
    ITicketRepository tickets,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid agentId, Guid ticketId, string motifRejet, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(motifRejet))
        {
            throw new ArgumentException("Le motif de rejet est obligatoire (EXP-02).", nameof(motifRejet));
        }

        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        await autorisation.ExigerAsync(agentId, Permission.BillingExportBackOfficeInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false);

        ticket.RejeterExport(horloge.MaintenantUtc);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "RejetBackOfficeExport", motifRejet, agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "RejetBackOfficeExport", nameof(Ticket), ticket.Id, null, motifRejet, ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
