namespace CIT.Plateforme.Domain.Enrolement;

/// <summary>
/// ENR-01 (remédiation) — valeur structurée soumise par le chauffeur/transporteur pour un champ du
/// référentiel <see cref="ChampFormulaireEnrolement"/>, rattachée au ticket d'enrôlement créé par la
/// soumission. Le code du champ est copié tel quel plutôt que porté par une clé étrangère stricte :
/// si un champ est désactivé plus tard, la valeur déjà soumise reste lisible sur la fiche.
/// </summary>
public sealed class ValeurChampEnrolement
{
    public Guid Id { get; private set; }

    public Guid TicketId { get; private set; }

    public string ChampCode { get; private set; } = string.Empty;

    public string Valeur { get; private set; } = string.Empty;

    private ValeurChampEnrolement() { }

    public static ValeurChampEnrolement Creer(Guid ticketId, string champCode, string valeur)
    {
        if (string.IsNullOrWhiteSpace(champCode))
        {
            throw new ArgumentException("Le code du champ est obligatoire.", nameof(champCode));
        }

        return new ValeurChampEnrolement
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            ChampCode = champCode.Trim(),
            Valeur = valeur?.Trim() ?? string.Empty,
        };
    }
}
