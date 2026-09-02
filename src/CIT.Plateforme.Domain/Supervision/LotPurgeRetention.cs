namespace CIT.Plateforme.Domain.Supervision;

/// <summary>
/// Certificat et trace d'audit d'un lot de purge ou d'anonymisation réglementaire ARTCI (SEC-06).
/// Ne contient AUCUNE donnée à caractère personnel.
/// </summary>
public sealed class LotPurgeRetention
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime DateExecutionUtc { get; set; }
    public string Categorie { get; set; } = string.Empty;
    public int NombreElementsTraites { get; set; }
    public bool EstSucces { get; set; } = true;
    public string? MessageErreur { get; set; }
}
