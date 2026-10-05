using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Domain.Documents;

namespace CIT.Plateforme.Application.BillingExport;

/// <summary>
/// Dépôt de la preuve de paiement par le client propriétaire du ticket Export — même règle qu'à
/// l'import (pièce classée « preuve de paiement », reprise du traitement après un refus).
/// </summary>
public sealed class DeposerPreuvePaiementExportCommand(DeposerPreuvePaiementCommand deposer)
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
        deposer.ExecuterAsync(demandeurId, ticketId, nomFichier, typeContenu, contenu, tailleOctets, reglesEtape, ct);
}
