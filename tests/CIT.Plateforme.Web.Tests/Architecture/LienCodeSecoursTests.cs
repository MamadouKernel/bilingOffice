using System.Text.RegularExpressions;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Architecture;

/// <summary>
/// Le paramètre <c>?secours=</c> de /comptes/verifier-code est un <c>bool</c> lié depuis la query
/// string : Blazor n'accepte que <c>true</c>/<c>false</c>. Le lien « Utiliser un code de secours »
/// pointait vers <c>?secours=1</c> et faisait tomber la page en erreur 500 (« Cannot parse the
/// value '1' as type 'System.Boolean' ») — la voie de repli sans téléphone était donc inutilisable.
///
/// Toute valeur littérale autre que true/false (1, 0, oui, yes…) casse le binding de la même façon ;
/// les valeurs dynamiques (<c>secours=@x</c>, <c>secours={x}</c>) ne sont pas jugées ici.
/// </summary>
public sealed class LienCodeSecoursTests
{
    private static readonly Regex ValeurLitteraleInvalide = new(
        @"secours=(?!true\b|false\b)[^@{""'&\s]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    [Fact]
    public void Aucun_fichier_source_ne_lie_le_mode_secours_avec_une_valeur_non_booleenne()
    {
        var racine = new DirectoryInfo(AppContext.BaseDirectory);
        while (racine is not null && !Directory.Exists(Path.Combine(racine.FullName, "src")))
        {
            racine = racine.Parent;
        }

        Assert.True(racine is not null, "Racine du dépôt introuvable depuis " + AppContext.BaseDirectory);

        var web = Path.Combine(racine!.FullName, "src", "CIT.Plateforme.Web");
        var fautifs = Directory.EnumerateFiles(web, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(f => ValeurLitteraleInvalide.IsMatch(File.ReadAllText(f)))
            .ToList();

        Assert.True(fautifs.Count == 0, "Lien « secours=… » non booléen (attendu true/false) : " + string.Join(", ", fautifs));
    }

    [Theory]
    [InlineData("?secours=1", true)]
    [InlineData("?secours=0", true)]
    [InlineData("?secours=oui", true)]
    [InlineData("?secours=true", false)]
    [InlineData("?secours=false", false)]
    [InlineData("?secours=@valeur", false)]
    [InlineData("[SupplyParameterFromQuery(Name = \"secours\")]", false)]
    public void Le_motif_distingue_les_valeurs_valides_des_valeurs_invalides(string extrait, bool attenduInvalide)
    {
        Assert.Equal(attenduInvalide, ValeurLitteraleInvalide.IsMatch(extrait));
    }
}
