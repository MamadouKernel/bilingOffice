using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Tarification;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Domain.Tarification.Referentiels;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

using A = CIT.Plateforme.Domain.Tarification.ParametrageInitialAnnexeA;

namespace CIT.Plateforme.Application.Tests.Tarification;

/// <summary>
/// Stub PDF en mémoire pour les tests de la couche Application — aucun rendu réel.
/// </summary>
internal sealed class GenerateurPdfPourTests : IGenerateurDocumentProformaPdf
{
    public Task<DocumentPdfProformaResultat> GenererPdfAsync(
        SimulationProforma simulation, ReferentielsTarifaires referentiels, CancellationToken ct = default)
    {
        var contenu = System.Text.Encoding.UTF8.GetBytes($"PDF-{simulation.Reference}");
        return Task.FromResult(new DocumentPdfProformaResultat(contenu, $"proforma-{simulation.Reference}.pdf", "application/pdf"));
    }
}

public sealed class GenererDocumentProformaPdfTests
{
    private static (SimulationProforma Simulation, ReferentielsAnnexeA Referentiels) ConstruireSimulation(
        string reference, Guid societeId)
    {
        var referentiels = ReferentielsDeTest.EnMemoire();
        var grille = ReferentielsDeTest.GrilleMinimale(referentiels);

        var conteneur = new ConteneurSimulation(
            "C1",
            referentiels.Taille(A.TailleVingtPieds),
            referentiels.Type(A.TypeDry),
            10m,
            EstDangereux: false,
            EstHorsGabarit: false,
            referentiels.Categorie(A.SensImportLocal, "C1"));

        var dossier = new DossierSimulation(
            referentiels.Sens(A.SensImportLocal),
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 5),
            [conteneur]);

        var resultat = new MoteurCalculProforma().Calculer(dossier, grille, referentiels.Index);

        var simulation = SimulationProforma.Creer(
            reference, societeId, Guid.NewGuid(), DateTime.UtcNow, grille.Id, "v1.0", dossier, resultat.LignesConteneurs);

        return (simulation, referentiels);
    }

    [Fact]
    public async Task GenererDocumentProformaPdf_produit_un_fichier_binaire()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        ReferentielsDeTest.Amorcer(db);
        var simRepo = new SimulationProformaRepository(db);
        var referentielsRepo = new ReferentielsTarifairesRepository(db);

        var societeId = Guid.NewGuid();
        var (simulation, _) = ConstruireSimulation("SIM-001", societeId);
        await simRepo.AjouterAsync(simulation);

        var handler = new GenererDocumentProformaPdfQueryHandler(
            simRepo, referentielsRepo, new GenerateurPdfPourTests(), CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.ControleAcces(db));
        var res = await handler.HandleAsync(new GenererDocumentProformaPdfRequete(CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.AppelantDeSociete(db, societeId), simulation.Id));

        Assert.NotNull(res);
        Assert.Equal("proforma-SIM-001.pdf", res.NomFichier);
        Assert.Equal("application/pdf", res.TypeContenu);
        Assert.True(res.Contenu.Length > 0);
    }

    [Fact]
    public async Task GenererDocumentProformaPdf_refuse_acces_si_societe_differente()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        ReferentielsDeTest.Amorcer(db);
        var simRepo = new SimulationProformaRepository(db);
        var referentielsRepo = new ReferentielsTarifairesRepository(db);

        var societeA = Guid.NewGuid();
        var societeB = Guid.NewGuid();
        var (simulation, _) = ConstruireSimulation("SIM-IDOR-PDF", societeA);
        await simRepo.AjouterAsync(simulation);

        var handler = new GenererDocumentProformaPdfQueryHandler(
            simRepo, referentielsRepo, new GenerateurPdfPourTests(), CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.ControleAcces(db));

        await Assert.ThrowsAsync<CIT.Plateforme.Application.Securite.AccesRefuseException>(
            () => handler.HandleAsync(new GenererDocumentProformaPdfRequete(CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.AppelantDeSociete(db, societeB), simulation.Id)));
    }
}
