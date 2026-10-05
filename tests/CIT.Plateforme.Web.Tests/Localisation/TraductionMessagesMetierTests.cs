using System.Globalization;
using System.Text.RegularExpressions;
using CIT.Plateforme.Web.Resources;
using CIT.Plateforme.Web.Services;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Localisation;

/// <summary>
/// Messages métier (exceptions et contrôles du Domain / de l'Application) affichés en anglais quand
/// l'interface est en anglais, inchangés en français.
/// </summary>
public sealed class TraductionMessagesMetierTests
{
    [Fact]
    public void Un_message_exact_est_traduit()
    {
        Assert.Equal("The signatory's capacity is required.",
            TraductionMessagesMetier.VersAnglais("La qualité du signataire est obligatoire."));
    }

    [Fact]
    public void Un_message_avec_valeurs_reprend_les_valeurs_dans_la_phrase_anglaise()
    {
        Assert.Equal("User 42 is not allowed to view ticket TKT-0007 (CPT-04, SEC-03).",
            TraductionMessagesMetier.VersAnglais("L'utilisateur 42 n'est pas autorisé à consulter le ticket TKT-0007 (CPT-04, SEC-03)."));
    }

    [Fact]
    public void Le_modele_le_plus_precis_l_emporte()
    {
        Assert.Equal("Ticket TKT-1 is already closed and can no longer be cancelled.",
            TraductionMessagesMetier.VersAnglais("Le ticket TKT-1 est déjà closed et ne peut plus être annulé."));
    }

    [Fact]
    public void Le_suffixe_technique_d_ArgumentException_est_retire()
    {
        var message = new ArgumentException("Le libellé est obligatoire.", "libelle").Message;

        Assert.Equal("The label is required.", TraductionMessagesMetier.VersAnglais(message));
    }

    [Fact]
    public void Un_message_compose_traduit_le_prefixe_et_chaque_phrase()
    {
        Assert.Equal("The tariff grid could not be built: Unknown direction \"XX\". The label is required.",
            TraductionMessagesMetier.VersAnglais("La grille tarifaire n'a pas pu être constituée : Sens d'opération « XX » inconnu. Le libellé est obligatoire."));
    }

    [Fact]
    public void Un_message_inconnu_reste_tel_quel()
    {
        Assert.Equal("Message inédit.", TraductionMessagesMetier.VersAnglais("Message inédit."));
    }

    [Fact]
    public void En_francais_le_message_n_est_pas_touche_et_en_anglais_il_est_traduit()
    {
        var exception = new InvalidOperationException("Le ticket TKT-9 n'existe pas.");

        Assert.Equal("Le ticket TKT-9 n'existe pas.", AvecCulture("fr-FR", () => MessagesUtilisateur.Pour(exception)));
        Assert.Equal("Ticket TKT-9 does not exist.", AvecCulture("en-US", () => MessagesUtilisateur.Pour(exception)));
        Assert.Equal(MessagesUtilisateur.GeneriqueAnglais, AvecCulture("en-US", () => MessagesUtilisateur.Pour(new TimeoutException("x"))));
        Assert.Equal(MessagesUtilisateur.Generique, AvecCulture("fr-FR", () => MessagesUtilisateur.Pour(new TimeoutException("x"))));
    }

    /// <summary>
    /// Garde de couverture : tout message français levé par le Domain ou l'Application a sa traduction.
    /// Un nouveau message sans traduction fait tomber ce test (ajouter la paire dans
    /// TraductionMessagesMetier.Donnees.cs).
    /// </summary>
    [Fact]
    public void Chaque_message_metier_francais_a_une_traduction_anglaise()
    {
        var connus = TraductionMessagesMetier.MessagesConnus.ToHashSet(StringComparer.Ordinal);
        var manquants = MessagesDuCode().Where(m => EstFrancais(m) && !connus.Contains(m)).Distinct().ToList();

        Assert.True(manquants.Count == 0, "Messages sans traduction anglaise :\n" + string.Join("\n", manquants));
    }

    [Fact]
    public void Chaque_traduction_garde_les_memes_valeurs_que_le_message_francais()
    {
        foreach (var message in TraductionMessagesMetier.MessagesConnus)
        {
            var valeurs = Regex.Matches(message, @"\{\d+\}").Select(m => m.Value).Distinct().ToArray();
            var exemple = valeurs.Aggregate(message, (texte, v) => texte.Replace(v, $"V{v[1..^1]}", StringComparison.Ordinal));
            var anglais = TraductionMessagesMetier.VersAnglais(exemple);

            Assert.NotEqual(exemple, anglais);
            Assert.All(valeurs, v => Assert.Contains($"V{v[1..^1]}", anglais, StringComparison.Ordinal));
        }
    }

    private static readonly Regex Morceau = new(@"(\$?)@?""((?:[^""\\]|\\.)*)""", RegexOptions.Compiled);
    private static readonly Regex Trou = new(@"(?<!\{)\{(?!\{)[^{}]*\}", RegexOptions.Compiled);
    private const string Chaines = @"((?:\$?@?""(?:[^""\\]|\\.)*""\s*\+?\s*)+)";
    private static readonly Regex[] Sources =
    [
        new(@"throw new \w+Exception\(\s*" + Chaines, RegexOptions.Compiled),
        new(@"\b(?:alertes|aides|problemes|erreurs|avertissements)\.Add\(\s*" + Chaines, RegexOptions.Compiled),
    ];

    private static IEnumerable<string> MessagesDuCode()
    {
        var racine = RacineDepot();
        var separateur = Path.DirectorySeparatorChar;
        var fichiers = new[] { "CIT.Plateforme.Domain", "CIT.Plateforme.Application" }
            .SelectMany(p => Directory.EnumerateFiles(Path.Combine(racine, "src", p), "*.cs", SearchOption.AllDirectories))
            .Where(f => !f.Contains($"{separateur}obj{separateur}", StringComparison.Ordinal));
        foreach (var fichier in fichiers)
        {
            var source = File.ReadAllText(fichier);
            foreach (var motif in Sources)
            {
                foreach (Match m in motif.Matches(source))
                {
                    yield return Modele(m.Groups[1].Value);
                }
            }
        }
    }

    // Reconstitue le message tel que la traduction le connaît : interpolations remplacées par {0}, {1}…
    private static string Modele(string chaines)
    {
        var texte = string.Empty;
        var n = 0;
        foreach (Match morceau in Morceau.Matches(chaines))
        {
            var s = morceau.Groups[2].Value;
            texte += morceau.Groups[1].Value == "$"
                ? Trou.Replace(s, _ => $"{{{n++}}}").Replace("{{", "{").Replace("}}", "}")
                : s;
        }
        return texte.Replace("\\\"", "\"");
    }

    private static bool EstFrancais(string message) =>
        Regex.IsMatch(message, @"[éèàêçùôî]|\b(le|la|les|des|du|une|un|est|pas|ou|et|aucun)\b", RegexOptions.IgnoreCase);

    private static string AvecCulture(string culture, Func<string> action)
    {
        var precedente = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
        try
        {
            return action();
        }
        finally
        {
            CultureInfo.CurrentUICulture = precedente;
        }
    }

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
}
