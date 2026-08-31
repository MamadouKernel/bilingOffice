using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.CustomerComplaint;

/// <summary>
/// T030 (closure-review.md "DÉCISION FINALE T030 / REC-02", CDC §6.19 "Administration
/// fonctionnelle" — catégories de réclamation explicitement listées comme administrables sans
/// développement). Distinct de <see cref="ListerCategoriesReclamationQuery"/> (C7-04, actives
/// uniquement, authentifié) : celle-ci expose aussi les catégories désactivées, réservée à
/// <see cref="Permission.AdministrationCategoriesReclamation"/>.
/// </summary>
public sealed record CategorieAdminVue(Guid Id, string Code, string Libelle, Guid? FileCibleDefautId, bool EstActif);

public sealed class ListerToutesCategoriesReclamationQuery(
    ICategorieReclamationRepository categorieRepository,
    IAutorisationService autorisationService)
{
    public async Task<IReadOnlyList<CategorieAdminVue>> ExecuterAsync(Guid appelantId, CancellationToken ct = default)
    {
        await autorisationService.ExigerAsync(appelantId, Permission.AdministrationCategoriesReclamation, null, ct).ConfigureAwait(false);

        var categories = await categorieRepository.ListerToutesAsync(ct).ConfigureAwait(false);

        return categories.Select(c => new CategorieAdminVue(
            Id: c.Id,
            Code: c.Code,
            Libelle: c.Libelle,
            FileCibleDefautId: c.FileCibleDefautId,
            EstActif: c.EstActif
        )).ToList();
    }
}
