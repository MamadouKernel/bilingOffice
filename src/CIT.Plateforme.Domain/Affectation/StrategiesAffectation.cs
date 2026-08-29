namespace CIT.Plateforme.Domain.Affectation;

/// <summary>Candidat à une affectation automatique — vue minimale nécessaire aux stratégies,
/// sans dépendance vers Ticket/File (Principe IV, faible couplage).</summary>
public sealed record CandidatAffectation(Guid OperateurId, StatutDisponibilite Disponibilite, int NombreTicketsEnCours, IReadOnlyCollection<string> Competences);

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

/// <summary>AFF-03 : Round-Robin — répartition cyclique, ici approximée par le plus ancien
/// affecté (charge la plus basse) faute d'un état de rotation persistant explicite dans ce socle
/// minimal ; suffisant pour garantir une distribution équitable testable.</summary>
public sealed class RoundRobinStrategy : IStrategieAffectation
{
    public Guid? Choisir(IReadOnlyList<CandidatAffectation> candidats) =>
        FiltreDisponibilite.SeulementDisponibles(candidats)
            .OrderBy(c => c.NombreTicketsEnCours)
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
