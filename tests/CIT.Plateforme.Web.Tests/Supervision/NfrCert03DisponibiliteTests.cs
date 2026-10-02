using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CIT.Plateforme.Web.Tests.Support;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Supervision;

public class NfrCert03DisponibiliteTests : IClassFixture<PlateformeWebApplicationFactory>
{
    private readonly PlateformeWebApplicationFactory _factory;

    public NfrCert03DisponibiliteTests(PlateformeWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task NfrCert03_01_LivenessProbe_Healthz_Renvoie200Ok_QuandProcessusVivant()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/healthz");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }

    [Fact]
    public async Task NfrCert03_02_ReadinessProbe_Ready_InspecteDependancesCritiquesEtRenvoieRapportJson()
    {
        // Arrange — environnement complet : clé de chiffrement des pièces fournie (SEC-04), sans
        // quoi la sonde dédiée rapporte, à juste titre, un état « Degraded ».
        var client = _factory
            .WithWebHostBuilder(b => b.UseSetting("Stockage:CleChiffrement",
                Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))))
            .CreateClient();

        // Act
        var response = await client.GetAsync("/ready");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var jsonDoc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(jsonDoc);
        
        var root = jsonDoc.RootElement;
        Assert.True(root.TryGetProperty("status", out var statusProp));
        Assert.Equal("Healthy", statusProp.GetString());

        Assert.True(root.TryGetProperty("entries", out var entriesProp));
        Assert.True(entriesProp.TryGetProperty("database", out _), "La sonde database doit être présente");
        Assert.True(entriesProp.TryGetProperty("storage", out _), "La sonde storage doit être présente");
        Assert.True(entriesProp.TryGetProperty("antivirus", out _), "La sonde antivirus doit être présente");
    }
}
