using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Incidents;
using Microsoft.EntityFrameworkCore;

namespace CIT.Plateforme.Infrastructure.Persistence;

public sealed class CategorieIncidentRepository(PlateformeDbContext context) : ICategorieIncidentRepository
{
    public async Task AjouterAsync(CategorieIncident categorie, CancellationToken ct = default)
    {
        await context.Set<CategorieIncident>().AddAsync(categorie, ct).ConfigureAwait(false);
    }

    public async Task<CategorieIncident?> ObtenirParIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.Set<CategorieIncident>().FindAsync([id], ct).ConfigureAwait(false);
    }

    public async Task<CategorieIncident?> ObtenirParCodeAsync(string code, CancellationToken ct = default)
    {
        return await context.Set<CategorieIncident>()
            .SingleOrDefaultAsync(c => c.Code == code, ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<CategorieIncident>> ListerActivesAsync(CancellationToken ct = default)
    {
        return await context.Set<CategorieIncident>()
            .Where(c => c.EstActif)
            .OrderBy(c => c.Libelle)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<CategorieIncident>> ListerToutesAsync(CancellationToken ct = default)
    {
        return await context.Set<CategorieIncident>()
            .OrderBy(c => c.Code)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
