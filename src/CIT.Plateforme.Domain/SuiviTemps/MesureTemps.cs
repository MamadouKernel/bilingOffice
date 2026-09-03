namespace CIT.Plateforme.Domain.SuiviTemps;

/// <summary>
/// TPS-01..05 : mesure des temps d'un ticket. Type de domaine pur — aucune dépendance
/// infrastructure.
///
/// DISTINCTION FONDAMENTALE, exigée par TPS-01 et TPS-03 :
/// <list type="bullet">
///   <item><b>Étape courante</b> — attente et traitement, mesurés depuis l'arrivée dans la FILE
///   COURANTE. Ces horodatages sont réinitialisés à chaque transfert.</item>
///   <item><b>Parcours</b> — temps total et temps interne, mesurés depuis la CRÉATION du ticket.
///   Ces durées ne sont jamais réinitialisées.</item>
/// </list>
///
/// DÉFAUT CORRIGÉ : le temps total était calculé depuis <c>DateArriveeEnFileUtc</c>, remis à zéro à
/// chaque transfert. Sur un ticket transféré, il affichait donc le temps passé dans la dernière
/// file en le nommant « temps total du parcours » — une mesure sous-évaluée, et trompeuse pour qui
/// la lisait. TPS-03 exige explicitement « création → clôture ».
/// </summary>
public sealed record MesureTemps(
    TimeSpan TempsAttente,
    TimeSpan TempsTraitement,
    TimeSpan TempsAttenteClient,
    TimeSpan TempsInterneCit,
    TimeSpan TempsTotal)
{
    /// <summary>
    /// TPS-01 — attente = arrivée en file → prise en charge ; traitement = prise en charge → maintenant.
    /// TPS-02 — attente client cumulée.
    /// TPS-03 — temps total = création → maintenant ; temps interne = total − attente client.
    /// TPS-04 — l'attente client n'est jamais imputée à la performance opérateur ou file.
    /// </summary>
    /// <param name="dateCreationUtc">Création du ticket. Base du temps de PARCOURS (TPS-03).</param>
    /// <param name="dateArriveeEnFileUtc">Arrivée dans la file courante. Base des temps d'ÉTAPE (TPS-01).</param>
    /// <param name="datePriseEnChargeUtc">Prise en charge dans la file courante, si elle a eu lieu.</param>
    /// <param name="instantMesureUtc">Instant de la mesure — la clôture, ou l'heure courante.</param>
    /// <param name="tempsAttenteClientCumule">Somme des périodes d'attente client sur tout le parcours.</param>
    public static MesureTemps Calculer(
        DateTime dateCreationUtc,
        DateTime dateArriveeEnFileUtc,
        DateTime? datePriseEnChargeUtc,
        DateTime instantMesureUtc,
        TimeSpan tempsAttenteClientCumule)
    {
        // Étape courante — bornée par l'arrivée dans la file.
        var tempsAttente = (datePriseEnChargeUtc ?? instantMesureUtc) - dateArriveeEnFileUtc;
        var tempsTraitement = datePriseEnChargeUtc is DateTime pec ? instantMesureUtc - pec : TimeSpan.Zero;

        // Parcours — borné par la création. C'est ce que TPS-03 nomme « temps total ».
        var tempsTotal = instantMesureUtc - dateCreationUtc;
        var tempsInterne = tempsTotal - tempsAttenteClientCumule;

        return new MesureTemps(
            TempsAttente: tempsAttente < TimeSpan.Zero ? TimeSpan.Zero : tempsAttente,
            TempsTraitement: tempsTraitement < TimeSpan.Zero ? TimeSpan.Zero : tempsTraitement,
            TempsAttenteClient: tempsAttenteClientCumule,
            TempsInterneCit: tempsInterne < TimeSpan.Zero ? TimeSpan.Zero : tempsInterne,
            TempsTotal: tempsTotal < TimeSpan.Zero ? TimeSpan.Zero : tempsTotal);
    }
}
