namespace CIT.Plateforme.Domain.AvoirExoneration;

/// <summary>
/// Décision formelle enregistrée sur une demande d'avoir ou d'exonération (AVE-03).
/// </summary>
public enum DecisionAvoirExo
{
    /// <summary>Demande acceptée par les services habilités (consignation d'accord).</summary>
    Accorde = 1,

    /// <summary>Demande refusée avec motif obligatoire (AVE-03).</summary>
    Rejete = 2
}
