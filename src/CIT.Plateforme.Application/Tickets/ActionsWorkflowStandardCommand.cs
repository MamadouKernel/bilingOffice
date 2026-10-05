using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

/// <summary>SOC-06 : actions de workflow standard — valider, rejeter avec motif obligatoire,
/// demander un complément au client. Le contrôle métier fin de ce qui est « valide » pour un
/// module donné appartient au module appelant ; le socle ne fait qu'enregistrer la décision et la
/// notifier (émission d'événement, hors périmètre des canaux eux-mêmes).</summary>
public sealed class ActionsWorkflowStandardCommand(
    ITicketRepository tickets,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ValiderAsync(Guid agentId, Guid ticketId, CancellationToken ct = default) =>
        await EnregistrerDecisionAsync(agentId, ticketId, "TicketValide", detail: null, ct).ConfigureAwait(false);

    public async Task RejeterAsync(Guid agentId, Guid ticketId, string motif, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(motif))
        {
            throw new ArgumentException("Le motif de rejet est obligatoire (SOC-06).", nameof(motif));
        }

        await EnregistrerDecisionAsync(agentId, ticketId, "TicketRejete", motif, ct).ConfigureAwait(false);
    }

    public async Task DemanderComplementAsync(Guid agentId, Guid ticketId, string motif, CancellationToken ct = default) =>
        await EnregistrerDecisionAsync(agentId, ticketId, "ComplementDemande", motif, ct).ConfigureAwait(false);

    private async Task EnregistrerDecisionAsync(Guid agentId, Guid ticketId, string evenement, string? detail, CancellationToken ct)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        await autorisation.ExigerAsync(agentId, Permission.TicketPrendreEnCharge, ticket.FileCouranteId, ct).ConfigureAwait(false);
        await global::CIT.Plateforme.Application.Securite.GardeAgentAffecte.ExigerAsync(autorisation, agentId, ticket, ct).ConfigureAwait(false);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, evenement, detail, agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, evenement, nameof(Ticket), ticket.Id, null, detail, ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync(evenement, $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
