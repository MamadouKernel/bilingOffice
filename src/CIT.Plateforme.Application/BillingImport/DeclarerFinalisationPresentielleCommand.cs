using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingImport;

/// <summary>
/// Contrat C3-19 — Déclarer une finalisation présentielle. CDC : IMP-15 (Should). research.md D12 :
/// point d'extension minimal pour la future Feature « Continuité Digital/Présentiel » (CON-01/02/03)
/// — aucun mécanisme de rendez-vous, de recherche de guichet, ou de créneau n'est implémenté ici.
/// </summary>
public sealed class DeclarerFinalisationPresentielleCommand(
    ITicketRepository tickets,
    IFileDeTraitementRepository files,
    CloisonnementService cloisonnement,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid clientId, Guid ticketId, CancellationToken ct = default)
    {
        await cloisonnement.ExigerProprieteClientAsync(clientId, ticketId, ct).ConfigureAwait(false);

        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        var toutesLesFiles = await files.ListerAsync(ct).ConfigureAwait(false);
        var filePiloteManifeste = toutesLesFiles.SingleOrDefault(f =>
            f.Module == SoumettreDemandeImportCommand.Module && f.Nom == SoumettreDemandeImportCommand.FilePiloteManifeste);

        // C3-19 : précondition « ticket validé par le Pilote Manifeste (à l'issue du contrôle BL) ».
        // Un ticket rejeté ou en attente de complément n'a jamais quitté la file Pilote Manifeste
        // (RejeterTicketImportCommand/GestionComplementManifesteCommand n'y transfèrent pas le
        // ticket) — seule la validation (aiguillage, D5) en fait sortir le ticket.
        if (filePiloteManifeste is not null && ticket.FileCouranteId == filePiloteManifeste.Id)
        {
            throw new InvalidOperationException(
                $"Le ticket {ticketId} n'a pas encore été validé par le Pilote Manifeste (IMP-15).");
        }

        ticket.DeclarerFinalisationPresentielle();

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "FinalisationPresentielleDeclaree", null, clientId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(clientId, "FinalisationPresentielleDeclaree", nameof(Ticket), ticket.Id, null, null, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
