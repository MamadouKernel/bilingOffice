namespace CIT.Plateforme.Application.Supervision;

public record ResultatCalculDisponibilite(
    TimeSpan TempsOuvreTotal,
    TimeSpan TempsMaintenancePlanifiee,
    TimeSpan TempsEligible,
    TimeSpan TempsIndisponible,
    TimeSpan TempsDisponible,
    double? PourcentageDisponibilite,
    bool EstConformeSla);

public static class CalculateurDisponibilite
{
    public const double SeuilSlaPourcentage = 99.0;

    public static ResultatCalculDisponibilite Calculer(
        TimeSpan tempsOuvreTotal,
        TimeSpan tempsMaintenancePlanifiee,
        IEnumerable<TimeSpan> pannesNonPlanifiees)
    {
        var maintenancePlafonnee = tempsMaintenancePlanifiee > tempsOuvreTotal
            ? tempsOuvreTotal
            : tempsMaintenancePlanifiee;

        var tempsEligible = tempsOuvreTotal - maintenancePlafonnee;

        var totalPannes = pannesNonPlanifiees.Aggregate(TimeSpan.Zero, (acc, p) => acc + p);
        var pannePlafonnee = totalPannes > tempsEligible
            ? tempsEligible
            : totalPannes;

        var tempsDisponible = tempsEligible - pannePlafonnee;

        if (tempsEligible == TimeSpan.Zero)
        {
            return new ResultatCalculDisponibilite(
                TempsOuvreTotal: tempsOuvreTotal,
                TempsMaintenancePlanifiee: maintenancePlafonnee,
                TempsEligible: TimeSpan.Zero,
                TempsIndisponible: totalPannes,
                TempsDisponible: TimeSpan.Zero,
                PourcentageDisponibilite: null,
                EstConformeSla: false);
        }

        var pourcentage = (tempsDisponible.TotalMilliseconds / tempsEligible.TotalMilliseconds) * 100.0;
        var estConforme = pourcentage >= SeuilSlaPourcentage;

        return new ResultatCalculDisponibilite(
            TempsOuvreTotal: tempsOuvreTotal,
            TempsMaintenancePlanifiee: maintenancePlafonnee,
            TempsEligible: tempsEligible,
            TempsIndisponible: totalPannes,
            TempsDisponible: tempsDisponible,
            PourcentageDisponibilite: Math.Round(pourcentage, 4),
            EstConformeSla: estConforme);
    }
}
