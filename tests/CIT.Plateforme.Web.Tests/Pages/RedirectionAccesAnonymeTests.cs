using System;
using System.Net;
using System.Threading.Tasks;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Pages;

public sealed class RedirectionAccesAnonymeTests(PlateformeWebApplicationFactory factory) : IClassFixture<PlateformeWebApplicationFactory>
{
    private readonly HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    [Theory]
    [InlineData("/client/dashboard")]
    [InlineData("/agent/dashboard")]
    [InlineData("/societe/dashboard")]
    public async Task AccesAnonyme_RouteProtegee_RedirigeVersConnexionAvecReturnUrl(string routeProtegee)
    {
        // Act
        var reponse = await client.GetAsync(routeProtegee);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
        var location = reponse.Headers.Location?.ToString();
        Assert.NotNull(location);
        Assert.Contains("/comptes/connexion", location);
        Assert.Contains($"ReturnUrl={Uri.EscapeDataString(routeProtegee)}", location);
    }

    [Fact]
    public async Task AccesAnonyme_HostUsurpe_RedirigeEnRelatifSansHoteExterne()
    {
        // ANTI-INJECTION D'EN-TÊTE HOST : un « Host » usurpé ne doit jamais produire une redirection
        // absolue vers ce domaine (redirection ouverte via BuildRedirectUri du cookie d'auth).
        var requete = new HttpRequestMessage(HttpMethod.Get, "/supervision");
        requete.Headers.Host = "evil.attacker.com";

        var reponse = await client.SendAsync(requete);

        Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
        var location = reponse.Headers.Location!.ToString();
        Assert.DoesNotContain("evil.attacker.com", location, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("/comptes/connexion", location);
    }
}
