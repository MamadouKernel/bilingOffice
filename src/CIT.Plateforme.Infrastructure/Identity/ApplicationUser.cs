using CIT.Plateforme.Domain.Securite;
using Microsoft.AspNetCore.Identity;

namespace CIT.Plateforme.Infrastructure.Identity;

/// <summary>
/// Compte ASP.NET Core Identity (authentification uniquement — decision D4). L'autorisation
/// repose sur <see cref="HabilitationUtilisateur"/>, jamais sur les rôles Identity, pour rester
/// paramétrable (PC-03 non résolu : aucun rôle métier n'est nommé ici).
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string NomAffiche { get; set; } = string.Empty;

    public NatureUtilisateur Nature { get; set; }

    public bool EstActif { get; set; } = true;

    /// <summary>Langue choisie dans l'interface (« fr » / « en ») : celle des notifications et e-mails.</summary>
    public string? LanguePreferee { get; set; }
}
