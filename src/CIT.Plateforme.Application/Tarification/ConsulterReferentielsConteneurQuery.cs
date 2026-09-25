using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tarification.Referentiels;

namespace CIT.Plateforme.Application.Tarification;

/// <summary>Vue de consultation des référentiels conteneur (sens, types, tailles, catégories),
/// inactifs inclus.</summary>
public sealed record ReferentielsConteneurVue(
    IReadOnlyList<SensOperation> Sens,
    IReadOnlyList<TypeConteneur> Types,
    IReadOnlyList<TailleConteneur> Tailles,
    IReadOnlyList<CategorieMarchandise> Categories);

/// <summary>
/// F3 (audit sécurité, SEC-02/03/12) — lecture cloisonnée des référentiels conteneur pour l'écran
/// d'administration. L'écran interrogeait <see cref="IReferentielsTarifairesRepository"/> EN DIRECT,
/// donc lisait la base sans passer par un cas d'usage et sans contrôle d'habilitation (une garde de
/// surface routable le signalait). Cette query recentralise cet accès derrière la même autorisation
/// que les écritures : <see cref="Permission.TarificationAdministrer"/>. Les référentiels inactifs
/// sont inclus — un administrateur doit pouvoir les réactiver, et une simulation historique peut
/// encore les référencer.
/// </summary>
public sealed class ConsulterReferentielsConteneurQuery(
    IReferentielsTarifairesRepository referentiels,
    IAutorisationService autorisation)
{
    public async Task<ReferentielsConteneurVue> ExecuterAsync(Guid agentId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(agentId, Permission.TarificationAdministrer, ct: ct).ConfigureAwait(false);

        var sens = await referentiels.ListerSensAsync(actifsSeulement: false, ct).ConfigureAwait(false);
        var types = await referentiels.ListerTypesAsync(actifsSeulement: false, ct).ConfigureAwait(false);
        var tailles = await referentiels.ListerTaillesAsync(actifsSeulement: false, ct).ConfigureAwait(false);
        var categories = await referentiels.ListerCategoriesAsync(sensOperationId: null, actifsSeulement: false, ct).ConfigureAwait(false);

        return new ReferentielsConteneurVue(sens, types, tailles, categories);
    }
}
