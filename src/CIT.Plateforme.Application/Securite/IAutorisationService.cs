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

/// <summary>Levée par <see cref="IAutorisationService.ExigerAsync"/> — jamais contournable côté
/// client, jamais silencieuse (règle métier critique « autorisations »).</summary>
public sealed class AccesRefuseException(string message) : Exception(message);
