using Xunit;

namespace CIT.Plateforme.Web.Tests.Architecture;

/// <summary>
/// AM-03 — les actions destructives / irréversibles doivent passer par une confirmation explicite.
///
/// Ces gardes vérifient le CÂBLAGE au niveau source : le bouton n'appelle plus directement la
/// commande destructive (il appelle une méthode <c>DemanderConfirmation…</c>), et la page monte bien
/// une <c>&lt;ModaleConfirmation&gt;</c>. La modale elle-même (rôle dialog, focus initial, Échap,
/// retour du focus) est couverte par ModaleConfirmationVisibiliteTests / …FocusTests.
///
/// Revenir à un appel direct (mutation) fait tomber la garde de la page concernée.
///
/// Ne couvre QUE les actions relevant de la décision (destructives/irréversibles). Les actions
/// réversibles (réactivation, retrait de rôle admin re-attribuable, retrait de compétence, édition
/// de lignes de formulaire non soumises) restent volontairement directes — voir docs AM-03.
/// </summary>
public sealed class Am03ConfirmationCablageTests
{
    [Theory]
    // (page, méthode destructive qui NE doit PAS être appelée directement depuis un bouton)
    [InlineData("Pages/Administration/MatriceTransitions.razor", "SupprimerAsync")]
    [InlineData("Pages/Administration/ParametresSla.razor", "SupprimerAsync")]
    [InlineData("Pages/Societe/Collaborateurs.razor", "RevoquerAsync")]
    [InlineData("Pages/Societe/Collaborateurs.razor", "DesactiverAsync")]
    public void L_action_destructive_passe_par_une_confirmation(string cheminRelatif, string methode)
    {
        var source = File.ReadAllText(CheminComposant(cheminRelatif));

        Assert.Contains("<ModaleConfirmation", source);
        Assert.DoesNotContain($"@onclick=\"() => {methode}(", source);
        Assert.Contains("DemanderConfirmation", source);
    }

    private static string CheminComposant(string cheminRelatif)
    {
        var racine = new DirectoryInfo(AppContext.BaseDirectory);
        while (racine is not null && !Directory.Exists(Path.Combine(racine.FullName, "src")))
        {
            racine = racine.Parent;
        }

        Assert.True(racine is not null, "Racine du dépôt introuvable depuis " + AppContext.BaseDirectory);
        return Path.Combine(
            racine!.FullName, "src", "CIT.Plateforme.Web", "Components",
            cheminRelatif.Replace('/', Path.DirectorySeparatorChar));
    }
}
