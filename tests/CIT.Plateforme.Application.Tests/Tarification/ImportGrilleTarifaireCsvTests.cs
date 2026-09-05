using System.Text;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tarification;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

using A = CIT.Plateforme.Domain.Tarification.ParametrageInitialAnnexeA;

namespace CIT.Plateforme.Application.Tests.Tarification;

public sealed class ImportGrilleTarifaireCsvTests
{
    private const string Entete = "Sens;Taille;Categorie;Acconage;Relevage;BranchementJour";

    private static MemoryStream Flux(string contenu) => new(Encoding.UTF8.GetBytes(contenu));

    [Fact]
    public async Task Import_csv_valide_cree_version_et_ses_lignes()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        ReferentielsDeTest.Amorcer(db);
        var repo = new VersionGrilleTarifaireRepository(db);

        var csv = new StringBuilder();
        csv.AppendLine(Entete);
        csv.AppendLine($"{A.SensImportLocal};20;C1;66800;25000;35000");
        csv.AppendLine($"{A.SensImportLocal};40;C1;133600;50000;67000");

        var service = new ImportateurGrilleTarifaireCsv();
        using var stream = Flux(csv.ToString());

        var resultat = service.AnalyserEtValider(stream);

        Assert.True(resultat.EstValide);
        Assert.Empty(resultat.Erreurs);
        Assert.Equal(2, resultat.Lignes.Count);

        var administrateur = Guid.NewGuid();
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, administrateur), Permission.TarificationAdministrer));
        await db.SaveChangesAsync();

        var handler = new ImporterGrilleTarifaireCsvCommandHandler(
            repo, new ReferentielsTarifairesRepository(db), service, new AutorisationService(db));

        // Paramètres non renseignés : la commande applique le paramétrage initial de l'Annexe A.
        var commande = new ImporterGrilleTarifaireCsvCommande(
            administrateur, "v2.0", DateTime.UtcNow.AddDays(10), Flux(csv.ToString()));

        var reponse = await handler.HandleAsync(commande);
        Assert.True(reponse.Succes);
        Assert.Equal(2, reponse.NombreLignesImportees);

        var persistee = await repo.ObtenirParIdAsync(reponse.VersionId!.Value);
        Assert.NotNull(persistee);
        Assert.Equal("v2.0", persistee.NumeroVersion);
        Assert.Equal(2, persistee.Lignes.Count);

        // La grille importée porte bien les paramètres de l'Annexe A : sans eux, le moteur
        // calculerait des montants nuls en silence.
        Assert.Equal(3, persistee.Tranches.Count);
        Assert.Equal(2, persistee.SeuilsPoids.Count);
        Assert.Equal(3, persistee.QuotesParts.Count);
    }

    /// <summary>
    /// La catégorie était un texte libre : n'importe quel libellé entrait dans la grille, et
    /// n'était jamais retrouvé au moment d'une simulation. Elle est désormais résolue contre le
    /// référentiel, et un code inconnu annule l'import entier (CLAR-SIM-01).
    /// </summary>
    [Fact]
    public async Task Import_csv_avec_categorie_inconnue_est_rejete_en_entier()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        ReferentielsDeTest.Amorcer(db);
        var repo = new VersionGrilleTarifaireRepository(db);

        var administrateur = Guid.NewGuid();
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, administrateur), Permission.TarificationAdministrer));
        await db.SaveChangesAsync();

        var csv = new StringBuilder();
        csv.AppendLine(Entete);
        csv.AppendLine($"{A.SensImportLocal};20;C1;66800;25000;35000");
        csv.AppendLine($"{A.SensImportLocal};20;C99_INCONNUE;70000;25000;35000");

        var handler = new ImporterGrilleTarifaireCsvCommandHandler(
            repo, new ReferentielsTarifairesRepository(db), new ImportateurGrilleTarifaireCsv(), new AutorisationService(db));

        var reponse = await handler.HandleAsync(new ImporterGrilleTarifaireCsvCommande(
            administrateur, "v3.0", DateTime.UtcNow, Flux(csv.ToString())));

        Assert.False(reponse.Succes);
        Assert.Null(reponse.VersionId);
        Assert.Contains(reponse.Erreurs, e => e.Contains("C99_INCONNUE"));
        Assert.Empty(await repo.ListerToutesAsync());
    }

    /// <summary>Une catégorie valide, mais rattachée à un autre sens — C4 n'existe qu'en import
    /// transit (Annexe A.6). L'import doit la refuser plutôt que de la tarifer en import local.</summary>
    [Fact]
    public async Task Import_csv_avec_categorie_dun_autre_sens_est_rejete()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        ReferentielsDeTest.Amorcer(db);
        var repo = new VersionGrilleTarifaireRepository(db);

        var administrateur = Guid.NewGuid();
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, administrateur), Permission.TarificationAdministrer));
        await db.SaveChangesAsync();

        var csv = new StringBuilder();
        csv.AppendLine(Entete);
        csv.AppendLine($"{A.SensImportLocal};20;C4;66800;25000;35000");

        var handler = new ImporterGrilleTarifaireCsvCommandHandler(
            repo, new ReferentielsTarifairesRepository(db), new ImportateurGrilleTarifaireCsv(), new AutorisationService(db));

        var reponse = await handler.HandleAsync(new ImporterGrilleTarifaireCsvCommande(
            administrateur, "v3.0", DateTime.UtcNow, Flux(csv.ToString())));

        Assert.False(reponse.Succes);
        Assert.Contains(reponse.Erreurs, e => e.Contains("C4") && e.Contains(A.SensImportLocal));
    }

    [Fact]
    public void Import_csv_entete_invalide_retourne_erreur_bloquante()
    {
        using var stream = Flux($"Sens;Taille;Categorie;MontantFaux\n{A.SensImportLocal};20;C1;1000");

        var res = new ImportateurGrilleTarifaireCsv().AnalyserEtValider(stream);

        Assert.False(res.EstValide);
        Assert.Contains(res.Erreurs, e => e.Contains("En-tête CSV invalide"));
    }

    [Fact]
    public void Import_csv_ligne_incorrecte_au_milieu_rejette_tout_le_fichier()
    {
        var csv = new StringBuilder();
        csv.AppendLine(Entete);
        csv.AppendLine($"{A.SensImportLocal};20;C1;66800;25000;35000");
        csv.AppendLine($"{A.SensImportLocal};20;C2;-500;25000;35000"); // Montant négatif
        csv.AppendLine($"{A.SensImportLocal};40;C1;133600;50000;67000");

        using var stream = Flux(csv.ToString());

        var res = new ImportateurGrilleTarifaireCsv().AnalyserEtValider(stream);

        Assert.False(res.EstValide);
        Assert.Contains(res.Erreurs, e => e.Contains("Ligne 3") && e.Contains("négatif"));
        Assert.Empty(res.Lignes); // Aucune ligne conservée en cas d'erreur
    }

    [Fact]
    public void Import_csv_doublon_de_cle_rejette_le_fichier()
    {
        var csv = new StringBuilder();
        csv.AppendLine(Entete);
        csv.AppendLine($"{A.SensImportLocal};20;C1;66800;25000;35000");
        csv.AppendLine($"{A.SensImportLocal};20;C1;70000;25000;35000"); // Même clé composite

        using var stream = Flux(csv.ToString());

        var res = new ImportateurGrilleTarifaireCsv().AnalyserEtValider(stream);

        Assert.False(res.EstValide);
        Assert.Contains(res.Erreurs, e => e.Contains("doublon"));
    }
}
