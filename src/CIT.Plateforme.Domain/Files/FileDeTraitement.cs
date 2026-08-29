namespace CIT.Plateforme.Domain.Files;

/// <summary>
/// File de traitement d'un module (SOC-04). Porte une stratégie d'affectation active,
/// administrable sans développement (AFF-07, contrat C-01).
/// </summary>
public enum StrategieAffectation
{
    PriseLibre,
    AffectationManuelle,
    RoundRobin,
    Charge,
    Disponibilite,
    Competence,
}

public sealed class FileDeTraitement
{
    public Guid Id { get; private set; }

    public string Module { get; private set; } = string.Empty;

    public string Nom { get; private set; } = string.Empty;

    public StrategieAffectation StrategieActive { get; private set; } = StrategieAffectation.PriseLibre;

    /// <summary>REMÉDIATION AUDIT (post-implementation-audit.md §3.C) : code de compétence requis
    /// pour la distribution automatique quand <see cref="StrategieActive"/> vaut
    /// <see cref="StrategieAffectation.Competence"/> (AFF-06). Non renseigné pour les autres
    /// stratégies. Aucun catalogue de compétences n'est fixé par le socle (Principe VI) : le code
    /// est une donnée administrée (voir <c>CompetenceOperateur</c>), jamais une valeur inventée
    /// ici.</summary>
    public string? CompetenceRequise { get; private set; }

    private FileDeTraitement() { }

    public static FileDeTraitement Creer(string module, string nom) => new()
    {
        Id = Guid.NewGuid(),
        Module = module,
        Nom = nom,
        StrategieActive = StrategieAffectation.PriseLibre,
    };

    /// <summary>C-01/AFF-07 : le changement de stratégie est réalisable sans développement. Le
    /// paramètre <paramref name="competenceRequise"/> n'a de sens que pour
    /// <see cref="StrategieAffectation.Competence"/> (AFF-06) ; il est ignoré (remis à
    /// <c>null</c>) pour toute autre stratégie afin d'éviter une configuration incohérente.</summary>
    public void DefinirStrategie(StrategieAffectation strategie, string? competenceRequise = null)
    {
        if (strategie == StrategieAffectation.Competence && string.IsNullOrWhiteSpace(competenceRequise))
        {
            throw new ArgumentException(
                "La stratégie Compétence (AFF-06) exige un code de compétence requis.", nameof(competenceRequise));
        }

        StrategieActive = strategie;
        CompetenceRequise = strategie == StrategieAffectation.Competence ? competenceRequise : null;
    }
}
