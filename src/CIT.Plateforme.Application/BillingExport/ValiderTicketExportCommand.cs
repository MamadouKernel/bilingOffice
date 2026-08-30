using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingExport;

public static class NomsFilesExport
{
    public const string BackOfficeExport = "Back Office Export";
    public const string Facturation = "Facturation";
    public const string ValidationPaiement = "Validation Paiement";
    public const string ComptabiliteClient = "Comptabilité Client";
    public const string ControleDocumentaire = "Contrôle Documentaire";
    public const string FileIes = "File IES";
}

/// <summary>
/// Contrat C5-03 — Valider un ticket au Back Office Export, puis aiguillage automatique selon type de client (EXP-02, EXP-03).
/// </summary>
public sealed class ValiderTicketExportCommand(
    ITicketRepository tickets,
    IDossierRepository dossiers,
    IFileDeTraitementRepository files,
    ISuiviBrancheIesRepository suivisBranchesIes,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid agentId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        await autorisation.ExigerAsync(agentId, Permission.BillingExportBackOfficeInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false);

        var dossier = await dossiers.ObtenirParIdAsync(ticket.DossierId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Dossier {ticket.DossierId} introuvable.");

        var typeClient = dossier.TypeClient
            ?? throw new InvalidOperationException($"Dossier {dossier.Id} sans instantané de type client — aiguillage impossible.");

        var toutesLesFiles = await files.ListerAsync(ct).ConfigureAwait(false);
        var maintenant = horloge.MaintenantUtc;

        if (typeClient == TypeClient.IES)
        {
            var fileIes = ObtenirFile(toutesLesFiles, NomsFilesExport.FileIes);
            ticket.Transferer(fileIes.Id, destinataireId: null, maintenant);

            // Branche BackOffice (2) et Branche ControleDocumentaire (1)
            await suivisBranchesIes.AjouterAsync(SuiviBrancheIes.Creer(ticket.Id, BrancheIes.BackOffice), ct).ConfigureAwait(false);
            await suivisBranchesIes.AjouterAsync(SuiviBrancheIes.Creer(ticket.Id, BrancheIes.ControleDocumentaire), ct).ConfigureAwait(false);
        }
        else
        {
            var fileFacturation = ObtenirFile(toutesLesFiles, NomsFilesExport.Facturation);
            ticket.Transferer(fileFacturation.Id, destinataireId: null, maintenant);
        }

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "ValidationBackOfficeExport", $"typeClient={typeClient}", agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "ValidationBackOfficeExport", nameof(Ticket), ticket.Id, null, $"typeClient={typeClient}", ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    private static FileDeTraitement ObtenirFile(IReadOnlyList<FileDeTraitement> toutesLesFiles, string nom) =>
        toutesLesFiles.SingleOrDefault(f => f.Module == SoumettreDemandeExportCommand.Module && f.Nom == nom)
            ?? throw new InvalidOperationException($"File « {nom} » (module {SoumettreDemandeExportCommand.Module}) non paramétrée.");
}
