using System.Globalization;
using System.Text.RegularExpressions;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Architecture;

/// <summary>
/// GARDE WCAG 2.2 — 1.4.11 « Contraste non textuel », appliqué aux champs de saisie.
///
/// Le fond d'un champ (<c>--surface</c>) est identique au fond de la carte qui le porte :
/// la BORDURE est donc la seule indication visuelle de la zone de saisie, et elle tombait
/// sous le seuil 3:1 du critère. Les filets décoratifs (<c>--line</c> sur les cartes,
/// séparateurs, tableaux) ne sont PAS concernés — d'où un jeton dédié, <c>--field-border</c>.
///
/// Cette garde couvre les deux moitiés de la régression réellement observée :
///   1. VALEUR — le jeton a bien un contraste ≥ 3:1 dans les deux thèmes, contre chacun
///      des fonds sur lesquels un champ peut reposer.
///   2. CÂBLAGE — le jeton est bien LU par les règles de champ. Un <c>--field-border</c>
///      défini mais jamais référencé (l'état exact du dépôt avant cette remédiation) laisse
///      les champs sur <c>--line</c> et la non-conformité intacte : la garde 1. passerait
///      pourtant sans rien voir.
///
/// Les deux moitiés doivent tenir ensemble, sinon la remédiation est cosmétique.
/// </summary>
public sealed class ContrasteChampsWcagTests
{
    /// <summary>Seuil WCAG 1.4.11 pour les composants d'interface.</summary>
    private const double SeuilNonTextuel = 3.0d;

    /// <summary>Jetons de frontière de champ, avec l'état d'interface qu'ils habillent.</summary>
    public static TheoryData<string, string> JetonsDeFrontiere() => new()
    {
        { "--field-border", "repos / lecture seule / désactivé / succès" },
        { "--field-border-hover", "survol" },
        { "--field-border-focus", "focus" },
        { "--stop-600", "erreur (.invalid)" },
    };

    /// <summary>
    /// Fonds sur lesquels un champ peut reposer. <c>--surface</c> est le fond nominal,
    /// <c>--surface-2</c> celui des panneaux et en-têtes de carte (.bg-light, .card-header),
    /// <c>--surface-sunken</c> celui de l'état désactivé.
    /// </summary>
    private static readonly string[] FondsDeChamp = ["--surface", "--surface-2", "--surface-sunken"];

    [Theory]
    [MemberData(nameof(JetonsDeFrontiere))]
    public void La_frontiere_des_champs_atteint_3_1_dans_les_deux_themes(string jeton, string etat)
    {
        var css = LireFeuilleDeStyle();
        var echecs = new List<string>();

        foreach (var sombre in new[] { false, true })
        {
            var nomTheme = sombre ? "sombre" : "clair";
            var jetons = JetonsDuTheme(css, sombre);

            Assert.True(jetons.ContainsKey(jeton), $"Jeton {jeton} absent du thème {nomTheme}.");

            var bordure = Resoudre(jetons, jeton);

            foreach (var fond in FondsDeChamp)
            {
                var couleurFond = Resoudre(jetons, fond);
                var ratio = Contraste(bordure, couleurFond);

                if (ratio < SeuilNonTextuel)
                {
                    echecs.Add(
                        $"  {nomTheme} · {etat} · {jeton} ({bordure}) sur {fond} ({couleurFond}) "
                        + $"= {ratio.ToString("0.00", CultureInfo.InvariantCulture)}:1 (< 3:1)");
                }
            }
        }

        Assert.True(
            echecs.Count == 0,
            $"WCAG 1.4.11 : la frontière visuelle des champs descend sous 3:1.{Environment.NewLine}"
            + string.Join(Environment.NewLine, echecs));
    }

    [Fact]
    public void Les_regles_de_champ_lisent_le_jeton_dedie_et_non_le_filet_decoratif()
    {
        var css = LireFeuilleDeStyle();

        // Bordure au repos : input, select et textarea (textarea.form-control hérite de .form-control).
        var repos = Bloc(css, ".form-control,\n.form-select {");

        Assert.Contains("border: 1px solid var(--field-border)", repos);

        Assert.True(
            !repos.Contains("var(--line)"),
            "La règle .form-control/.form-select ne doit pas retomber sur --line : ce filet est "
            + "décoratif et mesuré ~1,3:1 contre le fond des champs.");

        // Survol et focus passent aussi par des jetons dédiés, jamais par une valeur littérale.
        Assert.Matches(
            new Regex(@"\.form-control:hover[^{}]*\{[^{}]*var\(--field-border-hover\)", RegexOptions.Singleline),
            css);

        Assert.Matches(
            new Regex(@"\.form-control:focus,\s*\.form-select:focus\s*\{[^{}]*var\(--field-border-focus\)", RegexOptions.Singleline),
            css);

        // Cases à cocher et boutons radio : leur bordure EST le composant, pas un ornement.
        Assert.Matches(
            new Regex(@"\.form-check-input\s*\{[^{}]*border-color:\s*var\(--field-border\)", RegexOptions.Singleline),
            css);
    }

    [Fact]
    public void Le_jeton_de_champ_reste_distinct_du_filet_decoratif()
    {
        var css = LireFeuilleDeStyle();

        foreach (var sombre in new[] { false, true })
        {
            var jetons = JetonsDuTheme(css, sombre);
            var nom = sombre ? "sombre" : "clair";

            Assert.True(
                !Resoudre(jetons, "--field-border").Equals(Resoudre(jetons, "--line"), StringComparison.OrdinalIgnoreCase),
                $"Thème {nom} : --field-border a été réaligné sur --line. Le jeton existe précisément "
                + "parce que la frontière d'un champ est soumise à 1.4.11 alors qu'un filet de carte "
                + "en est exempté ; les fusionner rouvre la non-conformité.");
        }
    }

