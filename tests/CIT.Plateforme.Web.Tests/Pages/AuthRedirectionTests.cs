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
    public void Visiteur_AffichePageAccueil_AfficheLandingEtPasDeRedirection()
    {
        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with { EstAuthentifie = false };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        
        var navMan = Services.GetRequiredService<NavigationManager>();

        var cut = Render<Home>();

        Assert.Equal("http://localhost/", navMan.Uri);
        Assert.Contains("Côte d'Ivoire Terminal", cut.Markup);
        Assert.Contains("Créer un compte Société", cut.Markup);
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

        // L'accueil est la PREMIÈRE ENTRÉE ACCESSIBLE du menu (même règle pour tous les rôles) :
        // la vraie construction de navigation est donc branchée, avec le droit de consulter la
        // supervision (sans effet sur les autres espaces, dont le tableau de bord ouvre le menu).
        NavigationDeTest.Enregistrer(Services, fakeContexte.Contexte.UtilisateurId, [Permission.SupervisionConsulter]);

        var navMan = Services.GetRequiredService<NavigationManager>();

        var cut = Render<Home>();

        Assert.Equal($"http://localhost{urlAttendue}", navMan.Uri);
    }
}
