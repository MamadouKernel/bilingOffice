using CIT.Plateforme.Domain.ActualisationBadt;

namespace CIT.Plateforme.Application.ActualisationBadt;

/// <summary>Compteurs d'un acteur (agent ou décideur). Les taux portent sur ses décisions (accord/refus/renvoi).</summary>
public sealed record CompteurActeurBadt(
    Guid ActeurId,
    int Qualifications,
    int Depliages,
    int RejetsAgent,
    int Accords,
    int Refus,
    int Renvois,
    double TauxAccord,
    double TauxRefus,
    double TauxRenvoi);

/// <summary>Trace d'un dépliage (qui a ajouté quel conteneur, quand).</summary>
public sealed record TraceDepliageBadt(Guid ActeurId, string NumeroBl, string NumeroConteneur, DateTime HorodatageUtc);

/// <summary>Résultat agrégé du reporting BADT (indicateurs cibles, hors « BL non dépliés » qui est un état vivant).</summary>
public sealed record ResultatReportingBadt(
    int TotalDemandes,
    int NombreEscalades,
    int NombreAccords,
    int NombreRefus,
    int NombreRenvois,
    int NombreRejetsAgent,
    TimeSpan? DelaiMoyenQualificationDecision,
    IReadOnlyList<CompteurActeurBadt> ParActeur,
    IReadOnlyList<TraceDepliageBadt> Depliages);

/// <summary>
/// FEATURE 010 (Actualisation BADT) — agrégation pure des mesures de reporting en indicateurs.
/// Sans I/O ni dépendance : testable directement (approche B, plan #3).
/// </summary>
public static class CalculateurReportingBadt
{
    /// <summary>Nombre de traces de dépliage exposées (les plus récentes).</summary>
    private const int MaxDepliages = 50;

    public static ResultatReportingBadt Agreger(IReadOnlyList<MesureActualisationBadt> mesures)
    {
        var totalDemandes = mesures.Count(m => m.Type == TypeMesureBadt.Creation);
        // Escalade = qualification à cause terminale (le workflow escalade alors vers le Décideur).
        var nombreEscalades = mesures.Count(m => m.Type == TypeMesureBadt.Qualification && m.Cause == CauseExpirationBadt.Terminale);
        var nombreAccords = mesures.Count(m => m.Type == TypeMesureBadt.Accord);
        var nombreRefus = mesures.Count(m => m.Type == TypeMesureBadt.Refus);
        var nombreRenvois = mesures.Count(m => m.Type == TypeMesureBadt.Renvoi);
        var nombreRejets = mesures.Count(m => m.Type == TypeMesureBadt.RejetAgent);

        return new ResultatReportingBadt(
            totalDemandes,
            nombreEscalades,
            nombreAccords,
            nombreRefus,
            nombreRenvois,
            nombreRejets,
            CalculerDelaiMoyen(mesures),
            AgregerParActeur(mesures),
            mesures
                .Where(m => m.Type == TypeMesureBadt.Depliage && m.NumeroConteneur.Length > 0)
                .OrderByDescending(m => m.HorodatageUtc)
                .Take(MaxDepliages)
                .Select(m => new TraceDepliageBadt(m.ActeurId, m.NumeroBl, m.NumeroConteneur, m.HorodatageUtc))
                .ToList());
    }

    /// <summary>
    /// Délai qualification→décision : pour chaque décision (accord/refus) d'une ligne, on prend la
    /// dernière qualification de cette ligne antérieure à la décision (une ligne renvoyée est
    /// requalifiée, c'est la qualification effective qui compte). Moyenne sur les paires trouvées.
    /// </summary>
    private static TimeSpan? CalculerDelaiMoyen(IReadOnlyList<MesureActualisationBadt> mesures)
    {
        var qualifsParLigne = mesures
            .Where(m => m.Type == TypeMesureBadt.Qualification && m.LigneId is not null)
            .GroupBy(m => m.LigneId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(m => m.HorodatageUtc).ToList());

        var delais = new List<TimeSpan>();
        foreach (var decision in mesures.Where(m => (m.Type == TypeMesureBadt.Accord || m.Type == TypeMesureBadt.Refus) && m.LigneId is not null))
        {
            if (!qualifsParLigne.TryGetValue(decision.LigneId!.Value, out var qualifs))
            {
                continue;
            }

            var qualif = qualifs.LastOrDefault(q => q.HorodatageUtc <= decision.HorodatageUtc);
            if (qualif is not null)
            {
                delais.Add(decision.HorodatageUtc - qualif.HorodatageUtc);
            }
        }

        return delais.Count == 0 ? null : TimeSpan.FromTicks((long)delais.Average(d => d.Ticks));
    }

    private static IReadOnlyList<CompteurActeurBadt> AgregerParActeur(IReadOnlyList<MesureActualisationBadt> mesures)
    {
        return mesures
            // La création est le fait du client demandeur, pas d'un acteur du traitement : exclue du tableau.
            .Where(m => m.Type != TypeMesureBadt.Creation)
            .GroupBy(m => m.ActeurId)
            .Select(g =>
            {
                var accords = g.Count(m => m.Type == TypeMesureBadt.Accord);
                var refus = g.Count(m => m.Type == TypeMesureBadt.Refus);
                var renvois = g.Count(m => m.Type == TypeMesureBadt.Renvoi);
                var totalDecisions = accords + refus + renvois;
                double Taux(int n) => totalDecisions == 0 ? 0d : 100d * n / totalDecisions;

                return new CompteurActeurBadt(
                    g.Key,
                    g.Count(m => m.Type == TypeMesureBadt.Qualification),
                    g.Count(m => m.Type == TypeMesureBadt.Depliage),
                    g.Count(m => m.Type == TypeMesureBadt.RejetAgent),
                    accords,
                    refus,
                    renvois,
                    Taux(accords),
                    Taux(refus),
                    Taux(renvois));
            })
            .OrderByDescending(c => c.Accords + c.Refus + c.Renvois + c.Qualifications)
            .ToList();
    }
}
