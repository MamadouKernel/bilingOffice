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
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid agentId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        if (ticket.Module != "Enrolement")
        {
            throw new InvalidOperationException("Seuls les tickets Enrolement peuvent faire l'objet d'une remise de badge.");
        }

        await autorisation.ExigerAsync(agentId, Permission.EnrolementInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false);

        var statutAvant = ticket.Statut;
        ticket.Cloturer(MotifClotureType.Normale, horloge.MaintenantUtc);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "BadgeRemisEtDossierCloture", null, agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "BadgeRemisEtDossierCloture", nameof(Ticket), ticket.Id, statutAvant.ToString(), ticket.Statut.ToString(), ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("BadgeRemisEtDossierCloture", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
