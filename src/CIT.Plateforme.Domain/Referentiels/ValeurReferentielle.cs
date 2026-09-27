namespace CIT.Plateforme.Domain.Referentiels;

/// <summary>
/// VALEUR D'UN RÉFÉRENTIEL MÉTIER (Merise, 3FN) — une liste de valeurs affichée aux usagers
/// (gravité, motif de clôture, canal…) devient une ENTITÉ à part entière : table dédiée, libellés
/// FR/EN, ordre d'affichage et activation administrables, et clé étrangère depuis chaque colonne
/// qui la porte. Le libellé dépend ainsi de la seule clé (le code), et non plus du code source.
///
/// Le <b>code</b> reste la valeur de l'énumération <typeparamref name="TCode"/> : c'est lui que le
/// domaine teste pour ses règles (une gravité critique raccourcit le SLA, une clôture pour absence
/// de réponse exige un droit dédié…). Il n'est donc jamais modifiable ; ajouter une valeur au
/// comportement nouveau reste un développement, pas un paramétrage.
///
/// Une valeur désactivée n'est plus proposée à la saisie ; les lignes qui la portent la conservent
/// (aucun effet rétroactif) et la contrainte d'intégrité empêche toute suppression.
/// </summary>
public abstract class ValeurReferentielle<TCode> : IValeurReferentielle
    where TCode : struct, Enum
{
    public const int LongueurMaxLibelle = 120;

    public TCode Code { get; private set; }

    public string LibelleFr { get; private set; } = string.Empty;

    public string LibelleEn { get; private set; } = string.Empty;

    public int Ordre { get; private set; }

    public bool Actif { get; private set; } = true;

    string IValeurReferentielle.CodeTexte => Code.ToString();

    protected ValeurReferentielle() { }

    protected ValeurReferentielle(TCode code, string libelleFr, string libelleEn, int ordre)
    {
        Code = code;
        Modifier(libelleFr, libelleEn, ordre, actif: true);
    }

    public void Modifier(string libelleFr, string libelleEn, int ordre, bool actif)
    {
        LibelleFr = Exiger(libelleFr, nameof(libelleFr));
        LibelleEn = Exiger(libelleEn, nameof(libelleEn));
        Ordre = ordre;
        Actif = actif;
    }

    /// <summary>Libellé dans la langue demandée (« en » → anglais, sinon français).</summary>
    public string Libelle(string langue) =>
        Langue.EstAnglais(langue) ? LibelleEn : LibelleFr;

    private static string Exiger(string valeur, string nom)
    {
        if (string.IsNullOrWhiteSpace(valeur) || valeur.Trim().Length > LongueurMaxLibelle)
        {
            throw new ArgumentException($"Le libellé est obligatoire ({LongueurMaxLibelle} caractères au plus).", nom);
        }

        return valeur.Trim();
    }
}

/// <summary>
/// Vue non générique d'une valeur de référentiel : permet d'administrer tous les référentiels
/// avec un seul écran et une seule commande, quel que soit le type de leur code.
/// </summary>
public interface IValeurReferentielle
{
    string CodeTexte { get; }

    string LibelleFr { get; }

    string LibelleEn { get; }

    int Ordre { get; }

    bool Actif { get; }

    void Modifier(string libelleFr, string libelleEn, int ordre, bool actif);
}
