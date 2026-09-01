namespace CIT.Plateforme.Domain.Notifications;

/// <summary>
/// FEATURE 009 (T015) — une tentative de diffusion sur UN canal externe.
///
/// Porte les attributs « canal » et « statut d'envoi » du CDC §20. Chaque envoi évolue
/// INDÉPENDAMMENT des autres envois de la même notification : c'est ce qui rend démontrable
/// l'obligation d'ERR-02 (« l'échec d'un canal n'empêche pas les autres »).
///
/// Aucun envoi n'est créé pour <see cref="CanalNotification.InApp"/> : l'existence de la
/// <see cref="Notification"/> EST sa diffusion (research.md D3).
///
/// AUCUNE bascule de canal n'est offerte : l'isolation exigée par ERR-02 n'est pas une
/// suppléance ; le CDC ne prévoit aucune substitution automatique entre canaux (spec.md §15).
/// </summary>
public sealed class EnvoiNotification
{
    /// <summary>Le motif est BORNÉ et purement technique : il ne doit jamais porter le contenu
    /// du message (spec.md §16, Constitution Principe XIV — données personnelles).</summary>
    public const int LongueurMaxMotif = 512;

    public Guid Id { get; private set; }

    public Guid NotificationId { get; private set; }

    public CanalNotification Canal { get; private set; }

    public StatutEnvoi Statut { get; private set; }

    public int NombreTentatives { get; private set; }

    public DateTime? DerniereTentativeUtc { get; private set; }

    public DateTime? AboutiLeUtc { get; private set; }

    /// <summary>Motif technique de l'échec — jamais le contenu émis.</summary>
    public string? MotifEchec { get; private set; }

    private EnvoiNotification() { }

    public static EnvoiNotification Creer(Guid notificationId, CanalNotification canal)
    {
        if (notificationId == Guid.Empty)
        {
            throw new ArgumentException("La notification est obligatoire.", nameof(notificationId));
        }

        if (canal == CanalNotification.InApp)
        {
            throw new ArgumentException(
                "Le canal In-App ne produit aucun envoi : la notification elle-même est sa diffusion (research.md D3).",
                nameof(canal));
        }

        return new EnvoiNotification
        {
            Id = Guid.NewGuid(),
            NotificationId = notificationId,
            Canal = canal,
            Statut = StatutEnvoi.EnAttente,
            NombreTentatives = 0,
        };
    }

    public void EnregistrerSucces(DateTime maintenantUtc)
    {
        NombreTentatives++;
        DerniereTentativeUtc = maintenantUtc;
        AboutiLeUtc = maintenantUtc;
        Statut = StatutEnvoi.Envoye;
        MotifEchec = null;
    }

    /// <summary>ERR-02 : traçage de l'échec et comptage des tentatives. N'affecte AUCUN autre
    /// envoi de la même notification.</summary>
    public void EnregistrerEchec(string motif, DateTime maintenantUtc)
    {
        NombreTentatives++;
        DerniereTentativeUtc = maintenantUtc;
        Statut = StatutEnvoi.Echec;
        MotifEchec = Tronquer(motif);
    }

    /// <summary>CLAR-COM-04 : la coordonnée requise est absente, ou le canal n'est pas servable.
    /// Rien n'a été tenté — statut distinct d'un échec de transport.</summary>
    public void MarquerNonDelivrable(string motif, DateTime maintenantUtc)
    {
        DerniereTentativeUtc = maintenantUtc;
        Statut = StatutEnvoi.NonDelivrable;
        MotifEchec = Tronquer(motif);
    }

    private static string? Tronquer(string? motif)
    {
        if (string.IsNullOrWhiteSpace(motif))
        {
            return null;
        }

        return motif.Length <= LongueurMaxMotif ? motif : motif[..LongueurMaxMotif];
    }
}
