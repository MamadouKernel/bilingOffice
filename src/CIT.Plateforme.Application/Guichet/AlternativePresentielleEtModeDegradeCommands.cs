using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Guichet;

/// <summary>
/// FEATURE 010 (IMP-15, C10-10) — Bascule vers l'alternative présentielle au Billing Office à l'issue du contrôle BL Import.
/// </summary>
public sealed class BasculerAlternativePresentielleCommand(
    ITicketRepository ticketRepo,
    IDossierRepository dossierRepo,
    IJournalisationService journal,
    IUniteDeTravail uniteDeTravail,
    IHorloge horloge)
{
    public async Task ExecuterAsync(Guid demandeurId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await ticketRepo.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Ticket introuvable.");

        var dossier = await dossierRepo.ObtenirParIdAsync(ticket.DossierId, ct).ConfigureAwait(false);

        // Anti-IDOR
        if (dossier is not null && dossier.AuteurId.HasValue && dossier.AuteurId.Value != demandeurId && dossier.ClientId != demandeurId)
        {
            throw new AccesRefuseException("Vous n'avez pas l'autorisation de modifier ce ticket.");
        }

        var maintenant = horloge.MaintenantUtc;

        ticket.DeclarerFinalisationPresentielle();

        // Trace dans l'historique du ticket
        await journal.ConsignerHistoriqueAsync(
            ticket.Id,
            "BasculeAlternativePresentielle",
            "Bascule vers l'alternative présentielle (passage physique au Billing Office / CON-01, IMP-15)",
            demandeurId,
            ct).ConfigureAwait(false);

        var chargeUtile = JsonSerializer.Serialize(new
        {
            TicketId = ticket.Id,
            NumeroTicket = ticket.Numero,
            Module = ticket.Module,
            HorodatageUtc = maintenant
        });

        await journal.PublierEvenementAsync("BasculePresentielleEffectuee", chargeUtile, ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}

public sealed record RegulariserModeDegradeRequest(
    Guid AgentId,
    Guid TicketId,
    DateTime DateOperationReelleUtc,
    string DescriptionOperation
);

/// <summary>
/// FEATURE 010 (ERR-04, C10-11) — Régularisation d'une opération physique exécutée en mode dégradé lors d'une indisponibilité.
/// </summary>
public sealed class RegulariserModeDegradeCommand(
    ITicketRepository ticketRepo,
    IAutorisationService autorisation,
    IJournalisationService journal,
    IUniteDeTravail uniteDeTravail,
    IHorloge horloge)
{
    public async Task ExecuterAsync(RegulariserModeDegradeRequest request, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(request.AgentId, Permission.GuichetRecherche, null, ct).ConfigureAwait(false);

        var ticket = await ticketRepo.ObtenirParIdAsync(request.TicketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Ticket introuvable.");

        var maintenant = horloge.MaintenantUtc;

        // Trace spécifique dans l'historique du ticket
        await journal.ConsignerHistoriqueAsync(
            ticket.Id,
            "ModeDegradeRegularise",
            $"[MODE DÉGRADÉ] Régularisation de l'opération physique du {request.DateOperationReelleUtc:dd/MM/yyyy HH:mm} : {request.DescriptionOperation}",
            request.AgentId,
            ct).ConfigureAwait(false);

        // Audit immuable de régularisation
        await journal.ConsignerAuditAsync(
            request.AgentId,
            "ModeDegrade.Regularisation",
            nameof(Ticket),
            ticket.Id,
            null,
            $"Régularisation sur ticket {ticket.Numero} : {request.DescriptionOperation} (opération réelle du {request.DateOperationReelleUtc:yyyy-MM-dd HH:mm:ss})",
            ct).ConfigureAwait(false);

        var chargeUtile = JsonSerializer.Serialize(new
        {
            TicketId = ticket.Id,
            NumeroTicket = ticket.Numero,
            DateOperationReelleUtc = request.DateOperationReelleUtc,
            DateRegularisationUtc = maintenant,
            AgentId = request.AgentId
        });

        await journal.PublierEvenementAsync("ModeDegradeRegularise", chargeUtile, ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
