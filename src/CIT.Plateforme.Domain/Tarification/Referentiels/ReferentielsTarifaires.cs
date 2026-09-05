namespace CIT.Plateforme.Domain.Tarification.Referentiels;

/// <summary>
/// Vue résolue des quatre référentiels normalisés, passée au moteur de calcul.
///
/// POURQUOI CE TYPE EXISTE. Les référentiels sont devenus des tables (analyse Merise § 3.3,
/// correction n° 1), et le moteur a besoin de leurs attributs — le libellé pour restituer le
/// détail exigé par SIM-05, et surtout
/// <see cref="TypeConteneur.DeclencheBranchementFrigorifique"/> pour appliquer RG-SIM-05. Lui
/// donner un dépôt aurait rompu sa pureté : <c>MoteurCalculProforma</c> reste déterministe et
/// sans dépendance d'infrastructure (Principe I). L'appelant résout, le moteur consomme.
/// </summary>
public sealed class ReferentielsTarifaires
{
    private readonly Dictionary<Guid, SensOperation> _sens;
    private readonly Dictionary<Guid, TailleConteneur> _tailles;
    private readonly Dictionary<Guid, TypeConteneur> _types;
    private readonly Dictionary<Guid, CategorieMarchandise> _categories;

    public ReferentielsTarifaires(
        IEnumerable<SensOperation> sens,
        IEnumerable<TailleConteneur> tailles,
        IEnumerable<TypeConteneur> types,
        IEnumerable<CategorieMarchandise> categories)
    {
        ArgumentNullException.ThrowIfNull(sens);
        ArgumentNullException.ThrowIfNull(tailles);
        ArgumentNullException.ThrowIfNull(types);
        ArgumentNullException.ThrowIfNull(categories);

        _sens = sens.ToDictionary(s => s.Id);
        _tailles = tailles.ToDictionary(t => t.Id);
        _types = types.ToDictionary(t => t.Id);
        _categories = categories.ToDictionary(c => c.Id);
    }

    public SensOperation Sens(Guid id) => Resoudre(_sens, id, "sens d'opération");

    public TailleConteneur Taille(Guid id) => Resoudre(_tailles, id, "taille de conteneur");

    public TypeConteneur Type(Guid id) => Resoudre(_types, id, "type de conteneur");

    public CategorieMarchandise Categorie(Guid id) => Resoudre(_categories, id, "catégorie de marchandise");

    public IReadOnlyCollection<SensOperation> TousLesSens => _sens.Values;

    public IReadOnlyCollection<TailleConteneur> ToutesLesTailles => _tailles.Values;

    public IReadOnlyCollection<TypeConteneur> TousLesTypes => _types.Values;

    public IReadOnlyCollection<CategorieMarchandise> ToutesLesCategories => _categories.Values;

    // -----------------------------------------------------------------------
    // Résolution par code — le code est l'identité stable que désignent le CDC,
    // les imports CSV et le paramétrage initial de l'Annexe A. Contrairement à
    // la résolution par identifiant, un code absent est un cas courant (import
    // d'un fichier saisi à la main) : ces méthodes renvoient donc null.
    // -----------------------------------------------------------------------

    public SensOperation? SensParCode(string code) =>
        _sens.Values.SingleOrDefault(s => string.Equals(s.Code, code?.Trim(), StringComparison.OrdinalIgnoreCase));

    public TailleConteneur? TailleParCode(string code) =>
        _tailles.Values.SingleOrDefault(t => string.Equals(t.Code, code?.Trim(), StringComparison.OrdinalIgnoreCase));

    public TypeConteneur? TypeParCode(string code) =>
        _types.Values.SingleOrDefault(t => string.Equals(t.Code, code?.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>Annexe A.6 — une catégorie ne se désigne que par le couple (sens, code).</summary>
    public CategorieMarchandise? CategorieParCode(Guid sensOperationId, string code) =>
        _categories.Values.SingleOrDefault(c =>
            c.SensOperationId == sensOperationId
            && string.Equals(c.Code, code?.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Un identifiant absent est une erreur de programmation, pas une saisie invalide : les
    /// identifiants proviennent des listes servies à la saisie, jamais d'une frappe libre. Échouer
    /// explicitement vaut mieux que calculer avec un référentiel partiel.
    /// </summary>
    private static T Resoudre<T>(Dictionary<Guid, T> index, Guid id, string nature) =>
        index.TryGetValue(id, out var valeur)
            ? valeur
            : throw new InvalidOperationException(
                $"Référentiel incomplet : aucun {nature} d'identifiant {id} n'a été fourni au moteur de calcul.");
}
