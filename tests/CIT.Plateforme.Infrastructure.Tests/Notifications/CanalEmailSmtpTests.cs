using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Infrastructure.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Notifications;

/// <summary>COM-01 / ENR-04 (décision D-07) — le canal Email doit échouer proprement (jamais
/// lever d'exception, ERR-02) tant que la configuration SMTP réelle n'a pas remplacé les
/// placeholders, et rejeter une coordonnée absente sans même tenter le transport.</summary>
public sealed class CanalEmailSmtpTests
{
    private static IConfiguration ConfigurationVide() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

    [Fact]
    public async Task Adresse_absente_est_refusee_sans_tenter_le_transport()
    {
        var canal = new CanalEmailSmtp(ConfigurationVide(), NullLogger<CanalEmailSmtp>.Instance);

        var resultat = await canal.EnvoyerAsync(new DestinationEnvoi(Guid.NewGuid(), null), "titre", "corps");

        Assert.False(resultat.Abouti);
        Assert.Equal("coordonnée absente", resultat.Motif);
    }

    [Fact]
    public async Task Configuration_smtp_absente_echoue_proprement_sans_exception()
    {
        var canal = new CanalEmailSmtp(ConfigurationVide(), NullLogger<CanalEmailSmtp>.Instance);

        var resultat = await canal.EnvoyerAsync(new DestinationEnvoi(Guid.NewGuid(), "client@exemple.com"), "titre", "corps");

        Assert.False(resultat.Abouti);
        Assert.Equal("smtp non configuré", resultat.Motif);
    }
}
