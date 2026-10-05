using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Securite;

/// <summary>Levée quand un agent agit sur un ticket pris en charge par un collègue.</summary>
public sealed class TicketAffecteAUnAutreAgentException(string numeroTicket)
    : InvalidOperationException($"Le ticket {numeroTicket} est pris en charge par un autre agent : demandez sa réaffectation au superviseur.");

/// <summary>
/// « Qui a pris en charge, traite » (décision du 05/10/2026) : deux agents ne valident, ne facturent
/// ni n'encaissent le même BL. Un ticket affecté n'est traité que par son agent — ou par un superviseur
/// (habilité à réaffecter), qui peut toujours reprendre la main. Un ticket non affecté reste ouvert à
/// tout agent habilité sur la file, comme avant.
/// </summary>
public static class GardeAgentAffecte
{
    public static async Task ExigerAsync(IAutorisationService autorisation, Guid agentId, Ticket ticket, CancellationToken ct = default)
    {
        if (ticket.OperateurAssigneId is not Guid affecte || affecte == agentId)
        {
            return;
        }

        if (await autorisation.EstAutoriseAsync(agentId, Permission.TicketReaffecter, ticket.FileCouranteId, ct).ConfigureAwait(false))
        {
            return;
        }

        throw new TicketAffecteAUnAutreAgentException(ticket.Numero);
    }
}
