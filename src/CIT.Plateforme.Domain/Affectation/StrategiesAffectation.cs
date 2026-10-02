namespace CIT.Plateforme.Domain.Affectation;

/// <summary>Candidat à une affectation automatique — vue minimale nécessaire aux stratégies,
/// sans dépendance vers Ticket/File (Principe IV, faible couplage).</summary>
/// <param name="DerniereAffectationUtc">Dernière prise en charge de l'opérateur sur la file (null : jamais servi) — sert la rotation AFF-03.</param>
public sealed record CandidatAffectation(Guid OperateurId, StatutDisponibilite Disponibilite, int NombreTicketsEnCours, IReadOnlyCollection<string> Competences, DateTime? DerniereAffectationUtc = null);

/// <summary>AFF-03..06 : stratégies d'affectation automatique paramétrables par file (AFF-07).
/// Toutes DOIVENT respecter DIS-01 : jamais de candidat Indisponible/Absent (invariant #11).</summary>
public interface IStrategieAffectation
{
    Guid? Choisir(IReadOnlyList<CandidatAffectation> candidats);
}

internal static class FiltreDisponibilite
{
    /// <summary>DIS-01 : invariant appliqué systématiquement, avant toute logique de stratégie.</summary>
    public static IReadOnlyList<CandidatAffectation> SeulementDisponibles(IReadOnlyList<CandidatAffectation> candidats) =>
        [.. candidats.Where(c => c.Disponibilite == StatutDisponibilite.Disponible)];
}

/// <summary>AFF-03 : Round-Robin — vraie rotation : le prochain servi est l'opérateur dont la
/// dernière prise en charge sur la file est la plus ancienne (jamais servi en premier). L'état de
/// rotation est lu dans les passages en file : aucun compteur à maintenir, et la rotation reste
/// juste quand un opérateur entre, sort ou revient de congé.</summary>
public sealed class RoundRobinStrategy : IStrategieAffectation
{
    public Guid? Choisir(IReadOnlyList<CandidatAffectation> candidats) =>
        FiltreDisponibilite.SeulementDisponibles(candidats)
            .OrderBy(c => c.DerniereAffectationUtc ?? DateTime.MinValue)
            .ThenBy(c => c.OperateurId)
            .Select(c => (Guid?)c.OperateurId)
            .FirstOrDefault();
}

/// <summary>AFF-04 : distribution selon la charge actuelle (le moins chargé en premier).</summary>
public sealed class ChargeStrategy : IStrategieAffectation
{
    public Guid? Choisir(IReadOnlyList<CandidatAffectation> candidats) =>
        FiltreDisponibilite.SeulementDisponibles(candidats)
            .OrderBy(c => c.NombreTicketsEnCours)
            .Select(c => (Guid?)c.OperateurId)
            .FirstOrDefault();
}

/// <summary>AFF-05 : distribution selon la disponibilité des agents (seuls les Disponibles sont
/// éligibles — le premier candidat disponible, sans autre critère de tri).</summary>
public sealed class DisponibiliteStrategy : IStrategieAffectation
{
    public Guid? Choisir(IReadOnlyList<CandidatAffectation> candidats) =>
        FiltreDisponibilite.SeulementDisponibles(candidats)
            .Select(c => (Guid?)c.OperateurId)
            .FirstOrDefault();
}

/// <summary>AFF-06 : distribution selon la compétence/habilitation requise.</summary>
public sealed class CompetenceStrategy(string competenceRequise) : IStrategieAffectation
{
    public Guid? Choisir(IReadOnlyList<CandidatAffectation> candidats) =>
        FiltreDisponibilite.SeulementDisponibles(candidats)
            .Where(c => c.Competences.Contains(competenceRequise))
            .OrderBy(c => c.NombreTicketsEnCours)
            .Select(c => (Guid?)c.OperateurId)
            .FirstOrDefault();
}
