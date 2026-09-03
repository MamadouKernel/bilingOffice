using System.Text.RegularExpressions;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Securite;

/// <summary>
/// [TEST-TECH] INC-03 — reproductibilité du dépôt.
///
/// INCIDENT : un commit a publié des pages Razor référençant <c>&lt;ChampMotDePasse&gt;</c> et
/// <c>js/afficher-mot-de-passe.js</c> alors que ni le composant ni le script n'étaient versionnés.
/// Un clone propre ne compilait pas — le défaut ne se voyait pas localement, puisque les fichiers
/// existaient dans l'arbre de travail.
///
/// Ces tests contrôlent que toute ressource référencée par le balisage existe réellement. Ils
/// s'exécutent sur les fichiers du dépôt, pas sur l'arbre de travail d'un poste donné.
/// </summary>
public sealed class RessourcesReferenceesVersionneesTests
{
    private static string RacineDepot()
    {
        var repertoire = new DirectoryInfo(AppContext.BaseDirectory);
        while (repertoire is not null && !Directory.Exists(Path.Combine(repertoire.FullName, "src")))
        {
            repertoire = repertoire.Parent;
        }

        Assert.NotNull(repertoire);
        return repertoire!.FullName;
    }

    private static string RacineWeb() => Path.Combine(RacineDepot(), "src", "CIT.Plateforme.Web");

    private static IEnumerable<string> FichiersRazor() =>
        Directory.EnumerateFiles(Path.Combine(RacineWeb(), "Components"), "*.razor", SearchOption.AllDirectories);

