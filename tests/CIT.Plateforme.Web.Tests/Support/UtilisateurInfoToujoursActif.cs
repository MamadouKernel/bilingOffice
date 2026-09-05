using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Web.Tests.Support;

/// <summary>
/// Dépôt d'infos décrivant tout identifiant non vide comme un compte ACTIF.
///
/// Nécessaire depuis P2-06 : les requêtes de notification consultent la garde d'identité. Les
/// suites qui composent leur conteneur à la main doivent donc décrire un compte utilisable, sans
/// quoi elles éprouveraient un refus au lieu du scénario qu'elles décrivent.
///
/// L'ÉTAT DES COMPTES N'EST PAS ÉPROUVÉ ICI — il l'est par
/// <c>GardeIdentiteSurfacesPersonnellesTests</c>, sur une base réelle.
/// </summary>
public sealed class UtilisateurInfoToujoursActif : IUtilisateurInfoRepository
{
    private static UtilisateurInfo Info(Guid id) =>
        new(id, "u@test", "u@test", "Utilisateur", true, NatureUtilisateur.Client);

    public Task<UtilisateurInfo?> ObtenirInfoAsync(Guid utilisateurId, CancellationToken ct = default) =>
        Task.FromResult<UtilisateurInfo?>(utilisateurId == Guid.Empty ? null : Info(utilisateurId));

    public Task<IReadOnlyDictionary<Guid, UtilisateurInfo>> ObtenirInfosAsync(
        IEnumerable<Guid> utilisateursIds, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyDictionary<Guid, UtilisateurInfo>>(
            utilisateursIds.Distinct().ToDictionary(id => id, Info));

    public Task<UtilisateurInfo?> ObtenirParEmailAsync(string email, CancellationToken ct = default) =>
        Task.FromResult<UtilisateurInfo?>(null);

    public Task<ProfilUtilisateur?> ObtenirProfilAsync(Guid utilisateurId, CancellationToken ct = default) =>
        Task.FromResult<ProfilUtilisateur?>(null);

    public Task<bool> MettreAJourProfilAsync(
        Guid utilisateurId, string nomAffiche, string? telephone, CancellationToken ct = default) =>
        Task.FromResult(true);
}
