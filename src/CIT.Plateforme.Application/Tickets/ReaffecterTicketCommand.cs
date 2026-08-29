using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

/// <summary>Contrat C-16 — Réaffecter un ticket. CDC : TRF-04 — droit configurable, non réservé
/// au superviseur (la permission peut être attribuée à tout agent habilité).</summary>
public sealed class ReaffecterTicketCommand(
    ITicketRepository tickets,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid initiateurId, Guid ticketId, Guid nouvelOperateurId, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        await autorisation.ExigerAsync(initiateurId, Permission.TicketReaffecter, ticket.FileCouranteId, ct).ConfigureAwait(false);

        var ancienOperateur = ticket.OperateurAssigneId;
        ticket.Reaffecter(nouvelOperateurId, horloge.MaintenantUtc);

        await journalisation.ConsignerHistoriqueAsync(
            ticket.Id, "TicketReaffecte", $"de={ancienOperateur};vers={nouvelOperateurId}", initiateurId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(
            initiateurId, "TicketReaffecte", nameof(Ticket), ticket.Id, ancienOperateur?.ToString(), nouvelOperateurId.ToString(), ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
