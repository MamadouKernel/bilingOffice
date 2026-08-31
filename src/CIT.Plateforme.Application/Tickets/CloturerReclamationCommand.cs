using System;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

public record CloturerReclamationRequete(Guid AgentId, Guid TicketId, string ReponseFormalisee);

/// <summary>
/// Contrat C7-02 — Clôturer une réclamation avec réponse formalisée.
/// </summary>
public sealed class CloturerReclamationCommand(
    ITicketRepository tickets,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(CloturerReclamationRequete requete, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(requete.TicketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {requete.TicketId} introuvable.");

        if (ticket.Module != "CustomerComplaint")
        {
            throw new InvalidOperationException("Seuls les tickets CustomerComplaint peuvent être clôturés via ce contrat.");
        }

        await autorisation.ExigerAsync(requete.AgentId, Permission.ComplaintInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false);

        var statutAvant = ticket.Statut;
        ticket.CloturerAvecReponseFormalisee(requete.ReponseFormalisee, MotifClotureType.Normale, horloge.MaintenantUtc);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "ReclamationCloturee", null, requete.AgentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(requete.AgentId, "ReclamationCloturee", nameof(Ticket), ticket.Id, statutAvant.ToString(), ticket.Statut.ToString(), ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("TicketReclamationCloture", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
