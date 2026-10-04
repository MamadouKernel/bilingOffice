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

    [Fact]
    public async Task Hote_configure_mais_Actif_false_ne_tente_pas_l_envoi()
    {
        // Interrupteur d'activation : l'hôte est renseigné mais l'envoi n'est pas encore autorisé.
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Notifications:Smtp:Hote"] = "smtp.exemple.com",
            ["Notifications:Smtp:Actif"] = "false",
        }).Build();
        var canal = new CanalEmailSmtp(configuration, NullLogger<CanalEmailSmtp>.Instance);

        var resultat = await canal.EnvoyerAsync(new DestinationEnvoi(Guid.NewGuid(), "client@exemple.com"), "titre", "corps");

        Assert.False(resultat.Abouti);
        Assert.Equal("envoi désactivé", resultat.Motif);
    }

    /// <summary>Checklist de mise en ligne, point 8 « API qui ne répondent pas » : un serveur SMTP
    /// qui accepte la connexion mais ne répond jamais ne bloque pas l'envoi au-delà du délai
    /// configuré — échec propre (ERR-02), repris ensuite par les nouvelles tentatives.</summary>
    [Fact]
    public async Task Serveur_smtp_muet_abandonne_apres_le_delai_configure()
    {
        var ecoute = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        ecoute.Start();
        // Accepte puis ne dit jamais « 220 ». La connexion est retenue jusqu'à la fin du test : non
        // référencée, le ramasse-miettes la fermait sous charge (faux « échec transport »).
        var acceptation = ecoute.AcceptTcpClientAsync();
        var port = ((System.Net.IPEndPoint)ecoute.LocalEndpoint).Port;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Notifications:Smtp:Hote"] = "127.0.0.1",
            ["Notifications:Smtp:Port"] = port.ToString(),
            ["Notifications:Smtp:UtiliserSsl"] = "false",
            ["Notifications:Smtp:Actif"] = "true",
            ["Notifications:Smtp:DelaiSecondes"] = "1",
        }).Build();
        var canal = new CanalEmailSmtp(configuration, NullLogger<CanalEmailSmtp>.Instance);
        var chrono = System.Diagnostics.Stopwatch.StartNew();

        var resultat = await canal.EnvoyerAsync(new DestinationEnvoi(Guid.NewGuid(), "client@exemple.com"), "titre", "corps");

        chrono.Stop();
        (await acceptation).Dispose();
        ecoute.Stop();
        Assert.False(resultat.Abouti);
        Assert.Equal("délai dépassé", resultat.Motif);
        Assert.True(chrono.Elapsed < TimeSpan.FromSeconds(10), $"abandon trop tardif : {chrono.Elapsed}");
    }
}
