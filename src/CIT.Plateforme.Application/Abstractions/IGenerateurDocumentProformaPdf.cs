using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Domain.Tarification.Referentiels;

namespace CIT.Plateforme.Application.Abstractions;

public sealed record DocumentPdfProformaResultat(
    byte[] Contenu,
    string NomFichier,
    string TypeContenu);

public interface IGenerateurDocumentProformaPdf
{
    /// <summary>
    /// Les référentiels sont fournis par l'appelant : depuis la normalisation (analyse Merise
    /// § 3.3), l'instantané de simulation ne porte que des clés étrangères, et le document doit
    /// afficher « 40 pieds / Reefer / C1 » plutôt que trois identifiants.
    /// </summary>
    Task<DocumentPdfProformaResultat> GenererPdfAsync(
        SimulationProforma simulation, ReferentielsTarifaires referentiels, CancellationToken ct = default);
}
