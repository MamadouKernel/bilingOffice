using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Enrolement;
using Microsoft.EntityFrameworkCore;

namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>ENR-01 — référentiel des champs du formulaire d'enrôlement.</summary>
public sealed class ChampFormulaireEnrolementRepository(PlateformeDbContext dbContext) : IChampFormulaireEnrolementRepository
{
    public async Task<IReadOnlyList<ChampFormulaireEnrolement>> ListerAsync(bool activesUniquement, CancellationToken ct = default)
    {
        var requete = dbContext.ChampsFormulaireEnrolement.AsQueryable();
        if (activesUniquement)
        {
            requete = requete.Where(c => c.EstActif);
        }

        return await requete.OrderBy(c => c.Ordre).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<ChampFormulaireEnrolement?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) =>
        await dbContext.ChampsFormulaireEnrolement.SingleOrDefaultAsync(c => c.Id == id, ct).ConfigureAwait(false);

    public async Task<bool> CodeExisteAsync(string code, CancellationToken ct = default) =>
        await dbContext.ChampsFormulaireEnrolement.AnyAsync(c => c.Code == code, ct).ConfigureAwait(false);

    public async Task AjouterAsync(ChampFormulaireEnrolement champ, CancellationToken ct = default) =>
        await dbContext.ChampsFormulaireEnrolement.AddAsync(champ, ct).ConfigureAwait(false);
}
