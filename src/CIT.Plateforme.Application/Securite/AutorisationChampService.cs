using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Securite;

/// <summary>
/// Implémentation du moteur d'autorisation RBAC au niveau champ (SEC-02, DEC-017-01).
///
/// REMÉDIATION AUDIT FINAL (RG-SEC-01). Ce service existait, était enregistré dans le conteneur et
/// couvert par deux suites de tests — et <b>n'avait aucun appelant en production</b>. La matrice
/// ci-dessous est donc inchangée : ce qui manquait n'était pas la règle, c'était son câblage.
///
/// Deux changements, et deux seulement :
/// <list type="number">
///   <item>la décision porte désormais sur <see cref="RoleIes"/>, un rôle <b>résolu côté serveur</b>
///   depuis les habilitations par <see cref="PorteeChampsIesService"/> ;</item>
///   <item>la surcharge par chaîne délègue à celle-ci après conversion — une seule matrice, deux
///   points d'entrée.</item>
/// </list>
/// </summary>
public sealed class AutorisationChampService : IAutorisationChampService
{
    public EtatDroitChamp EvaluerDroit(BlocDonneesIes bloc, RoleIes role) =>
        role switch
        {
            RoleIes.Superviseur => EtatDroitChamp.AllowWrite,

            RoleIes.Administrateur => EtatDroitChamp.ReadOnly,

            RoleIes.PiloteManifeste => bloc switch
            {
                BlocDonneesIes.Manifeste => EtatDroitChamp.AllowWrite,
                BlocDonneesIes.Chauffeur => EtatDroitChamp.AllowWrite,
                BlocDonneesIes.Douane => EtatDroitChamp.ReadOnly,
                BlocDonneesIes.Financier => EtatDroitChamp.Masked,
                _ => EtatDroitChamp.Masked
            },

            RoleIes.PiloteControleDocumentaire => bloc switch
            {
                BlocDonneesIes.Douane => EtatDroitChamp.AllowWrite,
                BlocDonneesIes.Manifeste => EtatDroitChamp.ReadOnly,
                BlocDonneesIes.Chauffeur => EtatDroitChamp.ReadOnly,
                BlocDonneesIes.Financier => EtatDroitChamp.Masked,
                _ => EtatDroitChamp.Masked
            },

            RoleIes.Facturation => bloc switch
            {
                BlocDonneesIes.Financier => EtatDroitChamp.AllowWrite,
                BlocDonneesIes.Manifeste => EtatDroitChamp.ReadOnly,
                BlocDonneesIes.Douane => EtatDroitChamp.ReadOnly,
                BlocDonneesIes.Chauffeur => EtatDroitChamp.Masked,
                _ => EtatDroitChamp.Masked
            },

            _ => EtatDroitChamp.Masked // Fail-secure par défaut
        };

    public EtatDroitChamp EvaluerDroit(BlocDonneesIes bloc, string? roleUtilisateur) =>
        EvaluerDroit(bloc, ConvertirRole(roleUtilisateur));

    /// <summary>
    /// Conversion des noms historiques. Tout nom non reconnu — y compris vide ou nul — donne
    /// <see cref="RoleIes.Aucun"/>, donc <see cref="EtatDroitChamp.Masked"/> sur tous les blocs.
    /// </summary>
    private static RoleIes ConvertirRole(string? roleUtilisateur) =>
        roleUtilisateur switch
        {
            "Superviseur" => RoleIes.Superviseur,
            "Administrateur" => RoleIes.Administrateur,
            "PiloteManifeste" => RoleIes.PiloteManifeste,
            "PiloteCd" => RoleIes.PiloteControleDocumentaire,
            "Facturation" or "Caisse" => RoleIes.Facturation,
            _ => RoleIes.Aucun,
        };
}
