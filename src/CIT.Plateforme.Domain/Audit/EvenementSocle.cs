namespace CIT.Plateforme.Domain.Audit;

/// <summary>
/// Événement de domaine persisté (decision D10) — consommé in-process aujourd'hui ; la future
/// feature Notifications (PC-25, hors périmètre ici) s'y abonnera pour les canaux externes. Aucun
/// canal, modèle ou notification n'est implémenté dans ce socle.
///
/// EXTENSION FEATURE 009 (T024, research.md D2) : la Feature 009 réalise cet abonnement. Une
/// seule propriété nullable est ajoutée — <see cref="DistribueLeUtc"/>, curseur de distribution.
/// Extension STRICTEMENT ADDITIVE : aucune propriété existante n'est modifiée ni supprimée, et
/// les lignes historiques valent <c>null</c> (elles seront examinées une fois puis marquées).
/// </summary>
public sealed class EvenementSocle
{
    public Guid Id { get; private set; }

    public string TypeEvenement { get; private set; } = string.Empty;

    public string ChargeUtile { get; private set; } = string.Empty;

    public DateTime HorodatageUtc { get; private set; }

    /// <summary>
    /// FEATURE 009 — curseur de distribution. <c>null</c> = événement non encore examiné par le
    /// moteur de notification. Renseigné dans LA MÊME TRANSACTION que la persistance des
    /// notifications produites, et jamais avant : c'est ce qui garantit qu'un échec laisse
    /// l'événement à relire au tour suivant (garantie at-least-once, tasks.md I9).
    /// </summary>
    public DateTime? DistribueLeUtc { get; private set; }

    private EvenementSocle() { }

    public static EvenementSocle Creer(string typeEvenement, string chargeUtile, DateTime maintenantUtc) => new()
    {
        Id = Guid.NewGuid(),
        TypeEvenement = typeEvenement,
        ChargeUtile = chargeUtile,
        HorodatageUtc = maintenantUtc,
    };

    /// <summary>
    /// FEATURE 009 — marque l'événement comme distribué. IDEMPOTENT : un second passage ne
    /// redate jamais la distribution.
    /// </summary>
    public void MarquerDistribue(DateTime maintenantUtc)
    {
        DistribueLeUtc ??= maintenantUtc;
    }
}
