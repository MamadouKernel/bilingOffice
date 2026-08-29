using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

/// <summary>
/// Contrats C-12 (client) / C-20 (interne). CDC : CYC-01 (annulation libre, client, avant étape
/// bloquante paramétrable), CYC-02 (demande d'annulation au-delà, décision d'un agent habilité),
/// CYC-03 (motivation et traçabilité systématiques), CYC-05 (règle d'annulation interne).
/// <para>
/// Les « étapes bloquantes » sont un paramètre par workflow (Principe VI) — le socle ne connaît
/// que la distinction annulation libre / demande d'annulation, matérialisée ici par
/// <paramref name="etapeBloquanteAtteinte"/>, fournie par le module appelant selon son propre
/// paramétrage (hors périmètre du socle de connaître le contenu de ce paramètre).
/// </para>
/// </summary>
public sealed class AnnulerTicketCommand(
    ITicketRepository tickets,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    CloisonnementService cloisonnement,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    /// <summary>CYC-01 : annulation libre par le client, uniquement si l'étape bloquante n'est
    /// pas atteinte.</summary>
    public async Task AnnulerLibrementAsync(Guid clientId, Guid ticketId, string motif, bool etapeBloquanteAtteinte, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(motif))
        {
            throw new ArgumentException("Le motif d'annulation est obligatoire (CYC-03).", nameof(motif));
        }

        await cloisonnement.ExigerProprieteClientAsync(clientId, ticketId, ct).ConfigureAwait(false);

        if (etapeBloquanteAtteinte)
        {
            throw new InvalidOperationException(
                "L'étape bloquante est atteinte : l'annulation libre n'est plus possible, seule une demande d'annulation est recevable (CYC-02).");
        }

        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        ticket.Annuler(horloge.MaintenantUtc);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "TicketAnnuleParClient", motif, clientId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(clientId, "TicketAnnuleParClient", nameof(Ticket), ticket.Id, null, motif, ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("TicketAnnule", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    /// <summary>CYC-02 : au-delà de l'étape bloquante, le client soumet une demande d'annulation
    /// transmise pour décision — elle n'est jamais appliquée automatiquement.</summary>
    public async Task DemanderAnnulationAsync(Guid clientId, Guid ticketId, string motif, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(motif))
        {
            throw new ArgumentException("Le motif de la demande d'annulation est obligatoire (CYC-03).", nameof(motif));
        }

        await cloisonnement.ExigerProprieteClientAsync(clientId, ticketId, ct).ConfigureAwait(false);

        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        // Transmission pour décision (CYC-02) : ne modifie pas le statut, trace la demande.
        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "DemandeAnnulationSoumise", motif, clientId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(clientId, "DemandeAnnulationSoumise", nameof(Ticket), ticket.Id, null, motif, ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("DemandeAnnulationSoumise", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    /// <summary>CYC-02/CYC-05 : décision d'un agent habilité sur une demande d'annulation.</summary>
    public async Task DeciderAnnulationAsync(Guid agentId, Guid ticketId, bool accepter, string motifDecision, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        await autorisation.ExigerAsync(agentId, Permission.TicketAnnuler, ticket.FileCouranteId, ct).ConfigureAwait(false);

        if (accepter)
        {
            ticket.Annuler(horloge.MaintenantUtc);
        }

        await journalisation.ConsignerHistoriqueAsync(
            ticket.Id, accepter ? "DemandeAnnulationAcceptee" : "DemandeAnnulationRejetee", motifDecision, agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(
            agentId, accepter ? "DemandeAnnulationAcceptee" : "DemandeAnnulationRejetee", nameof(Ticket), ticket.Id, null, motifDecision, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
