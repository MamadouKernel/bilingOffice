namespace CIT.Plateforme.Domain.SuiviTemps;

public enum StatutSla
{
    EnCours,
    SeuilInformation,
    SeuilAlerte,
    Depasse,
}

/// <summary>Paramètres SLA — cible et seuils, tous administrables (SLA-01, SLA-02, SLA-05).
/// Aucune valeur n'est fixée par défaut ici (Principe VI/XIII) ; ce type ne porte que la forme.</summary>
public sealed record ParametresSla(TimeSpan DureeCible, double SeuilInformation, double SeuilAlerte)
{
    public TimeSpan EcheanceInformation => DureeCible * SeuilInformation;

    public TimeSpan EcheanceAlerte => DureeCible * SeuilAlerte;
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
