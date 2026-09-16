using CIT.Plateforme.Web.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace CIT.Plateforme.Web.Tests;

/// <summary>
/// Enregistre <see cref="IStringLocalizer{SharedResources}"/> dans le contexte de test bUnit.
///
/// Le chantier i18n a ajouté <c>@inject IStringLocalizer&lt;SharedResources&gt; Loc</c> à de
/// nombreux composants Razor sans mettre à jour les tests de rendu existants, qui ne
/// l'enregistraient pas dans leur collection de services : ils échouaient tous avec
/// <c>InvalidOperationException: Cannot provide a value for property 'Loc'</c>. Reprend le même
/// principe que <see cref="Components.Layout.NavigationDeTest.Enregistrer"/> pour les tests qui
/// ne passent pas par ce harnais (rendu direct d'une page, pas du menu).
/// </summary>
public static class LocalisationDeTest
{
    public static void Enregistrer(IServiceCollection services) =>
        services.AddSingleton<IStringLocalizer<SharedResources>, SharedResourcesLocalizer>();
}
