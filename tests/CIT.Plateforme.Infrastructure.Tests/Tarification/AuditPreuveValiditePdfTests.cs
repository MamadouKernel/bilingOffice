using System.Text;
using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Domain.Tarification.Referentiels;
using CIT.Plateforme.Infrastructure.Documents;
using Xunit;

using A = CIT.Plateforme.Domain.Tarification.ParametrageInitialAnnexeA;

namespace CIT.Plateforme.Infrastructure.Tests.Tarification;

/// <summary>
/// [AUDIT POST-IMPLÉMENTATION F005 — TEST DE PREUVE, §27] : SIM-09 exige la « génération d'une
/// proforma téléchargeable et imprimable ». <see cref="GenerateurDocumentProformaPdf"/> (le seul
/// générateur réel — les tests d'Application.Tests utilisent tous un stub en mémoire, jamais cette
/// classe) est audité ici directement, sans stub, pour déterminer si l'octet produit constitue un
/// document PDF valide au sens strict du format (ISO 32000) ou seulement un fichier texte portant
/// une extension et un en-tête trompeurs. Ce test ne corrige rien ; il constate.
/// </summary>
public sealed class AuditPreuveValiditePdfTests
{
    private static readonly ReferentielsAnnexeA Ref = ParametrageInitialAnnexeA.CreerReferentiels();

    private static SimulationProforma ConstruireSimulationMinimale()
    {
        var grille = VersionGrilleTarifaire.Creer(
            "v1.0", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Guid.NewGuid(), DateTime.UtcNow,
            A.FranchiseBranchementJours);
        ParametrageInitialAnnexeA.AppliquerParametres(grille, Ref);

        var sens = Ref.ParSens(A.SensImportLocal);
        var taille = Ref.ParTaille(A.TailleVingtPieds);
        var type = Ref.ParType(A.TypeDry);
        var categorie = Ref.ParCategorie(A.SensImportLocal, "C1");

        grille.AjouterLigne(sens.Id, taille.Id, categorie.Id, 66800m, 25000m, 0m);

        var dossier = new DossierSimulation(
            sens.Id,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 5),
            [new ConteneurSimulation("AUDIT01", taille.Id, type.Id, 10m, false, false, categorie.Id)]);

        var resultat = new MoteurCalculProforma().Calculer(dossier, grille, Ref.Index);

        return SimulationProforma.Creer(
            "SIM-AUDIT-PDF-0001", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow,
            grille.Id, "v1.0", dossier, resultat.LignesConteneurs);
    }

    [Fact]
    public async Task Document_genere_commence_par_un_en_tete_PDF_et_se_termine_par_EOF()
    {
        var resultat = await new GenerateurDocumentProformaPdf()
            .GenererPdfAsync(ConstruireSimulationMinimale(), Ref.Index);
        var texte = Encoding.UTF8.GetString(resultat.Contenu);

        Assert.StartsWith("%PDF-1.4", texte);
        Assert.Contains("%%EOF", texte);
    }

    /// <summary>
    /// ÉCART F005 CORRIGÉ : le générateur produit désormais un PDF réel (QuestPDF, commit 2992d39).
    /// Ce test, qui constatait autrefois l'ABSENCE de structure PDF, vérifie maintenant sa présence
    /// au sens ISO 32000 : objets indirects, table de références croisées et bande-annonce. Le
    /// contenu étant compressé, les champs normatifs ne sont plus lisibles en texte brut ; ils
    /// sont couverts côté rendu par les tests de la proforma.
    /// </summary>
    [Fact]
    public async Task Document_genere_contient_la_structure_minimale_d_un_PDF_valide()
    {
        var resultat = await new GenerateurDocumentProformaPdf()
            .GenererPdfAsync(ConstruireSimulationMinimale(), Ref.Index);
        var texte = Encoding.Latin1.GetString(resultat.Contenu);

        Assert.Contains(" 0 obj", texte);
        Assert.Contains("endobj", texte);
        Assert.Contains("xref", texte);
        Assert.Contains("startxref", texte);
    }
}
