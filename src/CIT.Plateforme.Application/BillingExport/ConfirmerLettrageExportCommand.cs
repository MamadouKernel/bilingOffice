using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingExport;

/// <summary>
/// Contrat C5-06 (variante Deposit) — Confirmation de lettrage par la comptabilité client (EXP-03).
/// </summary>
public sealed class ConfirmerLettrageExportCommand(
    ITicketRepository tickets,
    IFileDeTraitementRepository files,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid agentId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        await autorisation.ExigerAsync(agentId, Permission.BillingExportComptabiliteConfirmerLettrage, ticket.FileCouranteId, ct).ConfigureAwait(false);

        var toutesLesFiles = await files.ListerAsync(ct).ConfigureAwait(false);
        var fileControleDocumentaire = toutesLesFiles.SingleOrDefault(f => f.Module == SoumettreDemandeExportCommand.Module && f.Nom == NomsFilesExport.ControleDocumentaire)
            ?? throw new InvalidOperationException($"File « {NomsFilesExport.ControleDocumentaire} » non paramétrée.");

        var maintenant = horloge.MaintenantUtc;
        ticket.Transferer(fileControleDocumentaire.Id, destinataireId: null, maintenant);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "LettrageExportConfirme", null, agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "LettrageExportConfirme", nameof(Ticket), ticket.Id, null, null, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
