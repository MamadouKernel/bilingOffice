using CIT.Plateforme.Domain.Tarification;

namespace CIT.Plateforme.Application.Abstractions;

public interface IVersionGrilleTarifaireRepository
{
    Task<VersionGrilleTarifaire?> ObtenirParIdAsync(Guid id, CancellationToken ct = default);
    Task<VersionGrilleTarifaire?> ObtenirActiveADateUtcAsync(DateTime dateUtc, CancellationToken ct = default);
    Task<IReadOnlyList<VersionGrilleTarifaire>> ListerToutesAsync(CancellationToken ct = default);
    Task AjouterAsync(VersionGrilleTarifaire version, CancellationToken ct = default);
}

public interface ISimulationProformaRepository
{
    Task<SimulationProforma?> ObtenirParIdAsync(Guid id, CancellationToken ct = default);
    Task<SimulationProforma?> ObtenirParReferenceAsync(string reference, CancellationToken ct = default);
    Task<IReadOnlyList<SimulationProforma>> ListerParSocieteAsync(Guid societeId, int page, int taillePage, CancellationToken ct = default);
    Task<int> CompterParSocieteAsync(Guid societeId, CancellationToken ct = default);
    Task AjouterAsync(SimulationProforma simulation, CancellationToken ct = default);
}
