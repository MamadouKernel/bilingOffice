using CIT.Plateforme.Domain.Affectation;

namespace CIT.Plateforme.Application.Affectation;

/// <summary>Résultat d'évaluation de performance d'un agent sur une période [début, fin].</summary>
public sealed record PerformanceAgent(
    int NombreIndisponibilites,
    TimeSpan TempsIndisponible,
    TimeSpan TempsAbsent,
    TimeSpan TempsDisponible,
    double PourcentageDisponible);

/// <summary>
/// Fonction PURE : à partir de l'historique des transitions de disponibilité d'un agent, calcule
/// le temps passé dans chaque statut sur la fenêtre [débutUtc, finUtc] et le nombre de passages
/// en « Indisponible ». Aucune I/O — testable unitairement.
/// </summary>
public static class CalculateurPerformanceAgent
{
    public static PerformanceAgent Calculer(
        IReadOnlyList<(StatutDisponibilite Statut, DateTime HorodatageUtc)> historique,
        DateTime debutUtc,
        DateTime finUtc)
    {
        var ordonne = historique.OrderBy(h => h.HorodatageUtc).ToList();

        // Statut au début de la fenêtre = dernière transition antérieure ou égale à débutUtc.
        // Faute d'historique antérieur, l'agent est réputé disponible (défaut du domaine).
        var statutCourant = ordonne
            .Where(h => h.HorodatageUtc <= debutUtc)
            .Select(h => (StatutDisponibilite?)h.Statut)
            .LastOrDefault() ?? StatutDisponibilite.Disponible;

        var dansFenetre = ordonne.Where(h => h.HorodatageUtc > debutUtc && h.HorodatageUtc <= finUtc).ToList();

        var dureeParStatut = new Dictionary<StatutDisponibilite, TimeSpan>();
        var curseur = debutUtc;
        var nbIndispo = 0;

        void Accumuler(StatutDisponibilite statut, DateTime jusqua)
        {
            if (jusqua <= curseur) return;
            dureeParStatut[statut] = dureeParStatut.GetValueOrDefault(statut) + (jusqua - curseur);
            curseur = jusqua;
        }

        foreach (var transition in dansFenetre)
        {
            Accumuler(statutCourant, transition.HorodatageUtc);
            // ponytail: compte les passages EN indisponible survenus dans la fenêtre ; un agent
            // déjà indisponible au début n'est pas recompté (son passage est hors période).
            if (transition.Statut == StatutDisponibilite.Indisponible && statutCourant != StatutDisponibilite.Indisponible)
            {
                nbIndispo++;
            }
            statutCourant = transition.Statut;
        }

        Accumuler(statutCourant, finUtc);

        var total = finUtc - debutUtc;
        var dispo = dureeParStatut.GetValueOrDefault(StatutDisponibilite.Disponible);
        var pct = total > TimeSpan.Zero ? dispo.TotalSeconds / total.TotalSeconds * 100 : 0;

        return new PerformanceAgent(
            nbIndispo,
            dureeParStatut.GetValueOrDefault(StatutDisponibilite.Indisponible),
            dureeParStatut.GetValueOrDefault(StatutDisponibilite.Absent),
            dispo,
            Math.Round(pct, 1));
    }
}
