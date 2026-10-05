using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingExport;

/// <summary>
/// Contrat C5-04 — Demander un complément (Back Office) et le déposer (client). CDC : EXP-02.
/// </summary>
public sealed class GestionComplementExportCommand(
    ITicketRepository tickets,
    Application.Securite.CloisonnementService cloisonnement,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task DemanderComplementAsync(Guid agentId, Guid ticketId, string elementsAttendus, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        await autorisation.ExigerAsync(agentId, Permission.BillingExportBackOfficeInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false);

        var maintenant = horloge.MaintenantUtc;
        if (ticket.Statut == StatutTicket.EnAttenteDePriseEnCharge)
        {
            ticket.PrendreEnCharge(agentId, maintenant);
        }

        ticket.MettreEnAttenteClient(maintenant);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "ComplementDemandeExport", elementsAttendus, agentId, ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("ComplementDemande", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "ComplementDemandeExport", nameof(Ticket), ticket.Id, null, elementsAttendus, ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    public async Task DeposerComplementAsync(Guid clientId, Guid ticketId, CancellationToken ct = default)
    {
        await cloisonnement.ExigerProprieteClientAsync(clientId, ticketId, ct).ConfigureAwait(false);

        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        var maintenant = horloge.MaintenantUtc;
        ticket.RependreApresReponseClient(maintenant);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "ComplementDeposeExport", null, clientId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(clientId, "ComplementDeposeExport", nameof(Ticket), ticket.Id, null, null, ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
