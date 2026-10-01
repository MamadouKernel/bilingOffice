namespace CIT.Plateforme.Domain.Affectation;

/// <summary>
/// DIS-03 (reporting) — trace append-only de chaque changement de disponibilité d'un agent.
/// Distincte du journal d'audit (qui masque volontairement l'avant/après aux consommateurs) :
/// cette table expose les transitions pour le calcul de performance (temps et nombre
/// d'indisponibilités). Une ligne par changement effectif de statut.
/// </summary>
public sealed class HistoriqueDisponibilite
{
    public Guid Id { get; private set; }

    public Guid OperateurId { get; private set; }

    /// <summary>Statut avant le changement ; null pour la toute première déclaration de l'agent.</summary>
    public StatutDisponibilite? AncienStatut { get; private set; }

    public StatutDisponibilite NouveauStatut { get; private set; }

    public DateTime HorodatageUtc { get; private set; }

    private HistoriqueDisponibilite() { }

    public static HistoriqueDisponibilite Creer(
        Guid operateurId, StatutDisponibilite? ancienStatut, StatutDisponibilite nouveauStatut, DateTime horodatageUtc) => new()
    {
        Id = Guid.NewGuid(),
        OperateurId = operateurId,
        AncienStatut = ancienStatut,
        NouveauStatut = nouveauStatut,
        HorodatageUtc = horodatageUtc,
    };
}
