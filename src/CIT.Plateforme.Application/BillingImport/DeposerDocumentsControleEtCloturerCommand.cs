using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Documents;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingImport;

/// <summary>
/// Contrat C3-12 — Déposer les pièces de contrôle documentaire (BS/BADT) et clôturer le ticket.
/// CDC : IMP-14. Réutilisation intégrale du socle documentaire (D11) ; PC-08 non résolu — la liste
/// des pièces reste paramétrable, jamais nommée en dur au-delà de ce que le CDC cite explicitement.
/// </summary>
public sealed class DeposerDocumentsControleEtCloturerCommand(
    ITicketRepository tickets,
    DeposerDocumentCommand deposerDocument,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(
        Guid agentId, Guid ticketId, string nomFichier, string typeContenu, Stream contenu, long tailleOctets,
        ReglesDepotDocument reglesEtape, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        await autorisation.ExigerAsync(agentId, Permission.BillingImportControleDocumentaireTraiter, ticket.FileCouranteId, ct).ConfigureAwait(false);

        await deposerDocument.DeposerPourAgentAsync(agentId, ticketId, nomFichier, typeContenu, contenu, tailleOctets, reglesEtape, ct).ConfigureAwait(false);

        var maintenant = horloge.MaintenantUtc;
        ticket.Cloturer(MotifClotureType.Normale, maintenant);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "ControleDocumentaireEtClotureImport", nomFichier, agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "ControleDocumentaireEtClotureImport", nameof(Ticket), ticket.Id, null, nomFichier, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
