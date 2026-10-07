using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Enrolement;

/// <summary>
/// Correction de la date d'expiration du badge (carte transporteur) saisie par erreur à la remise
/// (décision 07/10/2026). Plutôt que de rouvrir tout le dossier — ce qui le renverrait en file et
/// désassignerait l'agent —, on rectifie directement l'échéance du badge déjà remis. La réouverture
/// (<see cref="Ticket.Rouvrir"/>) reste disponible pour un vrai re-traitement.
/// L'action est journalisée (historique + audit) : rien n'est corrigé en silence.
/// </summary>
public sealed class CorrigerExpirationBadgeCommand(
    ITicketRepository tickets,
    IBadgeEnrolementRepository badges,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid agentId, Guid ticketId, DateTime nouvelleExpirationUtc, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        if (ticket.Module != "Enrolement")
        {
            throw new InvalidOperationException("Seuls les tickets Enrolement portent un badge à corriger.");
        }

        await autorisation.ExigerAsync(agentId, Permission.EnrolementInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false);

        var badge = await badges.ObtenirParTicketAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Aucun badge n'a encore été remis pour le ticket {ticket.Numero}.");

        var ancienne = badge.DateExpirationUtc;
        badge.CorrigerExpiration(nouvelleExpirationUtc, horloge.MaintenantUtc);

        var detail = $"Expiration corrigée : {ancienne:dd/MM/yyyy} → {nouvelleExpirationUtc:dd/MM/yyyy}";
        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "ExpirationBadgeCorrigee", detail, agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "ExpirationBadgeCorrigee", nameof(Domain.Enrolement.BadgeEnrolement), badge.Id, ancienne.ToString("o"), detail, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
