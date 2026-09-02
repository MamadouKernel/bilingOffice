using System.Net;
using CIT.Plateforme.Web.Tests.Support;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Supervision;

public class MetrologiePerformanceMiddlewareTests : IClassFixture<PlateformeWebApplicationFactory>
{
    private readonly PlateformeWebApplicationFactory _factory;

    public MetrologiePerformanceMiddlewareTests(PlateformeWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task NfrCert04_01_RequeteHttp_InjecteEnTeteServerTiming_EtMesureDuree()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/healthz");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Server-Timing"), "L'en-tête Server-Timing doit être injecté");
        var serverTiming = response.Headers.GetValues("Server-Timing").FirstOrDefault();
        Assert.NotNull(serverTiming);
        Assert.StartsWith("total;dur=", serverTiming);
    }
}
