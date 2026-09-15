using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Securite;

/// <summary>
/// Service d'évaluation des droits au niveau champ pour les traitements parallèles IES (SEC-02).
/// </summary>
public interface IAutorisationChampService
{
    /// <summary>
    /// Évalue le droit d'un rôle IES sur un bloc de données. C'est <b>la</b> matrice de droits du
    /// CDC (IMP-40/IMP-41) : il n'en existe pas d'autre, et aucun appelant ne réinterprète son
    /// résultat.
    /// </summary>
    EtatDroitChamp EvaluerDroit(BlocDonneesIes bloc, RoleIes role);

    /// <summary>
    /// Surcharge historique par nom de rôle. CONSERVÉE POUR LES APPELANTS EXISTANTS, jamais
    /// utilisée dans le chemin production : un nom de rôle est une donnée d'entrée, et une donnée
    /// d'entrée ne fonde pas une décision d'autorisation (règle 1 de
    /// <c>docs/architecture-autorisation.md</c>). Un nom inconnu reste masqué.
    /// </summary>
    EtatDroitChamp EvaluerDroit(BlocDonneesIes bloc, string? roleUtilisateur);
}
