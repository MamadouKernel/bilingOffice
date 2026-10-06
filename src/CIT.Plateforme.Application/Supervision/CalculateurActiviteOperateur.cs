using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Supervision;

/// <summary>
/// §9.9.3 — activité opérateur sur une période : une ligne par opérateur ayant pris en charge ou
/// clôturé au moins un passage dans la fenêtre.
/// </summary>
public sealed record ActiviteOperateur(
    Guid OperateurId,
    int TicketsPrisEnCharge,
    int TicketsTraites,
    TimeSpan DelaiMoyenPriseEnCharge,
    TimeSpan DelaiMoyenTraitement,
    DateTime? DerniereActiviteUtc);

/// <summary>
/// Calcul PUR de l'activité opérateur à partir des passages en file (PEC-03/TPS-01). Finalité :
/// PILOTAGE opérationnel (§9.8), non notation individuelle.
///
/// Le « délai de prise en charge » (arrivée → acceptation) est net de toute attente client. Le
/// « délai de traitement » (acceptation → sortie) est ici BRUT : il peut inclure une attente client
/// si le ticket est resté dans la même file pendant cet état. Raffinement (déduire l'attente client
/// via MesureTemps) = évolution. ponytail: brut pour l'instant, affiner si le pilotage l'exige.
/// </summary>
public static class CalculateurActiviteOperateur
{
    public static IReadOnlyList<ActiviteOperateur> Calculer(
        IEnumerable<PassageEnFile> passages, DateTime debutUtc, DateTime finUtc)
    {
        bool DansFenetre(DateTime d) => d >= debutUtc && d <= finUtc;

        return passages
            .Where(p => p.OperateurId is Guid)
            .GroupBy(p => p.OperateurId!.Value)
            .Select(g =>
            {
                var prisEnCharge = g
                    .Where(p => p.DatePriseEnChargeUtc is DateTime d && DansFenetre(d))
                    .ToList();
                var traites = g
                    .Where(p => p.DateSortieUtc is DateTime d && DansFenetre(d) && p.DatePriseEnChargeUtc is not null)
                    .ToList();

                var delaiPec = prisEnCharge.Count > 0
                    ? Moyenne(prisEnCharge.Select(p => p.DatePriseEnChargeUtc!.Value - p.DateArriveeUtc))
                    : TimeSpan.Zero;
                var delaiTrait = traites.Count > 0
                    ? Moyenne(traites.Select(p => p.DateSortieUtc!.Value - p.DatePriseEnChargeUtc!.Value))
                    : TimeSpan.Zero;

                var activites = prisEnCharge.Select(p => p.DatePriseEnChargeUtc!.Value)
                    .Concat(traites.Select(p => p.DateSortieUtc!.Value))
                    .ToList();
                DateTime? derniere = activites.Count > 0 ? activites.Max() : null;

                return new ActiviteOperateur(
                    g.Key, prisEnCharge.Count, traites.Count, delaiPec, delaiTrait, derniere);
            })
            .Where(l => l.TicketsPrisEnCharge > 0 || l.TicketsTraites > 0)
            .OrderByDescending(l => l.TicketsTraites)
            .ThenByDescending(l => l.TicketsPrisEnCharge)
            .ToList();
    }

    private static TimeSpan Moyenne(IEnumerable<TimeSpan> durees)
    {
        var liste = durees.ToList();
        return TimeSpan.FromTicks(liste.Sum(d => d.Ticks) / liste.Count);
    }
}
