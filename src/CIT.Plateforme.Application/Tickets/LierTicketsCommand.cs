using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

/// <summary>SOC-10 : liaison entre tickets (y compris entre modules), créée par un agent
/// habilité, visible par les agents et par le client concerné.</summary>
public sealed class LierTicketsCommand(
    ILiaisonTicketRepository liaisons,
    ITicketRepository tickets,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid agentId, Guid ticketId, Guid ticketLieId, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");
        _ = await tickets.ObtenirParIdAsync(ticketLieId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketLieId} introuvable.");

        await autorisation.ExigerAsync(agentId, Permission.TicketPrendreEnCharge, ticket.FileCouranteId, ct).ConfigureAwait(false);

        var liaison = LiaisonTicket.Creer(ticketId, ticketLieId, agentId, horloge.MaintenantUtc);
        await liaisons.AjouterAsync(liaison, ct).ConfigureAwait(false);

        await journalisation.ConsignerHistoriqueAsync(ticketId, "TicketLie", $"lieA={ticketLieId}", agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "TicketLie", nameof(LiaisonTicket), liaison.Id, null, $"{ticketId}<->{ticketLieId}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
