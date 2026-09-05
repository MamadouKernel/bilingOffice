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
    IUtilisateurInfoRepository utilisateurs,
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

    /// <summary>
    /// MSG-04 depuis une fiche : l'agent désigne son collaborateur par son adresse
    /// professionnelle, la seule qu'il connaisse — personne ne retient un GUID. La résolution
    /// vit ici plutôt que dans la page, pour ne pas exposer une recherche d'annuaire à la
    /// couche Web.
    /// </summary>
    public async Task<Message> MentionnerParEmailAsync(
        Guid agentId, Guid ticketId, string emailCollaborateur, string contenu, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(emailCollaborateur))
        {
            throw new ArgumentException("L'adresse du collaborateur à mentionner est obligatoire.", nameof(emailCollaborateur));
        }

        var collaborateur = await utilisateurs.ObtenirParEmailAsync(emailCollaborateur.Trim(), ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Aucun compte ne porte l'adresse « {emailCollaborateur.Trim()} ».");

        if (collaborateur.Nature == NatureUtilisateur.Client)
        {
            // MSG-02 : la note interne n'est jamais visible d'un client. Y mentionner un compte
            // client lui adresserait une notification sur un contenu qu'il ne peut pas lire.
            throw new InvalidOperationException("Une note interne ne peut mentionner qu'un collaborateur interne.");
        }

        return await MentionnerAsync(agentId, ticketId, collaborateur.Id, contenu, ct).ConfigureAwait(false);
    }
}
