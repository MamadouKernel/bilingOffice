using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Documents;

/// <summary>
/// Des pièces ont été stockées sans passer par l'antivirus (module Avoir &amp; Exonération, avant
/// correctif) : restées « non analysées », elles étaient illisibles. Le rattrapage du démarrage les
/// analyse ; une pièce déjà analysée n'est jamais relue.
/// </summary>
public sealed class RattrapageAnalyseAntivirusTests
{
    [Fact]
    public async Task Les_pieces_non_analysees_sont_analysees_et_les_autres_ignorees()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var maintenant = DateTime.UtcNow;
        var nonAnalysee = Document.Deposer(Guid.NewGuid(), Guid.NewGuid(), "justificatif.pdf", "application/pdf", 3, "cle-1", maintenant);
        var dejaSaine = Document.Deposer(Guid.NewGuid(), Guid.NewGuid(), "facture.pdf", "application/pdf", 3, "cle-2", maintenant);
        dejaSaine.MarquerResultatAntivirus(StatutAnalyseAntivirus.Sain);
        db.Documents.AddRange(nonAnalysee, dejaSaine);
        await db.SaveChangesAsync();

        var stockage = new Mock<IStockageDocuments>();
        stockage.Setup(s => s.LireAsync("cle-1", It.IsAny<CancellationToken>())).ReturnsAsync(() => new MemoryStream([1, 2, 3]));
        var antivirus = new Mock<IAnalyseAntivirus>();
        antivirus.Setup(a => a.AnalyserAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>())).ReturnsAsync(StatutAnalyseAntivirus.Sain);

        var analysees = await new RattrapageAnalyseAntivirus(db, stockage.Object, antivirus.Object, NullLogger<RattrapageAnalyseAntivirus>.Instance)
            .ExecuterAsync();

        Assert.Equal(1, analysees);
        Assert.Equal(StatutAnalyseAntivirus.Sain, (await db.Documents.FindAsync(nonAnalysee.Id))!.StatutAntivirus);
        stockage.Verify(s => s.LireAsync("cle-2", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Une_piece_illisible_n_empeche_pas_le_rattrapage_des_autres()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var maintenant = DateTime.UtcNow;
        db.Documents.AddRange(
            Document.Deposer(Guid.NewGuid(), Guid.NewGuid(), "absente.pdf", "application/pdf", 3, "cle-absente", maintenant),
            Document.Deposer(Guid.NewGuid(), Guid.NewGuid(), "presente.pdf", "application/pdf", 3, "cle-presente", maintenant));
        await db.SaveChangesAsync();

        var stockage = new Mock<IStockageDocuments>();
        stockage.Setup(s => s.LireAsync("cle-absente", It.IsAny<CancellationToken>())).ThrowsAsync(new FileNotFoundException());
        stockage.Setup(s => s.LireAsync("cle-presente", It.IsAny<CancellationToken>())).ReturnsAsync(() => new MemoryStream([1]));
        var antivirus = new Mock<IAnalyseAntivirus>();
        antivirus.Setup(a => a.AnalyserAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>())).ReturnsAsync(StatutAnalyseAntivirus.Sain);

        var analysees = await new RattrapageAnalyseAntivirus(db, stockage.Object, antivirus.Object, NullLogger<RattrapageAnalyseAntivirus>.Instance)
            .ExecuterAsync();

        Assert.Equal(1, analysees);
    }
}
