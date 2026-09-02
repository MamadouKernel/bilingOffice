namespace CIT.Plateforme.Domain.Securite;

/// <summary>
/// Blocs de données concernés par le contrôle d'accès RBAC au niveau champ lors des
/// traitements parallèles IES (SEC-02, IMP-40, IMP-41, REC-SCE-04).
/// </summary>
public enum BlocDonneesIes
{
    Manifeste,
    Douane,
    Financier,
    Chauffeur
}
