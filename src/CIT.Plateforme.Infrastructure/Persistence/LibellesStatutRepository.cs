using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Tickets;
using Microsoft.EntityFrameworkCore;

namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>§11.4 / §9.10 — référentiel des libellés de statut ticket, administrable sans
/// développement. Les entités renvoyées sont suivies par le contexte : une modification suivie
/// d'un <c>IUniteDeTravail.EnregistrerAsync</c> est persistée.</summary>
public sealed class LibellesStatutRepository(PlateformeDbContext dbContext) : ILibelleStatutRepository
{
    public async Task<IReadOnlyList<LibelleStatutTicket>> ListerAsync(CancellationToken ct = default) =>
        await dbContext.LibellesStatutTicket.OrderBy(l => l.Ordre).ToListAsync(ct).ConfigureAwait(false);

    public async Task<LibelleStatutTicket?> ObtenirParCodeAsync(string code, CancellationToken ct = default) =>
        await dbContext.LibellesStatutTicket.SingleOrDefaultAsync(l => l.Code == code, ct).ConfigureAwait(false);
}
