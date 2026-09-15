using CIT.Plateforme.Domain.Documents;

namespace CIT.Plateforme.Domain.Tests.Documents;

/// <summary>
/// REMÉDIATION RECETTE UAT (D-11b).
///
/// La page générique de dépôt de pièces annonçait « Formats acceptés : PDF, JPG, PNG, XLSX » et les
/// refusait tous les quatre : elle déclarait sa liste avec le point (<c>".pdf"</c>) tandis que la
/// règle comparait l'extension du fichier après l'avoir retiré (<c>"pdf"</c>). Aucun test ne
/// pouvait le voir, car tous déclaraient leur liste dans la convention attendue par la règle.
///
/// Ces tests fixent le contrat : <b>les deux membres de la comparaison</b> sont normalisés, et une
/// liste écrite dans l'une ou l'autre convention donne le même résultat.
/// </summary>
public class ExtensionFichierTests
{
    [Theory]
    [InlineData(".pdf", "pdf")]
    [InlineData("pdf", "pdf")]
    [InlineData(".PDF", "pdf")]
    [InlineData("PDF", "pdf")]
    [InlineData(" .Pdf ", "pdf")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Normaliser_ramene_a_la_forme_canonique(string? entree, string attendu) =>
        Assert.Equal(attendu, ExtensionFichier.Normaliser(entree));

    [Theory]
    [InlineData("facture.pdf", "pdf")]
    [InlineData("facture.PDF", "pdf")]
    [InlineData("rapport.final.PDF", "pdf")]
    [InlineData("archive.tar.gz", "gz")]
    [InlineData("sans_extension", "")]
    [InlineData("", "")]
    public void DeNomFichier_retient_le_dernier_segment(string nom, string attendu) =>
        Assert.Equal(attendu, ExtensionFichier.DeNomFichier(nom));

    [Fact]
    public void Une_liste_declaree_avec_le_point_accepte_le_fichier()
    {
        // C'est LE cas qui bloquait tout dépôt de pièce sur un ticket.
        string[] avecPoint = [".pdf", ".jpg", ".jpeg", ".png", ".xlsx"];

        Assert.True(ExtensionFichier.EstAutorisee("facture.pdf", avecPoint));
    }

    [Fact]
    public void Une_liste_declaree_sans_le_point_accepte_le_meme_fichier()
    {
        string[] sansPoint = ["pdf", "jpg", "png"];

        Assert.True(ExtensionFichier.EstAutorisee("facture.pdf", sansPoint));
    }

    [Fact]
    public void La_casse_du_nom_de_fichier_est_indifferente()
    {
        Assert.True(ExtensionFichier.EstAutorisee("FACTURE.PDF", [".pdf"]));
        Assert.True(ExtensionFichier.EstAutorisee("facture.PdF", ["pdf"]));
    }

    [Fact]
    public void Un_nom_a_plusieurs_points_est_juge_sur_son_dernier_segment()
    {
        Assert.True(ExtensionFichier.EstAutorisee("rapport.2026.final.pdf", [".pdf"]));
        Assert.False(ExtensionFichier.EstAutorisee("rapport.pdf.exe", [".pdf"]));
    }

    [Fact]
    public void Une_extension_hors_liste_est_refusee()
    {
        Assert.False(ExtensionFichier.EstAutorisee("script.exe", [".pdf", ".png"]));
        Assert.False(ExtensionFichier.EstAutorisee("archive.zip", ["pdf", "png"]));
    }

    [Fact]
    public void Un_fichier_sans_extension_est_refuse()
    {
        Assert.False(ExtensionFichier.EstAutorisee("sans_extension", [".pdf"]));
        Assert.False(ExtensionFichier.EstAutorisee("", [".pdf"]));
    }

    [Fact]
    public void Enumerer_produit_une_liste_lisible_quelle_que_soit_la_convention_declaree()
    {
        Assert.Equal(".pdf, .jpg, .png", ExtensionFichier.Enumerer([".pdf", "JPG", " .png "]));
    }

    [Fact]
    public void La_regle_de_depot_accepte_une_liste_declaree_avec_le_point()
    {
        // Reproduction exacte de la déclaration de Tickets/Documents.razor.
        var regles = new ReglesDepotDocument([".pdf", ".jpg", ".jpeg", ".png", ".xlsx"], 10 * 1024 * 1024);

        var valide = regles.Valide("piece-jointe.pdf", 1024, out var erreur);

        Assert.True(valide);
        Assert.Null(erreur);
    }

    [Fact]
    public void La_regle_de_depot_nomme_les_formats_acceptes_quand_elle_refuse()
    {
        var regles = new ReglesDepotDocument(["pdf", "png"], 10 * 1024 * 1024);

        var valide = regles.Valide("script.exe", 1024, out var erreur);

        Assert.False(valide);
        Assert.Contains(".exe", erreur);
        Assert.Contains(".pdf", erreur);
        Assert.Contains(".png", erreur);
    }

    [Fact]
    public void La_regle_de_depot_refuse_toujours_un_fichier_trop_lourd()
    {
        // La normalisation d'extension ne doit rien assouplir d'autre.
        var regles = new ReglesDepotDocument([".pdf"], TailleMaxOctets: 100);

        var valide = regles.Valide("facture.pdf", 101, out var erreur);

        Assert.False(valide);
        Assert.Contains("taille maximale", erreur);
    }
}
