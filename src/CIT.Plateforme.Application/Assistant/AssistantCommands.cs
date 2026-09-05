using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Messagerie;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Assistant;

namespace CIT.Plateforme.Application.Assistant;

public sealed record MessageAssistantVue(string Auteur, string Contenu, DateTime HorodatageUtc, int Rang);

public sealed record ConversationAssistantVue(
    Guid Id,
    IReadOnlyList<MessageAssistantVue> Messages,
    bool EscaladeRecommandee,
    string? MotifEscaladeRecommandee,
    bool EstEscaladee,
    Guid? TicketId);

/// <summary>
/// RG-IA-01 — l'assistant répond en français aux questions de premier niveau, oriente l'usager et
/// l'aide à compléter ses dossiers (IA-01, IA-02).
///
/// Une seule conversation ouverte par usager : rouvrir un fil à chaque question ferait perdre le
/// contexte que RG-IA-02 demande justement de conserver.
/// </summary>
// P3 — `IJournalisationService` a été retiré : il n'était pas lu (avertissement CS9113).
// Poser une question à l'assistant n'est pas une « action significative » au sens de RG-SEC-03,
// qui énumère création, prise en charge, changement de statut, dépôt, décision, transfert,
// réaffectation, téléchargement et connexion. L'ESCALADE, elle, en est une — et
// `EscaladerAssistantVersOperateurCommand` la consigne. Garder une dépendance inutilisée aurait
// laissé croire à une trace qui n'existait pas.
public sealed class PoserQuestionAssistantCommand(
    IConversationAssistantRepository conversations,
    IAssistantConversationnel assistant,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<ConversationAssistantVue> ExecuterAsync(
        Guid utilisateurId, string question, CancellationToken ct = default)
    {
        if (utilisateurId == Guid.Empty)
        {
            throw new AccesRefuseException("L'assistant n'est accessible qu'à un usager authentifié (SEC-01).");
        }

        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException("La question ne peut pas être vide.", nameof(question));
        }

        var maintenant = horloge.MaintenantUtc;

        var conversation = await conversations.ObtenirOuverteParUtilisateurAsync(utilisateurId, ct).ConfigureAwait(false);
        if (conversation is null)
        {
            conversation = ConversationAssistant.Ouvrir(utilisateurId, maintenant);
            await conversations.AjouterAsync(conversation, ct).ConfigureAwait(false);
        }

        // L'historique est transmis AVANT d'y ajouter la question courante : le port reçoit le
        // contexte d'un côté et la question de l'autre, sans ambiguïté sur ce qu'il doit traiter.
        var historique = conversation.Messages.OrderBy(m => m.Rang).ToList();

        conversation.AjouterMessage(AuteurMessageAssistant.Client, question, maintenant);

        var reponse = await assistant.RepondreAsync(historique, question, ct).ConfigureAwait(false);
        conversation.AjouterMessage(AuteurMessageAssistant.Assistant, reponse.Texte, maintenant);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        return Projeter(conversation, reponse.EscaladeRecommandee, reponse.MotifEscalade);
    }

    internal static ConversationAssistantVue Projeter(
        ConversationAssistant conversation, bool escaladeRecommandee, string? motif) =>
        new(
            conversation.Id,
            [.. conversation.Messages
                .OrderBy(m => m.Rang)
                .Select(m => new MessageAssistantVue(
                    m.Auteur == AuteurMessageAssistant.Client ? "Vous" : "Assistant",
                    m.Contenu, m.HorodatageUtc, m.Rang))],
            escaladeRecommandee,
            motif,
            conversation.EstEscaladee,
            conversation.TicketId);
}

/// <summary>
/// RG-IA-02 — escalade vers un opérateur humain, <b>sans perte de contexte</b> : le fil est
/// rattaché au ticket, et sa transcription y est publiée pour que l'opérateur reprenne exactement
/// là où l'assistant s'est arrêté (IA-03, IA-06).
///
/// RÉUTILISE LA MESSAGERIE DU TICKET plutôt que d'inventer un second canal : le fil du ticket est
/// déjà le canal de référence des échanges (RG-COM-03).
///
/// LA TRANSCRIPTION EST UN MESSAGE CLIENT, PAS UNE NOTE INTERNE. Une première version la publiait
/// en note interne — ce que la messagerie a refusé, à juste titre : écrire une note interne exige
/// <c>message.note_interne</c>, permission d'agent qu'un client n'a jamais. Le refus était le bon
/// comportement, et il pointait une erreur de conception : cette transcription est constituée des
/// propres mots du client et des réponses qui lui ont été faites. Elle n'a rien d'interne, et la
/// masquer à son auteur n'aurait eu aucun sens.
/// </summary>
public sealed class EscaladerAssistantVersOperateurCommand(
    IConversationAssistantRepository conversations,
    PublierMessageCommand messages,
    CloisonnementService cloisonnement,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(
        Guid utilisateurId, Guid conversationId, Guid ticketId, string motif, CancellationToken ct = default)
    {
        var conversation = await conversations.ObtenirParIdAsync(conversationId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Conversation {conversationId} introuvable.");

        // CPT-04 — un usager n'escalade que SON fil, et seulement vers un ticket qui lui appartient.
        if (conversation.UtilisateurId != utilisateurId)
        {
            throw new AccesRefuseException(
                $"La conversation {conversationId} n'appartient pas à l'utilisateur {utilisateurId} (CPT-04).");
        }

        await cloisonnement.ExigerProprieteClientAsync(utilisateurId, ticketId, ct).ConfigureAwait(false);

        if (conversation.EstEscaladee)
        {
            return;
        }

        var transcription = conversation.Transcrire();
        conversation.Escalader(ticketId, motif, horloge.MaintenantUtc);

        // C'est cette publication qui matérialise « sans perte de contexte » : l'opérateur lit le
        // fil complet dans le ticket, sans avoir à ouvrir l'assistant.
        await messages.PublierMessageClientAsync(
            utilisateurId,
            ticketId,
            $"Escalade de l'assistant virtuel — motif : {motif}"
                + Environment.NewLine + Environment.NewLine
                + transcription,
            ct).ConfigureAwait(false);

        await journalisation.ConsignerHistoriqueAsync(
            ticketId, "AssistantEscaladeVersOperateur", motif, utilisateurId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(
            utilisateurId, "AssistantEscaladeVersOperateur", nameof(ConversationAssistant), conversation.Id,
            null, ticketId.ToString(), ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync(
            "AssistantEscaladeVersOperateur", $"{{\"ticketId\":\"{ticketId}\"}}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}

/// <summary>Consultation du fil courant de l'usager (RG-IA-01).</summary>
public sealed class ConsulterConversationAssistantQuery(IConversationAssistantRepository conversations)
{
    public async Task<ConversationAssistantVue?> ExecuterAsync(Guid utilisateurId, CancellationToken ct = default)
    {
        if (utilisateurId == Guid.Empty)
        {
            return null;
        }

        var conversation = await conversations.ObtenirOuverteParUtilisateurAsync(utilisateurId, ct).ConfigureAwait(false);
        return conversation is null
            ? null
            : PoserQuestionAssistantCommand.Projeter(conversation, escaladeRecommandee: false, motif: null);
    }
}
