namespace CIT.Plateforme.Application.SuiviTemps;

/// <summary>
/// SLA-04 — jours fériés en vigueur, lus sans accès base par le calcul des délais SLA (chargés au
/// démarrage, rafraîchis à chaque modification du référentiel). Même pattern que
/// <see cref="Exploitation.ParametresCourants"/> : instantané par processus.
/// ponytail: instantané par processus ; plusieurs instances IIS → le rafraîchissement a lieu à la
/// modification sur l'instance qui l'opère et au prochain démarrage des autres.
/// </summary>
public static class JoursFeriesCourants
{
    private static volatile IReadOnlySet<DateOnly> _actuels = new HashSet<DateOnly>();

    /// <summary>Ensemble des dates chômées actives, injecté dans les calendriers ouvrés SLA.</summary>
    public static IReadOnlySet<DateOnly> Actuels => _actuels;

    public static void Remplacer(IEnumerable<DateOnly> dates) =>
        _actuels = new HashSet<DateOnly>(dates);
}
