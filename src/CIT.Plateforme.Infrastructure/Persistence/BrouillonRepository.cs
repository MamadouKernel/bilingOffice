using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Formulaires;
using Microsoft.EntityFrameworkCore;

namespace CIT.Plateforme.Infrastructure.Persistence;

public sealed class BrouillonRepository(PlateformeDbContext dbContext) : IBrouillonRepository
{
    public async Task<BrouillonSaisie?> ObtenirAsync(Guid utilisateurId, string typeFormulaire, CancellationToken ct = default)
    {
        return await dbContext.BrouillonsSaisie
            .FirstOrDefaultAsync(b => b.UtilisateurId == utilisateurId && b.TypeFormulaire == typeFormulaire, ct)
            .ConfigureAwait(false);
    }

    public async Task AjouterAsync(BrouillonSaisie brouillon, CancellationToken ct = default)
    {
        await dbContext.BrouillonsSaisie.AddAsync(brouillon, ct).ConfigureAwait(false);
    }

    public async Task SupprimerAsync(Guid utilisateurId, string typeFormulaire, CancellationToken ct = default)
    {
        var brouillon = await ObtenirAsync(utilisateurId, typeFormulaire, ct).ConfigureAwait(false);
        if (brouillon is not null)
        {
            dbContext.BrouillonsSaisie.Remove(brouillon);
        }
    }
}
