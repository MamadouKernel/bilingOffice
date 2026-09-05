using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Assistant;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Infrastructure.Assistant;

/// <summary>
/// Adaptateur <b>simulé</b> de l'assistant virtuel (§ 1.15, IA-01 à IA-06).
///
/// POURQUOI SIMULÉ. Le fournisseur d'assistant n'est pas arbitré (PC-18). Conformément à l'option
/// retenue par le commanditaire — câbler l'architecture, servir un adaptateur simulé — cette
/// implémentation rend les règles RG-IA-01 et RG-IA-02 vérifiables de bout en bout sans engager
/// aucun tiers. Y substituer un fournisseur réel est un changement d'enregistrement dans la
/// composition : ni le domaine, ni les cas d'usage, ni les écrans n'en dépendent.
///
/// CE QU'IL FAIT, ET RIEN DE PLUS. Il répond en français à un petit nombre de sujets de premier
/// niveau tirés du CDC — suivi d'un dossier, pièces à fournir, délais, simulation tarifaire,
/// annulation — et oriente vers l'écran concerné. Sur tout le reste, il ne devine pas : il
/// recommande l'escalade (IA-03). Un assistant qui invente une réponse est plus nuisible qu'un
/// assistant qui passe la main.
///
/// IL N'APPREND RIEN ET NE SORT PAS DE LA MACHINE : aucune donnée client n'est transmise à un
/// tiers, ce qui est la seule posture tenable tant que l'hébergement de l'IA n'est pas arbitré
/// (NFR-09, SEC).
/// </summary>
public sealed class AssistantSimule(ILogger<AssistantSimule> journal) : IAssistantConversationnel
{
    private const string Signature =
        "\n\n(Réponse produite par l'assistant de démonstration de la plateforme. "
        + "Le fournisseur d'assistant conversationnel n'est pas encore arbitré.)";

    /// <summary>
    /// Sujets de premier niveau, avec les mots qui les déclenchent. L'ordre compte : le premier
    /// sujet reconnu l'emporte.
    /// </summary>
    private static readonly (string[] Mots, string Reponse)[] Sujets =
    [
        (["suivi", "suivre", "où en est", "ou en est", "statut", "avancement"],
         "Vous pouvez suivre l'avancement de chacune de vos demandes depuis « Mes demandes ». "
         + "Chaque ticket y affiche son statut, l'étape en cours et l'historique complet des échanges."),

        (["pièce", "piece", "document", "justificatif", "joindre"],
         "Les pièces attendues dépendent du module et de l'étape en cours. Ouvrez le ticket concerné : "
         + "l'onglet « Documents » liste précisément ce qui reste à fournir et vous permet de le déposer."),

        (["délai", "delai", "combien de temps", "sla", "retard"],
         "Le délai de traitement dépend du type de demande et de l'étape. L'écran de suivi du ticket "
         + "affiche le temps écoulé et l'échéance visée pour l'étape en cours."),

        (["simulation", "proforma", "tarif", "coût", "cout", "prix", "devis"],
         "La simulation de proforma est accessible depuis « Simulation de proforma ». Renseignez le navire, "
         + "les dates et les caractéristiques de chaque conteneur : le montant est calculé poste par poste. "
         + "Le résultat est indicatif et n'engage pas CIT."),

        (["annuler", "annulation", "renoncer"],
         "Tant qu'aucun traitement engageant n'a été produit, vous pouvez annuler votre demande depuis "
         + "la fiche du ticket. Au-delà, l'annulation directe se ferme : vous pouvez alors soumettre une "
         + "demande d'annulation, qui sera examinée par un agent."),

        (["réclamation", "reclamation", "plainte", "litige"],
         "Une réclamation se dépose depuis « Nouvelle réclamation ». Choisissez la catégorie qui correspond "
         + "à votre situation : elle détermine le service qui l'instruira."),

        (["enrôlement", "enrolement", "badge", "chauffeur", "transporteur"],
         "L'enrôlement d'un chauffeur se fait à distance depuis « Nouvel enrôlement ». Une présentation "
         + "physique au Gate reste nécessaire pour la photo et la remise du badge."),

        (["bonjour", "bonsoir", "salut", "aide", "aidez"],
         "Bonjour. Je peux vous renseigner sur le suivi de vos demandes, les pièces à fournir, les délais, "
         + "la simulation de proforma, l'annulation d'une demande, les réclamations et l'enrôlement des "
         + "chauffeurs. Que souhaitez-vous savoir ?"),
    ];

    public Task<ReponseAssistant> RepondreAsync(
        IReadOnlyList<MessageAssistant> historique, string question, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(historique);

        var normalisee = (question ?? string.Empty).ToLowerInvariant();

        foreach (var (mots, reponse) in Sujets)
        {
            if (mots.Any(m => normalisee.Contains(m, StringComparison.Ordinal)))
            {
                return Task.FromResult(new ReponseAssistant(reponse + Signature, EscaladeRecommandee: false));
            }
        }

        // IA-03 — hors des sujets de premier niveau, l'assistant passe la main plutôt que de deviner.
        journal.LogInformation(
            "Assistant simulé : sujet non couvert, escalade recommandée après {Tours} tour(s).",
            historique.Count / 2);

        return Task.FromResult(new ReponseAssistant(
            "Je ne sais pas répondre à cette question avec certitude. Un opérateur pourra vous aider : "
            + "vous pouvez transmettre cet échange, il lui sera remis en entier." + Signature,
            EscaladeRecommandee: true,
            MotifEscalade: "Question hors des sujets traités par l'assistant."));
    }
}
