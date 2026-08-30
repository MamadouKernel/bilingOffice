using CIT.Plateforme.Application.Abstractions;

namespace CIT.Plateforme.Application.Tarification;

public sealed record GenererDocumentProformaPdfRequete(
    Guid AppelantId,
    Guid SocieteId,
    Guid SimulationId);

public sealed class GenererDocumentProformaPdfQueryHandler(
    ISimulationProformaRepository repository,
    IGenerateurDocumentProformaPdf generateurPdf)
{
    public async Task<DocumentPdfProformaResultat> HandleAsync(
        GenererDocumentProformaPdfRequete requete,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(requete);

        var simulation = await repository.ObtenirParIdAsync(requete.SimulationId, ct)
            ?? throw new InvalidOperationException("Simulation de proforma introuvable.");

        if (simulation.SocieteId != requete.SocieteId)
        {
            throw new UnauthorizedAccessException("Accès non autorisé à cette simulation.");
        }

        return await generateurPdf.GenererPdfAsync(simulation, ct);
    }
}
