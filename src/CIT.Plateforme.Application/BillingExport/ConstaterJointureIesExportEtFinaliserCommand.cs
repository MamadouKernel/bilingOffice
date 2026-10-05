using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingExport;

/// <summary>
/// Contrat C5-08 (jointure & finalisation IES) — Constate l'achèvement des deux branches (Back Office + Contrôle Doc) et clôture le ticket. CDC : EXP-03, EXP-04.
/// </summary>
public sealed class ConstaterJointureIesExportEtFinaliserCommand(
    ITicketRepository tickets,
    ISuiviBrancheIesRepository suivisBranchesIes,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid auteurId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        var autoriseBO = await autorisation.EstAutoriseAsync(
            auteurId, Permission.BillingExportIesBackOfficeInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false);
        var autoriseDoc = await autorisation.EstAutoriseAsync(
            auteurId, Permission.BillingExportIesControleDocumentaireInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false);

        if (!autoriseBO && !autoriseDoc)
        {
            throw new AccesRefuseException(
                $"L'utilisateur {auteurId} ne détient aucune des permissions de branche IES requises pour finaliser le ticket {ticketId}.");
        }

        var suivis = await suivisBranchesIes.ListerParTicketAsync(ticketId, ct).ConfigureAwait(false);
        if (suivis.Count == 0 || suivis.Any(s => !s.EstTerminee))
        {
            throw new JointureIesIncompleteException(ticketId);
        }

        var maintenant = horloge.MaintenantUtc;
        await journalisation.ConsignerHistoriqueAsync(ticketId, "JointureIesExportConstatee", null, auteurId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(auteurId, "JointureIesExportConstatee", nameof(Ticket), ticket.Id, null, null, ct).ConfigureAwait(false);

        ticket.Cloturer(MotifClotureType.Normale, maintenant);

        await journalisation.ConsignerHistoriqueAsync(ticketId, "TicketIesExportCloture", null, auteurId, ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("TicketCloture", $"{{\"ticketId\":\"{ticketId}\"}}", ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(auteurId, "TicketIesExportCloture", nameof(Ticket), ticket.Id, null, null, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
