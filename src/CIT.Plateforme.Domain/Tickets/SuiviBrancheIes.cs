namespace CIT.Plateforme.Domain.Tickets;

/// <summary>
/// FEATURE 003 (research.md D8, IMP-40/IMP-41) : suivi de la progression indépendante d'une des
/// deux branches (Manifeste, Contrôle Documentaire) d'un ticket Client IES — deux enregistrements
/// par ticket IES. Entité satellite autonome, sans relation EF configurée vers <see cref="Ticket"/>
/// (même style que <c>EntreeHistorique</c>/<c>PeriodeAttenteClient</c>). La jointure (D9) est une
/// condition **dérivée** (les deux lignes <see cref="EstTerminee"/>), jamais un état persisté
/// séparément.
/// </summary>
public sealed class SuiviBrancheIes
{
    public Guid Id { get; private set; }

    public Guid TicketId { get; private set; }

    public BrancheIes Branche { get; private set; }

    public bool EstTerminee { get; private set; }

    public Guid? AuteurId { get; private set; }

    public DateTime? DateAchevementUtc { get; private set; }

    private SuiviBrancheIes() { }

    public static SuiviBrancheIes Creer(Guid ticketId, BrancheIes branche) => new()
    {
        Id = Guid.NewGuid(),
        TicketId = ticketId,
        Branche = branche,
        EstTerminee = false,
    };

    public void Achever(Guid auteurId, DateTime maintenantUtc)
    {
        if (EstTerminee)
        {
            throw new InvalidOperationException($"La branche {Branche} du ticket {TicketId} est déjà achevée.");
        }

        EstTerminee = true;
        AuteurId = auteurId;
        DateAchevementUtc = maintenantUtc;
    }
}
