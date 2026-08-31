using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

/// <summary>Contrat C-17 — Mettre en attente client / Reprendre après réponse. CDC : TPS-02,
/// TPS-04, WKF-01. Le temps interne CIT est suspendu pendant l'attente client (TPS-04).</summary>
public sealed class AttenteClientCommand(
    ITicketRepository tickets,
    IPeriodeAttenteClientRepository periodesAttenteClient,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task MettreEnAttenteAsync(Guid agentId, Guid ticketId, string motif, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        if (ticket.Module == "CustomerComplaint")
        {
            await autorisation.ExigerAsync(agentId, Permission.ComplaintInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false);
        }
        else if (ticket.Module == "Enrolement")
        {
            await autorisation.ExigerAsync(agentId, Permission.EnrolementInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false);
        }
        else
        {
            await autorisation.ExigerAsync(agentId, Permission.TicketPrendreEnCharge, ticket.FileCouranteId, ct).ConfigureAwait(false);
        }

        var maintenant = horloge.MaintenantUtc;
        ticket.MettreEnAttenteClient(maintenant);

        // REMÉDIATION AUDIT (post-implementation-audit.md §3.B) : la borne exacte de la période
        // est conservée (entité autonome, voir PeriodeAttenteClient) pour permettre à
        // SurveillerEcheancesUseCase de calculer une durée SLA « ouvrée » cohérente, sans jamais
        // soustraire une durée brute à une durée ouvrée.
        await periodesAttenteClient.AjouterAsync(PeriodeAttenteClient.Ouvrir(ticket.Id, maintenant), ct).ConfigureAwait(false);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "TicketEnAttenteClient", motif, agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "TicketEnAttenteClient", nameof(Ticket), ticket.Id, null, motif, ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("TicketEnAttenteClient", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    public async Task RependreApresReponseAsync(Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        var maintenant = horloge.MaintenantUtc;
        ticket.RependreApresReponseClient(maintenant);

        var periodeOuverte = await periodesAttenteClient.ObtenirDerniereOuvertePourTicketAsync(ticket.Id, ct).ConfigureAwait(false);
        periodeOuverte?.Fermer(maintenant);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "ReponseClientRecue", null, null, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(null, "ReponseClientRecue", nameof(Ticket), ticket.Id, null, null, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
