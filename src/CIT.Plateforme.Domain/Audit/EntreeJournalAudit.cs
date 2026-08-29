namespace CIT.Plateforme.Domain.Audit;

/// <summary>
/// Journal d'audit de sécurité — registre append-only (SOC-13, SEC-05, decision D8), réservé à
/// l'administrateur technique. Aucune méthode de modification n'existe volontairement sur ce
/// type : une fois créée, une entrée est immuable (garde renforcée côté Infrastructure — T098).
/// </summary>
public sealed class EntreeJournalAudit
{
    public Guid Id { get; private set; }

    public Guid? AuteurId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public string EntiteType { get; private set; } = string.Empty;

    public Guid EntiteId { get; private set; }

    public string? DetailAvant { get; private set; }

    public string? DetailApres { get; private set; }

    public DateTime HorodatageUtc { get; private set; }

    private EntreeJournalAudit() { }

    public static EntreeJournalAudit Creer(
        Guid? auteurId, string action, string entiteType, Guid entiteId,
        string? detailAvant, string? detailApres, DateTime maintenantUtc) => new()
    {
        Id = Guid.NewGuid(),
        AuteurId = auteurId,
        Action = action,
        EntiteType = entiteType,
        EntiteId = entiteId,
        DetailAvant = detailAvant,
        DetailApres = detailApres,
        HorodatageUtc = maintenantUtc,
    };
}
