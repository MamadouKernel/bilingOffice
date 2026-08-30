namespace CIT.Plateforme.Domain.Comptes;

/// <summary>
/// Rattachement d'un utilisateur (compte Identity existant, Feature 001) à une
/// <see cref="SocieteCliente"/> (ENT-01). Porte individuellement les droits de consultation et de
/// soumission (ENT-02) — jamais une entité fusionnant identité technique et société (research.md
/// D1, D2). Un utilisateur est rattaché à exactement une société à la fois (spec.md § Assumptions).
/// </summary>
public sealed class UtilisateurSociete
{
    public Guid Id { get; private set; }

    public Guid UtilisateurId { get; private set; }

    public Guid SocieteId { get; private set; }

    public bool EstAdministrateur { get; private set; }

    /// <summary>Par défaut : <see cref="DroitConsultation.PropresDemandesUniquement"/> (moindre
    /// privilège, Principe VII).</summary>
    public DroitConsultation DroitConsultation { get; private set; }

    public bool DroitSoumission { get; private set; }

    public bool EstActif { get; private set; }

    public DateTime DateRattachementUtc { get; private set; }

    private UtilisateurSociete() { }

    public static UtilisateurSociete Rattacher(
        Guid utilisateurId, Guid societeId, DateTime maintenantUtc, bool estAdministrateur = false, bool droitSoumission = true) => new()
    {
        Id = Guid.NewGuid(),
        UtilisateurId = utilisateurId,
        SocieteId = societeId,
        EstAdministrateur = estAdministrateur,
        DroitConsultation = DroitConsultation.PropresDemandesUniquement,
        DroitSoumission = droitSoumission,
        EstActif = true,
        DateRattachementUtc = maintenantUtc,
    };

    public void ModifierDroits(DroitConsultation droitConsultation, bool droitSoumission)
    {
        DroitConsultation = droitConsultation;
        DroitSoumission = droitSoumission;
    }

    public void Activer() => EstActif = true;

    public void Desactiver() => EstActif = false;

    public void AttribuerRoleAdministrateur() => EstAdministrateur = true;

    public void RetirerRoleAdministrateur() => EstAdministrateur = false;
}
