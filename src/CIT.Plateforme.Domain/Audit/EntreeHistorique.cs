namespace CIT.Plateforme.Domain.Audit;

/// <summary>
/// Historique fonctionnel du ticket — chronologie visible du client et des agents habilités
/// (SOC-02, SOC-09). Distinct du journal d'audit de sécurité (Principe IX de la constitution,
/// decision D8) : deux finalités différentes, deux entrées, jamais confondues.
/// </summary>
public sealed class EntreeHistorique
{
    public Guid Id { get; private set; }

    public Guid TicketId { get; private set; }

    public string Evenement { get; private set; } = string.Empty;

    public string? Detail { get; private set; }

    public Guid? AuteurId { get; private set; }

    public DateTime HorodatageUtc { get; private set; }

    private EntreeHistorique() { }

    public static EntreeHistorique Creer(Guid ticketId, string evenement, string? detail, Guid? auteurId, DateTime maintenantUtc) => new()
    {
        Id = Guid.NewGuid(),
        TicketId = ticketId,
        Evenement = evenement,
        Detail = detail,
        AuteurId = auteurId,
        HorodatageUtc = maintenantUtc,
    };
}
