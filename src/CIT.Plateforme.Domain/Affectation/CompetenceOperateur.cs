namespace CIT.Plateforme.Domain.Affectation;

/// <summary>
/// REMÉDIATION AUDIT (post-implementation-audit.md §3.C) : association compétence/opérateur,
/// nécessaire pour que la stratégie de distribution automatique AFF-06 (compétence) puisse
/// réellement retenir des candidats. Le socle ne fixe aucun catalogue de compétences (Principe
/// VI) : <see cref="Code"/> est une donnée administrée en texte libre, jamais une valeur métier
/// inventée par le code.
/// </summary>
public sealed class CompetenceOperateur
{
    public Guid Id { get; private set; }

    public Guid OperateurId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    private CompetenceOperateur() { }

    public static CompetenceOperateur Attribuer(Guid operateurId, string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Le code de compétence est obligatoire.", nameof(code));
        }

        return new CompetenceOperateur
        {
            Id = Guid.NewGuid(),
            OperateurId = operateurId,
            Code = code,
        };
    }
}