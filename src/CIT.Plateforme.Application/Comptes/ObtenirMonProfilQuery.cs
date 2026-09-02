using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Comptes;

/// <summary>
/// Profil personnel de l'utilisateur connecté.
/// <para>
/// Anti-IDOR par construction : l'identifiant lu est <c>appelantId</c>, dérivé du contexte
/// authentifié par la couche Web. Aucun paramètre ne permet de désigner un autre utilisateur —
/// il n'existe donc aucun chemin permettant de consulter le profil d'un tiers via ce contrat.
/// </para>
/// </summary>
public sealed record MonProfilVue(
    Guid UtilisateurId,
    string Email,
    string NomAffiche,
    string? Telephone,
    NatureUtilisateur Nature,
    bool EstActif);

public sealed class ObtenirMonProfilQuery(IUtilisateurInfoRepository utilisateurs)
{
    public async Task<MonProfilVue> ExecuterAsync(Guid appelantId, CancellationToken ct = default)
    {
        var profil = await utilisateurs.ObtenirProfilAsync(appelantId, ct).ConfigureAwait(false)
            ?? throw new AccesRefuseException($"Aucun compte actif ne correspond à l'utilisateur {appelantId}.");

        return new MonProfilVue(
            UtilisateurId: profil.Id,
            Email: profil.Email,
            NomAffiche: profil.NomAffiche ?? string.Empty,
            Telephone: profil.Telephone,
            Nature: profil.Nature,
            EstActif: profil.EstActif);
    }
}
