using System;
using Bunit;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Web.Components.Pages;
using CIT.Plateforme.Web.Services;
using CIT.Plateforme.Web.Tests.Components.Layout;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Pages;

public sealed class AuthRedirectionTests : BunitContext
{
    [Fact]
    public void Visiteur_AffichePageAccueil_RedirigeVersConnexion()
    {
        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with { EstAuthentifie = false };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        
        var navMan = Services.GetRequiredService<NavigationManager>();

        var cut = Render<Home>();

        Assert.Equal("http://localhost/comptes/connexion", navMan.Uri);
    }

    [Theory]
    [InlineData(EspaceFonctionnel.Client, "/client/dashboard")]
    [InlineData(EspaceFonctionnel.AdministrateurSociete, "/societe/dashboard")]
    [InlineData(EspaceFonctionnel.Agent, "/agent/dashboard")]
    [InlineData(EspaceFonctionnel.Supervision, "/supervision")]
    public void Authentifie_RedirigeVersDashboardCorrespondant(EspaceFonctionnel espace, string urlAttendue)
    {
        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            EstAuthentifie = true,
            EspacesDisponibles = new[] { espace },
            EspaceActif = espace
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);

        var navMan = Services.GetRequiredService<NavigationManager>();

        var cut = Render<Home>();

        Assert.Equal($"http://localhost{urlAttendue}", navMan.Uri);
    }
}
