using System.Globalization;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Assistant;
using CIT.Plateforme.Infrastructure.Assistant;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Assistant;

/// <summary>
/// L'assistant généraliste simulé répond dans la langue active (NFR-07) : sujet bilingue via
/// <see cref="SujetAssistant.Texte"/>, plus signature et message d'escalade traduits.
/// </summary>
public sealed class AssistantSimuleTests
{
    private static AssistantSimule Creer(params SujetAssistant[] sujets) =>
        new(new RepoFake(sujets), NullLogger<AssistantSimule>.Instance);

    private static readonly SujetAssistant SujetSuivi =
        SujetAssistant.Definir("suivi, track, where is", "Suivez vos demandes dans « Mes demandes ».", 1,
            "Track your requests in \"My requests\".");

    [Fact]
    public async Task Repond_en_francais_par_defaut()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
        var reponse = await Creer(SujetSuivi).RepondreAsync([], "où en est le suivi ?");

        Assert.False(reponse.EscaladeRecommandee);
        Assert.Contains("Suivez vos demandes", reponse.Texte);
        Assert.DoesNotContain("Track your requests", reponse.Texte);
    }

    [Fact]
    public async Task Repond_en_anglais_quand_la_culture_est_anglaise()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("en-US");
        var reponse = await Creer(SujetSuivi).RepondreAsync([], "where is my request?");

        Assert.False(reponse.EscaladeRecommandee);
        Assert.Contains("Track your requests", reponse.Texte);
        Assert.DoesNotContain("Suivez vos demandes", reponse.Texte);
    }

    [Fact]
    public async Task Escalade_est_traduite_selon_la_culture()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("en-US");
        var reponse = await Creer(SujetSuivi).RepondreAsync([], "totally unrelated zzz");

        Assert.True(reponse.EscaladeRecommandee);
        Assert.Contains("An operator can help you", reponse.Texte);
    }

    private sealed class RepoFake(params SujetAssistant[] sujets) : ISujetAssistantRepository
    {
        public Task<IReadOnlyList<SujetAssistant>> ListerAsync(bool activesUniquement, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<SujetAssistant>>(sujets);

        public Task<SujetAssistant?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult<SujetAssistant?>(null);

        public Task AjouterAsync(SujetAssistant sujet, CancellationToken ct = default) => Task.CompletedTask;
    }
}
