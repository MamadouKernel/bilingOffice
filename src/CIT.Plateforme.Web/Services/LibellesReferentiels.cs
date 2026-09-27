using System.Globalization;
using CIT.Plateforme.Application.Administration;
using Microsoft.Extensions.DependencyInjection;

namespace CIT.Plateforme.Web.Services;

/// <summary>
/// Libellés des référentiels métier pour l'affichage : lus dans leurs tables (et non plus codés
/// en dur dans chaque écran), puis mis en cache pour la durée du circuit. Un code absent du
/// référentiel s'affiche tel quel plutôt que de faire échouer la page.
/// </summary>
public sealed class LibellesReferentiels(IServiceScopeFactory scopes)
{
    private readonly Dictionary<DomaineReferentiel, IReadOnlyList<ValeurReferentielleVue>> _cache = [];

    /// <summary>Toutes les valeurs du référentiel, dans l'ordre d'affichage.</summary>
    public async Task<IReadOnlyList<ValeurReferentielleVue>> ValeursAsync(DomaineReferentiel domaine)
    {
        if (_cache.TryGetValue(domaine, out var valeurs))
        {
            return valeurs;
        }

        using var scope = scopes.CreateScope();
        valeurs = await scope.ServiceProvider.GetRequiredService<ListerReferentielQuery>()
            .ExecuterAsync(domaine, activesUniquement: false);
        _cache[domaine] = valeurs;
        return valeurs;
    }

    /// <summary>Libellé d'un code dans la langue courante ; « — » si aucun code.</summary>
    public static string Libelle<TCode>(IReadOnlyList<ValeurReferentielleVue> valeurs, TCode? code)
        where TCode : struct, Enum
    {
        if (code is null)
        {
            return "—";
        }

        var texte = code.Value.ToString();
        return valeurs.FirstOrDefault(v => v.Code == texte)?.Libelle(CultureInfo.CurrentUICulture.Name) ?? texte;
    }
}
