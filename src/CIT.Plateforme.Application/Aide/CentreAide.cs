using System.Globalization;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Aide;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Aide;

/// <summary>Un article tel qu'affiché dans le centre d'aide.</summary>
public sealed record ArticleAideVue(Guid Id, string Route, string Titre, IReadOnlyList<BlocAide> Blocs, bool Transverse);

/// <summary>
/// Une section du guide : un groupe du menu de l'utilisateur (titre = clé de traduction du menu),
/// ou la section générale des écrans transverses (<see cref="Titre"/> <c>null</c>).
/// </summary>
public sealed record SectionAide(string? Titre, string Icone, IReadOnlyList<ArticleAideVue> Articles);

/// <summary>
/// CENTRE D'AIDE — « le guide de mon rôle » : les articles des écrans que CE compte voit dans son
/// menu, rangés comme son menu, puis les écrans transverses. Le périmètre est donc exactement celui
/// de la navigation (même requête, mêmes habilitations) : impossible d'y lire l'aide d'un écran
/// qu'on n'a pas. La recherche porte sur le titre et le contenu, sans tenir compte des accents.
/// </summary>
public sealed class ConsulterCentreAideQuery(IArticleAideRepository articles, ConstruireNavigationQuery navigation)
{
    public async Task<IReadOnlyList<SectionAide>> ExecuterAsync(
        Guid utilisateurId, EspaceFonctionnel espace, bool aUneSocieteRattachee, string langue, string? recherche,
        CancellationToken ct = default)
    {
        var menu = await navigation.ExecuterAsync(utilisateurId, espace, aUneSocieteRattachee, ct).ConfigureAwait(false);
        var actifs = (await articles.ListerActifsAsync(ct).ConfigureAwait(false))
            .Where(a => Correspond(a, langue, recherche))
            .ToList();

        var dejaPlaces = new HashSet<Guid>();
        var sections = new List<SectionAide>();
        foreach (var groupe in menu)
        {
            var duGroupe = groupe.Liens
                .SelectMany(l => actifs.Where(a => a.Couvre(l.Route)))
                .Where(a => dejaPlaces.Add(a.Id))
                .Select(a => Vue(a, langue))
                .ToList();
            if (duGroupe.Count > 0)
            {
                sections.Add(new SectionAide(groupe.Titre, groupe.Icone, duGroupe));
            }
        }

        var transverses = actifs
            .Where(a => a.Transverse && dejaPlaces.Add(a.Id))
            .OrderBy(a => a.Titre(langue), StringComparer.CurrentCulture)
            .Select(a => Vue(a, langue))
            .ToList();
        if (transverses.Count > 0)
        {
            sections.Add(new SectionAide(null, "📌", transverses));
        }

        return sections;
    }

    private static ArticleAideVue Vue(ArticleAide a, string langue) =>
        new(a.Id, a.Route, a.Titre(langue), BlocAide.Analyser(a.Contenu(langue)), a.Transverse);

    private static bool Correspond(ArticleAide article, string langue, string? recherche)
    {
        if (string.IsNullOrWhiteSpace(recherche))
        {
            return true;
        }

        var comparaison = CultureInfo.InvariantCulture.CompareInfo;
        const CompareOptions options = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;
        var terme = recherche.Trim();
        return comparaison.IndexOf(article.Titre(langue), terme, options) >= 0
            || comparaison.IndexOf(article.Contenu(langue), terme, options) >= 0;
    }
}

/// <summary>Un article tel qu'administré (les deux langues).</summary>
public sealed record ArticleAideAdminVue(
    Guid Id, string Route, string TitreFr, string TitreEn, string ContenuFr, string ContenuEn, bool Actif, bool Transverse);

/// <summary>Liste de tous les articles pour l'écran d'administration.</summary>
public sealed class ListerArticlesAideQuery(IArticleAideRepository articles, IAutorisationService autorisation)
{
    public async Task<IReadOnlyList<ArticleAideAdminVue>> ExecuterAsync(Guid appelantId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationAide, ct: ct).ConfigureAwait(false);
        var tous = await articles.ListerTousAsync(ct).ConfigureAwait(false);
        return tous
            .Select(a => new ArticleAideAdminVue(a.Id, a.Route, a.TitreFr, a.TitreEn, a.ContenuFr, a.ContenuEn, a.Actif, a.Transverse))
            .ToList();
    }
}

/// <summary>
/// CRÉATION OU MODIFICATION D'UN ARTICLE D'AIDE, tracée à l'audit. La route est fixée à la
/// création (elle identifie l'écran) ; un écran n'a qu'un article. Un article ne se supprime pas,
/// il se désactive.
/// </summary>
public sealed class EnregistrerArticleAideCommand(
    IArticleAideRepository articles,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<Guid> ExecuterAsync(
        Guid appelantId, Guid? articleId, string route, string titreFr, string titreEn, string contenuFr,
        string contenuEn, bool actif, bool transverse, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationAide, ct: ct).ConfigureAwait(false);

        ArticleAide article;
        string? avant = null;
        if (articleId is { } id)
        {
            article = await articles.ObtenirAsync(id, ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Article d'aide introuvable.");
            avant = Resumer(article);
            article.Modifier(titreFr, titreEn, contenuFr, contenuEn, actif, transverse);
        }
        else
        {
            var routeNormalisee = ArticleAide.NormaliserRoute(route);
            if (await articles.RouteExisteAsync(routeNormalisee, ct).ConfigureAwait(false))
            {
                throw new RouteAideDejaUtiliseeException(routeNormalisee);
            }

            article = ArticleAide.Creer(routeNormalisee, titreFr, titreEn, contenuFr, contenuEn, transverse);
            article.Modifier(titreFr, titreEn, contenuFr, contenuEn, actif, transverse);
            articles.Ajouter(article);
        }

        await journalisation.ConsignerAuditAsync(
            appelantId, articleId is null ? "ArticleAideCree" : "ArticleAideModifie", nameof(ArticleAide), article.Id,
            avant, Resumer(article), ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
        return article.Id;
    }

    private static string Resumer(ArticleAide a) =>
        $"{a.Route} | {a.TitreFr} | {(a.Actif ? "actif" : "inactif")}{(a.Transverse ? " | transverse" : string.Empty)} | {a.ContenuFr.Length}+{a.ContenuEn.Length} car.";
}

/// <summary>Un écran n'a qu'un article d'aide.</summary>
public sealed class RouteAideDejaUtiliseeException(string route)
    : InvalidOperationException($"L'écran « {route} » a déjà un article d'aide.");
