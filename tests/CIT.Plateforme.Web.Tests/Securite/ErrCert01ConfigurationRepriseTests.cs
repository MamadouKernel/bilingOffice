using System;
using CIT.Plateforme.Application.Notifications;
using CIT.Plateforme.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Securite;

/// <summary>
/// FEATURE 011 — CERTIFICATION ERR-02 · suite TEST-SPEC « ERR-CERT-01 » (T035, T036, T042, T043).
///
/// Texte CDC (ERR-02, Must) : « Échec d'envoi d'une notification : nouvelle tentative
/// automatique et traçage ; l'échec d'un canal n'empêche pas les autres. »
///
/// Cette suite certifie la validation au démarrage du composant de reprise :
///   - T035 : démarrage sans configuration de reprise → échec explicite désignant le paramètre manquant ;
///   - T036 : démarrage avec configuration invalide (≤ 0) → échec explicite ;
///   - T042 : démarrage avec configuration valide → composant démarré, paramètres validés ;
///   - T043 : portée = composant de reprise, pas l'application entière (CLAR-NOT-02 reste ouverte, I4).
/// </summary>
public sealed class ErrCert01ConfigurationRepriseTests
{
    // ---- T035 : configuration absente → échec explicite ----

    [Fact]
    public void Demarrage_sans_configuration_de_reprise_echoue_explicitement()
    {
        // Configuration sans la section Notifications:Reessai
        var configVide = new ConfigurationBuilder().AddInMemoryCollection().Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configVide);
        services.AjouterInfrastructure(configVide);

        using var provider = services.BuildServiceProvider();

        // La résolution des paramètres de reprise doit échouer avec un message explicite
        var exception = Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<ParametresReessaiNotification>());

        Assert.Contains("Notifications:Reessai:NombreTentativesMax", exception.Message, StringComparison.Ordinal);
        Assert.Contains("ERR-02", exception.Message, StringComparison.Ordinal);
    }

    // ---- T036 : configuration invalide (valeur nulle ou négative) → échec explicite ----

    [Theory]
    [InlineData("0", "00:01:00", "NombreTentativesMax")]
    [InlineData("-1", "00:01:00", "NombreTentativesMax")]
    [InlineData("3", "00:00:00", "DelaiEntreTentatives")]
    [InlineData("3", "-00:01:00", "DelaiEntreTentatives")]
    public void Demarrage_avec_configuration_invalide_echoue_explicitement(
        string maxTentatives, string delai, string elementAttenduDansMessage)
    {
        var configInvalide = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Notifications:Reessai:NombreTentativesMax", maxTentatives),
                new KeyValuePair<string, string?>("Notifications:Reessai:DelaiEntreTentatives", delai),
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configInvalide);
        services.AjouterInfrastructure(configInvalide);

        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<ParametresReessaiNotification>());

        Assert.Contains(elementAttenduDansMessage, exception.Message, StringComparison.Ordinal);
    }

    // ---- T042 : configuration valide → démarrage réussi et paramètres conformes ----

    [Fact]
    public void Demarrage_avec_configuration_valide_reussit()
    {
        var configValide = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Notifications:Reessai:NombreTentativesMax", "5"),
                new KeyValuePair<string, string?>("Notifications:Reessai:DelaiEntreTentatives", "00:05:00"),
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configValide);
        services.AjouterInfrastructure(configValide);

        using var provider = services.BuildServiceProvider();

        var parametres = provider.GetRequiredService<ParametresReessaiNotification>();

        Assert.NotNull(parametres);
        Assert.Equal(5, parametres.NombreTentativesMax);
        Assert.Equal(TimeSpan.FromMinutes(5), parametres.DelaiEntreTentatives);
    }

    // ---- Cas 1 & 2 : démarrage du composant hébergé via IHostedService (vraie DI) ----

    [Fact]
    public void Demarrage_du_composant_reprise_via_hosted_service_echoue_sans_configuration()
    {
        var configVide = new ConfigurationBuilder().AddInMemoryCollection().Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configVide);
        services.AjouterInfrastructure(configVide);

        using var provider = services.BuildServiceProvider();

        // Lors du démarrage de l'hôte, les IHostedService sont résolus : le composant de reprise
        // ReessaiEnvoisNotificationService échoue immédiatement à l'instanciation si la config est absente.
        var exception = Assert.Throws<InvalidOperationException>(() =>
            provider.GetServices<Microsoft.Extensions.Hosting.IHostedService>().ToList());

        Assert.Contains("Notifications:Reessai:NombreTentativesMax", exception.Message, StringComparison.Ordinal);
        Assert.Contains("ERR-02", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Demarrage_du_composant_reprise_via_hosted_service_reussit_avec_configuration_valide()
    {
        var configValide = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Notifications:Reessai:NombreTentativesMax", "3"),
                new KeyValuePair<string, string?>("Notifications:Reessai:DelaiEntreTentatives", "00:01:00"),
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configValide);
        services.AjouterInfrastructure(configValide);

        using var provider = services.BuildServiceProvider();

        var hostedServices = provider.GetServices<Microsoft.Extensions.Hosting.IHostedService>().ToList();
        Assert.NotEmpty(hostedServices);
    }
}

