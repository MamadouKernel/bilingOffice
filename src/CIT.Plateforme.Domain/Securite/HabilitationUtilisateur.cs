namespace CIT.Plateforme.Domain.Securite;

/// <summary>
/// Attribution d'une permission à un utilisateur, éventuellement bornée à une file (rattachement
/// rôle/file paramétrable — SEC-11, WKF-01) ou laissée globale. Aucun rôle métier n'est nommé :
/// c'est la brique de base du modèle configurable (decision D4, contrat C-03).
/// </summary>
public sealed class HabilitationUtilisateur
{
    public Guid Id { get; private set; }

    public Guid UtilisateurId { get; private set; }

    public string PermissionCode { get; private set; } = string.Empty;

    /// <summary>Restreint la permission à une file donnée ; null = permission globale.</summary>
    public Guid? FileId { get; private set; }

    private HabilitationUtilisateur() { }

    public static HabilitationUtilisateur Attribuer(Guid utilisateurId, string permissionCode, Guid? fileId = null) => new()
    {
        Id = Guid.NewGuid(),
        UtilisateurId = utilisateurId,
        PermissionCode = permissionCode,
        FileId = fileId,
    };
}
