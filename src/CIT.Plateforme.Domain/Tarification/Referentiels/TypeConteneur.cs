namespace CIT.Plateforme.Domain.Tarification.Referentiels;

/// <summary>
/// Type de conteneur (Dry, Reefer…) — référentiel de l'Annexe A.6, saisi à la simulation (SIM-01).
///
/// ÉTAIT UNE ÉNUMÉRATION C#, et le moteur de calcul testait <c>Type == TypeConteneur.Reefer</c>
/// pour décider d'appliquer le branchement frigorifique. C'était une règle métier <b>codée en
/// dur</b> : introduire un type « Reefer haute puissance » demandait une recompilation, alors que
/// SIM-06 exige que ces référentiels soient administrables sans développement.
///
/// <see cref="DeclencheBranchementFrigorifique"/> porte désormais cette décision comme une
/// <b>donnée</b> du référentiel. Le moteur ne nomme plus aucun type : il lit l'attribut
/// (RG-SIM-05 — « le branchement frigorifique ne s'applique qu'aux conteneurs Reefer »).
/// </summary>
public sealed class TypeConteneur
{
    public Guid Id { get; private set; }

    /// <summary>Identité stable (ex. « DRY », « REEFER »). Jamais modifiable après création.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Libelle { get; private set; } = string.Empty;

    /// <summary>
    /// RG-SIM-05 — seul un type ainsi marqué déclenche le poste « branchement frigorifique ».
    /// Attribut de donnée, jamais un test sur le code dans le moteur.
    /// </summary>
    public bool DeclencheBranchementFrigorifique { get; private set; }

    public int Ordre { get; private set; }

    public bool Actif { get; private set; } = true;

    private TypeConteneur() { }

    public static TypeConteneur Creer(string code, string libelle, bool declencheBranchementFrigorifique, int ordre)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Le code du type de conteneur est obligatoire.", nameof(code));
        if (string.IsNullOrWhiteSpace(libelle))
            throw new ArgumentException("Le libellé du type de conteneur est obligatoire.", nameof(libelle));

        return new TypeConteneur
        {
            Id = Guid.NewGuid(),
            Code = code.Trim().ToUpperInvariant(),
            Libelle = libelle.Trim(),
            DeclencheBranchementFrigorifique = declencheBranchementFrigorifique,
            Ordre = ordre,
        };
    }

    public void Modifier(string libelle, bool declencheBranchementFrigorifique)
    {
        if (string.IsNullOrWhiteSpace(libelle))
            throw new ArgumentException("Le libellé du type de conteneur est obligatoire.", nameof(libelle));

        Libelle = libelle.Trim();
        DeclencheBranchementFrigorifique = declencheBranchementFrigorifique;
    }

    public void Reordonner(int ordre) => Ordre = ordre;

    public void Activer() => Actif = true;

    public void Desactiver() => Actif = false;
}
