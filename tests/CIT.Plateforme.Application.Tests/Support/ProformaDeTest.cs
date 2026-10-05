using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;

namespace CIT.Plateforme.Application.Tests.Support;

/// <summary>
/// Parcours TO BE (05/10/2026) : la facture n'est éditée qu'après validation de la proforma par le
/// client. Les tests de circuit qui ne portent PAS sur la proforma l'amènent directement à l'état
/// « validée » ; GestionProformaCommandTests couvre la commande elle-même.
/// </summary>
public static class ProformaDeTest
{
    public static async Task ValiderAsync(PlateformeDbContext dbContext, Guid ticketId, Guid agentId)
    {
        var ticket = await dbContext.Tickets.FindAsync(ticketId) ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");
        var maintenant = DateTime.UtcNow;
        if (ticket.Statut == StatutTicket.EnAttenteDePriseEnCharge)
        {
            ticket.PrendreEnCharge(agentId, maintenant);
        }

        ticket.EmettreProforma(maintenant);
        ticket.ValiderProforma(maintenant);
        await dbContext.SaveChangesAsync();
    }
}
