using CIT.Plateforme.Application.Aide;
using CIT.Plateforme.Domain.Aide;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Infrastructure.Persistence;

public sealed class ArticleAideRepository(PlateformeDbContext contexte) : IArticleAideRepository
{
    public async Task<IReadOnlyList<ArticleAide>> ListerActifsAsync(CancellationToken ct = default) =>
        await contexte.ArticlesAide.AsNoTracking().Where(a => a.Actif).ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<ArticleAide>> ListerTousAsync(CancellationToken ct = default) =>
        await contexte.ArticlesAide.OrderBy(a => a.Route).ToListAsync(ct).ConfigureAwait(false);

    public async Task<ArticleAide?> ObtenirAsync(Guid id, CancellationToken ct = default) =>
        await contexte.ArticlesAide.FirstOrDefaultAsync(a => a.Id == id, ct).ConfigureAwait(false);

    public async Task<bool> RouteExisteAsync(string route, CancellationToken ct = default) =>
        await contexte.ArticlesAide.AnyAsync(a => a.Route == route, ct).ConfigureAwait(false);

    public void Ajouter(ArticleAide article) => contexte.ArticlesAide.Add(article);
}

/// <summary>
/// Installe l'aide de départ (<see cref="ContenuAideInitial"/>). STRICTEMENT ADDITIF : un article
/// dont la route existe déjà n'est jamais réécrit, l'administration a pu le retoucher.
/// </summary>
public sealed class ProvisionnementAide(PlateformeDbContext contexte, ILogger<ProvisionnementAide> journal)
{
    public async Task ExecuterAsync(CancellationToken ct = default)
    {
        var presentes = await contexte.ArticlesAide.Select(a => a.Route).ToListAsync(ct).ConfigureAwait(false);
        var manquants = ContenuAideInitial.Articles
            .Where(a => !presentes.Contains(ArticleAide.NormaliserRoute(a.Route)))
            .ToList();
        if (manquants.Count == 0)
        {
            return;
        }

        contexte.ArticlesAide.AddRange(manquants.Select(a => ArticleAide.Creer(a.Route, a.TitreFr, a.TitreEn, a.ContenuFr, a.ContenuEn, a.Transverse)));
        await contexte.SaveChangesAsync(ct).ConfigureAwait(false);
        journal.LogInformation("Aide contextuelle : {Nombre} article(s) installé(s).", manquants.Count);
    }
}
