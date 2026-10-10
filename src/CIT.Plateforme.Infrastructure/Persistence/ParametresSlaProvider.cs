using CIT.Plateforme.Application.Supervision;
using CIT.Plateforme.Application.SuiviTemps;
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
        // SLA-01 — le paramétrage discrimine désormais sur la file (l'étape) et la priorité, en
        // plus du module et du type de demande. Plusieurs paramétrages peuvent donc s'appliquer à
        // un même ticket : un SLA général pour le module, un SLA plus serré pour une file donnée.
        //
        // `SingleOrDefaultAsync` levait une exception dans ce cas — c'était correct tant qu'un
        // seul paramétrage était possible, ce n'est plus le cas. Un `null` sur un axe signifie
        // « ce paramétrage ne discrimine pas dessus », il reste donc candidat.
        // RG-SLA-01 (remédiation post-audit final) : la catégorie de client est le sixième axe
        // exigé. Elle se lit sur l'INSTANTANÉ du dossier, pas sur le profil courant de la
        // société — sans quoi un retypage ferait changer le SLA d'un ticket en cours (CPT-06).
        var typeClientDuTicket = await dbContext.Dossiers
            .Where(d => d.Id == ticket.DossierId)
            .Select(d => d.TypeClient)
            .SingleOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var candidats = await dbContext.ParametresSla
            .Where(p => p.Module == ticket.Module
                     && p.TypeDemande == ticket.TypeDemande
                     && (p.FileId == null || p.FileId == ticket.FileCouranteId)
                     && (p.PrioriteId == null || p.PrioriteId == ticket.PrioriteId)
                     && (p.TypeClient == null || p.TypeClient == typeClientDuTicket))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        // Le plus spécifique l'emporte. Le tri se fait en mémoire : `Specificite` est une
        // propriété calculée, non une colonne — et l'ensemble candidat compte au plus huit
        // lignes (les combinaisons de trois axes optionnels).
        var parametre = candidats
            .OrderByDescending(p => p.Specificite)
            .FirstOrDefault();

        if (parametre is null)
        {
            return null;
        }

        // REMÉDIATION AUDIT (post-implementation-audit.md §3.B) : le calendrier ouvré réellement
        // administré (§ParametreSlaAdministrable.ConstruireCalendrierOuvre) est désormais utilisé
        // — avant correction, les deux branches de ce test retournaient la même valeur.
        var calendrier = parametre.ConstruireCalendrierOuvre(JoursFeriesCourants.Actuels);
        return (parametre.VersParametresSla(), calendrier);
    }

    /// <summary>
    /// Variante GROUPÉE : pré-charge en DEUX requêtes le type de client de chaque dossier concerné et
    /// l'ensemble des paramètres SLA, puis applique EN MÉMOIRE la même sélection par spécificité que
    /// <see cref="ObtenirPourAsync"/> pour chaque ticket. Élimine le N+1 (2 requêtes/ticket) du calcul
    /// SLA de supervision, sans réinventer la règle de sélection.
    /// </summary>
    public async Task<IReadOnlyDictionary<Guid, (ParametresSla Parametres, CalendrierOuvre Calendrier)?>> ObtenirPourPlusieursAsync(IReadOnlyList<Ticket> tickets, CancellationToken ct = default)
    {
        var resultat = new Dictionary<Guid, (ParametresSla Parametres, CalendrierOuvre Calendrier)?>();
        if (tickets.Count == 0)
        {
            return resultat;
        }

        var dossierIds = tickets.Select(t => t.DossierId).Distinct().ToList();
        var typeClientParDossier = await dbContext.Dossiers
            .Where(d => dossierIds.Contains(d.Id))
            .Select(d => new { d.Id, d.TypeClient })
            .ToDictionaryAsync(x => x.Id, x => x.TypeClient, ct)
            .ConfigureAwait(false);

        var tousParametres = await dbContext.ParametresSla.ToListAsync(ct).ConfigureAwait(false);

        foreach (var ticket in tickets)
        {
            var typeClientDuTicket = typeClientParDossier.GetValueOrDefault(ticket.DossierId);

            var parametre = tousParametres
                .Where(p => p.Module == ticket.Module
                         && p.TypeDemande == ticket.TypeDemande
                         && (p.FileId == null || p.FileId == ticket.FileCouranteId)
                         && (p.PrioriteId == null || p.PrioriteId == ticket.PrioriteId)
                         && (p.TypeClient == null || p.TypeClient == typeClientDuTicket))
                .OrderByDescending(p => p.Specificite)
                .FirstOrDefault();

            resultat[ticket.Id] = parametre is null
                ? null
                : (parametre.VersParametresSla(), parametre.ConstruireCalendrierOuvre(JoursFeriesCourants.Actuels));
        }

        return resultat;
    }
}
