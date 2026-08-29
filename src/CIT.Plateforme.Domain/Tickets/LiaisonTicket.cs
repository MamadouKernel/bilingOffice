namespace CIT.Plateforme.Domain.Tickets;

/// <summary>
/// SOC-10 : liaison entre tickets (y compris entre modules), créée par un agent, visible du
/// client concerné. Distincte du dossier (SOC-11) : une liaison n'implique pas un regroupement
/// sous un même numéro, juste une association de continuité d'un même dossier métier.
/// </summary>
public sealed class LiaisonTicket
{
    public Guid Id { get; private set; }

    public Guid TicketId { get; private set; }

    public Guid TicketLieId { get; private set; }

    public Guid CreeParId { get; private set; }

    public DateTime DateCreationUtc { get; private set; }

    private LiaisonTicket() { }

    public static LiaisonTicket Creer(Guid ticketId, Guid ticketLieId, Guid creeParId, DateTime maintenantUtc)
    {
        if (ticketId == ticketLieId)
        {
            throw new ArgumentException("Un ticket ne peut pas être lié à lui-même.");
        }

        return new LiaisonTicket
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            TicketLieId = ticketLieId,
            CreeParId = creeParId,
            DateCreationUtc = maintenantUtc,
        };
    }
}
