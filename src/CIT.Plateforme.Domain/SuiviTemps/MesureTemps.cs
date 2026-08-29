namespace CIT.Plateforme.Domain.SuiviTemps;

/// <summary>
/// TPS-01..05 : mesure des temps d'un ticket. Type de domaine pur, calculé à partir des
/// horodatages déjà portés par <c>Ticket</c> — aucune dépendance infrastructure.
/// </summary>
public sealed record MesureTemps(
    TimeSpan TempsAttente,
    TimeSpan TempsTraitement,
    TimeSpan TempsAttenteClient,
    TimeSpan TempsInterneCit,
    TimeSpan TempsTotal)
{
    /// <summary>
    /// TPS-01 (attente = arrivée → PEC, traitement = PEC → sortie/maintenant),
    /// TPS-02 (attente client cumulée), TPS-03 (temps interne = total - attente client),
    /// TPS-04 (le temps d'attente client n'est jamais imputé à la performance opérateur/file).
    /// </summary>
    public static MesureTemps Calculer(
        DateTime dateArriveeEnFileUtc,
        DateTime? datePriseEnChargeUtc,
        DateTime instantMesureUtc,
        TimeSpan tempsAttenteClientCumule)
    {
        var tempsAttente = (datePriseEnChargeUtc ?? instantMesureUtc) - dateArriveeEnFileUtc;
        var tempsTraitement = datePriseEnChargeUtc is DateTime pec ? instantMesureUtc - pec : TimeSpan.Zero;
        var tempsTotal = instantMesureUtc - dateArriveeEnFileUtc;
        var tempsInterne = tempsTotal - tempsAttenteClientCumule;

        return new MesureTemps(
            TempsAttente: tempsAttente,
            TempsTraitement: tempsTraitement,
            TempsAttenteClient: tempsAttenteClientCumule,
            TempsInterneCit: tempsInterne < TimeSpan.Zero ? TimeSpan.Zero : tempsInterne,
            TempsTotal: tempsTotal);
    }
}
