namespace CIT.Plateforme.Web.Services;

/// <summary>
/// État de pagination d'une table, côté écran. Une instance par table : la page la déclare
/// (<c>private readonly EtatPagination _pagination = new();</c>), itère sur
/// <see cref="Appliquer{T}"/> au lieu de la liste complète, et pose le composant
/// <c>&lt;Pagination Etat="_pagination" /&gt;</c> sous la table.
///
/// La page courante est RAMENÉE dans les bornes à chaque application : si un filtre réduit la
/// liste, on ne reste jamais sur une page vide.
/// </summary>
public sealed class EtatPagination
{
    public static readonly IReadOnlyList<int> TaillesProposees = [10, 25, 50, 100];

    public EtatPagination(int taillePage = 10) => TaillePage = taillePage;

    public int Page { get; private set; } = 1;

    public int TaillePage { get; private set; }

    public int Total { get; private set; }

    public int NombrePages => Total == 0 ? 1 : (int)Math.Ceiling(Total / (double)TaillePage);

    public bool APagePrecedente => Page > 1;

    public bool APageSuivante => Page < NombrePages;

    /// <summary>Premier et dernier rang affichés (1-indexés), pour « 11–20 sur 57 ».</summary>
    public int PremierRang => Total == 0 ? 0 : ((Page - 1) * TaillePage) + 1;

    public int DernierRang => Math.Min(Page * TaillePage, Total);

    /// <summary>Utile si la table dépasse la plus petite taille de page : sinon rien à paginer.</summary>
    public bool EstUtile => Total > TaillesProposees[0];

    public IEnumerable<T> Appliquer<T>(IEnumerable<T>? source)
    {
        var liste = source as IReadOnlyCollection<T> ?? (source ?? []).ToList();
        Total = liste.Count;
        Page = Math.Clamp(Page, 1, NombrePages);
        return liste.Skip((Page - 1) * TaillePage).Take(TaillePage);
    }

    public void AllerA(int page) => Page = Math.Clamp(page, 1, NombrePages);

    public void Precedente() => AllerA(Page - 1);

    public void Suivante() => AllerA(Page + 1);

    public void ChangerTaille(int taille)
    {
        if (taille <= 0 || taille == TaillePage)
        {
            return;
        }

        // On garde en vue la première ligne actuellement affichée.
        var premier = PremierRang;
        TaillePage = taille;
        Page = premier <= 0 ? 1 : ((premier - 1) / taille) + 1;
    }
}
