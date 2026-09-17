using CIT.Plateforme.Application.Abstractions;

namespace CIT.Plateforme.Application.Tickets;

/// <summary>
/// PRI-01 — au lieu de coder en dur une correspondance module/type → priorité, la création de
/// ticket interroge ce service, qui évalue le référentiel administrable
/// <c>ReglePrioriteAutomatique</c> (première règle active correspondante, dans l'ordre défini par
/// l'administrateur). Aucune règle configurée → aucun résultat : le comportement de création reste
/// inchangé (priorité non définie), exactement comme avant l'introduction de ce service.
/// </summary>
public sealed class ResoudrePrioriteAutomatiqueService(IReglePrioriteAutomatiqueRepository regles)
{
    public async Task<Guid?> ResoudreAsync(string module, string? typeDemande, string? typeClient, CancellationToken ct = default)
    {
        var actives = await regles.ListerAsync(activesUniquement: true, ct).ConfigureAwait(false);

        var correspondante = actives
            .OrderBy(r => r.Ordre)
            .FirstOrDefault(r => r.Correspond(module, typeDemande, typeClient));

        return correspondante?.PrioriteId;
    }
}
