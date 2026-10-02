using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Infrastructure.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Persistence;

/// <summary>
/// NON-RÉGRESSION INCIDENT PROD 02/10/2026 — « File « Pilote Manifeste » (module BillingImport)
/// non paramétrée » : les files d'entrée n'étaient créées que par l'amorçage de développement.
/// L'approvisionnement doit les garantir partout, sans jamais dupliquer à chaque démarrage.
/// </summary>
public sealed class ProvisionnementParametrageMetierTests
{
    [Fact]
    public async Task Les_files_d_entree_des_workflows_existent_et_un_second_demarrage_ne_duplique_rien()
    {
        await using var contexte = BaseDeTestSql.CreerContexte();
        var provisionnement = new ProvisionnementParametrageMetier(contexte, NullLogger<ProvisionnementParametrageMetier>.Instance);

        await provisionnement.ExecuterAsync();
        var files = await contexte.Files.CountAsync();
        var categories = await contexte.CategoriesReclamation.CountAsync();
        var pieces = await contexte.PiecesExigeesCompte.CountAsync();

        await provisionnement.ExecuterAsync();

        Assert.True(await contexte.Files.AnyAsync(f => f.Module == "BillingImport" && f.Nom == "Pilote Manifeste"));
        Assert.True(await contexte.Files.AnyAsync(f => f.Module == "BillingExport" && f.Nom == "Back Office Export"));
        Assert.True(await contexte.Files.AnyAsync(f => f.Module == "CustomerComplaint" && f.Nom == "Support Opérationnel"));
        Assert.True(categories > 0);
        Assert.Equal(files, await contexte.Files.CountAsync());
        Assert.Equal(categories, await contexte.CategoriesReclamation.CountAsync());
        Assert.Equal(pieces, await contexte.PiecesExigeesCompte.CountAsync());
    }
}
