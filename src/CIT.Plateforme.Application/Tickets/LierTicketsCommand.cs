using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

/// <summary>SOC-10 : liaison entre tickets (y compris entre modules), créée par un agent
/// habilité, visible par les agents et par le client concerné.</summary>
public sealed class LierTicketsCommand(
    ILiaisonTicketRepository liaisons,
    ITicketRepository tickets,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid agentId, Guid ticketId, Guid ticketLieId, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");
        _ = await tickets.ObtenirParIdAsync(ticketLieId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketLieId} introuvable.");

        await autorisation.ExigerAsync(agentId, Permission.TicketPrendreEnCharge, ticket.FileCouranteId, ct).ConfigureAwait(false);

        var liaison = LiaisonTicket.Creer(ticketId, ticketLieId, agentId, horloge.MaintenantUtc);
        await liaisons.AjouterAsync(liaison, ct).ConfigureAwait(false);

        await journalisation.ConsignerHistoriqueAsync(ticketId, "TicketLie", $"lieA={ticketLieId}", agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "TicketLie", nameof(LiaisonTicket), liaison.Id, null, $"{ticketId}<->{ticketLieId}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// SOC-10 depuis une fiche : l'agent désigne l'autre ticket par son NUMÉRO, seule référence
    /// qu'il a sous les yeux. Résoudre l'identifiant ici plutôt que dans la page évite d'exposer
    /// une recherche de ticket non cloisonnée à la couche Web.
    /// </summary>
    public async Task ExecuterParNumeroAsync(Guid agentId, Guid ticketId, string numeroTicketLie, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(numeroTicketLie))
        {
            throw new ArgumentException("Le numéro du ticket à lier est obligatoire.", nameof(numeroTicketLie));
        }

        var lie = await tickets.ObtenirParNumeroAsync(numeroTicketLie.Trim(), ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Aucun ticket ne porte le numéro « {numeroTicketLie.Trim()} ».");

        await ExecuterAsync(agentId, ticketId, lie.Id, ct).ConfigureAwait(false);
    }
}
