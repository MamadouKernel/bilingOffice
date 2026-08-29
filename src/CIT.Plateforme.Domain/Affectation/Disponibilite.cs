namespace CIT.Plateforme.Domain.Affectation;

public enum StatutDisponibilite
{
    Disponible,
    Indisponible,
    Absent,
}

/// <summary>DIS-01..03 : disponibilité d'un agent. Un agent Indisponible ou Absent ne reçoit
/// jamais de nouvelle affectation automatique (invariant #11 de data-model.md).</summary>
public sealed class Disponibilite
{
    public Guid Id { get; private set; }

    public Guid OperateurId { get; private set; }

    public StatutDisponibilite Statut { get; private set; }

    public DateTime DateDerniereActiviteUtc { get; private set; }

    public DateTime DateDernierChangementUtc { get; private set; }

    private Disponibilite() { }

    public static Disponibilite Creer(Guid operateurId, DateTime maintenantUtc) => new()
    {
        Id = Guid.NewGuid(),
        OperateurId = operateurId,
        Statut = StatutDisponibilite.Disponible,
        DateDerniereActiviteUtc = maintenantUtc,
        DateDernierChangementUtc = maintenantUtc,
    };

    /// <summary>DIS-03 : tout changement de disponibilité est tracé (par l'appelant, via le
    /// journal d'audit — ce type n'assure que l'invariant temporel).</summary>
    public void ChangerStatut(StatutDisponibilite nouveauStatut, DateTime maintenantUtc)
    {
        Statut = nouveauStatut;
        DateDernierChangementUtc = maintenantUtc;
    }

    public void EnregistrerActivite(DateTime maintenantUtc) => DateDerniereActiviteUtc = maintenantUtc;

    public bool PeutRecevoirAffectationAutomatique => Statut == StatutDisponibilite.Disponible;
}
