using System.Text.RegularExpressions;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Architecture;

/// <summary>
/// [TEST-SPEC — UX] Un composant partagé doit être utilisé quelque part.
///
/// DÉFAUT CORRIGÉ (audit UI/UX final, <c>docs/audit-final-ui-ux.md</c>) :
/// <c>ModaleConfirmation.razor</c> et <c>ToastNotification.razor</c> existaient depuis l'origine
/// avec <b>zéro référence</b>. Ce n'étaient pas des composants superflus — le premier était
/// exactement ce qui manquait aux actions destructives, le second au retour d'action. Ils avaient
/// été écrits pour des besoins qu'on constatait ensuite non couverts.
///
/// C'était la cinquième fois que ce projet produisait cette figure, après <c>MarquerEtatEngageant</c>
/// sans appelant, la garde de fiche sans branche agent, <c>AutorisationChampService</c> sans
/// consommateur et la chaîne de rétention sans déclencheur. La différence : aucune garde ne pouvait
/// la signaler. Celle-ci comble ce trou.
///
/// PÉRIMÈTRE VOLONTAIREMENT ÉTROIT. Le test ne vérifie QUE les composants de
/// <c>Components/Shared</c> — ceux dont l'existence même affirme qu'ils sont réutilisables. Il
/// n'exige rien des pages routables (elles sont des racines, atteintes par leur route), ni des
/// layouts (atteints par le moteur de rendu), ni des composants de page. Imposer un appelant à tout
/// composant Razor serait faux.
/// </summary>
public sealed class ComposantsPartagesAtteignablesTests
{
    /// <summary>
    /// DETTE ASSUMÉE. Un composant partagé sans usage y est inscrit NOMMÉMENT, avec sa raison.
    /// La liste ne doit que rétrécir : y ajouter une ligne, c'est décider sciemment de maintenir un
    /// composant que personne n'utilise.
    /// </summary>
    private static readonly Dictionary<string, string> SansUsageAssume = new(StringComparer.Ordinal)
    {
        // (vide — les deux composants qui motivaient ce test sont désormais câblés)
    };

    private static DirectoryInfo RacineDepot()
    {
        var dossier = new DirectoryInfo(AppContext.BaseDirectory);
        while (dossier is not null && !Directory.Exists(Path.Combine(dossier.FullName, "src")))
        {
            dossier = dossier.Parent;
        }

        Assert.True(dossier is not null, "Racine du dépôt introuvable depuis " + AppContext.BaseDirectory);
        return dossier!;
    }

    private static IEnumerable<string> FichiersRazor(string dossier) =>
        Directory.Exists(dossier)
            ? Directory.EnumerateFiles(dossier, "*.razor", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                         && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            : [];

    /// <summary>Commentaires Razor retirés : un composant cité pour dire qu'on ne l'utilise pas ne compte pas.</summary>
    private static string SansCommentaires(string source) =>
        Regex.Replace(source, @"@\*.*?\*@", " ", RegexOptions.Singleline);

    [Fact]
    public void Tout_composant_partage_est_utilise_par_au_moins_une_surface()
    {
        var racine = RacineDepot().FullName;
        var web = Path.Combine(racine, "src", "CIT.Plateforme.Web");
        var partages = Path.Combine(web, "Components", "Shared");

        var composants = FichiersRazor(partages)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(n => !string.IsNullOrEmpty(n))
            .Select(n => n!)
            .ToList();

        Assert.True(
            composants.Count > 5,
            $"Détection des composants partagés douteuse : {composants.Count} trouvés dans {partages}.");

        // Corpus d'usage : tout le projet Web SAUF la définition du composant lui-même.
        var usages = FichiersRazor(web)
            .ToDictionary(f => f, f => SansCommentaires(File.ReadAllText(f)));

        var orphelins = new List<string>();
        foreach (var composant in composants)
        {
            var motif = new Regex($@"<{Regex.Escape(composant)}[\s/>]");
            var definition = Path.Combine(partages, composant + ".razor");

            var utilise = usages.Any(u =>
                !string.Equals(u.Key, definition, StringComparison.OrdinalIgnoreCase)
                && motif.IsMatch(u.Value));

            if (!utilise && !SansUsageAssume.ContainsKey(composant))
            {
                orphelins.Add(composant);
            }
        }

        Assert.True(
            orphelins.Count == 0,
            "Ces composants partagés ne sont utilisés par aucune page ni aucun layout. Un composant "
            + "que personne n'emploie n'apporte rien à l'utilisateur, quelle que soit sa qualité — "
            + "et son absence se paie ailleurs, sous forme d'un besoin non couvert :"
            + Environment.NewLine
            + string.Join(Environment.NewLine, orphelins.OrderBy(o => o, StringComparer.Ordinal).Select(o => "  - " + o)));
    }

    /// <summary>
    /// Miroir : dès qu'une entrée de la dette est câblée, elle doit en sortir. Sans cela, la liste
    /// deviendrait un cimetière et masquerait le prochain composant orphelin.
    /// </summary>
    [Fact]
    public void La_dette_assumee_ne_contient_que_des_composants_encore_orphelins()
    {
        var racine = RacineDepot().FullName;
        var web = Path.Combine(racine, "src", "CIT.Plateforme.Web");
        var partages = Path.Combine(web, "Components", "Shared");

        var usages = FichiersRazor(web)
            .ToDictionary(f => f, f => SansCommentaires(File.ReadAllText(f)));

        var resorbes = SansUsageAssume.Keys
            .Where(composant =>
            {
                var motif = new Regex($@"<{Regex.Escape(composant)}[\s/>]");
                var definition = Path.Combine(partages, composant + ".razor");
                return usages.Any(u =>
                    !string.Equals(u.Key, definition, StringComparison.OrdinalIgnoreCase)
                    && motif.IsMatch(u.Value));
            })
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            resorbes.Count == 0,
            "Ces composants sont désormais utilisés : retirez-les de SansUsageAssume."
            + Environment.NewLine
            + string.Join(Environment.NewLine, resorbes.Select(c => $"  - {c} ({SansUsageAssume[c]})")));
    }
}
