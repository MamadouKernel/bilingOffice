using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Securite;

/// <summary>
/// Implémentation du moteur d'autorisation RBAC au niveau champ (SEC-02, DEC-017-01).
/// </summary>
public sealed class AutorisationChampService : IAutorisationChampService
{
    public EtatDroitChamp EvaluerDroit(BlocDonneesIes bloc, string? roleUtilisateur)
    {
        if (string.IsNullOrWhiteSpace(roleUtilisateur))
        {
            return EtatDroitChamp.Masked; // Fail-secure par défaut
        }

        return roleUtilisateur switch
        {
            "Superviseur" => EtatDroitChamp.AllowWrite,

            "Administrateur" => EtatDroitChamp.ReadOnly,

            "PiloteManifeste" => bloc switch
            {
                BlocDonneesIes.Manifeste => EtatDroitChamp.AllowWrite,
                BlocDonneesIes.Chauffeur => EtatDroitChamp.AllowWrite,
                BlocDonneesIes.Douane => EtatDroitChamp.ReadOnly,
                BlocDonneesIes.Financier => EtatDroitChamp.Masked,
                _ => EtatDroitChamp.Masked
            },

            "PiloteCd" => bloc switch
            {
                BlocDonneesIes.Douane => EtatDroitChamp.AllowWrite,
                BlocDonneesIes.Manifeste => EtatDroitChamp.ReadOnly,
                BlocDonneesIes.Chauffeur => EtatDroitChamp.ReadOnly,
                BlocDonneesIes.Financier => EtatDroitChamp.Masked,
                _ => EtatDroitChamp.Masked
            },

            "Facturation" or "Caisse" => bloc switch
            {
                BlocDonneesIes.Financier => EtatDroitChamp.AllowWrite,
                BlocDonneesIes.Manifeste => EtatDroitChamp.ReadOnly,
                BlocDonneesIes.Douane => EtatDroitChamp.ReadOnly,
                BlocDonneesIes.Chauffeur => EtatDroitChamp.Masked,
                _ => EtatDroitChamp.Masked
            },

            _ => EtatDroitChamp.Masked // Fail-secure par défaut
        };
    }
}
