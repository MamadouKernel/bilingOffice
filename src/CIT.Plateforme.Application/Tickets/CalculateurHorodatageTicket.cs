using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

/// <summary>Minutes tenues par un opérateur sur une étape (temps de traitement cumulé de ses passages).</summary>
public sealed record PorteurEtape(Guid OperateurId, TimeSpan TempsTraitement, int NombrePassages);

/// <summary>
/// TPS-01 / TPS-05 — temps passé par un ticket sur UNE étape (une file), tous passages confondus.
/// Une remise en file réaffecte le ticket à la même file : les passages successifs dans cette file
/// sont agrégés ici, mais <see cref="NombrePassages"/> garde la remise visible et <see cref="Porteurs"/>
/// ventile le traitement par opérateur.
/// </summary>
public sealed record EtapeHorodatage(
    Guid FileId,
    int NombrePassages,
    TimeSpan TempsAttente,
    TimeSpan TempsTraitement,
    bool EnCours,
    IReadOnlyList<PorteurEtape> Porteurs)
{
    public TimeSpan Total => TempsAttente + TempsTraitement;
}

/// <summary>
/// Horodatage reconstruit d'un ticket : temps total, décomposition par étape, étape goulot. IDs bruts ;
/// la résolution des libellés (file, opérateur) relève de la couche requête.
/// </summary>
public sealed record HorodatageTicket(
    TimeSpan TempsTotal,
    TimeSpan TempsAttenteTotal,
    TimeSpan TempsTraitementTotal,
    Guid? FileGoulotId,
    bool EnCours,
    IReadOnlyList<EtapeHorodatage> Etapes);

/// <summary>
/// Reconstruit PUREMENT l'horodatage d'un ticket à partir de ses passages en file (TPS-01/TPS-05),
/// sans table dédiée : le volume par ticket est borné (quelques passages) et la donnée brute
/// <see cref="PassageEnFile"/> est déjà persistée. Même parti pris que
/// <see cref="Supervision.CalculateurActiviteOperateur"/> : calcul sur la donnée brute, pas de
/// duplication de la capture.
///
/// Le temps de traitement d'une étape peut inclure une attente client restée dans la même file ;
/// raffinement via MesureTemps = évolution. ponytail: brut pour l'instant, affiner si le pilotage l'exige.
/// </summary>
public static class CalculateurHorodatageTicket
{
    public static HorodatageTicket Reconstruire(IEnumerable<PassageEnFile> passages, DateTime maintenantUtc)
    {
        var liste = passages.ToList();
        if (liste.Count == 0)
        {
            return new HorodatageTicket(TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, null, false, []);
        }

        var etapes = liste
            .GroupBy(p => p.FileId)
            .Select(g => (
                RangMin: g.Min(p => p.Rang),
                Etape: new EtapeHorodatage(
                    g.Key,
                    g.Count(),
                    Somme(g.Select(p => p.TempsAttente(maintenantUtc))),
                    Somme(g.Select(p => p.TempsTraitement(maintenantUtc))),
                    g.Any(p => p.EstEnCours),
                    g.Where(p => p.OperateurId is Guid)
                        .GroupBy(p => p.OperateurId!.Value)
                        .Select(pg => new PorteurEtape(
                            pg.Key,
                            Somme(pg.Select(p => p.TempsTraitement(maintenantUtc))),
                            pg.Count()))
                        .OrderByDescending(x => x.TempsTraitement)
                        .ToList())))
            .OrderBy(x => x.RangMin) // chronologie des étapes (TPS-05)
            .Select(x => x.Etape)
            .ToList();

        var attenteTotal = Somme(etapes.Select(e => e.TempsAttente));
        var traitementTotal = Somme(etapes.Select(e => e.TempsTraitement));

        // Goulot : l'étape au temps total le plus élevé. Null si aucune étape n'a consommé de temps.
        var goulot = etapes
            .Where(e => e.Total > TimeSpan.Zero)
            .OrderByDescending(e => e.Total)
            .FirstOrDefault();

        return new HorodatageTicket(
            attenteTotal + traitementTotal,
            attenteTotal,
            traitementTotal,
            goulot?.FileId,
            liste.Any(p => p.EstEnCours),
            etapes);
    }

    private static TimeSpan Somme(IEnumerable<TimeSpan> durees) =>
        durees.Aggregate(TimeSpan.Zero, (acc, d) => acc + d);
}
