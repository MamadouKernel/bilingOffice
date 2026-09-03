using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace CIT.Plateforme.Infrastructure.Tests.Support;

/// <summary>
/// Environnement d'hébergement contrôlé par le test. Plusieurs comportements fail-safe dépendent
/// de l'environnement — c'est donc une dimension qui doit être testable explicitement, et non
/// héritée du contexte d'exécution.
/// </summary>
public sealed class EnvironnementDeTest(string nom) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = nom;
    public string ApplicationName { get; set; } = "CIT.Plateforme.Infrastructure.Tests";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
