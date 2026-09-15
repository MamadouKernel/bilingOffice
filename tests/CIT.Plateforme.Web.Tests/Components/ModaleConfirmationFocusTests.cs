using Bunit;
using CIT.Plateforme.Web.Components.Shared;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Components;

/// <summary>
/// REMÉDIATION UI/UX — accessibilité (WCAG 2.4.3 « Focus Order »).
///
/// La modale de confirmation plaçait bien le focus sur « Annuler » à l'ouverture, mais ne le
/// rendait jamais au déclencheur à la fermeture : l'utilisateur au clavier ou au lecteur d'écran
/// était renvoyé en tête de document. Le composant appelle désormais <c>citFocus.memoriser</c> à
/// l'ouverture (avant de déplacer le focus) et <c>citFocus.restaurer</c> à la fermeture.
///
/// Ces tests verrouillent les deux appels d'interop. Retirer l'un d'eux — donc la protection —
/// fait échouer le test correspondant.
/// </summary>
public sealed class ModaleConfirmationFocusTests : BunitContext
{
    public ModaleConfirmationFocusTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void A_l_ouverture_le_declencheur_est_memorise()
    {
        _ = Render<ModaleConfirmation>(p => p.Add(m => m.EstOuvert, true));

        JSInterop.VerifyInvoke("citFocus.memoriser");
    }

    [Fact]
    public void A_la_fermeture_le_focus_est_restaure()
    {
        var rendu = Render<ModaleConfirmation>(p => p.Add(m => m.EstOuvert, true));

        rendu.Render(p => p.Add(m => m.EstOuvert, false));

        JSInterop.VerifyInvoke("citFocus.restaurer");
    }
}
