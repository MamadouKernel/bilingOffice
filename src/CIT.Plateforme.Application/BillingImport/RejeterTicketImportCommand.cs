using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingImport;

/// <summary>
/// Contrat C3-04 — Rejeter un ticket au contrôle BL, motif obligatoire. CDC : IMP-04.
/// research.md D13/D13bis : réutilise <see cref="Ticket.RejeterImport"/> (statut générique
/// <c>Annule</c>, jamais <c>Cloture</c>) ; le motif est tracé via l'historique/l'audit, distinct de
/// <see cref="Ticket.TypeAnnulationImport"/> qui préserve la distinction avec une annulation client.
/// </summary>
public sealed class RejeterTicketImportCommand(
    ITicketRepository tickets,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid agentId, Guid ticketId, string motif, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(motif))
        {
            throw new ArgumentException("Le motif de rejet est obligatoire (IMP-04).", nameof(motif));
        }

        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        await autorisation.ExigerAsync(agentId, Permission.BillingImportManifesteInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false);

        var maintenant = horloge.MaintenantUtc;
        ticket.RejeterImport(maintenant);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "TicketImportRejete", motif, agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "TicketImportRejete", nameof(Ticket), ticket.Id, null, motif, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
