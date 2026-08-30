using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Documents;
using CIT.Plateforme.Domain.Documents;

namespace CIT.Plateforme.Application.BillingExport;

/// <summary>
/// Dépôt de la preuve de paiement par le client propriétaire du ticket Export.
/// </summary>
public sealed class DeposerPreuvePaiementExportCommand(DeposerDocumentCommand deposerDocument)
{
    public Task ExecuterAsync(
        Guid demandeurId,
        Guid ticketId,
        string nomFichier,
        string typeContenu,
        Stream contenu,
        long tailleOctets,
        ReglesDepotDocument reglesEtape,
        CancellationToken ct = default) =>
        deposerDocument.DeposerPourClientAsync(demandeurId, ticketId, nomFichier, typeContenu, contenu, tailleOctets, reglesEtape, ct);
}
