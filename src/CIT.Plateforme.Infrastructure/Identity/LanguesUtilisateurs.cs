using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CIT.Plateforme.Infrastructure.Identity;

/// <summary>Langue préférée lue sur le compte (enregistrée quand l'utilisateur change de langue).</summary>
public sealed class LanguesUtilisateurs(PlateformeDbContext contexte) : ILanguesUtilisateurs
{
    public async Task<bool> EstAnglophoneAsync(Guid utilisateurId, CancellationToken ct = default) =>
        await contexte.Users.AsNoTracking()
            .Where(u => u.Id == utilisateurId)
            .Select(u => u.LanguePreferee)
            .FirstOrDefaultAsync(ct).ConfigureAwait(false) == "en";
}
