using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Securite;

/// <summary>
/// Ce qu'un appelant obtient sur un ticket IES : l'état des deux branches, <b>et</b> les droits au
/// niveau champ qui bornent ce qu'il peut en faire.
///
/// Les deux voyagent ensemble délibérément. Tant que la requête ne renvoyait que les suivis, aucun
/// écran ne pouvait distinguer « consulter » de « instruire », et la matrice de droits n'avait
/// aucun moyen d'atteindre l'interface.
/// </summary>
public sealed record VueBranchesIes(IReadOnlyList<SuiviBrancheIes> Suivis, DroitsChampsIes Droits)
{
    /// <summary>Branche Manifeste (import) / Back Office (export) — bloc <c>Manifeste</c>.</summary>
    public bool PeutInstruireManifeste => Droits.PeutEcrire(BlocDonneesIes.Manifeste);

    /// <summary>Branche Contrôle Documentaire — bloc <c>Douane</c>.</summary>
    public bool PeutInstruireControleDocumentaire => Droits.PeutEcrire(BlocDonneesIes.Douane);
}

/// <summary>
/// Droits au niveau champ effectivement accordés à un appelant sur un ticket IES. Projection close :
/// un bloc absent est masqué, jamais « inconnu ».
/// </summary>
public sealed class DroitsChampsIes
{
    private readonly IReadOnlyDictionary<BlocDonneesIes, EtatDroitChamp> _parBloc;

    internal DroitsChampsIes(IReadOnlyDictionary<BlocDonneesIes, EtatDroitChamp> parBloc) => _parBloc = parBloc;

    /// <summary>Aucun accès — compte inutilisable, ou aucun rôle IES détenu.</summary>
    public static DroitsChampsIes Aucun { get; } = new(
        Enum.GetValues<BlocDonneesIes>().ToDictionary(b => b, _ => EtatDroitChamp.Masked));

    public EtatDroitChamp Pour(BlocDonneesIes bloc) =>
        _parBloc.TryGetValue(bloc, out var droit) ? droit : EtatDroitChamp.Masked;

    public bool PeutEcrire(BlocDonneesIes bloc) => Pour(bloc) == EtatDroitChamp.AllowWrite;

    public bool PeutLire(BlocDonneesIes bloc) => Pour(bloc) != EtatDroitChamp.Masked;

    /// <summary>Vrai si tous les blocs sont masqués.</summary>
    public bool AucunAcces => _parBloc.Values.All(d => d == EtatDroitChamp.Masked);
}

/// <summary>
/// REMÉDIATION AUDIT FINAL (RG-SEC-01) — LE CÂBLAGE MANQUANT.
///
/// Le CDC exige un contrôle d'accès « y compris au niveau des champs (traitement parallèle IES) ».
/// <see cref="AutorisationChampService"/> portait cette matrice depuis l'origine sans qu'aucun
/// écran ni cas d'usage ne l'appelle : la règle existait, le comportement non.
///
/// Ce service est le pont, et il applique les règles de
/// <c>docs/architecture-autorisation.md</c> dans l'ordre :
/// <list type="number">
///   <item><b>garde d'identité d'abord</b> — un compte inconnu ou désactivé n'obtient aucun droit,
///   avant même qu'on regarde ses habilitations ;</item>
///   <item><b>le rôle se déduit des habilitations</b>, jamais d'une valeur transmise ;</item>
///   <item><b>la décision reste celle de la matrice</b> — aucune règle n'est réécrite ici.</item>
/// </list>
///
/// Un appelant détenant plusieurs rôles obtient, bloc par bloc, le droit le plus permissif de ceux
/// que la matrice lui reconnaît. C'est une composition de la matrice, pas une règle nouvelle : sans
/// cela, un agent habilité sur les deux branches serait moins bien servi que sur une seule.
/// </summary>
public sealed class PorteeChampsIesService(
    GardeIdentiteService garde,
    IAutorisationService autorisation,
    IAutorisationChampService champs)
{
    public async Task<DroitsChampsIes> ResoudreAsync(
        Guid utilisateurId, Guid fileId, ModuleIes module, CancellationToken ct = default)
    {
        // 1. Identité — une identité invalide ou désactivée n'obtient aucun droit (P0-ter).
        if (!await garde.CompteEstUtilisableAsync(utilisateurId, ct).ConfigureAwait(false))
        {
            return DroitsChampsIes.Aucun;
        }

        // 2. Rôles détenus, déduits des seules habilitations.
        var roles = await RolesDetenusAsync(utilisateurId, fileId, module, ct).ConfigureAwait(false);
        if (roles.Count == 0)
        {
            return DroitsChampsIes.Aucun;
        }

        // 3. Décision — la matrice, et rien d'autre.
        var parBloc = Enum.GetValues<BlocDonneesIes>()
            .ToDictionary(
                bloc => bloc,
                bloc => roles.Select(r => champs.EvaluerDroit(bloc, r)).Min());

        return new DroitsChampsIes(parBloc);
    }

    /// <summary>
    /// Les codes de permission qui portent chaque rôle IES. Le parcours export nomme « Back Office »
    /// la branche que l'import nomme « Manifeste » — c'est le même rôle vis-à-vis des blocs.
    /// </summary>
    private async Task<List<RoleIes>> RolesDetenusAsync(
        Guid utilisateurId, Guid fileId, ModuleIes module, CancellationToken ct)
    {
        var (permissionManifeste, permissionControleDocumentaire) = module == ModuleIes.Import
            ? (Permission.BillingImportIesManifesteInstruire, Permission.BillingImportIesControleDocumentaireInstruire)
            : (Permission.BillingExportIesBackOfficeInstruire, Permission.BillingExportIesControleDocumentaireInstruire);

        var permissionFacturation = module == ModuleIes.Import
            ? Permission.BillingImportFacturationEditer
            : Permission.BillingExportFacturationEditer;

        var roles = new List<RoleIes>();

        if (await autorisation.EstAutoriseAsync(utilisateurId, permissionManifeste, fileId, ct).ConfigureAwait(false))
        {
            roles.Add(RoleIes.PiloteManifeste);
        }

        if (await autorisation.EstAutoriseAsync(utilisateurId, permissionControleDocumentaire, fileId, ct).ConfigureAwait(false))
        {
            roles.Add(RoleIes.PiloteControleDocumentaire);
        }

        if (await autorisation.EstAutoriseAsync(utilisateurId, permissionFacturation, fileId, ct).ConfigureAwait(false))
        {
            roles.Add(RoleIes.Facturation);
        }

        if (await autorisation.EstAutoriseAsync(utilisateurId, Permission.SupervisionAgir, fileId, ct).ConfigureAwait(false))
        {
            roles.Add(RoleIes.Superviseur);
        }

        return roles;
    }
}
