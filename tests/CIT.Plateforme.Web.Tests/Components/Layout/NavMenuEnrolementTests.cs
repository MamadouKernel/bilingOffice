using Bunit;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Web.Components.Layout;
using CIT.Plateforme.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Components.Layout;

// FEATURE 008 (Enrôlement Transporteurs) — T061.
public sealed class NavMenuEnrolementTests : BunitContext
{
    [Fact]
    public void EspaceClient_AfficheLiensEnrolementDemandeur()
    {
        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with { EspaceActif = EspaceFonctionnel.Client };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        // REMÉDIATION RECETTE UAT (D-05/D-15) : le demandeur doit détenir « enrolement.soumettre »
        // pour voir le lien de soumission — c'est exactement ce que le menu ne vérifiait pas.
        NavigationDeTest.Enregistrer(
            Services, fakeContexte.Contexte.UtilisateurId,
            [CIT.Plateforme.Domain.Securite.Permission.EnrolementSoumettre]);

        var cut = Render<NavMenu>();

        Assert.Contains("/enrolement/soumettre", cut.Markup);
        Assert.Contains("/enrolement/mes-enrolements", cut.Markup);
        Assert.DoesNotContain("/enrolement/file-agent-gate", cut.Markup);
    }

    [Fact]
    public void EspaceAgent_AfficheLienFileAgentGateEtMasqueLiensDemandeur()
    {
        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with { EspaceActif = EspaceFonctionnel.Agent };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        NavigationDeTest.Enregistrer(
            Services, fakeContexte.Contexte.UtilisateurId,
            [CIT.Plateforme.Domain.Securite.Permission.EnrolementInstruire]);

        var cut = Render<NavMenu>();

        Assert.Contains("/enrolement/file-agent-gate", cut.Markup);
        Assert.DoesNotContain("/enrolement/soumettre", cut.Markup);
        Assert.DoesNotContain("/enrolement/mes-enrolements", cut.Markup);
    }

    [Fact]
    public void EspaceSupervision_NAfficheAucunLienEnrolement()
    {
        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with { EspaceActif = EspaceFonctionnel.Supervision };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        NavigationDeTest.Enregistrer(
            Services, fakeContexte.Contexte.UtilisateurId,
            NavigationDeTest.ToutesLesPermissions());

        var cut = Render<NavMenu>();

        Assert.DoesNotContain("/enrolement/", cut.Markup);
    }
}