    /// <summary>
    /// Tout script ou feuille de style servi par l'application doit exister sous wwwroot.
    /// Les ressources externes sont interdites par ailleurs (voir AucuneRessourceExterneTests).
    /// </summary>
    [Fact]
    public void Tout_script_ou_style_local_reference_existe_dans_wwwroot()
    {
        var motif = new Regex(
            @"(?:src|href)\s*=\s*""(?!https?:|//|@|/_|data:)(?<chemin>[^""]+\.(?:js|css))""",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        var manquants = new List<string>();

        foreach (var fichier in FichiersRazor())
        {
            foreach (Match correspondance in motif.Matches(File.ReadAllText(fichier)))
            {
                var chemin = correspondance.Groups["chemin"].Value.TrimStart('~', '/');
                var attendu = Path.Combine(RacineWeb(), "wwwroot", chemin.Replace('/', Path.DirectorySeparatorChar));

                if (!File.Exists(attendu))
                {
                    manquants.Add($"{Path.GetFileName(fichier)} référence « {chemin} » — introuvable sous wwwroot");
                }
            }
        }

        Assert.True(manquants.Count == 0, string.Join(Environment.NewLine, manquants));
    }

    /// <summary>
    /// Tout composant Razor employé comme élément doit avoir un fichier correspondant. C'est
    /// exactement ce qui manquait : les pages utilisaient un composant absent du dépôt.
    /// </summary>
    [Fact]
    public void Tout_composant_maison_utilise_dans_le_balisage_possede_son_fichier()
    {
        var composantsExistants = Directory
            .EnumerateFiles(Path.Combine(RacineWeb(), "Components"), "*.razor", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(n => n is not null)
            .Select(n => n!)
            .ToHashSet(StringComparer.Ordinal);

        // Un élément dont le nom commence par une majuscule et n'est pas une balise HTML connue
        // est un composant Razor. La liste d'exclusion couvre les composants fournis par le
        // framework, qui n'ont pas de fichier dans ce dépôt.
        var fournisParLeFramework = new HashSet<string>(StringComparer.Ordinal)
        {
            "EditForm", "InputText", "InputTextArea", "InputNumber", "InputCheckbox", "InputDate",
            "InputSelect", "InputRadio", "InputRadioGroup", "InputFile", "ValidationMessage",
            "ValidationSummary", "DataAnnotationsValidator", "PageTitle", "HeadContent", "HeadOutlet",
            "Router", "Found", "NotFound", "RouteView", "FocusOnNavigate", "AuthorizeView",
            "CascadingAuthenticationState", "Authorized", "NotAuthorized", "Authorizing",
            "NavLink", "Virtualize", "ErrorBoundary", "SectionOutlet", "SectionContent",
            "ImportMap", "Assets", "ChildContent", "Attributes", "DynamicComponent",
            "AntiforgeryToken",
        };

        // Références pendantes tolérées : la liste est VIDE, et doit le rester. Elle a servi à
        // rendre visible <ResourcePreloader> (INC-06), une balise qui n'a jamais correspondu à
        // aucun composant et ne produisait rien dans la réponse ; elle a été retirée.
        var pendantesConnues = new HashSet<string>(StringComparer.Ordinal);

        // Le lookbehind écarte les arguments génériques C# — `List<Message>`,
        // `IStringLocalizer<SharedResources>` — qui ne sont pas du balisage. Une balise est
        // toujours précédée d'un espace, d'un début de ligne ou d'un autre chevron.
        var motif = new Regex(@"(?<![A-Za-z0-9_,])<(?<nom>[A-Z][A-Za-z0-9]*)[\s/>]", RegexOptions.Compiled);
        var manquants = new List<string>();
        var pendantesRencontrees = new HashSet<string>(StringComparer.Ordinal);

        foreach (var fichier in FichiersRazor())
        {
            foreach (Match correspondance in motif.Matches(File.ReadAllText(fichier)))
            {
                var nom = correspondance.Groups["nom"].Value;
                if (fournisParLeFramework.Contains(nom) || composantsExistants.Contains(nom))
                {
                    continue;
                }

                if (pendantesConnues.Contains(nom))
                {
                    pendantesRencontrees.Add(nom);
                    continue;
                }

                manquants.Add($"{Path.GetFileName(fichier)} utilise « <{nom}> » — aucun fichier {nom}.razor dans le dépôt");
            }
        }

        Assert.True(manquants.Count == 0, string.Join(Environment.NewLine, manquants.Distinct()));

        // Si une référence pendante connue disparaît, la liste ci-dessus doit être mise à jour :
        // une exception qui ne correspond plus à rien finit par masquer un vrai défaut.
        Assert.Equal(pendantesConnues, pendantesRencontrees);
    }

    /// <summary>
    /// La bascule de visibilité doit rester accessible : bouton non soumettant, état annoncé, et
    /// libellés fournis par la localisation plutôt qu'écrits en dur.
    /// </summary>
    [Fact]
    public void La_bascule_de_mot_de_passe_expose_un_bouton_accessible()
    {
        var composant = Path.Combine(RacineWeb(), "Components", "Shared", "ChampMotDePasse.razor");
        Assert.True(File.Exists(composant), "Le composant ChampMotDePasse.razor est absent du dépôt.");

        var contenu = File.ReadAllText(composant);

        // type="button" : sans cela, le bouton soumettrait le formulaire à chaque bascule.
        Assert.Contains("type=\"button\"", contenu, StringComparison.Ordinal);
        Assert.Contains("aria-pressed", contenu, StringComparison.Ordinal);
        Assert.Contains("aria-label", contenu, StringComparison.Ordinal);
        Assert.Contains("Auth.ShowPassword", contenu, StringComparison.Ordinal);
        Assert.Contains("Auth.HidePassword", contenu, StringComparison.Ordinal);

        // Aucun gestionnaire Blazor : le rendu est statique (Routes.razor ne déclare pas de
        // @rendermode), un @onclick ne se déclencherait jamais.
        Assert.DoesNotContain("@onclick", contenu, StringComparison.Ordinal);
    }

    /// <summary>Les libellés de la bascule doivent exister dans les deux langues (NFR-07).</summary>
    [Fact]
    public void Les_libelles_de_la_bascule_sont_traduits_en_francais_et_en_anglais()
    {
        var localiseur = File.ReadAllText(
            Path.Combine(RacineWeb(), "Resources", "SharedResourcesLocalizer.cs"));

        Assert.Equal(2, Regex.Matches(localiseur, @"""Auth\.ShowPassword""").Count);
        Assert.Equal(2, Regex.Matches(localiseur, @"""Auth\.HidePassword""").Count);
    }
}
