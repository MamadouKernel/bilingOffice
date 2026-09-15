using System.Text.RegularExpressions;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Architecture;

/// <summary>
/// [TEST-SPEC — UI] GARDES DE FINITION UI/UX (clôture P1/P2/P3).
///
/// Ces gardes verrouillent des corrections transversales dont le seul défaut serait de régresser
/// silencieusement, page par page : le titre principal en <c>&lt;h1&gt;</c> (D-16), l'absence de
/// code technique dans les messages d'autorisation (D-06), le lien d'évitement (UI-09), la
/// redirection de la connexion pour un utilisateur déjà authentifié (D-07), et l'auto-fermeture
/// du toast (D-02).
/// </summary>
public sealed class FinitionsUiTests
{
    private static DirectoryInfo Racine()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !Directory.Exists(Path.Combine(d.FullName, "src")))
        {
            d = d.Parent;
        }

        Assert.True(d is not null, "Racine du dépôt introuvable.");
        return d!;
    }

    private static string Web(params string[] segments) =>
        Path.Combine(new[] { Racine().FullName, "src", "CIT.Plateforme.Web" }.Concat(segments).ToArray());

    private static IEnumerable<string> PagesRoutables()
    {
        var dossier = Web("Components", "Pages");
        foreach (var f in Directory.EnumerateFiles(dossier, "*.razor", SearchOption.AllDirectories))
        {
            if (f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") ||
                f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            var contenu = File.ReadAllText(f);
            if (Regex.IsMatch(contenu, @"^@page\s", RegexOptions.Multiline))
            {
                yield return f;
            }
        }
    }

    // ---------------------------------------------------------------------------------------
    // D-16 — un titre principal <h1> par page routable
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void Chaque_page_routable_porte_un_titre_h1()
    {
        var sans = PagesRoutables()
            .Where(f => !File.ReadAllText(f).Contains("<h1"))
            .Select(Path.GetFileName)
            .ToList();

        Assert.True(sans.Count == 0,
            "Ces pages n'ont pas de <h1> — le plan de titres démarre en <h2>, ce qui prive les "
            + "lecteurs d'écran du repère de premier niveau (D-16) :" + Environment.NewLine
            + string.Join(Environment.NewLine, sans));
    }

    // ---------------------------------------------------------------------------------------
    // D-06 — aucun code technique de permission dans un message d'écran
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void Aucune_page_n_affiche_un_code_de_permission_dans_un_message()
    {
        // Un code de permission ressemble à « administration.sla » ou « billing_export.facturation.editer ».
        var motifCode = new Regex(@"\([a-z]+(?:_[a-z]+)*\.[a-z_.]+\)");
        var fautifs = new List<string>();

        foreach (var f in PagesRoutables())
        {
            foreach (var ligne in File.ReadLines(f))
            {
                if (!ligne.Contains("permission", StringComparison.OrdinalIgnoreCase) &&
                    !ligne.Contains("habilitation", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (motifCode.IsMatch(ligne))
                {
                    fautifs.Add($"{Path.GetFileName(f)} : {ligne.Trim()}");
                }
            }
        }

        Assert.True(fautifs.Count == 0,
            "Un message d'autorisation cite un code technique de permission — l'utilisateur doit "
            + "voir un libellé fonctionnel (D-06) :" + Environment.NewLine
            + string.Join(Environment.NewLine, fautifs));
    }

    // ---------------------------------------------------------------------------------------
    // UI-09 — lien d'évitement, ancré au contenu principal
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void Le_layout_offre_un_lien_d_evitement_vers_le_contenu()
    {
        var layout = File.ReadAllText(Web("Components", "Layout", "MainLayout.razor"));

        Assert.Contains("cit-skip-link", layout);
        Assert.Contains("#cit-contenu-principal", layout);
        Assert.Contains("id=\"cit-contenu-principal\"", layout);
    }

    // ---------------------------------------------------------------------------------------
    // D-07 — la connexion renvoie un utilisateur déjà authentifié
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void La_page_de_connexion_redirige_un_utilisateur_deja_authentifie()
    {
        var connexion = File.ReadAllText(Web("Components", "Pages", "Comptes", "Connexion.razor"));

        Assert.Contains("IsAuthenticated == true", connexion);
        Assert.Contains("Navigation.NavigateTo(\"/\")", connexion);
    }

    // ---------------------------------------------------------------------------------------
    // D-02 — le toast se ferme tout seul
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void Le_toast_se_ferme_automatiquement()
    {
        var toast = File.ReadAllText(Web("Components", "Shared", "ToastNotification.razor"));

        Assert.Contains("DureeMs", toast);
        Assert.Contains("IDisposable", toast);
        Assert.Contains("Task.Delay", toast);
    }
}
