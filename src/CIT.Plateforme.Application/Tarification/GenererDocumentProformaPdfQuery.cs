using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Abstractions;

namespace CIT.Plateforme.Application.Tarification;

public sealed record GenererDocumentProformaPdfRequete(
    Guid AppelantId,
    Guid SimulationId);

public sealed class GenererDocumentProformaPdfQueryHandler(
    ISimulationProformaRepository repository,
    IReferentielsTarifairesRepository referentielsRepository,
    IGenerateurDocumentProformaPdf generateurPdf,
    ControleAccesRessourceService controleAcces)
{
    public async Task<DocumentPdfProformaResultat> HandleAsync(
        GenererDocumentProformaPdfRequete requete,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(requete);

        var simulation = await repository.ObtenirParIdAsync(requete.SimulationId, ct)
            ?? throw new InvalidOperationException("Simulation de proforma introuvable.");

        // REMÉDIATION P2-02 — LA SOCIÉTÉ N'EST PLUS REÇUE, ELLE EST RÉSOLUE.
        // Le contrôle comparait `simulation.SocieteId` à une valeur fournie par l'appelant :
        // transmettre la société convoitée suffisait. La portée vient désormais du contrôle
        // central, qui la déduit du rattachement actif de l'appelant et applique ENT-03.
        await controleAcces.ExigerConsultationRessourceDeSocieteAsync(
            requete.AppelantId, simulation.SocieteId, simulation.AuteurId, ct).ConfigureAwait(false);

        var referentiels = await referentielsRepository.ChargerAsync(ct);
        return await generateurPdf.GenererPdfAsync(simulation, referentiels, ct);
    }
}
