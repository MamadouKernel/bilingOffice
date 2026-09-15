using Xunit;

namespace CIT.Plateforme.Web.Tests.Architecture;

/// <summary>
/// REMÉDIATION UI/UX — cohérence. Un bloc &lt;style&gt; placé dans un composant Razor n'est PAS
/// scopé : Blazor l'injecte globalement dans la page. ComptesAgents.razor en portait un, qui
/// redéfinissait en double les classes partagées <c>.cit-stat-card</c> / <c>.cit-stat-icon</c>
/// (avec un repli <c>#edf1f5</c> non adaptatif au thème sombre). Les styles vivent désormais dans
/// <c>app.css</c>, source unique.
///
/// Cette garde interdit la réapparition d'un bloc de styles local : tout style transverse doit
/// passer par <c>app.css</c> (ou un fichier <c>.razor.css</c> réellement scopé, hors de ce dépôt
/// à ce jour).
/// </summary>
public sealed class CoherenceStylesTests
{
    [Fact]
    public void Aucun_composant_ne_porte_de_bloc_style_local()
    {
        var fautifs = new List<string>();

        foreach (var fichier in FichiersRazor())
        {
            var source = File.ReadAllText(fichier);
            if (source.Contains("<style", StringComparison.OrdinalIgnoreCase))
            {
                fautifs.Add(Path.GetFileName(fichier));
            }
        }

        Assert.True(
            fautifs.Count == 0,
            "Bloc <style> local détecté : non scopé, il fuit globalement et peut redéfinir des "
            + "classes partagées. Déplacez ces règles dans wwwroot/app.css." + Environment.NewLine
            + string.Join(Environment.NewLine, fautifs));
    }

    private static IEnumerable<string> FichiersRazor()
    {
        var racine = new DirectoryInfo(AppContext.BaseDirectory);
        while (racine is not null && !Directory.Exists(Path.Combine(racine.FullName, "src")))
        {
            racine = racine.Parent;
        }

        Assert.True(racine is not null, "Racine du dépôt introuvable depuis " + AppContext.BaseDirectory);

        var composants = Path.Combine(racine!.FullName, "src", "CIT.Plateforme.Web", "Components");
        return Directory.EnumerateFiles(composants, "*.razor", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));
    }
}
