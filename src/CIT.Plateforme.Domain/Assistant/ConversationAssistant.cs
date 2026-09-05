namespace CIT.Plateforme.Domain.Assistant;

/// <summary>Auteur d'un message du fil de l'assistant (RG-IA-01, RG-IA-02).</summary>
public enum AuteurMessageAssistant
{
    /// <summary>L'usager qui interroge l'assistant.</summary>
    Client,

    /// <summary>L'assistant lui-même — réponse de premier niveau ou orientation.</summary>
    Assistant,
}

/// <summary>
/// Fil de conversation avec l'assistant virtuel (§ 1.15 du document, IA-01, IA-02, IA-03, IA-06).
///
/// AUCUNE TABLE N'ÉTAIT PRÉVUE PAR LE MPD pour l'assistant. RG-IA-02 en suppose pourtant une :
/// « l'assistant escalade vers un opérateur humain […] <b>sans perte de contexte</b> (le fil de
/// conversation reste attaché au ticket) ». Un fil qui ne survit pas à l'escalade perd précisément
/// ce que la règle demande de conserver. Cet écart au MPD est assumé et documenté ici.
///
/// LE FOURNISSEUR N'EST PAS ARBITRÉ (PC-18). Le domaine n'en sait rien : il ne connaît qu'un fil
/// de messages et un état d'escalade. Ce qui produit les réponses est un port d'infrastructure,
/// aujourd'hui servi par un adaptateur simulé.
/// </summary>
public sealed class ConversationAssistant
{
    public Guid Id { get; private set; }

    /// <summary>Usager propriétaire du fil — fonde le cloisonnement (CPT-04).</summary>
    public Guid UtilisateurId { get; private set; }

    public DateTime DateOuvertureUtc { get; private set; }

    public DateTime DateDernierMessageUtc { get; private set; }

    /// <summary>
    /// RG-IA-02 — ticket auquel le fil est rattaché à l'escalade. Nul tant que l'assistant
    /// répond seul : rattacher d'office à un ticket obligerait à en créer un pour toute question,
    /// ce que la règle ne demande pas (« escalade […] uniquement lorsque nécessaire »).
    /// </summary>
    public Guid? TicketId { get; private set; }

    public bool EstEscaladee { get; private set; }

    public DateTime? DateEscaladeUtc { get; private set; }

    /// <summary>Motif de l'escalade, restitué à l'opérateur qui reprend le fil.</summary>
    public string? MotifEscalade { get; private set; }

    private readonly List<MessageAssistant> _messages = [];
    public IReadOnlyCollection<MessageAssistant> Messages => _messages.AsReadOnly();

    private ConversationAssistant() { }

    public static ConversationAssistant Ouvrir(Guid utilisateurId, DateTime maintenantUtc)
    {
        if (utilisateurId == Guid.Empty)
            throw new ArgumentException("La conversation doit avoir un propriétaire.", nameof(utilisateurId));

        return new ConversationAssistant
        {
            Id = Guid.NewGuid(),
            UtilisateurId = utilisateurId,
            DateOuvertureUtc = maintenantUtc,
            DateDernierMessageUtc = maintenantUtc,
        };
    }

    public MessageAssistant AjouterMessage(AuteurMessageAssistant auteur, string contenu, DateTime maintenantUtc)
    {
        if (string.IsNullOrWhiteSpace(contenu))
            throw new ArgumentException("Un message ne peut pas être vide.", nameof(contenu));

        if (EstEscaladee)
        {
            throw new ConversationEscaladeeException(
                "Cette conversation est passée à un opérateur humain : la suite des échanges se tient dans le ticket (RG-IA-02).");
        }

        var message = MessageAssistant.Creer(Id, auteur, contenu, maintenantUtc, _messages.Count + 1);
        _messages.Add(message);
        DateDernierMessageUtc = maintenantUtc;
        return message;
    }

    /// <summary>
    /// RG-IA-02 — passage à un opérateur humain, le fil restant attaché au ticket. Idempotent :
    /// une seconde escalade ne réécrit ni la date ni le rattachement d'origine.
    /// </summary>
    public void Escalader(Guid ticketId, string motif, DateTime maintenantUtc)
    {
        if (ticketId == Guid.Empty)
            throw new ArgumentException("L'escalade doit rattacher le fil à un ticket (RG-IA-02).", nameof(ticketId));
        if (string.IsNullOrWhiteSpace(motif))
            throw new ArgumentException("Le motif de l'escalade est obligatoire.", nameof(motif));

        if (EstEscaladee)
        {
            return;
        }

        TicketId = ticketId;
        EstEscaladee = true;
        DateEscaladeUtc = maintenantUtc;
        MotifEscalade = motif.Trim();
        DateDernierMessageUtc = maintenantUtc;
    }

    /// <summary>Transcription lisible du fil, telle qu'elle est remise à l'opérateur qui reprend
    /// la main — c'est elle qui matérialise l'absence de perte de contexte.</summary>
    public string Transcrire() =>
        string.Join(
            Environment.NewLine,
            _messages
                .OrderBy(m => m.Rang)
                .Select(m => $"[{(m.Auteur == AuteurMessageAssistant.Client ? "Client" : "Assistant")}] {m.Contenu}"));
}

/// <summary>Levée lorsqu'on écrit dans un fil déjà repris par un opérateur humain.</summary>
public sealed class ConversationEscaladeeException(string message) : InvalidOperationException(message);

/// <summary>Un message du fil de l'assistant.</summary>
public sealed class MessageAssistant
{
    public Guid Id { get; private set; }

    public Guid ConversationAssistantId { get; private set; }

    public AuteurMessageAssistant Auteur { get; private set; }

    public string Contenu { get; private set; } = string.Empty;

    public DateTime HorodatageUtc { get; private set; }

    /// <summary>Ordre dans le fil, à partir de 1 — rend la chronologie stable même si deux
    /// messages partagent le même horodatage.</summary>
    public int Rang { get; private set; }

    private MessageAssistant() { }

    internal static MessageAssistant Creer(
        Guid conversationId, AuteurMessageAssistant auteur, string contenu, DateTime maintenantUtc, int rang) => new()
    {
        Id = Guid.NewGuid(),
        ConversationAssistantId = conversationId,
        Auteur = auteur,
        Contenu = contenu.Trim(),
        HorodatageUtc = maintenantUtc,
        Rang = rang,
    };
}
