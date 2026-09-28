namespace CIT.Plateforme.Domain.Assistant;

/// <summary>
/// IA-04 — sujet de premier niveau de l'assistant conversationnel simulé, administrable sans
/// développement (§16, §17.3). Remplace la table C# codée en dur qui existait précédemment dans
/// <c>AssistantSimule</c> : modifier une réponse ou ajouter un sujet ne demande plus de
/// recompilation, seulement une action d'un administrateur habilité.
/// </summary>
public sealed class SujetAssistant
{
    public Guid Id { get; private set; }

    /// <summary>Mots-clés déclencheurs, séparés par des virgules, comparés en minuscules
    /// (« contains »), même convention que l'ancienne table codée en dur.</summary>
    public string MotsCles { get; private set; } = string.Empty;

    /// <summary>Réponse en français (langue de référence). Toujours renseignée.</summary>
    public string Reponse { get; private set; } = string.Empty;

    /// <summary>Réponse en anglais (NFR-07). Facultative : si absente, l'anglais retombe sur le
    /// français, comme le reste des libellés bilingues du projet.</summary>
    public string? ReponseEn { get; private set; }

    /// <summary>Ordre d'évaluation : le premier sujet dont un mot-clé correspond l'emporte.</summary>
    public int Ordre { get; private set; }

    /// <summary>Désactiver plutôt que supprimer : cohérent avec les autres référentiels
    /// administrables du projet (motifs de transfert, statuts, priorités).</summary>
    public bool EstActif { get; private set; }

    private SujetAssistant() { }

    public static SujetAssistant Definir(string motsCles, string reponse, int ordre, string? reponseEn = null)
    {
        if (string.IsNullOrWhiteSpace(motsCles))
        {
            throw new ArgumentException("Au moins un mot-clé est requis.", nameof(motsCles));
        }

        if (string.IsNullOrWhiteSpace(reponse))
        {
            throw new ArgumentException("La réponse est obligatoire.", nameof(reponse));
        }

        return new SujetAssistant
        {
            Id = Guid.NewGuid(),
            MotsCles = motsCles.Trim(),
            Reponse = reponse.Trim(),
            ReponseEn = Nettoyer(reponseEn),
            Ordre = ordre,
            EstActif = true,
        };
    }

    public void Modifier(string motsCles, string reponse, int ordre, string? reponseEn = null)
    {
        if (string.IsNullOrWhiteSpace(motsCles))
        {
            throw new ArgumentException("Au moins un mot-clé est requis.", nameof(motsCles));
        }

        if (string.IsNullOrWhiteSpace(reponse))
        {
            throw new ArgumentException("La réponse est obligatoire.", nameof(reponse));
        }

        MotsCles = motsCles.Trim();
        Reponse = reponse.Trim();
        ReponseEn = Nettoyer(reponseEn);
        Ordre = ordre;
    }

    /// <summary>Réponse dans la langue active : anglais si demandé ET renseigné, sinon français.</summary>
    public string Texte(string langue) =>
        Langue.EstAnglais(langue) && !string.IsNullOrWhiteSpace(ReponseEn) ? ReponseEn! : Reponse;

    private static string? Nettoyer(string? valeur) =>
        string.IsNullOrWhiteSpace(valeur) ? null : valeur.Trim();

    public void Activer() => EstActif = true;

    public void Desactiver() => EstActif = false;

    /// <summary>Les mots-clés éclatés pour la mise en correspondance, jamais persistés sous cette
    /// forme — dérivés à la lecture pour rester un simple champ texte administrable.</summary>
    public IReadOnlyList<string> MotsClesListe() =>
        MotsCles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
