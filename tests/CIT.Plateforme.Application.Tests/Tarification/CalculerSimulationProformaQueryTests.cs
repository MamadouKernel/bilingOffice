using CIT.Plateforme.Application.Tarification;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Tarification;

public sealed class CalculerSimulationProformaQueryTests
{
    private static VersionGrilleTarifaire CreerGrilleAvecLigne()
    {
        var g = VersionGrilleTarifaire.Creer("v1.0", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Guid.NewGuid(), DateTime.UtcNow);
        g.AjouterLigne(SensTrafic.ImportLocal, TailleConteneur.VingtPieds, "C1", 66800m, 25000m, 35000m);
        return g;
    }

    [Fact]
    public async Task CalculerSimulation_utilise_la_grille_active_et_retourne_le_resultat_detaille()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var repo = new VersionGrilleTarifaireRepository(db);

        var grille = CreerGrilleAvecLigne();
        await repo.AjouterAsync(grille);

        var handler = new CalculerSimulationProformaQueryHandler(repo, new MoteurCalculProforma());
        var requete = new CalculerSimulationProformaRequete(
            SensTrafic.ImportLocal,
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 2, 5),
            [new ConteneurSimulationRequete("C1", TailleConteneur.VingtPieds, TypeConteneur.Dry, 10m, false, false, "C1")]);

        var resultat = await handler.HandleAsync(requete);

        Assert.NotNull(resultat);
        Assert.Equal(91800m, resultat.TotalGeneralXof); // 66800 + 25000 = 91800
        Assert.Equal("v1.0", resultat.VersionGrilleLibelle);
        Assert.Single(resultat.Lignes);
        Assert.Contains("purement indicatif", resultat.MentionLegale);
    }
}
