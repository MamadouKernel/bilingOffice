using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Infrastructure.Tests.Support;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Persistence;

// FEATURE 008 (Enrôlement Transporteurs) — T007. ListerParClientAsync est une extension
// GÉNÉRIQUE de IDossierRepository (research.md D9), pas spécifique à Enrolement — testée sur
// SQL Server réel (LocalDB), comme ListerParSocieteAsync qu'elle complète.
public sealed class DossierRepositoryTests
{
    [Fact]
    public async Task ListerParClientAsync_RetourneUniquementLesDossiersDuClient()
    {
        using var db = BaseDeTestSql.CreerContexte();
        var repo = new DossierRepository(db);

        var clientA = Guid.NewGuid();
        var clientB = Guid.NewGuid();

        var dossierA1 = Dossier.Creer($"DOS-{Guid.NewGuid():N}", clientA, DateTime.UtcNow);
        var dossierA2 = Dossier.Creer($"DOS-{Guid.NewGuid():N}", clientA, DateTime.UtcNow);
        var dossierB1 = Dossier.Creer($"DOS-{Guid.NewGuid():N}", clientB, DateTime.UtcNow);

        await repo.AjouterAsync(dossierA1);
        await repo.AjouterAsync(dossierA2);
        await repo.AjouterAsync(dossierB1);
        await db.SaveChangesAsync();

        var resultatA = await repo.ListerParClientAsync(clientA);
        var resultatB = await repo.ListerParClientAsync(clientB);

        Assert.Equal(2, resultatA.Count);
        Assert.All(resultatA, d => Assert.Equal(clientA, d.ClientId));
        Assert.Single(resultatB);
        Assert.Equal(clientB, resultatB[0].ClientId);
    }

    [Fact]
    public async Task ListerParClientAsync_AucunDossier_RetourneListeVide()
    {
        using var db = BaseDeTestSql.CreerContexte();
        var repo = new DossierRepository(db);

        var resultat = await repo.ListerParClientAsync(Guid.NewGuid());

        Assert.Empty(resultat);
    }
}