    // ---------------------------------------------------------------------
    // Lecture de la feuille de style et résolution des jetons
    // ---------------------------------------------------------------------

    private static string LireFeuilleDeStyle()
    {
        var racine = new DirectoryInfo(AppContext.BaseDirectory);
        while (racine is not null && !Directory.Exists(Path.Combine(racine.FullName, "src")))
        {
            racine = racine.Parent;
        }

        Assert.True(racine is not null, "Racine du dépôt introuvable depuis " + AppContext.BaseDirectory);

        var chemin = Path.Combine(racine!.FullName, "src", "CIT.Plateforme.Web", "wwwroot", "app.css");
        Assert.True(File.Exists(chemin), "Feuille de style introuvable : " + chemin);

        return File.ReadAllText(chemin);
    }

    /// <summary>
    /// Reconstitue les jetons effectifs d'un thème. Le thème sombre part des jetons clairs
    /// (les blocs <c>:root[data-theme="dark"]</c> ne redéfinissent que les rôles qui changent)
    /// puis applique ses propres surcharges — c'est exactement la cascade du navigateur.
    /// </summary>
    private static Dictionary<string, string> JetonsDuTheme(string css, bool sombre)
    {
        var jetons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var bloc in Blocs(css, ":root {"))
        {
            Fusionner(jetons, bloc);
        }

        if (sombre)
        {
            foreach (var bloc in Blocs(css, ":root[data-theme=\"dark\"] {"))
            {
                Fusionner(jetons, bloc);
            }
        }

        return jetons;
    }

    private static readonly Regex Declaration =
        new(@"(?<nom>--[a-z0-9-]+)\s*:\s*(?<valeur>[^;}]+)\s*;", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static void Fusionner(Dictionary<string, string> jetons, string bloc)
    {
        foreach (Match declaration in Declaration.Matches(SansCommentaires(bloc)))
        {
            jetons[declaration.Groups["nom"].Value] = declaration.Groups["valeur"].Value.Trim();
        }
    }

    private static string SansCommentaires(string css) =>
        Regex.Replace(css, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);

    private static readonly Regex Reference =
        new(@"^var\(\s*(?<nom>--[a-z0-9-]+)\s*\)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>Déréférence les chaînes <c>var(--a) → var(--b) → #hex</c>.</summary>
    private static string Resoudre(Dictionary<string, string> jetons, string nom)
    {
        var valeur = jetons.TryGetValue(nom, out var trouvee) ? trouvee : nom;

        for (var saut = 0; saut < 10; saut++)
        {
            var reference = Reference.Match(valeur);
            if (!reference.Success)
            {
                return valeur;
            }

            var cible = reference.Groups["nom"].Value;
            Assert.True(jetons.ContainsKey(cible), $"Jeton {cible} référencé par {nom} mais jamais défini.");
            valeur = jetons[cible];
        }

        throw new InvalidOperationException($"Cycle de références de jetons à partir de {nom}.");
    }

    /// <summary>Contenu de chaque bloc ouvert par <paramref name="selecteur"/>, accolades appariées.</summary>
    private static IEnumerable<string> Blocs(string css, string selecteur)
    {
        var normalise = css.Replace("\r\n", "\n");
        var depuis = 0;

        while (true)
        {
            var debut = normalise.IndexOf(selecteur, depuis, StringComparison.Ordinal);
            if (debut < 0)
            {
                yield break;
            }

            var ouvrante = normalise.IndexOf('{', debut);
            var profondeur = 0;
            var i = ouvrante;

            for (; i < normalise.Length; i++)
            {
                if (normalise[i] == '{')
                {
                    profondeur++;
                }
                else if (normalise[i] == '}' && --profondeur == 0)
                {
                    break;
                }
            }

            yield return normalise[(ouvrante + 1)..i];
            depuis = i;
        }
    }

    private static string Bloc(string css, string selecteur)
    {
        var bloc = Blocs(css, selecteur.Replace("\r\n", "\n")).FirstOrDefault();
        Assert.True(bloc is not null, $"Bloc introuvable pour le sélecteur : {selecteur}");
        return bloc!;
    }

    // ---------------------------------------------------------------------
    // Contraste WCAG 2.x
    // ---------------------------------------------------------------------

    private static double Contraste(string hexA, string hexB)
    {
        var a = LuminanceRelative(hexA);
        var b = LuminanceRelative(hexB);
        var (haut, bas) = a > b ? (a, b) : (b, a);
        return (haut + 0.05d) / (bas + 0.05d);
    }

    private static double LuminanceRelative(string hex)
    {
        var brut = hex.Trim().TrimStart('#');

        if (brut.Length == 3)
        {
            brut = string.Concat(brut.Select(c => new string(c, 2)));
        }

        Assert.True(
            brut.Length == 6 && brut.All(Uri.IsHexDigit),
            $"Couleur non hexadécimale, contraste non mesurable : « {hex} ». "
            + "Une frontière de champ doit rester une couleur opaque et mesurable.");

        static double Canal(int octet)
        {
            var c = octet / 255d;
            return c <= 0.04045d ? c / 12.92d : Math.Pow((c + 0.055d) / 1.055d, 2.4d);
        }

        var r = Canal(Convert.ToInt32(brut[..2], 16));
        var g = Canal(Convert.ToInt32(brut.Substring(2, 2), 16));
        var b = Canal(Convert.ToInt32(brut.Substring(4, 2), 16));

        return (0.2126d * r) + (0.7152d * g) + (0.0722d * b);
    }
}
