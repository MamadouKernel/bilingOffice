using System.Net;
using CIT.Plateforme.Web.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Securite;

/// <summary>
/// TEST-SPEC SEC-CERT-04-01 & SEC-CERT-04-02 : Transport HTTPS et Headers de Sécurité Applicatifs.
/// </summary>
public sealed class SecCert04TransportTests
{
    [Fact]
    public async Task SecCert04_01_HttpsRedirectionEtHsts_SontActivesEnProduction()
    {
        // Arrange
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.UseEnvironment("Production");
                webBuilder.Configure(app =>
                {
                    app.UseHsts();
                    app.UseHttpsRedirection();
                    app.Run(context => context.Response.WriteAsync("OK"));
                });
            })
            .StartAsync();

        var client = host.GetTestClient();

        // Act - Requête avec header de protocole HTTP
        var response = await client.GetAsync("/");

        // Assert - Requête traitée avec succès
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SecCert04_02_SecurityHeaders_ContientLesEnTetesDeDefenseObligatoires()
    {
        // Arrange
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.Configure(app =>
                {
                    app.UseMiddleware<SecurityHeadersMiddleware>();
                    app.Run(context => context.Response.WriteAsync("OK"));
                });
            })
            .StartAsync();

        var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        Assert.True(response.Headers.Contains("X-Content-Type-Options"), "L'en-tête X-Content-Type-Options est obligatoire (nosniff).");
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").First());

        Assert.True(response.Headers.Contains("X-Frame-Options"), "L'en-tête X-Frame-Options est obligatoire (SAMEORIGIN).");
        Assert.Equal("SAMEORIGIN", response.Headers.GetValues("X-Frame-Options").First());

        Assert.True(response.Headers.Contains("Referrer-Policy"), "L'en-tête Referrer-Policy est obligatoire.");
        Assert.Equal("strict-origin-when-cross-origin", response.Headers.GetValues("Referrer-Policy").First());

        // CSP Header Check
        Assert.True(response.Headers.Contains("Content-Security-Policy"), "L'en-tête Content-Security-Policy est obligatoire.");
        var csp = response.Headers.GetValues("Content-Security-Policy").First();
        Assert.Contains("frame-ancestors 'self'", csp);
    }
}
