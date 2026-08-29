using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Affectation;

/// <summary>AFF-02 (affectation manuelle superviseur, Must autonome) et PEC-05 (affectation
/// directe, Should) — même mécanique applicative : un tiers habilité affecte un ticket non pris
/// en charge à un opérateur précis.</summary>
public sealed class AffecterManuellementCommand(
    ITicketRepository tickets,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid initiateurId, Guid ticketId, Guid operateurCibleId, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        await autorisation.ExigerAsync(initiateurId, Permission.TicketAffecterManuellement, ticket.FileCouranteId, ct).ConfigureAwait(false);

        ticket.AffecterManuellement(operateurCibleId, horloge.MaintenantUtc);

        await journalisation.ConsignerHistoriqueAsync(
            ticket.Id, "TicketAffecteManuellement", $"operateur={operateurCibleId}", initiateurId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(
            initiateurId, "TicketAffecteManuellement", nameof(Ticket), ticket.Id, null, operateurCibleId.ToString(), ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
