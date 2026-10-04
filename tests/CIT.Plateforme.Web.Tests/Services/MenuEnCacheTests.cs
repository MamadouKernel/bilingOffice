using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Web.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Services;

/// <summary>Checklist de mise en ligne, point 16 : le menu n'est plus reconstruit en base à chaque
/// appel de la même page ; chaque utilisateur garde son propre menu.</summary>
public sealed class MenuEnCacheTests
{
    [Fact]
    public async Task Le_second_appel_ne_relit_pas_la_base_et_les_utilisateurs_restent_separes()
    {
        var habilitations = new Mock<IHabilitationRepository>();
        habilitations.Setup(h => h.ListerParUtilisateurAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var services = new ServiceCollection()
            .AddScoped(_ => habilitations.Object)
            .AddScoped(_ =>
            {
                var files = new Mock<IFileDeTraitementRepository>();
                files.Setup(f => f.ListerAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
                return files.Object;
            })
            .AddScoped<ConstruireNavigationQuery>()
            .BuildServiceProvider();
        var menus = new MenuEnCache(new MemoryCache(new MemoryCacheOptions()), services.GetRequiredService<IServiceScopeFactory>());
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        await menus.ObtenirAsync(a, EspaceFonctionnel.Agent, false);
        await menus.ObtenirAsync(a, EspaceFonctionnel.Agent, false);
        await menus.ObtenirAsync(b, EspaceFonctionnel.Agent, false);

        habilitations.Verify(h => h.ListerParUtilisateurAsync(a, It.IsAny<CancellationToken>()), Times.Once);
        habilitations.Verify(h => h.ListerParUtilisateurAsync(b, It.IsAny<CancellationToken>()), Times.Once);
    }
}
