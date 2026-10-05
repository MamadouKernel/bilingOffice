namespace CIT.Plateforme.Domain.Tickets;

/// <summary>
/// Cas particuliers d'une demande d'import signalés par le client (parcours client Billing Office,
/// documents métier du 05/10/2026). Chacun exige des pièces supplémentaires ; l'enlèvement sous
/// palan (livraison directe au déchargement, sous peine de réembarquement) rend le ticket critique.
/// </summary>
[Flags]
public enum CasParticulierImport
{
    Aucun = 0,

    /// <summary>Fiche d'escorte exigée ; demande d'entreposage exceptionnel le cas échéant.</summary>
    MarchandiseDangereuse = 1,

    /// <summary>Demande d'enlèvement sous palan exigée ; priorité critique.</summary>
    EnlevementSousPalan = 2,

    /// <summary>Réquisition de transfert exigée.</summary>
    VehiculeUsage = 4,
}
