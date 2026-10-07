using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Affectation;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Supervision;

/// <summary>Contrat C-24 (vue par opérateur). CDC : SUP-03, SUP-05, DIS-02.</summary>
/// <summary>Le nom affiché de l'opérateur accompagne son identifiant : l'écran de supervision
/// présente une personne, pas un identifiant technique.</summary>
public sealed record VueOperateur(Guid OperateurId, StatutDisponibilite Disponibilite, int TicketsEnCours, DateTime DerniereActiviteUtc, string NomOperateur = "");

public sealed class VueChargeQuery(
    IDisponibiliteRepository disponibilites,
    ITicketRepository tickets,
    IAutorisationService autorisation,
    IPerimetreSupervisionRepository perimetres,
    IFileDeTraitementRepository files,
    IUtilisateurInfoRepository? utilisateurs = null)
{
    public async Task<IReadOnlyList<VueOperateur>> ExecuterAsync(Guid superviseurId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(superviseurId, Permission.SupervisionConsulter, ct: ct).ConfigureAwait(false);

        // BORNÉ AU PÉRIMÈTRE (SEC-02/03, cohérent avec VueFilesQuery et le rapport d'activité) : la
        // charge ne montre que les opérateurs qui traitent les files du périmètre du superviseur. Sans
        // périmètre défini, aucun opérateur — comme « aucune file dans votre périmètre ».
        var perimetre = await perimetres.ObtenirParSuperviseurAsync(superviseurId, ct).ConfigureAwait(false);
        if (perimetre is null)
        {
            return [];
        }

        var filesDuPerimetre = (await files.ListerAsync(ct).ConfigureAwait(false))
            .Where(f => perimetre.CouvreLaFile(f.Id))
            .Select(f => f.Id)
            .ToHashSet();
        if (filesDuPerimetre.Count == 0)
        {
            return [];
        }

        var toutesDisponibilites = await disponibilites.ListerAsync(ct).ConfigureAwait(false);
        var avecTickets = await tickets.ListerOperateursAvecTicketsEnCoursAsync(ct).ConfigureAwait(false);
        var operateurIds = toutesDisponibilites.Select(d => d.OperateurId).Union(avecTickets).ToList();

        var infos = utilisateurs is null
            ? new Dictionary<Guid, UtilisateurInfo>()
            : await utilisateurs.ObtenirInfosAsync(operateurIds, ct).ConfigureAwait(false);
        var ticketsParOperateur = await tickets.ListerParOperateursAsync(operateurIds, ct).ConfigureAwait(false);
        var dispoParOperateur = toutesDisponibilites.ToDictionary(d => d.OperateurId);

        var vues = new List<VueOperateur>();
        foreach (var operateurId in operateurIds)
        {
            // Seuls les tickets dans les files du périmètre comptent — un opérateur qui ne traite
            // aucune file du périmètre n'apparaît pas (cloisonnement).
            var ticketsPerimetre = (ticketsParOperateur.GetValueOrDefault(operateurId) ?? [])
                .Where(t => filesDuPerimetre.Contains(t.FileCouranteId))
                .ToList();
            if (ticketsPerimetre.Count == 0)
            {
                continue;
            }

            var dispo = dispoParOperateur.GetValueOrDefault(operateurId);
            var info = infos.GetValueOrDefault(operateurId);
            vues.Add(new VueOperateur(
                operateurId,
                dispo?.Statut ?? StatutDisponibilite.Disponible,
                ticketsPerimetre.Count(t => t.Statut == StatutTicket.EnCours),
                dispo?.DateDerniereActiviteUtc ?? ticketsPerimetre.Select(t => t.DateDerniereActiviteUtc).DefaultIfEmpty().Max(),
                NomOperateur: info?.NomAffiche ?? info?.Email ?? string.Empty));
        }

        return vues;
    }
}
