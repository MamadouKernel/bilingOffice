using System.Reflection;
using CIT.Plateforme.Domain.Aide;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Architecture;

/// <summary>
/// Garde-fou : chaque article d'aide de départ vise un écran qui existe. Un écran renommé ou
/// supprimé laisserait sinon une aide orpheline, invisible — l'erreur ne se verrait jamais.
/// </summary>
public sealed class AideRoutesTests
{
    [Fact]
    public void Chaque_article_d_aide_de_depart_vise_un_ecran_existant()
    {
        var gabarits = typeof(Program).Assembly.GetTypes()
            .SelectMany(t => t.GetCustomAttributes<RouteAttribute>())
            .Select(r => ArticleAide.NormaliserRoute(r.Template.Split('{')[0]))
            .ToHashSet();

        var orphelins = ContenuAideInitial.Articles
            .Select(a => ArticleAide.NormaliserRoute(a.Route))
            .Where(route => !gabarits.Any(g => g == route || g.StartsWith(route + "/", StringComparison.Ordinal)))
            .ToList();

        Assert.True(orphelins.Count == 0, "Aide sans écran : " + string.Join(", ", orphelins));
    }
}
