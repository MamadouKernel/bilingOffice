namespace CIT.Plateforme.Domain.SuiviTemps;

public enum StatutSla
{
    EnCours,
    SeuilInformation,
    SeuilAlerte,
    Depasse,

    /// <summary>SLA-02 — au-delà du dépassement. N'est atteint QUE si un seuil d'escalade a été
    /// administré : sans lui, un ticket très en retard reste <see cref="Depasse"/> plutôt que
    /// d'escalader à un instant deviné.</summary>
    Escalade,
}

/// <summary>Paramètres SLA — cible et seuils, tous administrables (SLA-01, SLA-02, SLA-05).
/// Aucune valeur n'est fixée par défaut ici (Principe VI/XIII) ; ce type ne porte que la forme.</summary>
public sealed record ParametresSla(
    TimeSpan DureeCible,
    double SeuilInformation,
    double SeuilAlerte,
    double SeuilEscalade = 0)
{
    public TimeSpan EcheanceInformation => DureeCible * SeuilInformation;

    public TimeSpan EcheanceAlerte => DureeCible * SeuilAlerte;

    /// <summary>
    /// SLA-02 — l'échéance au-delà de laquelle le dépassement devient une escalade.
    /// <c>null</c> quand aucun seuil d'escalade n'est administré : dans ce cas la plateforme
    /// signale le dépassement sans escalader, plutôt que d'escalader à un instant deviné.
    /// </summary>
    public TimeSpan? EcheanceEscalade => SeuilEscalade == 0 ? null : DureeCible * SeuilEscalade;
}

/// <summary>
/// Moteur SLA/temps pur (decision D7), sans dépendance infrastructure. Détermine le statut d'un
/// SLA à partir du temps « ouvré » déjà écoulé (SLA-03 : suspendu pendant l'attente client via
/// l'exclusion de cette période du calcul en amont — voir MesureTemps).
/// </summary>
public static class EcheanceSla
{
    public static StatutSla DeterminerStatut(TimeSpan dureeEcoulee, ParametresSla parametres)
    {
        // L'escalade est testée AVANT le dépassement : elle se situe au-delà, donc tout ticket
        // en escalade est aussi dépassé. Tester le dépassement d'abord masquerait l'escalade.
        if (parametres.EcheanceEscalade is TimeSpan echeanceEscalade && dureeEcoulee >= echeanceEscalade)
        {
            return StatutSla.Escalade;
        }

        if (dureeEcoulee >= parametres.DureeCible)
        {
            return StatutSla.Depasse;
        }

        if (dureeEcoulee >= parametres.EcheanceAlerte)
        {
            return StatutSla.SeuilAlerte;
        }

        if (dureeEcoulee >= parametres.EcheanceInformation)
        {
            return StatutSla.SeuilInformation;
        }

        return StatutSla.EnCours;
    }
}
