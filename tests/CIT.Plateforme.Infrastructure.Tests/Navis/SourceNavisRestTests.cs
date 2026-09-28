using System.Net;
using System.Net.Http;
using CIT.Plateforme.Infrastructure.Navis;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Navis;

public sealed class SourceNavisRestTests
{
    private static NavisOptions OptionsActives() => new()
    {
        Active = true,
        BaseUrl = "https://n4.test/apex",
        OperatorId = "LPC",
        ComplexId = "NZLYT",
        FacilityId = "LCT",
        YardId = "MAIN",
        Utilisateur = "n4api",
        MotDePasse = "secret",
    };

    private static SourceNavisRest Creer(NavisOptions options, HttpMessageHandler handler) =>
        new(new HttpClient(handler), Options.Create(options), NullLogger<SourceNavisRest>.Instance);

    [Fact]
    public async Task Inactive_ne_touche_pas_le_reseau_et_repond_indisponible()
    {
        var handler = new HandlerStub(_ => throw new HttpRequestException("ne doit pas être appelé"));
        var source = Creer(new NavisOptions { Active = false }, handler);

        var resultat = await source.InterrogerAsync("N_IMPORTE", new Dictionary<string, string>());

        Assert.False(resultat.Disponible);
        Assert.Equal(0, handler.NombreAppels);
    }

    [Fact]
    public async Task Construit_l_url_avec_filtername_scope_et_PARM()
    {
        string? urlVue = null;
        var handler = new HandlerStub(req =>
        {
            urlVue = req.RequestUri!.AbsoluteUri;
            return Reponse("<query-response></query-response>");
        });
        var source = Creer(OptionsActives(), handler);

        await source.InterrogerAsync("STATUT CONTENEUR", new Dictionary<string, string> { ["CTR_NBR"] = "MSCU1234567" });

        Assert.NotNull(urlVue);
        Assert.Contains("/apex/api/query?filtername=STATUT%20CONTENEUR", urlVue);
        Assert.Contains("operatorId=LPC", urlVue);
        Assert.Contains("PARM_CTR_NBR=MSCU1234567", urlVue);
    }

    [Fact]
    public async Task Element_error_devient_indisponible_avec_motif()
    {
        var handler = new HandlerStub(_ => Reponse(
            "<query-response><error>User N4api does not have access to requested scope.</error></query-response>"));
        var source = Creer(OptionsActives(), handler);

        var resultat = await source.InterrogerAsync("X", new Dictionary<string, string>());

        Assert.False(resultat.Disponible);
        Assert.Contains("does not have access", resultat.Motif);
    }

    [Fact]
    public async Task Lignes_sont_parsees_en_colonnes()
    {
        const string xml = """
            <query-response>
              <row><ctrNbr>MSCU1234567</ctrNbr><status>YARD</status></row>
              <row><ctrNbr>CMAU9876543</ctrNbr><status>OUT</status></row>
            </query-response>
            """;
        var handler = new HandlerStub(_ => Reponse(xml));
        var source = Creer(OptionsActives(), handler);

        var resultat = await source.InterrogerAsync("X", new Dictionary<string, string>());

        Assert.True(resultat.Disponible);
        Assert.Equal(2, resultat.Lignes.Count);
        Assert.Equal("MSCU1234567", resultat.Lignes[0].Colonnes["ctrNbr"]);
        Assert.Equal("OUT", resultat.Lignes[1].Colonnes["status"]);
    }

    [Fact]
    public async Task Code_http_non_2xx_devient_indisponible()
    {
        var handler = new HandlerStub(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var source = Creer(OptionsActives(), handler);

        var resultat = await source.InterrogerAsync("X", new Dictionary<string, string>());

        Assert.False(resultat.Disponible);
    }

    private static HttpResponseMessage Reponse(string xml) =>
        new(HttpStatusCode.OK) { Content = new StringContent(xml) };

    private sealed class HandlerStub(Func<HttpRequestMessage, HttpResponseMessage> repondre) : HttpMessageHandler
    {
        public int NombreAppels { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            NombreAppels++;
            return Task.FromResult(repondre(request));
        }
    }
}
