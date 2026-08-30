namespace CIT.Plateforme.Domain.Tickets;

/// <summary>Contrat C3-18 — levée lorsqu'une finalisation IES est tentée avant que les deux
/// branches (<see cref="SuiviBrancheIes"/>) ne soient achevées : « tentative de finalisation avant
/// jointure complète → refus serveur systématique ».</summary>
public sealed class JointureIesIncompleteException(Guid ticketId)
    : Exception($"La jointure des deux branches IES n'est pas encore atteinte pour le ticket {ticketId}.")
{
    public Guid TicketId { get; } = ticketId;
}
