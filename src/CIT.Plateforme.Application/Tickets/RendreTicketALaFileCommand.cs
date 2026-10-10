using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

/// <summary>
/// Passation de chiffre (relève d'équipe) : l'agent qui détient un ticket et ne peut pas le finir
/// avant la fin de son poste le <b>rend à sa file</b>. Le ticket repart « en attente de prise en
/// charge » sur la même file ; l'agent entrant disponible le reprend en prise libre (ou la
/// distribution automatique le ré-attribue). On ne désigne personne nommément — c'est voulu : le
/// remplaçant varie selon les tours (3×8 / 2×12), et « qui prend en charge, traite » reste vrai
/// pour celui qui le reprend.
///
/// Seul l'agent assigné (ou un superviseur habilité à réaffecter) peut rendre le ticket : le même
/// garde que les actions métier réservées à l'opérateur affecté (<see cref="GardeAgentAffecte"/>).
/// </summary>
public sealed class RendreTicketALaFileCommand(
    ITicketRepository tickets,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid operateurId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        if (ticket.OperateurAssigneId is null)
        {
            throw new InvalidOperationException(
                $"Le ticket {ticket.Numero} n'est pris en charge par personne : il est déjà disponible dans la file.");
        }

        // Seul le détenteur du ticket (ou un superviseur) peut le rendre à la file.
        await GardeAgentAffecte.ExigerAsync(autorisation, operateurId, ticket, ct).ConfigureAwait(false);

        ticket.RemettreEnDistribution(horloge.MaintenantUtc);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "TicketRenduALaFile", $"operateur={operateurId}", operateurId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(operateurId, "TicketRenduALaFile", nameof(Ticket), ticket.Id,
            StatutTicket.EnCours.ToString(), StatutTicket.EnAttenteDePriseEnCharge.ToString(), ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("TicketRenduALaFile", $"{{\"ticketId\":\"{ticket.Id}\",\"operateurId\":\"{operateurId}\"}}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
