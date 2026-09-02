namespace CIT.Plateforme.Domain.Securite;

/// <summary>
/// États de droit au niveau champ pour les traitements parallèles IES (SEC-02).
/// </summary>
public enum EtatDroitChamp
{
    AllowWrite,
    ReadOnly,
    Masked
}
