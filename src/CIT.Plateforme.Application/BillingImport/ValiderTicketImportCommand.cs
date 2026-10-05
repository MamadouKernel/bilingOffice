using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingImport;

/// <summary>
/// Contrat C3-03/C3-07 — Valider un ticket au contrôle BL, puis aiguillage automatique immédiat
/// selon le type de client. CDC : IMP-04, IMP-06.
/// </summary>
public static class NomsFiles
{
    public const string Facturation = "Facturation";
    public const string ValidationPaiement = "Validation Paiement";
    public const string ComptabiliteClient = "Comptabilité Client";
    public const string ControleDocumentaire = "Contrôle Documentaire";
    public const string FileIes = "File IES";
}

public sealed class ValiderTicketImportCommand(
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

        await autorisation.ExigerAsync(agentId, Permission.BillingImportManifesteInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false);

        var dossier = await dossiers.ObtenirParIdAsync(ticket.DossierId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Dossier {ticket.DossierId} introuvable.");

        // research.md D5 (IMP-06) : aiguillage exclusivement piloté par l'instantané figé à la
        // création du dossier — jamais une relecture du type courant de la société (CPT-06 jamais
        // rouverte).
        var typeClient = dossier.TypeClient
            ?? throw new InvalidOperationException($"Dossier {dossier.Id} sans instantané de type client — aiguillage impossible.");

        var toutesLesFiles = await files.ListerAsync(ct).ConfigureAwait(false);
        var maintenant = horloge.MaintenantUtc;

        if (typeClient == TypeClient.IES)
        {
            var fileIes = ObtenirFile(toutesLesFiles, NomsFiles.FileIes);
            ticket.Transferer(fileIes.Id, destinataireId: null, maintenant);

            // research.md D8 : les deux branches démarrent simultanément à l'entrée en File IES.
            await suivisBranchesIes.AjouterAsync(SuiviBrancheIes.Creer(ticket.Id, BrancheIes.Manifeste), ct).ConfigureAwait(false);
            await suivisBranchesIes.AjouterAsync(SuiviBrancheIes.Creer(ticket.Id, BrancheIes.ControleDocumentaire), ct).ConfigureAwait(false);
        }
        else
        {
            // Normal / En Compte / Deposit : tous rejoignent Facturation (D6) — la divergence de
            // parcours n'intervient qu'après édition de la facture (US6/US7).
            var fileFacturation = ObtenirFile(toutesLesFiles, NomsFiles.Facturation);
            ticket.Transferer(fileFacturation.Id, destinataireId: null, maintenant);
        }

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "TicketImportValide", $"typeClient={typeClient}", agentId, ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("TicketValide", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "TicketImportValide", nameof(Ticket), ticket.Id, null, typeClient.ToString(), ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    private static Domain.Files.FileDeTraitement ObtenirFile(IReadOnlyList<Domain.Files.FileDeTraitement> toutesLesFiles, string nom) =>
        toutesLesFiles.SingleOrDefault(f => f.Module == SoumettreDemandeImportCommand.Module && f.Nom == nom)
            ?? throw new InvalidOperationException($"File « {nom} » (module {SoumettreDemandeImportCommand.Module}) non paramétrée — voir quickstart.md §3.");
}
