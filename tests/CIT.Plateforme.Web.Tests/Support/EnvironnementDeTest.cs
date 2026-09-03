using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace CIT.Plateforme.Web.Tests.Support;

/// <summary>
/// Environnement d'hébergement contrôlé par le test. Plusieurs comportements fail-safe en
/// dépendent — c'est donc une dimension qui doit être explicite, et non héritée du contexte
/// d'exécution.
/// </summary>
public sealed class EnvironnementDeTest(string nom) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = nom;
    public string ApplicationName { get; set; } = "CIT.Plateforme.Web.Tests";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
