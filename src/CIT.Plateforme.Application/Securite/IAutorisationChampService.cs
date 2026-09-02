using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Securite;

/// <summary>
/// Service d'évaluation des droits au niveau champ pour les traitements parallèles IES (SEC-02).
/// </summary>
public interface IAutorisationChampService
{
    EtatDroitChamp EvaluerDroit(BlocDonneesIes bloc, string? roleUtilisateur);
}
