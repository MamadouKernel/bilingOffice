namespace CIT.Plateforme.Domain.ActualisationBadt;

/// <summary>
/// EXTENSION FEATURE 010 (Actualisation BADT) : une ligne conteneur d'une demande d'actualisation
/// de BADT, rattachée à un ticket (<see cref="TicketId"/>). Le grain de décision est le conteneur :
/// un même ticket (la demande) peut porter plusieurs lignes, chacune accordée ou refusée
/// indépendamment. Suit le motif de <c>PassageEnFile</c> : entité persistée indépendamment, jamais
/// une collection de l'agrégat.
///
/// La ligne ne connaît pas Navis : elle ne porte que la décision métier. L'extraction N4 (toutes les
/// autres colonnes du fichier) et la réécriture de la seule colonne T (<see cref="NouvelleDateValidite"/>)
/// sont du ressort du générateur, côté Infrastructure.
/// </summary>
public sealed class LigneActualisationBadt
{
    public Guid Id { get; private set; }

    /// <summary>Ticket (la demande d'actualisation) auquel la ligne est rattachée.</summary>
    public Guid TicketId { get; private set; }

    /// <summary>Numéro de BL métier. Un BL peut porter plusieurs conteneurs (plusieurs lignes).</summary>
    public string NumeroBl { get; private set; } = string.Empty;

    /// <summary>Numéro de conteneur (clé fonctionnelle de la ligne).</summary>
    public string NumeroConteneur { get; private set; } = string.Empty;

    /// <summary>Cause qualifiée par l'agent. <c>null</c> tant que l'analyse n'est pas faite.</summary>
    public CauseExpirationBadt? Cause { get; private set; }

    /// <summary>Motif précis issu du référentiel + commentaire d'analyse. Obligatoire à la qualification.</summary>
    public string? MotifCause { get; private set; }

    public DecisionLigneBadt Decision { get; private set; } = DecisionLigneBadt.EnAttente;

    /// <summary>Nombre de jours d'actualisation accordés par le Décideur (entier strictement positif).</summary>
    public int? NombreJoursAccordes { get; private set; }

    /// <summary>Ancienne date d'expiration du BADT (reprise de Navis), conservée pour la traçabilité.</summary>
    public DateOnly? AncienneDateExpiration { get; private set; }

    /// <summary>Nouvelle date de validité = colonne T du fichier Navis. Renseignée uniquement à l'accord.</summary>
    public DateOnly? NouvelleDateValidite { get; private set; }

    /// <summary>Motif obligatoire en cas de rejet (agent) ou de refus (Décideur).</summary>
    public string? MotifRefus { get; private set; }

    private LigneActualisationBadt() { }

    public static LigneActualisationBadt Creer(Guid ticketId, string numeroBl, string numeroConteneur)
    {
        if (string.IsNullOrWhiteSpace(numeroBl))
        {
            throw new ArgumentException("Le numéro de BL est obligatoire.", nameof(numeroBl));
        }

        if (string.IsNullOrWhiteSpace(numeroConteneur))
        {
            throw new ArgumentException("Le numéro de conteneur est obligatoire.", nameof(numeroConteneur));
        }

        return new LigneActualisationBadt
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            NumeroBl = numeroBl.Trim(),
            NumeroConteneur = numeroConteneur.Trim(),
        };
    }

    /// <summary>Qualifie la cause de l'expiration (agent). Le commentaire/motif est obligatoire.</summary>
    public void Qualifier(CauseExpirationBadt cause, string motif)
    {
        if (Decision != DecisionLigneBadt.EnAttente)
        {
            throw new InvalidOperationException($"La ligne {NumeroConteneur} est déjà décidée ({Decision}).");
        }

        if (string.IsNullOrWhiteSpace(motif))
        {
            throw new ArgumentException("Le motif/commentaire d'analyse est obligatoire.", nameof(motif));
        }

        Cause = cause;
        MotifCause = motif.Trim();
    }

    /// <summary>
    /// Accord d'actualisation. Réservé à une cause <see cref="CauseExpirationBadt.Terminale"/>
    /// (RG : un conteneur non imputable au terminal ne peut pas être actualisé). <paramref name="nombreJours"/>
    /// doit être strictement positif ; la nouvelle date de validité est calculée par l'appelant
    /// (règle « date de décision + N », paramétrable) et posée telle quelle dans la colonne T.
    /// </summary>
    public void Accorder(int nombreJours, DateOnly? ancienneDateExpiration, DateOnly nouvelleDateValidite)
    {
        if (Decision != DecisionLigneBadt.EnAttente)
        {
            throw new InvalidOperationException($"La ligne {NumeroConteneur} est déjà décidée ({Decision}).");
        }

        if (Cause != CauseExpirationBadt.Terminale)
        {
            throw new InvalidOperationException(
                "Seul un conteneur dont la cause est qualifiée « terminale » peut être actualisé.");
        }

        if (nombreJours <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(nombreJours), "Le nombre de jours accordés doit être strictement positif.");
        }

        Decision = DecisionLigneBadt.Accordee;
        NombreJoursAccordes = nombreJours;
        AncienneDateExpiration = ancienneDateExpiration;
        NouvelleDateValidite = nouvelleDateValidite;
        MotifRefus = null;
    }

    /// <summary>Rejet (agent, cause non terminale) ou refus (Décideur). Le motif est obligatoire.</summary>
    public void Refuser(string motif)
    {
        if (Decision != DecisionLigneBadt.EnAttente)
        {
            throw new InvalidOperationException($"La ligne {NumeroConteneur} est déjà décidée ({Decision}).");
        }

        if (string.IsNullOrWhiteSpace(motif))
        {
            throw new ArgumentException("Le motif de refus est obligatoire.", nameof(motif));
        }

        Decision = DecisionLigneBadt.Refusee;
        MotifRefus = motif.Trim();
        NombreJoursAccordes = null;
        NouvelleDateValidite = null;
    }
}
