using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.CustomerComplaint;

/// <summary>Une file éligible comme destination d'une catégorie de réclamation.</summary>
public sealed record FileCibleReclamationVue(Guid Id, string Nom);

/// <summary>
/// REMÉDIATION RECETTE UAT (D-14) — LES FILES ÉLIGIBLES, ÉNUMÉRÉES PAR LE SERVEUR.
///
/// Le routage d'une réclamation était déjà résolu côté serveur : la catégorie porte sa file cible,
/// et <see cref="SoumettreReclamationCommand"/> la lit. Ce qui manquait, c'était le moyen de
/// <b>configurer</b> ce lien : l'écran d'administration ne proposait aucun choix de file, si bien
/// que toute catégorie créée par l'interface naissait sans destination et refusait ensuite chaque
/// soumission. Le module Customer Complaint était donc inatteignable sur une installation neuve.
///
/// L'administrateur choisit une file <b>par son nom</b>, dans une liste que le serveur établit. À
/// aucun moment l'interface ne compose ni ne saisit un identifiant : elle renvoie l'un des choix
/// qu'elle a reçus, et la commande revérifie ce choix.
///
/// Le filtre par module n'est pas cosmétique : router une réclamation vers une file de facturation
/// import placerait le ticket dans une file dont les instructeurs n'ont ni le contexte ni les
/// écrans pour la traiter.
/// </summary>
public sealed class ListerFilesCibleReclamationQuery(
    IFileDeTraitementRepository fileRepository,
    IAutorisationService autorisationService)
{
    public async Task<IReadOnlyList<FileCibleReclamationVue>> ExecuterAsync(
        Guid appelantId, CancellationToken ct = default)
    {
        await autorisationService
            .ExigerAsync(appelantId, Permission.AdministrationCategoriesReclamation, null, ct)
            .ConfigureAwait(false);

        var files = await fileRepository.ListerAsync(ct).ConfigureAwait(false);

        return files
            .Where(f => string.Equals(f.Module, SoumettreReclamationCommand.Module, StringComparison.Ordinal))
            .OrderBy(f => f.Nom, StringComparer.CurrentCulture)
            .Select(f => new FileCibleReclamationVue(f.Id, f.Nom))
            .ToList();
    }
}
