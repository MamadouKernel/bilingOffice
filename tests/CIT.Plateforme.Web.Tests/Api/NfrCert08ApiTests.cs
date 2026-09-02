using System.Net;
using System.Net.Http.Json;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Api;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Api;

[Collection("PlateformeWebCollection")]
public sealed class NfrCert08ApiTests : IClassFixture<PlateformeWebApplicationFactory>
{
    private readonly PlateformeWebApplicationFactory _factory;

    public NfrCert08ApiTests(PlateformeWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Api_GetTicket_NonAuthentifie_Retourne401()
    {
        // Arrange
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/api/v1/tickets/TCK-NONAUTH-001");

        // Assert - Doit refuser sans authentification (401 ou redirection login 302)
        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Api_OpenApiContract_EstAccessibleEtConforme()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("openapi", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/api/v1/tickets/{numero}", content);
        Assert.Contains("/api/v1/dossiers/{reference}", content);
        Assert.Contains("/api/v1/simulations/calculer", content);
    }
}
