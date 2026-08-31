using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.CustomerComplaint;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Reclamations;
using CIT.Plateforme.Domain.Securite;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.CustomerComplaint;

/// <summary>
/// T030 (décision : OBLIGATOIRE POUR REC-02, closure-review.md "DÉCISION FINALE T030 / REC-02") :
/// contrat de lecture réservé à l'administration — contrairement à C7-04
/// (<see cref="ListerCategoriesReclamationQuery"/>), qui ne retourne que les catégories actives et
/// ne vérifie aucune permission (contrat "Authentifié"), cette requête expose aussi les catégories
/// désactivées et est donc réservée à <see cref="Permission.AdministrationCategoriesReclamation"/>.
/// </summary>
public class ListerToutesCategoriesReclamationQueryTests
{
    private readonly Mock<ICategorieReclamationRepository> _categorieRepo = new();
    private readonly Mock<IAutorisationService> _autorisation = new();
    private readonly ListerToutesCategoriesReclamationQuery _query;

    public ListerToutesCategoriesReclamationQueryTests()
    {
        _query = new ListerToutesCategoriesReclamationQuery(_categorieRepo.Object, _autorisation.Object);
    }

    [Fact]
    public async Task Admin_habilite_voit_les_categories_actives_et_inactives()
    {
        var adminId = Guid.NewGuid();
        var categorieActive = CategorieReclamation.Creer("ACT", "Active", null);
        var categorieInactive = CategorieReclamation.Creer("INA", "Inactive", null);
        categorieInactive.Desactiver();

        _autorisation.Setup(a => a.ExigerAsync(adminId, Permission.AdministrationCategoriesReclamation, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _categorieRepo.Setup(r => r.ListerToutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategorieReclamation> { categorieActive, categorieInactive });

        var result = await _query.ExecuterAsync(adminId, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.Code == "ACT" && c.EstActif);
        Assert.Contains(result, c => c.Code == "INA" && !c.EstActif);
    }

    [Fact]
    public async Task Utilisateur_sans_permission_est_refuse()
    {
        var userId = Guid.NewGuid();
        _autorisation.Setup(a => a.ExigerAsync(userId, Permission.AdministrationCategoriesReclamation, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AccesRefuseException("Refusé"));

        await Assert.ThrowsAsync<AccesRefuseException>(() => _query.ExecuterAsync(userId, CancellationToken.None));
    }
}
