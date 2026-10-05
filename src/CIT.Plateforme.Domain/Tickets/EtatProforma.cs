namespace CIT.Plateforme.Domain.Tickets;

/// <summary>
/// Proforma d'un ticket Billing IMPORT / EXPORT (parcours client TO BE, documents métier du
/// 05/10/2026) : le Pilote Facturation la dépose dans le ticket, le client la valide ou demande
/// une correction, et seule une proforma validée ouvre l'édition de la facture définitive.
/// </summary>
public enum EtatProforma
{
    Aucune = 0,

    /// <summary>Déposée par le Pilote Facturation, en attente de la réponse du client (SLA suspendu).</summary>
    EnAttenteValidation = 1,

    /// <summary>Le client a demandé une correction (motif, éventuellement nouvelle date d'enlèvement).</summary>
    CorrectionDemandee = 2,

    /// <summary>Acceptée par le client : la facture définitive peut être éditée.</summary>
    Validee = 3,
}
