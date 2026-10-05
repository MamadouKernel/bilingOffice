using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

/// <summary>Levée quand l'annulation directe n'est plus recevable (CYC-01) — seule une demande
/// d'annulation l'est alors (CYC-02). Dérive d'<see cref="InvalidOperationException"/> pour ne
/// pas rompre les appelants qui traitaient déjà ce refus.</summary>
public sealed class AnnulationLibreFermeeException(string message) : InvalidOperationException(message);

/// <summary>
/// Contrats C-12 (client) / C-20 (interne). CDC : CYC-01 (annulation libre, client, avant étape
/// bloquante paramétrable), CYC-02 (demande d'annulation au-delà, décision d'un agent habilité),
/// CYC-03 (motivation et traçabilité systématiques), CYC-05 (règle d'annulation interne).
/// <para>
/// REMÉDIATION P1-01. La commande <b>recevait</b> auparavant la réponse en paramètre
/// (<c>etapeBloquanteAtteinte</c>), à charge pour l'appelant de la calculer. Son unique appelant
/// passait <c>false</c> en dur : la garde CYC-01 ne s'est jamais déclenchée en production. Elle
/// décide désormais elle-même, à partir de deux sources et d'elles seules :
/// </para>
/// <list type="number">
///   <item><b>l'état engageant du ticket</b> — paiement confirmé ou document officiel produit,
///   posé par les commandes métier qui les produisent ;</item>
///   <item><b>le seuil paramétrable par workflow</b> — <see cref="EtapeBloquanteAnnulation"/> :
///   le ticket a-t-il traversé une file déclarée bloquante pour son module et son type ?</item>
/// </list>
/// Aucun appelant ne peut plus contourner la règle, parce qu'aucun appelant ne la porte.
/// </summary>
public sealed class AnnulerTicketCommand(
    ITicketRepository tickets,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    CloisonnementService cloisonnement,
    IEtapeBloquanteAnnulationRepository etapesBloquantes,
    IPassageEnFileRepository passages,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    /// <summary>CYC-01 : annulation libre par le client, tant que le seuil n'est pas franchi.</summary>
    public async Task AnnulerLibrementAsync(Guid clientId, Guid ticketId, string motif, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(motif))
        {
            throw new ArgumentException("Le motif d'annulation est obligatoire (CYC-03).", nameof(motif));
        }

        await cloisonnement.ExigerProprieteClientAsync(clientId, ticketId, ct).ConfigureAwait(false);

        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        var blocage = await MotifDeBlocageAsync(ticket, ct).ConfigureAwait(false);
        if (blocage is not null)
        {
            throw new AnnulationLibreFermeeException(
                $"L'annulation directe n'est plus possible : {blocage} Seule une demande d'annulation est recevable (CYC-02).");
        }

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

    /// <summary>
    /// CYC-01 — le seuil est-il franchi, et pourquoi ? Retourne <c>null</c> tant que l'annulation
    /// directe reste ouverte. Le motif est restitué au client : lui refuser une action sans lui
    /// dire ce qui l'a fermée serait incompréhensible.
    /// </summary>
    public async Task<string?> MotifDeBlocageAsync(Ticket ticket, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        if (ticket.EstEnEtatEngageant)
        {
            return "un traitement métier engageant ou un document officiel a déjà été produit.";
        }

        var seuils = await etapesBloquantes.ListerActifsAsync(ct).ConfigureAwait(false);
        var applicables = seuils.Where(s => s.SApplique(ticket.Module, ticket.TypeDemande)).ToList();
        if (applicables.Count == 0)
        {
            return null;
        }

        // Le seuil est franchi dès que la file a été TRAVERSÉE, pas seulement occupée : un ticket
        // reparti ailleurs a tout de même engagé CIT au passage.
        var filesTraversees = (await passages.ListerParTicketAsync(ticket.Id, ct).ConfigureAwait(false))
            .Select(p => p.FileId)
            .ToHashSet();
        filesTraversees.Add(ticket.FileCouranteId);

        return applicables.FirstOrDefault(s => filesTraversees.Contains(s.FileId))?.Motif;
    }

    /// <summary>CYC-02/CYC-05 : décision d'un agent habilité sur une demande d'annulation.</summary>
    public async Task DeciderAnnulationAsync(Guid agentId, Guid ticketId, bool accepter, string motifDecision, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        await autorisation.ExigerAsync(agentId, Permission.TicketAnnuler, ticket.FileCouranteId, ct).ConfigureAwait(false);
        await global::CIT.Plateforme.Application.Securite.GardeAgentAffecte.ExigerAsync(autorisation, agentId, ticket, ct).ConfigureAwait(false);

        if (accepter)
        {
            ticket.Annuler(horloge.MaintenantUtc);
        }

        await journalisation.ConsignerHistoriqueAsync(
            ticket.Id, accepter ? "DemandeAnnulationAcceptee" : "DemandeAnnulationRejetee", motifDecision, agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(
            agentId, accepter ? "DemandeAnnulationAcceptee" : "DemandeAnnulationRejetee", nameof(Ticket), ticket.Id, null, motifDecision, ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync(
            accepter ? "DemandeAnnulationAcceptee" : "DemandeAnnulationRejetee", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
