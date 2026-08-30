using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Tarification;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Tarification;

/// <summary>
/// Stub PDF en mémoire pour les tests de la couche Application — aucun rendu réel.
/// </summary>
internal sealed class GenerateurPdfPourTests : IGenerateurDocumentProformaPdf
{
    public Task<DocumentPdfProformaResultat> GenererPdfAsync(SimulationProforma simulation, CancellationToken ct = default)
    {
        var contenu = System.Text.Encoding.UTF8.GetBytes($"PDF-{simulation.Reference}");
        return Task.FromResult(new DocumentPdfProformaResultat(contenu, $"proforma-{simulation.Reference}.pdf", "application/pdf"));
    }
}

public sealed class GenererDocumentProformaPdfTests
{
    [Fact]
    public async Task GenererDocumentProformaPdf_produit_un_fichier_binaire()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var simRepo = new SimulationProformaRepository(db);

        var societeId = Guid.NewGuid();
        var conteneur = new ConteneurSimulation("C1", TailleConteneur.VingtPieds, TypeConteneur.Dry, 10m, false, false, "C1");
        var dossier = new DossierSimulation(SensTrafic.ImportLocal, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 5), [conteneur]);
        var ligne = new LigneResultatConteneur(conteneur, 66800m, 25000m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m);
        var simulation = SimulationProforma.Creer("SIM-001", societeId, Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), "v1.0", dossier, [ligne]);
        await simRepo.AjouterAsync(simulation);

        var pdfGen = new GenerateurPdfPourTests();
        var handler = new GenererDocumentProformaPdfQueryHandler(simRepo, pdfGen);
        var res = await handler.HandleAsync(new GenererDocumentProformaPdfRequete(Guid.NewGuid(), societeId, simulation.Id));

        Assert.NotNull(res);
        Assert.Equal("proforma-SIM-001.pdf", res.NomFichier);
        Assert.Equal("application/pdf", res.TypeContenu);
        Assert.True(res.Contenu.Length > 0);
    }

    [Fact]
    public async Task GenererDocumentProformaPdf_refuse_acces_si_societe_differente()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var simRepo = new SimulationProformaRepository(db);

        var societeA = Guid.NewGuid();
        var societeB = Guid.NewGuid();
        var conteneur = new ConteneurSimulation("C1", TailleConteneur.VingtPieds, TypeConteneur.Dry, 10m, false, false, "C1");
        var dossier = new DossierSimulation(SensTrafic.ImportLocal, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 5), [conteneur]);
        var ligne = new LigneResultatConteneur(conteneur, 66800m, 25000m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m);
        var simulation = SimulationProforma.Creer("SIM-IDOR-PDF", societeA, Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), "v1.0", dossier, [ligne]);
        await simRepo.AjouterAsync(simulation);

        var handler = new GenererDocumentProformaPdfQueryHandler(simRepo, new GenerateurPdfPourTests());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.HandleAsync(new GenererDocumentProformaPdfRequete(Guid.NewGuid(), societeB, simulation.Id)));
    }
}
