namespace CIT.Plateforme.Domain.ActualisationBadt;

/// <summary>Nature d'une mesure de reporting BADT (un type d'événement du workflow).</summary>
public enum TypeMesureBadt
{
    /// <summary>Dépôt d'une demande (un par ticket).</summary>
    Creation,

    /// <summary>Dépliage d'un BL : l'agent a ajouté un (des) conteneur(s).</summary>
    Depliage,

    /// <summary>Qualification d'une ligne par l'agent (cause posée). Une cause terminale vaut escalade.</summary>
    Qualification,

    /// <summary>Rejet « agent seul » d'une ligne à cause non terminale.</summary>
    RejetAgent,

    /// <summary>Accord du Décideur (N jours).</summary>
    Accord,

    /// <summary>Refus du Décideur.</summary>
    Refus,

    /// <summary>Renvoi à l'agent par le Décideur (requalification).</summary>
    Renvoi,
}

/// <summary>
/// FEATURE 010 (Actualisation BADT) — reporting (approche B, plan #3). Trace append-only d'un
/// événement du workflow, écrite par le handler qui le produit. Distincte du journal d'audit et de
/// l'historique du ticket (qui sérialisent le détail en texte) : ici chaque donnée utile au calcul
/// des indicateurs (acteur, cause, jours, horodatage) est portée en colonne, sans parsing de chaîne.
/// Une ligne par événement ; jamais mutée.
/// </summary>
public sealed class MesureActualisationBadt
{
    public Guid Id { get; private set; }

    public Guid TicketId { get; private set; }

    /// <summary>Ligne conteneur concernée ; <c>null</c> pour une mesure au niveau ticket (création).</summary>
    public Guid? LigneId { get; private set; }

    public string NumeroBl { get; private set; } = string.Empty;

    public string NumeroConteneur { get; private set; } = string.Empty;

    public TypeMesureBadt Type { get; private set; }

    /// <summary>Auteur de l'événement (demandeur, agent Support Client, ou Décideur selon le type).</summary>
    public Guid ActeurId { get; private set; }

    /// <summary>Cause qualifiée (sur une mesure de qualification). Une cause terminale = escalade.</summary>
    public CauseExpirationBadt? Cause { get; private set; }

    /// <summary>Nombre de jours accordés (sur une mesure d'accord).</summary>
    public int? NombreJoursAccordes { get; private set; }

    public DateTime HorodatageUtc { get; private set; }

    private MesureActualisationBadt() { }

    public static MesureActualisationBadt Creer(
        Guid ticketId,
        Guid? ligneId,
        string? numeroBl,
        string? numeroConteneur,
        TypeMesureBadt type,
        Guid acteurId,
        DateTime horodatageUtc,
        CauseExpirationBadt? cause = null,
        int? nombreJoursAccordes = null) => new()
    {
        Id = Guid.NewGuid(),
        TicketId = ticketId,
        LigneId = ligneId,
        NumeroBl = numeroBl?.Trim() ?? string.Empty,
        NumeroConteneur = numeroConteneur?.Trim() ?? string.Empty,
        Type = type,
        ActeurId = acteurId,
        Cause = cause,
        NombreJoursAccordes = nombreJoursAccordes,
        HorodatageUtc = horodatageUtc,
    };
}
