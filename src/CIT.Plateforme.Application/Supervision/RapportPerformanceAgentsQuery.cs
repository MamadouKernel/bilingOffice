using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Affectation;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Affectation;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Supervision;

/// <summary>Une ligne de rapport de performance par agent, sur la période demandée.</summary>
public sealed record LignePerformanceAgent(
    Guid AgentId,
    string Nom,
    StatutDisponibilite StatutActuel,
    int NombreIndisponibilites,
    TimeSpan TempsIndisponible,
    TimeSpan TempsAbsent,
    TimeSpan TempsDisponible,
    double PourcentageDisponible);

/// <summary>
/// DIS-03 (reporting) — évaluation de la performance des agents : temps et nombre
/// d'indisponibilités sur une période. Lecture seule, réservée à la supervision
/// (<see cref="Permission.SupervisionConsulter"/>).
/// </summary>
public sealed class RapportPerformanceAgentsQuery(
    IDisponibiliteRepository disponibilites,
    IHistoriqueDisponibiliteRepository historiques,
    IUtilisateurInfoRepository utilisateurs,
    IAutorisationService autorisation)
{
    public async Task<IReadOnlyList<LignePerformanceAgent>> ExecuterAsync(
        Guid demandeurId, DateTime debutUtc, DateTime finUtc, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(demandeurId, Permission.SupervisionConsulter, ct: ct).ConfigureAwait(false);

        var dispos = await disponibilites.ListerAsync(ct).ConfigureAwait(false);

        // On charge l'historique depuis le début de la fenêtre pour borner le volume lu. Le statut
        // initial (transition antérieure à débutUtc) se déduit alors du statut courant de l'agent :
        // sans transition dans la fenêtre, l'agent est resté dans son statut présent.
        var toutHistorique = await historiques.ListerDepuisAsync(debutUtc, ct).ConfigureAwait(false);
        var parAgent = toutHistorique.GroupBy(h => h.OperateurId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var noms = await utilisateurs.ObtenirInfosAsync(dispos.Select(d => d.OperateurId), ct).ConfigureAwait(false);

        var lignes = new List<LignePerformanceAgent>();
        foreach (var dispo in dispos)
        {
            var transitions = parAgent.GetValueOrDefault(dispo.OperateurId) ?? [];

            // Statut au début de la fenêtre : le plus ancien AncienStatut connu de la période, ou,
            // à défaut de transition, le statut courant (l'agent n'a pas bougé).
            var statutInitial = transitions.Count > 0
                ? (transitions[0].AncienStatut ?? transitions[0].NouveauStatut)
                : dispo.Statut;

            var serie = new List<(StatutDisponibilite, DateTime)>
            {
                (statutInitial, debutUtc),
            };
            serie.AddRange(transitions.Select(h => (h.NouveauStatut, h.HorodatageUtc)));

            var perf = CalculateurPerformanceAgent.Calculer(serie, debutUtc, finUtc);
            var info = noms.GetValueOrDefault(dispo.OperateurId);

            lignes.Add(new LignePerformanceAgent(
                dispo.OperateurId,
                info?.NomAffiche ?? info?.Email ?? string.Empty,
                dispo.Statut,
                perf.NombreIndisponibilites,
                perf.TempsIndisponible,
                perf.TempsAbsent,
                perf.TempsDisponible,
                perf.PourcentageDisponible));
        }

        return lignes
            .OrderByDescending(l => l.NombreIndisponibilites)
            .ThenByDescending(l => l.TempsIndisponible)
            .ToList();
    }
}
