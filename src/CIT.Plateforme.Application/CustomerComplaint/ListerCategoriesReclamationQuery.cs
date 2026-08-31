using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;

namespace CIT.Plateforme.Application.CustomerComplaint;

public sealed record CategorieReclamationVue(Guid Id, string Code, string Libelle);

public sealed class ListerCategoriesReclamationQuery(ICategorieReclamationRepository categorieRepository)
{
    public async Task<IReadOnlyList<CategorieReclamationVue>> ExecuterAsync(CancellationToken ct = default)
    {
        var categories = await categorieRepository.ListerActivesAsync(ct).ConfigureAwait(false);
        
        return categories.Select(c => new CategorieReclamationVue(
            Id: c.Id,
            Code: c.Code,
            Libelle: c.Libelle
        )).ToList();
    }
}
