using CIT.Plateforme.Application.Abstractions;

namespace CIT.Plateforme.Application.Tarification;

/// <summary>
/// Référentiels servis à la saisie d'une simulation.
///
/// POURQUOI CETTE REQUÊTE EXISTE. La catégorie de marchandise était saisie au clavier dans un
/// champ libre : une faute de frappe produisait « tarif non trouvé » au lieu d'un tarif. Servir
/// les référentiels à l'écran est la moitié utile de la correction n° 1 de l'analyse Merise
/// § 3.3 — l'autre moitié, la clé étrangère, empêche seulement d'enregistrer une valeur absurde.
/// </summary>
public sealed record ReferentielsSimulationVue(
    IReadOnlyList<ReferenceTarifaireVue> Sens,
    IReadOnlyList<ReferenceTarifaireVue> Tailles,
    IReadOnlyList<ReferenceTarifaireVue> Types,
    IReadOnlyList<CategorieMarchandiseVue> Categories)
{
    /// <summary>Annexe A.6 — les catégories proposées dépendent du sens choisi.</summary>
    public IReadOnlyList<ReferenceTarifaireVue> CategoriesDuSens(Guid sensOperationId) =>
        [.. Categories
            .Where(c => c.SensOperationId == sensOperationId)
            .Select(c => new ReferenceTarifaireVue(c.Id, c.Code, c.Libelle))];
}

public sealed record CategorieMarchandiseVue(Guid Id, string Code, string Libelle, Guid SensOperationId);

public sealed class ConsulterReferentielsTarifairesQueryHandler(
    IReferentielsTarifairesRepository repository)
{
    public async Task<ReferentielsSimulationVue> HandleAsync(CancellationToken ct = default)
    {
        var sens = await repository.ListerSensAsync(actifsSeulement: true, ct);
        var tailles = await repository.ListerTaillesAsync(actifsSeulement: true, ct);
        var types = await repository.ListerTypesAsync(actifsSeulement: true, ct);
        var categories = await repository.ListerCategoriesAsync(sensOperationId: null, actifsSeulement: true, ct);

        return new ReferentielsSimulationVue(
            [.. sens.OrderBy(s => s.Ordre).Select(s => new ReferenceTarifaireVue(s.Id, s.Code, s.Libelle))],
            [.. tailles.OrderBy(t => t.Ordre).Select(t => new ReferenceTarifaireVue(t.Id, t.Code, t.Libelle))],
            [.. types.OrderBy(t => t.Ordre).Select(t => new ReferenceTarifaireVue(t.Id, t.Code, t.Libelle))],
            [.. categories
                .OrderBy(c => c.Code, StringComparer.Ordinal)
                .Select(c => new CategorieMarchandiseVue(c.Id, c.Code, c.Libelle, c.SensOperationId))]);
    }
}
