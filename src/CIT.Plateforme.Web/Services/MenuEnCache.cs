using CIT.Plateforme.Application.Web;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace CIT.Plateforme.Web.Services;

/// <summary>
/// Menu de l'utilisateur mis en mémoire 30 secondes (checklist de mise en ligne, point 16) : il
/// était reconstruit en base plusieurs fois par page (menu latéral, recherche, accueil). Un délai
/// court suffit et reste sans risque : le menu ne fait que masquer des liens, chaque page vérifie
/// elle-même les droits. Une portée par lecture : jamais de DbContext partagé avec la page.
/// </summary>
public sealed class MenuEnCache(IMemoryCache cache, IServiceScopeFactory portees)
{
    public static readonly TimeSpan Duree = TimeSpan.FromSeconds(30);

    public async Task<IReadOnlyList<GroupeNavigationVue>> ObtenirAsync(Guid utilisateurId, EspaceFonctionnel espace, bool aUneSocieteRattachee)
    {
        var cle = $"menu:{utilisateurId}:{espace}:{aUneSocieteRattachee}";
        if (cache.TryGetValue(cle, out IReadOnlyList<GroupeNavigationVue>? menu) && menu is not null)
        {
            return menu;
        }

        using var portee = portees.CreateScope();
        menu = await portee.ServiceProvider.GetRequiredService<ConstruireNavigationQuery>()
            .ExecuterAsync(utilisateurId, espace, aUneSocieteRattachee);
        cache.Set(cle, menu, Duree);
        return menu;
    }
}
