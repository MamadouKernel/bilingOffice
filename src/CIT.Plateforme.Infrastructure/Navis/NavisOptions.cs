namespace CIT.Plateforme.Infrastructure.Navis;

/// <summary>
/// Configuration de la source Navis N4 (Universal Query API). Liée depuis la section
/// <see cref="Section"/> de la configuration.
///
/// Le mot de passe n'est JAMAIS placé dans appsettings : user-secrets en développement,
/// variable d'environnement ou coffre de secrets en production — même règle que les autres
/// secrets du projet.
/// </summary>
public sealed class NavisOptions
{
    public const string Section = "Navis";

    /// <summary>false (défaut) → aucune interrogation réseau : la source répond « indisponible »,
    /// l'assistant reste sur YABA seul.</summary>
    public bool Active { get; init; }

    /// <summary>Racine jusqu'à <c>/apex</c> inclus, ex. <c>https://n4.terminal.ci/apex</c>.</summary>
    public string BaseUrl { get; init; } = string.Empty;

    // Coordonnées de portée exigées par l'Universal Query (scope de la requête).
    public string OperatorId { get; init; } = string.Empty;
    public string ComplexId { get; init; } = string.Empty;
    public string FacilityId { get; init; } = string.Empty;
    public string YardId { get; init; } = string.Empty;

    /// <summary>Utilisateur N4 dédié : rôle <c>n4api</c>, privilège « Universal Query API Access
    /// – Allow », view-only.</summary>
    public string Utilisateur { get; init; } = string.Empty;

    /// <summary>Secret — hors appsettings.</summary>
    public string MotDePasse { get; init; } = string.Empty;

    public int TimeoutSecondes { get; init; } = 10;

    /// <summary>Prête à être interrogée : active, URL et utilisateur renseignés.</summary>
    public bool EstConfiguree =>
        Active
        && !string.IsNullOrWhiteSpace(BaseUrl)
        && !string.IsNullOrWhiteSpace(Utilisateur);
}
