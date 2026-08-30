using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Documents;

namespace CIT.Plateforme.Application.BillingImport;

/// <summary>
/// Contrat C3-14 — Récupérer le ticket clôturé avec documents (variante En Compte). CDC : IMP-22.
/// Réutilisation intégrale du socle documentaire (D11) — aucune extension, aucune liste de pièces
/// nommée en dur (PC-08 non résolu).
/// </summary>
public sealed class ConsulterDocumentsTicketClotureQuery(IDocumentRepository documents, CloisonnementService cloisonnement)
{
    public async Task<IReadOnlyList<Document>> ExecuterAsync(Guid clientId, Guid ticketId, CancellationToken ct = default)
    {
        await cloisonnement.ExigerProprieteClientAsync(clientId, ticketId, ct).ConfigureAwait(false);

        return await documents.ListerParTicketAsync(ticketId, ct).ConfigureAwait(false);
    }
}
