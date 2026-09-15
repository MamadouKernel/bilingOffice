namespace CIT.Plateforme.Application.Securite;

/// <summary>
/// Port d'autorisation — vérifié EXCLUSIVEMENT côté serveur (Principe VII de la constitution,
/// decision D4). Une permission masquée uniquement dans l'interface ne constitue jamais une
/// autorisation suffisante : tout cas d'usage sensible appelle ce service avant d'agir.
/// </summary>
public interface IAutorisationService
{
    /// <summary>Vérifie qu'un utilisateur dispose d'une permission, éventuellement bornée à une
    /// file (rattachement rôle/file paramétrable — SEC-11).</summary>
    Task<bool> EstAutoriseAsync(Guid utilisateurId, string permissionCode, Guid? fileId = null, CancellationToken ct = default);

    /// <summary>Lève <see cref="AccesRefuseException"/> si la permission n'est pas accordée.</summary>
    Task ExigerAsync(Guid utilisateurId, string permissionCode, Guid? fileId = null, CancellationToken ct = default);
}

/// <summary>
/// Levée par <see cref="IAutorisationService.ExigerAsync"/> — jamais contournable côté client,
/// jamais silencieuse (règle métier critique « autorisations »).
///
/// REMÉDIATION UI/UX (D-06) : le <see cref="Exception.Message"/> est ce que l'utilisateur VOIT. Il
/// ne doit donc plus porter l'identifiant technique du compte ni le code de permission — la recette
/// affichait « L'utilisateur 702bfafc-… ne dispose pas de la permission 'supervision.consulter' ».
/// Ces éléments restent utiles au diagnostic : ils sont conservés dans <see cref="DetailTechnique"/>,
/// destiné aux journaux, jamais à l'écran.
/// </summary>
public sealed class AccesRefuseException : Exception
{
    public AccesRefuseException(string message) : base(message)
    {
    }

    public AccesRefuseException(string message, string? detailTechnique) : base(message)
    {
        DetailTechnique = detailTechnique;
    }

    /// <summary>Détail réservé aux journaux — identifiant du compte, code de permission, file.
    /// N'est jamais affiché à l'utilisateur.</summary>
    public string? DetailTechnique { get; }
}
