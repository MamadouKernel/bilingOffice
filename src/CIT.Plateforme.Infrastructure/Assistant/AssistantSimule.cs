using System.Globalization;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain;
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
/// IA-04 — LA BASE DE CONNAISSANCES EST ADMINISTRABLE. Les sujets, mots-clés et réponses ne sont
/// plus une table C# codée en dur : ils vivent dans <see cref="ISujetAssistantRepository"/>, gérée
/// depuis l'écran d'administration. Ajouter un sujet ou corriger une réponse ne demande plus de
/// recompilation.
///
/// SUR TOUT LE RESTE, IL NE DEVINE PAS : il recommande l'escalade (IA-03). Un assistant qui invente
/// une réponse est plus nuisible qu'un assistant qui passe la main.
///
/// IL N'APPREND RIEN ET NE SORT PAS DE LA MACHINE : aucune donnée client n'est transmise à un
/// tiers, ce qui est la seule posture tenable tant que l'hébergement de l'IA n'est pas arbitré
/// (NFR-09, SEC).
/// </summary>
public sealed class AssistantSimule(ISujetAssistantRepository sujets, ILogger<AssistantSimule> journal) : IAssistantConversationnel
{
    private static string Signature(bool en) => en
        ? "\n\n(Response produced by the platform's demonstration assistant. "
          + "The conversational assistant provider has not been selected yet.)"
        : "\n\n(Réponse produite par l'assistant de démonstration de la plateforme. "
          + "Le fournisseur d'assistant conversationnel n'est pas encore arbitré.)";

    public async Task<ReponseAssistant> RepondreAsync(
        IReadOnlyList<MessageAssistant> historique, string question, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(historique);

        var langue = CultureInfo.CurrentUICulture.Name;
        var en = Langue.EstAnglais(langue);
        var normalisee = (question ?? string.Empty).ToLowerInvariant();
        var sujetsActifs = await sujets.ListerAsync(activesUniquement: true, ct).ConfigureAwait(false);

        foreach (var sujet in sujetsActifs.OrderBy(s => s.Ordre))
        {
            if (sujet.MotsClesListe().Any(m => normalisee.Contains(m.ToLowerInvariant(), StringComparison.Ordinal)))
            {
                return new ReponseAssistant(sujet.Texte(langue) + Signature(en), EscaladeRecommandee: false);
            }
        }

        // IA-03 — hors des sujets de premier niveau, l'assistant passe la main plutôt que de deviner.
        journal.LogInformation(
            "Assistant simulé : sujet non couvert, escalade recommandée après {Tours} tour(s).",
            historique.Count / 2);

        var incertitude = en
            ? "I can't answer this question with certainty. An operator can help you: "
              + "you can forward this conversation, it will be handed over in full."
            : "Je ne sais pas répondre à cette question avec certitude. Un opérateur pourra vous aider : "
              + "vous pouvez transmettre cet échange, il lui sera remis en entier.";

        return new ReponseAssistant(
            incertitude + Signature(en),
            EscaladeRecommandee: true,
            MotifEscalade: "Question hors des sujets traités par l'assistant.");
    }
}
