using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Enrolement;
using Microsoft.EntityFrameworkCore;

namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>ENR-01 — valeurs structurées soumises pour les champs du formulaire d'enrôlement.</summary>
public sealed class ValeurChampEnrolementRepository(PlateformeDbContext dbContext) : IValeurChampEnrolementRepository
{
    public async Task AjouterAsync(ValeurChampEnrolement valeur, CancellationToken ct = default) =>
        await dbContext.ValeursChampEnrolement.AddAsync(valeur, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<ValeurChampEnrolement>> ListerParTicketAsync(Guid ticketId, CancellationToken ct = default) =>
        await dbContext.ValeursChampEnrolement.Where(v => v.TicketId == ticketId).ToListAsync(ct).ConfigureAwait(false);
}
