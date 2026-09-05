namespace CIT.Plateforme.Domain.Tarification.Parametres;

/// <summary>
/// Les trois majorations de l'Annexe A.2 du CDC. Liste fermée par le cahier des charges : le MPD
/// la traduit par une contrainte CHECK, pas par une table de référence — c'est le discriminant
/// de la ligne, non une donnée administrable.
/// </summary>
public enum TypeMajoration
{
    /// <summary>RG-SIM-06 — poids au-delà du seuil fixé pour la taille du conteneur.</summary>
    Lourd,

    /// <summary>RG-SIM-07 — conteneur déclaré dangereux.</summary>
    Dangereux,

    /// <summary>RG-SIM-07 — conteneur déclaré hors gabarit (Out Of Gauge).</summary>
    HorsGabarit,
}

/// <summary>
/// Taux d'une majoration, pour une version de grille donnée (Annexe A.2, RG-SIM-06/07).
///
/// ÉTAIT TROIS COLONNES <c>TauxMajorationLourd</c> / <c>TauxMajorationDangereux</c> /
/// <c>TauxMajorationOog</c> sur un type possédé. Une ligne par couple (grille, type) rend le
/// paramétrage énumérable et administrable sans développement (SIM-06).
/// </summary>
public sealed class ParametreMajoration
{
    public Guid Id { get; private set; }

    public Guid VersionGrilleTarifaireId { get; private set; }

    public TypeMajoration Type { get; private set; }

    /// <summary>Taux appliqué à l'acconage, exprimé en fraction (0,30 = 30 %).</summary>
    public decimal Taux { get; private set; }

    private ParametreMajoration() { }

    public static ParametreMajoration Creer(Guid versionGrilleTarifaireId, TypeMajoration type, decimal taux)
    {
        if (taux < 0)
            throw new ArgumentException("Un taux de majoration ne peut pas être négatif.", nameof(taux));

        return new ParametreMajoration
        {
            Id = Guid.NewGuid(),
            VersionGrilleTarifaireId = versionGrilleTarifaireId,
            Type = type,
            Taux = taux,
        };
    }
}
