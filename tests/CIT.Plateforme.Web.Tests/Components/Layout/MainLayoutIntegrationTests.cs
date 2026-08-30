using System;
using System.Threading.Tasks;
using Bunit;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Web.Components.Layout;
using CIT.Plateforme.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Components.Layout;

public sealed class MainLayoutIntegrationTests : BunitContext
{
    [Fact]
    public void MainLayout_RenduComplet_HeaderEtSidebar()
    {
        var fakeContexte = new FakeContexteUtilisateurWeb();
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);

        var cut = Render<MainLayout>(parameters => parameters
            .Add(p => p.Body, (RenderFragment)(builder => builder.AddContent(0, "Contenu de test"))));

        Assert.Contains("Côte d'Ivoire Terminal", cut.Markup);
        Assert.Contains("Plateforme Digitale", cut.Markup);
        Assert.Contains("TRANS-AFRIQUE", cut.Markup);
        Assert.Contains("Contenu de test", cut.Markup);
        Assert.Contains("/client/dashboard", cut.Markup);
    }
}
