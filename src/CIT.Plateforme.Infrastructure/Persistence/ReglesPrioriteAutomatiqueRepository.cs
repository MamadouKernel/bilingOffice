using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Tickets;
using Microsoft.EntityFrameworkCore;

namespace CIT.Plateforme.Infrastructure.Persistence;

public sealed class ReglePrioriteAutomatiqueRepository(PlateformeDbContext dbContext) : IReglePrioriteAutomatiqueRepository
{
    public async Task<IReadOnlyList<ReglePrioriteAutomatique>> ListerAsync(bool activesUniquement, CancellationToken ct = default)
    {
        var requete = dbContext.ReglesPrioriteAutomatique.AsQueryable();
        if (activesUniquement)
        {
            requete = requete.Where(r => r.EstActif);
        }

        return await requete.OrderBy(r => r.Ordre).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<ReglePrioriteAutomatique?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) =>
        await dbContext.ReglesPrioriteAutomatique.SingleOrDefaultAsync(r => r.Id == id, ct).ConfigureAwait(false);

    public async Task AjouterAsync(ReglePrioriteAutomatique regle, CancellationToken ct = default) =>
        await dbContext.ReglesPrioriteAutomatique.AddAsync(regle, ct).ConfigureAwait(false);
}
