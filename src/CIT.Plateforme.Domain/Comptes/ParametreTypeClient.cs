namespace CIT.Plateforme.Domain.Comptes;

/// <summary>
/// PARAMÉTRAGE D'UN TYPE DE CLIENT — ce que l'administration peut changer sans développement :
/// libellé FR/EN, description, ordre d'affichage et activation.
///
/// LIMITE ASSUMÉE (même principe que <see cref="Tickets.LibelleStatutTicket"/>) : le <b>code</b>
/// est le nom de la valeur de <see cref="TypeClient"/> et n'est jamais modifiable. Chaque type
/// pilote un circuit métier (aiguillage Import/Export, facturation, files) : créer un type au
/// comportement nouveau reste un développement, pas un paramétrage.
///
/// Un type désactivé n'est plus proposé pour les nouvelles sociétés ; les sociétés qui le portent
/// le conservent (aucun effet rétroactif).
/// </summary>
public sealed class ParametreTypeClient
{
    public const int LongueurMaxLibelle = 120;
    public const int LongueurMaxDescription = 400;

    /// <summary>Identité technique : nom de la valeur de <see cref="TypeClient"/>. Jamais modifiable.</summary>
    public string Code { get; private set; } = string.Empty;

    public string LibelleFr { get; private set; } = string.Empty;

    public string LibelleEn { get; private set; } = string.Empty;

    public string DescriptionFr { get; private set; } = string.Empty;

    public string DescriptionEn { get; private set; } = string.Empty;

    public int Ordre { get; private set; }

    public bool Actif { get; private set; } = true;

    private ParametreTypeClient() { }

    public TypeClient Type => Enum.Parse<TypeClient>(Code);

    public static ParametreTypeClient Creer(
        TypeClient type, string libelleFr, string libelleEn, string descriptionFr, string descriptionEn, int ordre) =>
        new()
        {
            Code = type.ToString(),
            LibelleFr = Exiger(libelleFr, LongueurMaxLibelle, nameof(libelleFr)),
            LibelleEn = Exiger(libelleEn, LongueurMaxLibelle, nameof(libelleEn)),
            DescriptionFr = Borner(descriptionFr, nameof(descriptionFr)),
            DescriptionEn = Borner(descriptionEn, nameof(descriptionEn)),
            Ordre = ordre,
        };

    public void Modifier(
        string libelleFr, string libelleEn, string descriptionFr, string descriptionEn, int ordre, bool actif)
    {
        LibelleFr = Exiger(libelleFr, LongueurMaxLibelle, nameof(libelleFr));
        LibelleEn = Exiger(libelleEn, LongueurMaxLibelle, nameof(libelleEn));
        DescriptionFr = Borner(descriptionFr, nameof(descriptionFr));
        DescriptionEn = Borner(descriptionEn, nameof(descriptionEn));
        Ordre = ordre;
        Actif = actif;
    }

    /// <summary>Libellé dans la langue demandée (« en » → anglais, sinon français).</summary>
    public string Libelle(string langue) => langue.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? LibelleEn : LibelleFr;

    public string Description(string langue) => langue.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? DescriptionEn : DescriptionFr;

    private static string Exiger(string valeur, int max, string nom)
    {
        if (string.IsNullOrWhiteSpace(valeur) || valeur.Trim().Length > max)
        {
            throw new ArgumentException($"Le libellé est obligatoire ({max} caractères au plus).", nom);
        }

        return valeur.Trim();
    }

    private static string Borner(string? valeur, string nom)
    {
        var texte = (valeur ?? string.Empty).Trim();
        return texte.Length > LongueurMaxDescription
            ? throw new ArgumentException($"La description dépasse {LongueurMaxDescription} caractères.", nom)
            : texte;
    }
}
