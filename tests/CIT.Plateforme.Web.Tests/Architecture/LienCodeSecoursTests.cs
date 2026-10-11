using Xunit;

namespace CIT.Plateforme.Web.Tests.Architecture;

/// <summary>
/// Le paramètre <c>?secours=</c> de /comptes/verifier-code est un <c>bool</c> lié depuis la query
/// string : Blazor n'accepte que <c>true</c>/<c>false</c>. Le lien « Utiliser un code de secours »
/// pointait vers <c>?secours=1</c> et faisait tomber la page en erreur 500 (« Cannot parse the
/// value '1' as type 'System.Boolean' ») — la voie de repli sans téléphone était donc inutilisable.
/// </summary>
public sealed class LienCodeSecoursTests
{
    [Fact]
    public void Aucun_ecran_ne_lie_le_mode_secours_avec_une_valeur_numerique()
    {
        var racine = new DirectoryInfo(AppContext.BaseDirectory);
        while (racine is not null && !Directory.Exists(Path.Combine(racine.FullName, "src")))
        {
            racine = racine.Parent;
        }

        Assert.True(racine is not null, "Racine du dépôt introuvable depuis " + AppContext.BaseDirectory);

        var composants = Path.Combine(racine!.FullName, "src", "CIT.Plateforme.Web", "Components");
        var fautifs = Directory.EnumerateFiles(composants, "*.razor", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(f => File.ReadAllText(f).Contains("secours=1", StringComparison.Ordinal))
            .ToList();

        Assert.True(fautifs.Count == 0, "Lien « secours=1 » (doit être « secours=true ») : " + string.Join(", ", fautifs));
    }
}
