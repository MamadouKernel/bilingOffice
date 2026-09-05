namespace CIT.Plateforme.Domain.Tarification.Referentiels;

/// <summary>
/// Sens de l'opération portuaire — première composante de la clé de tarification
/// (Sens + Taille + Catégorie de marchandise, SIM-03).
///
/// ÉTAIT UNE ÉNUMÉRATION C# (<c>SensTrafic</c>). L'analyse Merise § 3.3 (correction n° 1) l'a
/// érigé en référentiel : l'Annexe A.6 du CDC le désigne comme « paramétrable » (SIM-06), et un
/// libellé recopié en clair dans chaque ligne de tarif crée exactement la dépendance transitive
/// que la 3FN interdit. Une énumération C# oblige de surcroît à recompiler pour ajouter un sens.
///
/// Le <see cref="Code"/> est l'identité stable, jamais modifiable : c'est lui que les imports CSV
/// et les jeux d'essai de l'Annexe A.8 désignent. Le <see cref="Libelle"/>, lui, est administrable.
/// </summary>
public sealed class SensOperation
{
    public Guid Id { get; private set; }

    /// <summary>Identité stable (ex. « IMPORT_LOCAL »). Jamais modifiable après création.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Libelle { get; private set; } = string.Empty;

    /// <summary>Ordre d'affichage dans les listes de saisie.</summary>
    public int Ordre { get; private set; }

    /// <summary>
    /// Un sens retiré du référentiel n'est plus proposé à la saisie, mais reste lisible sur les
    /// simulations et les grilles qui le référencent — une simulation est un fait daté (§ 3.3).
    /// </summary>
    public bool Actif { get; private set; } = true;

    private SensOperation() { }

    public static SensOperation Creer(string code, string libelle, int ordre)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Le code du sens d'opération est obligatoire.", nameof(code));
        if (string.IsNullOrWhiteSpace(libelle))
            throw new ArgumentException("Le libellé du sens d'opération est obligatoire.", nameof(libelle));

        return new SensOperation
        {
            Id = Guid.NewGuid(),
            Code = code.Trim().ToUpperInvariant(),
            Libelle = libelle.Trim(),
            Ordre = ordre,
        };
    }

    public void Renommer(string libelle)
    {
        if (string.IsNullOrWhiteSpace(libelle))
            throw new ArgumentException("Le libellé du sens d'opération est obligatoire.", nameof(libelle));

        Libelle = libelle.Trim();
    }

    public void Reordonner(int ordre) => Ordre = ordre;

    public void Activer() => Actif = true;

    public void Desactiver() => Actif = false;
}
