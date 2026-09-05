using CIT.Plateforme.Domain.Assistant;

namespace CIT.Plateforme.Application.Abstractions;

/// <summary>
/// Réponse de l'assistant à une question de premier niveau.
/// </summary>
/// <param name="Texte">Réponse en français (IA-01).</param>
/// <param name="EscaladeRecommandee">
/// IA-03 / RG-IA-02 — l'assistant estime avoir atteint sa limite. Ce n'est qu'une
/// <b>recommandation</b> : l'escalade reste une action explicite de l'usager, jamais un effet de
/// bord d'une réponse.
/// </param>
/// <param name="MotifEscalade">Ce que l'assistant n'a pas su traiter, transmis à l'opérateur.</param>
public sealed record ReponseAssistant(string Texte, bool EscaladeRecommandee, string? MotifEscalade = null);

/// <summary>
/// Port de l'assistant virtuel (§ 1.15, IA-01, IA-02, IA-03, IA-06).
///
/// AUCUN FOURNISSEUR N'EST DÉSIGNÉ ICI, ni ailleurs dans le code : le choix relève d'un arbitrage
/// DSI non rendu (PC-18). L'implémentation servie aujourd'hui est un adaptateur <b>simulé</b>,
/// déterministe et local ; y substituer un fournisseur réel est un changement d'enregistrement
/// dans la composition, sans effet sur le domaine, les écrans ni les règles.
///
/// L'HISTORIQUE EST PASSÉ EN ENTIER à chaque tour. C'est ce qui rend l'assistant capable de suivre
/// une démarche entamée (IA-02, « l'aide à compléter ses dossiers ») et ce qui permettra à un
/// fournisseur réel de raisonner sur le contexte sans que l'appelant change.
/// </summary>
public interface IAssistantConversationnel
{
    Task<ReponseAssistant> RepondreAsync(
        IReadOnlyList<MessageAssistant> historique, string question, CancellationToken ct = default);
}

/// <summary>Port de persistance des fils de conversation (RG-IA-02).</summary>
public interface IConversationAssistantRepository
{
    Task AjouterAsync(ConversationAssistant conversation, CancellationToken ct = default);

    Task<ConversationAssistant?> ObtenirParIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Le fil ouvert de l'usager, s'il en a un — au plus un à la fois.</summary>
    Task<ConversationAssistant?> ObtenirOuverteParUtilisateurAsync(Guid utilisateurId, CancellationToken ct = default);

    /// <summary>RG-IA-02 — les fils rattachés à un ticket, restitués à l'opérateur qui le reprend.</summary>
    Task<IReadOnlyList<ConversationAssistant>> ListerParTicketAsync(Guid ticketId, CancellationToken ct = default);
}
