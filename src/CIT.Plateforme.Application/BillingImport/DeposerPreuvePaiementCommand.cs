using CIT.Plateforme.Application.Documents;
using CIT.Plateforme.Domain.Documents;

namespace CIT.Plateforme.Application.BillingImport;

/// <summary>Contrat C3-10 — Déposer une preuve de paiement. CDC : IMP-12. Réutilisation intégrale
/// du socle documentaire (D11) — aucune extension.</summary>
public sealed class DeposerPreuvePaiementCommand(DeposerDocumentCommand deposerDocument)
{
    public Task ExecuterAsync(
        Guid clientId, Guid ticketId, string nomFichier, string typeContenu, Stream contenu, long tailleOctets,
        ReglesDepotDocument reglesEtape, CancellationToken ct = default) =>
        deposerDocument.DeposerPourClientAsync(clientId, ticketId, nomFichier, typeContenu, contenu, tailleOctets, reglesEtape, ct);
}
