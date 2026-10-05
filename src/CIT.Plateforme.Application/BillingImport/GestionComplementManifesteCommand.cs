using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingImport;

/// <summary>
/// Contrat C3-05/C3-06 — Demander un complément (Pilote Manifeste) et le déposer (client). CDC :
/// IMP-05. research.md D13 : réutilise directement <see cref="Ticket.MettreEnAttenteClient"/>/
/// <see cref="Ticket.RependreApresReponseClient"/> (SLA suspendu, TPS-02/04) — même mécanisme que
/// CLAR-PAIEMENT-01 (preuve de paiement invalide, US5).
/// </summary>
public sealed class GestionComplementManifesteCommand(
    ITicketRepository tickets,
    Application.Securite.CloisonnementService cloisonnement,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task DemanderComplementAsync(Guid agentId, Guid ticketId, string elementsAttendus, CancellationToken ct = default)
    {
        // Le client doit savoir quoi fournir : pas de demande de complément sans contenu.
        if (string.IsNullOrWhiteSpace(elementsAttendus))
        {
            throw new ArgumentException("Les éléments attendus du client sont obligatoires.", nameof(elementsAttendus));
        }

        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        await autorisation.ExigerAsync(agentId, Permission.BillingImportManifesteInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false);

        var maintenant = horloge.MaintenantUtc;
        // Ticket.MettreEnAttenteClient exige le statut EnCours (F001) : la demande de complément
        // implique que le Pilote Manifeste a pris en charge le ticket pour l'instruire — prise en
        // charge implicite si ce n'est pas déjà fait, cohérente avec IMP-04 (contrôle BL en cours).
        if (ticket.Statut == StatutTicket.EnAttenteDePriseEnCharge)
        {
            ticket.PrendreEnCharge(agentId, maintenant);
        }

        ticket.MettreEnAttenteClient(maintenant);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "ComplementDemandeImport", elementsAttendus, agentId, ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("ComplementDemande", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "ComplementDemandeImport", nameof(Ticket), ticket.Id, null, elementsAttendus, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    public async Task DeposerComplementAsync(Guid clientId, Guid ticketId, CancellationToken ct = default)
    {
        // Cloisonnement (CPT-04) : seul le propriétaire (société) du ticket peut y déposer un
        // complément — réutilisation intégrale de CloisonnementService (Feature 001, inchangé).
        await cloisonnement.ExigerProprieteClientAsync(clientId, ticketId, ct).ConfigureAwait(false);

        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        var maintenant = horloge.MaintenantUtc;
        ticket.RependreApresReponseClient(maintenant);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "ComplementDeposeImport", null, clientId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(clientId, "ComplementDeposeImport", nameof(Ticket), ticket.Id, null, null, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
