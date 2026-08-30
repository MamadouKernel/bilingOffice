using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingExport;

/// <summary>
/// Contrat C5-06 — Valider ou refuser la preuve de paiement Export. CDC : EXP-03.
/// </summary>
public sealed class ValiderPreuvePaiementExportCommand(
    ITicketRepository tickets,
    IFileDeTraitementRepository files,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ValiderAsync(Guid agentId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await ChargerEtAutoriserAsync(agentId, ticketId, ct).ConfigureAwait(false);
        var maintenant = horloge.MaintenantUtc;

        var toutesLesFiles = await files.ListerAsync(ct).ConfigureAwait(false);
        var fileControleDocumentaire = toutesLesFiles.SingleOrDefault(f => f.Module == SoumettreDemandeExportCommand.Module && f.Nom == NomsFilesExport.ControleDocumentaire)
            ?? throw new InvalidOperationException($"File « {NomsFilesExport.ControleDocumentaire} » non paramétrée.");

        ticket.Transferer(fileControleDocumentaire.Id, destinataireId: null, maintenant);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "PaiementExportValide", null, agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "PaiementExportValide", nameof(Ticket), ticket.Id, null, null, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    public async Task RefuserAsync(Guid agentId, Guid ticketId, string motif, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(motif))
        {
            throw new ArgumentException("Le motif de refus de la preuve de paiement est obligatoire.", nameof(motif));
        }

        var ticket = await ChargerEtAutoriserAsync(agentId, ticketId, ct).ConfigureAwait(false);
        var maintenant = horloge.MaintenantUtc;

        if (ticket.Statut == StatutTicket.EnAttenteDePriseEnCharge)
        {
            ticket.PrendreEnCharge(agentId, maintenant);
        }

        ticket.MettreEnAttenteClient(maintenant);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "PreuvePaiementExportRefusee", motif, agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "PreuvePaiementExportRefusee", nameof(Ticket), ticket.Id, null, motif, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    private async Task<Ticket> ChargerEtAutoriserAsync(Guid agentId, Guid ticketId, CancellationToken ct)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        await autorisation.ExigerAsync(agentId, Permission.BillingExportPaiementValider, ticket.FileCouranteId, ct).ConfigureAwait(false);
        return ticket;
    }
}
