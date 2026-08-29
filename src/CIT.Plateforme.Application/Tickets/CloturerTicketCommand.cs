using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

/// <summary>Contrat C-18 — Clôturer un ticket. CDC : SOC-09 (archivage automatique — représenté
/// par la simple présence de DateClotureUtc, jamais de suppression).</summary>
public sealed class CloturerTicketCommand(
    ITicketRepository tickets,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid agentId, Guid ticketId, CancellationToken ct = default) =>
        await ClorePourMotifAsync(agentId, ticketId, MotifClotureType.Normale, ct).ConfigureAwait(false);

    /// <summary>C-19 : clôture pour absence de réponse — habilitation paramétrable (PC-20), après
    /// relances (CYC-04). Le socle ne décide pas quel rôle est habilité : il vérifie seulement la
    /// permission accordée (paramétrable) au moment de l'appel.</summary>
    public async Task ClorePourAbsenceReponseAsync(Guid agentId, Guid ticketId, CancellationToken ct = default) =>
        await ClorePourMotifAsync(agentId, ticketId, MotifClotureType.AbsenceReponse, ct).ConfigureAwait(false);

    private async Task ClorePourMotifAsync(Guid agentId, Guid ticketId, MotifClotureType motif, CancellationToken ct)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        var permission = motif == MotifClotureType.AbsenceReponse
            ? Permission.TicketClotureAbsenceReponse
            : Permission.TicketCloturer;
        await autorisation.ExigerAsync(agentId, permission, ticket.FileCouranteId, ct).ConfigureAwait(false);

        ticket.Cloturer(motif, horloge.MaintenantUtc);

        // SOC-09 : archivage automatique — le ticket reste consultable en permanence (aucune
        // suppression, aucun déplacement d'entité, seule la date de clôture est renseignée).
        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "TicketCloture", motif.ToString(), agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "TicketCloture", nameof(Ticket), ticket.Id, null, motif.ToString(), ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("TicketCloture", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
