using System.Globalization;
using System.Text.RegularExpressions;

namespace CIT.Plateforme.Web.Resources;

/// <summary>
/// Messages métier (exceptions et contrôles du Domain / de l'Application) affichés dans la langue de
/// l'utilisateur. Ces couches écrivent en français ; la traduction se fait ici, au moment de
/// l'affichage, à partir du message français : texte exact, ou modèle {0}, {1}… pour les valeurs
/// interpolées (numéro de ticket, file, statut…). Un message inconnu reste tel quel.
/// </summary>
public static partial class TraductionMessagesMetier
{
    // ArgumentException ajoute « (Parameter 'x') » au message : sans intérêt pour l'utilisateur.
    private static readonly Regex SuffixeParametre = new(@"\s*\((?:Parameter|Paramètre) '[^']*'\)$", RegexOptions.Compiled);

    // Les modèles les plus longs d'abord : « Le ticket {0} est déjà {1} et ne peut plus être annulé. »
    // doit l'emporter sur « Le ticket {0} est déjà {1}. ».
    // Paresseux : le dictionnaire est déclaré dans l'autre fichier de la classe partielle, et l'ordre
    // d'initialisation des champs statiques entre fichiers n'est pas garanti.
    private static readonly Lazy<(Regex Motif, string Anglais)[]> Modeles = new(() => [.. Anglais
        .Where(p => p.Key.Contains("{0}", StringComparison.Ordinal))
        .OrderByDescending(p => Regex.Replace(p.Key, @"\{\d+\}", string.Empty).Length)
        .Select(p => (new Regex("^" + Regex.Replace(Regex.Escape(p.Key), @"\\\{(\d+)}", "(?<v$1>.+?)") + "$", RegexOptions.Compiled | RegexOptions.Singleline), p.Value))]);

    // Message composé : préfixe traduit + liste de phrases (ex. grille tarifaire non constituée).
    private static readonly Lazy<string[]> Prefixes = new(() => [.. Anglais.Keys.Where(k => k.EndsWith(": ", StringComparison.Ordinal))]);

    /// <summary>Message dans la langue d'interface courante (anglais si la culture est « en »).</summary>
    public static string VersCultureCourante(string message) =>
        string.IsNullOrEmpty(message) || !CIT.Plateforme.Application.Abstractions.LangueCourante.EnAnglais
            ? message
            : VersAnglais(message);

    /// <summary>Traduction anglaise ; le message d'origine si aucune traduction ne correspond.</summary>
    public static string VersAnglais(string message)
    {
        var texte = SuffixeParametre.Replace(message, string.Empty);
        if (Anglais.TryGetValue(texte, out var exact))
        {
            return exact;
        }
        foreach (var (motif, anglais) in Modeles.Value)
        {
            var m = motif.Match(texte);
            if (m.Success)
            {
                var valeurs = Enumerable.Range(0, 10).TakeWhile(i => m.Groups[$"v{i}"].Success).Select(i => (object)m.Groups[$"v{i}"].Value).ToArray();
                return string.Format(CultureInfo.InvariantCulture, anglais, valeurs);
            }
        }
        foreach (var prefixe in Prefixes.Value)
        {
            if (texte.StartsWith(prefixe, StringComparison.Ordinal))
            {
                var phrases = Regex.Split(texte[prefixe.Length..], @"(?<=\.)\s+").Select(VersAnglais);
                return Anglais[prefixe] + string.Join(" ", phrases);
            }
        }
        return message;
    }

    /// <summary>Messages français connus (contrôle de couverture par les tests).</summary>
    public static IReadOnlyCollection<string> MessagesConnus => Anglais.Keys;
}
