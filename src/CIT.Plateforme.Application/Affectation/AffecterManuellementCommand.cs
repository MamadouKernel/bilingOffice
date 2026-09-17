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

        // Même garde que la réaffectation (TRF-04, audit) : l'opérateur cible doit lui-même être
        // habilité sur la file, faute de quoi il ne verrait jamais le ticket dans son périmètre.
        var cibleHabilitee = await autorisation.EstAutoriseAsync(operateurCibleId, Permission.TicketPrendreEnCharge, ticket.FileCouranteId, ct).ConfigureAwait(false);
        if (!cibleHabilitee)
        {
            throw new AccesRefuseException($"L'opérateur cible {operateurCibleId} n'est pas habilité sur la file {ticket.FileCouranteId}.");
        }

        ticket.AffecterManuellement(operateurCibleId, horloge.MaintenantUtc);

        await journalisation.ConsignerHistoriqueAsync(
            ticket.Id, "TicketAffecteManuellement", $"operateur={operateurCibleId}", initiateurId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(
            initiateurId, "TicketAffecteManuellement", nameof(Ticket), ticket.Id, null, operateurCibleId.ToString(), ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
