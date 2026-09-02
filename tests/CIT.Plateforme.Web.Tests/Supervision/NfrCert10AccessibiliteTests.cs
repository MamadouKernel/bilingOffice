using System.Net;
using CIT.Plateforme.Application.Supervision;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Supervision;

public class NfrCert10AccessibiliteTests : IClassFixture<PlateformeWebApplicationFactory>
{
    private readonly PlateformeWebApplicationFactory _factory;

    public NfrCert10AccessibiliteTests(PlateformeWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task NfrCert10_01_AccesHorsHeuresOuvrees_EstOperationnelSansVerrouHoraire()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act : Appel de connexion et consultation générale
        var response = await client.GetAsync("/comptes/connexion");

        // Assert : Doit être 200 OK
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task NfrCert10_02_ModeMaintenanceActive_Renvoie503AvecRetryAfter_EtPreserveHealthz()
    {
        // Arrange
        var client = _factory.CreateClient();
        var maintenanceService = _factory.Services.GetRequiredService<GestionnaireMaintenanceService>();

        try
        {
            // Act 1 : Activer la maintenance
            maintenanceService.ActiverMaintenance("Maintenance planifiée du système");

            // Assert 1 : /healthz reste 200 (processus vivant)
            var healthzResp = await client.GetAsync("/healthz");
            Assert.Equal(HttpStatusCode.OK, healthzResp.StatusCode);

            // Assert 2 : /ready devient 503 (service non prêt à servir)
            var readyResp = await client.GetAsync("/ready");
            Assert.Equal(HttpStatusCode.ServiceUnavailable, readyResp.StatusCode);

            // Assert 3 : Page de connexion renvoie 503 Service Unavailable avec Retry-After
            var pageResp = await client.GetAsync("/comptes/connexion");
            Assert.Equal(HttpStatusCode.ServiceUnavailable, pageResp.StatusCode);
            Assert.True(pageResp.Headers.Contains("Retry-After"));
        }
        finally
        {
            // Nettoyage : Désactiver la maintenance
            maintenanceService.DesactiverMaintenance();
        }

        // Assert 4 : Reprise immédiate après désactivation de maintenance
        var repriseResp = await client.GetAsync("/comptes/connexion");
        Assert.Equal(HttpStatusCode.OK, repriseResp.StatusCode);
    }
}
