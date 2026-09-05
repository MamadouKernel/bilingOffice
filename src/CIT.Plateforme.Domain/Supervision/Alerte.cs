namespace CIT.Plateforme.Domain.Supervision;

/// <summary>ALE-01 : types d'alertes définis par le CDC.</summary>
public enum TypeAlerte
{
    NonPriseEnCharge,
    SansActivite,
    SlaSeuilInformation,
    SlaSeuilAlerte,
    SlaDepasse,

    /// <summary>ALE-03 — le dépassement a franchi le seuil d'escalade administré.</summary>
    SlaEscalade,
    BacklogFile,
    VolumeAnormal,
    OperateurIndisponibleAvecTickets,
}

/// <summary>Alerte interne (ALE-01) — destinataire/canal paramétrables (ALE-02), hors périmètre
/// des canaux effectifs (feature Notifications, PC-25).</summary>
public sealed class Alerte
{
    public Guid Id { get; private set; }

    public TypeAlerte Type { get; private set; }

    public Guid? TicketId { get; private set; }

    public Guid? FileId { get; private set; }

    public DateTime DateCreationUtc { get; private set; }

    public bool Traitee { get; private set; }

    private Alerte() { }

    public static Alerte Creer(TypeAlerte type, Guid? ticketId, Guid? fileId, DateTime maintenantUtc) => new()
    {
        Id = Guid.NewGuid(),
        Type = type,
        TicketId = ticketId,
        FileId = fileId,
        DateCreationUtc = maintenantUtc,
    };

    public void MarquerTraitee() => Traitee = true;
}
