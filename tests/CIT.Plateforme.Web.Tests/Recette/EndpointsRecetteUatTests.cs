using System;
using System.Net;
using System.Threading.Tasks;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Recette;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Recette;

/// <summary>
/// Cœur PUR des barrières de l'ouverture de session de recette UAT
/// (<see cref="EndpointsRecetteUat.Verifier"/>) — rapide, sans HTTP.
///
/// Mutation : retirer la barrière de politique OU la barrière <c>@local.test</c> dans
/// <c>Verifier</c> fait tomber les tests correspondants.
/// </summary>
public sealed class VerifierRecetteUatTests
{
    private const string BaseRecette = "CIT_UAT_RECETTE";
    private const string SecretFictif = "secret-de-recette-fictif";

    private static DecisionRecetteUat DecisionAutorisee() =>
        PolitiqueRecetteUat.Decider("UAT", SecretFictif, "true", BaseRecette, BaseRecette);

    [Fact]
    public void Perimetre_UAT_et_adresse_de_recette_sont_autorises()
    {
        Assert.True(DecisionAutorisee().AmorcageAutorise);
        Assert.Equal(
            EndpointsRecetteUat.Verdict.Autorise,
            EndpointsRecetteUat.Verifier(DecisionAutorisee(), "uat.superviseur@local.test"));
    }

    [Theory]
    [InlineData("Production", "true", BaseRecette, BaseRecette)]  // production
    [InlineData("UAT", "false", BaseRecette, BaseRecette)]        // seed non activé
    [InlineData("UAT", "true", BaseRecette, "AutreBase")]         // base courante ≠ base désignée
    [InlineData("UAT", "true", null, BaseRecette)]                // base non désignée
    public void Hors_perimetre_UAT_est_refuse(string env, string activation, string? baseAutorisee, string baseCourante)
    {
        var decision = PolitiqueRecetteUat.Decider(env, SecretFictif, activation, baseAutorisee, baseCourante);

        Assert.Equal(
            EndpointsRecetteUat.Verdict.HorsPerimetreUat,
            EndpointsRecetteUat.Verifier(decision, "uat.superviseur@local.test"));
    }

    [Fact]
    public void Sans_secret_hors_perimetre_est_refuse()
    {
        var decision = PolitiqueRecetteUat.Decider("UAT", motDePasse: null, "true", BaseRecette, BaseRecette);

        Assert.Equal(
            EndpointsRecetteUat.Verdict.HorsPerimetreUat,
            EndpointsRecetteUat.Verifier(decision, "uat.superviseur@local.test"));
    }

    [Theory]
    [InlineData("attaquant@gmail.com")]
    [InlineData("uat.superviseur@cotedivoireterminal.com")]
    [InlineData("")]
    [InlineData(null)]
    public void Adresse_hors_domaine_de_recette_est_refusee(string? email)
    {
        Assert.Equal(
            EndpointsRecetteUat.Verdict.AdresseNonRecette,
            EndpointsRecetteUat.Verifier(DecisionAutorisee(), email));
    }
}

/// <summary>
/// Intégration : l'endpoint câblé est **inerte hors du périmètre UAT**. La base de test est
/// <c>CitPlateformeTestsWeb</c>, pas <c>CIT_UAT_RECETTE</c> — la politique refuse, donc 404, et
/// aucune session n'est ouverte. C'est la preuve qu'aucun contournement n'existe hors UAT.
/// </summary>
[Collection("PlateformeWebCollection")]
public sealed class EndpointsRecetteUatIntegrationTests(PlateformeWebApplicationFactory factory)
{
    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true,
        BaseAddress = new Uri("https://localhost"),
    });

    [Fact]
    public async Task Inerte_dans_l_environnement_de_test_non_UAT()
    {
        var reponse = await Client().GetAsync("/uat/session/uat.superviseur@local.test");

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
        Assert.False(reponse.Headers.Contains("Set-Cookie"),
            "Aucun cookie d'authentification ne doit être posé hors périmètre UAT.");
    }

    [Fact]
    public async Task Refuse_une_adresse_hors_domaine_de_recette()
    {
        var reponse = await Client().GetAsync("/uat/session/attaquant@gmail.com");

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }
}
