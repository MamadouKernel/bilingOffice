using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Reclamations;
using Microsoft.EntityFrameworkCore;

namespace CIT.Plateforme.Infrastructure.Persistence;

public sealed class CategorieReclamationRepository(PlateformeDbContext context) : ICategorieReclamationRepository
{
    public async Task AjouterAsync(CategorieReclamation categorie, CancellationToken ct = default)
    {
        await context.Set<CategorieReclamation>().AddAsync(categorie, ct).ConfigureAwait(false);
    }

    public async Task<CategorieReclamation?> ObtenirParIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.Set<CategorieReclamation>().FindAsync([id], ct).ConfigureAwait(false);
    }

    public async Task<CategorieReclamation?> ObtenirParCodeAsync(string code, CancellationToken ct = default)
    {
        return await context.Set<CategorieReclamation>()
            .SingleOrDefaultAsync(c => c.Code == code, ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<CategorieReclamation>> ListerActivesAsync(CancellationToken ct = default)
    {
        return await context.Set<CategorieReclamation>()
            .Where(c => c.EstActif)
            .OrderBy(c => c.Libelle)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<CategorieReclamation>> ListerToutesAsync(CancellationToken ct = default)
    {
        return await context.Set<CategorieReclamation>()
            .OrderBy(c => c.Code)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
