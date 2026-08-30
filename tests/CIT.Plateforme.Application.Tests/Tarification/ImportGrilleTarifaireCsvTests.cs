using System.Text;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tarification;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Tarification;

public sealed class ImportGrilleTarifaireCsvTests
{
    [Fact]
    public async Task Import_csv_valide_cree_version_et_ses_lignes()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var repo = new VersionGrilleTarifaireRepository(db);

        var csv = new StringBuilder();
        csv.AppendLine("Sens;Taille;Categorie;Acconage;Relevage;BranchementJour");
        csv.AppendLine("ImportLocal;20;C1;66800;25000;35000");
        csv.AppendLine("ImportLocal;40;C1;133600;50000;67000");

        var service = new ImportateurGrilleTarifaireCsv();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv.ToString()));

        var resultat = service.AnalyserEtValider(stream);

        Assert.True(resultat.EstValide);
        Assert.Empty(resultat.Erreurs);
        Assert.Equal(2, resultat.Lignes.Count);

        var administrateur = Guid.NewGuid();
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(administrateur, Permission.TarificationAdministrer));
        await db.SaveChangesAsync();

        var handler = new ImporterGrilleTarifaireCsvCommandHandler(repo, service, new AutorisationService(db));
        var commande = new ImporterGrilleTarifaireCsvCommande(
            administrateur,
            "v2.0",
            DateTime.UtcNow.AddDays(10),
            ParametresTarification.CreerDefautAnnexeA(),
            new MemoryStream(Encoding.UTF8.GetBytes(csv.ToString())));

        var reponse = await handler.HandleAsync(commande);
        Assert.True(reponse.Succes);
        Assert.Equal(2, reponse.NombreLignesImportees);

        var persistee = await repo.ObtenirParIdAsync(reponse.VersionId!.Value);
        Assert.NotNull(persistee);
        Assert.Equal("v2.0", persistee.NumeroVersion);
        Assert.Equal(2, persistee.Lignes.Count);
    }

    [Fact]
    public void Import_csv_entete_invalide_retourne_erreur_bloquante()
    {
        var csv = "Sens;Taille;Categorie;MontantFaux\nImportLocal;20;C1;1000";
        var service = new ImportateurGrilleTarifaireCsv();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var res = service.AnalyserEtValider(stream);

        Assert.False(res.EstValide);
        Assert.Contains(res.Erreurs, e => e.Contains("En-tête CSV invalide"));
    }

    [Fact]
    public void Import_csv_ligne_incorrecte_au_milieu_rejette_tout_le_fichier()
    {
        var csv = new StringBuilder();
        csv.AppendLine("Sens;Taille;Categorie;Acconage;Relevage;BranchementJour");
        csv.AppendLine("ImportLocal;20;C1;66800;25000;35000");
        csv.AppendLine("ImportLocal;20;C2;-500;25000;35000"); // Montant négatif
        csv.AppendLine("ImportLocal;40;C1;133600;50000;67000");

        var service = new ImportateurGrilleTarifaireCsv();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv.ToString()));

        var res = service.AnalyserEtValider(stream);

        Assert.False(res.EstValide);
        Assert.Contains(res.Erreurs, e => e.Contains("Ligne 3") && e.Contains("négatif"));
        Assert.Empty(res.Lignes); // Aucune ligne conservée en cas d'erreur
    }

    [Fact]
    public void Import_csv_doublon_de_cle_rejette_le_fichier()
    {
        var csv = new StringBuilder();
        csv.AppendLine("Sens;Taille;Categorie;Acconage;Relevage;BranchementJour");
        csv.AppendLine("ImportLocal;20;C1;66800;25000;35000");
        csv.AppendLine("ImportLocal;20;C1;70000;25000;35000"); // Même clé composite

        var service = new ImportateurGrilleTarifaireCsv();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv.ToString()));

        var res = service.AnalyserEtValider(stream);

        Assert.False(res.EstValide);
        Assert.Contains(res.Erreurs, e => e.Contains("doublon"));
    }
}
