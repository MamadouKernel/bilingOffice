namespace CIT.Plateforme.Domain.SuiviTemps;

/// <summary>
/// Calendrier ouvré paramétrable (SLA-04, decision D7) — horaires par jour, jours fériés, aucune
/// valeur imposée ici (Principe VI/XIII). Le moteur de calcul est une classe de domaine pure,
/// sans dépendance infrastructure, unitairement testable (Principe IV).
/// </summary>
public sealed record CalendrierOuvre(
    TimeOnly HeureDebut,
    TimeOnly HeureFin,
    IReadOnlySet<DayOfWeek> JoursOuvres,
    IReadOnlySet<DateOnly> JoursFeries)
{
    /// <summary>Calendrier « toujours ouvert » (24h/24, 7j/7, aucun jour férié) — utile quand
    /// aucun paramétrage n'a encore été saisi (PC-12), sans jamais inventer d'horaires métier.</summary>
    public static CalendrierOuvre ToujoursOuvert { get; } = new(
        TimeOnly.MinValue, TimeOnly.MaxValue,
        new HashSet<DayOfWeek>(Enum.GetValues<DayOfWeek>()),
        new HashSet<DateOnly>());

    public bool EstJourOuvre(DateOnly jour) => JoursOuvres.Contains(jour.DayOfWeek) && !JoursFeries.Contains(jour);

    /// <summary>Durée « ouvrée » écoulée entre deux instants UTC, en ne comptant que les
    /// intervalles à l'intérieur des horaires/jours ouvrés paramétrés (SLA-03/SLA-04).</summary>
    public TimeSpan DureeOuvreeEntre(DateTime debutUtc, DateTime finUtc)
    {
        if (finUtc <= debutUtc)
        {
            return TimeSpan.Zero;
        }

        // Cas particulier « toujours ouvert » (24h/24, 7j/7) : éviter la perte de précision liée
        // à TimeOnly.MaxValue (23:59:59.9999999 ≠ minuit) en sommant jour par jour.
        if (HeureDebut == TimeOnly.MinValue && HeureFin == TimeOnly.MaxValue && JoursOuvres.Count == 7 && JoursFeries.Count == 0)
        {
            return finUtc - debutUtc;
        }

        var total = TimeSpan.Zero;
        var jourCourant = DateOnly.FromDateTime(debutUtc);
        var dernierJour = DateOnly.FromDateTime(finUtc);

        while (jourCourant <= dernierJour)
        {
            if (EstJourOuvre(jourCourant))
            {
                var ouvertureJour = jourCourant.ToDateTime(HeureDebut, DateTimeKind.Utc);
                var fermetureJour = jourCourant.ToDateTime(HeureFin, DateTimeKind.Utc);

                var debutEffectif = Max(ouvertureJour, debutUtc);
                var finEffective = Min(fermetureJour, finUtc);

                if (finEffective > debutEffectif)
                {
                    total += finEffective - debutEffectif;
                }
            }

            jourCourant = jourCourant.AddDays(1);
        }

        return total;
    }

    private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;

    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;
}
