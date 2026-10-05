namespace CIT.Plateforme.Domain.Notifications;

/// <summary>
/// FEATURE 009 (T010) — le fait notifié, adressé à UN destinataire nommé.
///
/// Porte les attributs « destinataire » et « modèle » du modèle de données minimal du CDC §20
/// (<c>Notification (canal, modèle, destinataire, statut d'envoi)</c>) ; « canal » et « statut
/// d'envoi » sont portés par <see cref="EnvoiNotification"/>, car NOT-01 exige un état Lu/Non lu
/// par notification tandis qu'ERR-02 exige un statut isolé par canal (research.md D3).
///
/// Le destinataire est TOUJOURS un utilisateur nommé : le CDC §20 le modélise au singulier et
/// ne nomme jamais une file comme destinataire (CLAR-NOT-04). Aucun broadcast n'existe.
///
/// Aucune suppression, aucun archivage, aucune transition « marquer comme non lu » : le CDC est
/// silencieux sur la conservation (CLAR-NOT-03) et n'en prévoit aucune (research.md D13).
/// </summary>
public sealed class Notification
{
    public Guid Id { get; private set; }

    /// <summary>Événement source (<c>EvenementSocle</c>). Avec <see cref="DestinataireId"/>,
    /// forme l'index unique qui garantit l'effet « effectively-once » de la distribution.</summary>
    public Guid EvenementSourceId { get; private set; }

    /// <summary>Destinataire — un seul, nommé, jamais un groupe (CDC §20, CLAR-NOT-04).</summary>
    public Guid DestinataireId { get; private set; }

    public string TypeEvenement { get; private set; } = string.Empty;

    /// <summary>Modèle appliqué — attribut « modèle » du CDC §20 (COM-03).</summary>
    public Guid? ModeleId { get; private set; }

    /// <summary>Rendu à la création puis FIGÉ : modifier un modèle plus tard n'altère jamais une
    /// notification déjà émise (contrats C9-06, cas TEST-NOT-02-3).</summary>
    public string Titre { get; private set; } = string.Empty;

    /// <summary>Rendu à la création puis figé, comme <see cref="Titre"/>.</summary>
    public string Corps { get; private set; } = string.Empty;

    /// <summary>Référence à l'objet métier (COM-02, NOT-01). JAMAIS une URL : le lien est
    /// relatif et résolu au rendu, sans jeton d'accès (SEC-12, research.md D12).</summary>
    public Guid? TicketId { get; private set; }

    /// <summary>État exigé littéralement par NOT-01.</summary>
    public bool EstLue { get; private set; }

    public DateTime? LueLeUtc { get; private set; }

    public DateTime CreeeLeUtc { get; private set; }

    /// <summary>Langue du destinataire à la création (« fr » / « en ») : celle de l'e-mail associé.</summary>
    public string Langue { get; private set; } = "fr";

    private Notification() { }

    public static Notification Creer(
        Guid evenementSourceId,
        Guid destinataireId,
        string typeEvenement,
        Guid? modeleId,
        string titre,
        string corps,
        Guid? ticketId,
        DateTime maintenantUtc,
        string langue = "fr")
    {
        if (evenementSourceId == Guid.Empty)
        {
            throw new ArgumentException("L'événement source est obligatoire.", nameof(evenementSourceId));
        }

        if (destinataireId == Guid.Empty)
        {
            throw new ArgumentException("Le destinataire est obligatoire — une notification vise toujours un utilisateur nommé (CDC §20).", nameof(destinataireId));
        }

        if (string.IsNullOrWhiteSpace(typeEvenement))
        {
            throw new ArgumentException("Le type d'événement est obligatoire.", nameof(typeEvenement));
        }

        if (string.IsNullOrWhiteSpace(titre))
        {
            throw new ArgumentException("Le titre est obligatoire.", nameof(titre));
        }

        return new Notification
        {
            Id = Guid.NewGuid(),
            EvenementSourceId = evenementSourceId,
            DestinataireId = destinataireId,
            TypeEvenement = typeEvenement,
            ModeleId = modeleId,
            Titre = titre,
            Corps = corps ?? string.Empty,
            TicketId = ticketId,
            EstLue = false,
            CreeeLeUtc = maintenantUtc,
            Langue = langue == "en" ? "en" : "fr",
        };
    }

    /// <summary>NOT-01. IDEMPOTENTE : sur une notification déjà lue, ne modifie rien et ne lève
    /// aucune erreur (cas TEST-NOT-01-3).</summary>
    public void MarquerCommeLue(DateTime maintenantUtc)
    {
        if (EstLue)
        {
            return;
        }

        EstLue = true;
        LueLeUtc = maintenantUtc;
    }
}
