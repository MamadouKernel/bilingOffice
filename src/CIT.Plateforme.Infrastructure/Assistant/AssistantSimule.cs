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
    /// <summary>IA-04 — chaque réponse cite le sujet de la base de connaissances dont elle provient.</summary>
    private static string Source(bool en, SujetAssistant sujet)
    {
        var nom = sujet.MotsClesListe().FirstOrDefault() ?? "?";
        return en
            ? $"\n\nSource: YABA knowledge base, topic « {nom} »."
            : $"\n\nSource : base de connaissances YABA, sujet « {nom} ».";
    }

    public async Task<ReponseAssistant> RepondreAsync(
        IReadOnlyList<MessageAssistant> historique, string question, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(historique);

        var langue = CultureInfo.CurrentUICulture.Name;
        var en = Langue.EstAnglais(langue);
        var sujetsActifs = await sujets.ListerAsync(activesUniquement: true, ct).ConfigureAwait(false);

        var meilleur = ComprehensionQuestion.MeilleurSujet(question ?? string.Empty, sujetsActifs);
        if (meilleur is not null)
        {
            return new ReponseAssistant(meilleur.Texte(langue) + Source(en, meilleur), EscaladeRecommandee: false);
        }

        // IA-03 — hors des sujets de premier niveau, l'assistant passe la main plutôt que de deviner.
        journal.LogInformation(
            "Assistant : sujet non couvert, escalade recommandée après {Tours} tour(s).",
            historique.Count / 2);

        var incertitude = en
            ? "I can't answer this question with certainty. An operator can help you: "
              + "you can forward this conversation, it will be handed over in full."
            : "Je ne sais pas répondre à cette question avec certitude. Un opérateur pourra vous aider : "
              + "vous pouvez transmettre cet échange, il lui sera remis en entier.";

        return new ReponseAssistant(
            incertitude,
            EscaladeRecommandee: true,
            MotifEscalade: "Question hors des sujets traités par l'assistant.");
    }
}

/// <summary>
/// IA-01 — compréhension du français courant SANS IA externe (décision du 02/10/2026) : la
/// question et les mots-clés sont normalisés (casse, accents, ponctuation), les mots sont comparés
/// entiers (« bl » ne correspond plus à « possible »), une faute de frappe est tolérée sur les mots
/// de 5 lettres et plus, et le sujet retenu est le PLUS pertinent, plus le premier venu.
/// ponytail: score lexical simple ; un modèle de langue local remplacerait cette classe seule.
/// </summary>
public static class ComprehensionQuestion
{
    private const int LongueurMinFauteTolere = 5;

    public static SujetAssistant? MeilleurSujet(string question, IEnumerable<SujetAssistant> sujets)
    {
        var mots = Mots(question);
        var texte = " " + string.Join(' ', mots) + " ";

        return sujets
            .Select(s => (Sujet: s, Score: s.MotsClesListe().Sum(m => Score(m, mots, texte))))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Sujet.Ordre)
            .Select(x => x.Sujet)
            .FirstOrDefault();
    }

    private static int Score(string motCle, IReadOnlyList<string> mots, string texte)
    {
        var cle = Mots(motCle);
        if (cle.Count == 0) return 0;
        if (cle.Count > 1) return texte.Contains(" " + string.Join(' ', cle) + " ", StringComparison.Ordinal) ? 3 : 0;

        var c = cle[0];
        if (mots.Contains(c)) return 2;
        return c.Length >= LongueurMinFauteTolere && mots.Any(m => Math.Abs(m.Length - c.Length) <= 1 && UneErreurAuPlus(m, c)) ? 1 : 0;
    }

    internal static IReadOnlyList<string> Mots(string texte)
    {
        var sansAccents = new string(texte.ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD)
            .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : ' ')
            .ToArray());
        return sansAccents.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>Distance d'édition ≤ 1 (substitution, insertion ou suppression d'une lettre).</summary>
    private static bool UneErreurAuPlus(string a, string b)
    {
        if (a.Length > b.Length) (a, b) = (b, a);
        int i = 0, j = 0, ecarts = 0;
        while (i < a.Length && j < b.Length)
        {
            if (a[i] == b[j]) { i++; j++; continue; }
            if (++ecarts > 1) return false;
            if (a.Length == b.Length) i++;
            j++;
        }
        return ecarts + (b.Length - j) + (a.Length - i) <= 1;
    }
}
