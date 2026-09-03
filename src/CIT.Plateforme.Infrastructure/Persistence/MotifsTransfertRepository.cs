using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Files;
using Microsoft.EntityFrameworkCore;

namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>§ 9.10 / § 17.3 — référentiel des motifs de transfert.</summary>
public sealed class MotifTransfertRepository(PlateformeDbContext dbContext) : IMotifTransfertRepository
{
    public async Task<IReadOnlyList<MotifTransfert>> ListerAsync(bool activesUniquement, CancellationToken ct = default)
    {
        var requete = dbContext.MotifsTransfert.AsQueryable();
        if (activesUniquement)
        {
            requete = requete.Where(m => m.EstActif);
        }

        return await requete.OrderBy(m => m.Ordre).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<MotifTransfert?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) =>
        await dbContext.MotifsTransfert.SingleOrDefaultAsync(m => m.Id == id, ct).ConfigureAwait(false);

    public async Task<bool> CodeExisteAsync(string code, CancellationToken ct = default) =>
        await dbContext.MotifsTransfert.AnyAsync(m => m.Code == code, ct).ConfigureAwait(false);

    public async Task<bool> ContientAuMoinsUnMotifActifAsync(CancellationToken ct = default) =>
        await dbContext.MotifsTransfert.AnyAsync(m => m.EstActif, ct).ConfigureAwait(false);

    public async Task<bool> CodeActifExisteAsync(string code, CancellationToken ct = default) =>
        await dbContext.MotifsTransfert.AnyAsync(m => m.EstActif && m.Code == code, ct).ConfigureAwait(false);

    public async Task AjouterAsync(MotifTransfert motif, CancellationToken ct = default) =>
        await dbContext.MotifsTransfert.AddAsync(motif, ct).ConfigureAwait(false);
}
