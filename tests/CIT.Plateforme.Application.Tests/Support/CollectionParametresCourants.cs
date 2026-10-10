using Xunit;

namespace CIT.Plateforme.Application.Tests.Support;

/// <summary>
/// Collection xUnit regroupant toutes les suites qui MUTENT le paramètre global
/// <c>ParametresCourants</c> (un état statique partagé). xUnit parallélise les classes de test entre
/// elles : sans regroupement, deux suites touchant ce global tournaient en même temps et se
/// marchaient dessus (échec intermittent, dépendant de l'ordre). Partager cette collection les
/// sérialise entre elles ; <c>DisableParallelization</c> évite aussi tout chevauchement avec les
/// autres collections pendant qu'elles manipulent le global.
/// </summary>
[CollectionDefinition(Nom, DisableParallelization = true)]
public sealed class CollectionParametresCourants
{
    public const string Nom = "ParametresCourants (global statique)";
}
