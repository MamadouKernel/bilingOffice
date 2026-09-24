namespace CIT.Plateforme.Domain.Enrolement;

/// <summary>
/// LOT 1 — Enrôlement société (formulaires CIT-CIV-CG-F-0001 / F-0002). Un ticket d'enrôlement
/// porte sur un objet précis : un <see cref="Chauffeur"/> (dont l'enrôlement produit une carte) ou
/// un <see cref="Camion"/> (dont l'enrôlement produit un BAT — Badge d'Accès Terminal). La distinction
/// n'est pas cosmétique : les pièces exigées et l'issue diffèrent (carte vs BAT).
/// </summary>
public enum TypeObjetEnrolement
{
    /// <summary>Chauffeur — pièces : permis de conduire + photo. Produit une carte.</summary>
    Chauffeur = 1,

    /// <summary>Camion — pièce : carte grise. Produit un BAT (Badge d'Accès Terminal).</summary>
    Camion = 2,
}
