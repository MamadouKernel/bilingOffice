using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.SuiviTemps;
using Microsoft.EntityFrameworkCore;

namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>SLA-04 — référentiel des jours fériés exclus du calcul des délais SLA.</summary>
public sealed class JourFerieRepository(PlateformeDbContext dbContext) : IJourFerieRepository
{
    public async Task<IReadOnlyList<JourFerie>> ListerAsync(bool activesUniquement, CancellationToken ct = default)
    {
        var requete = dbContext.JoursFeries.AsQueryable();
        if (activesUniquement)
        {
            requete = requete.Where(j => j.EstActif);
        }

        return await requete.OrderBy(j => j.Date).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<JourFerie?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) =>
        await dbContext.JoursFeries.SingleOrDefaultAsync(j => j.Id == id, ct).ConfigureAwait(false);

    public async Task<bool> DateExisteAsync(DateOnly date, CancellationToken ct = default) =>
        await dbContext.JoursFeries.AnyAsync(j => j.Date == date, ct).ConfigureAwait(false);

    public async Task AjouterAsync(JourFerie jour, CancellationToken ct = default) =>
        await dbContext.JoursFeries.AddAsync(jour, ct).ConfigureAwait(false);
}
