using CIT.Plateforme.Domain.Tarification;

namespace CIT.Plateforme.Application.Abstractions;

public sealed record DocumentPdfProformaResultat(
    byte[] Contenu,
    string NomFichier,
    string TypeContenu);

public interface IGenerateurDocumentProformaPdf
{
    Task<DocumentPdfProformaResultat> GenererPdfAsync(SimulationProforma simulation, CancellationToken ct = default);
}
