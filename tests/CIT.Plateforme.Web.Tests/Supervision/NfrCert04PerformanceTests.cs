using System.Net;
using CIT.Plateforme.Web.Tests.Support;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Supervision;

public class NfrCert04PerformanceTests : IClassFixture<PlateformeWebApplicationFactory>
{
    private readonly PlateformeWebApplicationFactory _factory;

    public NfrCert04PerformanceTests(PlateformeWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/comptes/connexion")]
    [InlineData("/healthz")]
    [InlineData("/ready")]
    [InlineData("/swagger/v1/swagger.json")]
    public async Task NfrCert04_02_SurfacesCourantes_SontInstrumenteesEtEmettentMetrologie(string route)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(route);

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Redirect);
        Assert.True(response.Headers.Contains("Server-Timing"), $"L'en-tête Server-Timing doit être présent sur {route}");
        var timing = response.Headers.GetValues("Server-Timing").FirstOrDefault();
        Assert.NotNull(timing);
        Assert.StartsWith("total;dur=", timing);
    }
}
