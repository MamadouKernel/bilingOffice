namespace CIT.Plateforme.Domain.Tickets;

/// <summary>
/// REMÉDIATION AUDIT (post-implementation-audit.md §3.B) : une période d'attente client d'un
/// ticket, avec ses bornes exactes. Introduite pour permettre à <c>SurveillerEcheancesUseCase</c>
/// de calculer une durée SLA « ouvrée » cohérente (jamais de soustraction d'une durée brute à une
/// durée ouvrée) : chaque période peut être exclue individuellement du calcul d'échéance via le
/// même calendrier ouvré que le reste du calcul (TPS-02/04, SLA-03).
/// <para>
/// Distincte de <see cref="Ticket.DureeAttenteClienteCumulee"/>, qui reste une durée brute (temps
/// réel écoulé, pour TPS-02/03/05 — restitution au client/agent de la durée réelle d'attente) :
/// aucun changement de comportement sur cette mesure existante.
/// </para>
/// </summary>
public sealed class PeriodeAttenteClient
{
    public Guid Id { get; private set; }

    public Guid TicketId { get; private set; }

    public DateTime DebutUtc { get; private set; }

    public DateTime? FinUtc { get; private set; }

    private PeriodeAttenteClient() { }

    public static PeriodeAttenteClient Ouvrir(Guid ticketId, DateTime maintenantUtc) => new()
    {
        Id = Guid.NewGuid(),
        TicketId = ticketId,
        DebutUtc = maintenantUtc,
    };

    public void Fermer(DateTime maintenantUtc) => FinUtc = maintenantUtc;
}