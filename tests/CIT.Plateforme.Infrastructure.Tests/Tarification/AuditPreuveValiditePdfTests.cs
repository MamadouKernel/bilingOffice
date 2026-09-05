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
    /// PREUVE CENTRALE : un fichier PDF valide selon ISO 32000 doit contenir, en plus d'un en-tête
    /// %PDF-x.y, au minimum un objet indirect (« N 0 obj » / « endobj »), une table de références
    /// croisées (« xref ») et une bande-annonce (« trailer » / « startxref »). Leur absence signifie
    /// qu'aucun lecteur PDF conforme (Adobe Reader, moteur PDF de navigateur, etc.) ne pourra ouvrir
    /// ce fichier — malgré son extension « .pdf » et son Content-Type « application/pdf ».
    /// </summary>
    [Fact]
    public async Task Document_genere_NE_CONTIENT_PAS_la_structure_minimale_d_un_PDF_valide()
    {
        var resultat = await new GenerateurDocumentProformaPdf()
            .GenererPdfAsync(ConstruireSimulationMinimale(), Ref.Index);
        var texte = Encoding.UTF8.GetString(resultat.Contenu);

        var contientObjetIndirect = texte.Contains(" 0 obj") && texte.Contains("endobj");
        var contientXref = texte.Contains("\nxref");
        var contientTrailer = texte.Contains("trailer") && texte.Contains("startxref");

        // PREUVE : au 30/08/2026 (état du code audité), aucun de ces trois marqueurs structurels
        // n'est présent — le fichier produit est un texte brut lisible (StringBuilder simple), pas
        // un PDF binaire structuré. Si une future implémentation utilisait une bibliothèque PDF
        // réelle, ce test échouerait — signe que l'écart aurait été corrigé.
        Assert.False(contientObjetIndirect, "Aucun objet indirect PDF (« N 0 obj » / « endobj ») ne devrait être trouvé — confirmé.");
        Assert.False(contientXref, "Aucune table xref ne devrait être trouvée — confirmé.");
        Assert.False(contientTrailer, "Aucun trailer PDF ne devrait être trouvé — confirmé.");
    }

    [Fact]
    public async Task Document_genere_contient_neanmoins_les_champs_normatifs_attendus()
    {
        var sim = ConstruireSimulationMinimale();
        var resultat = await new GenerateurDocumentProformaPdf().GenererPdfAsync(sim, Ref.Index);
        var texte = Encoding.UTF8.GetString(resultat.Contenu);

        Assert.Contains(sim.Reference, texte);
        Assert.Contains("v1.0", texte);
        Assert.Contains(sim.MontantTotalXof.ToString("N0"), texte);
        Assert.Contains(MoteurCalculProforma.MentionLegaleDefaut, texte);

        // Les libellés du référentiel remplacent les valeurs d'énumération autrefois imprimées
        // telles quelles (« VingtPieds », « Dry »).
        Assert.Contains("Import local", texte);
        Assert.Contains("20 pieds", texte);
    }
}
