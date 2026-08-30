using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Documents;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingExport;

/// <summary>
/// Contrat C5-07 — Dépôt des documents de contrôle (BEE) et clôture du ticket Billing EXPORT. CDC : EXP-04.
/// </summary>
public sealed class FinaliserControleDocumentaireExportCommand(
    ITicketRepository tickets,
    DeposerDocumentCommand deposerDocument,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(
        Guid agentId,
        Guid ticketId,
        string nomFichier,
        string typeContenu,
        Stream contenu,
        long tailleOctets,
        ReglesDepotDocument reglesEtape,
        CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        await autorisation.ExigerAsync(agentId, Permission.BillingExportControleDocumentaireTraiter, ticket.FileCouranteId, ct).ConfigureAwait(false);

        await deposerDocument.DeposerPourAgentAsync(agentId, ticketId, nomFichier, typeContenu, contenu, tailleOctets, reglesEtape, ct).ConfigureAwait(false);

        var maintenant = horloge.MaintenantUtc;
        ticket.Cloturer(MotifClotureType.Normale, maintenant);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "ControleDocumentaireEtClotureExport", nomFichier, agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "ControleDocumentaireEtClotureExport", nameof(Ticket), ticket.Id, null, nomFichier, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
