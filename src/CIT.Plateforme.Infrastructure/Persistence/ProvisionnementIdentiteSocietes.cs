using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>
/// REPRISE DE L'IDENTITÉ STRUCTURÉE des sociétés antérieures aux champs « raison sociale » et
/// « RCCM » : chacune est déduite du texte déclaré, une seule fois.
///
/// STRICTEMENT ADDITIF, comme <see cref="ProvisionnementLibellesStatut"/> : une société dont la
/// raison sociale est déjà renseignée n'est jamais réécrite, et un RCCM douteux n'est jamais
/// promu en champ de référence (il reste lisible dans le texte déclaré).
/// </summary>
public sealed class ProvisionnementIdentiteSocietes(
    PlateformeDbContext contexte,
    ILogger<ProvisionnementIdentiteSocietes> journal)
{
    public async Task ExecuterAsync(CancellationToken ct = default)
    {
        var aReprendre = await contexte.Societes
            .Where(s => s.RaisonSociale == null)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var reprises = aReprendre.Count(s => s.ReprendreIdentiteDeclaree());
        if (reprises == 0)
        {
            return;
        }

        await contexte.SaveChangesAsync(ct).ConfigureAwait(false);
        journal.LogInformation(
            "Identité structurée reprise pour {Reprises} société(s) ; {SansRccm} sans RCCM valide à compléter.",
            reprises, aReprendre.Count(s => s.Rccm is null));
    }
}
