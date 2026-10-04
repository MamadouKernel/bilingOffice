using CIT.Plateforme.Application.Enrolement;
using CIT.Plateforme.Infrastructure.Documents;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Documents;

/// <summary>Formulaires officiels CIT-CIV-CG-F-0001 (camions) et F-0002 (chauffeurs) produits par YABA.</summary>
public sealed class GenerateurFormulaireFlottePdfTests
{
    private static DemandeFormulaireFlotte Demande(TypeFormulaireFlotte type, string qualite = "Gérant") => new(
        type, "KOUASSI Yao", qualite, "TRANS-LAGUNE LOGISTIQUE", "Abidjan, Vridi",
        ["AB-2210-CI — tracteur Renault T", "AA-4578-CI — tracteur Volvo FH"], new DateTime(2026, 10, 4, 9, 30, 0, DateTimeKind.Utc), "transport@exemple.ci");

    [Theory]
    [InlineData(TypeFormulaireFlotte.AjoutCamion)]
    [InlineData(TypeFormulaireFlotte.RetraitCamion)]
    [InlineData(TypeFormulaireFlotte.AjoutChauffeur)]
    [InlineData(TypeFormulaireFlotte.RetraitChauffeur)]
    public void Chaque_formulaire_officiel_est_produit_en_pdf(TypeFormulaireFlotte type)
    {
        var pdf = new GenerateurFormulaireFlottePdf().Generer(Demande(type));

        Assert.True(pdf.Length > 1000);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public void Un_formulaire_sans_qualite_du_signataire_n_est_pas_produit()
    {
        var erreur = Assert.Throws<ArgumentException>(() => new GenerateurFormulaireFlottePdf().Generer(Demande(TypeFormulaireFlotte.AjoutCamion, qualite: " ")));

        Assert.Contains("qualité", erreur.Message);
    }

    [Theory]
    [InlineData(TypeFormulaireFlotte.AjoutCamion, "CIT-CIV-CG-F-0001 Rev.01", "DEMANDE D'AJOUT DE CAMION")]
    [InlineData(TypeFormulaireFlotte.RetraitChauffeur, "CIT-CIV-CG-F-0002 Rev.01", "DEMANDE DE RETRAIT DE CHAUFFEURS")]
    public void References_et_titres_suivent_les_formulaires_officiels(TypeFormulaireFlotte type, string reference, string titre)
    {
        Assert.Equal(reference, FormulairesFlotte.Reference(type));
        Assert.Equal(titre, FormulairesFlotte.Titre(type));
        Assert.EndsWith(".pdf", FormulairesFlotte.NomFichier(type));
    }
}
