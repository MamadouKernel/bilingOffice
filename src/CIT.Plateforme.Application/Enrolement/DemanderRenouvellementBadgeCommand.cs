using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Enrolement;

/// <summary>
/// Contrat C13-09 (ENR-05) : Demander le renouvellement d'un badge transporteur/chauffeur existant.
/// Crée un nouveau ticket de renouvellement rattaché au même dossier sans duplication.
/// </summary>
public sealed class DemanderRenouvellementBadgeCommand(
    IDossierRepository dossiers,
    ITicketRepository tickets,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IServiceNumerotation numerotation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<Ticket> ExecuterAsync(
        Guid demandeurId,
        Guid dossierId,
        Guid fileId,
        CancellationToken ct = default)
    {
        var dossier = await dossiers.ObtenirParIdAsync(dossierId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Dossier {dossierId} introuvable.");

        // Vérification de sécurité CPT-04 / appartenance
        if (dossier.ClientId != demandeurId)
        {
            await autorisation.ExigerAsync(demandeurId, Permission.EnrolementInstruire, null, ct).ConfigureAwait(false);
        }

        var maintenant = horloge.MaintenantUtc;
        var numeroTicket = await numerotation.ProchainNumeroTicketAsync("Enrolement", "RenouvellementBadge", ct).ConfigureAwait(false);

        var ticket = Ticket.Creer(
            numeroTicket,
            "Enrolement",
            "RenouvellementBadge",
            fileId,
            dossier.Id,
            maintenant);

        await tickets.AjouterAsync(ticket, ct).ConfigureAwait(false);

        await journalisation.ConsignerAuditAsync(
            demandeurId,
            "DemandeRenouvellementBadgeInitiee",
            nameof(Ticket),
            ticket.Id,
            null,
            ticket.Statut.ToString(),
            ct).ConfigureAwait(false);

        await journalisation.ConsignerHistoriqueAsync(
            ticket.Id,
            "DemandeRenouvellementBadgeInitiee",
            null,
            demandeurId,
            ct).ConfigureAwait(false);

        await journalisation.PublierEvenementAsync(
            "DemandeRenouvellementBadgeInitiee",
            $"{{\"ticketId\":\"{ticket.Id}\",\"dossierId\":\"{dossier.Id}\"}}",
            ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        return ticket;
    }
}
