using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Messagerie;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Messagerie;

/// <summary>Contrat C-10 — Échanger par la messagerie du ticket. CDC : SOC-05, MSG-01, MSG-02,
/// MSG-03.</summary>
public sealed class PublierMessageCommand(
    IMessageRepository messages,
    ITicketRepository tickets,
    IAutorisationService autorisation,
    CloisonnementService cloisonnement,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    /// <summary>SOC-05/MSG-01 : message client ↔ agent, visible des deux côtés.</summary>
    public async Task<Message> PublierMessageClientAsync(Guid clientId, Guid ticketId, string contenu, CancellationToken ct = default)
    {
        await cloisonnement.ExigerProprieteClientAsync(clientId, ticketId, ct).ConfigureAwait(false);
        return await PublierAsync(ticketId, clientId, TypeMessage.Externe, contenu, ct).ConfigureAwait(false);
    }

    public async Task<Message> PublierMessageAgentAsync(Guid agentId, Guid ticketId, string contenu, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");
        await autorisation.ExigerAsync(agentId, Permission.TicketPrendreEnCharge, ticket.FileCouranteId, ct).ConfigureAwait(false);

        return await PublierAsync(ticketId, agentId, TypeMessage.Externe, contenu, ct).ConfigureAwait(false);
    }

    /// <summary>MSG-02 : note interne — jamais visible du client (garanti par
    /// <see cref="IMessageRepository.ListerVisiblesParClientAsync"/>, jamais par ce seul type).</summary>
    public async Task<Message> PublierNoteInterneAsync(Guid agentId, Guid ticketId, string contenu, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");
        await autorisation.ExigerAsync(agentId, Permission.MessageNoteInterne, ticket.FileCouranteId, ct).ConfigureAwait(false);

        return await PublierAsync(ticketId, agentId, TypeMessage.NoteInterne, contenu, ct).ConfigureAwait(false);
    }

    private async Task<Message> PublierAsync(Guid ticketId, Guid auteurId, TypeMessage type, string contenu, CancellationToken ct)
    {
        var message = Message.Publier(ticketId, auteurId, type, contenu, horloge.MaintenantUtc);
        await messages.AjouterAsync(message, ct).ConfigureAwait(false);

        await journalisation.ConsignerHistoriqueAsync(ticketId, "MessagePublie", type.ToString(), auteurId, ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("MessagePoste", $"{{\"ticketId\":\"{ticketId}\",\"type\":\"{type}\"}}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
        return message;
    }
}
