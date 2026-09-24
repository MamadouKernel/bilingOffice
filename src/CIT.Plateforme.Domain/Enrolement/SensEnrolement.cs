namespace CIT.Plateforme.Domain.Enrolement;

/// <summary>
/// LOT 1 — Enrôlement société. Chaque formulaire CIT porte deux opérations opposées : l'ajout
/// (enrôler un chauffeur / un camion) et le retrait (révoquer sa carte / son BAT). Le sens
/// détermine l'issue du ticket : un <see cref="Ajout"/> aboutit à la production d'une carte/BAT
/// (récupérés par la société), un <see cref="Retrait"/> à la révocation de l'accès.
/// </summary>
public enum SensEnrolement
{
    /// <summary>Ajout — enrôlement produisant une carte (chauffeur) ou un BAT (camion).</summary>
    Ajout = 1,

    /// <summary>Retrait — révocation de la carte (chauffeur) ou du BAT (camion).</summary>
    Retrait = 2,
}
