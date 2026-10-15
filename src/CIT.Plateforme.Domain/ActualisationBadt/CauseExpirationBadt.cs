namespace CIT.Plateforme.Domain.ActualisationBadt;

/// <summary>
/// EXTENSION FEATURE 010 (Actualisation BADT) : qualification de la cause de l'expiration d'un BADT,
/// posée par l'agent Support. Elle commande la branche du workflow :
/// <list type="bullet">
///   <item><see cref="NonTerminale"/> → rejet par l'agent, sans escalade ni actualisation ;</item>
///   <item><see cref="Terminale"/> → escalade obligatoire vers le Décideur Actualisateur BADT,
///   seul habilité à accorder.</item>
/// </list>
/// </summary>
public enum CauseExpirationBadt
{
    /// <summary>Cause imputable au terminal (TC introuvable par CIT, panne système…) : escalade obligatoire.</summary>
    Terminale,

    /// <summary>Cause non imputable au terminal (chauffeur absent au chargement…) : rejet par l'agent.</summary>
    NonTerminale,
}
