namespace CIT.Plateforme.Domain.Tickets;

/// <summary>
/// Moyen de règlement d'une facture au comptant, saisi par la caisse lors de la confirmation du
/// paiement, avec la référence du reçu ou de la transaction (rapprochement IRIS).
/// </summary>
public enum MoyenPaiement
{
    Especes = 0,
    Wave = 1,
    Cheque = 2,
    VirementBanque = 3,
}
