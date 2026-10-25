using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;

namespace CIT.Plateforme.Application.Tests.Support;

/// <summary>
/// Option B (décision du 05/10/2026) : chaque étape Billing exige désormais une prise en charge
/// explicite — le ticket doit être <see cref="StatutTicket.EnCours"/> ET affecté à l'agent avant
/// toute action de traitement. Les tests de circuit amènent le ticket à l'étape voulue puis
/// appellent cette aide avant l'action agent, exactement comme l'écran de file le ferait via le
/// bouton « Prendre en charge ». Tolérant : sans effet si l'agent tient déjà le ticket, réaffecte
/// s'il est tenu par un autre — pour pouvoir l'appeler avant n'importe quelle étape sans précaution.
/// </summary>
public static class PriseEnChargeDeTest
{
    public static async Task PrendreAsync(PlateformeDbContext dbContext, Guid ticketId, Guid agentId)
    {
        var ticket = await dbContext.Tickets.FindAsync(ticketId)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");
        var maintenant = DateTime.UtcNow;
        if (ticket.Statut == StatutTicket.EnAttenteDePriseEnCharge)
        {
            ticket.PrendreEnCharge(agentId, maintenant);
        }
        else if (ticket.Statut == StatutTicket.EnCours && ticket.OperateurAssigneId != agentId)
        {
            ticket.Reaffecter(agentId, maintenant);
        }

        await dbContext.SaveChangesAsync();
    }
}
