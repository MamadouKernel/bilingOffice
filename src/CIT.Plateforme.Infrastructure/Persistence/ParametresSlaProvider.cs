using CIT.Plateforme.Application.Supervision;
using CIT.Plateforme.Domain.SuiviTemps;
using CIT.Plateforme.Domain.Tickets;
using Microsoft.EntityFrameworkCore;

namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>Implémentation EF Core de <see cref="IParametresSlaProvider"/> — retourne les
/// paramètres administrés (contrat C-05) pour le module/type du ticket, ou <c>null</c> si aucun
/// SLA n'a été configuré (PC-12).</summary>
public sealed class ParametresSlaProvider(PlateformeDbContext dbContext) : IParametresSlaProvider
{
    public async Task<(ParametresSla Parametres, CalendrierOuvre Calendrier)?> ObtenirPourAsync(Ticket ticket, CancellationToken ct = default)
    {
        var parametre = await dbContext.ParametresSla
            .SingleOrDefaultAsync(p => p.Module == ticket.Module && p.TypeDemande == ticket.TypeDemande, ct)
            .ConfigureAwait(false);

        if (parametre is null)
        {
            return null;
        }

        // REMÉDIATION AUDIT (post-implementation-audit.md §3.B) : le calendrier ouvré réellement
        // administré (§ParametreSlaAdministrable.ConstruireCalendrierOuvre) est désormais utilisé
        // — avant correction, les deux branches de ce test retournaient la même valeur.
        var calendrier = parametre.ConstruireCalendrierOuvre();
        return (parametre.VersParametresSla(), calendrier);
    }
}
