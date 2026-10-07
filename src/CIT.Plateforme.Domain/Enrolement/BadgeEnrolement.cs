namespace CIT.Plateforme.Domain.Enrolement;

/// <summary>
/// Badge d'accès remis à l'issue d'un enrôlement (ENR-03, ENR-05). Décision du 03/10/2026 : la
/// remise n'est plus un simple constat — le numéro et la date d'expiration sont enregistrés, ce
/// qui permet de savoir qui détient quel badge, de le révoquer et de relancer avant l'échéance
/// sur la VRAIE date (et non une durée théorique calculée depuis la clôture).
/// Un badge par ticket d'enrôlement ; un renouvellement passe par un nouveau ticket.
/// </summary>
public sealed class BadgeEnrolement
{
    public const int LongueurMaxNumero = 50;

    public Guid Id { get; private set; }

    /// <summary>Ticket d'enrôlement dont la clôture a remis ce badge.</summary>
    public Guid TicketId { get; private set; }

    /// <summary>Numéro imprimé sur le badge, tel que saisi par l'Agent Gate.</summary>
    public string Numero { get; private set; } = string.Empty;

    public DateTime DateRemiseUtc { get; private set; }

    public DateTime DateExpirationUtc { get; private set; }

    /// <summary>Agent Gate ayant constaté la remise.</summary>
    public Guid AgentId { get; private set; }

    private BadgeEnrolement() { }

    public static BadgeEnrolement Remettre(Guid ticketId, string numero, DateTime dateExpirationUtc, Guid agentId, DateTime maintenantUtc)
    {
        var numeroNettoye = numero?.Trim() ?? string.Empty;
        if (numeroNettoye.Length == 0 || numeroNettoye.Length > LongueurMaxNumero)
        {
            throw new ArgumentException($"Le numéro de badge est obligatoire ({LongueurMaxNumero} caractères au plus).", nameof(numero));
        }

        if (dateExpirationUtc <= maintenantUtc)
        {
            throw new ArgumentException("La date d'expiration du badge doit être postérieure à sa remise.", nameof(dateExpirationUtc));
        }

        return new BadgeEnrolement
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Numero = numeroNettoye,
            DateRemiseUtc = maintenantUtc,
            DateExpirationUtc = dateExpirationUtc,
            AgentId = agentId,
        };
    }

    /// <summary>
    /// Correction d'une date d'expiration saisie par erreur (décision 07/10/2026). Le badge déjà
    /// remis n'est pas recréé : seule l'échéance est rectifiée, et elle doit rester future (une
    /// carte déjà expirée n'a pas d'intérêt). La traçabilité de la correction est portée par
    /// l'historique du ticket, côté commande.
    /// </summary>
    public void CorrigerExpiration(DateTime nouvelleExpirationUtc, DateTime maintenantUtc)
    {
        if (nouvelleExpirationUtc <= maintenantUtc)
        {
            throw new ArgumentException("La date d'expiration corrigée doit être postérieure à aujourd'hui.", nameof(nouvelleExpirationUtc));
        }

        DateExpirationUtc = nouvelleExpirationUtc;
    }
}
