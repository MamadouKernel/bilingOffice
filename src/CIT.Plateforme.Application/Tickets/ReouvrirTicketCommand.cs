using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

/// <summary>Contrat C-13 — Rouvrir / Demander la réouverture. CDC : CYC-06 (réouverture par un
/// utilisateur CIT habilité, motivée, tracée — tickets clôturés uniquement, CLAR-02), CYC-07 (le
/// client ne rouvre jamais directement — il peut seulement demander).</summary>
public sealed class ReouvrirTicketCommand(
    ITicketRepository tickets,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    CloisonnementService cloisonnement,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    /// <summary>CYC-06 : réouverture par un utilisateur CIT habilité — motif obligatoire.</summary>
    public async Task RouvrirAsync(Guid agentId, Guid ticketId, string motif, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(motif))
        {
            throw new ArgumentException("Le motif de réouverture est obligatoire (CYC-06).", nameof(motif));
        }

        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        await autorisation.ExigerAsync(agentId, Permission.TicketRouvrir, ticket.FileCouranteId, ct).ConfigureAwait(false);

        // CLAR-02 appliqué par Ticket.Rouvrir() elle-même (lève si le ticket n'est pas clôturé).
        ticket.Rouvrir(horloge.MaintenantUtc);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "TicketRouvert", motif, agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "TicketRouvert", nameof(Ticket), ticket.Id, null, motif, ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("TicketRouvert", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    /// <summary>CYC-07 : le client ne rouvre jamais directement — il transmet une demande.</summary>
    public async Task DemanderReouvertureAsync(Guid clientId, Guid ticketId, string motif, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(motif))
        {
            throw new ArgumentException("Le motif de la demande est obligatoire (CYC-06/07).", nameof(motif));
        }

        await cloisonnement.ExigerProprieteClientAsync(clientId, ticketId, ct).ConfigureAwait(false);

        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        if (ticket.Statut != StatutTicket.Cloture)
        {
            throw new InvalidOperationException("Seul un ticket clôturé peut faire l'objet d'une demande de réouverture (CLAR-02).");
        }

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "DemandeReouvertureSoumise", motif, clientId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(clientId, "DemandeReouvertureSoumise", nameof(Ticket), ticket.Id, null, motif, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
