using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Incidents;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.SupportTechnique;

public sealed record CreerCategorieIncidentRequete(Guid AppelantId, string Code, string Libelle, Guid? FileCibleDefautId);

/// <summary>Création d'une catégorie d'incident — miroir de <c>CreerCategorieReclamationCommand</c>.
/// La file cible, si fournie, doit appartenir au module Support IT.</summary>
public sealed class CreerCategorieIncidentCommand(
    ICategorieIncidentRepository categorieRepository,
    IFileDeTraitementRepository fileRepository,
    IAutorisationService autorisationService,
    IJournalisationService journalisationService,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<Guid> ExecuterAsync(CreerCategorieIncidentRequete requete, CancellationToken ct = default)
    {
        await autorisationService.ExigerAsync(requete.AppelantId, Permission.AdministrationCategoriesIncident, null, ct).ConfigureAwait(false);

        var existante = await categorieRepository.ObtenirParCodeAsync(requete.Code, ct).ConfigureAwait(false);
        if (existante is not null)
        {
            throw new InvalidOperationException($"Une catégorie d'incident avec le code '{requete.Code}' existe déjà.");
        }

        await ExigerFileCibleEligibleAsync(fileRepository, requete.FileCibleDefautId, ct).ConfigureAwait(false);

        var categorie = CategorieIncident.Creer(requete.Code, requete.Libelle, requete.FileCibleDefautId);
        await categorieRepository.AjouterAsync(categorie, ct).ConfigureAwait(false);

        await journalisationService.ConsignerAuditAsync(
            requete.AppelantId, "CategorieIncidentCreee", nameof(CategorieIncident), categorie.Id, null, null, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
        return categorie.Id;
    }

    /// <summary>Une catégorie d'incident ne peut router que vers une file de son propre module.</summary>
    internal static async Task ExigerFileCibleEligibleAsync(IFileDeTraitementRepository fileRepository, Guid? fileCibleId, CancellationToken ct)
    {
        if (!fileCibleId.HasValue)
        {
            return;
        }

        var file = await fileRepository.ObtenirParIdAsync(fileCibleId.Value, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("La file cible sélectionnée n'existe pas.");

        if (file.Module != SupportTechnique.Module)
        {
            throw new InvalidOperationException("La file cible d'une catégorie d'incident doit appartenir au module Support IT.");
        }
    }
}

public sealed record ModifierCategorieIncidentRequete(Guid AppelantId, Guid CategorieId, string Libelle, Guid? FileCibleDefautId, bool EstActive);

/// <summary>Modification d'une catégorie d'incident (libellé, file cible, activation).</summary>
public sealed class ModifierCategorieIncidentCommand(
    ICategorieIncidentRepository categorieRepository,
    IFileDeTraitementRepository fileRepository,
    IAutorisationService autorisationService,
    IJournalisationService journalisationService,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(ModifierCategorieIncidentRequete requete, CancellationToken ct = default)
    {
        await autorisationService.ExigerAsync(requete.AppelantId, Permission.AdministrationCategoriesIncident, null, ct).ConfigureAwait(false);

        var categorie = await categorieRepository.ObtenirParIdAsync(requete.CategorieId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"La catégorie d'incident {requete.CategorieId} n'existe pas.");

        await CreerCategorieIncidentCommand.ExigerFileCibleEligibleAsync(fileRepository, requete.FileCibleDefautId, ct).ConfigureAwait(false);

        categorie.MettreAJour(requete.Libelle, requete.FileCibleDefautId);
        if (requete.EstActive)
        {
            categorie.Activer();
        }
        else
        {
            categorie.Desactiver();
        }

        await journalisationService.ConsignerAuditAsync(
            requete.AppelantId, "CategorieIncidentModifiee", nameof(CategorieIncident), categorie.Id, null, null, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}

/// <summary>Catégories actives — alimente le formulaire de déclaration. Aucune permission (comme la
/// liste des catégories de réclamation côté client).</summary>
public sealed class ListerCategoriesIncidentQuery(ICategorieIncidentRepository categorieRepository)
{
    public Task<IReadOnlyList<CategorieIncident>> ExecuterAsync(CancellationToken ct = default) =>
        categorieRepository.ListerActivesAsync(ct);
}

/// <summary>Toutes les catégories (actives et désactivées) — écran d'administration.</summary>
public sealed class ListerToutesCategoriesIncidentQuery(
    ICategorieIncidentRepository categorieRepository,
    IAutorisationService autorisationService)
{
    public async Task<IReadOnlyList<CategorieIncident>> ExecuterAsync(Guid appelantId, CancellationToken ct = default)
    {
        await autorisationService.ExigerAsync(appelantId, Permission.AdministrationCategoriesIncident, null, ct).ConfigureAwait(false);
        return await categorieRepository.ListerToutesAsync(ct).ConfigureAwait(false);
    }
}
