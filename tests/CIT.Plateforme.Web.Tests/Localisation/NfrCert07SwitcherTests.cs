using System.Globalization;
using CIT.Plateforme.Web.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Localisation;

public class NfrCert07SwitcherTests
{
    [Fact]
    public void CookieRequestCultureProvider_DoitGenererValeurCookieValidePourEnUS()
    {
        var requestCulture = new RequestCulture("en-US");
        var cookieValue = CookieRequestCultureProvider.MakeCookieValue(requestCulture);

        Assert.Contains("c=en-US", cookieValue);
        Assert.Contains("uic=en-US", cookieValue);

        var parsed = CookieRequestCultureProvider.ParseCookieValue(cookieValue);
        Assert.NotNull(parsed);
        Assert.Equal("en-US", parsed!.Cultures.First().Value);
    }

    [Fact]
    public void CookieRequestCultureProvider_DoitGenererValeurCookieValidePourFrFR()
    {
        var requestCulture = new RequestCulture("fr-FR");
        var cookieValue = CookieRequestCultureProvider.MakeCookieValue(requestCulture);

        Assert.Contains("c=fr-FR", cookieValue);
        Assert.Contains("uic=fr-FR", cookieValue);

        var parsed = CookieRequestCultureProvider.ParseCookieValue(cookieValue);
        Assert.NotNull(parsed);
        Assert.Equal("fr-FR", parsed!.Cultures.First().Value);
    }
}
