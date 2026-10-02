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

        // Base partagée avec d'autres tests exécutés en parallèle : on ne compte que les lignes
        // que ce provisionnement possède, jamais des tables entières.
        Task<int> PiloteManifeste() => contexte.Files.CountAsync(f => f.Module == "BillingImport" && f.Nom == "Pilote Manifeste");
        Task<int> PieceRccm() => contexte.PiecesExigeesCompte.CountAsync(p => p.Code == "RCCM");

        await provisionnement.ExecuterAsync();
        await provisionnement.ExecuterAsync();

        Assert.Equal(1, await PiloteManifeste());
        Assert.Equal(1, await PieceRccm());
        Assert.True(await contexte.Files.AnyAsync(f => f.Module == "BillingExport" && f.Nom == "Back Office Export"));
        Assert.True(await contexte.Files.AnyAsync(f => f.Module == "CustomerComplaint" && f.Nom == "Support Opérationnel"));
        Assert.True(await contexte.CategoriesReclamation.AnyAsync());
    }
}
