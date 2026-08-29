namespace CIT.Plateforme.Domain.Audit;

/// <summary>
/// Événement de domaine persisté (decision D10) — consommé in-process aujourd'hui ; la future
/// feature Notifications (PC-25, hors périmètre ici) s'y abonnera pour les canaux externes. Aucun
/// canal, modèle ou notification n'est implémenté dans ce socle.
/// </summary>
public sealed class EvenementSocle
{
    public Guid Id { get; private set; }

    public string TypeEvenement { get; private set; } = string.Empty;

    public string ChargeUtile { get; private set; } = string.Empty;

    public DateTime HorodatageUtc { get; private set; }

    private EvenementSocle() { }

    public static EvenementSocle Creer(string typeEvenement, string chargeUtile, DateTime maintenantUtc) => new()
    {
        Id = Guid.NewGuid(),
        TypeEvenement = typeEvenement,
        ChargeUtile = chargeUtile,
        HorodatageUtc = maintenantUtc,
    };
}
