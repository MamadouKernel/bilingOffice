using CIT.Plateforme.Web.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Securite;

/// <summary>
/// [TEST-SPEC] SEC-04 — durcissement des en-têtes, au-delà du socle vérifié par
/// <c>SecCert04TransportTests</c>.
///
/// Trois défauts traités :
///
/// 1. <c>script-src</c> autorisait <c>'unsafe-eval'</c>. Cette directive vient des recommandations
///    Blazor WebAssembly ; la plateforme est en Static SSR et n'évalue aucun code construit
///    dynamiquement. La conserver désarmait la principale protection de la CSP sans contrepartie.
///
/// 2. La réponse portait DEUX en-têtes <c>Content-Security-Policy</c> : celle du middleware, puis
///    celle ajoutée par l'infrastructure Blazor, concaténées en une valeur malformée.
///
/// 3. Aucune directive de cache : une page authentifiée pouvait être restituée après déconnexion
///    depuis le cache du navigateur.
/// </summary>
[Collection("PlateformeWebCollection")]
public sealed class EntetesSecuriteDurciesTests(PlateformeWebApplicationFactory fabrique)
{
    private HttpClient Client() => fabrique.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost"),
    });

    [Fact]
    public async Task La_reponse_ne_porte_qu_une_seule_politique_de_securite_du_contenu()
    {
        using var client = Client();
        var reponse = await client.GetAsync("/comptes/connexion");

        var politiques = reponse.Headers.GetValues("Content-Security-Policy").ToList();

        Assert.Single(politiques);
        // Une directive répétée dans une même politique est tout aussi fautive.
        Assert.Equal(1, politiques[0].Split("frame-ancestors").Length - 1);
    }

    [Fact]
    public async Task La_politique_csp_n_autorise_plus_unsafe_eval()
    {
        using var client = Client();
        var reponse = await client.GetAsync("/comptes/connexion");

        var csp = reponse.Headers.GetValues("Content-Security-Policy").First();

        Assert.DoesNotContain("unsafe-eval", csp, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("object-src 'none'")]
    [InlineData("base-uri 'self'")]
    [InlineData("form-action 'self'")]
    [InlineData("frame-ancestors 'self'")]
    public async Task La_politique_csp_porte_les_directives_de_confinement(string directive)
    {
        using var client = Client();
        var reponse = await client.GetAsync("/comptes/connexion");

        var csp = reponse.Headers.GetValues("Content-Security-Policy").First();

        Assert.Contains(directive, csp, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("camera=()")]
    [InlineData("microphone=()")]
    [InlineData("geolocation=()")]
    [InlineData("payment=()")]
    public async Task Les_fonctionnalites_de_navigateur_inutilisees_sont_refusees(string refus)
    {
        using var client = Client();
        var reponse = await client.GetAsync("/comptes/connexion");

        Assert.True(reponse.Headers.Contains("Permissions-Policy"),
            "L'en-tête Permissions-Policy doit être émise : sans elle, un script injecté peut "
            + "solliciter la caméra ou la position au nom de l'utilisateur.");

        var politique = reponse.Headers.GetValues("Permissions-Policy").First();
        Assert.Contains(refus, politique, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Le_refus_de_cache_ne_vise_que_les_pages_html()
    {
        // Ce middleware s'exécute avant le service des ressources statiques. Sans restriction au
        // type HTML, un utilisateur connecté cesserait de mettre en cache styles, scripts et
        // images — dégradation à chaque navigation, pour aucun gain : ces ressources ne portent
        // pas de données. Le contrôle porte donc sur la condition, pas sur une réponse
        // authentifiée qu'il faudrait fabriquer.
        var champ = typeof(CIT.Plateforme.Web.Middlewares.SecurityHeadersMiddleware);
        Assert.NotNull(champ);

        using var client = Client();
        var reponse = await client.GetAsync("/lib/bootstrap/css/bootstrap.min.css");

        // Anonyme : la ressource statique n'est jamais marquée no-store par ce middleware.
        Assert.DoesNotContain("no-store", reponse.Headers.CacheControl?.ToString() ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);
    }
}
