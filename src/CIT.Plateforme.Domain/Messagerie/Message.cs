namespace CIT.Plateforme.Domain.Messagerie;

public enum TypeMessage
{
    /// <summary>SOC-05/MSG-01 : visible par le client et les agents habilités.</summary>
    Externe,

    /// <summary>MSG-02 : jamais visible du client, réservé aux collaborateurs CIT habilités.</summary>
    NoteInterne,
}

/// <summary>
/// Élément du fil d'échange d'un ticket (SOC-05, MSG-01..05). Le caractère interne/externe est
/// porté par le type — jamais déductible implicitement, pour garantir la distinction visuelle
/// obligatoire (MSG-03) et l'étanchéité absolue des notes internes (MSG-02).
/// </summary>
public sealed class Message
{
    public Guid Id { get; private set; }

    public Guid TicketId { get; private set; }

    public Guid AuteurId { get; private set; }

    public TypeMessage Type { get; private set; }

    public string Contenu { get; private set; } = string.Empty;

    public DateTime HorodatageUtc { get; private set; }

    /// <summary>MSG-04 : collaborateur mentionné (@Nom), le cas échéant.</summary>
    public Guid? CollaborateurMentionneId { get; private set; }

    private Message() { }

    public static Message Publier(Guid ticketId, Guid auteurId, TypeMessage type, string contenu, DateTime maintenantUtc, Guid? collaborateurMentionneId = null)
    {
        if (string.IsNullOrWhiteSpace(contenu))
        {
            throw new ArgumentException("Le contenu du message ne peut pas être vide.", nameof(contenu));
        }

        return new Message
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            AuteurId = auteurId,
            Type = type,
            Contenu = contenu,
            HorodatageUtc = maintenantUtc,
            CollaborateurMentionneId = collaborateurMentionneId,
        };
    }
}
