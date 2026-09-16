using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Assistant;
using Microsoft.EntityFrameworkCore;

namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>IA-04 / §17.3 — référentiel des sujets de l'assistant conversationnel.</summary>
public sealed class SujetAssistantRepository(PlateformeDbContext dbContext) : ISujetAssistantRepository
{
    public async Task<IReadOnlyList<SujetAssistant>> ListerAsync(bool activesUniquement, CancellationToken ct = default)
    {
        var requete = dbContext.SujetsAssistant.AsQueryable();
        if (activesUniquement)
        {
            requete = requete.Where(s => s.EstActif);
        }

        return await requete.OrderBy(s => s.Ordre).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<SujetAssistant?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) =>
        await dbContext.SujetsAssistant.SingleOrDefaultAsync(s => s.Id == id, ct).ConfigureAwait(false);

    public async Task AjouterAsync(SujetAssistant sujet, CancellationToken ct = default) =>
        await dbContext.SujetsAssistant.AddAsync(sujet, ct).ConfigureAwait(false);
}
