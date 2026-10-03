namespace CIT.Plateforme.Domain.Comptes;

/// <summary>
/// Profil du compte société, déclaré à la création et confirmé par CIT (CPT-02). Quatre valeurs
/// fermées par le CDC — aucune évolutivité demandée (research.md D3). L'usage de cette valeur pour
/// orienter un workflow Billing Office est hors périmètre de cette feature.
/// </summary>
public enum TypeClient
{
    Normal,
    EnCompte,
    Deposit,
    IES,

    /// <summary>
    /// Entreprise de transport (ou transporteur indépendant), validée par CIT comme toute société.
    /// N'a PAS de parcours Billing Office : elle enrôle sa flotte (chauffeurs, camions) auprès de la
    /// Gate. Décision du 03/10/2026 — remplace l'auto-inscription publique des transporteurs.
    /// </summary>
    Transporteur,
}
