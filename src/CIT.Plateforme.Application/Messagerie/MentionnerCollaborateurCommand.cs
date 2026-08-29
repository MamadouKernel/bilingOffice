using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Messagerie;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Messagerie;

/// <summary>
/// MSG-04/MSG-05 : un collaborateur peut être mentionné (@Nom) dans une note interne ; il reçoit
/// une notification, et le ticket reste sous la responsabilité de son opérateur courant — une
/// mention n'entraîne JAMAIS de réaffectation (aucun appel à un mécanisme d'affectation ici, par
/// construction).
/// </summary>
public sealed class MentionnerCollaborateurCommand(
    IMessageRepository messages,
    ITicketRepository tickets,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<Message> MentionnerAsync(Guid agentId, Guid ticketId, Guid collaborateurMentionneId, string contenu, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");
        await autorisation.ExigerAsync(agentId, Permission.MessageNoteInterne, ticket.FileCouranteId, ct).ConfigureAwait(false);

        var operateurAvantMention = ticket.OperateurAssigneId;

        var message = Message.Publier(ticketId, agentId, TypeMessage.NoteInterne, contenu, horloge.MaintenantUtc, collaborateurMentionneId);
        await messages.AjouterAsync(message, ct).ConfigureAwait(false);

        // MSG-05 : le ticket reste sous la responsabilité de son opérateur courant — aucune
        // modification de ticket.OperateurAssigneId n'a lieu ici.
        await journalisation.ConsignerHistoriqueAsync(
            ticketId, "CollaborateurMentionne", $"mentionne={collaborateurMentionneId}", agentId, ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync(
            "CollaborateurMentionne", $"{{\"ticketId\":\"{ticketId}\",\"collaborateurId\":\"{collaborateurMentionneId}\"}}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        System.Diagnostics.Debug.Assert(ticket.OperateurAssigneId == operateurAvantMention, "MSG-05 : une mention ne réaffecte jamais le ticket.");
        return message;
    }
}
