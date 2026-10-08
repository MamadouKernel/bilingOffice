using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Documents;

/// <summary>Levée quand un document n'a pas encore été validé par l'analyse antivirale (SEC-07)
/// — jamais accessible avant ce contrôle (SEC-12).</summary>
public sealed class DocumentNonAccessibleException(string message) : Exception(message);

/// <summary>
/// Contrat C-11 (portion téléchargement). CDC : SEC-12 — aucun accès à un document sans contrôle
/// préalable d'identité et d'habilitation, à CHAQUE accès (pas seulement au dépôt).
/// </summary>
public sealed class TelechargerDocumentQuery(
    IDocumentRepository documents,
    IStockageDocuments stockage,
    IAutorisationService autorisation,
    ITicketRepository tickets,
    CloisonnementService cloisonnement,
    ControleAccesRessourceService controleAcces,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    /// <summary>Téléchargement par un agent habilité sur la file du ticket.</summary>
    public async Task<Stream> TelechargerPourAgentAsync(Guid agentId, Guid documentId, CancellationToken ct = default)
    {
        var document = await documents.ObtenirParIdAsync(documentId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Document {documentId} introuvable.");
        var ticket = await tickets.ObtenirParIdAsync(document.TicketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {document.TicketId} introuvable.");

        // L'accès documentaire suit le droit de VOIR le ticket (07/10/2026) : tout agent CIT qui peut
        // consulter le ticket (il en voit déjà la fiche et la liste des pièces) peut ouvrir ces pièces,
        // quel que soit le module — sans exiger la permission DocumentTelecharger séparée. Celle-ci
        // reste un chemin valable. Un agent hors périmètre reste refusé (consultation refusée).
        var autorise = await autorisation.EstAutoriseAsync(agentId, Permission.DocumentTelecharger, ticket.FileCouranteId, ct).ConfigureAwait(false)
            || await controleAcces.PeutConsulterLeTicketAsync(agentId, ticket, ct).ConfigureAwait(false);
        if (!autorise)
        {
            // Hors périmètre : refus standard de consultation (message et journalisation habituels).
            await controleAcces.ExigerConsultationTicketAsync(agentId, ticket, ct).ConfigureAwait(false);
        }

        return await TelechargerValideAsync(agentId, document, ct).ConfigureAwait(false);
    }

    /// <summary>Téléchargement par le client propriétaire du ticket.</summary>
    public async Task<Stream> TelechargerPourClientAsync(Guid clientId, Guid documentId, CancellationToken ct = default)
    {
        var document = await documents.ObtenirParIdAsync(documentId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Document {documentId} introuvable.");

        await cloisonnement.ExigerProprieteClientAsync(clientId, document.TicketId, ct).ConfigureAwait(false);
        return await TelechargerValideAsync(clientId, document, ct).ConfigureAwait(false);
    }

    private async Task<Stream> TelechargerValideAsync(Guid demandeurId, Domain.Documents.Document document, CancellationToken ct)
    {
        if (!document.EstAccessible)
        {
            throw new DocumentNonAccessibleException(
                $"Le document {document.Id} n'est pas accessible (analyse antivirus : {document.StatutAntivirus}).");
        }

        await journalisation.ConsignerAuditAsync(demandeurId, "DocumentTelecharge", nameof(Domain.Documents.Document), document.Id, null, null, ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        return await stockage.LireAsync(document.CleStockage, ct).ConfigureAwait(false);
    }
}
