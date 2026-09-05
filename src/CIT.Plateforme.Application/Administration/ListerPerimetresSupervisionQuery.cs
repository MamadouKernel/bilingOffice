using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Administration;

/// <summary>
/// SUP-05 / CLAR-05 — ce que l'administrateur doit avoir sous les yeux pour administrer un
/// périmètre : ceux qui existent, les files à cocher, et les agents à qui en attribuer un.
///
/// Requête séparée de <see cref="GererPerimetresSupervisionCommand"/> à dessein. La lecture a
/// besoin de l'annuaire des comptes — un port porté par <c>UserManager</c> — alors que l'écriture
/// n'a besoin que du dépôt des périmètres. Les fusionner aurait obligé chaque test de la commande
/// à monter tout l'annuaire d'identité pour vérifier une règle qui ne le concerne pas.
/// </summary>
public sealed class ListerPerimetresSupervisionQuery(
    IPerimetreSupervisionRepository perimetres,
    IFileDeTraitementRepository files,
    IAdministrationComptesRepository comptes,
    IUtilisateurInfoRepository utilisateurs,
    IAutorisationService autorisation)
{
    public async Task<PerimetresSupervisionVue> ExecuterAsync(Guid administrateurId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationPerimetresSupervision, ct: ct).ConfigureAwait(false);

        var tous = await perimetres.ListerAsync(ct).ConfigureAwait(false);
        var infos = await utilisateurs.ObtenirInfosAsync(tous.Select(p => p.SuperviseurId), ct).ConfigureAwait(false);

        var vues = tous
            .Select(p => new PerimetreAdministre(
                p.Id,
                p.SuperviseurId,
                infos.TryGetValue(p.SuperviseurId, out var info) ? (info.NomAffiche ?? info.Email) : p.SuperviseurId.ToString(),
                p.FilesCouvertes,
                p.FilesCouvertes.Count == 0))
            .OrderBy(p => p.SuperviseurNom, StringComparer.CurrentCulture)
            .ToList();

        var toutesLesFiles = await files.ListerAsync(ct).ConfigureAwait(false);
        var agents = await comptes.ListerAgentsAsync(ct).ConfigureAwait(false);

        return new PerimetresSupervisionVue(
            vues,
            [.. toutesLesFiles.OrderBy(f => f.Nom, StringComparer.CurrentCulture).Select(f => (f.Id, f.Nom, f.Module))],
            [.. agents.Where(a => a.EstActif)
                      .OrderBy(a => a.NomAffiche ?? a.Email, StringComparer.CurrentCulture)
                      .Select(a => (a.Id, a.NomAffiche ?? a.Email, a.Email))]);
    }
}
