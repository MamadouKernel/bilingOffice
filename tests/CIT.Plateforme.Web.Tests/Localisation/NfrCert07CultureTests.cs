using System.Globalization;
using CIT.Plateforme.Web.Resources;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Localisation;

public class NfrCert07CultureTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly RequestLocalizationOptions _options;

    public NfrCert07CultureTests()
    {
        var services = new ServiceCollection();
        services.AddLocalization(options => options.ResourcesPath = "Resources");
        services.AddSingleton<IStringLocalizer<SharedResources>, SharedResourcesLocalizer>();
        
        services.Configure<RequestLocalizationOptions>(options =>
        {
            var supportedCultures = new[]
            {
                new CultureInfo("fr-FR"),
                new CultureInfo("en-US")
            };

            options.DefaultRequestCulture = new RequestCulture("fr-FR");
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
            
            options.RequestCultureProviders = new IRequestCultureProvider[]
            {
                new CookieRequestCultureProvider(),
                new AcceptLanguageHeaderRequestCultureProvider()
            };
        });

        _serviceProvider = services.BuildServiceProvider();
        _options = _serviceProvider.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    [Fact]
    public async Task Defaut_DoitEtreFrancais_LorsqueAucunePreferenceFournie()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var provider = new CookieRequestCultureProvider();

        // Act
        var result = await provider.DetermineProviderCultureResult(context);

        // Assert
        Assert.Null(result);
        Assert.Equal("fr-FR", _options.DefaultRequestCulture.Culture.Name);
    }

    [Fact]
    public async Task AcceptLanguage_DoitResoudreAnglais_LorsqueHeaderEnUSPresent()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["Accept-Language"] = "en-US,en;q=0.9";
        var provider = new AcceptLanguageHeaderRequestCultureProvider();

        // Act
        var result = await provider.DetermineProviderCultureResult(context);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("en", result!.Cultures.First().Value);
    }

    [Fact]
    public async Task Cookie_DoitEtrePrioritaire_SurAcceptLanguage()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["Accept-Language"] = "fr-FR,fr;q=0.9";
        context.Request.Headers["Cookie"] = $"{CookieRequestCultureProvider.DefaultCookieName}={CookieRequestCultureProvider.MakeCookieValue(new RequestCulture("en-US"))}";
        
        var cookieProvider = new CookieRequestCultureProvider();
        var headerProvider = new AcceptLanguageHeaderRequestCultureProvider();

        // Act
        var cookieResult = await cookieProvider.DetermineProviderCultureResult(context);
        var headerResult = await headerProvider.DetermineProviderCultureResult(context);

        // Assert
        Assert.NotNull(cookieResult);
        Assert.Equal("en-US", cookieResult!.Cultures.First().Value);
        Assert.StartsWith("fr", headerResult!.Cultures.First().Value);
    }
}
