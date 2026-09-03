using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CIT.Plateforme.Web.Tests.Support;

/// <summary>
/// Retire les traitements de fond de l'hôte de test.
///
/// DÉFAUT CORRIGÉ : cinq <c>BackgroundService</c> — distribution automatique, surveillance SLA,
/// relances, distribution et réessai des notifications — démarrent avec l'hôte et interrogent la
/// base en boucle pendant toute la durée de la suite. Ils n'y font pas que lire : la distribution
/// automatique AFFECTE des tickets, la surveillance SLA lève des alertes. Un test qui prend un
/// ticket en charge manuellement pouvait donc trouver le ticket déjà affecté, et attendre en vain
/// un message de confirmation qui ne viendrait jamais.
///
/// Le symptôme observé était une corrélation parfaite : sur six exécutions, les trois qui
/// dépassaient la minute échouaient toutes sur le même scénario d'enrôlement, les trois qui
/// restaient sous la minute passaient. Une cause commune agissait — la contention avec ces
/// services, qui ralentit l'exécution et en perturbe l'état.
///
/// LE RETRAIT EST NOMMÉMENT CIBLÉ. <c>RemoveAll&lt;IHostedService&gt;()</c> emporterait aussi
/// <c>GenericWebHostService</c>, qui EST le serveur : l'hôte de test ne servirait plus rien. Le
/// filtre porte donc sur l'espace de noms des traitements applicatifs.
///
/// La couverture n'en souffre pas : ce qui est retiré est l'ORDONNANCEMENT périodique, pas la
/// logique. Celle-ci vit dans les cas d'usage correspondants, testés pour eux-mêmes.
/// </summary>
public static class TraitementsDeFondDeTest
{
    private const string EspaceDeNoms = "CIT.Plateforme.Infrastructure.TraitementsDeFond";

    public static void Retirer(IServiceCollection services)
    {
        var aRetirer = services
            .Where(d => d.ServiceType == typeof(IHostedService)
                     && d.ImplementationType?.Namespace == EspaceDeNoms)
            .ToList();

        foreach (var descripteur in aRetirer)
        {
            services.Remove(descripteur);
        }
    }
}
