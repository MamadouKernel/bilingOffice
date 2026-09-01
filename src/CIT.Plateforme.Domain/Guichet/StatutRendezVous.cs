namespace CIT.Plateforme.Domain.Guichet;

/// <summary>
/// FEATURE 010 (CON-03, research.md D2) — statuts stricts du cycle de vie d'un rendez-vous au guichet.
/// Strictement 4 états métier : aucun statut intermédiaire ni non documenté.
/// </summary>
public enum StatutRendezVous
{
    /// <summary>Rendez-vous réservé sur un créneau valide, en attente de présentation physique.</summary>
    Reserve,

    /// <summary>Usager accueilli au guichet, présence constatée par l'agent.</summary>
    Honore,

    /// <summary>Rendez-vous annulé avant échéance par l'usager ou l'agent (libère immédiatement le créneau).</summary>
    Annule,

    /// <summary>Créneau échu sans présentation constatée de l'usager.</summary>
    NonPresente
}
