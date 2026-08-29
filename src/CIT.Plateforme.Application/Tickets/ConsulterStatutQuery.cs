using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

/// <summary>SOC-08 : statut visible par le client en temps réel. Le cloisonnement (SEC-01) est
/// vérifié avant toute lecture.</summary>
public sealed record StatutTicketVue(string Numero, StatutTicket Statut, DateTime DateDerniereActiviteUtc);

public sealed class ConsulterStatutQuery(ITicketRepository tickets, CloisonnementService cloisonnement)
{
    public async Task<StatutTicketVue> ExecuterAsync(Guid clientId, Guid ticketId, CancellationToken ct = default)
    {
        await cloisonnement.ExigerProprieteClientAsync(clientId, ticketId, ct).ConfigureAwait(false);

        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        return new StatutTicketVue(ticket.Numero, ticket.Statut, ticket.DateDerniereActiviteUtc);
    }
}
