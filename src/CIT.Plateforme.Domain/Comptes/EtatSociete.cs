namespace CIT.Plateforme.Domain.Comptes;

/// <summary>
/// État du cycle de vie d'une <see cref="SocieteCliente"/> (CPT-01). Aucune transition automatique,
/// aucun délai/expiration (le CDC n'en prévoit pas — Edge Case non résolu, spec.md).
/// </summary>
public enum EtatSociete
{
    EnAttenteValidation,
    Active,
    Refusee,
}
