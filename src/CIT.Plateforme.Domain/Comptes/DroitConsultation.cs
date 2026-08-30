namespace CIT.Plateforme.Domain.Comptes;

/// <summary>
/// Portée de visibilité des demandes accordée à un <see cref="UtilisateurSociete"/> (ENT-03). Par
/// défaut à la création d'un rattachement : <see cref="PropresDemandesUniquement"/> (moindre
/// privilège, Principe VII). Un administrateur de société voit toujours l'ensemble des demandes de
/// sa société, indépendamment de cette valeur (CLAR-02, cf. <see cref="UtilisateurSociete"/>).
/// </summary>
public enum DroitConsultation
{
    PropresDemandesUniquement,
    ToutesDemandesSociete,
}
