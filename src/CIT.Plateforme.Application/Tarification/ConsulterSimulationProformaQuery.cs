using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Tarification;

namespace CIT.Plateforme.Application.Tarification;

public sealed record SimulationProformaDetailVue(
    Guid Id,
    string Reference,
    Guid SocieteId,
    DateTime DateCreationUtc,
    Guid VersionGrilleTarifaireId,
    string VersionGrilleLibelle,
    bool EstGrilleObsolete,
    string VersionActiveActuelleLibelle,
    SensTrafic Sens,
    DateOnly DateArrivee,
    DateOnly DateEnlevementPrevisionnel,
    int DureeSejourJours,
    string? NomNavire,
    string? NumeroVoyage,
    string? NumeroBlOuBooking,
    decimal MontantTotalXof,
    IReadOnlyList<LigneResultatSimulationVue> Lignes,
    string MentionLegale);

public sealed record ConsulterSimulationProformaRequete(
    Guid AppelantId,
    Guid SocieteId,
    Guid SimulationId);

public sealed class ConsulterSimulationProformaQueryHandler(
    ISimulationProformaRepository simulationRepository,
    IVersionGrilleTarifaireRepository grilleRepository)
{
    public async Task<SimulationProformaDetailVue?> HandleAsync(
        ConsulterSimulationProformaRequete requete,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(requete);

        var simulation = await simulationRepository.ObtenirParIdAsync(requete.SimulationId, ct);
        if (simulation == null) return null;

        // Contrôle de cloisonnement multi-tenant
        if (simulation.SocieteId != requete.SocieteId)
        {
            throw new UnauthorizedAccessException("Accès non autorisé à cette simulation.");
        }

        var grilleActive = await grilleRepository.ObtenirActiveADateUtcAsync(DateTime.UtcNow, ct);
        var estObsolete = grilleActive != null && grilleActive.Id != simulation.VersionGrilleTarifaireId;
        var versionActiveLibelle = grilleActive?.NumeroVersion ?? simulation.VersionGrilleLibelle;

        var lignesVue = simulation.LignesConteneurs.Select(l => new LigneResultatSimulationVue(
            l.NumeroConteneur,
            l.Taille,
            l.Type,
            l.PoidsTonnes,
            l.EstDangereux,
            l.EstHorsGabarit,
            l.CategorieMarchandise,
            l.MontantAcconage,
            l.MontantRelevage,
            l.MontantBranchement,
            l.MontantMajorationLourd,
            l.MontantMajorationDangereux,
            l.MontantMajorationOog,
            l.MontantStationnement,
            l.PartCit,
            l.PartOic,
            l.PartPaa,
            l.TotalLigneXof)).ToList();

        return new SimulationProformaDetailVue(
            simulation.Id,
            simulation.Reference,
            simulation.SocieteId,
            simulation.DateCreationUtc,
            simulation.VersionGrilleTarifaireId,
            simulation.VersionGrilleLibelle,
            estObsolete,
            versionActiveLibelle,
            simulation.Sens,
            simulation.DateArrivee,
            simulation.DateEnlevementPrevisionnel,
            simulation.DureeSejourJours,
            simulation.NomNavire,
            simulation.NumeroVoyage,
            simulation.NumeroBlOuBooking,
            simulation.MontantTotalXof,
            lignesVue,
            MoteurCalculProforma.MentionLegaleDefaut);
    }
}
