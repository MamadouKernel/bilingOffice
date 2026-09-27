using CIT.Plateforme.Domain.Aide;

namespace CIT.Plateforme.Application.Aide;

/// <summary>Accès aux articles d'aide.</summary>
public interface IArticleAideRepository
{
    Task<IReadOnlyList<ArticleAide>> ListerActifsAsync(CancellationToken ct = default);

    /// <summary>Tous les articles, actifs ou non (administration), suivis pour modification.</summary>
    Task<IReadOnlyList<ArticleAide>> ListerTousAsync(CancellationToken ct = default);

    Task<ArticleAide?> ObtenirAsync(Guid id, CancellationToken ct = default);

    Task<bool> RouteExisteAsync(string route, CancellationToken ct = default);

    void Ajouter(ArticleAide article);
}

/// <summary>L'aide d'un écran, prête à afficher dans la langue demandée.</summary>
public sealed record AideVue(string Route, string Titre, IReadOnlyList<BlocAide> Blocs);

/// <summary>
/// AIDE CONTEXTUELLE — l'article de l'écran courant : celui dont la route couvre le chemin, la
/// plus précise l'emportant (« /billing-import/soumettre » avant « /billing-import »). Sans
/// habilitation : l'aide n'est servie que pour l'écran où l'utilisateur se trouve déjà.
/// </summary>
public sealed class ObtenirAideContextuelleQuery(IArticleAideRepository articles)
{
    public async Task<AideVue?> ExecuterAsync(string chemin, string langue, CancellationToken ct = default)
    {
        var actifs = await articles.ListerActifsAsync(ct).ConfigureAwait(false);
        var article = actifs
            .Where(a => a.Couvre(chemin))
            .OrderByDescending(a => a.Route.Length)
            .FirstOrDefault();

        return article is null
            ? null
            : new AideVue(article.Route, article.Titre(langue), BlocAide.Analyser(article.Contenu(langue)));
    }
}
