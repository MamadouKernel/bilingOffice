using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Notifications;
using CIT.Plateforme.Infrastructure.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Notifications;

/// <summary>COM-01 / ENR-04 (décision D-07) — les canaux Sms/WhatsApp doivent échouer proprement
/// (jamais lever d'exception, ERR-02) tant que la configuration réelle n'a pas remplacé les
/// placeholders, et rejeter une coordonnée absente sans même tenter le transport.</summary>
public sealed class CanalHttpGeneriqueTests
{
    private static IConfiguration ConfigurationVide() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

    private sealed class FabriqueHttpFactice : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    [Fact]
    public async Task Adresse_absente_est_refusee_sans_tenter_le_transport()
    {
        var canal = new CanalHttpGenerique(
            CanalNotification.Sms, "Notifications:Sms", ConfigurationVide(), new FabriqueHttpFactice(),
            NullLogger<CanalHttpGenerique>.Instance);

        var resultat = await canal.EnvoyerAsync(new DestinationEnvoi(Guid.NewGuid(), null), "titre", "corps");

        Assert.False(resultat.Abouti);
        Assert.Equal("coordonnée absente", resultat.Motif);
    }

    [Fact]
    public async Task Configuration_absente_echoue_proprement_sans_exception()
    {
        var canal = new CanalHttpGenerique(
            CanalNotification.WhatsApp, "Notifications:WhatsApp", ConfigurationVide(), new FabriqueHttpFactice(),
            NullLogger<CanalHttpGenerique>.Instance);

        var resultat = await canal.EnvoyerAsync(new DestinationEnvoi(Guid.NewGuid(), "+2250700000000"), "titre", "corps");

        Assert.False(resultat.Abouti);
        Assert.Contains("non configuré", resultat.Motif);
    }
}
