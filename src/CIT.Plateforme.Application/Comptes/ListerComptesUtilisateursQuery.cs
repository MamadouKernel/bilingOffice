using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Comptes;

/// <summary>Catégorie lisible d'un compte, pour l'annuaire de l'administration.</summary>
public enum CategorieCompte
{
    AgentCit,
    ClientSociete,
    Transporteur,
    ClientSansSociete,
}

/// <summary>Ligne de l'annuaire : identité, catégorie, rattachement et état de sécurité.</summary>
public sealed record CompteUtilisateurVue(
    Guid Id,
    string Email,
    string NomAffiche,
    CategorieCompte Categorie,
    string? Societe,
    bool EstActif,
    bool DeuxFacteursActifs,
    bool InvitationEnAttente);

/// <summary>Lecture de TOUS les comptes (agents, clients, transporteurs), côté infrastructure.</summary>
public interface IAnnuaireComptes
{
    Task<IReadOnlyList<CompteUtilisateurVue>> ListerAsync(CancellationToken ct = default);
}

/// <summary>
/// ANNUAIRE DES COMPTES — l'administration voyait les agents (Comptes agents) et les demandes de
/// société, mais ni les transporteurs ni les collaborateurs clients : un compte pouvait exister
/// sans être visible, ni désactivable, ni dépannable (2FA perdue). Cette vue les montre tous.
/// Réservée à <see cref="Permission.AdministrationComptes"/> : l'annuaire ne doit pas être
/// énumérable par un profil non habilité.
/// </summary>
public sealed class ListerComptesUtilisateursQuery(IAnnuaireComptes annuaire, IAutorisationService autorisation)
{
    public async Task<IReadOnlyList<CompteUtilisateurVue>> ExecuterAsync(Guid appelantId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationComptes, ct: ct).ConfigureAwait(false);
        return await annuaire.ListerAsync(ct).ConfigureAwait(false);
    }
}
