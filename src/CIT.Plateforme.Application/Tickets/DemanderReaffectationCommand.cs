using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

/// <summary>
/// « Qui a pris en charge, traite » : un agent qui doit reprendre un ticket pris en charge par un
/// collègue (absence, urgence) ne le traite pas en direct — il demande sa réaffectation. Les
/// superviseurs de la file et l'agent en charge sont notifiés ; la réaffectation reste leur décision.
/// </summary>
public sealed class DemanderReaffectationCommand(
    ITicketRepository tickets,
    IHabilitationRepository habilitations,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid agentId, Guid ticketId, string? motif, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        // Seul un agent habilité sur la file du ticket (ou sur toutes les files) peut la demander.
        var droits = await habilitations.ListerParUtilisateurAsync(agentId, ct).ConfigureAwait(false);
        if (!droits.Any(h => h.FileId is null || h.FileId == ticket.FileCouranteId))
        {
            throw new AccesRefuseException($"L'utilisateur {agentId} n'est pas habilité sur la file du ticket {ticket.Numero}.");
        }

        if (ticket.OperateurAssigneId is not Guid affecte || affecte == agentId)
        {
            throw new InvalidOperationException($"Le ticket {ticket.Numero} n'est pas pris en charge par un autre agent.");
        }

        var detail = string.IsNullOrWhiteSpace(motif) ? null : motif.Trim();
        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "ReaffectationDemandee", detail, agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "ReaffectationDemandee", nameof(Ticket), ticket.Id, affecte.ToString(), detail, ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("ReaffectationDemandee", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
