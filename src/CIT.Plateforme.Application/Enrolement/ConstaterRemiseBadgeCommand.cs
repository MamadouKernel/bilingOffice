using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Enrolement;

/// <summary>
/// Contrat C8-08 — Constater la remise du badge et clôturer. CDC : ENR-03.
/// <para>
/// research.md D6/CLAR-ENR-05 : le badge reste exclusivement un événement de remise physique
/// constaté par l'Agent Gate — aucune référence, numéro, ni technologie (QR/RFID/NFC/API) n'est
/// modélisée. Réutilise <see cref="Ticket.Cloturer"/> (générique, garde déjà existante contre la
/// double clôture) ; seul le libellé d'historique distingue cet événement d'une clôture générique
/// (mirroring <c>CloturerReclamationCommand</c>).
/// </para>
/// </summary>
public sealed class ConstaterRemiseBadgeCommand(
    ITicketRepository tickets,
    IBadgeEnrolementRepository badges,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    /// <summary>
    /// Décision du 03/10/2026 : la remise enregistre le NUMÉRO du badge et sa DATE D'EXPIRATION
    /// (traçabilité, révocation, rappel avant échéance sur la vraie date).
    /// </summary>
    public async Task ExecuterAsync(
        Guid agentId, Guid ticketId, string numeroBadge, DateTime dateExpirationUtc, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        if (ticket.Module != "Enrolement")
        {
            throw new InvalidOperationException("Seuls les tickets Enrolement peuvent faire l'objet d'une remise de badge.");
        }

        await autorisation.ExigerAsync(agentId, Permission.EnrolementInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false);

        // Le badge est validé AVANT la clôture : un numéro manquant ou une date passée ne doit
        // jamais laisser un dossier clôturé sans badge enregistré.
        var badge = Domain.Enrolement.BadgeEnrolement.Remettre(ticket.Id, numeroBadge, dateExpirationUtc, agentId, horloge.MaintenantUtc);

        var statutAvant = ticket.Statut;
        ticket.Cloturer(MotifClotureType.Normale, horloge.MaintenantUtc);
        await badges.AjouterAsync(badge, ct).ConfigureAwait(false);

        var detail = $"Badge n° {badge.Numero}, expire le {badge.DateExpirationUtc:dd/MM/yyyy}";
        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "BadgeRemisEtDossierCloture", detail, agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "BadgeRemisEtDossierCloture", nameof(Ticket), ticket.Id, statutAvant.ToString(), detail, ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("BadgeRemisEtDossierCloture", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
