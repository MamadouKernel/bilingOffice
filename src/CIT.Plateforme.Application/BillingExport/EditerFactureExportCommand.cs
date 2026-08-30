using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Documents;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingExport;

/// <summary>
/// Contrat C5-05 — Éditer et joindre la facture Export, puis orienter selon le type de client (EXP-03).
/// </summary>
public sealed class EditerFactureExportCommand(
    ITicketRepository tickets,
    IDossierRepository dossiers,
    IFileDeTraitementRepository files,
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

        await autorisation.ExigerAsync(agentId, Permission.BillingExportFacturationEditer, ticket.FileCouranteId, ct).ConfigureAwait(false);

        await deposerDocument.DeposerPourAgentAsync(agentId, ticketId, nomFichier, typeContenu, contenu, tailleOctets, reglesEtape, ct).ConfigureAwait(false);

        var dossier = await dossiers.ObtenirParIdAsync(ticket.DossierId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Dossier {ticket.DossierId} introuvable.");
        var typeClient = dossier.TypeClient
            ?? throw new InvalidOperationException($"Dossier {dossier.Id} sans instantané de type client.");

        var toutesLesFiles = await files.ListerAsync(ct).ConfigureAwait(false);
        var nomFileCible = typeClient switch
        {
            TypeClient.Normal => NomsFilesExport.ValidationPaiement,
            TypeClient.EnCompte => NomsFilesExport.ControleDocumentaire, // EXP-03 : transition directe vers contrôle doc
            TypeClient.Deposit => NomsFilesExport.ComptabiliteClient,
            _ => throw new InvalidOperationException($"EditerFactureExportCommand ne s'applique pas au type {typeClient}."),
        };

        var fileCible = toutesLesFiles.SingleOrDefault(f => f.Module == SoumettreDemandeExportCommand.Module && f.Nom == nomFileCible)
            ?? throw new InvalidOperationException($"File « {nomFileCible} » (module {SoumettreDemandeExportCommand.Module}) non paramétrée.");

        var maintenant = horloge.MaintenantUtc;
        ticket.Transferer(fileCible.Id, destinataireId: null, maintenant);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "FactureExportEditee", nomFichier, agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "FactureExportEditee", nameof(Ticket), ticket.Id, null, nomFichier, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
