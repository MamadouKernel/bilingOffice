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

        var cut = Render<NavMenu>();

        Assert.DoesNotContain("/enrolement/", cut.Markup);
    }
}
