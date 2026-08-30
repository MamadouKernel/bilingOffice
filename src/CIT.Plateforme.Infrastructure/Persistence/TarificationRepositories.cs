using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Tarification;
using Microsoft.EntityFrameworkCore;

namespace CIT.Plateforme.Infrastructure.Persistence;

public sealed class VersionGrilleTarifaireRepository(PlateformeDbContext dbContext) : IVersionGrilleTarifaireRepository
{
    public async Task<VersionGrilleTarifaire?> ObtenirParIdAsync(Guid id, CancellationToken ct = default)
    {
        return await dbContext.VersionsGrillesTarifaires
            .Include(v => v.Lignes)
            .SingleOrDefaultAsync(v => v.Id == id, ct);
    }

    public async Task<VersionGrilleTarifaire?> ObtenirActiveADateUtcAsync(DateTime dateUtc, CancellationToken ct = default)
    {
        return await dbContext.VersionsGrillesTarifaires
            .Include(v => v.Lignes)
            .Where(v => v.EstActive && v.DateEffetUtc <= dateUtc)
            .OrderByDescending(v => v.DateEffetUtc)
            .ThenByDescending(v => v.DateCreationUtc)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<VersionGrilleTarifaire>> ListerToutesAsync(CancellationToken ct = default)
    {
        return await dbContext.VersionsGrillesTarifaires
            .Include(v => v.Lignes)
            .OrderByDescending(v => v.DateEffetUtc)
            .ToListAsync(ct);
    }

    public async Task AjouterAsync(VersionGrilleTarifaire version, CancellationToken ct = default)
    {
        await dbContext.VersionsGrillesTarifaires.AddAsync(version, ct);
        await dbContext.SaveChangesAsync(ct);
    }
}

public sealed class SimulationProformaRepository(PlateformeDbContext dbContext) : ISimulationProformaRepository
{
    public async Task<SimulationProforma?> ObtenirParIdAsync(Guid id, CancellationToken ct = default)
    {
        return await dbContext.SimulationsProforma
            .Include(s => s.LignesConteneurs)
            .SingleOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<SimulationProforma?> ObtenirParReferenceAsync(string reference, CancellationToken ct = default)
    {
        return await dbContext.SimulationsProforma
            .Include(s => s.LignesConteneurs)
            .SingleOrDefaultAsync(s => s.Reference == reference.Trim(), ct);
    }

    public async Task<IReadOnlyList<SimulationProforma>> ListerParSocieteAsync(Guid societeId, int page, int taillePage, CancellationToken ct = default)
    {
        return await dbContext.SimulationsProforma
            .Include(s => s.LignesConteneurs)
            .Where(s => s.SocieteId == societeId)
            .OrderByDescending(s => s.DateCreationUtc)
            .Skip((page - 1) * taillePage)
            .Take(taillePage)
            .ToListAsync(ct);
    }

    public async Task<int> CompterParSocieteAsync(Guid societeId, CancellationToken ct = default)
    {
        return await dbContext.SimulationsProforma
            .CountAsync(s => s.SocieteId == societeId, ct);
    }

    public async Task AjouterAsync(SimulationProforma simulation, CancellationToken ct = default)
    {
        await dbContext.SimulationsProforma.AddAsync(simulation, ct);
        await dbContext.SaveChangesAsync(ct);
    }
}
