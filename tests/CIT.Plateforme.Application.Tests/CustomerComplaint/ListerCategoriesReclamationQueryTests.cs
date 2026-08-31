using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.CustomerComplaint;
using CIT.Plateforme.Domain.Reclamations;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.CustomerComplaint;

public class ListerCategoriesReclamationQueryTests
{
    private readonly Mock<ICategorieReclamationRepository> _categorieRepo = new();
    private readonly ListerCategoriesReclamationQuery _query;

    public ListerCategoriesReclamationQueryTests()
    {
        _query = new ListerCategoriesReclamationQuery(_categorieRepo.Object);
    }

    [Fact]
    public async Task Ne_retourne_que_les_categories_actives()
    {
        // Arrange
        var categorie1 = CategorieReclamation.Creer("CAT1", "Catégorie 1", Guid.NewGuid());
        var categorie2 = CategorieReclamation.Creer("CAT2", "Catégorie 2", Guid.NewGuid());
        
        _categorieRepo.Setup(r => r.ListerActivesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategorieReclamation> { categorie1, categorie2 });

        // Act
        var result = await _query.ExecuterAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("CAT1", result[0].Code);
        Assert.Equal("CAT2", result[1].Code);
    }
}
