using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tarification;

namespace CIT.Plateforme.Application.Tarification;

public sealed record VersionGrilleTarifaireSommaireVue(
    Guid Id,
    string NumeroVersion,
    DateTime DateEffetUtc,
    DateTime DateCreationUtc,
    bool EstActive,
    int NombreLignes,
    ParametresTarification Parametres);

public sealed record ConsulterVersionsGrillesTarifairesRequete(Guid AgentId);

public sealed class ConsulterVersionsGrillesTarifairesQueryHandler(
    IVersionGrilleTarifaireRepository repository,
    IAutorisationService autorisation)
{
    public async Task<IReadOnlyList<VersionGrilleTarifaireSommaireVue>> HandleAsync(
        ConsulterVersionsGrillesTarifairesRequete requete,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(requete);

        // FEATURE 005 (spec.md §26) : aucune donnée tarifaire sensible n'est restituée avant
        // autorisation, y compris en simple consultation.
        await autorisation.ExigerAsync(requete.AgentId, Permission.TarificationAdministrer, ct: ct);

        var versions = await repository.ListerToutesAsync(ct);
        return versions.Select(v => new VersionGrilleTarifaireSommaireVue(
            v.Id,
            v.NumeroVersion,
            v.DateEffetUtc,
            v.DateCreationUtc,
            v.EstActive,
            v.Lignes.Count,
            v.Parametres)).ToList();
    }
}
