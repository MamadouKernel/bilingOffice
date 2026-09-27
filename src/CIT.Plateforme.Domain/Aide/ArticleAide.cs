namespace CIT.Plateforme.Domain.Aide;

/// <summary>
/// ARTICLE D'AIDE CONTEXTUELLE — l'aide d'un écran, affichée par le bouton « ? » de la barre
/// supérieure. Rattaché à une <see cref="Route"/> (ex. « /billing-import/soumettre ») : l'article
/// s'applique à cet écran et à ses sous-écrans (« /billing-import/soumettre/… »).
///
/// Aucune habilitation propre : l'aide d'un écran n'est montrée QUE sur cet écran, que
/// l'utilisateur n'atteint déjà qu'avec les droits requis. Dupliquer la règle ici la ferait
/// diverger en silence.
///
/// Contenu en texte simple, balisage minimal (voir <see cref="BlocAide"/>) : jamais de HTML
/// stocké, donc aucun risque d'injection par l'écran d'administration.
/// </summary>
public sealed class ArticleAide
{
    public const int LongueurMaxRoute = 200;
    public const int LongueurMaxTitre = 150;
    public const int LongueurMaxContenu = 8000;

    public Guid Id { get; private set; }

    public string Route { get; private set; } = string.Empty;

    public string TitreFr { get; private set; } = string.Empty;

    public string TitreEn { get; private set; } = string.Empty;

    public string ContenuFr { get; private set; } = string.Empty;

    public string ContenuEn { get; private set; } = string.Empty;

    public bool Actif { get; private set; } = true;

    /// <summary>
    /// Écran ouvert à tout utilisateur connecté sans figurer dans son menu (fiche d'un dossier,
    /// notifications) : l'article est alors proposé à tous dans le centre d'aide. Sinon, il n'y
    /// apparaît que pour les comptes dont le menu mène à cet écran.
    /// </summary>
    public bool Transverse { get; private set; }

    private ArticleAide() { }

    public static ArticleAide Creer(
        string route, string titreFr, string titreEn, string contenuFr, string contenuEn, bool transverse = false, bool actif = true)
    {
        var article = new ArticleAide { Id = Guid.NewGuid(), Route = NormaliserRoute(route) };
        article.Modifier(titreFr, titreEn, contenuFr, contenuEn, actif, transverse);
        return article;
    }

    public void Modifier(string titreFr, string titreEn, string contenuFr, string contenuEn, bool actif, bool transverse)
    {
        TitreFr = Exiger(titreFr, LongueurMaxTitre, nameof(titreFr));
        TitreEn = Exiger(titreEn, LongueurMaxTitre, nameof(titreEn));
        ContenuFr = Exiger(contenuFr, LongueurMaxContenu, nameof(contenuFr));
        ContenuEn = Exiger(contenuEn, LongueurMaxContenu, nameof(contenuEn));
        Actif = actif;
        Transverse = transverse;
    }

    public string Titre(string langue) => Langue.EstAnglais(langue) ? TitreEn : TitreFr;

    public string Contenu(string langue) => Langue.EstAnglais(langue) ? ContenuEn : ContenuFr;

    /// <summary>Vrai si l'article couvre ce chemin : même écran, ou l'un de ses sous-écrans.</summary>
    public bool Couvre(string chemin)
    {
        var cible = NormaliserRoute(chemin);
        return cible == Route || (Route != "/" && cible.StartsWith(Route + "/", StringComparison.Ordinal));
    }

    /// <summary>Chemin sans requête ni fragment, en minuscules, avec « / » initial et sans « / » final.</summary>
    public static string NormaliserRoute(string route)
    {
        var chemin = (route ?? string.Empty).Split('?', '#')[0].Trim().ToLowerInvariant();
        chemin = "/" + chemin.Trim('/');
        if (chemin.Length > LongueurMaxRoute)
        {
            throw new ArgumentException($"La route dépasse {LongueurMaxRoute} caractères.", nameof(route));
        }

        return chemin;
    }

    private static string Exiger(string valeur, int max, string nom)
    {
        if (string.IsNullOrWhiteSpace(valeur) || valeur.Trim().Length > max)
        {
            throw new ArgumentException($"Champ obligatoire ({max} caractères au plus).", nom);
        }

        return valeur.Trim();
    }
}
