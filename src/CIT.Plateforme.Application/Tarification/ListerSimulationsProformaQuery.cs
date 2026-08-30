using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Tarification;

namespace CIT.Plateforme.Application.Tarification;

public sealed record SimulationProformaSommaireVue(
    Guid Id,
    string Reference,
    DateTime DateCreationUtc,
    SensTrafic Sens,
    int NombreConteneurs,
    int DureeSejourJours,
    decimal MontantTotalXof,
    string VersionGrilleLibelle);

public sealed record ListerSimulationsProformaRequete(
    Guid AppelantId,
    Guid SocieteId,
    int Page = 1,
    int TaillePage = 20);

public sealed class ListerSimulationsProformaQueryHandler(ISimulationProformaRepository repository)
{
    public async Task<IReadOnlyList<SimulationProformaSommaireVue>> HandleAsync(
        ListerSimulationsProformaRequete requete,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(requete);

        var simulations = await repository.ListerParSocieteAsync(
            requete.SocieteId,
            requete.Page,
            requete.TaillePage,
            ct);

        return simulations.Select(s => new SimulationProformaSommaireVue(
            s.Id,
            s.Reference,
            s.DateCreationUtc,
            s.Sens,
            s.LignesConteneurs.Count,
            s.DureeSejourJours,
            s.MontantTotalXof,
            s.VersionGrilleLibelle)).ToList();
    }
}
