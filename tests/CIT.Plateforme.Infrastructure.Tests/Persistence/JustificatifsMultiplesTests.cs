using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Persistence;

/// <summary>
/// Une pièce d'ouverture de compte peut tenir en plusieurs fichiers (recto/verso, pages séparées) :
/// le schéma ne doit plus interdire deux dépôts pour la même pièce d'une même demande.
/// </summary>
public sealed class JustificatifsMultiplesTests
{
    [Fact]
    public void Le_schema_autorise_plusieurs_fichiers_pour_une_meme_piece()
    {
        var options = new DbContextOptionsBuilder<PlateformeDbContext>().UseSqlServer("Server=.;Database=ModeleSeulement").Options;
        using var contexte = new PlateformeDbContext(options);

        var entite = contexte.Model.FindEntityType(typeof(JustificatifDemandeCompte))!;
        var index = entite.GetIndexes().Single(i => i.Properties.Select(p => p.Name).SequenceEqual([nameof(JustificatifDemandeCompte.SocieteId), nameof(JustificatifDemandeCompte.PieceCode)]));

        Assert.False(index.IsUnique);
    }
}
